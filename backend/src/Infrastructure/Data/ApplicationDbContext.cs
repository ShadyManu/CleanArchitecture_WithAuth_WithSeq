using Domain.Entities;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public virtual DbSet<ToDoEntity> ToDos { get; init; }
    
    public virtual DbSet<UserEntity> Users { get; init; }
    public virtual DbSet<UserRefreshTokenEntity> UserRefreshTokens { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresExtension("citext");
        
        // Every class that inherits from IEntityTypeConfiguration will be automatically configured
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
