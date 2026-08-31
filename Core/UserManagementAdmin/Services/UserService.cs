using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
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
    public async Task<PagedResponse<UserInfo>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var query = _userManager.Users
            .Where(u => string.IsNullOrEmpty(search)
                || u.UserName!.Contains(search)
                || u.Email!.Contains(search)
                || u.FirstName.Contains(search)
                || u.LastName.Contains(search))
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
        return await MapToUserInfoAsync(user);
    }
    private async Task<UserInfo> MapToUserInfoAsync(BshUser user)
    {
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
    public async Task<IReadOnlySet<string>> GetDomicileScopeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.DomicileUnitId)) return new HashSet<string>();
        return await _organizationUnitService.ResolveScopeAsync(user.DomicileUnitId, true);
    }
    public async Task<UserInfo?> FindByExternalLoginAsync(string loginProvider, string providerKey)
    {
        var user = await _userManager.FindByLoginAsync(loginProvider, providerKey);
        if (user == null) return null;
        return await MapToUserInfoAsync(user);
    }
    public async Task<UserInfo?> FindByEmailAsync(string email)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.NormalizedEmail == _userManager.NormalizeEmail(email));
        if (user == null) return null;
        return await MapToUserInfoAsync(user);
    }
    public async Task<bool> LinkExternalLoginAsync(string userId, string loginProvider, string providerKey, string? providerDisplayName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, providerDisplayName));
        return result.Succeeded;
    }
    public async Task<AdminResult<UserInfo>> UpdateAsync(string id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return AdminResult<UserInfo>.Fail("User not found");

        if (string.IsNullOrWhiteSpace(request.DomicileUnitId))
            return AdminResult<UserInfo>.Fail("Domicile organization unit is required");
        var domicile = await _uow.Repository<OrganizationUnit>().FirstOrDefaultAsync(o => o.Id == request.DomicileUnitId);
        if (domicile == null) return AdminResult<UserInfo>.Fail("Domicile organization unit not found");
        if (domicile.Status != OrganizationUnitStatus.Active)
            return AdminResult<UserInfo>.Fail("Domicile organization unit is not active");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.DomicileUnitId = request.DomicileUnitId;
        if (request.StartDate.HasValue) user.StartDate = request.StartDate.Value;
        user.EndDate = request.EndDate;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return AdminResult<UserInfo>.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));

        return AdminResult<UserInfo>.Ok(await MapToUserInfoAsync(user));
    }
    public async Task<AdminResult<bool>> DeactivateAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return AdminResult<bool>.Fail("User not found");
        if (user.EndDate != null && user.EndDate.Value <= DateTime.UtcNow)
            return AdminResult<bool>.Fail("User is already inactive");

        user.EndDate = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return AdminResult<bool>.Fail("Failed to deactivate user");

        await _permissionVersionService.BumpUserAsync(id);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> DeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return AdminResult<bool>.Fail("User not found");

        var hasActiveAssignments =
            await _uow.Repository<UserRole>().AnyAsync(ur => ur.UserId == id && ur.Status == AssignmentStatus.Active)
            || await _uow.Repository<UserPermission>().AnyAsync(up => up.UserId == id && up.Status == AssignmentStatus.Active)
            || await _uow.Repository<UserAccessGroup>().AnyAsync(uag => uag.UserId == id && uag.Status == AssignmentStatus.Active);
        if (hasActiveAssignments)
            return AdminResult<bool>.Fail("Cannot delete a user with active assignments; deactivate or remove them first");

        var hasSessions = await _uow.Repository<UserSession>().AnyAsync(s => s.UserId == id && s.IsActive);
        if (hasSessions)
            return AdminResult<bool>.Fail("Cannot delete a user with active sessions");

        _uow.Repository<UserRole>().DeleteRange(await _uow.Repository<UserRole>().FindAsync(ur => ur.UserId == id));
        _uow.Repository<UserPermission>().DeleteRange(await _uow.Repository<UserPermission>().FindAsync(up => up.UserId == id));
        _uow.Repository<UserAccessGroup>().DeleteRange(await _uow.Repository<UserAccessGroup>().FindAsync(uag => uag.UserId == id));
        _uow.Repository<UserSession>().DeleteRange(await _uow.Repository<UserSession>().FindAsync(s => s.UserId == id));
        await _uow.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return AdminResult<bool>.Fail("Failed to delete user");

        await _permissionVersionService.BumpUserAsync(id);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<List<UserLoginInfo>> GetLoginsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<UserLoginInfo>();
        return (await _userManager.GetLoginsAsync(user)).ToList();
    }
    public async Task<AdminResult<bool>> RemoveLoginAsync(string userId, string loginProvider, string providerKey)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return AdminResult<bool>.Fail("User not found");
        var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (!result.Succeeded) return AdminResult<bool>.Fail("Failed to remove external login");
        return AdminResult<bool>.Ok(true);
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

        var now = DateTime.UtcNow;
        var user = new BshUser
        {
            UserName = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DomicileUnitId = domicileUnitId,
            StartDate = startDate ?? now,
            EndDate = endDate,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system"
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
    public async Task<AdminResult<bool>> UpdateUserRoleScopeAsync(string userRoleId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var assignment = await _uow.Repository<UserRole>().GetByIdAsync(userRoleId);
        if (assignment == null) return AdminResult<bool>.Fail("Role assignment not found");

        var validationError = await ValidateScopeAsync(scopeOrganizationUnitId);
        if (validationError != null) return AdminResult<bool>.Fail(validationError);

        var duplicate = await _uow.Repository<UserRole>().AnyAsync(ur =>
            ur.Id != userRoleId && ur.UserId == assignment.UserId && ur.RoleId == assignment.RoleId
            && ur.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && ur.CascadeOrgStructure == cascadeOrgStructure);
        if (duplicate) return AdminResult<bool>.Fail("Role already assigned to user at this scope");

        assignment.ScopeOrganizationUnitId = scopeOrganizationUnitId;
        assignment.CascadeOrgStructure = cascadeOrgStructure;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.LastUpdatedBy = "system";
        _uow.Repository<UserRole>().Update(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> RemoveUserRoleAsync(string userRoleId)
    {
        var assignment = await _uow.Repository<UserRole>().GetByIdAsync(userRoleId);
        if (assignment == null) return AdminResult<bool>.Fail("Role assignment not found");
        _uow.Repository<UserRole>().Delete(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> UpdateUserPermissionScopeAsync(string userPermissionId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var assignment = await _uow.Repository<UserPermission>().GetByIdAsync(userPermissionId);
        if (assignment == null) return AdminResult<bool>.Fail("Permission assignment not found");

        var validationError = await ValidateScopeAsync(scopeOrganizationUnitId);
        if (validationError != null) return AdminResult<bool>.Fail(validationError);

        var duplicate = await _uow.Repository<UserPermission>().AnyAsync(up =>
            up.Id != userPermissionId && up.UserId == assignment.UserId && up.PermissionId == assignment.PermissionId
            && up.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && up.CascadeOrgStructure == cascadeOrgStructure);
        if (duplicate) return AdminResult<bool>.Fail("Permission already assigned to user at this scope");

        assignment.ScopeOrganizationUnitId = scopeOrganizationUnitId;
        assignment.CascadeOrgStructure = cascadeOrgStructure;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.LastUpdatedBy = "system";
        _uow.Repository<UserPermission>().Update(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> RemoveUserPermissionAsync(string userPermissionId)
    {
        var assignment = await _uow.Repository<UserPermission>().GetByIdAsync(userPermissionId);
        if (assignment == null) return AdminResult<bool>.Fail("Permission assignment not found");
        _uow.Repository<UserPermission>().Delete(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> UpdateUserAccessGroupScopeAsync(string userAccessGroupId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var assignment = await _uow.Repository<UserAccessGroup>().GetByIdAsync(userAccessGroupId);
        if (assignment == null) return AdminResult<bool>.Fail("Access group assignment not found");

        var validationError = await ValidateScopeAsync(scopeOrganizationUnitId);
        if (validationError != null) return AdminResult<bool>.Fail(validationError);

        var duplicate = await _uow.Repository<UserAccessGroup>().AnyAsync(uag =>
            uag.Id != userAccessGroupId && uag.UserId == assignment.UserId && uag.AccessGroupId == assignment.AccessGroupId
            && uag.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && uag.CascadeOrgStructure == cascadeOrgStructure);
        if (duplicate) return AdminResult<bool>.Fail("Access group already assigned to user at this scope");

        assignment.ScopeOrganizationUnitId = scopeOrganizationUnitId;
        assignment.CascadeOrgStructure = cascadeOrgStructure;
        assignment.UpdatedAt = DateTime.UtcNow;
        assignment.LastUpdatedBy = "system";
        _uow.Repository<UserAccessGroup>().Update(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    public async Task<AdminResult<bool>> RemoveUserAccessGroupAsync(string userAccessGroupId)
    {
        var assignment = await _uow.Repository<UserAccessGroup>().GetByIdAsync(userAccessGroupId);
        if (assignment == null) return AdminResult<bool>.Fail("Access group assignment not found");
        _uow.Repository<UserAccessGroup>().Delete(assignment);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(assignment.UserId);
        return AdminResult<bool>.Ok(true);
    }
    private async Task<string?> ValidateScopeAsync(string scopeOrganizationUnitId)
    {
        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId)) return "Scope organization unit is required";
        var scope = await _uow.Repository<OrganizationUnit>().FirstOrDefaultAsync(o => o.Id == scopeOrganizationUnitId);
        if (scope == null) return "Scope organization unit not found";
        if (scope.Status != OrganizationUnitStatus.Active) return "Scope organization unit is not active";
        return null;
    }
}
