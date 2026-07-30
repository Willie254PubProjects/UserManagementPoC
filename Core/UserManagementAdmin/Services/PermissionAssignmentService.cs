using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class PermissionAssignmentService : IPermissionAssignmentService
{
    private readonly IUnitOfWork _uow;
    public PermissionAssignmentService(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    {
        var userRoles = await _uow.Repository<UserRole>().FindAsync(
            ur => ur.UserId == userId,
            q => q.Include(ur => ur.Role)
                  .ThenInclude(r => r.Permissions)
                  .ThenInclude(rp => rp.Permission)
                  .ThenInclude(p => p.Workflow)
                  .Include(ur => ur.Role)
                  .ThenInclude(r => r.Permissions)
                  .ThenInclude(rp => rp.Permission)
                  .ThenInclude(p => p.Action)
                  .Include(ur => ur.Role)
                  .ThenInclude(r => r.Permissions)
                  .ThenInclude(rp => rp.Permission)
                  .ThenInclude(p => p.Type));
        return userRoles
            .SelectMany(ur => ur.Role.Permissions)
            .Select(rp => new
            {
                WorkflowName = rp.Permission.Workflow.Name,
                ActionName = rp.Permission.Action?.Name,
                TypeName = rp.Permission.Type.Name
            })
            .Distinct()
            .Select(p => $"{p.WorkflowName}.{p.ActionName ?? "*"}.{p.TypeName}")
            .ToList();
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
        }
    }
    public async Task RemovePermissionFromRoleAsync(string roleId, string permissionId)
    {
        var rp = await _uow.Repository<RolePermission>().FirstOrDefaultAsync(r => r.RoleId == roleId && r.PermissionId == permissionId);
        if (rp != null)
        {
            _uow.Repository<RolePermission>().Delete(rp);
            await _uow.SaveChangesAsync();
        }
    }
}
