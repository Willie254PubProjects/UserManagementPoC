using Microsoft.AspNetCore.Http;

namespace UserManagementPoC.Shared.Authorization.Contracts;

public record ResourceScope(string? BankId, string? BranchId);

public interface IResourceScopeResolver
{
    Task<ResourceScope?> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}