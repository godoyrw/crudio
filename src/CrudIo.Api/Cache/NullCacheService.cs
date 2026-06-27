// Cache/NullCacheService.cs
namespace CrudIo.Api.Cache;

public class NullCacheService : IRedisCacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T?));
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
}