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
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var users = await _userService.GetAllAsync(page, pageSize, search);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "User update failed");
        return this.ApiOk(result.Data, "User updated");
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _userService.DeactivateAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "User deactivation failed");
        return this.ApiOk("User deactivated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "User deletion failed");
        return this.ApiOk("User deleted");
    }

    [HttpGet("{id}/logins")]
    public async Task<IActionResult> GetLogins(string id)
    {
        var logins = await _userService.GetLoginsAsync(id);
        return this.ApiOk(logins);
    }

    [HttpDelete("{id}/logins/{loginProvider}/{providerKey}")]
    public async Task<IActionResult> RemoveLogin(string id, string loginProvider, string providerKey)
    {
        var result = await _userService.RemoveLoginAsync(id, loginProvider, providerKey);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "External login removal failed");
        return this.ApiOk("External login removed");
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

    [HttpPut("{userId}/assignments/roles/{assignmentId}")]
    public async Task<IActionResult> UpdateRoleAssignment(string assignmentId, [FromBody] UpdateAssignmentScopeRequest request)
    {
        var result = await _userService.UpdateUserRoleScopeAsync(assignmentId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Role assignment update failed");
        return this.ApiOk("Role scope updated");
    }

    [HttpDelete("{userId}/assignments/roles/{assignmentId}")]
    public async Task<IActionResult> RemoveRoleAssignment(string assignmentId)
    {
        var result = await _userService.RemoveUserRoleAsync(assignmentId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Role assignment removal failed");
        return this.ApiOk("Role assignment removed");
    }

    [HttpPut("{userId}/assignments/permissions/{assignmentId}")]
    public async Task<IActionResult> UpdatePermissionAssignment(string assignmentId, [FromBody] UpdateAssignmentScopeRequest request)
    {
        var result = await _userService.UpdateUserPermissionScopeAsync(assignmentId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission assignment update failed");
        return this.ApiOk("Permission scope updated");
    }

    [HttpDelete("{userId}/assignments/permissions/{assignmentId}")]
    public async Task<IActionResult> RemovePermissionAssignment(string assignmentId)
    {
        var result = await _userService.RemoveUserPermissionAsync(assignmentId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission assignment removal failed");
        return this.ApiOk("Permission assignment removed");
    }

    [HttpPut("{userId}/assignments/access-groups/{assignmentId}")]
    public async Task<IActionResult> UpdateAccessGroupAssignment(string assignmentId, [FromBody] UpdateAssignmentScopeRequest request)
    {
        var result = await _userService.UpdateUserAccessGroupScopeAsync(assignmentId, request.ScopeOrganizationUnitId, request.CascadeOrgStructure);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Access group assignment update failed");
        return this.ApiOk("Access group scope updated");
    }

    [HttpDelete("{userId}/assignments/access-groups/{assignmentId}")]
    public async Task<IActionResult> RemoveAccessGroupAssignment(string assignmentId)
    {
        var result = await _userService.RemoveUserAccessGroupAsync(assignmentId);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Access group assignment removal failed");
        return this.ApiOk("Access group assignment removed");
    }
}
