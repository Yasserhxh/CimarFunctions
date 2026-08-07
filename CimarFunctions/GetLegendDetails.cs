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

                // sp_GetLegendDetailsById n'est pas versionnée dans ce repo : ces champs
                // sont lus directement pour ne pas dépendre de la proc. PlombNumber est
                // casté en NVARCHAR car son type diffère selon les mappings existants.
                var extra = await conn.QueryFirstOrDefaultAsync<LegendExtraFieldsRow>(
                    @"SELECT LoadingPointTare,
                             DeuxiemePoid,
                             Weight_Charged,
                             Plombs,
                             CAST(PlombNumber AS NVARCHAR(50)) AS PlombNumber
                      FROM dbo.Ecare_Order_Legend
                      WHERE Id = @Id",
                    new { Id = id });

                if (extra != null)
                {
                    result.LoadingPointTare = extra.LoadingPointTare;
                    result.DeuxiemePoid = extra.DeuxiemePoid;
                    result.Weight_Charged = extra.Weight_Charged;
                    result.Plombs = extra.Plombs;
                    result.PlombNumber = extra.PlombNumber;
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
        public decimal? Quantite1 { get; set; }
        public decimal? Quantite2 { get; set; }

        public string? Produit1Type { get; set; }
        public int QualityCode { get; set; }

        public DateTime? StartChargingAt { get; set; }
        public DateTime? FinishedChargingAt { get; set; }
        public int? SacNumber { get; set; }
        public string? LigneName { get; set; }
        public int NumberSacs_Charged { get; set; }
        public string? SecondLigne {  get; set; }
        public int? MinusBags { get; set; }
        public int? PlusBags { get; set; }
        public int? Rest {  get; set; }

        // Tare relevée par la bascule du point de chargement VRAC (comparaison PremierePoid)
        public int? LoadingPointTare { get; set; }

        // Champs DB attendus par l'AppMobile après fin de chargement
        public int? DeuxiemePoid { get; set; }
        public int? Weight_Charged { get; set; }
        public string? Plombs { get; set; }
        public string? PlombNumber { get; set; }
    }

    // Colonnes lues directement dans Ecare_Order_Legend (hors sp_GetLegendDetailsById)
    public sealed class LegendExtraFieldsRow
    {
        public int? LoadingPointTare { get; set; }
        public int? DeuxiemePoid { get; set; }
        public int? Weight_Charged { get; set; }
        public string? Plombs { get; set; }
        public string? PlombNumber { get; set; }
    }
}
