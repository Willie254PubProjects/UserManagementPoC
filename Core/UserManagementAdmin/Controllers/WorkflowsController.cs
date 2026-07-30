using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAdmin.Models.Requests;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Extensions;

namespace UserManagementAdmin.Controllers;

[Authorize]
[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowAdministrationService _service;

    public WorkflowsController(IWorkflowAdministrationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var workflows = await _service.GetWorkflowTypesAsync();
        return this.ApiOk(workflows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest request)
    {
        var wf = await _service.CreateWorkflowTypeAsync(request.Name, request.Description);
        return this.ApiOk(wf, "Workflow created");
    }

    [HttpPost("{workflowId}/actions")]
    public async Task<IActionResult> CreateAction(string workflowId, [FromBody] CreateWorkflowActionRequest request)
    {
        var action = await _service.CreateWorkflowActionAsync(workflowId, request.Name, request.Description);
        return this.ApiOk(action, "Action created");
    }
}
