namespace CimarFunctions.Services.Alerts;

/// <summary>
/// Paramètres de l'alerte « camion trop longtemps dans l'usine », codés en dur
/// (demande du 23/10/2026) : ils ne sont plus lus depuis local.settings.json ni depuis
/// les app settings Azure. Seuil et cadence : voir <see cref="OverstayAlertRules"/>.
/// Les identifiants AzureAdMail restent en configuration (secrets).
/// </summary>
public static class OverstayAlertSettings
{
    /// <summary>Boîte expéditrice (Azure AD / Graph).</summary>
    public const string Sender = "cimar.noreplay@cimar.co.ma";

    /// <summary>Destinataires principaux (TO).</summary>
    public static readonly IReadOnlyList<string> ToRecipients = Array.AsReadOnly(new[]
    {
        "mohamed.elrhattaoui@heidelbergmaterials.com",
        "ismail.chakra@heidelbergmaterials.com",
    });

    /// <summary>Destinataires en copie (CC).</summary>
    public static readonly IReadOnlyList<string> CcRecipients = Array.AsReadOnly(new[]
    {
        "abdelkarim.mezianemtalsi@heidelbergmaterials.com",
        "mohamedkhalil.kada@heidelbergmaterials.com",
        "abderrahman.louza@heidelbergmaterials.com",
        "abd-ettaouab.elhrarti@heidelbergmaterials.com",
        "m.oufkir@cimar.co.ma",
        "mohamed.aggoujil@heidelbergmaterials.com",
        "adnane.elbakhri@heidelbergmaterials.com",
        "safaa.elbouabidi.ext@cimar.co.ma",
        "oussama.elhilali.ext@cimar.co.ma",
        "souhail.elbasett@heidelbergmaterials.com",
    });

    /// <summary>
    /// Plancher anti-fantômes : on ignore tout camion dont la 1ère pesée (PabEntryAt) est
    /// antérieure à cette date. Évite d'alerter sur les enregistrements jamais clôturés
    /// (2ème pesée faite manuellement / hors système). Heure Maroc (= GMT depuis le 20/09/2026).
    /// </summary>
    public static readonly DateTime IgnoreEntriesBeforeLocal = new(2026, 10, 22, 0, 0, 0);
}
