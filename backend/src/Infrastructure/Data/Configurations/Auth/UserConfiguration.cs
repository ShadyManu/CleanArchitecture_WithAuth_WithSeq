using Domain.Common.Constants;
using Domain.Common.Enums;
using Domain.Entities.Auth;
using Infrastructure.Data.Configurations.Common;
using Infrastructure.Data.TableNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Auth;

public class UserConfiguration : BaseAuditableEntityConfiguration<UserEntity>
{
    public override void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        base.Configure(builder);

        builder.ToTable(DatabaseConstants.UserTable, DatabaseConstants.AuthSchema);

        builder.HasIndex(x => new { x.Provider, x.ProviderId })
            .IsUnique();
        builder.HasIndex(x => x.Username)
            .IsUnique();
        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(t => t.Provider)
            .HasConversion(new EnumSnakeCaseConverter<ProviderEnum>())
            .HasMaxLength(DbConstraints.EnumMaxLength)
            .IsRequired();
        builder.Property(t => t.ProviderId)
            .HasMaxLength(DbConstraints.ProviderIdMaxLength)
            .IsRequired();
        builder.Property(t => t.Email)
            .HasMaxLength(DbConstraints.EmailMaxLength)
            .HasColumnType("citext")
            .IsRequired(false);
        builder.Property(t => t.Username)
            .HasMaxLength(DbConstraints.UserUsernameMaxLength)
            .HasColumnType("citext")
            .IsRequired(false);
    }
}
