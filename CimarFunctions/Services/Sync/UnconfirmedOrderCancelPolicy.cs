namespace CimarFunctions.Services.Sync;

/// <summary>
/// Decides whether a client order (dbo.Orders) that was never confirmed must be
/// auto-cancelled. A "Crée" order older than <c>delayDays</c> days is cancelled
/// locally (status only, no SAP call). Applies to both Départ and Rendu.
/// </summary>
public static class UnconfirmedOrderCancelPolicy
{
    public const string UnconfirmedStatus = "Crée";
    public const string CanceledStatus = "Annulée";
    public const int DefaultDelayDays = 10;

    /// <summary>Orders created at or before this instant are considered stale.</summary>
    public static DateTime CutoffFor(DateTime now, int delayDays)
        => now.AddDays(-delayDays);

    public static bool IsEligible(string? statut, DateTime dateCommande, DateTime now, int delayDays)
        => string.Equals(statut?.Trim(), UnconfirmedStatus, StringComparison.OrdinalIgnoreCase)
           && dateCommande <= CutoffFor(now, delayDays);
}
