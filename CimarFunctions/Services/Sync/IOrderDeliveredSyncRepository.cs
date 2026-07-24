using CimarFunctions.Models;

namespace CimarFunctions.Services.Sync;

public interface IOrderDeliveredSyncRepository
{
    /// <summary>Confirmed orders that carry a SAP sales document and are not yet Livrée.</summary>
    Task<IReadOnlyList<ConfirmedOrderModel>> GetConfirmedOrdersAwaitingDeliveryAsync(
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Of the given candidate sales-document numbers (leading zeros already stripped,
    /// as BIGINT), returns the ones whose Ecare_Order_Legend has a BonDeLivraison
    /// (Step 5, not commercially cancelled) — i.e. really delivered.
    /// </summary>
    Task<IReadOnlyList<string>> GetDeliveredSalesDocumentsAsync(
        IReadOnlyCollection<long> candidateDocNumbers,
        CancellationToken cancellationToken = default);

    /// <summary>Sets Statut='Livrée' for the given ids that are still 'Confirmée'. Returns rows affected.</summary>
    Task<int> MarkOrdersDeliveredAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);
}
