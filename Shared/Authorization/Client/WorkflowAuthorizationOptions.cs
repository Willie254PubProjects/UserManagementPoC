namespace UserManagementPoC.Shared.Authorization.Client;

public class WorkflowAuthorizationOptions
{
    public string? Authority { get; set; }
    public string ServiceName { get; set; } = "authorization";

}