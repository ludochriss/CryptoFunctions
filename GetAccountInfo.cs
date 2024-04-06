using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class GetAccountInfo : BaseFunction<GetAccountInfo>
    {

        private readonly CryptoService _cryptoService;
        public GetAccountInfo(ILogger<GetAccountInfo> logger, TableService tableService, RestApiService restApiService, CryptoService cryptoService)
        : base(logger, tableService, restApiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("GetAccountInfo")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            string name = "GetAccountInfo Function";
            logger.LogInformation("Started the GetAccountInfo function.");
            string message = string.Empty;
            JsonObject jsonResponse = null;
            //JsonObject requestJson = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
            try
            {
                jsonResponse =await  _cryptoService.GetSpotAccountBalanceAsync();
            }
            catch (Exception ex)
            {
                // Handle exception
                logger.LogError($"{name}: Exception: {ex.Message}");
            }
            finally
            {
                logger.LogInformation($"{name}: Complete");
            }
            return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, jsonResponse);
        }
    }
}
