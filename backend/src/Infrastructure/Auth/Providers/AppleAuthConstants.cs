namespace Infrastructure.Auth.Providers;

internal static class AppleAuthConstants
{
    public const string Issuer = "https://appleid.apple.com";
    public const string MetadataOpenIdUrl =
        "https://appleid.apple.com/.well-known/openid-configuration";
    public const string TokenUrl = "https://appleid.apple.com/auth/token";
    public const string RevokeTokenUrl = "https://appleid.apple.com/auth/revoke";
}
