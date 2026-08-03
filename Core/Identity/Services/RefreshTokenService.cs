using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Identity.Services;

public class RefreshTokenService
{
    private readonly ICacheService _cache;
    private readonly TimeSpan _tokenExpiry;
    public RefreshTokenService(ICacheService cache, IConfiguration configuration)
    {
        _cache = cache;
        _tokenExpiry = TimeSpan.FromMinutes(configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationMinutes", 30));
    }
    public async Task<string> GenerateAsync(string userId, string? securityVersion)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var value = $"{userId}|{securityVersion ?? ""}";
        await _cache.SetAsync($"refresh:{token}", value, _tokenExpiry);
        return token;
    }
    public async Task<string?> ValidateAsync(string refreshToken)
    {
        var key = $"refresh:{refreshToken}";
        var value = await _cache.GetAsync<string>(key);
        if (value != null) await _cache.RemoveAsync(key);
        return value;
    }
}
