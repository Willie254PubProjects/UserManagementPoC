using Microsoft.EntityFrameworkCore;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;

namespace UserManagementAdmin.Services;

public class UserSessionService : IUserSessionService
{
    private readonly IUnitOfWork _uow;
    private readonly TimeSpan _sessionTimeout;
    private readonly TimeSpan _idleTimeout;
    private static readonly TimeSpan LastAccessWriteThrottle = TimeSpan.FromMinutes(2);

    public UserSessionService(IUnitOfWork uow, IConfiguration configuration)
    {
        _uow = uow;
        _sessionTimeout = TimeSpan.FromMinutes(configuration.GetValue<int>("Session:SessionTimeoutMinutes", 30));
        _idleTimeout = TimeSpan.FromMinutes(configuration.GetValue<int>("Session:IdleTimeoutMinutes", 30));
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
            ExpiresAt = now.Add(_sessionTimeout),
            IdleTimeoutMinutes = (int)_idleTimeout.TotalMinutes,
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
        var session = await _uow.Repository<UserSession>().FirstOrDefaultAsync(
            s => s.SecurityVersion == securityVersion,
            q => q.Include(s => s.User));
        if (session == null) return null;

        var now = DateTime.UtcNow;
        var expired = false;
        if (session.ExpiresAt != null && now > session.ExpiresAt.Value) expired = true;
        if (!expired && session.IdleTimeoutMinutes.HasValue
            && session.LastAccessedAt != null
            && now - session.LastAccessedAt.Value > TimeSpan.FromMinutes(session.IdleTimeoutMinutes.Value))
            expired = true;

        if (expired)
        {
            session.IsActive = false;
            session.SecurityVersion = Guid.NewGuid().ToString();
            session.UpdatedAt = now;
            _uow.Repository<UserSession>().Update(session);
            await _uow.SaveChangesAsync();
            return null;
        }

        if (session.LastAccessedAt == null
            || now - session.LastAccessedAt.Value >= LastAccessWriteThrottle)
        {
            session.LastAccessedAt = now;
            session.UpdatedAt = now;
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
