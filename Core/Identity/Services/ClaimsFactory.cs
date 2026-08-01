using System.Security.Claims;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Identity.Services;

public class ClaimsFactory
{
    public List<Claim> Create(UserInfo user, string? securityVersion = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName),
            new("bank_id", user.BankId),
            new("branch_id", user.BranchId),
            new("country_code", user.CountryCode)
        };

        if (!string.IsNullOrEmpty(securityVersion))
        {
            claims.Add(new("security_version", securityVersion));
        }

        return claims;
    }
}