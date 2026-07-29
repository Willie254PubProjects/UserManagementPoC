using Microsoft.AspNetCore.Authorization;

using UserManagementPoC.Shared.Authorization.Enums;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthRequirementAttribute : AuthorizeAttribute
{
    public AuthRequirementAttribute(AuthPolicyType policyType, AuthOperator authOperator, params string[] items)
    {
        Policy = AuthPolicyName.Create(policyType, authOperator, items);

    }
}