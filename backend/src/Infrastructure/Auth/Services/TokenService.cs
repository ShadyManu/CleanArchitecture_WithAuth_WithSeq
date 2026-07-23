using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Auth;
using Infrastructure.Auth.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _jwt;
    private readonly TimeProvider _timeProvider;

    public TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
    {
        _jwt = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public int AccessTokenExpiresInSeconds => _jwt.AccessTokenMinutes * 60;

    public string CreateAccessToken(Guid userId, string providerId)
    {
        var now = _timeProvider.GetUtcNow();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("providerId", providerId),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(_jwt.AccessTokenMinutes).UtcDateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // 64 bytes => strong enough, then base64url
        var bytes = RandomNumberGenerator.GetBytes(_jwt.RefreshTokenBytes);
        return Base64UrlEncode(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        // Prefer HMACSHA256 if you want to protect against DB leaks + rainbow tables.
        // If you keep a server-side secret ("Pepper"), hashing is stronger than plain SHA256.
        if (!string.IsNullOrWhiteSpace(_jwt.RefreshTokenHmacKey))
        {
            var keyBytes = Encoding.UTF8.GetBytes(_jwt.RefreshTokenHmacKey);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Base64UrlEncode(hash);
        }

        using var sha = SHA256.Create();
        var shaHash = sha.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Base64UrlEncode(shaHash);
    }

    public DateTime GetRefreshTokenExpiryUtc()
        => _timeProvider.GetUtcNow().UtcDateTime.AddDays(_jwt.RefreshTokenDays);

    private static string Base64UrlEncode(byte[] data)
    {
        // Base64Url per tokens (no + / =)
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
