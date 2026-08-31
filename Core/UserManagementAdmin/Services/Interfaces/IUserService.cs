using Microsoft.AspNetCore.Identity;
using UserManagementAdmin.Models.Requests;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserInfo>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<UserInfo?> GetByIdAsync(string id);
    Task<IReadOnlySet<string>> GetDomicileScopeAsync(string userId);
    Task<UserInfo?> FindByExternalLoginAsync(string loginProvider, string providerKey);
    Task<UserInfo?> FindByEmailAsync(string email);
    Task<bool> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName);
    Task<AdminResult<UserInfo>> UpdateAsync(string id, UpdateUserRequest request);
    Task<AdminResult<bool>> DeactivateAsync(string id);
    Task<AdminResult<bool>> DeleteAsync(string id);
    Task<AdminResult<bool>> UpdateUserRoleScopeAsync(string userRoleId, string scopeOrganizationUnitId, bool cascadeOrgStructure);
    Task<AdminResult<bool>> RemoveUserRoleAsync(string userRoleId);
    Task<AdminResult<bool>> UpdateUserPermissionScopeAsync(string userPermissionId, string scopeOrganizationUnitId, bool cascadeOrgStructure);
    Task<AdminResult<bool>> RemoveUserPermissionAsync(string userPermissionId);
    Task<AdminResult<bool>> UpdateUserAccessGroupScopeAsync(string userAccessGroupId, string scopeOrganizationUnitId, bool cascadeOrgStructure);
    Task<AdminResult<bool>> RemoveUserAccessGroupAsync(string userAccessGroupId);
    Task<List<UserLoginInfo>> GetLoginsAsync(string userId);
    Task<AdminResult<bool>> RemoveLoginAsync(string userId, string loginProvider, string providerKey);
    Task<IdentityResult> CreateAsync(string username, string email, string password, string firstName, string lastName, string domicileUnitId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IdentityResult> AssignRoleAsync(string userId, string roleName, string scopeOrganizationUnitId, bool cascadeOrgStructure);
    Task<IdentityResult> RemoveRoleAsync(string userId, string roleName, string? scopeOrganizationUnitId = null);
    Task<IdentityResult> AssignPermissionAsync(string userId, string permissionId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null);
    Task<IdentityResult> RemovePermissionAsync(string userId, string permissionId, string? scopeOrganizationUnitId = null);
    Task<IdentityResult> AssignAccessGroupAsync(string userId, string accessGroupId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null);
    Task<IdentityResult> RemoveAccessGroupAsync(string userId, string accessGroupId, string? scopeOrganizationUnitId = null);
}
