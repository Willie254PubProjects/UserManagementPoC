using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/organization-unit-types")]
public class OrganizationUnitTypesController : ControllerBase
{
    private readonly IOrganizationUnitService _service;
    public OrganizationUnitTypesController(IOrganizationUnitService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetTypesAsync();
        return this.ApiOk(types);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationUnitTypeRequest request)
    {
        var result = await _service.CreateTypeAsync(request.Name, request.Description, request.IsSubsidiary);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Organization unit type creation failed");
        return this.ApiOk(result.Data, "Organization unit type created");
    }
}
