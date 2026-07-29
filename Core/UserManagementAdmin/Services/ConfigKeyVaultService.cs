using Microsoft.Extensions.Configuration;

using UserManagementPoC.Shared.Security.Contracts;

namespace UserManagementAdmin.Services;

public class ConfigKeyVaultService : IKeyVaultService
{
    private readonly IConfiguration _configuration;
    public ConfigKeyVaultService(IConfiguration configuration)
    {
        _configuration = configuration;

    }
    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = _configuration[$"EncryptionSettings:{key}"] ?? _configuration[key];
        return Task.FromResult(value);

    }
}