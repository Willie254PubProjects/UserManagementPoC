using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services;

public class RoleService
{
    private readonly RoleManager<BshRole> _roleManager;
    public RoleService(RoleManager<BshRole> roleManager)
    {
        _roleManager = roleManager;

    }
    public async Task<List<BshRole>> GetAllAsync()
    {
        return await _roleManager.Roles.ToListAsync();

    }
    public async Task<IdentityResult> CreateAsync(string name)
    {
        return await _roleManager.CreateAsync(new BshRole
        {
            Name = name
        });

    }
    public async Task<IdentityResult> DeleteAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return IdentityResult.Failed(new IdentityError
        {
            Description = "Role not found"
        });

        return await _roleManager.DeleteAsync(role);
    }
}