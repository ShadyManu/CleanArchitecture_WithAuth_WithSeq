using Domain.Common.Enums;
using TypeGen.Core.TypeAnnotations;

namespace Application.Dtos.Auth.Request;

[ExportTsInterface]
public class SignInRequest
{
    public required ProviderEnum Provider { get; init; }
    public required string IdToken { get; init; }
    public string? AuthorizationCode { get; init; }
    public required string DeviceId { get; init; }
}
