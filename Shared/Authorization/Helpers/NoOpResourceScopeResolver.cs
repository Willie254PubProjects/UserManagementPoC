using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Authorization.Contracts;

namespace UserManagementPoC.Shared.Authorization.Helpers;

internal class NoOpResourceScopeResolver : IResourceScopeResolver
{
    public Task<ResourceScope?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ResourceScope?>(default);
    }
}