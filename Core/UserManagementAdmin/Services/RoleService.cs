using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Repositories;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<BshRole> _roleManager;
    private readonly UserManager<BshUser> _userManager;
    private readonly IOrganizationUnitService _organizationUnitService;
    private readonly IUnitOfWork _uow;
    public RoleService(RoleManager<BshRole> roleManager, UserManager<BshUser> userManager, IOrganizationUnitService organizationUnitService, IUnitOfWork uow)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _organizationUnitService = organizationUnitService;
        _uow = uow;
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
    public async Task<BshRole?> GetByIdAsync(string id)
    {
        return await _roleManager.Roles
            .Include(r => r.Permissions).ThenInclude(rp => rp.Permission).ThenInclude(p => p.Type)
            .Include(r => r.Permissions).ThenInclude(rp => rp.Permission).ThenInclude(p => p.SubPermission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    public async Task<IdentityResult> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return IdentityResult.Failed(new IdentityError { Description = "Name is required" });
        var now = DateTime.UtcNow;
        var role = new BshRole
        {
            Name = name,
            Description = name,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        };
        return await _roleManager.CreateAsync(role);
    }
    public async Task<IdentityResult> UpdateAsync(string id, string name, string description)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });
        if (string.IsNullOrWhiteSpace(name)) return IdentityResult.Failed(new IdentityError { Description = "Name is required" });

        if (!string.Equals(role.Name, name, StringComparison.Ordinal))
        {
            var nameResult = await _roleManager.SetRoleNameAsync(role, name);
            if (!nameResult.Succeeded) return nameResult;
        }
        role.Description = description ?? role.Description;
        role.UpdatedAt = DateTime.UtcNow;
        role.LastUpdatedBy = "system";
        return await _roleManager.UpdateAsync(role);
    }
    public async Task<IdentityResult> DeleteAsync(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Role not found" });

        var hasUsers = await _uow.Repository<UserRole>().AnyAsync(ur => ur.RoleId == roleId && ur.Status == AssignmentStatus.Active);
        if (hasUsers) return IdentityResult.Failed(new IdentityError { Description = "Cannot delete a role that has active user assignments" });
        var inGroups = await _uow.Repository<AccessGroupRole>().AnyAsync(agr => agr.RoleId == roleId);
        if (inGroups) return IdentityResult.Failed(new IdentityError { Description = "Cannot delete a role that is assigned to an access group" });

        return await _roleManager.DeleteAsync(role);
    }
    public async Task<PagedResponse<UserInfo>> GetUsersAsync(string roleId, int page = 1, int pageSize = 20)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return new PagedResponse<UserInfo>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            Items = new List<UserInfo>()
        };

        var now = DateTime.UtcNow;
        var predicate = (System.Linq.Expressions.Expression<Func<UserRole, bool>>)(ur =>
            ur.RoleId == roleId
            && ur.Status == AssignmentStatus.Active
            && ur.StartDate <= now
            && (ur.EndDate == null || ur.EndDate >= now));
        var totalCount = await _uow.Repository<UserRole>().CountAsync(predicate);
        var items = await _uow.Repository<UserRole>().FindAsync(
            predicate,
            q => q.Include(ur => ur.User).OrderBy(ur => ur.User!.UserName).Skip((page - 1) * pageSize).Take(pageSize));

        var result = new List<UserInfo>();
        foreach (var assignment in items)
        {
            if (assignment.User == null) continue;
            var codes = await _organizationUnitService.ResolveCodesAsync(assignment.User.DomicileUnitId);
            result.Add(new UserInfo
            {
                Id = assignment.User.Id,
                UserName = assignment.User.UserName ?? "",
                Email = assignment.User.Email ?? "",
                FirstName = assignment.User.FirstName,
                LastName = assignment.User.LastName,
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
}