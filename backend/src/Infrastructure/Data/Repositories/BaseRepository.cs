using System.Linq.Expressions;
using Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public abstract class BaseRepository<TEntity, TId> : IBaseRepository<TEntity, TId>
    where TEntity : class
{
    private readonly ApplicationDbContext _context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(ApplicationDbContext context)
    {
        _context = context;
        DbSet = context.Set<TEntity>();
    }

    public abstract Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    public abstract Task<TEntity?> GetByIdAsNoTrackingAsync(TId id, CancellationToken cancellationToken = default);

    public abstract Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsNoTrackingAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(predicate)
            .ToListAsync(cancellationToken);
    
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<TEntity>> AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        return entities;
    }

    public async Task ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(predicate)
            .ExecuteDeleteAsync(cancellationToken);

    public void DeleteRangeAsync(IReadOnlyList<TEntity> entitiesToRemove, CancellationToken cancellationToken = default) =>
        DbSet.RemoveRange(entitiesToRemove);

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public IQueryable<TEntity> Queryable() =>
        DbSet.AsQueryable();

    public IQueryable<TEntity> FromSqlRaw(string sql, params object[] parameters) =>
        DbSet.FromSqlRaw(sql, parameters);
}
