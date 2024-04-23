using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Binance.Common;
using Binance.Spot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
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
        public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string name = "TradingViewAlertTrigger";
            logger.LogInformation($"{name}: Started");
            string result = string.Empty;
            JsonObject jsonResponse = new JsonObject();
            string tableName = "TradingViewAlertOrders";
            try
            {
                //types of requests
                //1. Execute pre-determined orders from table storage, spot stop loss and take profit
                //2. Execute order with details from the trading view body can use as stop loss on trend line break or entry
                //3. Create trading strategy rules in table storage and have the strategy execute from the alert, query table storage and execute the strategy
                //4. Execute an order from an id in the alert body
                if (req.Body == null)
                {
                    logger.LogError($"{name}: No body in the alert request");
                    return;
                    //TODO: need to alert me/user that the alert failed somehow
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
                TableEntity te  = tResult.First();
                string symbol = te["PartitionKey"]?.ToString()?? string.Empty;
                if (string.IsNullOrWhiteSpace(symbol)) {
                    logger.LogError($"{name} : No symbol found in order with id {id}");
                    return;
                }
                decimal quantity = decimal.Parse(te["quantity"]?.ToString()?? "0");
                if(quantity == 0)
                {
                    logger.LogError($"{name} : No quantity found in order with id {id}");
                    return;
                }
                string sideValue = te["side"]?.ToString()?? string.Empty;
                if(string.IsNullOrWhiteSpace(sideValue) || (sideValue.ToLower() != "buy" && sideValue.ToLower() != "sell"))
                {
                    logger.LogError($"{name} : No valid side found in order with id {id}");
                    return;
                }
                Side side = sideValue.ToLower() == "buy" ? Side.BUY : Side.SELL;

                var bResult  = await  _cryptoService.SpotAccountTrade.NewOrder(symbol, side, OrderType.MARKET,null, quantity);
                logger.LogInformation($"{name}: Order Executed: \n{bResult}");
                JsonDocument jResult = JsonDocument.Parse(bResult);
                jResult.RootElement.TryGetProperty("status", out JsonElement status);
                bool orderFilled = status.ToString().ToLower() == "filled"? true:false;
                if(orderFilled){
                    te["executed"] = true;
                    await _tableService.UpsertAsync(tableName, te);
                }
                else{
                    logger.LogError($"{name}: Order not filled: \n{bResult}");
                }
        
                
                


                //TODO: remove if not used - 23/04this is for an order in the body, probably not going to use but keep for now 
                // var side = requestBody.FirstOrDefault(x => x.Key == "side").Value.ToString().ToLower()  == "buy" ? Side.BUY : Side.SELL;
                // var symbol = requestBody.FirstOrDefault(x => x.Key == "symbol").Value.ToString();
                // var price = requestBody.FirstOrDefault(x => x.Key == "price").Value.ToString();
                // var quantity = requestBody.FirstOrDefault(x => x.Key == "quantity").Value.ToString();
                // var orderId = requestBody.FirstOrDefault(x => x.Key == "orderId").Value.ToString();
                // //logger.LogInformation($"{name}:  {symbol} {price} {quantity} {orderId} extracted from body");
                // if (string.IsNullOrEmpty(side.ToString()) || string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(price) || string.IsNullOrEmpty(quantity) || string.IsNullOrEmpty(orderId))
                // {
                //     logger.LogError($"{name}: Missing required parameters in the alert request");
                //     return new BadRequestObjectResult(new {message= "Please pass action, symbol, price, quantity and orderId in the alert request body"});
                // }

                // decimal dquantity = decimal.Parse(quantity);    
                // decimal dprice = decimal.Parse(price);  
                // logger.LogInformation($"Quantity and price parsed.");


                // logger.LogInformation($"Attempting to execute order");
                // result = await _cryptoService.SpotAccountTrade.NewOrder(symbol, side,OrderType.LIMIT,TimeInForce.GTC, dquantity,null,dprice );
                // logger.LogInformation($"Order Executed: \n{result}");
                // jsonResponse.Add("orderResult", result);
                // // TradingViewAlertModel tv = new TradingViewAlertModel(action, symbol, decimal.Parse(price), decimal.Parse(quantity), orderId);
                // TableEntity te = new TableEntity();
                // JsonDocument response =JsonDocument.Parse(result);

                // foreach (var property in response.RootElement.EnumerateObject())
                // {
                //     string propertyName = property.Name;
                //     object propertyValue = property.Value;
                //     te[property.Name] = propertyValue.ToString();
                // }
                // te.Add("PartitionKey", $"TradingViewAlert-{symbol}");
                // te.Add("RowKey", _cryptoService.GenerateNewBinanceOrderId().ToString());
                // logger.LogInformation($"Response parsed. Attempting to upsert to table storage.");
                // //TODO: change this to a specific table for confirmations
                // var tResult = await _tableService.UpsertAsync(te);
                // logger.LogInformation($"{name}: {result}");
                // jsonResponse.Add("upsertResult", tResult);
            }
            catch(BinanceHttpException ex){
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
