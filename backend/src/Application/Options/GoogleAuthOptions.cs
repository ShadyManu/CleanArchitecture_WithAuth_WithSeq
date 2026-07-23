namespace Application.Common.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public required string[] ClientIds { get; init; }
}
