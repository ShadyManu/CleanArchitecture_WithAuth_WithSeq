namespace Application.Common.Interfaces.Auth;

public interface ITokenService
{
    /// <summary>
    /// Creates the application's JWT access token for the given user.
    /// </summary>
    string CreateAccessToken(Guid userId, string providerId);

    /// <summary>
    /// Access token TTL in seconds (useful for clients).
    /// </summary>
    int AccessTokenExpiresInSeconds { get; }

    /// <summary>
    /// Generates a cryptographically-secure refresh token (raw value).
    /// Never store the raw value in DB; store only the hash.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Hashes a refresh token for storage (e.g., SHA-256 or HMAC-SHA256).
    /// </summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Returns the refresh token expiration date (UTC).
    /// </summary>
    DateTime GetRefreshTokenExpiryUtc();
}
