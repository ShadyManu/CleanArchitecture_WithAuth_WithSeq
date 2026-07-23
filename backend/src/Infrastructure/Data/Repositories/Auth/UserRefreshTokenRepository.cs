using Application.Common.Interfaces.Repositories.Auth;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public sealed class UserRefreshTokenRepository : BaseGuidRepository<UserRefreshTokenEntity>,
    IUserRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public UserRefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<UserRefreshTokenEntity?> GetByHashWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        _context.UserRefreshTokens
            .AsNoTracking()
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryRotateAsync(
        Guid tokenId,
        Guid userId,
        string currentTokenHash,
        string newTokenHash,
        DateTime newExpiresAtUtc,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var modifiedAt = new DateTimeOffset(utcNow, TimeSpan.Zero);
        var affectedRows = await _context.UserRefreshTokens
            .Where(token =>
                token.Id == tokenId &&
                token.UserId == userId &&
                token.TokenHash == currentTokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.TokenHash, newTokenHash)
                    .SetProperty(token => token.ExpiresAt, newExpiresAtUtc)
                    .SetProperty(token => token.LastModified, modifiedAt)
                    .SetProperty(token => token.LastModifiedBy, userId.ToString()),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryRevokeSessionAsync(
        Guid tokenId,
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var modifiedAt = new DateTimeOffset(revokedAtUtc, TimeSpan.Zero);

        var revokedRows = await _context.UserRefreshTokens
            .Where(token =>
                token.Id == tokenId &&
                token.UserId == userId &&
                token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAtUtc)
                    .SetProperty(token => token.LastModified, modifiedAt)
                    .SetProperty(token => token.LastModifiedBy, userId.ToString()),
                cancellationToken);

        return revokedRows == 1;
    }

    public Task<int> RevokeAllSessionsAsync(
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var modifiedAt = new DateTimeOffset(revokedAtUtc, TimeSpan.Zero);

        return _context.UserRefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAtUtc)
                    .SetProperty(token => token.LastModified, modifiedAt)
                    .SetProperty(token => token.LastModifiedBy, userId.ToString()),
                cancellationToken);
    }
}
