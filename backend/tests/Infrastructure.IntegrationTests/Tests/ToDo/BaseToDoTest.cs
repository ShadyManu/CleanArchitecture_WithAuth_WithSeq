using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.IntegrationTests.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Tests.ToDo;

[Collection(IntegrationTestCollection.Name)]
public abstract partial class BaseToDoTest : BaseIntegrationTest<ToDoEntity>
{
    // protected IReadOnlyList<ToDoEntity> ToDoEntitiesSeed { get; private set; } = [];

    protected BaseToDoTest(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await context.ToDos.AddRangeAsync(ToDoEntitiesSeed, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}

// Seed Data
public abstract partial class BaseToDoTest
{
    protected const string BaseEndpoint = "/api/todo";

    protected IReadOnlyList<ToDoEntity> ToDoEntitiesSeed { get; } =
    [
        new()
        {
            Id = Guid.CreateVersion7(),
            Title = "First ToDo",
            Priority = 1,
            Note = "This is the first to-do item."
        },
        new()
        {
            Id = Guid.CreateVersion7(),
            Title = "Second ToDo",
            Priority = 2,
            Note = "This is the second to-do item."
        }
    ];

    protected Guid FirstToDoId => ToDoEntitiesSeed[0].Id;
    protected Guid SecondToDoId => ToDoEntitiesSeed[1].Id;
}
