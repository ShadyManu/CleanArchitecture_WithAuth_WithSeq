using System.IdentityModel.Tokens.Jwt;
using Infrastructure;
using Infrastructure.Auth.Models;
using Infrastructure.Auth.Services;
using Infrastructure.Auth.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Application.UnitTests.Tests.Auth;

public sealed class TokenSecurityTests
{
    private static readonly string EncryptionKey =
        Convert.ToBase64String("FAKE-TEST-ENCRYPTION-KEY-32BYTES"u8);

    [Fact]
    public void ProviderTokenProtector_ShouldEncryptAndAuthenticateToken()
    {
        var protector = new AesGcmProviderTokenProtector(
            Options.Create(ValidJwtOptions()));

        var protectedToken = protector.Protect("apple-refresh-token");

        Assert.NotEqual("apple-refresh-token", protectedToken);
        Assert.True(protector.TryUnprotect(protectedToken, out var plaintext));
        Assert.Equal("apple-refresh-token", plaintext);

        var parts = protectedToken.Split('.');
        parts[2] = $"{(parts[2][0] == 'A' ? 'B' : 'A')}{parts[2][1..]}";
        var tampered = string.Join('.', parts);
        Assert.False(protector.TryUnprotect(tampered, out _));
    }

    [Fact]
    public void AccessToken_ShouldContainUniqueIdAndUserId()
    {
        var userId = Guid.NewGuid();
        var tokenService = new TokenService(
            Options.Create(ValidJwtOptions()),
            TimeProvider.System);

        var encodedToken = tokenService.CreateAccessToken(userId, "provider-id");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);

        Assert.Equal(userId.ToString(), token.Subject);
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }

    [Fact]
    public void JwtOptionsValidator_ShouldRejectWeakAndMalformedKeys()
    {
        var options = ValidJwtOptions();
        options.SigningKey = "short";
        options.ProviderTokenEncryptionKey = "not-base64";

        var result = new JwtOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(nameof(JwtOptions.SigningKey), StringComparison.Ordinal));
        Assert.Contains(
            result.Failures,
            failure => failure.Contains(
                nameof(JwtOptions.ProviderTokenEncryptionKey),
                StringComparison.Ordinal));
    }

    [Fact]
    public void JwtOptionsValidator_ShouldAcceptEnterpriseDefaults()
    {
        var result = new JwtOptionsValidator().Validate(null, ValidJwtOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void InfrastructureRegistration_ShouldRejectUnknownDatabaseProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:DatabaseProvider"] = "Postgre"
            })
            .Build();

        var exception = Assert.Throws<NotSupportedException>(() =>
            new ServiceCollection().AddInfrastructureServices(configuration));

        Assert.Contains("Postgre", exception.Message, StringComparison.Ordinal);
    }

    private static JwtOptions ValidJwtOptions() =>
        new()
        {
            Issuer = "unit-tests",
            Audience = "unit-tests-client",
            SigningKey = "FAKE-UNIT-TEST-SIGNING-KEY-DO-NOT-USE",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 60,
            RefreshTokenBytes = 64,
            RefreshTokenHmacKey = "FAKE-UNIT-TEST-REFRESH-HMAC-KEY-DO-NOT-USE",
            ProviderTokenEncryptionKey = EncryptionKey
        };
}
