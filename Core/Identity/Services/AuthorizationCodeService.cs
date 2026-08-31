using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Identity.Services;

public class AuthorizationCodeService
{
    private readonly ICacheService _cache;
    private readonly TimeSpan _codeTtl;
    public AuthorizationCodeService(ICacheService cache, IConfiguration configuration)
    {
        _cache = cache;
        _codeTtl = TimeSpan.FromMinutes(configuration.GetValue<int>("OpenIdConnect:AuthorizationCodeTtlMinutes", 5));
    }
    public async Task<string> GenerateAsync(string userId, string securityVersion, string clientId, CancellationToken cancellationToken = default)
    {
        var code = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        await _cache.SetAsync($"authcode:{code}", new AuthorizationCode
        {
            UserId = userId,
            SecurityVersion = securityVersion,
            ClientId = clientId
        }, _codeTtl, cancellationToken);
        return code;
    }
    public async Task<AuthorizationCode?> ConsumeAsync(string code, CancellationToken cancellationToken = default)
    {
        var key = $"authcode:{code}";
        var value = await _cache.GetAsync<AuthorizationCode>(key, cancellationToken);
        if (value != null) await _cache.RemoveAsync(key, cancellationToken);
        return value;
    }
}

public class AuthorizationCode
{
    public string UserId { get; set; }
    public string SecurityVersion { get; set; }
    public string ClientId { get; set; }
}