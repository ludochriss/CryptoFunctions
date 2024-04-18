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
    public class CancelOrder :BaseFunction<CancelOrder>

    {
        private readonly CryptoService _cryptoService;
        public CancelOrder(ILogger<CancelOrder> logger, TableService tableService, RestApiService apiService, CryptoService cryptoService):base(logger,tableService,apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("CancelOrder")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            
                string name = "CancelOrder";
                JsonObject jResponse = new JsonObject();
                logger.LogInformation($"{name}: Started");
                try
                {                    
                    var body =await  JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
                    long orderId = (long)body?["orderId"];
                    string symbol = body?["symbol"]?.ToString();
                    if(string.IsNullOrEmpty(symbol))
                        throw new Exception("Missing required parameters");
                   var result  = await _cryptoService.SpotAccountTrade.CancelOrder(symbol, orderId);
                   jResponse.Add("result",JsonSerializer.Deserialize<JsonObject>(result));
                   
                }
                catch(JsonException ex){
                    logger.LogError($"{name}: JsonException: {ex.Message}");
                    jResponse.Add("Json Exception", ex.Message);
                    return new BadRequestObjectResult(jResponse);
                }
                catch (Exception ex)
                {
                    // Handle exception
                    logger.LogError($"{name}: Exception: {ex.Message}");
                    jResponse.Add("Exception", ex.Message);
                    return new BadRequestObjectResult(jResponse);
                }
                finally
                {
                    logger.LogInformation($"{name}: Complete");
                }
                return new OkObjectResult(jResponse);
            }
        
    }
}
