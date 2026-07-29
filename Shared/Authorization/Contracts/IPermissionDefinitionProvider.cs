using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Contracts;

public interface IPermissionDefinitionProvider
{
    IEnumerable<PermissionDefinition> GetDefinitions();

}