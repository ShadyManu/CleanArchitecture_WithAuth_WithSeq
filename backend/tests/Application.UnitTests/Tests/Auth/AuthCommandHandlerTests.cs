using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Application.Dtos.Auth.Request;
using Application.Features.Auth.Commands;
using Domain.Common.Enums;
using Domain.Entities.Auth;
using Moq;
using Xunit;

namespace Application.UnitTests.Tests.Auth;

public class AuthCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IGoogleAuthService> _google = new();
    private readonly Mock<IAppleAuthService> _apple = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IUser> _currentUser = new();
    private readonly IProviderTokenProtector _providerTokenProtector =
        new TestProviderTokenProtector();

    public AuthCommandHandlerTests()
    {
        _tokens.SetupGet(x => x.AccessTokenExpiresInSeconds).Returns(900);
        _tokens.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
        _tokens.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns<string>(x => $"hash:{x}");
        _tokens.Setup(x => x.GetRefreshTokenExpiryUtc()).Returns(DateTime.UtcNow.AddDays(7));
        _tokens.Setup(x => x.CreateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<string>()))
            .Returns("access");
        _currentUser.SetupGet(x => x.Id).Returns(UserId);
    }

    [Fact]
    public async Task SignIn_ShouldReturnUnauthorized_WhenProviderTokenIsInvalid()
    {
        // Arrange
        _google.Setup(x => x.ExtractEmailAndProviderIdAsync("invalid"))
            .ReturnsAsync((false, null, ""));
        var handler = CreateSignInHandler();

        // Act
        var result = await handler.Handle(
            new SignInCommand(ProviderEnum.Google, "invalid", "device", null), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _users.Verify(x => x.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignIn_ShouldRegisterUserAndToken_WhenUserDoesNotExist()
    {
        // Arrange
        _google.Setup(x => x.ExtractEmailAndProviderIdAsync("valid"))
            .ReturnsAsync((true, "user@example.com", "provider"));
        _users.Setup(x => x.GetByProviderIdAsync(ProviderEnum.Google, "provider", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);
        _users.Setup(x => x.EmailExistsAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Google, "valid", "device", null), CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.Equal("new-refresh", result.Data?.RefreshToken);
        _users.Verify(x => x.AddAsync(
            It.Is<UserEntity>(u => u.ProviderId == "provider" &&
                                   u.UserRefreshTokens.Single().DeviceId == "device"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignIn_ShouldRejectDuplicateEmail()
    {
        // Arrange
        _google.Setup(x => x.ExtractEmailAndProviderIdAsync("valid"))
            .ReturnsAsync((true, "user@example.com", "provider"));
        _users.Setup(x => x.EmailExistsAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Google, "valid", "device", null), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.EmailAlreadyExists, result.Error?.Message);
    }

    [Fact]
    public async Task SignIn_ShouldRegisterAppleUserOnlyAfterCodeExchangeIsValidated()
    {
        // Arrange
        SetupValidAppleAuthentication();
        _users.Setup(x => x.GetByProviderIdAsync(
                ProviderEnum.Apple,
                "apple-provider",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);
        _users.Setup(x => x.EmailExistsAsync("apple@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Apple, "apple-id-token", "device", "apple-code"),
            CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        _users.Verify(x => x.AddAsync(
            It.Is<UserEntity>(u =>
                u.Email == "apple@example.com" &&
                u.ProviderId == "apple-provider" &&
                u.UserRefreshTokens.Single().ProviderRefreshToken == "protected:apple-refresh-token"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignIn_ShouldNotCreateAppleUser_WhenCodeExchangeFails()
    {
        // Arrange
        _apple.Setup(x => x.ExtractEmailAndProviderIdAsync(
                "apple-id-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "apple@example.com", "apple-provider"));
        _apple.Setup(x => x.ExchangeCodeForTokenAsync(
                "apple-code",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppleTokenResponse { Error = "invalid_grant" });

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Apple, "apple-id-token", "device", "apple-code"),
            CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _users.Verify(
            x => x.AddAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignIn_ShouldRejectAppleCodeBelongingToDifferentUser()
    {
        // Arrange
        SetupValidAppleAuthentication(exchangedProviderId: "different-apple-provider");

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Apple, "apple-id-token", "device", "apple-code"),
            CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _users.Verify(
            x => x.GetByProviderIdAsync(
                It.IsAny<ProviderEnum>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SignIn_ShouldReplaceStoredAppleCredential_WhenUserAlreadyExists()
    {
        // Arrange
        SetupValidAppleAuthentication();
        var oldAppleToken = Token(
            "old-hash",
            "old-device",
            providerRefreshToken: "old-apple-refresh");
        var appleUser = new UserEntity
        {
            Id = UserId,
            Email = "apple@example.com",
            Provider = ProviderEnum.Apple,
            ProviderId = "apple-provider",
            UserRefreshTokens = [oldAppleToken]
        };
        _users.Setup(x => x.GetByProviderIdAsync(
                ProviderEnum.Apple,
                "apple-provider",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(appleUser);
        _refreshTokens.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Apple, "apple-id-token", "new-device", "apple-code"),
            CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.Null(oldAppleToken.ProviderRefreshToken);
        _refreshTokens.Verify(x => x.AddAsync(
            It.Is<UserRefreshTokenEntity>(t =>
                t.DeviceId == "new-device" &&
                t.ProviderRefreshToken == "protected:apple-refresh-token"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignIn_ShouldRotateExistingDeviceToken()
    {
        // Arrange
        var stored = Token("old", "device");
        var user = User(stored);
        _google.Setup(x => x.ExtractEmailAndProviderIdAsync("valid"))
            .ReturnsAsync((true, user.Email, user.ProviderId));
        _users.Setup(x => x.GetByProviderIdAsync(ProviderEnum.Google, user.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _refreshTokens.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await CreateSignInHandler().Handle(
            new SignInCommand(ProviderEnum.Google, "valid", "device", null), CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.Equal("hash:new-refresh", stored.TokenHash);
        _refreshTokens.Verify(x => x.AddAsync(It.IsAny<UserRefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("wrong-device")]
    public async Task RefreshToken_ShouldRejectInvalidTokenState(string state)
    {
        // Arrange
        UserRefreshTokenEntity? stored = state == "missing" ? null : Token("hash:old", "device");
        if (stored is not null)
        {
            if (state == "revoked") stored.RevokedAt = DateTime.UtcNow;
            if (state == "expired") stored.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        }
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync("hash:old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var device = state == "wrong-device" ? "other" : "device";

        // Act
        var result = await new RefreshTokenCommandHandler(
                _refreshTokens.Object,
                _tokens.Object,
                TimeProvider.System)
            .Handle(new RefreshTokenCommand("old", device), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateValidToken()
    {
        // Arrange
        var stored = Token("hash:old", "device", User());
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync("hash:old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _refreshTokens.Setup(x => x.TryRotateAsync(
                stored.Id,
                stored.UserId,
                "hash:old",
                "hash:new-refresh",
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await new RefreshTokenCommandHandler(
                _refreshTokens.Object,
                _tokens.Object,
                TimeProvider.System)
            .Handle(new RefreshTokenCommand("old", "device"), CancellationToken.None);

        // Assert
        Assert.Null(result.Error);
        Assert.Equal("new-refresh", result.Data?.RefreshToken);
        _refreshTokens.Verify(x => x.TryRotateAsync(
            stored.Id,
            stored.UserId,
            "hash:old",
            "hash:new-refresh",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_ShouldRejectConcurrentReplay_WhenAtomicRotationLoses()
    {
        var stored = Token("hash:old", "device", User());
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync(
                "hash:old",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _refreshTokens.Setup(x => x.TryRotateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await new RefreshTokenCommandHandler(
                _refreshTokens.Object,
                _tokens.Object,
                TimeProvider.System)
            .Handle(new RefreshTokenCommand("old", "device"), CancellationToken.None);

        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _tokens.Verify(x => x.CreateAccessToken(
            It.IsAny<Guid>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ShouldBeIdempotent_WhenTokenDoesNotExist()
    {
        // Arrange
        var handler = CreateLogoutHandler();
        var command = new LogoutCommand("missing", "device");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Logout_ShouldRejectDifferentDevice()
    {
        // Arrange
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync("hash:old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Token("hash:old", "device"));

        // Act
        var result = await CreateLogoutHandler()
            .Handle(new LogoutCommand("old", "other"), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
    }

    [Fact]
    public async Task Logout_ShouldRejectTokenOwnedByAnotherUser()
    {
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync(
                "hash:old",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRefreshTokenEntity
            {
                UserId = otherUserId,
                DeviceId = "device",
                TokenHash = "hash:old",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });

        var result = await CreateLogoutHandler()
            .Handle(new LogoutCommand("old", "device"), CancellationToken.None);

        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _refreshTokens.Verify(x => x.TryRevokeSessionAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ShouldRevokeValidToken()
    {
        // Arrange
        var stored = Token("hash:old", "device");
        _refreshTokens.Setup(x => x.GetByHashWithUserAsync("hash:old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        _refreshTokens.Setup(x => x.TryRevokeSessionAsync(
                stored.Id,
                UserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await CreateLogoutHandler()
            .Handle(new LogoutCommand("old", "device"), CancellationToken.None);

        // Assert
        Assert.True(result.Data);
        _refreshTokens.Verify(x => x.TryRevokeSessionAsync(
            stored.Id,
            UserId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAll_ShouldRejectAnonymousUser()
    {
        // Arrange
        _currentUser.SetupGet(x => x.Id).Returns(Guid.Empty);

        // Act
        var result = await CreateLogoutAllHandler()
            .Handle(new LogoutAllCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
        _refreshTokens.Verify(x => x.RevokeAllSessionsAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeEverySessionForCurrentUser()
    {
        // Arrange
        _refreshTokens.Setup(x => x.RevokeAllSessionsAsync(
                UserId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await CreateLogoutAllHandler()
            .Handle(new LogoutAllCommand(), CancellationToken.None);

        // Assert
        Assert.True(result.Data);
        Assert.Null(result.Error);
        _refreshTokens.Verify(x => x.RevokeAllSessionsAsync(
            UserId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_ShouldRejectAnonymousUser()
    {
        // Arrange
        _currentUser.SetupGet(x => x.Id).Returns(Guid.Empty);

        // Act
        var result = await CreateDeleteHandler().Handle(new DeleteUserCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.UnauthorizedAction, result.Error?.Message);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _currentUser.SetupGet(x => x.Id).Returns(UserId);

        // Act
        var result = await CreateDeleteHandler().Handle(new DeleteUserCommand(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.NotFound, result.Error?.Message);
    }

    [Fact]
    public async Task DeleteUser_ShouldRevokeAppleTokenAndDeleteUser()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = UserId,
            Email = "apple@example.com",
            Provider = ProviderEnum.Apple,
            ProviderId = "apple",
            UserRefreshTokens = [Token("hash", "device", providerRefreshToken: "protected:apple-refresh")]
        };
        _currentUser.SetupGet(x => x.Id).Returns(UserId);
        _users.Setup(x => x.GetByIdWithUserRefreshTokenAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apple.Setup(x => x.RevokeTokenAsync("apple-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _users.Setup(x => x.DeleteAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await CreateDeleteHandler().Handle(new DeleteUserCommand(), CancellationToken.None);

        // Assert
        Assert.True(result.Data);
        _apple.Verify(x => x.RevokeTokenAsync("apple-refresh", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_ShouldNotDeleteAppleUser_WhenCredentialIsMissing()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = UserId,
            Email = "apple@example.com",
            Provider = ProviderEnum.Apple,
            ProviderId = "apple",
            UserRefreshTokens = [Token("hash", "device")]
        };
        _currentUser.SetupGet(x => x.Id).Returns(UserId);
        _users.Setup(x => x.GetByIdWithUserRefreshTokenAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await CreateDeleteHandler().Handle(
            new DeleteUserCommand(),
            CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.AppleReauthorizationRequired, result.Error?.Message);
        _users.Verify(
            x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUser_ShouldNotDeleteAppleUser_WhenRevocationFails()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = UserId,
            Email = "apple@example.com",
            Provider = ProviderEnum.Apple,
            ProviderId = "apple",
            UserRefreshTokens = [Token(
                "hash",
                "device",
                providerRefreshToken: "protected:apple-refresh")]
        };
        _currentUser.SetupGet(x => x.Id).Returns(UserId);
        _users.Setup(x => x.GetByIdWithUserRefreshTokenAsync(
                UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _apple.Setup(x => x.RevokeTokenAsync(
                "apple-refresh",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await CreateDeleteHandler().Handle(
            new DeleteUserCommand(),
            CancellationToken.None);

        // Assert
        Assert.Equal(ErrorMessage.AppleRevokeFailed, result.Error?.Message);
        _users.Verify(
            x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private SignInCommandHandler CreateSignInHandler() =>
        new(
            _users.Object,
            _refreshTokens.Object,
            _google.Object,
            _apple.Object,
            _tokens.Object,
            _providerTokenProtector);

    private LogoutCommandHandler CreateLogoutHandler() =>
        new(
            _currentUser.Object,
            _refreshTokens.Object,
            _tokens.Object,
            TimeProvider.System);

    private LogoutAllCommandHandler CreateLogoutAllHandler() =>
        new(
            _currentUser.Object,
            _refreshTokens.Object,
            TimeProvider.System);

    private DeleteUserCommandHandler CreateDeleteHandler() =>
        new(
            _currentUser.Object,
            _users.Object,
            _apple.Object,
            _providerTokenProtector);

    private void SetupValidAppleAuthentication(
        string exchangedProviderId = "apple-provider")
    {
        _apple.Setup(x => x.ExtractEmailAndProviderIdAsync(
                "apple-id-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, "apple-provider"));
        _apple.Setup(x => x.ExchangeCodeForTokenAsync(
                "apple-code",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppleTokenResponse
            {
                RefreshToken = "apple-refresh-token",
                IdToken = "exchanged-apple-id-token"
            });
        _apple.Setup(x => x.ExtractEmailAndProviderIdAsync(
                "exchanged-apple-id-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "apple@example.com", exchangedProviderId));
    }

    private static UserEntity User(params UserRefreshTokenEntity[] tokens) =>
        new()
        {
            Id = UserId,
            Email = "user@example.com",
            Provider = ProviderEnum.Google,
            ProviderId = "provider",
            UserRefreshTokens = tokens
        };

    private static UserRefreshTokenEntity Token(
        string hash,
        string device,
        UserEntity? user = null,
        string? providerRefreshToken = null) =>
        new()
        {
            UserId = user?.Id ?? UserId,
            User = user ?? null!,
            DeviceId = device,
            TokenHash = hash,
            ProviderRefreshToken = providerRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

    private sealed class TestProviderTokenProtector : IProviderTokenProtector
    {
        public string Protect(string providerToken) => $"protected:{providerToken}";

        public bool TryUnprotect(string protectedProviderToken, out string? providerToken)
        {
            const string prefix = "protected:";
            if (!protectedProviderToken.StartsWith(prefix, StringComparison.Ordinal))
            {
                providerToken = null;
                return false;
            }

            providerToken = protectedProviderToken[prefix.Length..];
            return true;
        }
    }
}
