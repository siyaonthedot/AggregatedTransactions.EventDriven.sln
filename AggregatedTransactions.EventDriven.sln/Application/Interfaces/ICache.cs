namespace Api.Application.Interfaces;

public interface ICache
{
    Task<T?> TryGetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
}