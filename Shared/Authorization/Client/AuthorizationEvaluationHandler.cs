using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using UserManagementPoC.Shared.Abstractions;
using UserManagementPoC.Shared.Authorization.Attributes;
using UserManagementPoC.Shared.Authorization.Contracts;
using UserManagementPoC.Shared.Authorization.Enums;
using UserManagementPoC.Shared.Authorization.Models;
namespace UserManagementPoC.Shared.Authorization.Client;

using AuthorizationEvaluator = Contracts.IAuthorizationEvaluator;
internal class AuthorizationEvaluationHandler : AuthorizationHandler<IdentityAuthorizationRequirement>
{
    private readonly AuthorizationEvaluator _evaluator;
    private readonly IWorkflowContextResolver _workflowContextResolver;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthorizationEvaluationHandler(AuthorizationEvaluator evaluator, IWorkflowContextResolver workflowContextResolver, ICurrentUser currentUser, IHttpContextAccessor httpContextAccessor)
    {
        _evaluator = evaluator;
        _workflowContextResolver = workflowContextResolver;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;

    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IdentityAuthorizationRequirement requirement)
    {
        if (!_currentUser.IsAuthenticated)
        {
            context.Fail();
            return;

        }
        var authContext = new AuthorizationContext
        {
            UserId = _currentUser.Id!
        };
        if (requirement.PolicyType.HasValue)
        {
            authContext.Operator = requirement.Operator;
            if (requirement.PolicyType == AuthPolicyType.Role)
            {
                authContext.Roles = requirement.Items;
            }
            else
            {
                authContext.Permissions = requirement.Items;
            }
        }
        else
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;

            }
            authContext.Workflow = await _workflowContextResolver.ResolveAsync(httpContext);

        }
        var result = await _evaluator.EvaluateAsync(authContext);
        if (result.IsAllowed) context.Succeed(requirement); else context.Fail();

    }
}