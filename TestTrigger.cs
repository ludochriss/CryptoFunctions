using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class TestTrigger : BaseFunction<TestTrigger>
    {

        private readonly CryptoService _cryptoService;

        public TestTrigger(ILogger<TestTrigger> logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("TestTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            string name = "TestTrigger";
            logger.LogInformation($"{name}: Started");
            try
            {
                JsonObject requestBody =req.Body != null? await JsonSerializer.DeserializeAsync<JsonObject>(req.Body)?? new JsonObject(): new JsonObject();
                var orderId=  requestBody.FirstOrDefault(x => x.Key == "orderId").Value;
               var result = await  _cryptoService.SpotAccountTrade.QueryOrder("BTCUSDT",(long)orderId);
               logger.LogInformation("Complete");
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

            return new OkObjectResult("Welcome to Azure Functions!");

        }
    }
}
