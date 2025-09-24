using StackExchange.Redis;
using System.Text.Json;

namespace Api.Infrastructure
{
    public interface ICache
    {
        Task<T?> TryGetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan ttl);
    }

    public class RedisCache(IConnectionMultiplexer mux) : ICache
    {
        private readonly IDatabase _db = mux.GetDatabase();
        public async Task<T?> TryGetAsync<T>(string key)
        {
            var json = await _db.StringGetAsync(key);
            if (json.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(json!);
        }
        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, ttl);
        }
    }
}
