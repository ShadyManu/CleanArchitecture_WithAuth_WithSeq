namespace Infrastructure.Auth.Models;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Symmetric key used to sign access tokens (HMAC).
    /// Must be strong (>= 32 chars, ideally 64+).
    /// </summary>
    public string SigningKey { get; set; } = null!;

    /// <summary>
    /// Access token lifetime in minutes.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenDays { get; set; } = 60;

    /// <summary>
    /// Random bytes for refresh token generation.
    /// 64 is a good default.
    /// </summary>
    public int RefreshTokenBytes { get; set; } = 64;

    /// <summary>
    /// Optional secret key for HMAC hashing refresh tokens (recommended).
    /// If not set, SHA-256 is used.
    /// </summary>
    public string? RefreshTokenHmacKey { get; set; }

    /// <summary>
    /// Base64-encoded 256-bit key used exclusively to encrypt reversible
    /// refresh tokens issued by external identity providers.
    /// </summary>
    public string ProviderTokenEncryptionKey { get; set; } = null!;
}

