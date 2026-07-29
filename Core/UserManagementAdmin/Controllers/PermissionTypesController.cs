using Microsoft.AspNetCore.Mvc;

using UserManagementAdmin.Models.Requests;

using UserManagementAdmin.Services;

using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[ApiController]
[Route("api/permission-types")]
public class PermissionTypesController : ControllerBase
{
    private readonly WorkflowAdministrationService _service;
    public PermissionTypesController(WorkflowAdministrationService service)
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