using Api.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Api.Infrastructure.Cache;

public class RedisCache : ICache
{
    private readonly IDatabase _db;
    private readonly IConfiguration _config;

    public RedisCache(IConnectionMultiplexer mux, IConfiguration config)
    {
        _db = mux.GetDatabase();
        _config = config;
    }

    public async Task<T?> TryGetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(json!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }
}