using System;

using System.Collections.Generic;

using System.Text;

using UserManagementPoC.Shared.Authorization.Enums;

namespace UserManagementPoC.Shared.Authorization.Models;

public static class AuthPolicyName
{
    public static string Create(AuthPolicyType authType, AuthOperator authOperator, params string[] items)
    {
        return $"{authType}|{authOperator}|{string.Join(",", items)}";
    }
}