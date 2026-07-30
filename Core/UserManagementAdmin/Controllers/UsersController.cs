using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Extensions;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _userService.GetAllAsync(page, pageSize);
        return this.ApiOk(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return this.ApiNotFound();
        return this.ApiOk(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateAsync(request.Username, request.Email, request.Password, request.FirstName, request.LastName);
        if (!result.Succeeded) return this.ApiBadRequest(result, "User creation failed");
        return this.ApiOk("User created");
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] RoleRequest request)
    {
        var result = await _userService.AssignRoleAsync(id, request.RoleName);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role assignment failed");
        return this.ApiOk("Role assigned");
    }

    [HttpDelete("{id}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string id, string roleName)
    {
        var result = await _userService.RemoveRoleAsync(id, roleName);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role removal failed");
        return this.ApiOk("Role removed");
    }
}
