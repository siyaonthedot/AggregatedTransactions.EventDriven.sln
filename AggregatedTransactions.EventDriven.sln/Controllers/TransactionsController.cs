using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using Contracts.Dtos;

namespace Api.Controllers
{
    using Api.Infrastructure;
    using Contracts.Dtos;
    using Microsoft.AspNetCore.Mvc;
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionsRepository _repo;
        private readonly ICache _cache;
        private readonly IConfiguration _cfg;

        public TransactionsController(ITransactionsRepository repo, ICache cache, IConfiguration cfg)
        {
            _repo = repo;
            _cache = cache;
            _cfg = cfg;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string customerId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? category,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { error = "customerId is required" });

            var maxPage = _cfg.GetValue<int>("Pagination:MaxPageSize", 200);
            pageSize = Math.Clamp(pageSize, 1, maxPage);
            pageNumber = Math.Max(pageNumber, 1);

            var cacheKey = $"tx:{customerId}:{from?.ToString("yyyyMMdd")}:{to?.ToString("yyyyMMdd")}:{category}:{pageNumber}:{pageSize}";
            if (await _cache.TryGetAsync<PagedResponse<TransactionDto>>(cacheKey) is { } cached)
                return Ok(cached);

            var (items, total) = await _repo.QueryTransactionsAsync(customerId, from, to, category, pageNumber, pageSize, ct);
            var meta = new PaginationMeta(pageNumber, pageSize, (int)Math.Ceiling(total / (double)pageSize), total);
            var response = new PagedResponse<TransactionDto>(items, meta);

            await _cache.SetAsync(cacheKey, response, TimeSpan.FromSeconds(_cfg.GetValue<int>("Redis:DefaultTtlSeconds", 60)));

            return Ok(response);
        }
    }
}
