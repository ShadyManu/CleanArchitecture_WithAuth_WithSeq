using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces.Auth;
using Application.Common.Result;
using Application.Dtos.Auth.Request;
using Application.Dtos.Auth.Response;
using Domain.Common.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.Auth;

public class DeleteAccountTests : BaseAuthTest
{
    public DeleteAccountTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task DeleteProfile_ShouldRequireAuthentication()
    {
        // Arrange
        var endpoint = $"{Endpoint}/delete-user";

        // Act
        var response = await _httpClientAnonymous.DeleteAsync(
            endpoint, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProfile_ShouldDeleteAuthenticatedUserAndTokens()
    {
        // Arrange
        var signIn = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/sign-in",
            new SignInRequest
            {
                Provider = ProviderEnum.Google,
                IdToken = ValidIdToken,
                DeviceId = "delete-user-device"
            },
            TestContext.Current.CancellationToken);
        signIn.EnsureSuccessStatusCode();
        var signInResult = await signIn.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);
        var tokens = Assert.IsType<AuthTokenResponse>(signInResult?.Data);
        SetBearerToken(HttpClientWithMockedProviders, tokens.AccessToken);

        // Act
        var response = await HttpClientWithMockedProviders.DeleteAsync(
            $"{Endpoint}/delete-user", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result?.Data);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Users.AnyAsync(
            x => x.Id == tokens.UserId, TestContext.Current.CancellationToken));
        Assert.False(await db.UserRefreshTokens.AnyAsync(
            x => x.UserId == tokens.UserId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteProfile_ShouldRevokeAppleAuthorizationBeforeDeletingUser()
    {
        // Arrange
        AppleAuthMock
            .Setup(a => a.RevokeTokenAsync(
                FakeAppleRefreshToken,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var signIn = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/sign-in",
            new SignInRequest
            {
                Provider = ProviderEnum.Apple,
                IdToken = ValidAppleIdToken,
                AuthorizationCode = ValidAppleAuthorizationCode,
                DeviceId = "delete-apple-user-device"
            },
            TestContext.Current.CancellationToken);
        signIn.EnsureSuccessStatusCode();

        var signInResult = await signIn.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);
        var tokens = Assert.IsType<AuthTokenResponse>(signInResult?.Data);

        using (var signInScope = _factory.Services.CreateScope())
        {
            var signInDb = signInScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var storedAppleToken = await signInDb.UserRefreshTokens
                .Where(x => x.UserId == tokens.UserId)
                .Select(x => x.ProviderRefreshToken)
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(storedAppleToken);
            Assert.NotEqual(FakeAppleRefreshToken, storedAppleToken);

            var protector = signInScope.ServiceProvider
                .GetRequiredService<IProviderTokenProtector>();
            Assert.True(protector.TryUnprotect(storedAppleToken, out var plaintextToken));
            Assert.Equal(FakeAppleRefreshToken, plaintextToken);
        }

        SetBearerToken(HttpClientWithMockedProviders, tokens.AccessToken);

        // Act
        var response = await HttpClientWithMockedProviders.DeleteAsync(
            $"{Endpoint}/delete-user",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<bool>>(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result?.Data);
        AppleAuthMock.Verify(
            a => a.RevokeTokenAsync(
                FakeAppleRefreshToken,
                It.IsAny<CancellationToken>()),
            Times.Once);

        using var deleteScope = _factory.Services.CreateScope();
        var deleteDb = deleteScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await deleteDb.Users.AnyAsync(
            x => x.Id == tokens.UserId,
            TestContext.Current.CancellationToken));
        Assert.False(await deleteDb.UserRefreshTokens.AnyAsync(
            x => x.UserId == tokens.UserId,
            TestContext.Current.CancellationToken));
    }
}
