using System.Net;
using System.Text;
using CimarFunctions.Models;
using CimarFunctions.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CimarFunctions.Services.Alerts;

/// <summary>
/// Surveille les camions restés trop longtemps dans l'usine (de la 1ère pesée jusqu'à la
/// 2ème pesée) et envoie UN seul e-mail (Azure AD / Graph) au-delà de
/// <see cref="OverstayAlertRules.AlertThresholdMinutes"/> minutes.
/// Dédoublonnage par camion via le journal Ecare_Overstay_Alert_Log (palier 90).
/// </summary>
public sealed class OverstayAlertService : IOverstayAlertService
{
    private readonly IOverstayAlertRepository _repository;
    private readonly IEmailSender _email;
    private readonly OverstayAlertOptions _opt;
    private readonly ILogger<OverstayAlertService> _logger;

    public OverstayAlertService(
        IOverstayAlertRepository repository,
        IEmailSender email,
        IOptions<OverstayAlertOptions> opt,
        ILogger<OverstayAlertService> logger)
    {
        _repository = repository;
        _email = email;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task RunScanAsync(CancellationToken cancellationToken = default)
    {
        if (!_opt.Enabled)
        {
            _logger.LogInformation("OverstayAlert désactivé (OverstayAlert:Enabled = false).");
            return;
        }

        var (to, cc) = OverstayAlertRules.SplitRecipients(_opt.Recipients);
        if (string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("Aucun destinataire configuré (OverstayAlert:Recipients) — scan ignoré.");
            return;
        }

        // Maroc revenu définitivement à GMT le 20/09/2026 (décret 2.26.530) : heure Maroc = UTC.
        // Ne pas passer par TimeZoneInfo : les données de fuseau du serveur peuvent encore dire GMT+1,
        // alors que PabEntryAt est horodaté par Azure SQL (déjà en GMT).
        var nowLocal = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var ignoreBefore = _opt.IgnoreEntriesBeforeLocal;

        const int threshold = OverstayAlertRules.AlertThresholdMinutes;
        var rows = await _repository.GetOverstayCandidatesAsync(nowLocal, threshold, ignoreBefore, cancellationToken);
        if (rows.Count == 0) return;

        foreach (var r in rows)
        {
            // Les anciennes alertes 75 min (avant le 23/10/2026) ne bloquent pas : seul le palier 90 compte.
            var alreadyAlerted = ParseStages(r.SentStagesCsv).Contains(threshold);
            if (!OverstayAlertRules.IsAlertDue(r.PabEntryAt, null, nowLocal, alreadyAlerted, ignoreBefore))
                continue;

            var claimed = await _repository.TryClaimStageAsync(r.Id, threshold, nowLocal, cancellationToken);
            if (!claimed) continue; // déjà alerté par une exécution concurrente

            var elapsed = OverstayAlertRules.ElapsedMinutes(r.PabEntryAt, nowLocal);
            var subject = $"⚠ Camion en dépassement (> {threshold} min) — {r.Matricule ?? "?"} — {elapsed} min dans l'usine";
            var body = BuildHtmlBody(r, threshold, elapsed, nowLocal);

            var ok = await _email.SendHtmlAsync(_opt.Sender, to, cc, subject, body, highPriority: true, cancellationToken);
            if (!ok)
                _logger.LogWarning("Alerte {Threshold} min non délivrée pour la legend {LegendId} ({Matricule}).",
                    threshold, r.Id, r.Matricule);
        }
    }

    private static ISet<int> ParseStages(string? csv)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(csv)) return set;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (int.TryParse(part, out var v)) set.Add(v);
        return set;
    }

    private string BuildHtmlBody(OverstayRow r, int stageMinutes, int elapsedMinutes, DateTime nowLocal)
    {
        string E(string? s) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(s) ? "—" : s);
        string Article(string? produit, decimal? qte)
        {
            if (string.IsNullOrWhiteSpace(produit)) return "—";
            var q = qte.HasValue ? qte.Value.ToString("0.##") : "—";
            return $"{E(produit)} — {q}";
        }

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><body style=\"font-family:Segoe UI,Arial,sans-serif;\">");
        sb.Append("<h2 style=\"color:#b00020;\">⚠ Camion en dépassement dans l'usine</h2>");
        sb.Append($"<p>Le camion <b>{E(r.Matricule)}</b> est dans l'usine depuis <b>{elapsedMinutes} minutes</b> " +
                  $"(palier <b>{stageMinutes} min</b> franchi), sans 2ème pesée enregistrée.</p>");
        sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
        Row(sb, "Matricule", E(r.Matricule));
        Row(sb, "Carte RFID", E(r.RFIDCard));
        Row(sb, "Transporteur", E(r.TransporteurName));
        Row(sb, "Chauffeur", E(r.ChauffeurName));
        Row(sb, "Client", E(r.ClientName));
        Row(sb, "Chantier", E(r.Chantier));
        Row(sb, "Article 1", Article(r.Produit1, r.Quantite1));
        if (!string.IsNullOrWhiteSpace(r.Produit2))
            Row(sb, "Article 2", Article(r.Produit2, r.Quantite2));
        Row(sb, "Entrée pont bascule (scan)", r.PabEntryAt.ToString("dd/MM/yyyy HH:mm"));
        Row(sb, "Durée dans l'usine", $"{elapsedMinutes} min");
        sb.Append("</table>");
        sb.Append($"<p><small>Alerte générée le {nowLocal:dd/MM/yyyy HH:mm} (heure Maroc) — ID legend {r.Id}.</small></p>");
        sb.Append("</body></html>");
        return sb.ToString();

        static void Row(StringBuilder b, string k, string v)
            => b.Append($"<tr><td style=\"background:#f2f2f2;\"><b>{k}</b></td><td>{v}</td></tr>");
    }
}
