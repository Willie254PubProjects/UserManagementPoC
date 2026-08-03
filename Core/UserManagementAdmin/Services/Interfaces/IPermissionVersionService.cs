namespace UserManagementAdmin.Services.Interfaces;

public interface IPermissionVersionService
{
    Task BumpUserAsync(string userId);
    Task BumpRoleUsersAsync(string roleId);
    Task BumpAccessGroupUsersAsync(string accessGroupId);
}
