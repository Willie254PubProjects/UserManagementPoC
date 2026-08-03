using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Identity.Services;

using UserManagementPoC.Shared.Extensions;

using UserManagementPoC.Shared.Security.Contracts;

using UserManagementPoC.Shared.Security.Models;

using Microsoft.AspNetCore.RateLimiting;

namespace UserManagementPoC.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserAuthenticator _authenticator;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenValidator _tokenValidator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IUserManagementApiClient _userManagementClient;
    public AuthController(IUserAuthenticator authenticator, 
        ITokenGenerator tokenGenerator, 
        ITokenValidator tokenValidator, 
        RefreshTokenService refreshTokenService, 
        IUserManagementApiClient userManagementClient)
    {
        _authenticator = authenticator;
        _tokenGenerator = tokenGenerator;
        _tokenValidator = tokenValidator;
        _refreshTokenService = refreshTokenService;
        _userManagementClient = userManagementClient;

    }
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authenticator.LoginAsync(request);
        if (string.IsNullOrEmpty(response.AccessToken))
        {
            return this.ApiUnauthorized("Invalid username or password");

        }
        return this.ApiOk(response);

    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var stored = await _refreshTokenService.ValidateAsync(request.RefreshToken);
        if (stored == null)
        {
            return this.ApiUnauthorized("Invalid or expired refresh token");

        }
        var parts = stored.Split('|', 2);
        var userId = parts[0];
        var securityVersion = parts.Length > 1 ? parts[1] : string.Empty;
        if (string.IsNullOrEmpty(securityVersion))
        {
            return this.ApiUnauthorized("Invalid or expired refresh token");

        }
        var session = await _userManagementClient.GetSessionAsync(securityVersion);
        if (session == null || !session.IsActive
            || !string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return this.ApiUnauthorized("Session invalid, expired, or logged out");

        }
        var user = await _userManagementClient.GetUserByIdAsync(userId);
        if (user == null)
        {
            return this.ApiUnauthorized("User not found");

        }
        var tokenResponse = await _tokenGenerator.GenerateTokenAsync(user, securityVersion);
        return this.ApiOk(tokenResponse);

    }
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var securityVersion = User.FindFirst("security_version")?.Value;
        if (string.IsNullOrEmpty(securityVersion))
        {
            return this.ApiUnauthorized("No active session");

        }
        await _userManagementClient.InvalidateSessionAsync(securityVersion);
        return this.ApiOk("Logged out successfully");

    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return this.ApiUnauthorized("User not authenticated");

        }
        var user = await _userManagementClient.GetUserByIdAsync(userId);
        if (user == null)
        {
            return this.ApiNotFound("User not found");

        }
        return this.ApiOk(user);
    }
}