using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Services;
using Services.Functions;

namespace CryptoFunctions
{
    public class QueryExistingOrders : BaseFunction<QueryExistingOrders>
    {

        private readonly CryptoService _cryptoService;
        public QueryExistingOrders(ILogger<QueryExistingOrders> logger, TableService tableService, RestApiService restApiService, CryptoService cryptoService)
        : base(logger, tableService, restApiService)
        {
            _cryptoService = cryptoService;
        }
        [Function("QueryExistingOrders")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            string name = "QueryExistingOrders";
            string responseMessage = string.Empty;
            logger.LogInformation("Started the query existing orders function.");
            JsonObject response = null;
            try
            {
                //SECURITY: This function should be secured by moving the parameters to the headers of the request. 
                switch (req.Method.ToString().ToUpper())
                {
                    case "GET":
                        response = await _cryptoService.QueryAllOpenOrdersAsync();

                        break;
                    case "POST":
                        JsonObject body = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
                        long orderId = _cryptoService.GenerateNewBinanceOrderId();
                        var symbol = body.FirstOrDefault(x => x.Key == "symbol").Value.ToString();
                        response = await _cryptoService.QueryOpenOrdersForSymbolAsync(symbol, orderId);
                        break;
                }
            }
            catch (JsonException jex)
            {
                responseMessage = $"{name} Exception encountered while serializing Json";
                logger.LogError(responseMessage);
                return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, responseMessage,jex.Message);
            }
            catch (Exception ex)
            {
                responseMessage = $"{name} Exception encountered performing request";
                logger.LogError($"{name}: Exception: {ex}");
                return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, responseMessage, ex.Message);
            }
            finally
            {
                logger.LogInformation($"{name}: Complete");
            }
            return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, response);
        }

    }
}

