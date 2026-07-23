using Application.Common.Interfaces.Repositories.Auth;
using Domain.Common.Enums;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public sealed class UserRepository : BaseGuidRepository<UserEntity>, IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<UserEntity?> GetByProviderIdAsync(
        ProviderEnum provider,
        string providerId,
        CancellationToken ct) =>
        _context.Users
            .Include(x => x.UserRefreshTokens)
            .SingleOrDefaultAsync(x => x.Provider == provider && x.ProviderId == providerId, ct);

    public Task<UserEntity?> GetByIdWithUserRefreshTokenAsync(Guid userId, CancellationToken ct) =>
        _context.Users
            .Include(x => x.UserRefreshTokens)
            .SingleOrDefaultAsync(x => x.Id == userId, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _context.Users.AnyAsync(x => x.Email == email, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct) =>
        _context.Users.AnyAsync(x => x.Username == username, ct);

}
