using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class GetTradingViewOrders : BaseFunction<GetTradingViewOrders>
    {
        private readonly CryptoService _cryptoService;

        public GetTradingViewOrders(ILogger<GetTradingViewOrders> logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("GetTradingViewOrders")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            string name = "GetTradingViewOrders";
            logger.LogInformation($"{name} Function: Started");
            List<Dictionary<string, object>> orders = new();
            try
            {
                string filter = $"";
                var response = await _tableService.QueryAsync("", "TradingViewAlertOrders");
                foreach (var entity in response)
                {
                    orders.Add(entity.ToDictionary());
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

            logger.LogInformation("C# HTTP trigger function processed a request.");
            return await _restApiService.HandleHttpResponseAsync(req, System.Net.HttpStatusCode.OK, orders);
        }
    }
}
