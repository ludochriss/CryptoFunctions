using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class TimerCheckOne: BaseFunction<TimerCheckOne>
    {
        private readonly CryptoService _cryptoService;

        public TimerCheckOne(ILogger<TimerCheckOne> _logger, TableService tableService, RestApiService apiService, CryptoService cryptoService) : base(_logger, tableService, apiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("TimerCheckOne")]
        public async Task Run([TimerTrigger("1 * * * * *")] TimerInfo myTimer)
        {
           
            string name = "TimerCheckOne";
            logger.LogInformation($"{name}: Started");
            try
            {
                var bResult  = await _cryptoService.SpotAccountTrade.CurrentOpenOrders();
                string filter = $"orderSubmitted eq false";
                var tResult = await _tableService.QueryAsync(filter);


                foreach (var entity in tResult)
                {
                    var bId = entity.FirstOrDefault(x=> x.Key == "orderBindingId").Value;
                    var symbol =  entity.FirstOrDefault(x=> x.Key == "symbol").Value;
                    var bOrder =  JsonSerializer.Deserialize<JsonObject>( await _cryptoService.SpotAccountTrade.QueryOrder((string)symbol, (long)bId));
                    var filled  =  bOrder.TryGetPropertyValue("filled", out var filledValue);
                    if(filledValue.ToString().ToUpper() == "FILLED") logger.LogInformation("Order Placed");

                    

                    //check if the order has been executed
                    //if the order has been executed, update the table storage
                    //if the order has not been executed, check if the conditions for the order to be executed are met
                    //if the conditions are met, execute the order
                }


                //_cryptoService.SpotAccountTrade.
                //get pending orders from table storage
                //check conditions for order executions are met. E.g if a sl/tp order is waiting for a limit order to be executed, check the limit order has been executed.
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
