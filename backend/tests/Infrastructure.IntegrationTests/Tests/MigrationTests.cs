using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class MigrationTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public async Task DatabaseModel_ShouldHaveNoPendingMigrations()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(
            TestContext.Current.CancellationToken);

        Assert.Empty(pendingMigrations);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
