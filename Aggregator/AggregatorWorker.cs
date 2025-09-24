using Confluent.Kafka;
using Contracts.Events;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class AggregatorWorker : BackgroundService
{
    private readonly ILogger<AggregatorWorker> _logger;
    private readonly ConsumerConfig _config;
    private readonly SqlConnection _conn;
    private readonly IConfiguration _configuration;

    public AggregatorWorker(
        ILogger<AggregatorWorker> logger,
        ConsumerConfig config,
        SqlConnection conn,
        IConfiguration configuration)
    {
        _logger = logger;
        _config = config;
        _conn = conn;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        consumer.Subscribe(_configuration["Kafka:Topic"] ?? "transactions");

        await _conn.OpenAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var payload = JsonSerializer.Deserialize<TransactionCreatedV1>(cr.Message.Value);

                if (payload is null)
                    continue;

                var sql = @"
                    MERGE dbo.Transactions AS target
                    USING (SELECT @TransactionId AS TransactionId) AS src
                    ON target.TransactionId = src.TransactionId
                    WHEN MATCHED THEN 
                        UPDATE SET 
                            CustomerId=@CustomerId,
                            Bank=@Bank,
                            PostedAtUtc=@PostedAtUtc,
                            Category=@Category,
                            Amount=@Amount,
                            Currency=@Currency,
                            Description=@Description
                    WHEN NOT MATCHED THEN 
                        INSERT (TransactionId, CustomerId, Bank, PostedAtUtc, Category, Amount, Currency, Description)
                        VALUES (@TransactionId, @CustomerId, @Bank, @PostedAtUtc, @Category, @Amount, @Currency, @Description);";

                await _conn.ExecuteAsync(sql, new
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

                _logger.LogInformation("Transaction {TxId} upserted from {Bank}", payload.TransactionId, payload.Bank);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AggregatorWorker");
            }
        }
    }
}
