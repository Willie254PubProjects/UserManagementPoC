using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services.Interfaces;

public interface IPermissionAdministrationService
{
    Task<List<PermissionType>> GetPermissionTypesAsync();
    Task<PermissionType> CreatePermissionTypeAsync(string name, string description);
    Task<AdminResult<PermissionType>> UpdatePermissionTypeAsync(string id, string name, string description);
    Task<AdminResult<bool>> DeletePermissionTypeAsync(string id);
    Task<List<SubPermission>> GetSubPermissionsAsync();
    Task<SubPermission> CreateSubPermissionAsync(string name, string description);
    Task<AdminResult<SubPermission>> UpdateSubPermissionAsync(string id, string name, string description);
    Task<AdminResult<bool>> DeleteSubPermissionAsync(string id);
    Task<List<Permission>> GetPermissionsAsync();
    Task<AdminResult<Permission>> CreatePermissionAsync(string permissionTypeId, string subPermissionId, string? description);
    Task<AdminResult<bool>> DeletePermissionAsync(string permissionId);
}