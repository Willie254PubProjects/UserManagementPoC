namespace UserManagementAdmin.Services.Interfaces;

public record OrgUnitCodes(string BankId, string BranchId, string CountryCode);

public interface IOrganizationUnitService
{
    Task<OrgUnitCodes> ResolveCodesAsync(string domicileUnitId);
    Task<IReadOnlySet<string>> ResolveScopeAsync(string scopeOrganizationUnitId, bool cascade);
}
