using TypeGen.Core.TypeAnnotations;

namespace Application.Dtos.Auth.Response;

[ExportTsInterface]
public record AuthTokenResponse(
    Guid UserId,
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken
);
