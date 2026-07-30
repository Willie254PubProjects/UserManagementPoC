using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Services.Interfaces;

public interface IUserSessionService
{
    Task<UserSession> CreateAsync(string userId, string? remoteIp = null, string? userAgent = null);
    Task<UserSession?> GetBySecurityVersionAsync(string securityVersion);
    Task<bool> InvalidateAsync(string securityVersion);
}
