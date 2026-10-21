namespace CimarFunctions.Models;

/// <summary>
/// Camion candidat à l'alerte dépassement : ligne de dbo.Ecare_Order_Legend entrée
/// (1ère pesée) mais pas encore sortie (2ème pesée), enrichie des paliers déjà notifiés.
/// </summary>
public sealed class OverstayRow
{
    public int Id { get; set; }
    public string? ClientName { get; set; }
    public string? Chantier { get; set; }
    public string? Matricule { get; set; }
    public string? RFIDCard { get; set; }
    public string? TransporteurName { get; set; }
    public string? ChauffeurName { get; set; }
    public string? Produit1 { get; set; }
    public decimal? Quantite1 { get; set; }
    public string? Produit2 { get; set; }
    public decimal? Quantite2 { get; set; }
    public DateTime PabEntryAt { get; set; }

    /// <summary>Paliers (minutes) déjà notifiés pour ce camion, CSV (ex. "75,90").</summary>
    public string? SentStagesCsv { get; set; }
}
