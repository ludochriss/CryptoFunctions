using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Binance.Common;
using Binance.Spot.Models;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

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
        public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            string name = "TradingViewAlertTrigger";
            logger.LogInformation($"{name}: Started");
            string result = string.Empty;
            string tableName = "TradingViewAlertOrders";
            try
            {
                string ipAddress = context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString()?? "Not Found in Context";
                logger.LogInformation($"{name}: Request from IP: {ipAddress}");
                //types of requests
                //1. Execute pre-determined orders from table storage, spot stop loss and take profit
                //2. Execute order with details from the trading view body can use as stop loss on trend line break or entry
                //3. Create trading strategy rules in table storage and have the strategy execute from the alert, query table storage and execute the strategy
                //4. Execute an order from an id in the alert body
                if (req.Body == null)
                {
                    logger.LogError($"{name}: No body in the alert request");
                    return;
                    //TODO: need to alert me/user that the alert failed somehow, add failed order to table storage, then have a notified field and an api that checks periodically
                }

                JsonObject requestBody = req.Body != null ? await JsonSerializer.DeserializeAsync<JsonObject>(req.Body) ?? new JsonObject() : new JsonObject();

                string id = requestBody["triggerId"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) logger.LogError($"{name} : No triggerId in the alert request");

                string filter = $"RowKey eq '{id}' and executed eq false";
                var tResult = await _tableService.QueryAsync(filter, tableName);
                if (tResult.Count == 0)
                {
                    logger.LogError($"{name} : No order found with id {id}");
                    return;
                }
                TableEntity te = tResult.First();
                string symbol = te["PartitionKey"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(symbol))
                {
                    logger.LogError($"{name} : No symbol found in order with id {id}");
                    return;
                }
                double quantity = double.Parse(te["quantity"].ToString() ?? "0");
                if (quantity == 0)
                {
                    logger.LogError($"{name} : No quantity found in order with id {id}");
                    return;
                }
                string sideValue = te["side"]?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sideValue) || (sideValue.ToLower() != "buy" && sideValue.ToLower() != "sell"))
                {
                    logger.LogError($"{name} : No valid side found in order with id {id}");
                    return;
                }
                Side side = sideValue.ToLower() == "buy" ? Side.BUY : Side.SELL;

                //logger.LogInformation($"Symbol : {symbol}, Quantity : {quantity.ToString()}, Side : {sideValue}");
                var bResult = await _cryptoService.SpotAccountTrade.NewOrder(symbol, side, OrderType.MARKET, null, (decimal)quantity);
                logger.LogInformation($"{name}: Order Executed: \n{bResult}");
                JsonDocument jResult = JsonDocument.Parse(bResult);
                jResult.RootElement.TryGetProperty("status", out JsonElement status);
                bool orderFilled = status.ToString().ToLower() == "filled" ? true : false;
                if (orderFilled)
                {
                    te["executed"] = true;
                    te.Add("orderAtUtc", DateTime.UtcNow);
                    var updateResult = await _tableService.UpsertAsync(tableName, te);
                    logger.LogInformation(updateResult["status"]?.ToString() ?? "No update Status found from result");
                }
                else
                {
                    logger.LogError($"{name}: Order not filled: \n{bResult}");
                }         
            }
            catch (BinanceClientException ex)
            {
                logger.LogError($"{name}: BinanceClientException: {ex.Message}");
            }
            catch (BinanceServerException ex)
            {
                logger.LogError($"{name}: BinanceServerException: {ex.Message}");
            }
            catch (BinanceHttpException ex)
            {
                logger.LogError($"{name}: BinanceHttpException: {ex.Message}");
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

        }
    }
}
