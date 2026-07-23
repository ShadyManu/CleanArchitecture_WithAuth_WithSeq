namespace Application.Common.Interfaces.Auth;

public interface IGoogleAuthService
{
    Task<(bool isValid, string? Email, string ProviderId)> ExtractEmailAndProviderIdAsync(string idToken);
}
