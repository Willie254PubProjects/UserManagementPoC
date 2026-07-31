using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Models;



public class AuthorizationContext
{
    public string UserId { get; set; }
    public WorkflowContext? Workflow { get; set; }
    public IEnumerable<string> Permissions { get; set; } = [];
    public IEnumerable<string> Roles { get; set; } = [];
    public AuthOperator? Operator{ get; set; }
    public string? BankId { get; set; }
    public string? BranchId { get; set; }
}