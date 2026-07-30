using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IUnitOfWork _uow;
    public UserSessionService(IUnitOfWork uow)
    {
        _uow = uow;
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
        await _uow.Repository<UserSession>().AddAsync(session);
        await _uow.SaveChangesAsync();
        return session;
    }
    public async Task<UserSession?> GetBySecurityVersionAsync(string securityVersion)
    {
        var session = await _uow.Repository<UserSession>().FirstOrDefaultAsync(s => s.SecurityVersion == securityVersion);
        if (session != null)
        {
            session.LastAccessedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            _uow.Repository<UserSession>().Update(session);
            await _uow.SaveChangesAsync();
        }
        return session;
    }
    public async Task<bool> InvalidateAsync(string securityVersion)
    {
        var session = await _uow.Repository<UserSession>().FirstOrDefaultAsync(s => s.SecurityVersion == securityVersion);
        if (session == null) return false;
        session.IsActive = false;
        session.SecurityVersion = Guid.NewGuid().ToString();
        session.LastAccessedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        _uow.Repository<UserSession>().Update(session);
        await _uow.SaveChangesAsync();
        return true;
    }
}
