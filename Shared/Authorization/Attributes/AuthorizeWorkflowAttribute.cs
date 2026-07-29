using Microsoft.AspNetCore.Authorization;

namespace UserManagementPoC.Shared.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeWorkflowAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "WorkflowAuthorization";
    public AuthorizeWorkflowAttribute()
    {
        Policy = PolicyPrefix;
    }
}