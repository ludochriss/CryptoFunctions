using System;
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
        public async Task Run([TimerTrigger("30 * * * * *")] TimerInfo myTimer)
        {
           
            string name = "TimerCheckOne";
            logger.LogInformation($"{name}: Started");
            try
            {
                var result  = await _cryptoService.SpotAccountTrade.CurrentOpenOrders();

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
