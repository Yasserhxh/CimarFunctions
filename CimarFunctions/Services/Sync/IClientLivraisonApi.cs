using CimarFunctions.Models;

namespace CimarFunctions.Services.Sync;

public interface IClientLivraisonApi
{
    Task<IReadOnlyList<ExternalLivraisonModel>> GetLivraisonsAsync(
        string customerNumber,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
