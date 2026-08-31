using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class PermissionAdministrationService : IPermissionAdministrationService
{
    private readonly IUnitOfWork _uow;
    public PermissionAdministrationService(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<List<PermissionType>> GetPermissionTypesAsync()
    {
        var result = await _uow.Repository<PermissionType>().GetAllAsync();
        return result.ToList();
    }
    public async Task<PermissionType> CreatePermissionTypeAsync(string name, string description)
    {
        var now = DateTime.UtcNow;
        var pt = new PermissionType
        {
            Name = name,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        };
        await _uow.Repository<PermissionType>().AddAsync(pt);
        await _uow.SaveChangesAsync();
        await GenerateMatrixForTypeAsync(pt);
        return pt;
    }
    public async Task<AdminResult<PermissionType>> UpdatePermissionTypeAsync(string id, string name, string description)
    {
        var pt = await _uow.Repository<PermissionType>().GetByIdAsync(id);
        if (pt == null) return AdminResult<PermissionType>.Fail("Permission type not found");
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<PermissionType>.Fail("Name is required");
        var duplicate = await _uow.Repository<PermissionType>().AnyAsync(t => t.Id != id && t.Name.ToLower() == name.ToLower());
        if (duplicate) return AdminResult<PermissionType>.Fail($"Permission type '{name}' already exists");

        pt.Name = name;
        pt.Description = description ?? pt.Description;
        pt.UpdatedAt = DateTime.UtcNow;
        pt.LastUpdatedBy = "system";
        _uow.Repository<PermissionType>().Update(pt);
        await _uow.SaveChangesAsync();
        return AdminResult<PermissionType>.Ok(pt);
    }
    public async Task<AdminResult<bool>> DeletePermissionTypeAsync(string id)
    {
        var pt = await _uow.Repository<PermissionType>().GetByIdAsync(id);
        if (pt == null) return AdminResult<bool>.Fail("Permission type not found");
        var hasPermissions = await _uow.Repository<Permission>().AnyAsync(p => p.PermissionTypeId == id);
        if (hasPermissions) return AdminResult<bool>.Fail("Cannot delete a permission type that has permissions");

        _uow.Repository<PermissionType>().Delete(pt);
        await _uow.SaveChangesAsync();
        return AdminResult<bool>.Ok(true);
    }
    public async Task<List<SubPermission>> GetSubPermissionsAsync()
    {
        var result = await _uow.Repository<SubPermission>().GetAllAsync();
        return result.ToList();
    }
    public async Task<SubPermission> CreateSubPermissionAsync(string name, string description)
    {
        var now = DateTime.UtcNow;
        var sp = new SubPermission
        {
            Name = name,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        };
        await _uow.Repository<SubPermission>().AddAsync(sp);
        await _uow.SaveChangesAsync();
        await GenerateMatrixForSubPermissionAsync(sp);
        return sp;
    }
    public async Task<AdminResult<SubPermission>> UpdateSubPermissionAsync(string id, string name, string description)
    {
        var sp = await _uow.Repository<SubPermission>().GetByIdAsync(id);
        if (sp == null) return AdminResult<SubPermission>.Fail("Sub-permission not found");
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<SubPermission>.Fail("Name is required");
        var duplicate = await _uow.Repository<SubPermission>().AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower());
        if (duplicate) return AdminResult<SubPermission>.Fail($"Sub-permission '{name}' already exists");

        sp.Name = name;
        sp.Description = description ?? sp.Description;
        sp.UpdatedAt = DateTime.UtcNow;
        sp.LastUpdatedBy = "system";
        _uow.Repository<SubPermission>().Update(sp);
        await _uow.SaveChangesAsync();
        return AdminResult<SubPermission>.Ok(sp);
    }
    public async Task<AdminResult<bool>> DeleteSubPermissionAsync(string id)
    {
        var sp = await _uow.Repository<SubPermission>().GetByIdAsync(id);
        if (sp == null) return AdminResult<bool>.Fail("Sub-permission not found");
        var hasPermissions = await _uow.Repository<Permission>().AnyAsync(p => p.SubPermissionId == id);
        if (hasPermissions) return AdminResult<bool>.Fail("Cannot delete a sub-permission that has permissions");

        _uow.Repository<SubPermission>().Delete(sp);
        await _uow.SaveChangesAsync();
        return AdminResult<bool>.Ok(true);
    }
    public async Task<List<Permission>> GetPermissionsAsync()
    {
        var result = await _uow.Repository<Permission>().GetAllAsync(
            q => q.Include(p => p.SubPermission)
                  .Include(p => p.Type));
        return result.ToList();
    }
    public async Task<AdminResult<Permission>> CreatePermissionAsync(string permissionTypeId, string subPermissionId, string? description)
    {
        var type = await _uow.Repository<PermissionType>().GetByIdAsync(permissionTypeId);
        if (type == null) return AdminResult<Permission>.Fail("Permission type not found");
        var sub = await _uow.Repository<SubPermission>().GetByIdAsync(subPermissionId);
        if (sub == null) return AdminResult<Permission>.Fail("Sub-permission not found");

        var duplicate = await _uow.Repository<Permission>().AnyAsync(p => p.PermissionTypeId == permissionTypeId && p.SubPermissionId == subPermissionId);
        if (duplicate) return AdminResult<Permission>.Fail("Permission already exists for this type and sub-permission");

        var now = DateTime.UtcNow;
        var permission = new Permission
        {
            PermissionTypeId = permissionTypeId,
            SubPermissionId = subPermissionId,
            Description = string.IsNullOrWhiteSpace(description) ? $"{type.Name}.{sub.Name}" : description,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        };
        await _uow.Repository<Permission>().AddAsync(permission);
        await _uow.SaveChangesAsync();
        return AdminResult<Permission>.Ok(permission);
    }
    public async Task<AdminResult<bool>> DeletePermissionAsync(string permissionId)
    {
        var permission = await _uow.Repository<Permission>().GetByIdAsync(permissionId);
        if (permission == null) return AdminResult<bool>.Fail("Permission not found");

        var referenced =
            await _uow.Repository<RolePermission>().AnyAsync(rp => rp.PermissionId == permissionId)
            || await _uow.Repository<AccessGroupPermission>().AnyAsync(agp => agp.PermissionId == permissionId)
            || await _uow.Repository<UserPermission>().AnyAsync(up => up.PermissionId == permissionId);
        if (referenced) return AdminResult<bool>.Fail("Cannot delete a permission that is assigned to a role, access group, or user");

        _uow.Repository<Permission>().Delete(permission);
        await _uow.SaveChangesAsync();
        return AdminResult<bool>.Ok(true);
    }
    private async Task GenerateMatrixForTypeAsync(PermissionType type)
    {
        var subPermissions = await _uow.Repository<SubPermission>().GetAllAsync();
        var now = DateTime.UtcNow;
        foreach (var sub in subPermissions)
        {
            var exists = await _uow.Repository<Permission>().AnyAsync(p => p.PermissionTypeId == type.Id && p.SubPermissionId == sub.Id);
            if (exists) continue;
            await _uow.Repository<Permission>().AddAsync(new Permission
            {
                PermissionTypeId = type.Id,
                SubPermissionId = sub.Id,
                Description = $"{type.Name}.{sub.Name}",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system",
                LastUpdatedBy = "system",
                StartDate = now
            });
        }
        await _uow.SaveChangesAsync();
    }
    private async Task GenerateMatrixForSubPermissionAsync(SubPermission sub)
    {
        var types = await _uow.Repository<PermissionType>().GetAllAsync();
        var now = DateTime.UtcNow;
        foreach (var type in types)
        {
            var exists = await _uow.Repository<Permission>().AnyAsync(p => p.PermissionTypeId == type.Id && p.SubPermissionId == sub.Id);
            if (exists) continue;
            await _uow.Repository<Permission>().AddAsync(new Permission
            {
                PermissionTypeId = type.Id,
                SubPermissionId = sub.Id,
                Description = $"{type.Name}.{sub.Name}",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system",
                LastUpdatedBy = "system",
                StartDate = now
            });
        }
        await _uow.SaveChangesAsync();
    }
}