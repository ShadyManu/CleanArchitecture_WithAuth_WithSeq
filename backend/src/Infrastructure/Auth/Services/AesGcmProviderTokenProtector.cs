using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Auth;
using Infrastructure.Auth.Models;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth.Services;

public sealed class AesGcmProviderTokenProtector : IProviderTokenProtector
{
    private const string Version = "v1";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly byte[] AssociatedData =
        "CCTemplate.ProviderToken.v1"u8.ToArray();

    private readonly byte[] _key;

    public AesGcmProviderTokenProtector(IOptions<JwtOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.ProviderTokenEncryptionKey);
    }

    public string Protect(string providerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerToken);

        var plaintext = Encoding.UTF8.GetBytes(providerToken);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);

            return string.Join(
                '.',
                Version,
                Base64UrlEncode(nonce),
                Base64UrlEncode(ciphertext),
                Base64UrlEncode(tag));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public bool TryUnprotect(string protectedProviderToken, out string? providerToken)
    {
        providerToken = null;

        try
        {
            var parts = protectedProviderToken.Split('.');
            if (parts.Length != 4 || !string.Equals(parts[0], Version, StringComparison.Ordinal))
            {
                return false;
            }

            var nonce = Base64UrlDecode(parts[1]);
            var ciphertext = Base64UrlDecode(parts[2]);
            var tag = Base64UrlDecode(parts[3]);
            if (nonce.Length != NonceSize || tag.Length != TagSize)
            {
                return false;
            }

            var plaintext = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(_key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);
                providerToken = Encoding.UTF8.GetString(plaintext);
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        catch (Exception exception) when (
            exception is FormatException or
            CryptographicException or
            ArgumentException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        base64 = (base64.Length % 4) switch
        {
            0 => base64,
            2 => $"{base64}==",
            3 => $"{base64}=",
            _ => throw new FormatException("Invalid Base64Url value.")
        };

        return Convert.FromBase64String(base64);
    }
}
