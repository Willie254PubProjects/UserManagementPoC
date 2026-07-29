using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Attributes;

public sealed class AuthorizeAllPermissionsAttribute : AuthRequirementAttribute
{
    public AuthorizeAllPermissionsAttribute(params string[] permissions) : base(AuthPolicyType.Permission, AuthOperator.And, permissions)
    {
    }
}