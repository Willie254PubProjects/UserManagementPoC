using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/sub-permissions")]
public class SubPermissionsController : ControllerBase
{
    private readonly IPermissionAdministrationService _service;
    public SubPermissionsController(IPermissionAdministrationService service)
    {
        _service = service;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetSubPermissionsAsync();
        return this.ApiOk(types);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubPermissionRequest request)
    {
        var sp = await _service.CreateSubPermissionAsync(request.Name, request.Description);
        return this.ApiOk(sp, "Sub-permission created");
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSubPermissionRequest request)
    {
        var result = await _service.UpdateSubPermissionAsync(id, request.Name, request.Description);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Sub-permission update failed");
        return this.ApiOk(result.Data, "Sub-permission updated");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteSubPermissionAsync(id);
        if (!result.Success) return this.ApiBadRequest(result.Error ?? "Sub-permission deletion failed");
        return this.ApiOk("Sub-permission deleted");
    }
}