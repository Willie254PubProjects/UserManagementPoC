using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Enums;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Identity.Services;

public class AuthorizationService : IAuthorizationEvaluator
{
    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthorizationService(IUserManagementApiClient userManagementClient, IHttpContextAccessor httpContextAccessor)
    {
        _userManagementClient = userManagementClient;
        _httpContextAccessor = httpContextAccessor;

    }
    public async Task<AuthorizationResult> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return AuthorizationResult.Denied("No HTTP context");
        var tokenSecurityVersion = httpContext.User.FindFirst("security_version")?.Value;
        if (string.IsNullOrEmpty(tokenSecurityVersion)) return AuthorizationResult.Denied("No security version in token");
        var session = await _userManagementClient.GetSessionAsync(tokenSecurityVersion, cancellationToken);
        if (session == null || !session.IsActive) return AuthorizationResult.Denied("Session invalid, expired, or logged out");
        if (context.Roles.Any())
        {
            var userRoles = await _userManagementClient.GetUserRolesAsync(context.UserId, cancellationToken);
            var matches = context.Roles.Count(r => userRoles.Contains(r));
            var passed = context.Operator == AuthOperator.Or ? matches > 0 : matches == context.Roles.Count();
            if (!passed) return AuthorizationResult.Denied("User lacks required roles");

        }
        if (context.Permissions.Any() || context.Workflow != null)
        {
            var userPermissions = await _userManagementClient.GetUserPermissionsAsync(context.UserId, cancellationToken);
            var requiredPermissions = new List<string>();
            if (context.Permissions.Any())
            {
                requiredPermissions.AddRange(context.Permissions);

            }
            if (context.Workflow != null)
            {
                var workflowPermission = $"{context.Workflow.WorkflowName}.{context.Workflow.Action}.*";
                requiredPermissions.Add(workflowPermission);

            }
            var matches = requiredPermissions.Count(p => userPermissions.Contains(p));
            var passed = context.Operator == AuthOperator.Or ? matches > 0 : matches == requiredPermissions.Count;
            if (!passed) return AuthorizationResult.Denied("User lacks required permissions");

        }
        if (!context.Roles.Any() && !context.Permissions.Any() && context.Workflow == null)
        {
            return AuthorizationResult.Denied("No authorization requirements specified");

        }

        return AuthorizationResult.Allowed();
    }
}