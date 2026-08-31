using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionAdministrationService _service;
    public PermissionsController(IPermissionAdministrationService service)
    {
        _service = service;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var permissions = await _service.GetPermissionsAsync();
        var result = permissions.Select(p => new
        {
            p.Id,
            p.Code,
            p.Description,
            PermissionType = p.Type?.Name,
            SubPermission = p.SubPermission?.Name
        });
        return this.ApiOk(result);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequest request)
    {
        var result = await _service.CreatePermissionAsync(request.PermissionTypeId, request.SubPermissionId, request.Description);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission creation failed");
        return this.ApiOk(result.Data, "Permission created");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeletePermissionAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Permission deletion failed");
        return this.ApiOk("Permission deleted");
    }
}