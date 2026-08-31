using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Authorization.Sso;

using UserManagementPoC.Shared.Extensions;

namespace UserManagementPoC.SampleIdentityConsumer.Controllers;

[ApiController]
[Route("api/sample")]
public class SampleMeController : ControllerBase
{
    private readonly IdentitySsoClient _identitySsoClient;
    public SampleMeController(IdentitySsoClient identitySsoClient)
    {
        _identitySsoClient = identitySsoClient;
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _identitySsoClient.GetMeAsync();
        if (user == null)
        {
            return this.ApiUnauthorized("Could not resolve the current user");

        }
        return this.ApiOk(user);
    }
}