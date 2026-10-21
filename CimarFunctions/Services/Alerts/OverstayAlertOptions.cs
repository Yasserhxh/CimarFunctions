namespace CimarFunctions.Services.Alerts;

/// <summary>
/// Configuration de l'alerte « camion trop longtemps dans l'usine »
/// (section "OverstayAlert" des app settings).
/// </summary>
public sealed class OverstayAlertOptions
{
    /// <summary>Active/désactive l'alerte.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Paliers d'alerte, en minutes (une alerte par palier et par camion).
    /// Défaut : 75 min puis 90 min.
    /// </summary>
    public List<int> Thresholds { get; set; } = new() { 75, 90 };

    /// <summary>
    /// Fréquence de scan, en minutes. Informatif : la cadence réelle est fixée par
    /// le CRON du TimerTrigger (voir OverstayAlertTimer). Conservé pour cohérence
    /// avec l'ancienne config du web app.
    /// </summary>
    public int PollIntervalMinutes { get; set; } = 5;

    /// <summary>Boîte expéditrice (Azure AD / Graph).</summary>
    public string Sender { get; set; } = "cimar.noreplay@cimar.co.ma";

    /// <summary>Liste de diffusion : 1er = TO, les suivants = CC (dédoublonnés).</summary>
    public List<string> Recipients { get; set; } = new();
}
