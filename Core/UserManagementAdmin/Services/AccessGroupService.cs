using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Helpers;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class AccessGroupService : IAccessGroupService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionVersionService _permissionVersionService;
    public AccessGroupService(IUnitOfWork uow, IPermissionVersionService permissionVersionService)
    {
        _uow = uow;
        _permissionVersionService = permissionVersionService;
    }

    public async Task<PagedResponse<AccessGroup>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _uow.Repository<AccessGroup>().CountAsync();
        var items = await _uow.Repository<AccessGroup>().FindAsync(
            _ => true,
            q => q.OrderBy(g => g.Name).Skip((page - 1) * pageSize).Take(pageSize));
        return new PagedResponse<AccessGroup>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.ToList()
        };
    }

    public async Task<AccessGroup?> GetByIdAsync(string id)
    {
        return await _uow.Repository<AccessGroup>().FirstOrDefaultAsync(
            g => g.Id == id,
            q => q.Include(g => g.Roles).ThenInclude(r => r.Role)
                  .Include(g => g.Permissions).ThenInclude(p => p.Permission).ThenInclude(p => p.Type)
                  .Include(g => g.Permissions).ThenInclude(p => p.Permission).ThenInclude(p => p.SubPermission)
                  .Include(g => g.Users).ThenInclude(u => u.User));
    }
    public async Task<PagedResponse<UserAccessGroup>> GetUsersAsync(string accessGroupId, int page = 1, int pageSize = 20)
    {
        var predicate = (Expression<Func<UserAccessGroup, bool>>)(uag => uag.AccessGroupId == accessGroupId);
        var totalCount = await _uow.Repository<UserAccessGroup>().CountAsync(predicate);
        var items = await _uow.Repository<UserAccessGroup>().FindAsync(
            predicate,
            q => q.Include(uag => uag.User).OrderByDescending(uag => uag.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize));
        return new PagedResponse<UserAccessGroup>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.ToList()
        };
    }

    public async Task<AdminResult<AccessGroup>> CreateAsync(string name, string description, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<AccessGroup>.Fail("Name is required");
        var duplicate = await _uow.Repository<AccessGroup>().AnyAsync(g => g.Name.ToLower() == name.ToLower());
        if (duplicate) return AdminResult<AccessGroup>.Fail($"Access group '{name}' already exists");

        var now = DateTime.UtcNow;
        var group = new AccessGroup
        {
            Id = KeyGen.GenerateKey(),
            Name = name,
            Description = description ?? "",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = startDate ?? now,
            EndDate = endDate
        };
        await _uow.Repository<AccessGroup>().AddAsync(group);
        await _uow.SaveChangesAsync();
        return AdminResult<AccessGroup>.Ok(group);
    }

    public async Task<AdminResult<AccessGroup>> UpdateAsync(string id, string name, string description, DateTime? endDate)
    {
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(id);
        if (group == null) return AdminResult<AccessGroup>.Fail("Access group not found");
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<AccessGroup>.Fail("Name is required");

        var duplicate = await _uow.Repository<AccessGroup>().AnyAsync(g => g.Id != id && g.Name.ToLower() == name.ToLower());
        if (duplicate) return AdminResult<AccessGroup>.Fail($"Access group '{name}' already exists");

        group.Name = name;
        group.Description = description ?? "";
        group.EndDate = endDate;
        group.UpdatedAt = DateTime.UtcNow;
        group.LastUpdatedBy = "system";

        _uow.Repository<AccessGroup>().Update(group);
        await _uow.SaveChangesAsync();
        return AdminResult<AccessGroup>.Ok(group);
    }

    public async Task<AdminResult<bool>> DeleteAsync(string id)
    {
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(id);
        if (group == null) return AdminResult<bool>.Fail("Access group not found");

        _uow.Repository<AccessGroup>().Delete(group);
        await _uow.SaveChangesAsync();
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> AssignRoleAsync(string accessGroupId, string roleId)
    {
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(accessGroupId);
        if (group == null) return AdminResult<bool>.Fail("Access group not found");
        var role = await _uow.Repository<BshRole>().GetByIdAsync(roleId);
        if (role == null) return AdminResult<bool>.Fail("Role not found");

        var exists = await _uow.Repository<AccessGroupRole>().AnyAsync(r => r.AccessGroupId == accessGroupId && r.RoleId == roleId);
        if (exists) return AdminResult<bool>.Fail("Role already assigned to access group");

        await _uow.Repository<AccessGroupRole>().AddAsync(new AccessGroupRole
        {
            AccessGroupId = accessGroupId,
            RoleId = roleId
        });
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpRoleUsersAsync(roleId);
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> RemoveRoleAsync(string accessGroupId, string roleId)
    {
        var row = await _uow.Repository<AccessGroupRole>().FirstOrDefaultAsync(r => r.AccessGroupId == accessGroupId && r.RoleId == roleId);
        if (row == null) return AdminResult<bool>.Fail("Role is not assigned to access group");
        _uow.Repository<AccessGroupRole>().Delete(row);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpRoleUsersAsync(roleId);
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> AssignPermissionAsync(string accessGroupId, string permissionId)
    {
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(accessGroupId);
        if (group == null) return AdminResult<bool>.Fail("Access group not found");
        var permission = await _uow.Repository<Permission>().GetByIdAsync(permissionId);
        if (permission == null) return AdminResult<bool>.Fail("Permission not found");

        var exists = await _uow.Repository<AccessGroupPermission>().AnyAsync(p => p.AccessGroupId == accessGroupId && p.PermissionId == permissionId);
        if (exists) return AdminResult<bool>.Fail("Permission already assigned to access group");

        await _uow.Repository<AccessGroupPermission>().AddAsync(new AccessGroupPermission
        {
            AccessGroupId = accessGroupId,
            PermissionId = permissionId
        });
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpAccessGroupUsersAsync(accessGroupId);
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> RemovePermissionAsync(string accessGroupId, string permissionId)
    {
        var row = await _uow.Repository<AccessGroupPermission>().FirstOrDefaultAsync(p => p.AccessGroupId == accessGroupId && p.PermissionId == permissionId);
        if (row == null) return AdminResult<bool>.Fail("Permission is not assigned to access group");
        _uow.Repository<AccessGroupPermission>().Delete(row);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpAccessGroupUsersAsync(accessGroupId);
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> AssignUserAsync(string accessGroupId, string userId, string scopeOrganizationUnitId, bool cascadeOrgStructure, DateTime? startDate = null, DateTime? endDate = null)
    {
        var group = await _uow.Repository<AccessGroup>().GetByIdAsync(accessGroupId);
        if (group == null) return AdminResult<bool>.Fail("Access group not found");
        var user = await _uow.Repository<BshUser>().GetByIdAsync(userId);
        if (user == null) return AdminResult<bool>.Fail("User not found");

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId))
            return AdminResult<bool>.Fail("Scope organization unit is required");
        var scopeExists = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.Id == scopeOrganizationUnitId);
        if (!scopeExists) return AdminResult<bool>.Fail("Scope organization unit not found");

        var exists = await _uow.Repository<UserAccessGroup>().AnyAsync(uag =>
            uag.AccessGroupId == accessGroupId && uag.UserId == userId
            && uag.ScopeOrganizationUnitId == scopeOrganizationUnitId
            && uag.CascadeOrgStructure == cascadeOrgStructure);
        if (exists) return AdminResult<bool>.Fail("Access group already assigned to user at this scope");

        var now = DateTime.UtcNow;
        await _uow.Repository<UserAccessGroup>().AddAsync(new UserAccessGroup
        {
            AccessGroupId = accessGroupId,
            UserId = userId,
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
        await _permissionVersionService.BumpUserAsync(userId);
        return AdminResult<bool>.Ok(true);
    }

    public async Task<AdminResult<bool>> RemoveUserAsync(string accessGroupId, string userId, string? scopeOrganizationUnitId = null)
    {
        var matches = await _uow.Repository<UserAccessGroup>().FindAsync(uag =>
            uag.AccessGroupId == accessGroupId && uag.UserId == userId
            && (scopeOrganizationUnitId == null || uag.ScopeOrganizationUnitId == scopeOrganizationUnitId));
        if (!matches.Any()) return AdminResult<bool>.Fail("Access group not assigned to user");
        _uow.Repository<UserAccessGroup>().DeleteRange(matches);
        await _uow.SaveChangesAsync();
        await _permissionVersionService.BumpUserAsync(userId);
        return AdminResult<bool>.Ok(true);
    }
}
