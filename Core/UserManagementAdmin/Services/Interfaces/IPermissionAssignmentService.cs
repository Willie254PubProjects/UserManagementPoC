namespace UserManagementAdmin.Services.Interfaces;

public interface IPermissionAssignmentService
{
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task AssignPermissionToRoleAsync(string roleId, string permissionId);
    Task RemovePermissionFromRoleAsync(string roleId, string permissionId);
}
