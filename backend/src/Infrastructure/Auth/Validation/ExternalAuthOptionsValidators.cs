using System.Security.Cryptography;
using Application.Common.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth.Validation;

public sealed class GoogleAuthOptionsValidator : IValidateOptions<GoogleAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, GoogleAuthOptions options)
    {
        return options.ClientIds.Length > 0 &&
               options.ClientIds.All(clientId => !string.IsNullOrWhiteSpace(clientId))
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "Authentication:Google:ClientIds must contain at least one non-empty client ID.");
    }
}

public sealed class AppleAuthOptionsValidator : IValidateOptions<AppleAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AppleAuthOptions options)
    {
        var failures = new List<string>();

        RequireValue(options.Audience, nameof(options.Audience), failures);
        RequireValue(options.TeamId, nameof(options.TeamId), failures);
        RequireValue(options.KeyId, nameof(options.KeyId), failures);
        RequireValue(options.PrivateKey, nameof(options.PrivateKey), failures);
        ValidatePrivateKey(options.PrivateKey, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireValue(
        string? value,
        string propertyName,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"Authentication:Apple:{propertyName} is required.");
        }
    }

    private static void ValidatePrivateKey(
        string privateKey,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            return;
        }

        try
        {
            var normalizedKey = privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", string.Empty)
                .Replace("-----END PRIVATE KEY-----", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);
            var keyBytes = Convert.FromBase64String(normalizedKey);
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);
        }
        catch (Exception exception) when (
            exception is FormatException or
            CryptographicException or
            ArgumentException)
        {
            failures.Add(
                "Authentication:Apple:PrivateKey must be a valid Base64 or PEM PKCS#8 EC private key.");
        }
    }
}
