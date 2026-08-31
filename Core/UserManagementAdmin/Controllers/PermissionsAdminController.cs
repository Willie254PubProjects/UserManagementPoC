using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;

namespace UserManagementAdmin.Controllers;

[Authorize]
public class PermissionsAdminController : Controller
{
    private readonly IPermissionAdministrationService _service;
    public PermissionsAdminController(IPermissionAdministrationService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _service.GetPermissionsAsync());
    }

    public async Task<IActionResult> Types()
    {
        ViewBag.NewType = new CreatePermissionTypeRequest();
        return View(await _service.GetPermissionTypesAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(CreatePermissionTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            TempData["Error"] = "Name is required";
            return RedirectToAction(nameof(Types));
        }
        await _service.CreatePermissionTypeAsync(request.Name, request.Description);
        TempData["Success"] = "Permission type created (permission matrix auto-generated)";
        return RedirectToAction(nameof(Types));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditType(string id, UpdatePermissionTypeRequest request)
    {
        var result = await _service.UpdatePermissionTypeAsync(id, request.Name, request.Description);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Permission type updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(Types));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteType(string id)
    {
        var result = await _service.DeletePermissionTypeAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Permission type deleted" : (result.Error ?? "Deletion failed");
        return RedirectToAction(nameof(Types));
    }

    public async Task<IActionResult> SubPermissions()
    {
        ViewBag.NewSub = new CreateSubPermissionRequest();
        return View(await _service.GetSubPermissionsAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSub(CreateSubPermissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            TempData["Error"] = "Name is required";
            return RedirectToAction(nameof(SubPermissions));
        }
        await _service.CreateSubPermissionAsync(request.Name, request.Description);
        TempData["Success"] = "Sub-permission created (permission matrix auto-generated)";
        return RedirectToAction(nameof(SubPermissions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSub(string id, UpdateSubPermissionRequest request)
    {
        var result = await _service.UpdateSubPermissionAsync(id, request.Name, request.Description);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sub-permission updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(SubPermissions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSub(string id)
    {
        var result = await _service.DeleteSubPermissionAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sub-permission deleted" : (result.Error ?? "Deletion failed");
        return RedirectToAction(nameof(SubPermissions));
    }
}