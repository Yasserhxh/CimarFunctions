using CimarFunctions.Services.Sync;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CimarFunctions;

public sealed class UnconfirmedOrderCancelTimer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnconfirmedOrderCancelTimer> _logger;

    public UnconfirmedOrderCancelTimer(
        IServiceScopeFactory scopeFactory,
        ILogger<UnconfirmedOrderCancelTimer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Runs once a day at 02:00 (server time).
    [Function(nameof(UnconfirmedOrderCancelTimer))]
    public async Task Run(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Unconfirmed-order auto-cancel timer fired at {Now}. Next={NextRun}",
            DateTime.Now,
            timer.ScheduleStatus?.Next);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IUnconfirmedOrderCancelService>();

            await service.CancelStaleUnconfirmedOrdersAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Unconfirmed-order auto-cancel timer cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled unconfirmed-order auto-cancel timer error.");
            throw;
        }
    }
}
