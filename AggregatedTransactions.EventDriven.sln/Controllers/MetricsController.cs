using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    //using OpenTelemetry.Metrics.Export;

    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            // Delegates to Prometheus exporter already wired in Program.cs.
            // Provide a hint endpoint.
            return Ok(new { message = "Metrics are available at /metrics for Prometheus scraping." });
        }
    }
}
