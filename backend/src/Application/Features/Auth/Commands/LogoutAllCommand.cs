using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Domain.Common;

namespace Application.Features.Auth.Commands;

public record LogoutAllCommand : ICommand<bool>;

internal sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand, bool>
{
    private readonly IUser _currentUser;
    private readonly IUserRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;

    public LogoutAllCommandHandler(
        IUser currentUser,
        IUserRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<bool>> Handle(
        LogoutAllCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id;
        if (userId == Guid.Empty)
        {
            return Result<bool>.Failure(ErrorMessage.UnauthorizedAction);
        }

        await _refreshTokenRepository.RevokeAllSessionsAsync(
            userId,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
