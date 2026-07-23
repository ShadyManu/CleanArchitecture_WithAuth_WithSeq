using Domain.Common.Constants;
using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Common;

public class BaseAuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IBaseAuditableEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.Created)
            .IsRequired(false);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(DbConstraints.CreatedByMaxLength)
            .IsRequired(false);

        builder.Property(e => e.LastModified)
            .IsRequired(false);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(DbConstraints.LastModifiedByMaxLength)
            .IsRequired(false);
    }
}
