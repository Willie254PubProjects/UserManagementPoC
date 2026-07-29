using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Shared.Security.Contracts;

public interface ITokenValidator
{
    Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

}