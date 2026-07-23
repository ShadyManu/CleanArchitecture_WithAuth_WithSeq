using Application.Common.Interfaces.Auth;
using Application.Dtos.Auth.Request;
using Domain.Entities.Auth;
using Infrastructure.Data;
using Infrastructure.IntegrationTests.Utilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.Auth;

[Collection(IntegrationTestCollection.Name)]
public abstract partial class BaseAuthTest : BaseIntegrationTest<UserEntity>
{
    protected BaseAuthTest(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    protected HttpClient HttpClientWithMockedProviders { get; private set; } = null!;
    private Mock<IGoogleAuthService> GoogleAuthMock { get; } = new();
    protected Mock<IAppleAuthService> AppleAuthMock { get; } = new();

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        GoogleAuthMock
            .Setup(g => g.ExtractEmailAndProviderIdAsync(ValidIdToken))
            .ReturnsAsync((true, FakeEmail, FakeProviderId));

        GoogleAuthMock
            .Setup(g => g.ExtractEmailAndProviderIdAsync(InvalidIdToken))
            .ReturnsAsync((false, "", ""));

        AppleAuthMock
            .Setup(a => a.ExtractEmailAndProviderIdAsync(
                ValidAppleIdToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, FakeAppleEmail, FakeAppleProviderId));

        AppleAuthMock
            .Setup(a => a.ExchangeCodeForTokenAsync(
                ValidAppleAuthorizationCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppleTokenResponse
            {
                RefreshToken = FakeAppleRefreshToken,
                IdToken = ExchangedAppleIdToken
            });

        AppleAuthMock
            .Setup(a => a.ExtractEmailAndProviderIdAsync(
                ExchangedAppleIdToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, FakeAppleEmail, FakeAppleProviderId));

        HttpClientWithMockedProviders = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(GoogleAuthMock.Object);
                services.AddSingleton(AppleAuthMock.Object);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}

public abstract partial class BaseAuthTest
{
    protected const string Endpoint = "/api/auth";
    protected const string FakeProviderId = "google-provider-id-999";
    protected const string FakeEmail = "signin-test@cctemplate.com";
    protected const string ValidIdToken = "valid-google-id-token";
    protected const string InvalidIdToken = "invalid-token";
    protected const string ValidAppleIdToken = "valid-apple-id-token";
    private const string ExchangedAppleIdToken = "exchanged-apple-id-token";
    protected const string ValidAppleAuthorizationCode = "valid-apple-authorization-code";
    private const string FakeAppleProviderId = "apple-provider-id-999";
    private const string FakeAppleEmail = "apple-signin-test@privaterelay.appleid.com";
    protected const string FakeAppleRefreshToken = "apple-refresh-token";
}
