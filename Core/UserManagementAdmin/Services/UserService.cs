using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services;

public class UserService : IUserService
{
    private readonly UserManager<BshUser> _userManager;
    public UserService(UserManager<BshUser> userManager)
    {
        _userManager = userManager;
    }
    public async Task<PagedResponse<UserInfo>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _userManager.Users.Select(u => new UserInfo
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            Email = u.Email ?? "",
            FirstName = u.FirstName,
            LastName = u.LastName
        });
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResponse<UserInfo>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
    public async Task<UserInfo?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles
        };
    }
    public async Task<IdentityResult> CreateAsync(string username, string email, string password, string firstName, string lastName)
    {
        var user = new BshUser
        {
            UserName = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName
        };
        return await _userManager.CreateAsync(user, password);
    }
    public async Task<IdentityResult> AssignRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        return await _userManager.AddToRoleAsync(user, roleName);
    }
    public async Task<IdentityResult> RemoveRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        return await _userManager.RemoveFromRoleAsync(user, roleName);
    }
}
