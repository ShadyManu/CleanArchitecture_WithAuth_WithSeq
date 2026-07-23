using TypeGen.Core.TypeAnnotations;

namespace Application.Dtos.Auth.Request;

[ExportTsInterface]
public class LogoutRequest
{
    public required string RefreshToken { get; init; }
    public required string DeviceId { get; init; }
}
