using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using UserManagementPoC.Identity.Models;

using UserManagementPoC.Identity.Services;

using UserManagementPoC.Shared.Extensions;

using UserManagementPoC.Shared.Security.Contracts;

namespace UserManagementPoC.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class TokenController : ControllerBase
{
    private readonly AuthorizationCodeService _authorizationCodeService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IConfiguration _configuration;
    public TokenController(AuthorizationCodeService authorizationCodeService, ITokenGenerator tokenGenerator, IUserManagementApiClient userManagementClient, IConfiguration configuration)
    {
        _authorizationCodeService = authorizationCodeService;
        _tokenGenerator = tokenGenerator;
        _userManagementClient = userManagementClient;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        var client = _configuration.GetSection("ApiClients").Get<ApiClientConfig[]>()?
            .FirstOrDefault(c => string.Equals(c.ClientId, request.ClientId, StringComparison.OrdinalIgnoreCase));
        if (client == null || !SecretsEqual(client.ClientSecret, request.ClientSecret))
        {
            return this.ApiUnauthorized("Invalid client credentials");

        }
        var authCode = await _authorizationCodeService.ConsumeAsync(request.Code);
        if (authCode == null)
        {
            return this.ApiUnauthorized("Invalid or expired authorization code");

        }
        if (!string.Equals(authCode.ClientId, request.ClientId, StringComparison.OrdinalIgnoreCase))
        {
            return this.ApiUnauthorized("Authorization code is not valid for this client");

        }
        var user = await _userManagementClient.GetUserByIdAsync(authCode.UserId);
        if (user == null)
        {
            return this.ApiUnauthorized("User not found");

        }
        var tokenResponse = await _tokenGenerator.GenerateTokenAsync(user, authCode.SecurityVersion);
        tokenResponse.User = user;
        return this.ApiOk(tokenResponse);
    }

    private static bool SecretsEqual(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}