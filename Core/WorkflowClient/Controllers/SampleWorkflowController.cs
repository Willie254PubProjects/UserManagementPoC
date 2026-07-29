using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementPoC.Shared.Extensions;
using UserManagementPoC.Shared.Authorization.Attributes;

namespace UserManagementPoC.WorkflowClient.Controllers;

[ApiController]
[Route("api/sample")]
public class SampleWorkflowController : ControllerBase
{
    [HttpGet]
    public IActionResult Info()
    {
        return this.ApiOk(new
        {
            Message = "WorkflowClient is running",
            Endpoints = new[]
            {
                "GET /api/sample",
                "GET /api/sample/{workflow}/{action}  [AuthorizeWorkflow]",
                "GET /api/sample/permission-check    [AuthorizeAllPermissions]",
                "GET /api/sample/admin-only           [Authorize(Roles)]"
            }
        });
    }

    [HttpGet("{workflow}/{action}")]
    [AuthorizeWorkflow]
    public IActionResult ExecuteWorkflow(string workflow, string action)
    {
        return this.ApiOk(new
        {
            Message = $"Workflow '{workflow}' action '{action}' authorized",
            User = User.Identity?.Name
        });
    }

    [HttpGet("permission-check")]
    [AuthorizeAllPermissions("Loan.Create.Invoke")]
    public IActionResult PermissionCheck()
    {
        return this.ApiOk(new
        {
            Message = "User has the 'Loan.Create.Invoke' permission",
            User = User.Identity?.Name
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Administrator")]
    public IActionResult AdminOnly()
    {
        return this.ApiOk(new
        {
            Message = "User is an Administrator",
            User = User.Identity?.Name
        });
    }
}
