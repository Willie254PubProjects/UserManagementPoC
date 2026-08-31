using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;

namespace UserManagementAdmin.Controllers;

[Authorize]
public class OrganizationUnitsAdminController : Controller
{
    private readonly IOrganizationUnitService _service;
    public OrganizationUnitsAdminController(IOrganizationUnitService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index()
    {
        var units = await _service.GetAllAsync();
        return View(units);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new CreateOrganizationUnitRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrganizationUnitRequest request)
    {
        var result = await _service.CreateAsync(request.Name, request.Description, request.UnitCode, request.CountryCode, request.TypeId, request.ParentId, request.StartDate, request.EndDate);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Creation failed");
            await PopulateDropdownsAsync();
            return View(request);
        }
        TempData["Success"] = "Organization unit created";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var unit = await _service.GetByIdAsync(id);
        if (unit == null) return NotFound();
        await PopulateDropdownsAsync();
        return View(new UpdateOrganizationUnitRequest
        {
            Name = unit.Name,
            Description = unit.Description,
            UnitCode = unit.UnitCode,
            CountryCode = unit.CountryCode,
            TypeId = unit.TypeId,
            ParentId = unit.ParentId,
            Status = unit.Status,
            EndDate = unit.EndDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UpdateOrganizationUnitRequest request)
    {
        var result = await _service.UpdateAsync(id, request.Name, request.Description, request.UnitCode, request.CountryCode, request.TypeId, request.ParentId, request.Status, request.EndDate);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Update failed");
            await PopulateDropdownsAsync();
            return View(request);
        }
        TempData["Success"] = "Organization unit updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? name)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) TempData["Error"] = result.Error ?? "Deletion failed";
        else TempData["Success"] = "Organization unit deleted";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Types()
    {
        ViewBag.NewType = new CreateOrganizationUnitTypeRequest();
        return View(await _service.GetTypesAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(CreateOrganizationUnitTypeRequest request)
    {
        var result = await _service.CreateTypeAsync(request.Name, request.Description, request.IsSubsidiary);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Type created" : (result.Error ?? "Creation failed");
        return RedirectToAction(nameof(Types));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditType(string id, UpdateOrganizationUnitTypeRequest request)
    {
        var result = await _service.UpdateTypeAsync(id, request.Name, request.Description, request.IsSubsidiary);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Type updated" : (result.Error ?? "Update failed");
        return RedirectToAction(nameof(Types));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteType(string id)
    {
        var result = await _service.DeleteTypeAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Type deleted" : (result.Error ?? "Deletion failed");
        return RedirectToAction(nameof(Types));
    }

    private async Task PopulateDropdownsAsync()
    {
        var types = await _service.GetTypesAsync();
        ViewData["Types"] = new SelectList(types, "Id", "Name");
        var units = await _service.GetAllAsync();
        ViewData["Parents"] = new SelectList(units, "Id", "Name");
    }
}