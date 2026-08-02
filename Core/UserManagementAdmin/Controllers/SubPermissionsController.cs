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
}
