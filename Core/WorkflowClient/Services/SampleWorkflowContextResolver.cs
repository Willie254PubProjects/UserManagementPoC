using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.WorkflowClient.Services;
 public class SampleWorkflowContextResolver : IWorkflowContextResolver {
 public Task<WorkflowContext> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default) {
 var routeData = httpContext.GetRouteData();
 var workflow = routeData.Values["workflow"]?.ToString() ?? "Unknown";
 var action = routeData.Values["action"]?.ToString() ?? "Unknown";
 var entityId = routeData.Values["id"]?.ToString();
 return Task.FromResult(new WorkflowContext {
 WorkflowName = workflow, Action = action, EntityId = entityId 
});
 
} 
}