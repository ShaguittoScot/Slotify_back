using Microsoft.EntityFrameworkCore;
using Slotify.Domain.Entities;
using Slotify.Domain.Interfaces;

namespace Slotify.Infrastructure.Data.Repositories;

public class UserSessionRepository(AppDbContext context) : IUserSessionRepository
{
    private readonly AppDbContext _context = context;

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        await _context.UserSessions.AddAsync(session, cancellationToken);
        return session;
    }

    public async Task<UserSession?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        _context.UserSessions.UpdateRange(sessions);
    }
}
