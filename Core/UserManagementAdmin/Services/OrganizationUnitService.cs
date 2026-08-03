using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Abstractions;
using UserManagementPoC.Shared.Helpers;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class OrganizationUnitService : IOrganizationUnitService
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private const string LookupCacheKey = "org-unit-lookup";

    public OrganizationUnitService(IUnitOfWork uow, ICacheService cache)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<OrgUnitCodes> ResolveCodesAsync(string domicileUnitId)
    {
        if (string.IsNullOrWhiteSpace(domicileUnitId))
            return new OrgUnitCodes("", "", "");

        var lookup = await GetLookupAsync();
        if (!lookup.ById.TryGetValue(domicileUnitId, out var current))
            return new OrgUnitCodes("", "", "");

        if (current.Status != OrganizationUnitStatus.Active)
            return new OrgUnitCodes("", "", "");

        var branchCode = current.UnitCode;
        var countryCode = current.CountryCode;
        var bankCode = string.Empty;

        var ancestor = current;
        OrgUnitNode? root = current;
        while (ancestor != null)
        {
            if (ancestor.IsSubsidiary)
            {
                bankCode = ancestor.UnitCode;
                break;
            }
            root = ancestor;
            if (ancestor.ParentId == null || !lookup.ById.TryGetValue(ancestor.ParentId, out var parent)) break;
            if (parent.Status != OrganizationUnitStatus.Active)
                return new OrgUnitCodes("", "", "");
            ancestor = parent;
        }

        if (string.IsNullOrEmpty(bankCode))
            bankCode = root?.UnitCode ?? "";

        return new OrgUnitCodes(bankCode, branchCode, countryCode);
    }

    public async Task<IReadOnlySet<string>> ResolveScopeAsync(string scopeOrganizationUnitId, bool cascade)
    {
        var lookup = await GetLookupAsync();

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId) || !lookup.ById.TryGetValue(scopeOrganizationUnitId, out var scopeUnit))
            return new HashSet<string>();

        if (scopeUnit.Status != OrganizationUnitStatus.Active)
            return new HashSet<string>();

        if (!cascade)
        {
            var code = scopeUnit.UnitCode;
            return string.IsNullOrWhiteSpace(code)
                ? new HashSet<string>()
                : new HashSet<string> { code };
        }

        var codes = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(scopeOrganizationUnitId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!lookup.ById.TryGetValue(id, out var unit)) continue;
            if (unit.Status != OrganizationUnitStatus.Active) continue;
            var code = unit.UnitCode;
            if (!string.IsNullOrWhiteSpace(code)) codes.Add(code);
            if (lookup.ChildrenByParent.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
        }
        return codes;
    }

    public async Task<IEnumerable<OrganizationUnit>> GetAllAsync()
    {
        return await _uow.Repository<OrganizationUnit>().GetAllAsync(
            q => q.AsNoTracking().Include(o => o.Type).Include(o => o.Parent));
    }

    public async Task<OrganizationUnit?> GetByIdAsync(string id)
    {
        return await _uow.Repository<OrganizationUnit>().FirstOrDefaultAsync(
            o => o.Id == id,
            q => q.Include(o => o.Type).Include(o => o.Parent));
    }

    public async Task<IEnumerable<OrganizationUnit>> GetTreeAsync()
    {
        var all = await _uow.Repository<OrganizationUnit>().GetAllAsync(
            q => q.AsNoTracking().Include(o => o.Type));
        var childrenByParent = new Dictionary<string, List<OrganizationUnit>>(StringComparer.Ordinal);
        foreach (var unit in all)
        {
            if (!string.IsNullOrEmpty(unit.ParentId))
            {
                if (!childrenByParent.TryGetValue(unit.ParentId, out var children))
                {
                    children = new List<OrganizationUnit>();
                    childrenByParent[unit.ParentId] = children;
                }
                children.Add(unit);
            }
        }
        foreach (var unit in all)
        {
            if (childrenByParent.TryGetValue(unit.Id, out var children))
            {
                unit.Children = children;
            }
        }
        return all;
    }

    public async Task<AdminResult<OrganizationUnit>> CreateAsync(string name, string description, string unitCode, string countryCode, string typeId, string? parentId, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<OrganizationUnit>.Fail("Name is required");
        if (string.IsNullOrWhiteSpace(unitCode)) return AdminResult<OrganizationUnit>.Fail("Unit code is required");
        if (string.IsNullOrWhiteSpace(typeId)) return AdminResult<OrganizationUnit>.Fail("Type is required");

        var type = await _uow.Repository<OrganizationUnitType>().GetByIdAsync(typeId);
        if (type == null) return AdminResult<OrganizationUnit>.Fail("Organization unit type not found");

        var duplicate = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.UnitCode.ToLower() == unitCode.ToLower());
        if (duplicate) return AdminResult<OrganizationUnit>.Fail($"Unit code '{unitCode}' is already in use");

        if (!string.IsNullOrEmpty(parentId))
        {
            var parent = await _uow.Repository<OrganizationUnit>().GetByIdAsync(parentId);
            if (parent == null) return AdminResult<OrganizationUnit>.Fail("Parent organization unit not found");
        }

        var now = DateTime.UtcNow;
        var unit = new OrganizationUnit
        {
            Id = KeyGen.GenerateKey(),
            Name = name,
            Description = description ?? "",
            UnitCode = unitCode,
            CountryCode = countryCode ?? "",
            TypeId = typeId,
            ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId,
            Status = OrganizationUnitStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = startDate ?? now,
            EndDate = endDate
        };
        await _uow.Repository<OrganizationUnit>().AddAsync(unit);
        await _uow.SaveChangesAsync();
        await InvalidateLookupAsync();
        return AdminResult<OrganizationUnit>.Ok(unit);
    }

    public async Task<AdminResult<OrganizationUnit>> UpdateAsync(string id, string name, string description, string unitCode, string countryCode, string typeId, string? parentId, OrganizationUnitStatus status, DateTime? endDate)
    {
        var unit = await _uow.Repository<OrganizationUnit>().GetByIdAsync(id);
        if (unit == null) return AdminResult<OrganizationUnit>.Fail("Organization unit not found");

        if (string.IsNullOrWhiteSpace(name)) return AdminResult<OrganizationUnit>.Fail("Name is required");
        if (string.IsNullOrWhiteSpace(unitCode)) return AdminResult<OrganizationUnit>.Fail("Unit code is required");
        if (string.IsNullOrWhiteSpace(typeId)) return AdminResult<OrganizationUnit>.Fail("Type is required");

        var type = await _uow.Repository<OrganizationUnitType>().GetByIdAsync(typeId);
        if (type == null) return AdminResult<OrganizationUnit>.Fail("Organization unit type not found");

        var duplicate = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.Id != id && o.UnitCode.ToLower() == unitCode.ToLower());
        if (duplicate) return AdminResult<OrganizationUnit>.Fail($"Unit code '{unitCode}' is already in use");

        if (!string.IsNullOrEmpty(parentId) && parentId == id)
            return AdminResult<OrganizationUnit>.Fail("A unit cannot be its own parent");

        if (!string.IsNullOrEmpty(parentId))
        {
            var parent = await _uow.Repository<OrganizationUnit>().GetByIdAsync(parentId);
            if (parent == null) return AdminResult<OrganizationUnit>.Fail("Parent organization unit not found");

            var lookup = await GetLookupAsync();
            var cursor = parentId;
            while (cursor != null)
            {
                if (cursor == id)
                    return AdminResult<OrganizationUnit>.Fail("A unit cannot be moved under its own descendant");
                if (!lookup.ById.TryGetValue(cursor, out var node) || node.ParentId == null) break;
                cursor = node.ParentId;
            }
        }

        unit.Name = name;
        unit.Description = description ?? "";
        unit.UnitCode = unitCode;
        unit.CountryCode = countryCode ?? "";
        unit.TypeId = typeId;
        unit.ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId;
        unit.Status = status;
        unit.EndDate = endDate;
        unit.UpdatedAt = DateTime.UtcNow;
        unit.LastUpdatedBy = "system";

        _uow.Repository<OrganizationUnit>().Update(unit);
        await _uow.SaveChangesAsync();
        await InvalidateLookupAsync();
        return AdminResult<OrganizationUnit>.Ok(unit);
    }

    public async Task<AdminResult<bool>> DeleteAsync(string id)
    {
        var unit = await _uow.Repository<OrganizationUnit>().GetByIdAsync(id);
        if (unit == null) return AdminResult<bool>.Fail("Organization unit not found");

        var hasChildren = await _uow.Repository<OrganizationUnit>().AnyAsync(o => o.ParentId == id);
        if (hasChildren) return AdminResult<bool>.Fail("Cannot delete a unit that has children");

        var inUseAsScope =
            await _uow.Repository<UserRole>().AnyAsync(ur => ur.ScopeOrganizationUnitId == id)
            || await _uow.Repository<UserAccessGroup>().AnyAsync(uag => uag.ScopeOrganizationUnitId == id)
            || await _uow.Repository<UserPermission>().AnyAsync(up => up.ScopeOrganizationUnitId == id);
        if (inUseAsScope) return AdminResult<bool>.Fail("Cannot delete a unit that is used as an assignment scope");

        var isDomicile = await _uow.Repository<BshUser>().AnyAsync(u => u.DomicileUnitId == id);
        if (isDomicile) return AdminResult<bool>.Fail("Cannot delete a unit that is a user's domicile unit");

        _uow.Repository<OrganizationUnit>().Delete(unit);
        await _uow.SaveChangesAsync();
        await InvalidateLookupAsync();
        return AdminResult<bool>.Ok(true);
    }

    public async Task<IEnumerable<OrganizationUnitType>> GetTypesAsync()
    {
        return await _uow.Repository<OrganizationUnitType>().GetAllAsync(q => q.AsNoTracking());
    }

    public async Task<AdminResult<OrganizationUnitType>> CreateTypeAsync(string name, string description, bool isSubsidiary)
    {
        if (string.IsNullOrWhiteSpace(name)) return AdminResult<OrganizationUnitType>.Fail("Name is required");
        var duplicate = await _uow.Repository<OrganizationUnitType>().AnyAsync(t => t.Name.ToLower() == name.ToLower());
        if (duplicate) return AdminResult<OrganizationUnitType>.Fail($"Type '{name}' already exists");

        var now = DateTime.UtcNow;
        var type = new OrganizationUnitType
        {
            Name = name,
            Description = description ?? "",
            IsSubsidiary = isSubsidiary,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "system",
            LastUpdatedBy = "system",
            StartDate = now
        };
        await _uow.Repository<OrganizationUnitType>().AddAsync(type);
        await _uow.SaveChangesAsync();
        await InvalidateLookupAsync();
        return AdminResult<OrganizationUnitType>.Ok(type);
    }

    private async Task<OrgUnitLookup> GetLookupAsync()
    {
        var cached = await _cache.GetAsync<OrgUnitLookup>(LookupCacheKey);
        if (cached != null) return cached;

        var nodes = await _uow.Repository<OrganizationUnit>().FindAsync(
            _ => true,
            q => q.AsNoTracking().Include(o => o.Type));
        var byId = new Dictionary<string, OrgUnitNode>(StringComparer.Ordinal);
        var childrenByParent = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var unit in nodes)
        {
            if (string.IsNullOrEmpty(unit.Id)) continue;
            var node = new OrgUnitNode(unit.Id, unit.ParentId, unit.UnitCode, unit.CountryCode, unit.Status, unit.Type?.IsSubsidiary == true);
            byId[unit.Id] = node;
            if (!string.IsNullOrEmpty(unit.ParentId))
            {
                if (!childrenByParent.TryGetValue(unit.ParentId, out var children))
                {
                    children = new List<string>();
                    childrenByParent[unit.ParentId] = children;
                }
                children.Add(unit.Id);
            }
        }
        var lookup = new OrgUnitLookup(byId, childrenByParent);
        await _cache.SetAsync(LookupCacheKey, lookup, TimeSpan.FromMinutes(5));
        return lookup;
    }

    private Task InvalidateLookupAsync()
    {
        return _cache.RemoveAsync(LookupCacheKey);
    }

    private sealed record OrgUnitLookup(
        IReadOnlyDictionary<string, OrgUnitNode> ById,
        IReadOnlyDictionary<string, List<string>> ChildrenByParent);

    private sealed record OrgUnitNode(
        string Id,
        string? ParentId,
        string UnitCode,
        string CountryCode,
        OrganizationUnitStatus Status,
        bool IsSubsidiary);
}