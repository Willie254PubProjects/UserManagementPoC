using UserManagementPoC.Shared.Authorization.Constants;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.SampleIdentityConsumer.Services;

public class SampleWorkflowContextResolver : IWorkflowContextResolver
{
    public Task<WorkflowContext> ResolveAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var routeData = httpContext.GetRouteData();
        var workflow = routeData.Values["workflow"]?.ToString() ?? "Unknown";
        var action = routeData.Values["wfAction"]?.ToString() ?? "Unknown";
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
            ("CardPrinting", "Create") => ([Permissions.CardPrinting.Create], []),
            ("CardPrinting", "View") => ([Permissions.CardPrinting.View], []),
            ("CardPrinting", "Edit") => ([Permissions.CardPrinting.Edit], []),
            ("CardPrinting", "Approve") => ([Permissions.CardPrinting.Approve], []),
            ("CardPrinting", "Submit") => ([Permissions.CardPrinting.Submit], []),
            ("CardPrinting", "Invoke") => ([Permissions.CardPrinting.Invoke], []),
            ("CardRequest", "Create") => ([Permissions.CardRequest.Create], []),
            ("CardRequest", "View") => ([Permissions.CardRequest.View], []),
            ("CardRequest", "Edit") => ([Permissions.CardRequest.Edit], []),
            ("CardRequest", "Approve") => ([Permissions.CardRequest.Approve], []),
            ("CardRequest", "Submit") => ([Permissions.CardRequest.Submit], []),
            ("CardRequest", "Invoke") => ([Permissions.CardRequest.Invoke], []),
            _ => ([], [])
        };
    }
}
