using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;

namespace Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly SqlConnection _writeDb;
        private readonly SqlConnection _readDb;
        private readonly IConnectionMultiplexer _redis;

        public HealthController(SqlConnection writeDb, SqlConnection readDb, IConnectionMultiplexer redis)
        {
            _writeDb = writeDb;
            _readDb = readDb;
            _redis = redis;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var status = new Dictionary<string, string>();

            try
            {
                await _writeDb.OpenAsync();
                status["sql-primary"] = "Healthy";
                _writeDb.Close();
            }
            catch { status["sql-primary"] = "Unhealthy"; }

            try
            {
                await _readDb.OpenAsync();
                status["sql-read"] = "Healthy";
                _readDb.Close();
            }
            catch { status["sql-read"] = "Unhealthy"; }

            try
            {
                var pong = await _redis.GetDatabase().PingAsync();
                status["redis"] = $"Healthy ({pong.TotalMilliseconds} ms)";
            }
            catch { status["redis"] = "Unhealthy"; }

            return Ok(new { status });
        }
    }
}
