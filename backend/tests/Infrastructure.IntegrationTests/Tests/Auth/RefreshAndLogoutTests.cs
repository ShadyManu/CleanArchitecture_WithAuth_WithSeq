using System.Net;
using System.Net.Http.Json;
using Application.Common.Result;
using Application.Dtos.Auth.Request;
using Application.Dtos.Auth.Response;
using Domain.Common.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.Auth;

public sealed class RefreshAndLogoutTests : BaseAuthTest
{
    private const string DeviceId = "refresh-logout-device";
    private const string OtherDeviceId = "refresh-logout-other-device";

    public RefreshAndLogoutTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task RefreshToken_ShouldRotateTokenAndInvalidatePreviousValue()
    {
        // Arrange
        var signedIn = await SignInAsync();

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest { RefreshToken = signedIn.RefreshToken, DeviceId = DeviceId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        var replay = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest { RefreshToken = signedIn.RefreshToken, DeviceId = DeviceId },
            TestContext.Current.CancellationToken);
        var replayResult = await replay.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(refreshed?.Data);
        Assert.NotEqual(signedIn.RefreshToken, refreshed.Data.RefreshToken);
        Assert.Equal(ErrorMessage.UnauthorizedAction, replayResult?.Error?.Message);
    }

    [Fact]
    public async Task RefreshToken_ShouldAllowOnlyOneConcurrentRotation()
    {
        // Arrange
        var signedIn = await SignInAsync();
        var request = new RefreshTokenRequest
        {
            RefreshToken = signedIn.RefreshToken,
            DeviceId = DeviceId
        };

        // Act
        var responses = await Task.WhenAll(
            HttpClientWithMockedProviders.PostAsJsonAsync(
                $"{Endpoint}/refresh-token",
                request,
                TestContext.Current.CancellationToken),
            HttpClientWithMockedProviders.PostAsJsonAsync(
                $"{Endpoint}/refresh-token",
                request,
                TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
                TestContext.Current.CancellationToken)));

        // Assert
        Assert.Single(results, result => result?.Data is not null);
        var rejected = Assert.Single(results, result => result?.Error is not null);
        Assert.Equal(ErrorMessage.UnauthorizedAction, rejected?.Error?.Message);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenDeviceDiffers()
    {
        // Arrange
        var signedIn = await SignInAsync();

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest { RefreshToken = signedIn.RefreshToken, DeviceId = "other-device" },
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result?.Error?.Message);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        // Arrange
        var signedIn = await SignInAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var token = await db.UserRefreshTokens.SingleAsync(
                x => x.DeviceId == DeviceId, TestContext.Current.CancellationToken);
            token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest { RefreshToken = signedIn.RefreshToken, DeviceId = DeviceId },
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result?.Error?.Message);
    }

    [Fact]
    public async Task Logout_ShouldRequireAuthentication()
    {
        // Arrange
        var request = new LogoutRequest { RefreshToken = "token", DeviceId = DeviceId };

        // Act
        var response = await _httpClientAnonymous.PostAsJsonAsync(
            $"{Endpoint}/logout",
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeOnlyCurrentSessionAndKeepAccessTokenValid()
    {
        // Arrange
        var signedIn = await SignInAsync();
        var otherSession = await SignInAsync(OtherDeviceId);
        SetBearerToken(HttpClientWithMockedProviders, signedIn.AccessToken);
        var request = new LogoutRequest { RefreshToken = signedIn.RefreshToken, DeviceId = DeviceId };

        // Act
        var first = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/logout", request, TestContext.Current.CancellationToken);
        first.EnsureSuccessStatusCode();
        var firstResult = await first.Content.ReadFromJsonAsync<Result<bool>>(
            TestContext.Current.CancellationToken);

        var second = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/logout", request, TestContext.Current.CancellationToken);

        var currentSessionRefresh = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest
            {
                RefreshToken = signedIn.RefreshToken,
                DeviceId = DeviceId
            },
            TestContext.Current.CancellationToken);
        var currentSessionRefreshResult =
            await currentSessionRefresh.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
                TestContext.Current.CancellationToken);

        var otherSessionRefresh = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest
            {
                RefreshToken = otherSession.RefreshToken,
                DeviceId = OtherDeviceId
            },
            TestContext.Current.CancellationToken);
        var otherSessionRefreshResult =
            await otherSessionRefresh.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
                TestContext.Current.CancellationToken);

        // Assert
        Assert.True(firstResult?.Data);
        second.EnsureSuccessStatusCode();
        Assert.Equal(ErrorMessage.UnauthorizedAction, currentSessionRefreshResult?.Error?.Message);
        Assert.NotNull(otherSessionRefreshResult?.Data);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currentSession = await db.UserRefreshTokens.SingleAsync(
            x => x.DeviceId == DeviceId, TestContext.Current.CancellationToken);
        var otherStoredSession = await db.UserRefreshTokens.SingleAsync(
            x => x.DeviceId == OtherDeviceId, TestContext.Current.CancellationToken);
        Assert.NotNull(currentSession.RevokedAt);
        Assert.Null(otherStoredSession.RevokedAt);
    }

    [Fact]
    public async Task LogoutAll_ShouldRequireAuthentication()
    {
        // Act
        var response = await _httpClientAnonymous.PostAsync(
            $"{Endpoint}/logout-all",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeEveryRefreshTokenAndKeepAccessTokenValid()
    {
        // Arrange
        var firstSession = await SignInAsync();
        var secondSession = await SignInAsync(OtherDeviceId);
        SetBearerToken(HttpClientWithMockedProviders, firstSession.AccessToken);

        // Act
        var firstLogout = await HttpClientWithMockedProviders.PostAsync(
            $"{Endpoint}/logout-all",
            null,
            TestContext.Current.CancellationToken);
        var firstLogoutResult = await firstLogout.Content.ReadFromJsonAsync<Result<bool>>(
            TestContext.Current.CancellationToken);

        var secondLogout = await HttpClientWithMockedProviders.PostAsync(
            $"{Endpoint}/logout-all",
            null,
            TestContext.Current.CancellationToken);

        var firstRefresh = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest
            {
                RefreshToken = firstSession.RefreshToken,
                DeviceId = DeviceId
            },
            TestContext.Current.CancellationToken);
        var firstRefreshResult = await firstRefresh.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        var secondRefresh = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/refresh-token",
            new RefreshTokenRequest
            {
                RefreshToken = secondSession.RefreshToken,
                DeviceId = OtherDeviceId
            },
            TestContext.Current.CancellationToken);
        var secondRefreshResult = await secondRefresh.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);

        // Assert
        firstLogout.EnsureSuccessStatusCode();
        secondLogout.EnsureSuccessStatusCode();
        Assert.True(firstLogoutResult?.Data);
        Assert.Equal(ErrorMessage.UnauthorizedAction, firstRefreshResult?.Error?.Message);
        Assert.Equal(ErrorMessage.UnauthorizedAction, secondRefreshResult?.Error?.Message);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessions = await db.UserRefreshTokens
            .Where(token => token.User.ProviderId == FakeProviderId)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, session => Assert.NotNull(session.RevokedAt));
    }

    private async Task<AuthTokenResponse> SignInAsync(string deviceId = DeviceId)
    {
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync(
            $"{Endpoint}/sign-in",
            new SignInRequest
            {
                Provider = ProviderEnum.Google,
                IdToken = ValidIdToken,
                DeviceId = deviceId
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(
            TestContext.Current.CancellationToken);
        return Assert.IsType<AuthTokenResponse>(result?.Data);
    }
}
