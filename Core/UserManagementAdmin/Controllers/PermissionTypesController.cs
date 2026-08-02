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
}
