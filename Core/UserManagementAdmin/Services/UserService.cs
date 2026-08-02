using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Repositories;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services;

public class UserService : IUserService
{
    private readonly UserManager<BshUser> _userManager;
    private readonly RoleManager<BshRole> _roleManager;
    private readonly IOrganizationUnitService _organizationUnitService;
    private readonly IUnitOfWork _uow;
    public UserService(UserManager<BshUser> userManager, RoleManager<BshRole> roleManager, IOrganizationUnitService organizationUnitService, IUnitOfWork uow)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _organizationUnitService = organizationUnitService;
        _uow = uow;
    }
    public async Task<PagedResponse<UserInfo>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _userManager.Users
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FirstName,
                u.LastName,
                u.DomicileUnitId
            });
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = new List<UserInfo>();
        foreach (var x in items)
        {
            var codes = await _organizationUnitService.ResolveCodesAsync(x.DomicileUnitId);
            result.Add(new UserInfo
            {
                Id = x.Id,
                UserName = x.UserName ?? "",
                Email = x.Email ?? "",
                FirstName = x.FirstName,
                LastName = x.LastName,
                BankId = codes.BankId,
                BranchId = codes.BranchId,
                CountryCode = codes.CountryCode,
                IsAuthenticated = true
            });
        }
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
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;
        var codes = await _organizationUnitService.ResolveCodesAsync(user.DomicileUnitId);
        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            BankId = codes.BankId,
            BranchId = codes.BranchId,
            CountryCode = codes.CountryCode,
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
    public async Task<IdentityResult> AssignRoleAsync(string userId, string roleName, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        var exists = await _uow.Repository<UserRole>().AnyAsync(ur => ur.RoleId == role.Id && ur.UserId == user.Id);
        if (exists) return IdentityResult.Failed(new IdentityError { Description = "Role already assigned to user" });

        var now = DateTime.UtcNow;
        await _uow.Repository<UserRole>().AddAsync(new UserRole
        {
            RoleId = role.Id,
            UserId = user.Id,
            ScopeOrganizationUnitId = scopeOrganizationUnitId,
            CascadeOrgStructure = cascadeOrgStructure,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        });
        await _uow.SaveChangesAsync();
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> RemoveRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        var ur = await _uow.Repository<UserRole>().FirstOrDefaultAsync(r => r.RoleId == role.Id && r.UserId == user.Id);
        if (ur == null) return IdentityResult.Failed(new IdentityError { Description = "Role not assigned to user" });
        _uow.Repository<UserRole>().Delete(ur);
        await _uow.SaveChangesAsync();
        return IdentityResult.Success;
    }
}
