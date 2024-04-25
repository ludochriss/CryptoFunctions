using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Binance.Spot.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class CreateTradingViewAlertOrder : BaseFunction<CreateTradingViewAlertOrder>
    {

        private readonly CryptoService _cryptoService;
        public CreateTradingViewAlertOrder(ILogger<CreateTradingViewAlertOrder> logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("CreateTradingViewAlertOrder")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function,"post")] HttpRequestData req)
        {
            string name = "CreateTradingViewAlertOrder";
            JsonObject jResponse = new JsonObject();
            try
            {
                logger.LogInformation($"{name}: Started");
                var jBody = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
                string orderBindingId = Guid.NewGuid().ToString();
                string symbol = jBody["symbol"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(symbol)) throw new Exception("Symbol not found");
                decimal quantity = decimal.Parse(jBody["quantity"]?.ToString() ?? "0");
                //TODO: check the amount in the account before submitting the order to make sure this can actually be done
                if (quantity == 0) throw new Exception("Quantity not valid");
                var sideValue = jBody["side"]?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(sideValue) ||
                    (sideValue.ToLower() != "buy" && sideValue.ToLower() != "sell"))
                         throw new Exception("Side not valid");
                var side = sideValue.ToLower() == "buy" ? Side.BUY : Side.SELL;
                TableEntity te = new TableEntity
                {
                    PartitionKey = symbol.ToUpper(),
                    RowKey = orderBindingId
                };
                te.Add("executed", false);
                te.Add("quantity", quantity);
                te.Add("side", side.ToString());
                te.Add("orderCreated",DateTime.UtcNow);
                var tResult = await _tableService.UpsertAsync("TradingViewAlertOrders", te);
                jResponse.Add("Successful", tResult);
            }
            catch (Exception ex)
            {
                // Handle exception
                logger.LogError($"{name}: Exception: {ex.Message}");
                jResponse.Add("error", ex.Message); 
                jResponse.Add("exceptionType", ex.GetType().ToString());
            }
            finally
            {
                logger.LogInformation($"{name}: Complete");
            }
            return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, jResponse);
        }
    }
}
