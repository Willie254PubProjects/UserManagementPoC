using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Extensions;

using UserManagementPoC.Shared.Authorization.Models;

using Contracts = UserManagementPoC.Shared.Authorization.Contracts;

namespace UserManagementPoC.Identity.Controllers;

[Authorize]
[ApiController]
[Route("api/authorization")]
public class AuthorizationController : ControllerBase
{
    private readonly Contracts.IAuthorizationEvaluator _evaluator;
    public AuthorizationController(Contracts.IAuthorizationEvaluator evaluator)
    {
        _evaluator = evaluator;

    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] AuthorizationContext context)
    {
        var result = await _evaluator.EvaluateAsync(context);
        return this.ApiOk(result);

    }
}