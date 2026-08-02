using UserManagementPoC.Shared.Security.Models;

using UserManagementPoC.Shared.Authorization.DTOs;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Identity.Services;

public interface IUserManagementApiClient
{
    Task<VerifyCredentialsResponse?> VerifyCredentialsAsync(VerifyCredentialsRequest request, CancellationToken cancellationToken = default);
    Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<CreateSessionResponse?> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<bool> InvalidateSessionAsync(string securityVersion, CancellationToken cancellationToken = default);
    Task<RoleDto[]> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PermissionDto[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<SessionValidationResult?> GetSessionAsync(string securityVersion, CancellationToken cancellationToken = default);
}