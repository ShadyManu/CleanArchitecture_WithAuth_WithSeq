using Application.Common.Interfaces.Repositories;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public abstract class BaseGuidRepository<TEntity> : BaseRepository<TEntity, Guid>, IBaseGuidRepository<TEntity>
    where TEntity : class, IBaseGuidEntity
{
    protected BaseGuidRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public override Task<TEntity?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public override Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        DbSet
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
}
