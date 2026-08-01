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
        var query = _userManager.Users
            .Include(u => u.Subsidiary)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FirstName,
                u.LastName,
                u.BranchId,
                BankId = u.Subsidiary.BankId,
                u.Subsidiary.CountryCode
            });
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = items.Select(x => new UserInfo
        {
            Id = x.Id,
            UserName = x.UserName ?? "",
            Email = x.Email ?? "",
            FirstName = x.FirstName,
            LastName = x.LastName,
            BankId = x.BankId.ToString(),
            BranchId = x.BranchId,
            CountryCode = x.CountryCode,
            IsAuthenticated = true
        }).ToList();
        return new PagedResponse<UserInfo>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = result
        };
    }
    public async Task<UserInfo?> GetByIdAsync(string id)
    {
        var user = await _userManager.Users
            .Include(u => u.Subsidiary)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;
        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            BankId = user.Subsidiary.BankId.ToString(),
            BranchId = user.BranchId,
            CountryCode = user.Subsidiary.CountryCode,
            IsAuthenticated = true
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
