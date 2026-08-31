using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/permission-types")]
public class PermissionTypesController : ControllerBase
{
    private readonly IPermissionAdministrationService _service;
    public PermissionTypesController(IPermissionAdministrationService service)
    {
        _service = service;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetPermissionTypesAsync();
        return this.ApiOk(types);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionTypeRequest request)
    {
        var pt = await _service.CreatePermissionTypeAsync(request.Name, request.Description);
        return this.ApiOk(pt, "Permission type created");
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePermissionTypeRequest request)
    {
        var result = await _service.UpdatePermissionTypeAsync(id, request.Name, request.Description);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission type update failed");
        return this.ApiOk(result.Data, "Permission type updated");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeletePermissionTypeAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission type deletion failed");
        return this.ApiOk("Permission type deleted");
    }
}