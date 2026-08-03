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
    private readonly IPermissionVersionService _permissionVersionService;
    private readonly IUnitOfWork _uow;
    public UserService(UserManager<BshUser> userManager, RoleManager<BshRole> roleManager, IOrganizationUnitService organizationUnitService, IPermissionVersionService permissionVersionService, IUnitOfWork uow)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _organizationUnitService = organizationUnitService;
        _permissionVersionService = permissionVersionService;
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
    public async Task<IdentityResult> CreateAsync(string username, string email, string password, string firstName, string lastName, string domicileUnitId, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(domicileUnitId))
            return IdentityResult.Failed(new IdentityError { Description = "Domicile organization unit is required" });
        var domicile = await _uow.Repository<OrganizationUnit>().FirstOrDefaultAsync(o => o.Id == domicileUnitId);
        if (domicile == null)
            return IdentityResult.Failed(new IdentityError { Description = "Domicile organization unit not found" });
        if (domicile.Status != OrganizationUnitStatus.Active)
            return IdentityResult.Failed(new IdentityError { Description = "Domicile organization unit is not active" });

        var user = new BshUser
        {
            UserName = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DomicileUnitId = domicileUnitId,
            StartDate = startDate ?? DateTime.UtcNow,
            EndDate = endDate
        };
        return await _userManager.CreateAsync(user, password);
    }
    public async Task<IdentityResult> AssignRoleAsync(string userId, string roleName, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId))
            return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit is required" });
        var scopeExists = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.Id == scopeOrganizationUnitId);
        if (!scopeExists) return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit not found" });

        var exists = await _uow.Repository<UserRole>().AnyAsync(ur =>
            ur.RoleId == role.Id && ur.UserId == user.Id
            && ur.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && ur.CascadeOrgStructure == cascadeOrgStructure);
        if (exists) return IdentityResult.Failed(new IdentityError { Description = "Role already assigned to user at this scope" });

        var now = DateTime.UtcNow;
        await _uow.Repository<UserRole>().AddAsync(new UserRole
        {
            RoleId = role.Id,
            UserId = user.Id,
            ScopeOrganizationUnitId = scopeOrganizationUnitId,
            CascadeOrgStructure = cascadeOrgStructure,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        });
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> RemoveRoleAsync(string userId, string roleName, string? scopeOrganizationUnitId = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        var matches = await _uow.Repository<UserRole>().FindAsync(r =>
            r.RoleId == role.Id && r.UserId == user.Id
            && (scopeOrganizationUnitId == null || r.ScopeOrganizationUnitId == scopeOrganizationUnitId));
        if (!matches.Any()) return IdentityResult.Failed(new IdentityError { Description = "Role not assigned to user" });
        _uow.Repository<UserRole>().DeleteRange(matches);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> AssignPermissionAsync(string userId, string permissionId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var permission = await _uow.Repository<Permission>().GetByIdAsync(permissionId);
        if (permission == null) return IdentityResult.Failed(new IdentityError { Description = "Permission not found" });

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId))
            return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit is required" });
        var scopeExists = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.Id == scopeOrganizationUnitId);
        if (!scopeExists) return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit not found" });

        var exists = await _uow.Repository<UserPermission>().AnyAsync(up =>
            up.PermissionId == permission.Id && up.UserId == user.Id
            && up.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && up.CascadeOrgStructure == cascadeOrgStructure);
        if (exists) return IdentityResult.Failed(new IdentityError { Description = "Permission already assigned to user at this scope" });

        var now = DateTime.UtcNow;
        await _uow.Repository<UserPermission>().AddAsync(new UserPermission
        {
            PermissionId = permission.Id,
            UserId = user.Id,
            ScopeOrganizationUnitId = scopeOrganizationUnitId,
            CascadeOrgStructure = cascadeOrgStructure,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = startDate ?? now,
            EndDate = endDate
        });
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> RemovePermissionAsync(string userId, string permissionId, string? scopeOrganizationUnitId = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var permission = await _uow.Repository<Permission>().GetByIdAsync(permissionId);
        if (permission == null) return IdentityResult.Failed(new IdentityError { Description = "Permission not found" });

        var matches = await _uow.Repository<UserPermission>().FindAsync(p =>
            p.PermissionId == permission.Id && p.UserId == user.Id
            && (scopeOrganizationUnitId == null || p.ScopeOrganizationUnitId == scopeOrganizationUnitId));
        if (!matches.Any()) return IdentityResult.Failed(new IdentityError { Description = "Permission not assigned to user" });
        _uow.Repository<UserPermission>().DeleteRange(matches);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> AssignAccessGroupAsync(string userId, string accessGroupId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(accessGroupId);
        if (group == null) return IdentityResult.Failed(new IdentityError { Description = "Access group not found" });

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId))
            return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit is required" });
        var scopeExists = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.Id == scopeOrganizationUnitId);
        if (!scopeExists) return IdentityResult.Failed(new IdentityError { Description = "Scope organization unit not found" });

        var exists = await _uow.Repository<UserAccessGroup>().AnyAsync(uag =>
            uag.AccessGroupId == group.Id && uag.UserId == user.Id
            && uag.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && uag.CascadeOrgStructure == cascadeOrgStructure);
        if (exists) return IdentityResult.Failed(new IdentityError { Description = "Access group already assigned to user at this scope" });

        var now = DateTime.UtcNow;
        await _uow.Repository<UserAccessGroup>().AddAsync(new UserAccessGroup
        {
            AccessGroupId = group.Id,
            UserId = user.Id,
            ScopeOrganizationUnitId = scopeOrganizationUnitId,
            CascadeOrgStructure = cascadeOrgStructure,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = startDate ?? now,
            EndDate = endDate
        });
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
    public async Task<IdentityResult> RemoveAccessGroupAsync(string userId, string accessGroupId, string? scopeOrganizationUnitId = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(accessGroupId);
        if (group == null) return IdentityResult.Failed(new IdentityError { Description = "Access group not found" });

        var matches = await _uow.Repository<UserAccessGroup>().FindAsync(uag =>
            uag.AccessGroupId == group.Id && uag.UserId == user.Id
            && (scopeOrganizationUnitId == null || uag.ScopeOrganizationUnitId == scopeOrganizationUnitId));
        if (!matches.Any()) return IdentityResult.Failed(new IdentityError { Description = "Access group not assigned to user" });
        _uow.Repository<UserAccessGroup>().DeleteRange(matches);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(user.Id);
        return IdentityResult.Success;
    }
}
