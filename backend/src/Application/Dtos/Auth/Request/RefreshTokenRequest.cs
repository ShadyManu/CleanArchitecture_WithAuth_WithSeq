using TypeGen.Core.TypeAnnotations;

namespace Application.Dtos.Auth.Request;

[ExportTsInterface]
public class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
    public required string DeviceId { get; init; }
}
