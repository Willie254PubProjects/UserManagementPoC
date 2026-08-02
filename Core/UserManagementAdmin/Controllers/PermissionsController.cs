using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
