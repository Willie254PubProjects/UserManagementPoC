using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/organization-units")]
public class OrganizationUnitsController : ControllerBase
{
    private readonly IOrganizationUnitService _service;
    public OrganizationUnitsController(IOrganizationUnitService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var units = await _service.GetAllAsync();
        var result = units.Select(ToSummary);
        return this.ApiOk(result);
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var units = await _service.GetTreeAsync();
        var roots = units.Where(o => o.ParentId == null);
        return this.ApiOk(roots.Select(BuildNode));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var unit = await _service.GetByIdAsync(id);
        if (unit == null) return this.ApiNotFound();
        return this.ApiOk(ToDetail(unit));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationUnitRequest request)
    {
        var result = await _service.CreateAsync(request.Name, request.Description, request.UnitCode, request.CountryCode, request.TypeId, request.ParentId, request.StartDate, request.EndDate);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Organization unit creation failed");
        return this.ApiOk(ToDetail(await _service.GetByIdAsync(result.Data!.Id) ?? result.Data), "Organization unit created");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateOrganizationUnitRequest request)
    {
        var result = await _service.UpdateAsync(id, request.Name, request.Description, request.UnitCode, request.CountryCode, request.TypeId, request.ParentId, request.Status, request.EndDate);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Organization unit update failed");
        return this.ApiOk(ToDetail(await _service.GetByIdAsync(id) ?? result.Data!), "Organization unit updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Organization unit deletion failed");
        return this.ApiOk("Organization unit deleted");
    }

    private static object ToSummary(OrganizationUnit unit)
    {
        return new
        {
            unit.Id,
            unit.Name,
            unit.Description,
            unit.UnitCode,
            unit.CountryCode,
            unit.Status,
            Type = unit.Type?.Name,
            unit.ParentId
        };
    }

    private static object ToDetail(OrganizationUnit unit)
    {
        return new
        {
            unit.Id,
            unit.Name,
            unit.Description,
            unit.UnitCode,
            unit.CountryCode,
            unit.Status,
            unit.TypeId,
            Type = unit.Type?.Name,
            unit.ParentId,
            ParentName = unit.Parent?.Name
        };
    }

    private static object BuildNode(OrganizationUnit unit)
    {
        return new
        {
            unit.Id,
            unit.Name,
            unit.Description,
            unit.UnitCode,
            unit.CountryCode,
            unit.Status,
            Type = unit.Type?.Name,
            unit.ParentId,
            Children = (unit.Children ?? Enumerable.Empty<OrganizationUnit>()).Select(BuildNode)
        };
    }
}
