using Microsoft.AspNetCore.Mvc;
using UserManagementPoC.Shared.Authorization.Attributes;
using UserManagementPoC.Shared.Authorization.Enums;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

[ApiController]
[Route("api/sample/permissions")]
public class SamplePermissionController : ControllerBase
{
    [HttpGet("any-of")]
    [AuthorizeAnyPermission("Loan.Create.Invoke", "AccountOpening.Approve.Approve")]
    public IActionResult AnyOf()
    {
        return this.ApiOk(new
        {
            Message = "User has at least one of the permissions: Loan.Create.Invoke, AccountOpening.Approve.Approve",
            User = User.Identity?.Name
        });
    }

    [HttpGet("all-of")]
    [AuthorizeAllPermissions("Loan.Create.Invoke", "Loan.View.Create")]
    public IActionResult AllOf()
    {
        return this.ApiOk(new
        {
            Message = "User has all of the permissions: Loan.Create.Invoke, Loan.View.Create",
            User = User.Identity?.Name
        });
    }

    [HttpGet("custom-policy")]
    [AuthRequirement(AuthPolicyType.Permission, AuthOperator.Or, "Loan.Create.Invoke")]
    public IActionResult CustomPolicy()
    {
        return this.ApiOk(new
        {
            Message = "Authorized via the base AuthRequirementAttribute",
            User = User.Identity?.Name
        });
    }

    [HttpGet("combined")]
    [AuthorizeAnyRole("Administrator")]
    [AuthorizeAnyPermission("Loan.Create.Invoke")]
    public IActionResult Combined()
    {
        return this.ApiOk(new
        {
            Message = "User satisfies both the role and permission requirements",
            User = User.Identity?.Name
        });
    }
}
