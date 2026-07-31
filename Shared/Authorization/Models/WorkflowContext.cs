namespace UserManagementPoC.Shared.Authorization.Models;

public class WorkflowContext
{
    public string WorkflowName { get; set; } = string.Empty;
    public string? Action { get; set; }
    public string ActionStep { get; set; } = string.Empty; // Create | Invoke | Approve | Submit
    public IEnumerable<string> RequiredPermissions { get; set; } = [];
    public IEnumerable<string> RequiredRoles { get; set; } = [];
}