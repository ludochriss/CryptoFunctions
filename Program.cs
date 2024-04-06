using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .Build();
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context,services)=> {
        services.AddLogging(builder=>builder.AddConsole());
        services.AddHttpClient();
        services.AddCors(opt=>
        {
            opt.AddPolicy("AllowLocalHost",builder =>
            {
                builder.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        });
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