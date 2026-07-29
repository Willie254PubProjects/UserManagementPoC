using Microsoft.AspNetCore.Authorization;

using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Models;

public class WorkflowAuthorizationRequirement : IAuthorizationRequirement
{
    public string[] Items { get; init;} = [];
    public AuthPolicyType? PolicyType { get; init; }
    public AuthOperator? Operator { get; init; }
}