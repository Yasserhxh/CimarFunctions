namespace CimarFunctions.Models;

/// <summary>Row from dbo.Orders used by the unconfirmed-order auto-cancel job.</summary>
public sealed class UnconfirmedOrderModel
{
    public int Id { get; set; }
    public string? Statut { get; set; }
    public DateTime DateCommande { get; set; }
}
