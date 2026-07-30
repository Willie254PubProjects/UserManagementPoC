using Microsoft.AspNetCore.Identity;
using UserManagementAdmin.Models.Entities;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Services.Interfaces;

public interface IRoleService
{
    Task<PagedResponse<BshRole>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IdentityResult> CreateAsync(string name);
    Task<IdentityResult> DeleteAsync(string roleId);
}
