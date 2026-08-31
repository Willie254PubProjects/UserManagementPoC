using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Abstractions;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Enums;

using UserManagementPoC.Shared.Authorization.Models;

using UserManagementPoC.Shared.Extensions;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

using AuthEvaluator = UserManagementPoC.Shared.Authorization.Contracts.IAuthorizationEvaluator;

[ApiController]
[Route("api/sample")]
public class SampleDynamicController : ControllerBase
{
    private readonly AuthEvaluator _evaluator;
    private readonly ICurrentUser _currentUser;
    public SampleDynamicController(AuthEvaluator evaluator, ICurrentUser currentUser)
    {
        _evaluator = evaluator;
        _currentUser = currentUser;
    }
    [HttpGet("roles/dynamic")]
    [Authorize]
    public async Task<IActionResult> RolesDynamic(
        [FromQuery] string @operator = "any",
        [FromQuery] string[]? role = null,
        [FromQuery] string? bank = null,
        [FromQuery] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _evaluator.EvaluateAsync(new AuthorizationContext
        {
            UserId = _currentUser.Id!,
            Operator = string.Equals(@operator, "all", StringComparison.OrdinalIgnoreCase) ? AuthOperator.And : AuthOperator.Or,
            Roles = role ?? [],
            BankId = bank ?? _currentUser.BankId,
            BranchId = branch ?? _currentUser.BranchId
        }, cancellationToken);
        return this.DynamicResult(result);
    }
    [HttpGet("permissions/dynamic")]
    [Authorize]
    public async Task<IActionResult> PermissionsDynamic(
        [FromQuery] string @operator = "any",
        [FromQuery] string[]? permission = null,
        [FromQuery] string? bank = null,
        [FromQuery] string? branch = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _evaluator.EvaluateAsync(new AuthorizationContext
        {
            UserId = _currentUser.Id!,
            Operator = string.Equals(@operator, "all", StringComparison.OrdinalIgnoreCase) ? AuthOperator.And : AuthOperator.Or,
            Permissions = permission ?? [],
            BankId = bank ?? _currentUser.BankId,
            BranchId = branch ?? _currentUser.BranchId
        }, cancellationToken);
        return this.DynamicResult(result);
    }
    private IActionResult DynamicResult(UserManagementPoC.Shared.Authorization.Models.AuthorizationResult result)
    {
        if (!result.IsAllowed)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                UserManagementPoC.Shared.Responses.ApiResponse<UserManagementPoC.Shared.Authorization.Models.AuthorizationResult>.Success("Denied", result));
        }
        return this.ApiOk(result);
    }
}