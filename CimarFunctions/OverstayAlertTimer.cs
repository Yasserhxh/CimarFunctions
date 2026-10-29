using CimarFunctions.Services.Alerts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CimarFunctions;

public sealed class OverstayAlertTimer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverstayAlertTimer> _logger;

    public OverstayAlertTimer(
        IServiceScopeFactory scopeFactory,
        ILogger<OverstayAlertTimer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Runs every minute (OverstayAlertRules.ScanSchedule), hardcoded on purpose.
    [Function(nameof(OverstayAlertTimer))]
    public async Task Run(
        [TimerTrigger(OverstayAlertRules.ScanSchedule)] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Overstay alert timer fired at {Now}. Next={NextRun}",
            DateTime.Now,
            timer.ScheduleStatus?.Next);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOverstayAlertService>();

            await service.RunScanAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Overstay alert timer cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled overstay alert timer error.");
            throw;
        }
    }
}
