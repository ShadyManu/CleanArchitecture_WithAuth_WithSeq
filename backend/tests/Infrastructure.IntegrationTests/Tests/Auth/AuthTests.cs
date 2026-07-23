using System.Net.Http.Json;
using Application.Common.Result;
using Application.Dtos.Auth.Request;
using Application.Dtos.Auth.Response;
using Application.Features.Auth.Commands;
using Domain.Common.Enums;
using Domain.Entities.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.Auth;

public class AuthTests : BaseAuthTest
{
    public AuthTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    [Fact]
    public async Task SignIn_ShouldRegisterAndReturnTokens_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new SignInRequest
        {
            Provider = ProviderEnum.Google,
            IdToken = ValidIdToken,
            DeviceId = "integration-device-1"
        };

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        Assert.NotEqual(Guid.Empty, result.Data.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
        Assert.True(result.Data.ExpiresInSeconds > 0);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users
            .Include(u => u.UserRefreshTokens)
            .FirstOrDefaultAsync(u => u.ProviderId == FakeProviderId, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.Equal(ProviderEnum.Google, user.Provider);
        Assert.Single(user.UserRefreshTokens);
    }

    [Fact]
    public async Task SignIn_ShouldLoginAndReturnTokens_WhenUserAlreadyExists()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingUser = new UserEntity
            {
                Email = FakeEmail,
                Provider = ProviderEnum.Google,
                ProviderId = FakeProviderId
            };
            await context.Users.AddAsync(existingUser, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var request = new SignInRequest
        {
            Provider = ProviderEnum.Google,
            IdToken = ValidIdToken,
            DeviceId = "integration-device-2"
        };

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);
        Assert.NotNull(result?.Data);
        Assert.Null(result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Data.RefreshToken));
    }

    [Fact]
    public async Task SignIn_ShouldReturnUnauthorized_WhenIdTokenIsInvalid()
    {
        // Arrange
        var request = new SignInRequest
        {
            Provider = ProviderEnum.Google,
            IdToken = InvalidIdToken,
            DeviceId = "integration-device-3"
        };

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result?.Error);
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error.Message);
    }

    [Fact]
    public async Task SignIn_ShouldReturnValidationError_WhenIdTokenIsEmpty()
    {
        // Arrange
        var request = new SignInRequest
        {
            Provider = ProviderEnum.Google,
            IdToken = "",
            DeviceId = "integration-device-4"
        };

        // Act
        var response = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result?.Error);
        Assert.Equal(
            ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.IdToken)),
            result.Error.Message);
    }

    [Fact]
    public async Task SignIn_ShouldRotateRefreshToken_WhenSameDeviceSignsInAgain()
    {
        // Arrange
        var request = new SignInRequest
        {
            Provider = ProviderEnum.Google,
            IdToken = ValidIdToken,
            DeviceId = "integration-device-rotate"
        };

        // Act
        var firstResponse = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);
        firstResponse.EnsureSuccessStatusCode();
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);

        var secondResponse = await HttpClientWithMockedProviders.PostAsJsonAsync($"{Endpoint}/sign-in", request, TestContext.Current.CancellationToken);
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Result<AuthTokenResponse>>(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstResult?.Data);
        Assert.NotNull(secondResult?.Data);
        Assert.NotEqual(firstResult.Data.RefreshToken, secondResult.Data.RefreshToken);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users
            .Include(u => u.UserRefreshTokens)
            .FirstOrDefaultAsync(u => u.ProviderId == FakeProviderId, TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        var deviceTokens = user.UserRefreshTokens
            .Where(t => t.DeviceId == "integration-device-rotate")
            .ToList();
        Assert.Single(deviceTokens);
    }
}
