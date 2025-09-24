using Microsoft.AspNetCore.Mvc;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Contracts.Events;


namespace BankA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConfiguration _config;

        public TransactionsController(IProducer<string, string> producer, IConfiguration config)
        {
            _producer = producer;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionCreatedV1 tx)
        {
            var topic = _config["Kafka:Topic"];
            if (string.IsNullOrWhiteSpace(topic))
            {
                return StatusCode(500, "Kafka topic not configured properly");
            }

            var payload = JsonSerializer.Serialize(tx);

            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = tx.CustomerId,
                Value = payload
            });

            return Ok(new { status = "published", tx.TransactionId });
        }


    }
}

