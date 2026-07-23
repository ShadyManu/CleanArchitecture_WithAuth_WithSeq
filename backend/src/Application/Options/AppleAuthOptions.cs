namespace Application.Common.Options;

public sealed class AppleAuthOptions
{
    public const string SectionName = "Authentication:Apple";

    public required string Audience { get; init; }
    public required string TeamId { get; init; }
    public required string KeyId { get; init; }
    public required string PrivateKey { get; init; }
}

