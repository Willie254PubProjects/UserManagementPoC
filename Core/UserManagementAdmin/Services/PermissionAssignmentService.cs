using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Authorization.DTOs;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class PermissionAssignmentService : IPermissionAssignmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrganizationUnitService _organizationUnitService;
    private readonly IPermissionVersionService _permissionVersionService;
    public PermissionAssignmentService(IUnitOfWork uow, IOrganizationUnitService organizationUnitService, IPermissionVersionService permissionVersionService)
    {
        _uow = uow;
        _organizationUnitService = organizationUnitService;
        _permissionVersionService = permissionVersionService;
    }

    public async Task<RoleDto[]> GetUserRolesAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var assignments = await _uow.Repository<UserRole>().FindAsync(
            ur => ur.UserId == userId
                && ur.Status == AssignmentStatus.Active
                && ur.StartDate <= now
                && (ur.EndDate == null || ur.EndDate >= now),
            q => q.AsNoTracking().Include(ur => ur.Role));

        var roles = new Dictionary<string, List<IReadOnlySet<string>>>(StringComparer.OrdinalIgnoreCase);
        var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in assignments)
        {
            if (assignment.Role == null) continue;
            var code = assignment.Role.Name;
            var scope = await _organizationUnitService.ResolveScopeAsync(assignment.ScopeOrganizationUnitId, assignment.CascadeOrgStructure);
            if (!roles.TryGetValue(code, out var scopes))
            {
                scopes = new List<IReadOnlySet<string>>();
                roles[code] = scopes;
                descriptions[code] = assignment.Role.Description ?? "";
            }
            scopes.Add(scope);
        }

        return roles.Select(r => new RoleDto
        {
            Code = r.Key,
            Description = descriptions[r.Key],
            Scope = IntersectScopes(r.Value)
        }).ToArray();
    }

    public async Task<PermissionDto[]> GetUserPermissionsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var permissions = new Dictionary<string, (string Description, List<IReadOnlySet<string>> Scopes)>(StringComparer.OrdinalIgnoreCase);

        var roleAssignments = await _uow.Repository<UserRole>().FindAsync(
            ur => ur.UserId == userId
                && ur.Status == AssignmentStatus.Active
                && ur.StartDate <= now
                && (ur.EndDate == null || ur.EndDate >= now),
            q => q.AsNoTracking().Include(ur => ur.Role)
                  .ThenInclude(r => r.Permissions)
                  .ThenInclude(rp => rp.Permission)
                  .ThenInclude(p => p.SubPermission)
                  .Include(ur => ur.Role)
                  .ThenInclude(r => r.Permissions)
                  .ThenInclude(rp => rp.Permission)
                  .ThenInclude(p => p.Type));
        foreach (var assignment in roleAssignments)
        {
            var scope = await _organizationUnitService.ResolveScopeAsync(assignment.ScopeOrganizationUnitId, assignment.CascadeOrgStructure);
            foreach (var rp in assignment.Role?.Permissions ?? Enumerable.Empty<RolePermission>())
            {
                AddPermission(permissions, rp.Permission, scope);
            }
        }

        var accessGroupAssignments = await _uow.Repository<UserAccessGroup>().FindAsync(
            uag => uag.UserId == userId
                && uag.Status == AssignmentStatus.Active
                && uag.StartDate <= now
                && (uag.EndDate == null || uag.EndDate >= now),
            q => q.AsNoTracking().Include(uag => uag.AccessGroup)
                  .ThenInclude(ag => ag.Permissions)
                  .ThenInclude(agp => agp.Permission)
                  .ThenInclude(p => p.SubPermission)
                  .Include(uag => uag.AccessGroup)
                  .ThenInclude(ag => ag.Permissions)
                  .ThenInclude(agp => agp.Permission)
                  .ThenInclude(p => p.Type));
        foreach (var assignment in accessGroupAssignments)
        {
            var scope = await _organizationUnitService.ResolveScopeAsync(assignment.ScopeOrganizationUnitId, assignment.CascadeOrgStructure);
            foreach (var agp in assignment.AccessGroup?.Permissions ?? Enumerable.Empty<AccessGroupPermission>())
            {
                AddPermission(permissions, agp.Permission, scope);
            }
        }

        var directAssignments = await _uow.Repository<UserPermission>().FindAsync(
            up => up.UserId == userId
                && up.Status == AssignmentStatus.Active
                && up.StartDate <= now
                && (up.EndDate == null || up.EndDate >= now),
            q => q.AsNoTracking().Include(up => up.Permission)
                  .ThenInclude(p => p.SubPermission)
                  .Include(up => up.Permission)
                  .ThenInclude(p => p.Type));
        foreach (var assignment in directAssignments)
        {
            var scope = await _organizationUnitService.ResolveScopeAsync(assignment.ScopeOrganizationUnitId, assignment.CascadeOrgStructure);
            AddPermission(permissions, assignment.Permission, scope);
        }

        return permissions.Select(p => new PermissionDto
        {
            Code = p.Key,
            Description = p.Value.Description,
            Scope = IntersectScopes(p.Value.Scopes)
        }).ToArray();
    }

    public async Task AssignPermissionToRoleAsync(string roleId, string permissionId)
    {
        var exists = await _uow.Repository<RolePermission>().AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        if (!exists)
        {
            await _uow.Repository<RolePermission>().AddAsync(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
            await _uow.SaveChangesAsync();
            await _permissionVersionService.BumpRoleUsersAsync(roleId);
        }
    }
    public async Task RemovePermissionFromRoleAsync(string roleId, string permissionId)
    {
        var rp = await _uow.Repository<RolePermission>().FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId);
        if (rp != null)
        {
            _uow.Repository<RolePermission>().Delete(rp);
            await _uow.SaveChangesAsync();
            await _permissionVersionService.BumpRoleUsersAsync(roleId);
        }
    }

    private static void AddPermission(Dictionary<string, (string Description, List<IReadOnlySet<string>> Scopes)> permissions, Permission permission, IReadOnlySet<string> scope)
    {
        if (permission == null) return;
        var code = permission.Code;
        if (!permissions.TryGetValue(code, out var entry))
        {
            entry = (permission.Description ?? "", new List<IReadOnlySet<string>>());
            permissions[code] = entry;
        }
        entry.Scopes.Add(scope);
    }

    private static string[] IntersectScopes(IEnumerable<IReadOnlySet<string>> scopes)
    {
        IEnumerable<string>? result = null;
        foreach (var scope in scopes)
        {
            result = result == null ? scope : result.Intersect(scope);
        }
        return (result ?? Enumerable.Empty<string>()).ToArray();
    }
}
