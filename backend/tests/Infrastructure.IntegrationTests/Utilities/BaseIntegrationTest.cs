using System.Net.Http.Headers;
using Application.Common.Interfaces.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.IntegrationTests.Utilities;

public abstract class BaseIntegrationTest<TEntity> : IAsyncLifetime
    where TEntity : class
{
    protected readonly HttpClient _httpClientAnonymous;
    protected readonly IntegrationTestWebAppFactory _factory;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        
        _httpClientAnonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected HttpClient CreateAuthenticatedClient(Guid userId, string providerId)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var accessToken = tokenService.CreateAccessToken(userId, providerId);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        SetBearerToken(client, accessToken);

        return client;
    }

    protected static void SetBearerToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;
    
    public virtual async ValueTask DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var dbSet = context.Set<TEntity>();
        dbSet.RemoveRange(dbSet);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        GC.SuppressFinalize(this);
    }
}
