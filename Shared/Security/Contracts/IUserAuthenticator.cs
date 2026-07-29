using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Shared.Security.Contracts;

public interface IUserAuthenticator
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

}