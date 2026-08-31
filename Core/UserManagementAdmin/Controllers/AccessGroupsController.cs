using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/access-groups")]
public class AccessGroupsController : ControllerBase
{
    private readonly IAccessGroupService _service;
    public AccessGroupsController(IAccessGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var groups = await _service.GetAllAsync(page, pageSize);
        return this.ApiOk(groups);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var group = await _service.GetByIdAsync(id);
        if (group == null) return this.ApiNotFound();
        return this.ApiOk(new
        {
            group.Id,
            group.Name,
            group.Description,
            Roles = (group.Roles ?? Enumerable.Empty<AccessGroupRole>()).Select(r => new
            {
                r.RoleId,
                Name = r.Role?.Name
            }),
            Permissions = (group.Permissions ?? Enumerable.Empty<AccessGroupPermission>()).Select(p => new
            {
                p.PermissionId,
                p.Permission?.Code
            }),
            Users = (group.Users ?? Enumerable.Empty<UserAccessGroup>()).Select(u => new
            {
                u.UserId,
                UserName = u.User?.UserName,
                Email = u.User?.Email,
                u.ScopeOrganizationUnitId,
                u.CascadeOrgStructure,
                u.Status,
                u.StartDate,
                u.EndDate
            })
        });
    }

    [HttpGet("{id}/users")]
    public async Task<IActionResult> GetUsers(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _service.GetUsersAsync(id, page, pageSize);
        return this.ApiOk(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccessGroupRequest request)
    {
        var result = await _service.CreateAsync(request.Name, request.Description, request.StartDate, request.EndDate);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Access group creation failed");
        return this.ApiOk(result.Data, "Access group created");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAccessGroupRequest request)
    {
        var result = await _service.UpdateAsync(id, request.Name, request.Description, request.EndDate);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Access group update failed");
        return this.ApiOk(result.Data, "Access group updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Access group deletion failed");
        return this.ApiOk("Access group deleted");
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleToAccessGroupRequest request)
    {
        var result = await _service.AssignRoleAsync(id, request.RoleId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Role assignment failed");
        return this.ApiOk("Role assigned to access group");
    }

    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<IActionResult> RemoveRole(string id, string roleId)
    {
        var result = await _service.RemoveRoleAsync(id, roleId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Role removal failed");
        return this.ApiOk("Role removed from access group");
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermission(string id, [FromBody] AssignPermissionToAccessGroupRequest request)
    {
        var result = await _service.AssignPermissionAsync(id, request.PermissionId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission assignment failed");
        return this.ApiOk("Permission assigned to access group");
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermission(string id, string permissionId)
    {
        var result = await _service.RemovePermissionAsync(id, permissionId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission removal failed");
        return this.ApiOk("Permission removed from access group");
    }

    [HttpPost("{id}/users")]
    public async Task<IActionResult> AssignUser(string id, [FromBody] AssignUserToAccessGroupRequest request)
    {
        var result = await _service.AssignUserAsync(id, request.UserId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure, request.StartDate, request.EndDate);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "User assignment failed");
        return this.ApiOk("User assigned to access group");
    }

    [HttpDelete("{id}/users/{userId}")]
    public async Task<IActionResult> RemoveUser(string id, string userId, [FromQuery] string? scopeOrganizationUnitId = null)
    {
        var result = await _service.RemoveUserAsync(id, userId, scopeOrganizationUnitId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "User removal failed");
        return this.ApiOk("User removed from access group");
    }
}
