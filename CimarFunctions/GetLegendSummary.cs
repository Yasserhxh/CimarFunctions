using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace CimarFunctions
{
    public class GetLegendSummary
    {
        private readonly IConfiguration _config;

        public GetLegendSummary(IConfiguration config)
        {
            _config = config;
        }

        [Function("GetLegendSummary")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "legend/summary")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            string connStr = _config.GetConnectionString("SqlServer");

            await using var conn = new SqlConnection(connStr);

            var result = await conn.QueryAsync("sp_GetLegendSummary",
                commandType: CommandType.StoredProcedure);

            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
