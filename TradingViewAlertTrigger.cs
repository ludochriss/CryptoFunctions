using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;
using Services.Models;

namespace CryptoFunctions
{
    public class TradingViewAlertTrigger : BaseFunction<TradingViewAlertTrigger>
    {

        private readonly CryptoService _cryptoService;
        public TradingViewAlertTrigger(ILogger<TradingViewAlertTrigger> _logger, RestApiService restApiService, CryptoService cryptoService, TableService tableService)
        : base(_logger, tableService, restApiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("TradingViewAlertTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {            
                string name = "TradingViewAlertTrigger";
                logger.LogInformation($"{name}: Started");
                try
                {
                    if (req.Body == null)
                    {
                        return new BadRequestObjectResult("Please pass body in the alert request body");
                        //TODO: need to alert me/user that the alert failed somehow
                    }
                    JsonObject requestBody =req.Body != null? await JsonSerializer.DeserializeAsync<JsonObject>(req.Body)?? new JsonObject(): new JsonObject();
                    string action = requestBody.FirstOrDefault(x => x.Key == "action").Value.ToString();
                    string symbol = requestBody.FirstOrDefault(x => x.Key == "symbol").Value.ToString();
                    string price = requestBody.FirstOrDefault(x => x.Key == "price").Value.ToString();
                    string quantity = requestBody.FirstOrDefault(x => x.Key == "quantity").Value.ToString();
                    string orderId = requestBody.FirstOrDefault(x => x.Key == "orderId").Value.ToString();

                    TradingViewAlertModel tv = new TradingViewAlertModel(action, symbol, decimal.Parse(price), decimal.Parse(quantity), orderId);
                    TableEntity te = new TableEntity();
                    foreach (var property in tv.GetType().GetProperties())
                    {
                        string propertyName = property.Name;
                        object propertyValue = property.GetValue(tv);
                        te[property.Name] = propertyValue.ToString();
                    }

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
