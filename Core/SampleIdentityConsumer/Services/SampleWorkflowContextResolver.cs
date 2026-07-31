using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.SampleIdentityConsumer.Services;

public class SampleWorkflowContextResolver : IWorkflowContextResolver
{
    public Task<WorkflowContext> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var routeData = httpContext.GetRouteData();
        var workflow = routeData.Values["workflow"]?.ToString() ?? "Unknown";
        var action = routeData.Values["action"]?.ToString() ?? "Unknown";
        var actionStep = routeData.Values["actionStep"]?.ToString();

        var wf = ResolveWorkflow(workflow, action);

        return Task.FromResult(new WorkflowContext
        {
            WorkflowName = workflow,
            Action = action,
            ActionStep = actionStep ?? action,
            RequiredPermissions = wf.permissions,
            RequiredRoles = wf.roles
        });
    }

    private static (IEnumerable<string> permissions, IEnumerable<string> roles) ResolveWorkflow(string workflow, string action)
    {
        // Client app owns this mapping.
        // In production this could be a DB lookup, config, or service call.
        return (workflow, action) switch
        {
            ("Loan", "Create") => (["Loan.Create.Invoke"], []),
            ("Loan", "View") => (["Loan.View.*"], []),
            ("Loan", "Edit") => (["Loan.Edit.*"], []),
            ("Loan", "Approve") => (["Loan.Approve.*"], []),
            _ => ([], [])
        };
    }
}