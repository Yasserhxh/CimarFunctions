using CimarFunctions.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CimarFunctions.Services.Sync;

public sealed class UnconfirmedOrderCancelRepository : IUnconfirmedOrderCancelRepository
{
    private readonly string _connectionString;

    public UnconfirmedOrderCancelRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");
    }

    public async Task<IReadOnlyList<UnconfirmedOrderModel>> GetStaleUnconfirmedOrdersAsync(
        DateTime cutoff,
        int take,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Take)
                  [Id]
                , [Statut]
                , [DateCommande]
            FROM [dbo].[Orders]
            WHERE [Statut] = N'Crée'
              AND [DateCommande] <= @Cutoff
            ORDER BY [DateCommande] ASC, [Id] ASC;
            """;

        await using var connection = new SqlConnection(_connectionString);

        var rows = await connection.QueryAsync<UnconfirmedOrderModel>(
            new CommandDefinition(
                sql,
                new { Take = take, Cutoff = cutoff },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<int> CancelOrdersAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;

        // Re-check Statut='Crée' in the UPDATE so a concurrent confirmation is never overwritten.
        const string sql = """
            UPDATE [dbo].[Orders]
            SET [Statut] = N'Annulée'
            WHERE [Id] IN @Ids
              AND [Statut] = N'Crée';
            """;

        await using var connection = new SqlConnection(_connectionString);

        return await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Ids = ids },
                cancellationToken: cancellationToken));
    }
}
