using CimarFunctions.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CimarFunctions.Services.Alerts;

public sealed class OverstayAlertRepository : IOverstayAlertRepository
{
    private readonly string _connectionString;

    public OverstayAlertRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");
    }

    public async Task<IReadOnlyList<OverstayRow>> GetOverstayCandidatesAsync(
        DateTime nowLocal,
        int minThreshold,
        CancellationToken cancellationToken = default)
    {
        // Camions entrés (1ère pesée), pas encore sortis (2ème pesée), non annulés, au-delà
        // du plus petit palier. SentStagesCsv = paliers déjà notifiés pour ce camion.
        const string sql = """
            SELECT l.Id, l.ClientName, l.Chantier, l.Matricule, l.RFIDCard,
                   l.TransporteurName, l.ChauffeurName,
                   l.Produit1, l.Quantite1, l.Produit2, l.Quantite2, l.PabEntryAt,
                   (SELECT STRING_AGG(CAST(a.ThresholdMinutes AS varchar(10)), ',')
                      FROM dbo.Ecare_Overstay_Alert_Log a WHERE a.LegendId = l.Id) AS SentStagesCsv
            FROM dbo.Ecare_Order_Legend l
            WHERE l.PabEntryAt IS NOT NULL
              AND l.DeuxiemePoid IS NULL
              AND (l.AnnulationCommercial IS NULL OR l.AnnulationCommercial = 0)
              AND (l.Status IS NULL OR l.Status NOT IN ('Completed', 'Canceled'))
              AND DATEDIFF(MINUTE, l.PabEntryAt, @NowLocal) > @MinThreshold;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var rows = await connection.QueryAsync<OverstayRow>(
            new CommandDefinition(
                sql,
                new { NowLocal = nowLocal, MinThreshold = minThreshold },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<bool> TryClaimStageAsync(
        int legendId,
        int thresholdMinutes,
        DateTime sentAt,
        CancellationToken cancellationToken = default)
    {
        // Verrou = claim par insertion (PK LegendId+ThresholdMinutes) : dédoublonne
        // même en multi-instances / multi-ticks.
        const string sql = """
            INSERT INTO dbo.Ecare_Overstay_Alert_Log (LegendId, ThresholdMinutes, SentAt)
            VALUES (@LegendId, @ThresholdMinutes, @SentAt);
            """;

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { LegendId = legendId, ThresholdMinutes = thresholdMinutes, SentAt = sentAt },
                    cancellationToken: cancellationToken));

            return inserted > 0;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return false; // palier déjà alerté ailleurs
        }
    }
}
