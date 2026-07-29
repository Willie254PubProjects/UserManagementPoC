using Microsoft.AspNetCore.Authorization;

using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Attributes;

public sealed class AuthorizeAnyPermissionAttribute : AuthRequirementAttribute
{
    public AuthorizeAnyPermissionAttribute(params string[] permissions) : base(AuthPolicyType.Permission, AuthOperator.Or, permissions)
    {
    }
}