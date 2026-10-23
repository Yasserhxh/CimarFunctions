namespace CimarFunctions.Services.Alerts;

/// <summary>
/// Règles pures (testables sans DB) de l'alerte « camion trop longtemps dans l'usine ».
/// « Dans l'usine » = de la 1ère pesée (PabEntryAt renseigné) jusqu'à la 2ème pesée
/// (DeuxiemePoid renseigné). PabEntryAt est stocké en heure Maroc par le kiosk : la
/// comparaison doit donc se faire avec un « maintenant » lui aussi en heure Maroc.
/// </summary>
public static class OverstayAlertRules
{
    /// <summary>
    /// Seuil unique d'alerte (demande client du 23/10/2026 : un seul e-mail par camion).
    /// Codé en dur volontairement : non surchargeable par les app settings Azure.
    /// </summary>
    public const int AlertThresholdMinutes = 90;

    /// <summary>CRON du TimerTrigger : un scan toutes les 15 minutes.</summary>
    public const string ScanSchedule = "0 */15 * * * *";

    /// <summary>
    /// Vrai si le camion doit être alerté maintenant : entré (1ère pesée), pas encore sorti
    /// (pas de 2ème pesée), au-delà du seuil et pas encore alerté. Le dépassement est
    /// strict : à 90 min pile, pas encore d'alerte.
    /// </summary>
    public static bool IsAlertDue(
        DateTime? pabEntryAt,
        int? deuxiemePoid,
        DateTime nowLocal,
        bool alreadyAlerted,
        DateTime? ignoreBefore = null)
    {
        if (alreadyAlerted || pabEntryAt is null || deuxiemePoid is not null)
            return false;

        // Plancher anti-fantômes : les enregistrements entrés avant cette date (2e pesée
        // faite manuellement / hors système, legend jamais clôturée) ne sont jamais alertés.
        if (ignoreBefore is not null && pabEntryAt.Value < ignoreBefore.Value)
            return false;

        return ElapsedMinutes(pabEntryAt.Value, nowLocal) > AlertThresholdMinutes;
    }

    /// <summary>Minutes écoulées depuis la 1ère pesée, arrondies à la minute.</summary>
    public static int ElapsedMinutes(DateTime pabEntryAt, DateTime nowLocal)
        => (int)Math.Round((nowLocal - pabEntryAt).TotalMinutes);

    /// <summary>
    /// Dédoublonne (insensible à la casse, en préservant la 1ère occurrence),
    /// ignore les entrées vides, puis renvoie le 1er destinataire en TO et les
    /// suivants en CC (joints par des virgules, ou null si aucun).
    /// </summary>
    public static (string To, string? Cc) SplitRecipients(IEnumerable<string> recipients)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clean = new List<string>();
        foreach (var raw in recipients ?? Enumerable.Empty<string>())
        {
            var e = raw?.Trim();
            if (string.IsNullOrEmpty(e)) continue;
            if (seen.Add(e)) clean.Add(e);
        }

        if (clean.Count == 0) return (string.Empty, null);
        var to = clean[0];
        var cc = clean.Count > 1 ? string.Join(",", clean.Skip(1)) : null;
        return (to, cc);
    }
}
