using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class PermissionVersionService : IPermissionVersionService
{
    private readonly IUnitOfWork _uow;
    public PermissionVersionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task BumpUserAsync(string userId)
    {
        var user = await _uow.Repository<BshUser>().GetByIdAsync(userId);
        if (user == null) return;
        user.PermissionVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        user.LastUpdatedBy = "system";
        _uow.Repository<BshUser>().Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task BumpRoleUsersAsync(string roleId)
    {
        var userIds = (await _uow.Repository<UserRole>().FindAsync(r => r.RoleId == roleId))
            .Select(r => r.UserId).Distinct().ToList();
        foreach (var userId in userIds)
        {
            await BumpUserAsync(userId);
        }
    }

    public async Task BumpAccessGroupUsersAsync(string accessGroupId)
    {
        var userIds = (await _uow.Repository<UserAccessGroup>().FindAsync(uag => uag.AccessGroupId == accessGroupId))
            .Select(uag => uag.UserId).Distinct().ToList();
        foreach (var userId in userIds)
        {
            await BumpUserAsync(userId);
        }
    }
}
