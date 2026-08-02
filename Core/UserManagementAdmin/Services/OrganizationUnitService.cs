using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class OrganizationUnitService : IOrganizationUnitService
{
    private readonly IUnitOfWork _uow;
    public OrganizationUnitService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<OrgUnitCodes> ResolveCodesAsync(string domicileUnitId)
    {
        if (string.IsNullOrWhiteSpace(domicileUnitId))
            return new OrgUnitCodes("", "", "");

        var current = await GetUnitWithParentAsync(domicileUnitId);
        if (current == null)
            return new OrgUnitCodes("", "", "");

        var branchCode = current.UnitCode;
        var countryCode = current.CountryCode;
        var bankCode = string.Empty;

        var ancestor = current;
        OrganizationUnit? root = current;
        while (ancestor != null)
        {
            if (IsSubsidiary(ancestor))
            {
                bankCode = ancestor.UnitCode;
                break;
            }
            root = ancestor;
            if (ancestor.Parent == null) break;
            ancestor = await GetUnitWithParentAsync(ancestor.ParentId!);
        }

        if (string.IsNullOrEmpty(bankCode))
            bankCode = root?.UnitCode ?? "";

        return new OrgUnitCodes(bankCode, branchCode, countryCode);
    }

    public async Task<IReadOnlySet<string>> ResolveScopeAsync(string scopeOrganizationUnitId, bool cascade)
    {
        var all = await _uow.Repository<OrganizationUnit>().GetAllAsync();
        var units = all.ToList();
        var byId = units.Where(u => !string.IsNullOrEmpty(u.Id)).ToDictionary(u => u.Id);

        if (string.IsNullOrWhiteSpace(scopeOrganizationUnitId) || !byId.ContainsKey(scopeOrganizationUnitId))
            return units.Select(u => u.UnitCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet();

        var codes = new HashSet<string>();
        if (!cascade)
        {
            var code = byId[scopeOrganizationUnitId].UnitCode;
            if (!string.IsNullOrWhiteSpace(code)) codes.Add(code);
            return codes;
        }

        var queue = new Queue<string>();
        queue.Enqueue(scopeOrganizationUnitId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!byId.TryGetValue(id, out var unit)) continue;
            var code = unit.UnitCode;
            if (!string.IsNullOrWhiteSpace(code)) codes.Add(code);
            foreach (var child in units.Where(u => u.ParentId == id))
            {
                queue.Enqueue(child.Id);
            }
        }
        return codes;
    }

    private async Task<OrganizationUnit?> GetUnitWithParentAsync(string id)
    {
        return await _uow.Repository<OrganizationUnit>().FirstOrDefaultAsync(
            o => o.Id == id,
            q => q.Include(o => o.Type).Include(o => o.Parent));
    }

    private static bool IsSubsidiary(OrganizationUnit unit)
    {
        return string.Equals(unit.Type?.Name, "Subsidiary", StringComparison.OrdinalIgnoreCase);
    }
}
