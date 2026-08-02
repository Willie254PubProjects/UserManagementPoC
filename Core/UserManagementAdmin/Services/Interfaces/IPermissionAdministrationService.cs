using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services.Interfaces;

public interface IPermissionAdministrationService
{
    Task<List<PermissionType>> GetPermissionTypesAsync();
    Task<PermissionType> CreatePermissionTypeAsync(string name, string description);
    Task<List<SubPermission>> GetSubPermissionsAsync();
    Task<SubPermission> CreateSubPermissionAsync(string name, string description);
    Task<List<Permission>> GetPermissionsAsync();
}
