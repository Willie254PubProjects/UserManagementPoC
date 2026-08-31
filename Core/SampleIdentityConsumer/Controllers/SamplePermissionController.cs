using Microsoft.AspNetCore.Mvc;
using UserManagementPoC.Shared.Authorization.Attributes;
using UserManagementPoC.Shared.Authorization.Constants;
using UserManagementPoC.Shared.Authorization.Enums;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

[ApiController]
[Route("api/sample/permissions")]
public class SamplePermissionController : ControllerBase
{
    [HttpGet("any-of")]
    [AuthorizeAnyPermission(Permissions.CardPrinting.Create, Permissions.CardPrinting.Approve)]
    public IActionResult AnyOf()
    {
        return this.ApiOk(new
        {
            Message = "User has at least one of the permissions: CardPrinting.Create, CardPrinting.Approve",
            User = User.Identity?.Name
        });
    }

    [HttpGet("all-of")]
    [AuthorizeAllPermissions(Permissions.CardPrinting.Create, Permissions.CardRequest.View)]
    public IActionResult AllOf()
    {
        return this.ApiOk(new
        {
            Message = "User has all of the permissions: CardPrinting.Create, CardRequest.View",
            User = User.Identity?.Name
        });
    }

    [HttpGet("custom-policy")]
    [AuthRequirement(AuthPolicyType.Permission, AuthOperator.Or, Permissions.CardPrinting.Create)]
    public IActionResult CustomPolicy()
    {
        return this.ApiOk(new
        {
            Message = "Authorized via the base AuthRequirementAttribute",
            User = User.Identity?.Name
        });
    }

    [HttpGet("combined")]
    [AuthorizeAnyRole(BshRoles.Administrator)]
    [AuthorizeAnyPermission(Permissions.CardPrinting.Create)]
    public IActionResult Combined()
    {
        return this.ApiOk(new
        {
            Message = "User satisfies both the role and permission requirements",
            User = User.Identity?.Name
        });
    }
}
