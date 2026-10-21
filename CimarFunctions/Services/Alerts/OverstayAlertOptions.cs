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

    /// <summary>
    /// Plancher anti-fantômes par défaut, codé en dur faute d'accès au portail Azure.
    /// Surchargeable sans redéploiement via le réglage d'application
    /// <c>OverstayAlert__IgnoreEntriesBeforeLocal</c>.
    /// </summary>
    public static readonly DateTime DefaultIgnoreEntriesBeforeLocal = new(2026, 10, 22, 0, 0, 0);

    /// <summary>
    /// Plancher anti-fantômes : on ignore tout camion dont la 1ère pesée (PabEntryAt) est
    /// antérieure à cette date. Évite d'alerter sur les enregistrements jamais clôturés
    /// (2ème pesée faite manuellement / hors système, le camion est parti depuis longtemps).
    /// Null = pas de plancher. Heure Maroc (= GMT depuis le 20/09/2026).
    /// </summary>
    public DateTime? IgnoreEntriesBeforeLocal { get; set; } = DefaultIgnoreEntriesBeforeLocal;
}
