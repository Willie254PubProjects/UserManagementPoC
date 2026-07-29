using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Shared.Security.Contracts;

public interface ITokenGenerator
{
    Task<TokenResponse> GenerateTokenAsync(UserInfo user, string? securityVersion = null, CancellationToken cancellationToken = default);

}