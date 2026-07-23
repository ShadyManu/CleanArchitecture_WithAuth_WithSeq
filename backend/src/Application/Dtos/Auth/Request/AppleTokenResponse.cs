using System.Text.Json.Serialization;

namespace Application.Dtos.Auth.Request;

public record AppleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }
    
    public bool IsSuccess =>
        string.IsNullOrEmpty(Error) &&
        !string.IsNullOrWhiteSpace(RefreshToken) &&
        !string.IsNullOrWhiteSpace(IdToken);
}
