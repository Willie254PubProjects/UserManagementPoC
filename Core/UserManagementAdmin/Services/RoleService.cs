using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<BshRole> _roleManager;
    public RoleService(RoleManager<BshRole> roleManager)
    {
        _roleManager = roleManager;
    }
    public async Task<PagedResponse<BshRole>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _roleManager.Roles;
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResponse<BshRole>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
    public async Task<IdentityResult> CreateAsync(string name)
    {
        return await _roleManager.CreateAsync(new BshRole { Name = name });
    }
    public async Task<IdentityResult> DeleteAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });
        return await _roleManager.DeleteAsync(role);
    }
}
