using Confluent.Kafka;
using NLog;
using NLog.Web;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;


var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    logger.Info("Starting Aggregator service...");
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8091); // HTTP only
                                   // Remove HTTPS endpoint
    });


    // OpenTelemetry configuration  
    builder.Services.AddOpenTelemetry()
   .ConfigureResource(r => r.AddService("AggregatedTransactions.Aggregator"))
   .WithMetrics(m => m
       .AddAspNetCoreInstrumentation()
       .AddHttpClientInstrumentation()
      // .AddRuntimeInstrumentation() // Ensure the correct package is referenced  
      // .AddPrometheusExporter()
       )
   .WithTracing(t => t
       .AddAspNetCoreInstrumentation()
       .AddHttpClientInstrumentation());

// Add controllers (for /api/health)  
builder.Services.AddControllers();

// Kafka consumer configuration  
builder.Services.AddSingleton(sp =>
{
    var cfg = builder.Configuration.GetSection("Kafka");
    return new ConsumerConfig
    {
        BootstrapServers = cfg["BootstrapServers"],
        GroupId = cfg["GroupId"] ?? "aggregator-service",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };
});

builder.Services.AddSingleton<NpgsqlConnection>(sp =>
{
    var connString = builder.Configuration.GetConnectionString("WriteDb")
        ?? "Host=postgres;Port=5432;Database=transactionsdb;Username=postgres;Password=postgres";
    return new NpgsqlConnection(connString);
});

    var retryPolicy = Policy
    .Handle<NpgsqlException>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(5));;

    builder.Services.AddHostedService<AggregatorWorker>();


var app = builder.Build();

app.MapControllers();
//app.MapPrometheusScrapingEndpoint();

app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
