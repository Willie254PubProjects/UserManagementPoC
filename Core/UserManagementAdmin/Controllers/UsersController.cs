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
        var result = await _userService.CreateAsync(request.Username, request.Email, request.Password, request.FirstName, request.LastName, request.DomicileUnitId, request.StartDate, request.EndDate);
        if (!result.Succeeded) return this.ApiBadRequest(result, "User creation failed");
        return this.ApiOk("User created");
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] RoleRequest request)
    {
        var result = await _userService.AssignRoleAsync(id, request.RoleName, request.ScopeOrganizationUnitId, request.CascadeOrgStructure);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role assignment failed");
        return this.ApiOk("Role assigned");
    }

    [HttpDelete("{id}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string id, string roleName, [FromQuery] string? scopeOrganizationUnitId = null)
    {
        var result = await _userService.RemoveRoleAsync(id, roleName, scopeOrganizationUnitId);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role removal failed");
        return this.ApiOk("Role removed");
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermission(string id, [FromBody] AssignPermissionRequest request)
    {
        var result = await _userService.AssignPermissionAsync(id, request.PermissionId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure, request.StartDate, request.EndDate);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Permission assignment failed");
        return this.ApiOk("Permission assigned");
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermission(string id, string permissionId, [FromQuery] string? scopeOrganizationUnitId = null)
    {
        var result = await _userService.RemovePermissionAsync(id, permissionId, scopeOrganizationUnitId);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Permission removal failed");
        return this.ApiOk("Permission removed");
    }

    [HttpPost("{id}/access-groups")]
    public async Task<IActionResult> AssignAccessGroup(string id, [FromBody] AssignAccessGroupRequest request)
    {
        var result = await _userService.AssignAccessGroupAsync(id, request.AccessGroupId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure, request.StartDate, request.EndDate);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Access group assignment failed");
        return this.ApiOk("Access group assigned");
    }

    [HttpDelete("{id}/access-groups/{accessGroupId}")]
    public async Task<IActionResult> RemoveAccessGroup(string id, string accessGroupId, [FromQuery] string? scopeOrganizationUnitId = null)
    {
        var result = await _userService.RemoveAccessGroupAsync(id, accessGroupId, scopeOrganizationUnitId);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Access group removal failed");
        return this.ApiOk("Access group removed");
    }
}
