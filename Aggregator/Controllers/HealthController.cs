using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SqlConnection _conn;
    private readonly ConsumerConfig _kafkaConfig;

    public HealthController(SqlConnection conn, ConsumerConfig kafkaConfig)
    {
        _conn = conn;
        _kafkaConfig = kafkaConfig;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var status = new Dictionary<string, string>();

        try
        {
            await _conn.OpenAsync();
            status["sql-primary"] = "Healthy";
            _conn.Close();
        }
        catch
        {
            status["sql-primary"] = "Unhealthy";
        }

        try
        {
            status["kafka"] = string.IsNullOrWhiteSpace(_kafkaConfig.BootstrapServers)
                ? "Unhealthy"
                : "Healthy";
        }
        catch
        {
            status["kafka"] = "Unhealthy";
        }

        return Ok(new { status });
    }
}
