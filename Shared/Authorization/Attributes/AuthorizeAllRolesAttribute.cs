using System;

using System.Collections.Generic;

using System.Text;

using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Attributes;

public class AuthorizeAllRolesAttribute : AuthRequirementAttribute
{
    public AuthorizeAllRolesAttribute(params string[] roles) : base(AuthPolicyType.Role, AuthOperator.And, roles)
    {
    }
}