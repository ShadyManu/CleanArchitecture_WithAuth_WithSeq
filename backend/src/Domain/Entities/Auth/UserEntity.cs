using Domain.Common.Enums;
using Domain.Common.Models;

namespace Domain.Entities.Auth;

public class UserEntity : BaseGuidEntity
{
    public string? Email { get; init; }
    public string? Username { get; set; }
    public required ProviderEnum Provider { get; init; }
    public required string ProviderId { get; init; }

    public ICollection<UserRefreshTokenEntity> UserRefreshTokens { get; init; } = [];
}
