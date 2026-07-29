using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Identity.Services;

public class RefreshTokenService
{
    private readonly ICacheService _cache;
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromDays(7);
    public RefreshTokenService(ICacheService cache)
    {
        _cache = cache;
    }
    public async Task<string> GenerateAsync(string userId)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        await _cache.SetAsync($"refresh:{token}", userId, TokenExpiry);
        return token;
    }
    public async Task<string?> ValidateAsync(string refreshToken)
    {
        var key = $"refresh:{refreshToken}";
        var userId = await _cache.GetAsync<string>(key);
        if (userId != null) await _cache.RemoveAsync(key);
        return userId;
    }
}