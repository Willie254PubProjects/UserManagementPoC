using Microsoft.EntityFrameworkCore;

using UserManagementAdmin.Models.Entities;

using UserManagementAdmin.Persistence;

namespace UserManagementAdmin.Services;

public class PermissionAssignmentService
{
    private readonly AdminDbContext _context;
    public PermissionAssignmentService(AdminDbContext context)
    {
        _context = context;

    }
    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        return await _context.Set<UserRole>()
                             .Where(ur => ur.UserId == userId)
                             .SelectMany(ur => ur.Role.Permissions)
                             .Select(rp => new
                             {
                                 WorkflowName = rp.Permission.Workflow.Name,
                                 ActionName = rp.Permission.Action != null ? rp.Permission.Action.Name : null,
                                 TypeName = rp.Permission.Type.Name
                             })
                            .Distinct()
                            .Select(p => p.WorkflowName + "." + (p.ActionName ?? "*") + "." + p.TypeName)
                            .ToListAsync();

    }
    public async Task AssignPermissionToRoleAsync(string roleId, string permissionId)
    {
        if (!await _context.Set<RolePermission>().AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
        {
            _context.Set<RolePermission>().Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
            await _context.SaveChangesAsync();

        }
    }
    public async Task RemovePermissionFromRoleAsync(string roleId, string permissionId)
    {
        var rp = await _context.Set<RolePermission>()
                               .FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId);
        if (rp != null)
        {
            _context.Set<RolePermission>().Remove(rp);

            await _context.SaveChangesAsync();
        }
    }
}