using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Result;
using Domain.Common;
using Domain.Common.Enums;

namespace Application.Features.Auth.Commands;

public record DeleteUserCommand : ICommand<bool>;

internal sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
{
    private readonly IUser _user;
    private readonly IUserRepository _userRepository;
    private readonly IAppleAuthService _appleAuthService;
    private readonly IProviderTokenProtector _providerTokenProtector;

    public DeleteUserCommandHandler(
        IUser user,
        IUserRepository userRepository,
        IAppleAuthService appleAuthService,
        IProviderTokenProtector providerTokenProtector)
    {
        _user = user;
        _userRepository = userRepository;
        _appleAuthService = appleAuthService;
        _providerTokenProtector = providerTokenProtector;
    }

    public async Task<Result<bool>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        if (userId == Guid.Empty)
        {
            return Result<bool>.Failure(ErrorMessage.UnauthorizedAction);
        }

        var user = await _userRepository.GetByIdWithUserRefreshTokenAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<bool>.Failure(ErrorMessage.NotFound);
        }

        if (user.Provider == ProviderEnum.Apple)
        {
            var protectedAppleTokens = user.UserRefreshTokens
                .Where(t => !string.IsNullOrWhiteSpace(t.ProviderRefreshToken))
                .Select(t => t.ProviderRefreshToken!)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // A successful deletion must also revoke Sign in with Apple. Otherwise
            // Apple will not show the first-authorization flow on the next sign-in.
            if (protectedAppleTokens.Count == 0)
            {
                return Result<bool>.Failure(ErrorMessage.AppleReauthorizationRequired);
            }

            foreach (var protectedAppleToken in protectedAppleTokens)
            {
                if (!_providerTokenProtector.TryUnprotect(
                        protectedAppleToken,
                        out var appleToken) ||
                    string.IsNullOrWhiteSpace(appleToken))
                {
                    return Result<bool>.Failure(ErrorMessage.AppleReauthorizationRequired);
                }

                var isRevoked = await _appleAuthService.RevokeTokenAsync(
                    appleToken,
                    cancellationToken);

                if (!isRevoked)
                {
                    return Result<bool>.Failure(ErrorMessage.AppleRevokeFailed);
                }
            }
        }

        var deleted = await _userRepository.DeleteAsync(userId, cancellationToken);
        if (deleted <= 0)
        {
            return Result<bool>.Failure(ErrorMessage.SomethingWentWrong);
        }

        return Result<bool>.Success(true);
    }
}
