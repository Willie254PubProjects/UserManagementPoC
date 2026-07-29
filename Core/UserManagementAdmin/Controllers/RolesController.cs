using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Extensions;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly PermissionAssignmentService _permissionAssignmentService;

    public RolesController(RoleService roleService, PermissionAssignmentService permissionAssignmentService)
    {
        _roleService = roleService;
        _permissionAssignmentService = permissionAssignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleService.GetAllAsync();
        return this.ApiOk(roles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _roleService.CreateAsync(request.Name);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role creation failed");
        return this.ApiOk("Role created");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _roleService.DeleteAsync(id);
        if (!result.Succeeded) return this.ApiBadRequest(result, "Role deletion failed");
        return this.ApiOk("Role deleted");
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
