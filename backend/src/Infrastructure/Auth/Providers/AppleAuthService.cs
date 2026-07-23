using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Application.Common.Interfaces.Auth;
using Application.Common.Options;
using Application.Dtos.Auth.Request;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth.Providers;

public sealed class AppleAuthService : IAppleAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _appleAudience;
    private readonly string _teamId;
    private readonly string _keyId;
    private readonly string _privateKeyBase64;

    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public AppleAuthService(
        HttpClient httpClient,
        IOptions<AppleAuthOptions> options,
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager)
    {
        _httpClient = httpClient;
        _configurationManager = configurationManager;

        _appleAudience = options.Value.Audience;
        _teamId = options.Value.TeamId;
        _keyId = options.Value.KeyId;

        // Normalize the private key once and keep the Base64 payload in memory.
        _privateKeyBase64 = options.Value.PrivateKey
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "");
    }

    public async Task<(bool isValid, string? Email, string ProviderId)> ExtractEmailAndProviderIdAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return (false, string.Empty, string.Empty);

        try
        {
            var config = await _configurationManager.GetConfigurationAsync(cancellationToken);
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = AppleAuthConstants.Issuer,
                ValidateAudience = true,
                ValidAudience = _appleAudience,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            handler.ValidateToken(idToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
                return (false, string.Empty, string.Empty);

            string providerId = jwtToken.Subject;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            return (true, email, providerId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return (false, string.Empty, string.Empty);
        }
    }

    public async Task<AppleTokenResponse> ExchangeCodeForTokenAsync(
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        try
        {
            using var requestData = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", _appleAudience),
                new KeyValuePair<string, string>("client_secret", GenerateAppleClientSecret()),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            ]);

            using var response = await _httpClient.PostAsync(
                AppleAuthConstants.TokenUrl,
                requestData,
                cancellationToken);

            var appleResponse = await response.Content.ReadFromJsonAsync<AppleTokenResponse>(cancellationToken: cancellationToken);
            return appleResponse ?? new AppleTokenResponse { Error = $"Apple returned {(int)response.StatusCode}" };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            JsonException or
            NotSupportedException or
            FormatException or
            CryptographicException)
        {
            return new AppleTokenResponse { Error = "Apple token exchange failed" };
        }
    }

    public async Task<bool> RevokeTokenAsync(
        string appleRefreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var requestData = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", _appleAudience),
                new KeyValuePair<string, string>("client_secret", GenerateAppleClientSecret()),
                new KeyValuePair<string, string>("token", appleRefreshToken),
                new KeyValuePair<string, string>("token_type_hint", "refresh_token")
            ]);

            using var response = await _httpClient.PostAsync(
                AppleAuthConstants.RevokeTokenUrl,
                requestData,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or
            FormatException or
            CryptographicException)
        {
            return false;
        }
    }

    private string GenerateAppleClientSecret()
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(_privateKeyBase64), out _);

        var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = _keyId };
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _teamId,
            Audience = AppleAuthConstants.Issuer,
            Subject = new ClaimsIdentity([new Claim("sub", _appleAudience)]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);
        return handler.WriteToken(token);
    }
}
