using Confluent.Kafka;
using Contracts.Events;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddOpenTelemetry()
//    .ConfigureResource(r => r.AddService("AggregatedTransactions.Ingestor"))
//    .WithMetrics(m => m.AddRuntimeInstrumentation().AddProcessInstrumentation().AddPrometheusExporter())
//    .WithTracing(t => t.AddHttpClientInstrumentation());

builder.Services.AddHostedService<IngestWorker>();

await builder.Build().RunAsync();

public class IngestWorker(ILogger<IngestWorker> logger, IConfiguration cfg) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafka = cfg.GetSection("Kafka");
        var conf = new ConsumerConfig
        {
            BootstrapServers = kafka["BootstrapServers"],
            GroupId = kafka["GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = bool.Parse(kafka["EnableAutoCommit"] ?? "true")
        };

        using var consumer = new ConsumerBuilder<string, string>(conf).Build();
        consumer.Subscribe(kafka["Topic"]);

        await using var conn = new SqlConnection(cfg.GetConnectionString("WriteDb"));
        await conn.OpenAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var payload = JsonSerializer.Deserialize<TransactionCreatedV1>(cr.Message.Value);
                if (payload is null) continue;

                // Upsert (idempotent) to avoid duplicates
                var sql = @"MERGE dbo.Transactions AS target
                            USING (SELECT @TransactionId AS TransactionId) AS src
                            ON target.TransactionId = src.TransactionId
                            WHEN MATCHED THEN UPDATE SET CustomerId=@CustomerId, Bank=@Bank, PostedAtUtc=@PostedAtUtc, Category=@Category, Amount=@Amount, Currency=@Currency, Description=@Description
                            WHEN NOT MATCHED THEN INSERT(TransactionId, CustomerId, Bank, PostedAtUtc, Category, Amount, Currency, Description)
                            VALUES(@TransactionId, @CustomerId, @Bank, @PostedAtUtc, @Category, @Amount, @Currency, @Description);";

                await conn.ExecuteAsync(sql, new
                {
                    payload.TransactionId,
                    payload.CustomerId,
                    payload.Bank,
                    payload.PostedAtUtc,
                    payload.Category,
                    payload.Amount,
                    payload.Currency,
                    payload.Description
                });
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error");
            }
        }
    }
}