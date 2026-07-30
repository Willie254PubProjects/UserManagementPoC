using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services.Interfaces;

public interface IWorkflowAdministrationService
{
    Task<List<WorkflowType>> GetWorkflowTypesAsync();
    Task<WorkflowType> CreateWorkflowTypeAsync(string name, string description);
    Task<WorkflowAction> CreateWorkflowActionAsync(string workflowId, string name, string description);
    Task<List<PermissionType>> GetPermissionTypesAsync();
    Task<PermissionType> CreatePermissionTypeAsync(string name, string description);
    Task<List<Permission>> GetPermissionsAsync();
}
