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
            ("CardPrinting", "Card printing operations"), ("CardRequest", "Card request operations")
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

        var types = new Dictionary<string, OrganizationUnitType>();
        foreach (var (name, desc, isSubsidiary) in new[] {
            ("Group", "Holding group root", false),
            ("Subsidiary", "Top-level subsidiary", true),
            ("Department", "Functional department", false),
            ("RegionalBranch", "Regional branch", false),
            ("Branch", "Operational branch unit", false)
        })
        {
            var t = new OrganizationUnitType
            {
                Name = name,
                Description = desc,
                IsSubsidiary = isSubsidiary,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser,
                StartDate = now
            };
            context.Set<OrganizationUnitType>().Add(t);
            types[name] = t;
        }
        await context.SaveChangesAsync();

        var units = new Dictionary<string, OrganizationUnit>();
        OrganizationUnit AddUnit(string key, string name, string description, string typeKey, string unitCode, string countryCode, string? parentKey = null)
        {
            var unit = new OrganizationUnit
            {
                Name = name,
                Description = description,
                TypeId = types[typeKey].Id,
                UnitCode = unitCode,
                CountryCode = countryCode,
                ParentId = parentKey == null ? null : units[parentKey].Id,
                Status = OrganizationUnitStatus.Active,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = systemUser,
                LastUpdatedBy = systemUser,
                StartDate = now
            };
            context.Set<OrganizationUnit>().Add(unit);
            units[key] = unit;
            return unit;
        }

        AddUnit("group", "Demo Group Holdings", "Group holding company root", "Group", "0001", "KE");
        AddUnit("ke", "KE Subsidiary", "Kenya subsidiary", "Subsidiary", "KE", "KE", "group");
        AddUnit("ke-ops", "Kenya Operations", "Kenya operations department", "Department", "0002", "KE", "ke");
        AddUnit("nairobi", "Nairobi Regional Branch", "Nairobi region", "RegionalBranch", "0004", "KE", "ke");
        AddUnit("nairobi-hq", "Nairobi HQ Branch", "Nairobi headquarters branch", "Branch", "0005", "KE", "nairobi");
        AddUnit("westlands", "Westlands Branch", "Westlands branch", "Branch", "0006", "KE", "nairobi");
        AddUnit("coast", "Coast Regional Branch", "Coast region", "RegionalBranch", "0007", "KE", "ke");
        AddUnit("mombasa", "Mombasa Branch", "Mombasa branch", "Branch", "0008", "KE", "coast");
        AddUnit("ug", "UG Subsidiary", "Uganda subsidiary", "Subsidiary", "UG", "UG", "group");
        AddUnit("kampala", "Kampala Branch", "Kampala branch", "Branch", "0009", "UG", "ug");
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
            DomicileUnitId = units["group"].Id
        };
        await userManager.CreateAsync(admin, "Admin@123!");

        var manager = new BshUser
        {
            UserName = "manager",
            Email = "manager@company.com",
            FirstName = "Branch",
            LastName = "Manager",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now,
            DomicileUnitId = units["nairobi-hq"].Id
        };
        await userManager.CreateAsync(manager, "Manager@123!");

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
            DomicileUnitId = units["mombasa"].Id
        };
        await userManager.CreateAsync(viewer, "Viewer@123!");

        context.Set<UserRole>().Add(new UserRole
        {
            RoleId = adminRole.Id,
            UserId = admin.Id,
            ScopeOrganizationUnitId = units["group"].Id,
            CascadeOrgStructure = true,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });
        context.Set<UserRole>().Add(new UserRole
        {
            RoleId = managerRole.Id,
            UserId = manager.Id,
            ScopeOrganizationUnitId = units["nairobi-hq"].Id,
            CascadeOrgStructure = false,
            Status = AssignmentStatus.Active,
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
            ScopeOrganizationUnitId = units["coast"].Id,
            CascadeOrgStructure = true,
            Status = AssignmentStatus.Active,
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
        var corporateGroup = new AccessGroup
        {
            Name = "Corporate Governance",
            Description = "Corporate governance permissions",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        };
        context.Set<AccessGroup>().AddRange(opsGroup, corporateGroup);
        await context.SaveChangesAsync();

        context.Set<AccessGroupRole>().Add(new AccessGroupRole
        {
            AccessGroupId = opsGroup.Id,
            RoleId = managerRole.Id
        });
        context.Set<AccessGroupRole>().Add(new AccessGroupRole
        {
            AccessGroupId = corporateGroup.Id,
            RoleId = adminRole.Id
        });

        var cardRequestSubmit = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardRequest"].Id && p.SubPermissionId == subPermissions["Submit"].Id);
        var cardRequestView = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardRequest"].Id && p.SubPermissionId == subPermissions["View"].Id);
        var cardPrintingInvoke = allPermissions.First(p => p.PermissionTypeId == permissionTypes["CardPrinting"].Id && p.SubPermissionId == subPermissions["Invoke"].Id);
        foreach (var permission in new[] { cardRequestSubmit, cardRequestView, cardPrintingInvoke })
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
            ScopeOrganizationUnitId = units["coast"].Id,
            CascadeOrgStructure = true,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = systemUser,
            LastUpdatedBy = systemUser,
            StartDate = now
        });

        context.Set<UserPermission>().Add(new UserPermission
        {
            PermissionId = cardPrintingInvoke.Id,
            UserId = admin.Id,
            ScopeOrganizationUnitId = units["ke"].Id,
            CascadeOrgStructure = true,
            Status = AssignmentStatus.Active,
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

        var managerPermissions = new[]
        {
            ("CardRequest", "Create"), ("CardRequest", "View"), ("CardRequest", "Edit"), ("CardRequest", "Submit"), ("CardRequest", "Approve"),
            ("CardPrinting", "View"), ("CardPrinting", "Invoke")
        };
        foreach (var (ptName, spName) in managerPermissions)
        {
            var permission = allPermissions.First(p => p.PermissionTypeId == permissionTypes[ptName].Id && p.SubPermissionId == subPermissions[spName].Id);
            context.Set<RolePermission>().Add(new RolePermission
            {
                RoleId = managerRole.Id,
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
