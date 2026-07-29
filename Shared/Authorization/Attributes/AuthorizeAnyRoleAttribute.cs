using System;

using System.Collections.Generic;

using System.Text;

using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Attributes;

public class AuthorizeAnyRoleAttribute : AuthRequirementAttribute
{
    public AuthorizeAnyRoleAttribute(params string[] roles) : base(AuthPolicyType.Role, AuthOperator.Or, roles)
    {
    }
}