using CimarFunctions.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CimarFunctions.Services.Sync;

public sealed class OrderLegendSyncRepository : IOrderLegendSyncRepository
{
    private readonly string _connectionString;

    public OrderLegendSyncRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");
    }

    public async Task EnsureLowCreditDeliveryRiskColumnAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'IsLowCreditDeliveryRisk') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD IsLowCreditDeliveryRisk BIT NOT NULL
                    CONSTRAINT DF_Ecare_Order_Legend_IsLowCreditDeliveryRisk DEFAULT (0);
            END;
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task EnsureSecondPesageCancelColumnsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'SecondPesageCanceledAt') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD SecondPesageCanceledAt DATETIME NULL;
            END;

            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'SecondPesageCanceledBy') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD SecondPesageCanceledBy NVARCHAR(255) NULL;
            END;
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task EnsureMesDocumentsCircuitColumnsAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'SacNumber') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD SacNumber INT NULL;
            END;

            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'NumberSacs_Charged') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD NumberSacs_Charged INT NULL;
            END;

            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'Weight_Charged') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD Weight_Charged DECIMAL(18, 3) NULL;
            END;

            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'FirstPlaceAt') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD FirstPlaceAt DATETIME NULL;
            END;

            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'TimeElapsedInFirstPlace') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD TimeElapsedInFirstPlace INT NULL;
            END;
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task EnsureDocumentUpdatedAtColumnAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF COL_LENGTH('dbo.Ecare_Order_Legend', 'DocumentUpdatedAt') IS NULL
            BEGIN
                ALTER TABLE dbo.Ecare_Order_Legend
                ADD DocumentUpdatedAt DATETIME NULL;
            END;
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task EnsureSpecificLegendStepFixesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Ecare_Order_Legend
            SET [Step] = 5
            WHERE RTRIM(LTRIM(ISNULL([RFIDCard], ''))) = '1198'
              AND [FinishedChargingAt] = '2026-05-13T16:06:00'
              AND ISNULL([Step], 0) < 5;

            UPDATE dbo.Ecare_Order_Legend
            SET [Step] = 5
            WHERE RTRIM(LTRIM(ISNULL([RFIDCard], ''))) = '2700'
              AND [FinishedChargingAt] = '2026-04-30T12:40:00'
              AND ISNULL([Step], 0) < 5;
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task EnsureSpecificClientEquipmentHexFixesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE dbo.Ecare_ClientEquipements
            SET [RfidHex] = '8D24BFDA'
            WHERE LTRIM(RTRIM(ISNULL([CarteSLV], ''))) = '2612'
              AND ISNULL(LTRIM(RTRIM([RfidHex])), '') <> '8D24BFDA';
            """;

        await using var connection = new SqlConnection(_connectionString);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PendingOrderSyncModel>> GetPendingOrdersAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take)
                  [Id]
                , [CodeSapClient]
                , [CodeSapCommande]
                , [CreatedAt]
            FROM [dbo].[Ecare_Order_Legend]
            WHERE [BonDeLivraison] IS NULL
              AND [IsSynced] = 0
              AND [CodeSapClient] IS NOT NULL
              AND [CodeSapCommande] IS NOT NULL
            ORDER BY [CreatedAt] ASC, [Id] ASC;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var rows = await connection.QueryAsync<PendingOrderSyncModel>(
            new CommandDefinition(
                sql,
                new { Take = take },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<bool> MarkAsSyncedAsync(
        long id,
        string bonDeLivraison,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DECLARE @Updated TABLE (Ligne NVARCHAR(150));

            UPDATE [dbo].[Ecare_Order_Legend]
            SET [BonDeLivraison] = @BonDeLivraison,
                [Step] = 5,
                [IsSynced] = 1,
                [DocumentUpdatedAt] = SYSUTCDATETIME(),
                [Status] = CASE
                    WHEN ISNULL([AnnulationCommercial], 0) = 1 THEN 'Canceled'
                    ELSE 'Completed'
                END
            OUTPUT inserted.[Ligne] INTO @Updated([Ligne])
            WHERE [Id] = @Id
              AND [BonDeLivraison] IS NULL
              AND [IsSynced] = 0;

            IF @@ROWCOUNT > 0
            BEGIN
                UPDATE L
                SET [RealtimeCapacity] =
                    CASE
                        WHEN ISNULL(L.[RealtimeCapacity], 0) < ISNULL(L.[Capacity], 0)
                            THEN ISNULL(L.[RealtimeCapacity], 0) + 1
                        ELSE ISNULL(L.[Capacity], 0)
                    END
                FROM [dbo].[Ecare_Ligne] L
                WHERE L.[Nom] = (
                        SELECT TOP (1) U.[Ligne]
                        FROM @Updated U
                        WHERE U.[Ligne] IS NOT NULL
                    )
                  AND EXISTS (
                        SELECT 1
                        FROM [dbo].[Ecare_Order_Legend] O
                        WHERE O.[Id] = @Id
                          AND ISNULL(O.[AnnulationCommercial], 0) <> 1
                          AND (
                                O.[PabEntryAt] IS NOT NULL
                                OR O.[PremierePoid] IS NOT NULL
                          )
                    );
            END;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = id,
                    BonDeLivraison = bonDeLivraison
                },
                cancellationToken: cancellationToken));

        return affected > 0;
    }
}
