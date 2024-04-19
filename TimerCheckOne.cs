using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class TimerCheckOne : BaseFunction<TimerCheckOne>
    {
        private readonly CryptoService _cryptoService;

        public TimerCheckOne(ILogger<TimerCheckOne> _logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(_logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("TimerCheckOne")]
        public async Task Run([TimerTrigger("1 * * * * *")] TimerInfo myTimer)
        {

            string tableName =  "SPOT";
            string name = "TimerCheckOne";
            logger.LogInformation($"{name}: Started");
            try
            {
                //get open orders from binance
                var bResult = await _cryptoService.SpotAccountTrade.CurrentOpenOrders();
                //get table storage orders that are not executed
                string filter = $"orderSubmitted eq false";
                var tResult = await _tableService.QueryAsync(filter,tableName);

                //query each binance order Id to check if the order has been filled
                foreach (var entity in tResult)
                {
                    string bId = entity.FirstOrDefault(x => x.Key == "bindingOrderId").Value?.ToString() ?? string.Empty;
                    var parseable = long.TryParse(bId, out long orderId);   
                    string symbol = entity.FirstOrDefault(x => x.Key == "symbol").Value?.ToString() ?? string.Empty;
                    //if the field is null for these remove the entity from table storage as it doesn't belong
                    if (string.IsNullOrEmpty(bId) || string.IsNullOrEmpty(symbol) || !parseable)
                    {
                        logger.LogWarning($"Order Id or Symbol not found in record");
                        await _tableService._tableClient.DeleteEntityAsync(entity["PartitionKey"].ToString(),entity["RowKey"].ToString());
                        continue;
                    }
                    var orderResult = await _cryptoService.QueryOpenOrdersForSymbolAsync(symbol, orderId);
                    var data = orderResult["data"];
                    //if order can't be retrieved delete order from table storage
                    if (data == null)
                    {
                        logger.LogWarning($"{name}: Order not found on Binance");
                        await _tableService._tableClient.DeleteEntityAsync(entity["PartitionKey"].ToString(),entity["RowKey"].ToString());
                        continue;
                    }
                    string filled = data["status"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(filled)){
                        //something for null filled might need to remove order
                        continue;
                    }
                    //if binance order is filled place a new oco sl/tp order
                    if (filled.ToUpper() == "FILLED")
                    {
                        var ocoResult = await _cryptoService.HandleOcoExitOrderAsync(entity);
                        if (!ocoResult.ContainsKey("orderListId"))
                        {
                            logger.LogError($"{name}: Oco order not placed");
                            throw new Exception("Oco order not placed");
                        }
                        else
                        {
                            //update table storage for oco order to prevent from sending again
                            entity["orderSubmitted"] = true;
                            var upsertResult = await  _tableService.UpsertAsync(tableName,entity);
                            string result = upsertResult["Status"]?.ToString() ?? string.Empty;
                            logger.LogInformation($"{name}: Order Successful  : {result}");
                        }
                    }
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

        }
    }
}
