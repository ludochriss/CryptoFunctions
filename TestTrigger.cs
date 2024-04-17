using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
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
                var data=  requestBody.FirstOrDefault(x => x.Key == "data").Value;
                var symbol = requestBody.FirstOrDefault(x=>x.Key == "symbol").Value;
                var result = await  _cryptoService.SpotAccountTrade.QueryOrder("BTCUSDT",(long)orderId);
                TableEntity testEntity = new TableEntity(data.ToString(), "testEntity");
                
                var tResult = await _tableService.UpsertAsync(symbol.ToString(),testEntity);

               logger.LogInformation($"Complete, tResult : {tResult}");
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
