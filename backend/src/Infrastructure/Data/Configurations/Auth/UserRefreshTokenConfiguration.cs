using Domain.Common.Constants;
using Domain.Entities.Auth;
using Infrastructure.Data.Configurations.Common;
using Infrastructure.Data.TableNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Auth;

public class UserRefreshTokenConfiguration : BaseAuditableEntityConfiguration<UserRefreshTokenEntity>
{
    public override void Configure(EntityTypeBuilder<UserRefreshTokenEntity> builder)
    {
        base.Configure(builder);

        builder.ToTable(DatabaseConstants.UserRefreshTokenTable, DatabaseConstants.AuthSchema);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.DeviceId })
            .IsUnique();

        builder.Property(t => t.UserId)
            .IsRequired(true);
        builder.Property(t => t.DeviceId)
            .HasMaxLength(DbConstraints.DeviceIdMaxLength)
            .IsRequired(true);
        builder.Property(t => t.TokenHash)
            .HasMaxLength(DbConstraints.TokenHashMaxLength)
            .IsRequired(true);
        builder.Property(t => t.ProviderRefreshToken)
            .HasMaxLength(DbConstraints.ProviderRefreshTokenMaxLength)
            .IsRequired(false);
        builder.Property(t => t.ExpiresAt)
            .IsRequired(true);
        builder.Property(t => t.RevokedAt)
            .IsRequired(false);

        // Relations
        builder.HasOne(e => e.User)
            .WithMany(p => p.UserRefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
