using System.Text;
using Infrastructure.Auth.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth.Validation;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        RequireValue(options.Issuer, nameof(options.Issuer), failures);
        RequireValue(options.Audience, nameof(options.Audience), failures);

        if (string.IsNullOrWhiteSpace(options.SigningKey) ||
            Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            failures.Add("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        if (options.AccessTokenMinutes is < 1 or > 1440)
        {
            failures.Add("Jwt:AccessTokenMinutes must be between 1 and 1440.");
        }

        if (options.RefreshTokenDays is < 1 or > 3650)
        {
            failures.Add("Jwt:RefreshTokenDays must be between 1 and 3650.");
        }

        if (options.RefreshTokenBytes is < 32 or > 128)
        {
            failures.Add("Jwt:RefreshTokenBytes must be between 32 and 128.");
        }

        if (!string.IsNullOrWhiteSpace(options.RefreshTokenHmacKey) &&
            Encoding.UTF8.GetByteCount(options.RefreshTokenHmacKey) < 32)
        {
            failures.Add("Jwt:RefreshTokenHmacKey must contain at least 32 UTF-8 bytes when configured.");
        }

        try
        {
            var encryptionKey = Convert.FromBase64String(options.ProviderTokenEncryptionKey);
            if (encryptionKey.Length != 32)
            {
                failures.Add("Jwt:ProviderTokenEncryptionKey must be a Base64-encoded 256-bit key.");
            }
        }
        catch (Exception exception) when (
            exception is FormatException or
            ArgumentNullException)
        {
            failures.Add("Jwt:ProviderTokenEncryptionKey must be a Base64-encoded 256-bit key.");
        }

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
            failures.Add($"Jwt:{propertyName} is required.");
        }
    }
}
