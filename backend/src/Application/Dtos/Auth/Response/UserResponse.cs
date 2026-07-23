using Domain.Common.Enums;
using TypeGen.Core.TypeAnnotations;

namespace Application.Dtos.Auth.Response;

[ExportTsInterface]
public class UserResponse
{
    public string? Email { get; init; }
    public string? Username { get; init; }
    public required ProviderEnum Provider { get; init; }
    public required string ProviderId { get; init; }
}
