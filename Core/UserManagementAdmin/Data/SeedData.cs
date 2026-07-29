using Microsoft.AspNetCore.Identity;

using UserManagementAdmin.Models.Entities;

using UserManagementAdmin.Persistence;

namespace UserManagementAdmin.Data;

public static class SeedData
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<BshUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<BshRole>>();
        if (context.Set<WorkflowType>().Any()) return;
        var now = DateTime.UtcNow;
        var systemUser = "system";
        var workflowTypes = new Dictionary<string, WorkflowType>();
        foreach (var name in new[] {
 "Loan", "AccountOpening", "CustomerOnboarding"
})
        {
            var wf = new WorkflowType
            {
                Name = name,
                Description = $"{name} workflow",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser,
                StartDate = now
            };
            context.Set<WorkflowType>().Add(wf);
            workflowTypes[name] = wf;

        }
        var workflowActions = new List<WorkflowAction>();
        var actionNames = new[] {
 "Create", "View", "Approve", "Edit"
};
        foreach (var wf in workflowTypes.Values)
        {
            foreach (var actionName in actionNames)
            {
                workflowActions.Add(new WorkflowAction
                {
                    Name = actionName,
                    Description = $"{actionName} {wf.Name}",
                    WorkflowId = wf.WorkflowId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = systemUser,
                    LastUpdatedBy = systemUser,
                    StartDate = now
                });

            }
        }
        context.Set<WorkflowAction>().AddRange(workflowActions);
        var permissionTypes = new Dictionary<string, PermissionType>();
        foreach (var (name, desc) in new[] {
 ("Create", "Create new records"), ("Invoke", "Invoke operations"), ("View", "View records"), ("Approve", "Approve records")
})
        {
            var pt = new PermissionType
            {
                Name = name,
                Description = desc,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser,
                StartDate = now
            };
            context.Set<PermissionType>().Add(pt);
            permissionTypes[name] = pt;

        }
        await context.SaveChangesAsync();
        var allPermissions = new List<Permission>();
        foreach (var wf in workflowTypes.Values)
        {
            var wfActions = workflowActions.Where(a => a.WorkflowId == wf.WorkflowId).ToList();
            foreach (var action in wfActions)
            {
                foreach (var pt in permissionTypes.Values)
                {
                    allPermissions.Add(new Permission
                    {
                        WorkflowId = wf.WorkflowId,
                        ActionId = action.ActionId,
                        TypeId = pt.Id,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedBy = systemUser,
                        LastUpdatedBy = systemUser,
                        StartDate = now
                    });

                }
            }
            foreach (var pt in permissionTypes.Values)
            {
                allPermissions.Add(new Permission
                {
                    WorkflowId = wf.WorkflowId,
                    ActionId = null,
                    TypeId = pt.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = systemUser,
                    LastUpdatedBy = systemUser,
                    StartDate = now
                });

            }
        }
        context.Set<Permission>().AddRange(allPermissions);
        await context.SaveChangesAsync();
        var subsidiary = new Subsidiary
        {
            BankId = 1,
            Description = "Main Subsidiary",
            CountryCode = "US",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<Subsidiary>().Add(subsidiary);
        await context.SaveChangesAsync();
        var branch = new Branch
        {
            Name = "Main Branch",
            Description = "Main Branch",
            BranchCode = "001",
            SubsidiaryId = subsidiary.Id
        };
        context.Set<Branch>().Add(branch);
        await context.SaveChangesAsync();
        var adminRole = new BshRole
        {
            Name = "Administrator",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        var managerRole = new BshRole
        {
            Name = "Manager",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        var viewerRole = new BshRole
        {
            Name = "Viewer",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        await roleManager.CreateAsync(adminRole);
        await roleManager.CreateAsync(managerRole);
        await roleManager.CreateAsync(viewerRole);
        var admin = new BshUser
        {
            UserName = "admin",
            Email = "admin@company.com",
            FirstName = "System",
            LastName = "Admin",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now,
            SubsidiaryId = subsidiary.Id,
            BranchId = branch.Id
        };
        await userManager.CreateAsync(admin, "Admin@123!");
        await userManager.AddToRoleAsync(admin, "Administrator");
        foreach (var permission in allPermissions)
        {
            context.Set<RolePermission>().Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser
            });

        }
        var viewActionIds = workflowActions.Where(a => a.Name == "View").Select(a => a.ActionId).ToHashSet();
        var viewerPermissions = allPermissions.Where(p => p.ActionId != null && viewActionIds.Contains(p.ActionId)).ToList();
        foreach (var permission in viewerPermissions)
        {
            context.Set<RolePermission>().Add(new RolePermission
            {
                RoleId = viewerRole.Id,
                PermissionId = permission.Id,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser
            });

        }
        await context.SaveChangesAsync();

    }
}