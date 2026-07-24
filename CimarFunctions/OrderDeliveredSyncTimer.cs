using CimarFunctions.Services.Sync;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CimarFunctions;

public sealed class OrderDeliveredSyncTimer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderDeliveredSyncTimer> _logger;

    public OrderDeliveredSyncTimer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderDeliveredSyncTimer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Every 15 minutes, aligned with the existing external-delivery sync cadence.
    [Function(nameof(OrderDeliveredSyncTimer))]
    public async Task Run(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Delivered status sync timer fired at {Now}. Next={NextRun}",
            DateTime.Now,
            timer.ScheduleStatus?.Next);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOrderDeliveredSyncService>();

            await service.SyncDeliveredStatusAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Delivered status sync timer cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled delivered status sync timer error.");
            throw;
        }
    }
}
