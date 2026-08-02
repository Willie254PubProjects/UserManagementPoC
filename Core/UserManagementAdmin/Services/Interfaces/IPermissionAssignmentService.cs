using UserManagementPoC.Shared.Authorization.DTOs;

namespace UserManagementAdmin.Services.Interfaces;

public interface IPermissionAssignmentService
{
    Task<RoleDto[]> GetUserRolesAsync(string userId);
    Task<PermissionDto[]> GetUserPermissionsAsync(string userId);
    Task AssignPermissionToRoleAsync(string roleId, string permissionId);
    Task RemovePermissionFromRoleAsync(string roleId, string permissionId);
}
