using Domain.Entities.Auth;

namespace Application.Common.Interfaces.Repositories.Auth;

public interface IUserRefreshTokenRepository : IBaseGuidRepository<UserRefreshTokenEntity>
{
    Task<UserRefreshTokenEntity?> GetByHashWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<bool> TryRotateAsync(
        Guid tokenId,
        Guid userId,
        string currentTokenHash,
        string newTokenHash,
        DateTime newExpiresAtUtc,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> TryRevokeSessionAsync(
        Guid tokenId,
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);

    Task<int> RevokeAllSessionsAsync(
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);
}
