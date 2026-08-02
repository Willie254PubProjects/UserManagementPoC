using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Abstractions;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.DTOs;

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

        var resourceBank = context.BankId;
        var resourceBranch = context.BranchId;
        var userBank = httpContext.User.FindFirstValue("bank_id");

        if (!string.IsNullOrEmpty(resourceBank) && !string.IsNullOrEmpty(userBank)
            && !string.Equals(resourceBank, userBank, StringComparison.OrdinalIgnoreCase))
        {
            return AuthorizationResult.Denied("Resource is outside the user's subsidiary");
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
            var matches = requiredRoles.Count(r => userRoles.Any(ur =>
                string.Equals(ur.Code, r, StringComparison.OrdinalIgnoreCase) && ScopeCovers(ur.Scope, resourceBranch)));
            var passed = context.Roles.Any() && context.Workflow?.RequiredRoles.Any() != true
                ? context.Operator == AuthOperator.Or ? matches > 0 : matches == requiredRoles.Count
                : matches > 0;

            if (!passed)
                return AuthorizationResult.Denied("User lacks required roles for the resource scope");

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
            var matches = requiredPermissions.Count(req => userPermissions.Any(p =>
                string.Equals(p.Code, req, StringComparison.OrdinalIgnoreCase) && ScopeCovers(p.Scope, resourceBranch)));
            var effectiveOperator = context.Workflow != null && !context.Permissions.Any()
                ? AuthOperator.Or
                : context.Operator ?? AuthOperator.And;
            var passed = effectiveOperator == AuthOperator.Or
                ? matches > 0
                : matches == requiredPermissions.Count;

            if (!passed)
                return AuthorizationResult.Denied("User lacks required permissions for the resource scope");

        }

        return AuthorizationResult.Allowed();
    }

    private static bool ScopeCovers(string[] scope, string? resourceBranch)
    {
        if (string.IsNullOrEmpty(resourceBranch)) return true;
        if (scope == null || scope.Length == 0) return false;
        return scope.Contains(resourceBranch, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<RoleDto[]> GetCachedRolesAsync(string userId, string securityVersion, CancellationToken cancellationToken)
    {
        var cacheKey = $"roles:{userId}:{securityVersion}";
        var cached = await _cache.GetAsync<RoleDto[]>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var roles = await _userManagementClient.GetUserRolesAsync(userId, cancellationToken);
        await _cache.SetAsync(cacheKey, roles, PermissionCacheTtl, cancellationToken);
        return roles;
    }

    private async Task<PermissionDto[]> GetCachedPermissionsAsync(string userId, string securityVersion, CancellationToken cancellationToken)
    {
        var cacheKey = $"permissions:{userId}:{securityVersion}";
        var cached = await _cache.GetAsync<PermissionDto[]>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var permissions = await _userManagementClient.GetUserPermissionsAsync(userId, cancellationToken);
        await _cache.SetAsync(cacheKey, permissions, PermissionCacheTtl, cancellationToken);
        return permissions;
    }
}
