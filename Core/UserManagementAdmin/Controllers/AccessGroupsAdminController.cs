using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;

namespace UserManagementAdmin.Controllers;

[Authorize]
public class AccessGroupsAdminController : Controller
{
    private readonly IAccessGroupService _service;
    private readonly IRoleService _roleService;
    private readonly IPermissionAdministrationService _permissionService;
    private readonly IOrganizationUnitService _orgUnitService;
    private readonly IUserService _userService;

    public AccessGroupsAdminController(
        IAccessGroupService service,
        IRoleService roleService,
        IPermissionAdministrationService permissionService,
        IOrganizationUnitService orgUnitService,
        IUserService userService)
    {
        _service = service;
        _roleService = roleService;
        _permissionService = permissionService;
        _orgUnitService = orgUnitService;
        _userService = userService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        return View(await _service.GetAllAsync(page, 20));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateAccessGroupRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccessGroupRequest request)
    {
        var result = await _service.CreateAsync(request.Name, request.Description, request.StartDate, request.EndDate);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Creation failed");
            return View(request);
        }
        TempData["Success"] = "Access group created";
        return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var group = await _service.GetByIdAsync(id);
        if (group == null) return NotFound();
        return View(new UpdateAccessGroupRequest
        {
            Name = group.Name,
            Description = group.Description,
            EndDate = group.EndDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateAccessGroupRequest request)
    {
        var result = await _service.UpdateAsync(id, request.Name, request.Description, request.EndDate);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Update failed");
            return View(request);
        }
        TempData["Success"] = "Access group updated";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) TempData["Error"] = result.Error ?? "Deletion failed";
        else TempData["Success"] = "Access group deleted";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var group = await _service.GetByIdAsync(id);
        if (group == null) return NotFound();
        ViewBag.Users = await _service.GetUsersAsync(id, 1, 20);
        ViewData["Roles"] = new SelectList((await _roleService.GetAllAsync(1, 500)).Items, "Id", "Name");
        ViewData["Permissions"] = new SelectList(await _permissionService.GetPermissionsAsync(), "Id", "Code");
        ViewData["OrgUnits"] = new SelectList(await _orgUnitService.GetAllAsync(), "Id", "Name");
        ViewData["UserSelect"] = new SelectList((await _userService.GetAllAsync(1, 500)).Items, "Id", "UserName");
        return View(group);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string id, string roleId)
    {
        var result = await _service.AssignRoleAsync(id, roleId);
        if (!result.Success) TempData["Error"] = result.Error ?? "Assignment failed";
        else TempData["Success"] = "Role assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(string id, string roleId)
    {
        var result = await _service.RemoveRoleAsync(id, roleId);
        if (!result.Success) TempData["Error"] = result.Error ?? "Removal failed";
        else TempData["Success"] = "Role removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermission(string id, string permissionId)
    {
        var result = await _service.AssignPermissionAsync(id, permissionId);
        if (!result.Success) TempData["Error"] = result.Error ?? "Assignment failed";
        else TempData["Success"] = "Permission assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePermission(string id, string permissionId)
    {
        var result = await _service.RemovePermissionAsync(id, permissionId);
        if (!result.Success) TempData["Error"] = result.Error ?? "Removal failed";
        else TempData["Success"] = "Permission removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignUser(string id, string userId, string scopeOrganizationUnitId, bool cascadeOrgStructure)
    {
        var result = await _service.AssignUserAsync(id, userId, scopeOrganizationUnitId, cascadeOrgStructure);
        if (!result.Success) TempData["Error"] = result.Error ?? "Assignment failed";
        else TempData["Success"] = "User assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(string id, string userId)
    {
        var result = await _service.RemoveUserAsync(id, userId);
        if (!result.Success) TempData["Error"] = result.Error ?? "Removal failed";
        else TempData["Success"] = "User removed";
        return RedirectToAction(nameof(Details), new { id });
    }
}