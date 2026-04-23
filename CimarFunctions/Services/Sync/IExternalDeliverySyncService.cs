namespace CimarFunctions.Services.Sync;

public interface IExternalDeliverySyncService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}
