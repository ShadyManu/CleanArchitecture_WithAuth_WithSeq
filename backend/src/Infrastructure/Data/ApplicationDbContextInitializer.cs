using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public static class AppInitializer
{
    extension(IServiceProvider services)
    {
        public async Task MigrateAsync()
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (context is null)
                return;

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();
            if (logger is null)
                return;
        
            await ApplyMigrationsAsync(context, logger);
        }

        public async Task SeedAsync()
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (context is null)
                return;

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();
            if (logger is null)
                return;
        
            await SeedAsync(context, logger);
        }
    }

    private static async Task ApplyMigrationsAsync(ApplicationDbContext context, ILogger<ApplicationDbContext> logger)
    {
        if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw;
            }
        }
        else
        {
            Console.WriteLine("Skipping migrations for InMemoryDatabase.");
        }
    }
    
    
    private static async Task SeedAsync(ApplicationDbContext context, ILogger<ApplicationDbContext> logger)
    {
        try
        {
            // Add anything starting seeding to the database with the context
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seed the database.");
            throw;
        }
    }
}
