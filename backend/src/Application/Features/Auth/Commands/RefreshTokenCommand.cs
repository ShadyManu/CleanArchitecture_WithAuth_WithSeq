using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Application.Dtos.Auth.Response;
using Domain.Common.Constants;

namespace Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string DeviceId
) : ICommand<AuthTokenResponse?>
{
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(RefreshToken))
            return (false, ValidatorMessage.CannotBeEmpty(nameof(RefreshToken)));

        if (string.IsNullOrWhiteSpace(DeviceId))
            return (false, ValidatorMessage.CannotBeEmpty(nameof(DeviceId)));

        return DeviceId.Length > DbConstraints.DeviceIdMaxLength 
            ? (false, ValidatorMessage.MaxLength(nameof(DeviceId), DbConstraints.DeviceIdMaxLength)) 
            : (true, null);
    }
}

internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthTokenResponse?>
{
    private readonly IUserRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IUserRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthTokenResponse?>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var deviceId = request.DeviceId;

        // 1) Hash incoming refresh token
        var incomingHash = _tokenService.HashRefreshToken(request.RefreshToken);

        // 2) Load the current token and its user without tracking.
        var stored = await _refreshTokenRepository.GetByHashWithUserAsync(
            incomingHash,
            cancellationToken);
        if (stored is null)
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);

        // 3) Validate token status
        if (stored.RevokedAt is not null)
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (stored.ExpiresAt <= utcNow)
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);

        // 4) Ensure refresh token is used only from same device
        if (stored.DeviceId != deviceId)
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);

        var user = stored.User;

        // 5) Rotate with one conditional database update. If another request has
        // already consumed this token, the old hash no longer matches and this
        // request fails without issuing an access token.
        var newRefreshRaw = _tokenService.GenerateRefreshToken();
        var newRefreshHash = _tokenService.HashRefreshToken(newRefreshRaw);
        var rotated = await _refreshTokenRepository.TryRotateAsync(
            stored.Id,
            stored.UserId,
            incomingHash,
            newRefreshHash,
            _tokenService.GetRefreshTokenExpiryUtc(),
            utcNow,
            cancellationToken);
        if (!rotated)
            return Result<AuthTokenResponse?>.Failure(ErrorMessage.UnauthorizedAction);

        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.ProviderId);

        return Result<AuthTokenResponse?>.Success(new AuthTokenResponse(
            user.Id,
            accessToken,
            _tokenService.AccessTokenExpiresInSeconds,
            newRefreshRaw
        ));
    }
}
