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
        var pt = new PermissionType
        {
            Name = name,
            Description = description
        };
        await _uow.Repository<PermissionType>().AddAsync(pt);
        await _uow.SaveChangesAsync();
        return pt;
    }
    public async Task<List<SubPermission>> GetSubPermissionsAsync()
    {
        var result = await _uow.Repository<SubPermission>().GetAllAsync();
        return result.ToList();
    }
    public async Task<SubPermission> CreateSubPermissionAsync(string name, string description)
    {
        var sp = new SubPermission
        {
            Name = name,
            Description = description
        };
        await _uow.Repository<SubPermission>().AddAsync(sp);
        await _uow.SaveChangesAsync();
        return sp;
    }
    public async Task<List<Permission>> GetPermissionsAsync()
    {
        var result = await _uow.Repository<Permission>().GetAllAsync(
            q => q.Include(p => p.SubPermission)
                  .Include(p => p.Type));
        return result.ToList();
    }
}
