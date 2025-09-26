using Confluent.Kafka;
using Contracts.Events;
using Npgsql;
using System.Text.Json;

public class AggregatorWorker : BackgroundService
{
    private readonly ILogger<AggregatorWorker> _logger;
    private readonly ConsumerConfig _config;
    private readonly NpgsqlConnection _conn;
    private readonly IConfiguration _configuration;

    public AggregatorWorker(
        ILogger<AggregatorWorker> logger,
        ConsumerConfig config,
        NpgsqlConnection conn,
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
        consumer.Subscribe(_configuration["Kafka:Topic"] ?? "transactions.created.v1");

        await _conn.OpenAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var payload = JsonSerializer.Deserialize<TransactionCreatedV1>(cr.Message.Value);

                if (payload is null)
                    continue;

                // ✅ Postgres UPSERT (ON CONFLICT DO UPDATE)
                var sql = @"
                    INSERT INTO transactions 
                        (transaction_id, customer_id, bank, posted_at_utc, category, amount, currency, description)
                    VALUES 
                        (@transaction_id, @customer_id, @bank, @posted_at_utc, @category, @amount, @currency, @description)
                    ON CONFLICT (transaction_id) DO UPDATE SET
                        customer_id = EXCLUDED.customer_id,
                        bank = EXCLUDED.bank,
                        posted_at_utc = EXCLUDED.posted_at_utc,
                        category = EXCLUDED.category,
                        amount = EXCLUDED.amount,
                        currency = EXCLUDED.currency,
                        description = EXCLUDED.description;";

                await using var cmd = new NpgsqlCommand(sql, _conn);
                cmd.Parameters.AddWithValue("@transaction_id", payload.TransactionId);
                cmd.Parameters.AddWithValue("@customer_id", payload.CustomerId);
                cmd.Parameters.AddWithValue("@bank", payload.Bank);
                cmd.Parameters.AddWithValue("@posted_at_utc", payload.PostedAtUtc);
                cmd.Parameters.AddWithValue("@category", payload.Category);
                cmd.Parameters.AddWithValue("@amount", payload.Amount);
                cmd.Parameters.AddWithValue("@currency", payload.Currency);
                cmd.Parameters.AddWithValue("@description", payload.Description ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync(stoppingToken);

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
