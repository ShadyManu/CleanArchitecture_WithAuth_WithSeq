using Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Common;

public class BaseGuidEntityConfiguration<TEntity> : BaseAuditableEntityConfiguration<TEntity>
    where TEntity : class, IBaseGuidEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedNever();
    }
}
