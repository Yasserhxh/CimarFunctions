namespace CimarFunctions.Services.Sync;

public interface IOrderDeliveredSyncService
{
    Task SyncDeliveredStatusAsync(CancellationToken cancellationToken = default);
}
