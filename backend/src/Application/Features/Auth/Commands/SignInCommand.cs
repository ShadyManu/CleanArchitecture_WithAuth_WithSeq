using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Application.Common.Utilities;
using Application.Dtos.Auth.Response;
using Domain.Common.Constants;
using Domain.Common.Enums;
using Domain.Entities.Auth;

namespace Application.Features.Auth.Commands;

public record SignInCommand(
    ProviderEnum Provider,
    string IdToken,
    string DeviceId,
    string? AuthorizationCode
) : ICommand<AuthTokenResponse?>
{
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            return (false, ValidatorMessage.CannotBeEmpty(nameof(DeviceId)));
        }

        if (DeviceId.Length > DbConstraints.DeviceIdMaxLength)
        {
            return (false, ValidatorMessage.MaxLength(nameof(DeviceId), DbConstraints.DeviceIdMaxLength));
        }

        if (Provider == ProviderEnum.Apple && string.IsNullOrWhiteSpace(AuthorizationCode))
        {
            return (false, ValidatorMessage.CannotBeEmpty(nameof(AuthorizationCode)));
        }

        return string.IsNullOrWhiteSpace(IdToken)
            ? (false, ValidatorMessage.CannotBeEmpty(nameof(IdToken)))
            : (true, null);
    }
}

internal sealed class SignInCommandHandler : ICommandHandler<SignInCommand, AuthTokenResponse?>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRefreshTokenRepository _userRefreshTokenRepository;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IAppleAuthService _appleAuthService;
    private readonly ITokenService _tokenService;
    private readonly IProviderTokenProtector _providerTokenProtector;

    public SignInCommandHandler(
        IUserRepository userRepository,
        IUserRefreshTokenRepository userRefreshTokenRepository,
        IGoogleAuthService googleAuthService,
        IAppleAuthService appleAuthService,
        ITokenService tokenService,
        IProviderTokenProtector providerTokenProtector)
    {
        _userRepository = userRepository;
        _userRefreshTokenRepository = userRefreshTokenRepository;
        _googleAuthService = googleAuthService;
        _appleAuthService = appleAuthService;
        _tokenService = tokenService;
        _providerTokenProtector = providerTokenProtector;
    }

    public async Task<Result<AuthTokenResponse?>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        (bool isValid, string? email, string providerId) = request.Provider switch
        {
            ProviderEnum.Google => await _googleAuthService
                .ExtractEmailAndProviderIdAsync(request.IdToken),
            ProviderEnum.Apple => await _appleAuthService
                .ExtractEmailAndProviderIdAsync(request.IdToken, cancellationToken),
            _ => (false, string.Empty, string.Empty)
        };
        if (!isValid)
        {
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);
        }

        string? providerRefreshToken = null;
        if (request.Provider == ProviderEnum.Apple)
        {
            var appleResponse = await _appleAuthService.ExchangeCodeForTokenAsync(
                request.AuthorizationCode!,
                cancellationToken);

            if (!appleResponse.IsSuccess)
            {
                return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);
            }

            var (isExchangedTokenValid, exchangedEmail, exchangedProviderId) =
                await _appleAuthService.ExtractEmailAndProviderIdAsync(
                    appleResponse.IdToken!,
                    cancellationToken);

            if (!isExchangedTokenValid ||
                !string.Equals(providerId, exchangedProviderId, StringComparison.Ordinal))
            {
                return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);
            }

            providerRefreshToken = string.IsNullOrWhiteSpace(appleResponse.RefreshToken)
                ? null
                : _providerTokenProtector.Protect(appleResponse.RefreshToken);
            email ??= exchangedEmail;
        }

        var userExistingByProviderId = await _userRepository.GetByProviderIdAsync(
            request.Provider,
            providerId,
            cancellationToken);
        if (userExistingByProviderId is null)
        {
            // User does not exist in the platform, we will proceed with registration
            return await RegisterUserAsync(
                email,
                request.Provider,
                providerId,
                request.DeviceId,
                providerRefreshToken,
                cancellationToken);
        }

        // User exists, we will proceed with login, so we invalidate previous token and create a new one
        var refreshRaw = _tokenService.GenerateRefreshToken();
        var refreshHash = _tokenService.HashRefreshToken(refreshRaw);

        var tokenRow = userExistingByProviderId.UserRefreshTokens
            .SingleOrDefault(t => t.DeviceId == request.DeviceId);

        if (request.Provider == ProviderEnum.Apple)
        {
            // Keep exactly one current Apple credential. It is independent from the
            // application's per-device refresh tokens and is needed for account deletion.
            foreach (var userToken in userExistingByProviderId.UserRefreshTokens)
            {
                userToken.ProviderRefreshToken = null;
            }
        }

        if (tokenRow is null)
        {
            var newToken = new UserRefreshTokenEntity
            {
                UserId = userExistingByProviderId.Id,
                DeviceId = request.DeviceId,
                TokenHash = refreshHash,
                ProviderRefreshToken = providerRefreshToken,
                ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
                RevokedAt = null
            };
            
            await _userRefreshTokenRepository.AddAsync(newToken, cancellationToken);
        }
        else
        {
            tokenRow.TokenHash = refreshHash;
            tokenRow.ProviderRefreshToken = providerRefreshToken;
            tokenRow.ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc();
            tokenRow.RevokedAt = null;
        }

        var existingSaved = await _userRefreshTokenRepository.SaveChangesAsync(cancellationToken);
        if (existingSaved <= 0)
        {
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.SomethingWentWrong);
        }

        var existingAccessToken = _tokenService.CreateAccessToken(
            userExistingByProviderId.Id,
            providerId);
        return Result<AuthTokenResponse?>.Success(new AuthTokenResponse(
            userExistingByProviderId.Id,
            existingAccessToken,
            _tokenService.AccessTokenExpiresInSeconds,
            refreshRaw
        ));
    }

    private async Task<Result<AuthTokenResponse?>> RegisterUserAsync(
        string? email,
        ProviderEnum provider,
        string providerId,
        string deviceId,
        string? providerRefreshToken,
        CancellationToken cancellationToken)
    {
        if (EmailUtilities.IsEmailAddress(email))
        {
            var emailExists = await _userRepository.EmailExistsAsync(email, cancellationToken);
            if (emailExists)
            {
                return Result<AuthTokenResponse?>.Failure(ErrorMessage.EmailAlreadyExists);
            }
        }

        var user = new UserEntity
        {
            Email = email,
            Username = null, // Will be set during username selection step on frontend
            Provider = provider,
            ProviderId = providerId,
        };
        var refreshTokenRaw = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshTokenRaw);

        var refreshTokenEntity = new UserRefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ProviderRefreshToken = providerRefreshToken,
            ExpiresAt = _tokenService.GetRefreshTokenExpiryUtc(),
            DeviceId = deviceId,
            RevokedAt = null,
        };
        user.UserRefreshTokens.Add(refreshTokenEntity);
        await _userRepository.AddAsync(user, cancellationToken);

        var saved = await _userRepository.SaveChangesAsync(cancellationToken);
        if (saved <= 0)
        {
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.SomethingWentWrong);
        }

        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            providerId);
        return Result<AuthTokenResponse?>.Success(new AuthTokenResponse(
            user.Id,
            accessToken,
            _tokenService.AccessTokenExpiresInSeconds,
            refreshTokenRaw
        ));
    }
}
