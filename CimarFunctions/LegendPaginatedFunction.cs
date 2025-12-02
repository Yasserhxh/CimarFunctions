using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

public class LegendPaginatedFunction
{
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public LegendPaginatedFunction(IConfiguration config, IHttpClientFactory factory)
    {
        _config = config;
        _http = factory.CreateClient();
    }

    [Function("LegendPaginated")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        int page = int.TryParse(query["page"], out var p) ? p : 1;
        int pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 10;

        int offset = (page - 1) * pageSize;

        var connStr = _config.GetConnectionString("SqlServer");

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        // Fetch paged rows
        const string sql = @"
        SELECT
            L.Id,
            L.ClientName,
            L.ParkingAt,
            L.Matricule,
            L.RFIDCard,
            L.ChequeImg,
            D.Nom AS ChauffeurNom,
            D.Prenom AS ChauffeurPrenom
        FROM dbo.Ecare_Order_Legend L
        LEFT JOIN dbo.Ecare_Truck T ON T.Matricule = L.Matricule
        LEFT JOIN dbo.Ecare_Driver D ON D.Id = T.DriverId
        ORDER BY L.Id DESC
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

        var rows = (await conn.QueryAsync(sql, new
        {
            Offset = offset,
            PageSize = pageSize
        })).ToList();


        // Count total
        int totalCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Ecare_Order_Legend");


        // Build final result list with imageUrl from SAS generator
        var finalList = new List<object>();

        foreach (var r in rows)
        {
            string? chequeImg = r.ChequeImg;

            string? imageUrl = null;

            

            if (!string.IsNullOrWhiteSpace(chequeImg))
            {
                // Call SAS endpoint
                string sasUrl = $"https://ecare.azurewebsites.net/files/{chequeImg}/sas";

                try
                {
                    // Expect JSON like { "blobName": "...", "url": "..." }
                    var sasJson = await _http.GetFromJsonAsync<JsonElement>(sasUrl);

                    if (sasJson.TryGetProperty("url", out var u) &&
                        u.ValueKind == JsonValueKind.String)
                    {
                        imageUrl = u.GetString();
                    }
                }
                catch
                {
                    imageUrl = null;
                }
            }


            finalList.Add(new
            {
                id = r.Id,
                clientName = r.ClientName,
                parkingAt = r.ParkingAt,
                chauffeurName = $"{r.ChauffeurNom} {r.ChauffeurPrenom}".Trim(),
                matricule = r.Matricule,
                rfidCard = r.RFIDCard,
                imageUrl
            });
        }

        var responseObj = new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items = finalList
        };

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(responseObj);

        return res;
    }
}
