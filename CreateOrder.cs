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
    public class CreateOrder : BaseFunction<CreateOrder>
    {
        private readonly CryptoService _cryptoService;
        public CreateOrder(ILogger<CreateOrder> logger, TableService tableService, RestApiService restApiService, CryptoService cryptoService)
        : base(logger, tableService, restApiService)
        {
            _cryptoService = cryptoService;
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            logger.LogInformation("Started the CreateOrder function.");
            string message = string.Empty;
            JsonObject jsonResponse = null;
            long orderId = _cryptoService.GenerateNewBinanceOrderId();
            JsonObject requestJson = await JsonSerializer.DeserializeAsync<JsonObject>(req.Body);
            string orderType = requestJson.FirstOrDefault(x => x.Key == "orderType").Value.ToString().ToLower();
            JsonObject orderDetails =  requestJson.FirstOrDefault(x => x.Key == "orderDetails").Value as JsonObject;          
            if (string.IsNullOrEmpty(orderType) ||null == orderDetails ||null == requestJson)
            {
                return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.BadRequest, "Invalid request body. Order details or order type not found.");
            }
            
            //TODO: add validation for properties in each order type as per binance docs
            switch(orderType){
                case "oco" :
                jsonResponse = await _cryptoService.PostOneCancelsOtherOrderAsync(orderDetails,orderId);
                break;
                case  "limit" :
                jsonResponse = await _cryptoService.CreateLimitOrderAsync(orderDetails, orderId); 
                break;
            }          
            

        

            return await _restApiService.HandleHttpResponseAsync(req, HttpStatusCode.OK, jsonResponse);


        }
    }
}
