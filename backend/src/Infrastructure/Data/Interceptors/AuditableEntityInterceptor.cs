using Application.Common.Interfaces.Auth;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data.Interceptors;

/// <summary>
/// Every entity that inherits from IBaseAuditableEntity will be automatically audited before saving changes
/// </summary>
public class AuditableEntityInterceptor(TimeProvider timeProvider, IUser user) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IBaseAuditableEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified) &&
                !entry.HasChangedOwnedEntities()) continue;

            var utcNow = timeProvider.GetUtcNow();
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = user.Id.ToString();
                entry.Entity.Created = utcNow;
            }
            entry.Entity.LastModifiedBy = user.Id.ToString();
            entry.Entity.LastModified = utcNow;
        }
    }
}

internal static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry)
    {
        return entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            r.TargetEntry.State is EntityState.Added or EntityState.Modified);
    }
}
