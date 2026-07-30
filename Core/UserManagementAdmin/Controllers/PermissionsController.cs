using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IWorkflowAdministrationService _service;
    public PermissionsController(IWorkflowAdministrationService service)
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
            p.Name,
            Workflow = p.Workflow?.Name,
            Action = p.Action?.Name,
            Type = p.Type?.Name
        });
        return this.ApiOk(result);
    }
}
