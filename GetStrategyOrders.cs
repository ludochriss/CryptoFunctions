using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;
using Services.Models;

namespace CryptoFunctions
{
    public class GetStrategyOrders : BaseFunction<GetStrategyOrders>
    {
        private readonly CryptoService _cryptoService;
        public GetStrategyOrders(ILogger<GetStrategyOrders> logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("GetStrategyOrders")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            string name = "GetStrategyOrders";
            logger.LogInformation($"{name}: Started");
            JsonObject jResponse = new();
            string tableName = "SPOT";
            List<Dictionary<string,object>> orders = new();
            try
            {
                switch (req.Method.ToLower())
                {
                    case "get":
                        string filter = "orderSubmitted eq false";
                        var response = await _tableService.QueryAsync(filter, tableName);
                        foreach (var entity in response)
                        {                                                                                  
                            orders.Add(entity.ToDictionary());                             
                        }
                        break;
                    case "post":
                        JsonObject requestJson = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
                        string strategyId = requestJson.FirstOrDefault(x => x.Key == "strategyId").Value.ToString();
                        //jResponse = await _cryptoService.GetStrategyOrdersAsync(strategyId);
                        break;
                    default:
                        return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                logger.LogError($"{name}: Exception: {ex.Message}");
                return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.InternalServerError, ex.Message);
            }
            finally
            {
                logger.LogInformation($"{name}: Complete");
            }
                return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, orders);
        }
    }
}
