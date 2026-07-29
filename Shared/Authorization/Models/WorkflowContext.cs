namespace UserManagementPoC.Shared.Authorization.Models;

public class WorkflowContext
{
    public string WorkflowName { get; set; }
    public string Action { get; set; }
    public string? EntityId { get; set; }
    public string? State { get; set; }
}