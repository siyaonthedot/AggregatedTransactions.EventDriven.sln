using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;

var config = new ProducerConfig { BootstrapServers = "kafka:9092" };
using var producer = new ProducerBuilder<string, string>(config).Build();

// Demo seed events at startup
for (int i = 0; i < 5; i++)
{
    var evt = new TransactionCreatedV1(
        TransactionId: Guid.NewGuid().ToString("N"),
        CustomerId: "CUST-123",
        Bank: i % 2 == 0 ? "BankA" : "BankB",
        PostedAtUtc: DateTime.UtcNow.AddMinutes(-i * 17),
        Category: i % 2 == 0 ? "Groceries" : "Transport",
        Amount: 50 + i * 10,
        Currency: "ZAR",
        Description: "Seed event " + i
    );

    var json = JsonSerializer.Serialize(evt);
    await producer.ProduceAsync(Topics.Transactions, new Message<string, string> { Key = evt.CustomerId, Value = json });
}

Console.WriteLine("Seed events published.");

