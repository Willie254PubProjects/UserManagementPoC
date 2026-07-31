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
            new(ClaimTypes.Email, user.Email), new("given_name", 
            user.FirstName), new("family_name", user.LastName)
        };

        if (!string.IsNullOrEmpty(securityVersion))
        {
            claims.Add(new("security_version", securityVersion));
        }

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }
}