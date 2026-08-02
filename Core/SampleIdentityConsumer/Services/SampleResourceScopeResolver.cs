using UserManagementPoC.Shared.Authorization.Contracts;

namespace UserManagementPoC.SampleIdentityConsumer.Services;

public class SampleResourceScopeResolver : IResourceScopeResolver
{
    public Task<ResourceScope?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var bank = httpContext.Request.Query["bank"].FirstOrDefault();
        var branch = httpContext.Request.Query["branch"].FirstOrDefault();
        if (string.IsNullOrEmpty(bank) && string.IsNullOrEmpty(branch))
            return Task.FromResult<ResourceScope?>(default);

        return Task.FromResult<ResourceScope?>(new ResourceScope(bank, branch));
    }
}