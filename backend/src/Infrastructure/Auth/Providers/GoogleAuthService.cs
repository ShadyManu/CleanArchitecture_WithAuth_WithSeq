using Application.Common.Interfaces.Auth;
using Application.Common.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth.Providers;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly string[] _googleClientIds;

    public GoogleAuthService(IOptions<GoogleAuthOptions> options)
    {
        _googleClientIds = options.Value.ClientIds;
    }

    public async Task<(bool isValid, string? Email, string ProviderId)> ExtractEmailAndProviderIdAsync(string idToken)
    {
        var payload = await ValidateIdTokenAsync(idToken);
        
        return payload is null 
            ? (false, string.Empty, string.Empty)
            : (true, payload.Email, payload.Subject);
    }

    private async Task<GoogleJsonWebSignature.Payload?> ValidateIdTokenAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = _googleClientIds
        };

        // throws InvalidJwtException if invalid (signature/exp/iss/aud)
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch
        {
            return null;
        }
    }
}
