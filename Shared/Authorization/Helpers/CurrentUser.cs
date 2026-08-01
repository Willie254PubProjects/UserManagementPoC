using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Abstractions;

namespace UserManagementPoC.Shared.Authorization.Helpers;

internal class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

    }
    public string? Id => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string UserName => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public string DisplayName => _httpContextAccessor.HttpContext?.User.FindFirstValue("display_name") ?? string.Empty;
    public string Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string BankId => _httpContextAccessor.HttpContext?.User.FindFirstValue("bank_id") ?? string.Empty;
    public string BranchId => _httpContextAccessor.HttpContext?.User.FindFirstValue("branch_id") ?? string.Empty;
    public string CountryCode => _httpContextAccessor.HttpContext?.User.FindFirstValue("country_code") ?? string.Empty;
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

}
