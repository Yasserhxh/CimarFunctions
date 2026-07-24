using Microsoft.Extensions.Logging;

namespace CimarFunctions.Services.Sync;

/// <summary>
/// Reflects the real Ecare expedition-flow state onto client orders: a confirmed
/// order (dbo.Orders) becomes "Livrée" once its linked Ecare_Order_Legend carries
/// a BonDeLivraison (Step 5). Because dbo.Orders.Statut is the single source of the
/// status shown to both the commercial desk and the client account, one update
/// propagates to both views.
/// </summary>
public sealed class OrderDeliveredSyncService : IOrderDeliveredSyncService
{
    private const string LockName = "Orders_DeliveredStatusSync";
    private const int BatchSize = 500;

    private readonly IOrderDeliveredSyncRepository _repository;
    private readonly ISyncExecutionLockProvider _lockProvider;
    private readonly ILogger<OrderDeliveredSyncService> _logger;

    public OrderDeliveredSyncService(
        IOrderDeliveredSyncRepository repository,
        ISyncExecutionLockProvider lockProvider,
        ILogger<OrderDeliveredSyncService> logger)
    {
        _repository = repository;
        _lockProvider = lockProvider;
        _logger = logger;
    }

    public async Task SyncDeliveredStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Delivered status sync started at {Now}", DateTime.Now);

        await using var syncLock = await _lockProvider.TryAcquireAsync(LockName, cancellationToken);
        if (!syncLock.Acquired)
        {
            _logger.LogInformation("Delivered status sync skipped because another instance is already running.");
            return;
        }

        var confirmed = await _repository.GetConfirmedOrdersAwaitingDeliveryAsync(BatchSize, cancellationToken);
        if (confirmed.Count == 0)
        {
            _logger.LogInformation("No confirmed orders awaiting delivery.");
            return;
        }

        // Numeric, zero-pad-normalized candidate sales docs for the SQL pre-filter.
        var candidateNumbers = new HashSet<long>();
        foreach (var order in confirmed)
        {
            var norm = OrderDeliveredSyncPolicy.NormalizeSapDoc(order.NumeroSap);
            if (norm is not null && long.TryParse(norm, out var num))
                candidateNumbers.Add(num);
        }

        if (candidateNumbers.Count == 0)
        {
            _logger.LogInformation("No numeric SAP sales documents among confirmed orders.");
            return;
        }

        var deliveredDocs = await _repository.GetDeliveredSalesDocumentsAsync(candidateNumbers, cancellationToken);

        var deliveredNormalized = deliveredDocs
            .Select(OrderDeliveredSyncPolicy.NormalizeSapDoc)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToHashSet();

        var idsToMark = new List<int>();
        foreach (var order in confirmed)
        {
            var norm = OrderDeliveredSyncPolicy.NormalizeSapDoc(order.NumeroSap);
            var hasBonDeLivraison = norm is not null && deliveredNormalized.Contains(norm);

            if (OrderDeliveredSyncPolicy.ShouldMarkDelivered(order.Statut, hasBonDeLivraison))
                idsToMark.Add(order.Id);
        }

        if (idsToMark.Count == 0)
        {
            _logger.LogInformation("No confirmed order reached delivery yet.");
            return;
        }

        var affected = await _repository.MarkOrdersDeliveredAsync(idsToMark, cancellationToken);

        _logger.LogInformation(
            "Marked {Affected} order(s) as Livrée: [{Ids}]",
            affected, string.Join(", ", idsToMark));
    }
}
