using Domain.Common.Models;

namespace Domain.Entities.Auth;

public class UserRefreshTokenEntity : BaseGuidEntity
{
    public required Guid UserId { get; init; }
    public UserEntity User { get; init; } = null!;

    public required string DeviceId { get; init; }
    public required string TokenHash { get; set; }
    public string? ProviderRefreshToken { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

}
