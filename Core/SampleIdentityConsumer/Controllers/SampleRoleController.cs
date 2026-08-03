using Microsoft.AspNetCore.Mvc;
using UserManagementPoC.Shared.Authorization.Attributes;
using UserManagementPoC.Shared.Authorization.Constants;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

[ApiController]
[Route("api/sample/roles")]
public class SampleRoleController : ControllerBase
{
    [HttpGet("any-of")]
    [AuthorizeAnyRole(BshRoles.Administrator, BshRoles.Manager)]
    public IActionResult AnyOf()
    {
        return this.ApiOk(new
        {
            Message = "User has at least one of the roles: Administrator, Manager",
            User = User.Identity?.Name
        });
    }

    [HttpGet("all-of")]
    [AuthorizeAllRoles(BshRoles.Administrator)]
    public IActionResult AllOf()
    {
        return this.ApiOk(new
        {
            Message = "User has all of the roles: Administrator",
            User = User.Identity?.Name
        });
    }
}
