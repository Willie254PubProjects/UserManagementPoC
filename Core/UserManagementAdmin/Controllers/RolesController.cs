using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Extensions;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IPermissionAssignmentService _permissionAssignmentService;

    public RolesController(IRoleService roleService, IPermissionAssignmentService permissionAssignmentService)
    {
        _roleService = roleService;
        _permissionAssignmentService = permissionAssignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var roles = await _roleService.GetAllAsync(page, pageSize);
        return this.ApiOk(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return this.ApiNotFound();
        return this.ApiOk(new
        {
            role.Id,
            role.Name,
            role.Description,
            Permissions = (role.Permissions ?? Enumerable.Empty<RolePermission>()).Select(rp => new
            {
                rp.PermissionId,
                rp.Permission?.Code
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateAsync(request.Name);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role creation failed");
        return this.ApiOk("Role created");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _roleService.UpdateAsync(id, request.Name, request.Description);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role update failed");
        return this.ApiOk("Role updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _roleService.DeleteAsync(id);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role deletion failed");
        return this.ApiOk("Role deleted");
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetUsers(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _roleService.GetUsersAsync(id, page, pageSize);
        return this.ApiOk(users);
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermission(string id, [FromBody] AssignPermissionRequest request)
    {
        await _permissionAssignmentService.AssignPermissionToRoleAsync(id, request.PermissionId);
        return this.ApiOk("Permission assigned");
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermission(string id, string permissionId)
    {
        await _permissionAssignmentService.RemovePermissionFromRoleAsync(id, permissionId);
        return this.ApiOk("Permission removed");
    }
}