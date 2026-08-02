using Microsoft.AspNetCore.Identity;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services.Interfaces;

public interface IUserService
{
    Task<PagedResponse<UserInfo>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<UserInfo?> GetByIdAsync(string id);
    Task<IdentityResult> CreateAsync(string username, string email, string password, string firstName, string lastName);
    Task<IdentityResult> AssignRoleAsync(string userId, string roleName, string scopeOrganizationUnitId, bool cascadeOrgStructure);
    Task<IdentityResult> RemoveRoleAsync(string userId, string roleName);
}
