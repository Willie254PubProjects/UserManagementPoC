using Microsoft.AspNetCore.Authorization;

using Microsoft.Extensions.Options;

using UserManagementPoC.Shared.Authorization.Attributes;

using UserManagementPoC.Shared.Authorization.Enums;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Client;

internal class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);

    }
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName == AuthorizeWorkflowAttribute.PolicyPrefix)
        {
            var policy = new AuthorizationPolicyBuilder().AddRequirements(new WorkflowAuthorizationRequirement()).Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);

        }
        if (TryParseAuthRequirementPolicy(policyName, out var requirement))
        {
            var policy = new AuthorizationPolicyBuilder().AddRequirements(requirement).Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);

        }
        return _fallback.GetPolicyAsync(policyName);

    }
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
    private static bool TryParseAuthRequirementPolicy(string policyName, out WorkflowAuthorizationRequirement requirement)
    {
        requirement = null!;
        var segments = policyName.Split('|', 3);
        if (segments.Length != 3) return false;
        if (!Enum.TryParse<AuthPolicyType>(segments[0], out var policyType)) return false;
        if (!Enum.TryParse<AuthOperator>(segments[1], out var authOperator)) return false;
        var items = segments[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (items.Length == 0) return false;
        requirement = new WorkflowAuthorizationRequirement
        {
            Items = items,
            PolicyType = policyType,
            Operator = authOperator
        };
        return true;

    }
}