using UserManagementPoC.Shared.Security.Models;

using UserManagementPoC.Shared.Authorization.DTOs;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Identity.Services;

public interface IUserManagementApiClient
{
    Task<UserInfo?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<CreateSessionResponse?> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<bool> InvalidateSessionAsync(string securityVersion, CancellationToken cancellationToken = default);
    Task<RoleDto[]> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<PermissionDto[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<SessionValidationResult?> GetSessionAsync(string securityVersion, CancellationToken cancellationToken = default);
    Task<UserInfo?> FindByExternalLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default);
    Task<UserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName, CancellationToken cancellationToken = default);
    Task<string[]?> ResolveOrgUnitScopeAsync(string value, CancellationToken cancellationToken = default);
    Task<string[]?> GetDomicileScopeAsync(string userId, CancellationToken cancellationToken = default);
}