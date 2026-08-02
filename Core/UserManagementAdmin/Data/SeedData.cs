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
        if (context.Set<PermissionType>().Any()) return;
        var now = DateTime.UtcNow;
        var systemUser = "system";

        var permissionTypes = new Dictionary<string, PermissionType>();
        foreach (var (name, desc) in new[] {
            ("CardPrinting", "Card printing operations"), ("Account", "Account operations"), ("CardRequest", "Card request operations")
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

        var subPermissions = new Dictionary<string, SubPermission>();
        foreach (var (name, desc) in new[] {
            ("Create", "Create new records"), ("View", "View records"), ("Edit", "Edit records"), ("Approve", "Approve records"), ("Submit", "Submit records"), ("Invoke", "Invoke operations")
        })
        {
            var sp = new SubPermission
            {
                Name = name,
                Description = desc,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser,
                StartDate = now
            };
            context.Set<SubPermission>().Add(sp);
            subPermissions[name] = sp;
        }
        await context.SaveChangesAsync();

        var allPermissions = new List<Permission>();
        foreach (var pt in permissionTypes.Values)
        {
            foreach (var sp in subPermissions.Values)
            {
                allPermissions.Add(new Permission
                {
                    PermissionTypeId = pt.Id,
                    SubPermissionId = sp.Id,
                    Description = $"{sp.Name} {pt.Name}",
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

        var subsidiaryType = new OrganizationUnitType
        {
            Name = "Subsidiary",
            Description = "Top-level organizational unit",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        var branchType = new OrganizationUnitType
        {
            Name = "Branch",
            Description = "Operational branch unit",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<OrganizationUnitType>().AddRange(subsidiaryType, branchType);
        await context.SaveChangesAsync();

        var hq = new OrganizationUnit
        {
            Name = "Main Subsidiary",
            Description = "Headquarters subsidiary",
            TypeId = subsidiaryType.Id,
            UnitCode = "KE",
            CountryCode = "KE",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<OrganizationUnit>().Add(hq);
        await context.SaveChangesAsync();
        var mainBranch = new OrganizationUnit
        {
            Name = "Main Branch",
            Description = "Main branch unit",
            TypeId = branchType.Id,
            ParentId = hq.Id,
            UnitCode = "001",
            CountryCode = "KE",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<OrganizationUnit>().Add(mainBranch);
        await context.SaveChangesAsync();

        var adminRole = new BshRole
        {
            Name = "Administrator",
            Description = "Full system administrator",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        var managerRole = new BshRole
        {
            Name = "Manager",
            Description = "Branch manager",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        var viewerRole = new BshRole
        {
            Name = "Viewer",
            Description = "Read-only viewer",
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
            DomicileUnitId = mainBranch.Id
        };
        await userManager.CreateAsync(admin, "Admin@123!");

        var viewer = new BshUser
        {
            UserName = "viewer",
            Email = "viewer@company.com",
            FirstName = "Viewer",
            LastName = "User",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now,
            DomicileUnitId = mainBranch.Id
        };
        await userManager.CreateAsync(viewer, "Viewer@123!");

        context.Set<UserRole>().Add(new UserRole
        {
            RoleId = adminRole.Id,
            UserId = admin.Id,
            ScopeOrganizationUnitId = hq.Id,
            CascadeOrgStructure = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });
        context.Set<UserRole>().Add(new UserRole
        {
            RoleId = viewerRole.Id,
            UserId = viewer.Id,
            ScopeOrganizationUnitId = mainBranch.Id,
            CascadeOrgStructure = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });

        var opsGroup = new AccessGroup
        {
            Name = "Branch Operations",
            Description = "Branch-level operational permissions",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<AccessGroup>().Add(opsGroup);
        await context.SaveChangesAsync();

        var cardRequestSubmit = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardRequest"].Id && p.SubPermissionId == subPermissions["Submit"].Id);
        var cardRequestView = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardRequest"].Id && p.SubPermissionId == subPermissions["View"].Id);
        foreach (var permission in new[] { cardRequestSubmit, cardRequestView })
        {
            context.Set<AccessGroupPermission>().Add(new AccessGroupPermission
            {
                AccessGroupId = opsGroup.Id,
                PermissionId = permission.Id
            });
        }
        context.Set<UserAccessGroup>().Add(new UserAccessGroup
        {
            AccessGroupId = opsGroup.Id,
            UserId = viewer.Id,
            ScopeOrganizationUnitId = hq.Id,
            CascadeOrgStructure = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });

        var cardPrintingInvoke = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardPrinting"].Id && p.SubPermissionId == subPermissions["Invoke"].Id);
        context.Set<UserPermission>().Add(new UserPermission
        {
            PermissionId = cardPrintingInvoke.Id,
            UserId = admin.Id,
            ScopeOrganizationUnitId = mainBranch.Id,
            CascadeOrgStructure = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });

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

        var viewPermissions = allPermissions
            .Where(p => p.SubPermissionId == subPermissions["View"].Id)
            .ToList();
        foreach (var permission in viewPermissions)
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
