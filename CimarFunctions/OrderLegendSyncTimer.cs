using CimarFunctions.Services.Sync;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CimarFunctions;

public sealed class OrderLegendSyncTimer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderLegendSyncTimer> _logger;

    public OrderLegendSyncTimer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderLegendSyncTimer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [Function(nameof(OrderLegendSyncTimer))]
    public async Task Run(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order legend sync timer fired at {UtcNow}. Next={NextRun}",
            DateTime.UtcNow,
            timer.ScheduleStatus?.Next);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExternalDeliverySyncService>();

            await service.SyncAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Order legend sync timer cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled order legend sync timer error.");
            throw;
        }
    }
}
