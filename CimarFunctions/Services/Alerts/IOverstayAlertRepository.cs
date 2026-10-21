using CimarFunctions.Models;

namespace CimarFunctions.Services.Alerts;

public interface IOverstayAlertRepository
{
    /// <summary>
    /// Camions entrés (1ère pesée) et pas encore sortis (2ème pesée), non annulés/non clôturés,
    /// au-delà du plus petit palier. Chaque ligne porte les paliers déjà notifiés (SentStagesCsv).
    /// </summary>
    Task<IReadOnlyList<OverstayRow>> GetOverstayCandidatesAsync(
        DateTime nowLocal,
        int minThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Réserve (claim) l'envoi d'un palier pour un camion par insertion dans le journal.
    /// Idempotent : renvoie false si le palier était déjà réservé (dédoublonne même en
    /// multi-instances / multi-ticks). PK = (LegendId, ThresholdMinutes).
    /// </summary>
    Task<bool> TryClaimStageAsync(
        int legendId,
        int thresholdMinutes,
        DateTime sentAt,
        CancellationToken cancellationToken = default);
}
