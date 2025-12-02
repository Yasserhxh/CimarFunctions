using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MyFunctions.Functions
{
    public class GetLegendDetails
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GetLegendDetails> _log;

        public GetLegendDetails(IConfiguration config, ILogger<GetLegendDetails> log)
        {
            _config = config;
            _log = log;
        }

        [Function("GetLegendDetails")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "legend/{id:int}")] HttpRequestData req,
            int id)
        {
            var connStr = _config.GetConnectionString("SqlServer");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                var bad = req.CreateResponse(HttpStatusCode.InternalServerError);
                await bad.WriteStringAsync("SQL ConnectionString missing");
                return bad;
            }

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                var result = await conn.QueryFirstOrDefaultAsync<LegendDetailsVm>(
                    "sp_GetLegendDetailsById",
                    new { LegendId = id },
                    commandType: System.Data.CommandType.StoredProcedure
                );

                if (result == null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync("Legend not found");
                    return notFound;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error in GetLegendDetails for Id={id}", id);

                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync("ERROR_EXECUTING_FUNCTION");
                return error;
            }
        }
    }
    public sealed class LegendDetailsVm
    {
        public string? RFIDCard { get; set; }
        public int PremierePoid { get; set; }
        public string Matricule { get; set; } = string.Empty;
        public string? ClientName {  get; set; }
        public string? Produit1 { get; set; }
        public string? Produit2 { get; set; }
        public int? Quantite1 { get; set; }
        public int? Quantite2 { get; set; }

        public string? Produit1Type { get; set; }

        public DateTime? StartChargingAt { get; set; }
        public DateTime? FinishedChargingAt { get; set; }
        public int? SacNumber { get; set; }
    }
}
