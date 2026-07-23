using Application.Common.Result;
using Application.Features.Auth.Commands;
using Domain.Common.Constants;
using Domain.Common.Enums;
using Xunit;

namespace Application.UnitTests.Tests.Auth;

public class AuthCommandValidationTests
{
    public static TheoryData<string, string, string> InvalidSignInInputs =>
        new()
        {
            {
                "",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.IdToken))
            },
            {
                " ",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.IdToken))
            },
            {
                "token",
                "",
                ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.DeviceId))
            },
            {
                "token",
                new string('*', DbConstraints.DeviceIdMaxLength + 1),
                ValidatorMessage.MaxLength(
                    nameof(SignInCommand.DeviceId),
                    DbConstraints.DeviceIdMaxLength)
            }
        };

    public static TheoryData<string, string, string> InvalidRefreshTokenInputs =>
        new()
        {
            {
                "",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(RefreshTokenCommand.RefreshToken))
            },
            {
                " ",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(RefreshTokenCommand.RefreshToken))
            },
            {
                "token",
                "",
                ValidatorMessage.CannotBeEmpty(nameof(RefreshTokenCommand.DeviceId))
            },
            {
                "token",
                new string('*', DbConstraints.DeviceIdMaxLength + 1),
                ValidatorMessage.MaxLength(
                    nameof(RefreshTokenCommand.DeviceId),
                    DbConstraints.DeviceIdMaxLength)
            }
        };

    public static TheoryData<string, string, string> InvalidLogoutInputs =>
        new()
        {
            {
                "",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(LogoutCommand.RefreshToken))
            },
            {
                " ",
                "device",
                ValidatorMessage.CannotBeEmpty(nameof(LogoutCommand.RefreshToken))
            },
            {
                "token",
                "",
                ValidatorMessage.CannotBeEmpty(nameof(LogoutCommand.DeviceId))
            },
            {
                "token",
                new string('*', DbConstraints.DeviceIdMaxLength + 1),
                ValidatorMessage.MaxLength(
                    nameof(LogoutCommand.DeviceId),
                    DbConstraints.DeviceIdMaxLength)
            }
        };

    [Theory]
    [MemberData(nameof(InvalidSignInInputs))]
    public void SignInValidate_ShouldRejectInvalidInput(
        string token,
        string deviceId,
        string expectedMessage)
    {
        // Arrange
        var command = new SignInCommand(ProviderEnum.Google, token, deviceId, null);

        // Act
        var result = command.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void SignInValidate_ShouldRejectWhitespaceAuthorizationCode()
    {
        // Arrange
        var command = new SignInCommand(ProviderEnum.Apple, "token", "device", " ");

        // Act
        var result = command.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.AuthorizationCode)), result.ErrorMessage);
    }

    [Fact]
    public void SignInValidate_ShouldRequireAuthorizationCodeForApple()
    {
        // Arrange
        var command = new SignInCommand(ProviderEnum.Apple, "token", "device", null);

        // Act
        var result = command.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidatorMessage.CannotBeEmpty(nameof(SignInCommand.AuthorizationCode)), result.ErrorMessage);
    }

    [Fact]
    public void SignInValidate_ShouldAcceptValidInput()
    {
        // Arrange
        var command = new SignInCommand(ProviderEnum.Google, "token", "device", null);

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [MemberData(nameof(InvalidRefreshTokenInputs))]
    public void RefreshTokenValidate_ShouldRejectInvalidInput(
        string token,
        string deviceId,
        string expectedMessage)
    {
        // Arrange
        var command = new RefreshTokenCommand(token, deviceId);

        // Act
        var result = command.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Theory]
    [MemberData(nameof(InvalidLogoutInputs))]
    public void LogoutValidate_ShouldRejectInvalidInput(
        string token,
        string deviceId,
        string expectedMessage)
    {
        // Arrange
        var command = new LogoutCommand(token, deviceId);

        // Act
        var result = command.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public void AuthCommandsValidate_ShouldAcceptDeviceIdAtMaximumLength()
    {
        // Arrange
        var deviceId = new string('*', DbConstraints.DeviceIdMaxLength);

        // Act
        var signInResult = new SignInCommand(
            ProviderEnum.Google,
            "token",
            deviceId,
            null).Validate();
        var refreshTokenResult = new RefreshTokenCommand("token", deviceId).Validate();
        var logoutResult = new LogoutCommand("token", deviceId).Validate();

        // Assert
        Assert.True(signInResult.IsValid);
        Assert.True(refreshTokenResult.IsValid);
        Assert.True(logoutResult.IsValid);
    }
}
