using CimarFunctions.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CimarFunctions.Services.Sync;

public sealed class OrderDeliveredSyncRepository : IOrderDeliveredSyncRepository
{
    private readonly string _connectionString;

    public OrderDeliveredSyncRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");
    }

    public async Task<IReadOnlyList<ConfirmedOrderModel>> GetConfirmedOrdersAwaitingDeliveryAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take)
                  [Id]
                , [NumeroSap]
                , [Statut]
            FROM [dbo].[Orders]
            WHERE [Statut] = N'Confirmée'
              AND ISNULL([NumeroSap], '') <> ''
            ORDER BY [DateCommande] ASC, [Id] ASC;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var rows = await connection.QueryAsync<ConfirmedOrderModel>(
            new CommandDefinition(
                sql,
                new { Take = take },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<string>> GetDeliveredSalesDocumentsAsync(
        IReadOnlyCollection<long> candidateDocNumbers,
        CancellationToken cancellationToken = default)
    {
        if (candidateDocNumbers.Count == 0)
            return Array.Empty<string>();

        const string sql = """
            SELECT DISTINCT LTRIM(RTRIM([CodeSapCommande])) AS CodeSapCommande
            FROM [dbo].[Ecare_Order_Legend]
            WHERE ISNULL([BonDeLivraison], '') <> ''
              AND ISNULL([AnnulationCommercial], 0) <> 1
              AND TRY_CONVERT(BIGINT, [CodeSapCommande]) IN @Docs;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new { Docs = candidateDocNumbers },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<int> MarkOrdersDeliveredAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        // Re-check Statut='Confirmée' so a concurrent status change is never overwritten.
        const string sql = """
            UPDATE [dbo].[Orders]
            SET [Statut] = N'Livrée'
            WHERE [Id] IN @Ids
              AND [Statut] = N'Confirmée';
            """;

        await using var connection = new SqlConnection(_connectionString);

        return await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Ids = ids },
                cancellationToken: cancellationToken));
    }
}
