using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Models;
using UserManagementPoC.Shared.Repositories;
using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Controllers;

[Authorize]
public class UsersAdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IAccessGroupService _accessGroupService;
    private readonly IPermissionAdministrationService _permissionAdministrationService;
    private readonly IPermissionAssignmentService _permissionAssignmentService;
    private readonly IOrganizationUnitService _organizationUnitService;
    private readonly IUnitOfWork _uow;

    public UsersAdminController(
        IUserService userService,
        IRoleService roleService,
        IAccessGroupService accessGroupService,
        IPermissionAdministrationService permissionAdministrationService,
        IPermissionAssignmentService permissionAssignmentService,
        IOrganizationUnitService organizationUnitService,
        IUnitOfWork uow)
    {
        _userService = userService;
        _roleService = roleService;
        _accessGroupService = accessGroupService;
        _permissionAdministrationService = permissionAdministrationService;
        _permissionAssignmentService = permissionAssignmentService;
        _organizationUnitService = organizationUnitService;
        _uow = uow;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewBag.Search = search;
        return View(await _userService.GetAllAsync(page, 20, search));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateOrgUnitDropdownAsync();
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOrgUnitDropdownAsync();
            return View(request);
        }
        var result = await _userService.CreateAsync(request.Username, request.Email, request.Password, request.FirstName, request.LastName, request.DomicileUnitId, request.StartDate, request.EndDate);
        if (!result.Succeeded)
        {
            AddErrors(result);
            await PopulateOrgUnitDropdownAsync();
            return View(request);
        }
        TempData["Success"] = "User created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _uow.Repository<BshUser>().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        await PopulateOrgUnitDropdownAsync();
        return View(new UpdateUserRequest
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            DomicileUnitId = user.DomicileUnitId,
            StartDate = user.StartDate,
            EndDate = user.EndDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOrgUnitDropdownAsync();
            return View(request);
        }
        var result = await _userService.UpdateAsync(id, request);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Update failed");
            await PopulateOrgUnitDropdownAsync();
            return View(request);
        }
        TempData["Success"] = "User updated";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        ViewBag.UserEntity = await _uow.Repository<BshUser>().FirstOrDefaultAsync(u => u.Id == id);
        ViewBag.RoleAssignments = await _uow.Repository<UserRole>().FindAsync(
            ur => ur.UserId == id,
            q => q.AsNoTracking().Include(ur => ur.Role).OrderByDescending(ur => ur.CreatedAt));
        ViewBag.Roles = await _permissionAssignmentService.GetUserRolesAsync(id);
        ViewBag.Permissions = await _permissionAssignmentService.GetUserPermissionsAsync(id);
        ViewBag.DirectPermissions = await _uow.Repository<UserPermission>().FindAsync(
            up => up.UserId == id,
            q => q.AsNoTracking()
                  .Include(up => up.Permission).ThenInclude(p => p.Type)
                  .Include(up => up.Permission).ThenInclude(p => p.SubPermission)
                  .OrderByDescending(up => up.CreatedAt));
        ViewBag.AccessGroups = await _uow.Repository<UserAccessGroup>().FindAsync(
            uag => uag.UserId == id,
            q => q.AsNoTracking().Include(uag => uag.AccessGroup).OrderByDescending(uag => uag.CreatedAt));
        ViewBag.Logins = await _userService.GetLoginsAsync(id);
        await PopulateAssignmentDropdownsAsync();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _userService.DeactivateAsync(id);
        if (!result.Success) TempData["Error"] = result.Error ?? "Deactivation failed";
        else TempData["Success"] = "User deactivated";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteAsync(id);
        if (!result.Success) TempData["Error"] = result.Error ?? "Deletion failed";
        else TempData["Success"] = "User deleted";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string id, string roleId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var role = await _roleService.GetByIdAsync(roleId);
        var roleName = role?.Name ?? roleId;
        var result = await _userService.AssignRoleAsync(id, roleName, scopeOrganizationUnitId, cascadeOrgStructure);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Role assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(string id, string roleName, string? scopeOrganizationUnitId)
    {
        var result = await _userService.RemoveRoleAsync(id, roleName, scopeOrganizationUnitId);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Role removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermission(string id, string permissionId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _userService.AssignPermissionAsync(id, permissionId, scopeOrganizationUnitId, cascadeOrgStructure);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Permission assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePermission(string id, string permissionId, string? scopeOrganizationUnitId)
    {
        var result = await _userService.RemovePermissionAsync(id, permissionId, scopeOrganizationUnitId);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Permission removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAccessGroup(string id, string accessGroupId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _userService.AssignAccessGroupAsync(id, accessGroupId, scopeOrganizationUnitId, cascadeOrgStructure);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Access group assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAccessGroup(string id, string accessGroupId, string? scopeOrganizationUnitId)
    {
        var result = await _userService.RemoveAccessGroupAsync(id, accessGroupId, scopeOrganizationUnitId);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Access group removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogin(string id, string loginProvider, string providerKey)
    {
        var result = await _userService.RemoveLoginAsync(id, loginProvider, providerKey);
        if (!result.Success) TempData["Error"] = result.Error ?? "Removal failed";
        else TempData["Success"] = "External login removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoleScope(string id, string assignmentId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _userService.UpdateUserRoleScopeAsync(assignmentId, scopeOrganizationUnitId, cascadeOrgStructure);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Role scope updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRoleAssignment(string id, string assignmentId)
    {
        var result = await _userService.RemoveUserRoleAsync(assignmentId);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Role assignment removed" : (result.Error ?? "Removal failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePermissionScope(string id, string assignmentId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _userService.UpdateUserPermissionScopeAsync(assignmentId, scopeOrganizationUnitId, cascadeOrgStructure);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Permission scope updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePermissionAssignment(string id, string assignmentId)
    {
        var result = await _userService.RemoveUserPermissionAsync(assignmentId);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Permission assignment removed" : (result.Error ?? "Removal failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccessGroupScope(string id, string assignmentId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _userService.UpdateUserAccessGroupScopeAsync(assignmentId, scopeOrganizationUnitId, cascadeOrgStructure);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Access group scope updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAccessGroupAssignment(string id, string assignmentId)
    {
        var result = await _userService.RemoveUserAccessGroupAsync(assignmentId);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Access group assignment removed" : (result.Error ?? "Removal failed");
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateOrgUnitDropdownAsync()
    {
        var units = await _organizationUnitService.GetAllAsync();
        ViewData["OrgUnits"] = new SelectList(units, "Id", "Name");
    }

    private async Task PopulateAssignmentDropdownsAsync()
    {
        var units = await _organizationUnitService.GetAllAsync();
        ViewData["OrgUnits"] = new SelectList(units, "Id", "Name");
        var roles = await _roleService.GetAllAsync(1, 500);
        ViewData["RoleOptions"] = new SelectList(roles.Items, "Id", "Name");
        var permissions = await _permissionAdministrationService.GetPermissionsAsync();
        ViewData["PermissionOptions"] = new SelectList(permissions, "Id", "Code");
        var groups = await _accessGroupService.GetAllAsync(1, 500);
        ViewData["AccessGroupOptions"] = new SelectList(groups.Items, "Id", "Name");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
    }
}