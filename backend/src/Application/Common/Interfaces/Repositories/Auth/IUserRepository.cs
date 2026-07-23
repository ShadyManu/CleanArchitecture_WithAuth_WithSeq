using Domain.Common.Enums;
using Domain.Entities.Auth;

namespace Application.Common.Interfaces.Repositories.Auth;

public interface IUserRepository : IBaseGuidRepository<UserEntity>
{
    Task<UserEntity?> GetByProviderIdAsync(ProviderEnum provider, string providerId, CancellationToken ct);
    Task<UserEntity?> GetByIdWithUserRefreshTokenAsync(Guid userId, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
}
