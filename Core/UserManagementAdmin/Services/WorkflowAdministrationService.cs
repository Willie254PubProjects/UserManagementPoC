using Microsoft.EntityFrameworkCore;

using UserManagementAdmin.Models.Entities;

using UserManagementAdmin.Persistence;

namespace UserManagementAdmin.Services;

public class WorkflowAdministrationService
{
    private readonly AdminDbContext _context;
    public WorkflowAdministrationService(AdminDbContext context)
    {
        _context = context;

    }
    public async Task<List<WorkflowType>> GetWorkflowTypesAsync()
    {
        return await _context.Set<WorkflowType>()
                             .Include(w => w.Actions)
                             .ToListAsync();

    }
    public async Task<WorkflowType> CreateWorkflowTypeAsync(string name, string description)
    {
        var wf = new WorkflowType
        {
            Name = name,
            Description = description
        };
        _context.Set<WorkflowType>().Add(wf);
        await _context.SaveChangesAsync();

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
        _context.Set<WorkflowAction>().Add(action);
        await _context.SaveChangesAsync();

        return action;
    }
    public async Task<List<PermissionType>> GetPermissionTypesAsync()
    {

        return await _context.Set<PermissionType>().ToListAsync();
    }
    public async Task<PermissionType> CreatePermissionTypeAsync(string name, string description)
    {
        var pt = new PermissionType
        {
            Name = name,
            Description = description
        };
        _context.Set<PermissionType>().Add(pt);
        await _context.SaveChangesAsync();

        return pt;
    }
    public async Task<List<Permission>> GetPermissionsAsync()
    {
        return await _context.Set<Permission>()
                             .Include(p => p.Workflow)
                             .Include(p => p.Action)
                             .Include(p => p.Type)
                             .ToListAsync();

    }
}