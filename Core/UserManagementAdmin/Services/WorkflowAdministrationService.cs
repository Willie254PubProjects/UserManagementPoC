using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class WorkflowAdministrationService : IWorkflowAdministrationService
{
    private readonly IUnitOfWork _uow;
    public WorkflowAdministrationService(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<List<WorkflowType>> GetWorkflowTypesAsync()
    {
        var result = await _uow.Repository<WorkflowType>().GetAllAsync(q => q.Include(w => w.Actions));
        return result.ToList();
    }
    public async Task<WorkflowType> CreateWorkflowTypeAsync(string name, string description)
    {
        var wf = new WorkflowType
        {
            Name = name,
            Description = description
        };
        await _uow.Repository<WorkflowType>().AddAsync(wf);
        await _uow.SaveChangesAsync();
        return wf;
    }
    public async Task<WorkflowAction> CreateWorkflowActionAsync(string workflowId, string name, string description)
    {
        var action = new WorkflowAction
        {
            WorkflowId = workflowId,
            Name = name,
            Description = description
        };
        await _uow.Repository<WorkflowAction>().AddAsync(action);
        await _uow.SaveChangesAsync();
        return action;
    }
    public async Task<List<PermissionType>> GetPermissionTypesAsync()
    {
        var result = await _uow.Repository<PermissionType>().GetAllAsync();
        return result.ToList();
    }
    public async Task<PermissionType> CreatePermissionTypeAsync(string name, string description)
    {
        var pt = new PermissionType
        {
            Name = name,
            Description = description
        };
        await _uow.Repository<PermissionType>().AddAsync(pt);
        await _uow.SaveChangesAsync();
        return pt;
    }
    public async Task<List<Permission>> GetPermissionsAsync()
    {
        var result = await _uow.Repository<Permission>().GetAllAsync(
            q => q.Include(p => p.Workflow)
                  .Include(p => p.Action)
                  .Include(p => p.Type));
        return result.ToList();
    }
}
