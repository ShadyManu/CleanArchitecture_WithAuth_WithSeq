namespace Application.Common.Interfaces.Auth;

/// <summary>
/// Protects refresh tokens issued by external identity providers before persistence.
/// Unlike application refresh-token hashing, this protection is reversible because
/// the original provider token is required for remote revocation.
/// </summary>
public interface IProviderTokenProtector
{
    string Protect(string providerToken);
    bool TryUnprotect(string protectedProviderToken, out string? providerToken);
}
