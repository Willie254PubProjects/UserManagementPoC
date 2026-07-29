using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Contracts;

public interface IWorkflowContextResolver
{
    Task<WorkflowContext> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default);

}