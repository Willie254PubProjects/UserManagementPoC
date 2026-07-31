using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;
using UserManagementPoC.Shared.Security.Contracts;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<BshUser> _userManager;
    private readonly SignInManager<BshUser> _signInManager;
    private readonly IEncryptionService _encryptionService;
    private readonly IUserSessionService _userSessionService;
    private readonly IPermissionAssignmentService _permissionAssignmentService;

    public AuthController(UserManager<BshUser> userManager, SignInManager<BshUser> signInManager, IEncryptionService encryptionService, IUserSessionService userSessionService, IPermissionAssignmentService permissionAssignmentService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _encryptionService = encryptionService;
        _userSessionService = userSessionService;
        _permissionAssignmentService = permissionAssignmentService;
    }

    [AllowAnonymous]
    [HttpPost("verify-credentials")]
    public async Task<IActionResult> VerifyCredentials([FromBody] VerifyCredentialsRequest request)
    {
        var password = _encryptionService.Decrypt(request.EncryptedPassword, request.Iv);
        var user = await _userManager.FindByNameAsync(request.Username) ?? await _userManager.FindByEmailAsync(request.Username);
        if (user == null) return this.ApiOk(new VerifyCredentialsResponse
        {
            Success = false, ErrorMessage = "Invalid credentials"
        });
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded) return this.ApiOk(new VerifyCredentialsResponse
        {
            Success = false, ErrorMessage = "Invalid credentials"
        });
        var roles = await _userManager.GetRolesAsync(user);
        return this.ApiOk(new VerifyCredentialsResponse
        {
            Success = true,
            User = new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles
            }
        });
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return this.ApiNotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return this.ApiOk(new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles
        });
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
        var roles = await _userManager.GetRolesAsync(user);
        return this.ApiOk(roles);
    }

    [HttpGet("users/{userId}/permissions")]
    public async Task<IActionResult> GetUserPermissions(string userId)
    {
        var names = await _permissionAssignmentService.GetUserPermissionsAsync(userId);
        return this.ApiOk(names);
    }

    [HttpGet("sessions/{securityVersion}")]
    public async Task<IActionResult> GetSession(string securityVersion)
    {
        var session = await _userSessionService.GetBySecurityVersionAsync(securityVersion);
        if (session == null) return this.ApiNotFound();
        return this.ApiOk(new
        {
            session.UserId,
            session.IsActive
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
