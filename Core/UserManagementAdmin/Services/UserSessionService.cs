using Microsoft.EntityFrameworkCore;

using UserManagementAdmin.Models.Entities;

using UserManagementAdmin.Persistence;

namespace UserManagementAdmin.Services;

public class UserSessionService
{
    private readonly AdminDbContext _context;
    public UserSessionService(AdminDbContext context)
    {
        _context = context;

    }
    public async Task<UserSession> CreateAsync(string userId, string? remoteIp = null, string? userAgent = null)
    {
        var now = DateTime.UtcNow;
        var session = new UserSession
        {
            UserId = userId,
            SecurityVersion = Guid.NewGuid().ToString(),
            RemoteIP = remoteIp,
            UserAgent = userAgent,
            CreatedAt = now,
            UpdatedAt = now,
            LastAccessedAt = now,
            IsActive = true,
            CreatedBy = userId,
            LastUpdatedBy = userId,
        };

        _context.Set<UserSession>().Add(session);
        await _context.SaveChangesAsync();
        return session;

    }
    public async Task<UserSession?> GetBySecurityVersionAsync(string securityVersion)
    {
        return await _context.Set<UserSession>().FirstOrDefaultAsync(s => s.SecurityVersion == securityVersion);

    }
    public async Task<bool> InvalidateAsync(string securityVersion)
    {
        var session = await _context.Set<UserSession>().FirstOrDefaultAsync(s => s.SecurityVersion == securityVersion);
        if (session == null) return false;
        session.IsActive = false;
        session.SecurityVersion = Guid.NewGuid().ToString();
        session.LastAccessedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;

    }
}