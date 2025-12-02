using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;

public class DashboardOverviewFunction
{
    private readonly IConfiguration _config;

    public DashboardOverviewFunction(IConfiguration config)
    {
        _config = config;
    }

    [Function("DashboardOverview")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            var connStr = _config.GetConnectionString("SqlServer");
            List<dynamic> rows;

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            const string sql = @"
        SELECT
            L.Id,
            L.ClientName,
            L.Matricule,
            L.Produit1,
            L.Quantite1,
            L.Produit2,
            L.Quantite2,
            L.TypeProduit,
            L.Step,

            ElapsedTime =
                ISNULL(
                    CASE 
                        WHEN Step = 1 THEN DATEDIFF(
                                MINUTE, 
                                ParkingAt, 
                                CONVERT(datetime, SYSDATETIMEOFFSET() AT TIME ZONE 'Morocco Standard Time')
                            )
                        WHEN Step = 2 THEN DATEDIFF(
                                MINUTE, 
                                PabEntryAt, 
                                CONVERT(datetime, SYSDATETIMEOFFSET() AT TIME ZONE 'Morocco Standard Time')
                            )
                        WHEN Step = 3 THEN DATEDIFF(
                                MINUTE, 
                                StartChargingAt, 
                                CONVERT(datetime, SYSDATETIMEOFFSET() AT TIME ZONE 'Morocco Standard Time')
                            )
                        WHEN Step = 4 THEN DATEDIFF(
                                MINUTE, 
                                FinishedChargingAt, 
                                CONVERT(datetime, SYSDATETIMEOFFSET() AT TIME ZONE 'Morocco Standard Time')
                            )
                    END,
                0),

            StatusColor =
                CASE 
                    WHEN Step = 1 THEN 'Red'      -- Parking
                    WHEN Step = 2 THEN 'Red'      -- Usine Step 2
                    WHEN Step = 3 THEN 'Yellow'   -- Chargement
                    WHEN Step = 4 THEN 'Green'    -- Usine Step 4
                    ELSE 'NA'
                END,

            Produit1Type =
                (SELECT TOP 1 Type FROM dbo.EcareCiments WHERE Name = L.Produit1)

        FROM dbo.Ecare_Order_Legend L
        ORDER BY L.CreatedAt DESC;
        ";

            rows = (await conn.QueryAsync(sql)).ToList();

            // -----------------------------
            // GROUPING RULES
            // -----------------------------

            // Parking = Step 1
            var parking = rows.Where(r => (int)r.Step == 1).ToList();

            // Usine = Step 2 + Step 4
            var usine = rows.Where(r => (int)r.Step == 2 || (int)r.Step == 4).ToList();

            // Chargement = Step 3
            var chargement = rows.Where(r => (int)r.Step == 3).ToList();


            // -----------------------------
            // SAFE AGGREGATE FUNCTIONS
            // -----------------------------
            int Min(List<dynamic> list) => list.Count == 0 ? 0 : list.Min(x => (int)x.ElapsedTime);
            int Max(List<dynamic> list) => list.Count == 0 ? 0 : list.Max(x => (int)x.ElapsedTime);
            int Sum(List<dynamic> list) => list.Sum(x => (int)x.ElapsedTime);


            // -----------------------------
            // BUILD RESPONSE
            // -----------------------------

            var responseObj = new
            {
                parking = new
                {
                    count = parking.Count,
                    minElapsed = Min(parking),
                    maxElapsed = Max(parking),
                    totalElapsed = Sum(parking),
                    items = parking
                },
                usine = new
                {
                    count = usine.Count,
                    minElapsed = Min(usine),
                    maxElapsed = Max(usine),
                    totalElapsed = Sum(usine),
                    items = usine
                },
                chargement = new
                {
                    count = chargement.Count,
                    minElapsed = Min(chargement),
                    maxElapsed = Max(chargement),
                    totalElapsed = Sum(chargement),
                    items = chargement
                }
            };

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(responseObj);
            return res;
        }
        catch (Exception ex)
        {
            var res = req.CreateResponse(HttpStatusCode.InternalServerError);
            await res.WriteStringAsync(ex.ToString()); 
            return res;
        }

    }
}
