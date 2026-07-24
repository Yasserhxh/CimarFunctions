using CimarFunctions.Models;

namespace CimarFunctions.Services.Sync;

public interface IUnconfirmedOrderCancelRepository
{
    /// <summary>Unconfirmed ("Crée") orders created at or before <paramref name="cutoff"/>.</summary>
    Task<IReadOnlyList<UnconfirmedOrderModel>> GetStaleUnconfirmedOrdersAsync(
        DateTime cutoff,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Sets Statut='Annulée' for the given ids that are still 'Crée'. Returns rows affected.</summary>
    Task<int> CancelOrdersAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);
}
