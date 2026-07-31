using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Abstractions;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Enums;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Identity.Services;

public class AuthorizationService : IAuthorizationEvaluator
{
    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheService _cache;
    private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(3);

    public AuthorizationService(IUserManagementApiClient userManagementClient, IHttpContextAccessor httpContextAccessor, ICacheService cache)
    {
        _userManagementClient = userManagementClient;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;

    }
    public async Task<AuthorizationResult> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) 
            return AuthorizationResult.Denied("No HTTP context");

        var tokenSecurityVersion = httpContext.User.FindFirst("security_version")?.Value;
        if (string.IsNullOrEmpty(tokenSecurityVersion)) 
            return AuthorizationResult.Denied("No security version in token");

        var session = await _userManagementClient.GetSessionAsync(tokenSecurityVersion, cancellationToken);

        if (session == null || !session.IsActive) 
            return AuthorizationResult.Denied("Session invalid, expired, or logged out");

        if (!context.Roles.Any() && !context.Permissions.Any() 
            && context.Workflow == null)
        {
            return AuthorizationResult.Denied("No authorization requirements specified");
        }

        if (context.Roles.Any() || context.Workflow?.RequiredRoles.Any() == true)
        {
            var userRoles = await GetCachedRolesAsync(context.UserId, tokenSecurityVersion, cancellationToken);
            var requiredRoles = new List<string>();
            requiredRoles.AddRange(context.Roles);
            if (context.Workflow?.RequiredRoles.Any() == true)
            {
                requiredRoles.AddRange(context.Workflow.RequiredRoles);
            }
            var matches = requiredRoles.Count(r => userRoles.Any(ur => string.Equals(ur, r, StringComparison.OrdinalIgnoreCase)));
            var passed = context.Roles.Any() && context.Workflow?.RequiredRoles.Any() != true
                ? context.Operator == AuthOperator.Or ? matches > 0 : matches == requiredRoles.Count
                : matches > 0;

            if (!passed) 
                return AuthorizationResult.Denied("User lacks required roles");

        }

        if (context.Permissions.Any() || context.Workflow != null)
        {
            var userPermissions = await GetCachedPermissionsAsync(context.UserId, tokenSecurityVersion, cancellationToken);
            var requiredPermissions = new List<string>();
            if (context.Permissions.Any())
            {
                requiredPermissions.AddRange(context.Permissions);

            }
            if (context.Workflow?.RequiredPermissions.Any() == true)
            {
                requiredPermissions.AddRange(context.Workflow.RequiredPermissions);

            }
            var matches = requiredPermissions.Count(req => userPermissions.Contains(req, StringComparer.OrdinalIgnoreCase));
            var effectiveOperator = context.Workflow != null && !context.Permissions.Any()
                ? AuthOperator.Or
                : context.Operator ?? AuthOperator.And;
            var passed = effectiveOperator == AuthOperator.Or
                ? matches > 0
                : matches == requiredPermissions.Count;

            if (!passed) 
                return AuthorizationResult.Denied("User lacks required permissions");

        }

        return AuthorizationResult.Allowed();
    }

    private async Task<IReadOnlySet<string>> GetCachedRolesAsync(string userId, string securityVersion, CancellationToken cancellationToken)
    {
        var cacheKey = $"roles:{userId}:{securityVersion}";
        var cached = await _cache.GetAsync<IReadOnlySet<string>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var roles = await _userManagementClient.GetUserRolesAsync(userId, cancellationToken);
        await _cache.SetAsync(cacheKey, roles, PermissionCacheTtl, cancellationToken);
        return roles;
    }

    private async Task<IReadOnlySet<string>> GetCachedPermissionsAsync(string userId, string securityVersion, CancellationToken cancellationToken)
    {
        var cacheKey = $"permissions:{userId}:{securityVersion}";
        var cached = await _cache.GetAsync<IReadOnlySet<string>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var permissions = await _userManagementClient.GetUserPermissionsAsync(userId, cancellationToken);
        await _cache.SetAsync(cacheKey, permissions, PermissionCacheTtl, cancellationToken);
        return permissions;
    }}