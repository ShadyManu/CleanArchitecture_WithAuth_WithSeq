using Application.Dtos.Auth.Request;

namespace Application.Common.Interfaces.Auth;

public interface IAppleAuthService
{
    Task<(bool isValid, string? Email, string ProviderId)> ExtractEmailAndProviderIdAsync(
        string idToken,
        CancellationToken cancellationToken = default);
    Task<AppleTokenResponse> ExchangeCodeForTokenAsync(
        string authorizationCode,
        CancellationToken cancellationToken);
    Task<bool> RevokeTokenAsync(
        string appleRefreshToken,
        CancellationToken cancellationToken);
}

