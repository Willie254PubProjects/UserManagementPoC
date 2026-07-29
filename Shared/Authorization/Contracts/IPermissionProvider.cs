namespace UserManagementPoC.Shared.Authorization.Contracts;

public interface IPermissionProvider
{
    Task<IEnumerable<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken = default);

}