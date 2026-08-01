using Microsoft.AspNetCore.Mvc;
using UserManagementPoC.Shared.Extensions;
using UserManagementPoC.Shared.Authorization.Attributes;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

[ApiController]
[Route("api/sample")]
public class SampleWorkflowController : ControllerBase
{
    [HttpGet]
    public IActionResult Info()
    {
        return this.ApiOk(new
        {
            Message = "SampleIdentityConsumer is running",
            Endpoints = new[]
            {
                "GET /api/sample",
                "GET /api/sample/{workflow}/{action}                 [AuthorizeWorkflow]",
                "GET /api/sample/permission-check                   [AuthorizeAllPermissions]",
                "GET /api/sample/admin-only                          [AuthorizeAnyRole]",
                "GET /api/sample/roles/any-of                        [AuthorizeAnyRole]",
                "GET /api/sample/roles/all-of                        [AuthorizeAllRoles]",
                "GET /api/sample/permissions/any-of                  [AuthorizeAnyPermission]",
                "GET /api/sample/permissions/all-of                  [AuthorizeAllPermissions]",
                "GET /api/sample/permissions/custom-policy           [AuthRequirement]",
                "GET /api/sample/permissions/combined                [AuthorizeAnyRole + AuthorizeAnyPermission]"
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
    [AuthorizeAnyRole("Administrator")]
    public IActionResult AdminOnly()
    {
        return this.ApiOk(new
        {
            Message = "User is an Administrator",
            User = User.Identity?.Name
        });
    }
}
