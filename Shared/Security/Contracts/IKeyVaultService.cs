namespace UserManagementPoC.Shared.Security.Contracts;

public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);

}