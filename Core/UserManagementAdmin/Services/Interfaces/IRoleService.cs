using Microsoft.AspNetCore.Identity;
using UserManagementAdmin.Models.Entities;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services.Interfaces;

public interface IRoleService
{
    Task<PagedResponse<BshRole>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<BshRole?> GetByIdAsync(string id);
    Task<IdentityResult> CreateAsync(string name);
    Task<IdentityResult> UpdateAsync(string id, string name, string description);
    Task<IdentityResult> DeleteAsync(string roleId);
    Task<PagedResponse<UserInfo>> GetUsersAsync(string roleId, int page = 1, int pageSize = 20);
}