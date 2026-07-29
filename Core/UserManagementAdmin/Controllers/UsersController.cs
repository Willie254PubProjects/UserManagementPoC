using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Extensions;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
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
