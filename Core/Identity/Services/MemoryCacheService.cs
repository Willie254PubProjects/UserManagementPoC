using Microsoft.Extensions.Caching.Memory;

using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Identity.Services;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;

    }
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(key, out T? value);

        return Task.FromResult(value);
    }
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue) options.AbsoluteExpirationRelativeToNow = expiration;
        _cache.Set(key, value, options);

        return Task.CompletedTask;
    }
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);

        return Task.CompletedTask;
    }
    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out _))
        {
            var existing = _cache.Get(key);
            _cache.Set(key, existing);

        }
        return Task.CompletedTask;
    }
}