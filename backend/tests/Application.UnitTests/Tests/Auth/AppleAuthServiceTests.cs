using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Options;
using Infrastructure.Auth.Providers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Application.UnitTests.Tests.Auth;

public class AppleAuthServiceTests
{
    private const string ClientId = "com.example.app";
    private const string TeamId = "APPLETEAMID";
    private const string KeyId = "APPLEKEYID";
    private const string TokenUrl = "https://appleid.apple.com/auth/token";
    private const string RevokeUrl = "https://appleid.apple.com/auth/revoke";

    [Fact]
    public async Task ExchangeCodeForToken_ShouldSendValidAppleRequest()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(TokenUrl, request.RequestUri?.AbsoluteUri);

            var form = await ReadFormAsync(request, cancellationToken);
            Assert.Equal(ClientId, form["client_id"]);
            Assert.Equal("authorization-code", form["code"]);
            Assert.Equal("authorization_code", form["grant_type"]);
            AssertClientSecret(form["client_secret"]);

            return JsonResponse(
                """
                {
                  "access_token": "apple-access",
                  "refresh_token": "apple-refresh",
                  "id_token": "apple-id-token",
                  "token_type": "Bearer",
                  "expires_in": 3600
                }
                """);
        });
        var service = CreateService(handler);

        // Act
        var result = await service.ExchangeCodeForTokenAsync(
            "authorization-code",
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("apple-refresh", result.RefreshToken);
        Assert.Equal("apple-id-token", result.IdToken);
    }

    [Fact]
    public async Task RevokeToken_ShouldSendRefreshTokenAndReturnSuccess()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(RevokeUrl, request.RequestUri?.AbsoluteUri);

            var form = await ReadFormAsync(request, cancellationToken);
            Assert.Equal(ClientId, form["client_id"]);
            Assert.Equal("apple-refresh", form["token"]);
            Assert.Equal("refresh_token", form["token_type_hint"]);
            AssertClientSecret(form["client_secret"]);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = CreateService(handler);

        // Act
        var result = await service.RevokeTokenAsync(
            "apple-refresh",
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExchangeCodeForToken_ShouldReturnFailureForInvalidConfiguration()
    {
        // Arrange
        var service = CreateService(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))),
            privateKey: "not-a-private-key");

        // Act
        var result = await service.ExchangeCodeForTokenAsync(
            "authorization-code",
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    private static AppleAuthService CreateService(
        HttpMessageHandler handler,
        string? privateKey = null)
    {
        if (privateKey is null)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            privateKey = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());
        }

        var options = Options.Create(new AppleAuthOptions
        {
            Audience = ClientId,
            TeamId = TeamId,
            KeyId = KeyId,
            PrivateKey = privateKey
        });
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://appleid.apple.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());

        return new AppleAuthService(
            new HttpClient(handler),
            options,
            configurationManager);
    }

    private static async Task<Dictionary<string, string>> ReadFormAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = await request.Content!.ReadAsStringAsync(cancellationToken);

        return content
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => WebUtility.UrlDecode(part[0]),
                part => WebUtility.UrlDecode(part[1]),
                StringComparer.Ordinal);
    }

    private static void AssertClientSecret(string clientSecret)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(clientSecret);

        Assert.Equal(TeamId, token.Issuer);
        Assert.Equal("https://appleid.apple.com", token.Audiences.Single());
        Assert.Equal(ClientId, token.Subject);
        Assert.Equal(KeyId, token.Header.Kid);
        Assert.Equal("ES256", token.Header.Alg);
        Assert.True(token.ValidTo > DateTime.UtcNow);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
