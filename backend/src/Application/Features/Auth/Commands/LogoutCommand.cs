using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Domain.Common.Constants;

namespace Application.Features.Auth.Commands;

public record LogoutCommand(
    string RefreshToken,
    string DeviceId
) : ICommand<bool>
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

internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly IUser _currentUser;
    private readonly IUserRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public LogoutCommandHandler(
        IUser currentUser,
        IUserRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        if (userId == Guid.Empty)
            return Result<bool>.Failure(ErrorMessage.UnauthorizedAction);

        var incomingHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByHashWithUserAsync(
            incomingHash,
            cancellationToken);

        if (stored is null)
            return Result<bool>.Success(true);

        var isNotAuthorized =
            stored.UserId != userId ||
            stored.DeviceId != request.DeviceId;
        if (isNotAuthorized)
            return Result<bool>.Failure(ErrorMessage.UnauthorizedAction);

        if (stored.RevokedAt is not null)
            return Result<bool>.Success(true);

        await _refreshTokenRepository.TryRevokeSessionAsync(
            stored.Id,
            stored.UserId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        // Logout is idempotent: a concurrent request may have revoked the same
        // session after it was read.
        return Result<bool>.Success(true);
    }
}
