using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<BshUser> _userManager;
    private readonly IUserService _userService;
    private readonly IUserSessionService _userSessionService;
    private readonly IPermissionAssignmentService _permissionAssignmentService;
    private readonly IOrganizationUnitService _organizationUnitService;

    public AuthController(UserManager<BshUser> userManager, IUserService userService, IUserSessionService userSessionService, IPermissionAssignmentService permissionAssignmentService, IOrganizationUnitService organizationUnitService)
    {
        _userManager = userManager;
        _userService = userService;
        _userSessionService = userSessionService;
        _permissionAssignmentService = permissionAssignmentService;
        _organizationUnitService = organizationUnitService;
    }

    private async Task<UserInfo> MapToUserInfoAsync(BshUser user)
    {
        var codes = await _organizationUnitService.ResolveCodesAsync(user.DomicileUnitId);
        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            BankId = codes.BankId,
            BranchId = codes.BranchId,
            CountryCode = codes.CountryCode,
            IsAuthenticated = true
        };
    }

    [AllowAnonymous]
    [HttpGet("users/by-login")]
    public async Task<IActionResult> FindByExternalLogin([FromQuery] string provider, [FromQuery] string providerKey)
    {
        var user = await _userService.FindByExternalLoginAsync(provider, providerKey);
        if (user == null) return this.ApiNotFound();
        return this.ApiOk(user);
    }

    [AllowAnonymous]
    [HttpGet("users/by-email")]
    public async Task<IActionResult> FindByEmail([FromQuery] string email)
    {
        var user = await _userService.FindByEmailAsync(email);
        if (user == null) return this.ApiNotFound();
        return this.ApiOk(user);
    }

    [AllowAnonymous]
    [HttpGet("org-units/resolve")]
    public async Task<IActionResult> ResolveOrgUnit([FromQuery] string value)
    {
        var scope = await _organizationUnitService.ResolveScopeByCodeAsync(value);
        return this.ApiOk(scope);
    }

    [AllowAnonymous]
    [HttpGet("users/{userId}/domicile-scope")]
    public async Task<IActionResult> GetDomicileScope(string userId)
    {
        var scope = await _userService.GetDomicileScopeAsync(userId);
        return this.ApiOk(scope);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("users/{userId}/logins")]
    public async Task<IActionResult> LinkExternalLogin(string userId, [FromBody] LinkExternalLoginRequest request)
    {
        var linked = await _userService.LinkExternalLoginAsync(userId, request.LoginProvider, request.ProviderKey, request.ProviderDisplayName);
        if (!linked) return this.ApiBadRequest("External login link failed", null);
        return this.ApiOk("External login linked");
    }

    [AllowAnonymous]
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return this.ApiNotFound();
        return this.ApiOk(await MapToUserInfoAsync(user));
    }

    [AllowAnonymous]
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        var session = await _userSessionService.CreateAsync(request.UserId, request.RemoteIp, request.UserAgent);
        return this.ApiOk(new CreateSessionResponse
        {
            SecurityVersion = session.SecurityVersion
        });
    }

    [HttpGet("users/{userId}/roles")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return this.ApiNotFound();
        var roles = await _permissionAssignmentService.GetUserRolesAsync(userId);
        return this.ApiOk(roles);
    }

    [HttpGet("users/{userId}/permissions")]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        var permissions = await _permissionAssignmentService.GetUserPermissionsAsync(userId);
        return this.ApiOk(permissions);
    }

    [AllowAnonymous]
    [HttpGet("sessions/{securityVersion}")]
    public async Task<IActionResult> GetSession(string securityVersion)
    {
        var session = await _userSessionService.GetBySecurityVersionAsync(securityVersion);
        if (session == null) return this.ApiNotFound();
        return this.ApiOk(new
        {
            session.UserId,
            session.IsActive,
            PermissionVersion = session.User?.PermissionVersion ?? 0
        });
    }

    [HttpPost("sessions/{securityVersion}/invalidate")]
    public async Task<IActionResult> InvalidateSession(string securityVersion)
    {
        var result = await _userSessionService.InvalidateAsync(securityVersion);
        if (!result) return this.ApiNotFound();
        return this.ApiOk("Session invalidated");
    }
}