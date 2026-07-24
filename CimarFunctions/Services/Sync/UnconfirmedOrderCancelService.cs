using Microsoft.Extensions.Logging;

namespace CimarFunctions.Services.Sync;

/// <summary>
/// Auto-cancels client orders (dbo.Orders) that were never confirmed within
/// <see cref="UnconfirmedOrderCancelPolicy.DefaultDelayDays"/> days of creation.
/// Local status change only (no SAP call). Applies to both Départ and Rendu.
/// </summary>
public sealed class UnconfirmedOrderCancelService : IUnconfirmedOrderCancelService
{
    private const string LockName = "Orders_UnconfirmedAutoCancel";
    private const int BatchSize = 500;

    private readonly IUnconfirmedOrderCancelRepository _repository;
    private readonly ISyncExecutionLockProvider _lockProvider;
    private readonly ILogger<UnconfirmedOrderCancelService> _logger;

    public UnconfirmedOrderCancelService(
        IUnconfirmedOrderCancelRepository repository,
        ISyncExecutionLockProvider lockProvider,
        ILogger<UnconfirmedOrderCancelService> logger)
    {
        _repository = repository;
        _lockProvider = lockProvider;
        _logger = logger;
    }

    public async Task CancelStaleUnconfirmedOrdersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var days = UnconfirmedOrderCancelPolicy.DefaultDelayDays;
        var cutoff = UnconfirmedOrderCancelPolicy.CutoffFor(now, days);

        _logger.LogInformation(
            "Unconfirmed-order auto-cancel started at {Now}. Cutoff={Cutoff} ({Days} days).",
            now, cutoff, days);

        await using var syncLock = await _lockProvider.TryAcquireAsync(LockName, cancellationToken);
        if (!syncLock.Acquired)
        {
            _logger.LogInformation("Auto-cancel skipped because another instance is already running.");
            return;
        }

        var candidates = await _repository.GetStaleUnconfirmedOrdersAsync(cutoff, BatchSize, cancellationToken);

        // Authoritative per-row check (defense in depth over the SQL WHERE).
        var eligibleIds = candidates
            .Where(o => UnconfirmedOrderCancelPolicy.IsEligible(o.Statut, o.DateCommande, now, days))
            .Select(o => o.Id)
            .ToList();

        if (eligibleIds.Count == 0)
        {
            _logger.LogInformation("No stale unconfirmed orders to cancel.");
            return;
        }

        var affected = await _repository.CancelOrdersAsync(eligibleIds, cancellationToken);

        _logger.LogInformation(
            "Auto-cancelled {Affected} unconfirmed order(s): [{Ids}]",
            affected, string.Join(", ", eligibleIds));
    }
}
