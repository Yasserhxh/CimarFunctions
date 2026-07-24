namespace CimarFunctions.Services.Sync;

public interface IUnconfirmedOrderCancelService
{
    Task CancelStaleUnconfirmedOrdersAsync(CancellationToken cancellationToken = default);
}
