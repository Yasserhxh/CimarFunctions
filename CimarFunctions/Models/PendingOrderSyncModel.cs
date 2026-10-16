namespace CimarFunctions.Models;

public sealed class PendingOrderSyncModel
{
    public int Id { get; set; }
    public string? CodeSapClient { get; set; }
    public string? CodeSapCommande { get; set; }
    /// <summary>COALESCE(CreatedAt, ParkingAt, AddedToQueueAt, DateAffectation) — see PendingLegendSyncPolicy.</summary>
    public DateTime? ReferenceDate { get; set; }
}
