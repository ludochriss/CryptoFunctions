using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Data.Tables;
using Binance.Spot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {            
                string name = "TradingViewAlertTrigger";
                logger.LogInformation($"{name}: Started");
                string result  =  string.Empty;
                JsonObject jsonResponse = new JsonObject();
                try
                {
                    //types of requests
                    //1. Execute pre-determined orders from table storage, spot stop loss and take profit
                    //2. Execute order with details from the trading view body can use as stop loss on trend line break or entry
                    //3. Create trading strategy rules in table storage and have the strategy execute from the alert, query table storage and execute the strategy
                    if (req.Body == null)
                    {
                        logger.LogError($"{name}: No body in the alert request");
                        return new BadRequestObjectResult(new {message = "Please pass body in the alert request body"});
                        //TODO: need to alert me/user that the alert failed somehow
                    }
                    JsonObject requestBody =req.Body != null? await JsonSerializer.DeserializeAsync<JsonObject>(req.Body)?? new JsonObject(): new JsonObject();
                    var side = requestBody.FirstOrDefault(x => x.Key == "side").Value.ToString().ToLower()  == "buy" ? Side.BUY : Side.SELL;
                    var symbol = requestBody.FirstOrDefault(x => x.Key == "symbol").Value.ToString();
                    var price = requestBody.FirstOrDefault(x => x.Key == "price").Value.ToString();
                    var quantity = requestBody.FirstOrDefault(x => x.Key == "quantity").Value.ToString();
                    var orderId = requestBody.FirstOrDefault(x => x.Key == "orderId").Value.ToString();
                    //logger.LogInformation($"{name}:  {symbol} {price} {quantity} {orderId} extracted from body");
                    if (string.IsNullOrEmpty(side.ToString()) || string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(price) || string.IsNullOrEmpty(quantity) || string.IsNullOrEmpty(orderId))
                    {
                        logger.LogError($"{name}: Missing required parameters in the alert request");
                        return new BadRequestObjectResult(new {message= "Please pass action, symbol, price, quantity and orderId in the alert request body"});
                    }
                
                    decimal dquantity = decimal.Parse(quantity);    
                    decimal dprice = decimal.Parse(price);  
                    logger.LogInformation($"Quantity and price parsed.");


                    logger.LogInformation($"Attempting to execute order");
                    result = await _cryptoService.SpotAccountTrade.NewOrder(symbol, side,OrderType.LIMIT,TimeInForce.GTC, dquantity,null,dprice );
                    logger.LogInformation($"Order Executed: \n{result}");
                    jsonResponse.Add("orderResult", result);
                    // TradingViewAlertModel tv = new TradingViewAlertModel(action, symbol, decimal.Parse(price), decimal.Parse(quantity), orderId);
                    TableEntity te = new TableEntity();
                    JsonDocument response =JsonDocument.Parse(result);

                    foreach (var property in response.RootElement.EnumerateObject())
                    {
                        string propertyName = property.Name;
                        object propertyValue = property.Value;
                        te[property.Name] = propertyValue.ToString();
                    }
                    te.Add("PartitionKey", $"TradingViewAlert-{symbol}");
                    te.Add("RowKey", _cryptoService.GenerateNewBinanceOrderId().ToString());
                    logger.LogInformation($"Response parsed. Attempting to upsert to table storage.");
                    //TODO: change this to a specific table for confirmations
                    var tResult = await _tableService.UpsertAsync(te);
                    logger.LogInformation($"{name}: {result}");
                    jsonResponse.Add("upsertResult", tResult);

                    

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

                return new OkObjectResult(jsonResponse);
        }
    }
}
