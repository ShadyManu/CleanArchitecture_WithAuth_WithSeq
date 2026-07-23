using Domain.Common.Constants;
using Domain.Entities;
using Infrastructure.Data.Configurations.Common;
using Infrastructure.Data.TableNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ToDoConfiguration : BaseGuidEntityConfiguration<ToDoEntity>
{
    public override void Configure(EntityTypeBuilder<ToDoEntity> builder)
    {
        base.Configure(builder);
        
        builder.ToTable(DatabaseConstants.ToDoItemTable);

        builder.Property(t => t.Title)
            .HasMaxLength(DbConstraints.MaxToDoNameLength)
            .IsRequired();
        
        builder.Property(t => t.Note)
            .HasMaxLength(DbConstraints.MaxToDoNoteLength)
            .IsRequired(false);
        
        builder.Property(t => t.Priority)
            .IsRequired();
        
        builder.Property(t => t.Reminder)
            .IsRequired(false);
    }
}
