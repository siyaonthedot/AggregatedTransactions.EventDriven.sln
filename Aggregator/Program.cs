using Confluent.Kafka;
using Microsoft.Data.SqlClient;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
//using OpenTelemetry.Instrumentation.Runtime; // Added namespace for runtime instrumentation  

var builder = WebApplication.CreateBuilder(args);

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

// SQL connection factory  
builder.Services.AddSingleton(sp =>
   new SqlConnection(builder.Configuration.GetConnectionString("WriteDb")));

// Register background worker  
builder.Services.AddHostedService<AggregatorWorker>();

var app = builder.Build();

app.MapControllers();
//app.MapPrometheusScrapingEndpoint();

app.Run();
