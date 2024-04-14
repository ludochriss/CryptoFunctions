using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

string environment =  Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?? "DEVELOPMENT";//["Values:ASPNETCORE_ENVIRONMENT"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";


var config= new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables();
    if(environment ==  "Production"){
        string jsonString = Environment.GetEnvironmentVariable("cloud.settings.json")?? "";
        if(string.IsNullOrEmpty(jsonString)){
            throw new Exception("cloud.settings.json not found in environment variables");
        }
        var dictionary =  JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonString);
        config.AddInMemoryCollection(dictionary);
    }
    else if (environment == "Development")
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);        
    }   
    var configuration = config.Build();
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context,services)=> {
        services.AddLogging(builder=>builder.AddConsole());
        services.AddHttpClient();
        // services.AddCors(opt=>
        // {
        //     opt.AddPolicy("AllowLocalHost",builder =>
        //     {
        //         builder.WithOrigins("http://localhost:4200")
        //         .AllowAnyHeader()
        //         .AllowAnyMethod();
        //     });
        // });
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddTransient<TableService>(sp=>
        {
            var tableServiceConnectionString = context.Configuration.GetWebJobsConnectionString("AzureWebJobsStorage");
            return new TableService(tableServiceConnectionString, sp.GetService<ILogger<TableService>>(),sp.GetService<HttpClient>());
        });
        services.AddScoped<RestApiService>(sp =>
        {
            return new RestApiService(sp.GetService<ILogger<RestApiService>>(), sp.GetService<HttpClient>());
        });
        services.AddScoped<CryptoService>(sp =>
        {            
            return new CryptoService(sp.GetService<ILogger<CryptoService>>(), sp.GetService<HttpClient>(), context.Configuration);
        });
    })
    .ConfigureAppConfiguration(config=> config.AddConfiguration(configuration))    
    .Build();
host.Run(); 