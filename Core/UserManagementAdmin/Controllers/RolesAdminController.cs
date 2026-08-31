using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;

namespace UserManagementAdmin.Controllers;

[Authorize]
public class RolesAdminController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IPermissionAssignmentService _permissionAssignmentService;
    private readonly IPermissionAdministrationService _permissionAdministrationService;

    public RolesAdminController(IRoleService roleService, IPermissionAssignmentService permissionAssignmentService, IPermissionAdministrationService permissionAdministrationService)
    {
        _roleService = roleService;
        _permissionAssignmentService = permissionAssignmentService;
        _permissionAdministrationService = permissionAdministrationService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        return View(await _roleService.GetAllAsync(page, 20));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateRoleRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleRequest request)
    {
        if (!ModelState.IsValid) return View(request);
        var result = await _roleService.CreateAsync(request.Name);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(request);
        }
        TempData["Success"] = "Role created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return NotFound();
        return View(new UpdateRoleRequest { Name = role.Name, Description = role.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateRoleRequest request)
    {
        if (!ModelState.IsValid) return View(request);
        var result = await _roleService.UpdateAsync(id, request.Name, request.Description);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(request);
        }
        TempData["Success"] = "Role updated";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _roleService.DeleteAsync(id);
        if (!result.Succeeded) TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        else TempData["Success"] = "Role deleted";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return NotFound();
        ViewBag.Users = await _roleService.GetUsersAsync(id, 1, 20);
        var permissions = await _permissionAdministrationService.GetPermissionsAsync();
        ViewData["Permissions"] = new SelectList(permissions, "Id", "Code");
        return View(role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPermission(string id, string permissionId)
    {
        await _permissionAssignmentService.AssignPermissionToRoleAsync(id, permissionId);
        TempData["Success"] = "Permission assigned";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePermission(string id, string permissionId)
    {
        await _permissionAssignmentService.RemovePermissionFromRoleAsync(id, permissionId);
        TempData["Success"] = "Permission removed";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
    }
}