using CimarFunctions.Models;

namespace CimarFunctions.Services.Sync;

public interface IOrderLegendSyncRepository
{
    Task EnsureLowCreditDeliveryRiskColumnAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingOrderSyncModel>> GetPendingOrdersAsync(
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsSyncedAsync(
        long id,
        string bonDeLivraison,
        CancellationToken cancellationToken = default);
}
