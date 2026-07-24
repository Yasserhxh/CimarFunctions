namespace CimarFunctions.Models;

/// <summary>Row from dbo.Orders used by the "Livrée" status-sync job.</summary>
public sealed class ConfirmedOrderModel
{
    public int Id { get; set; }
    public string? NumeroSap { get; set; }
    public string? Statut { get; set; }
}
