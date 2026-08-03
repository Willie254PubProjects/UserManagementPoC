using UserManagementAdmin.Models.Entities;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Services.Interfaces;

public interface IAccessGroupService
{
    Task<PagedResponse<AccessGroup>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<AccessGroup?> GetByIdAsync(string id);
    Task<AdminResult<AccessGroup>> CreateAsync(string name, string description, DateTime? startDate = null, DateTime? endDate = null);
    Task<AdminResult<AccessGroup>> UpdateAsync(string id, string name, string description, DateTime? endDate);
    Task<AdminResult<bool>> DeleteAsync(string id);

    Task<AdminResult<bool>> AssignRoleAsync(string accessGroupId, string roleId);
    Task<AdminResult<bool>> RemoveRoleAsync(string accessGroupId, string roleId);
    Task<AdminResult<bool>> AssignPermissionAsync(string accessGroupId, string permissionId);
    Task<AdminResult<bool>> RemovePermissionAsync(string accessGroupId, string permissionId);
    Task<AdminResult<bool>> AssignUserAsync(string accessGroupId, string userId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null);
    Task<AdminResult<bool>> RemoveUserAsync(string accessGroupId, string userId, string? scopeOrganizationUnitId = null);
}
