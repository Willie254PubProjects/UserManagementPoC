using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services.Interfaces;

public record OrgUnitCodes(string BankId, string BranchId, string CountryCode);

public sealed record AdminResult<T>(bool Success, string? Error, T? Data)
{
    public static AdminResult<T> Ok(T data) => new(true, null, data);
    public static AdminResult<T> Fail(string error) => new(false, error, default);
}

public interface IOrganizationUnitService
{
    Task<OrgUnitCodes> ResolveCodesAsync(string domicileUnitId);
    Task<IReadOnlySet<string>> ResolveScopeAsync(string scopeOrganizationUnitId, bool cascade);

    Task<IEnumerable<OrganizationUnit>> GetAllAsync();
    Task<OrganizationUnit?> GetByIdAsync(string id);
    Task<IEnumerable<OrganizationUnit>> GetTreeAsync();
    Task<AdminResult<OrganizationUnit>> CreateAsync(string name, string description, string unitCode, string countryCode, string typeId, string? parentId, DateTime? startDate = null, DateTime? endDate = null);
    Task<AdminResult<OrganizationUnit>> UpdateAsync(string id, string name, string description, string unitCode, string countryCode, string typeId, string? parentId, OrganizationUnitStatus status, DateTime? endDate);
    Task<AdminResult<bool>> DeleteAsync(string id);

    Task<IEnumerable<OrganizationUnitType>> GetTypesAsync();
    Task<AdminResult<OrganizationUnitType>> CreateTypeAsync(string name, string description, bool isSubsidiary);
}
