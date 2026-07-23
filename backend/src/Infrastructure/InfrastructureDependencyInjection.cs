using Application.Common.Interfaces.Auth;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Repositories.Auth;
using Application.Common.Options;
using Infrastructure.Auth.Models;
using Infrastructure.Auth.Providers;
using Infrastructure.Auth.Services;
using Infrastructure.Auth.Validation;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        #region Database

        var databaseProvider = configuration["Database:DatabaseProvider"];
        if (databaseProvider is null)
            throw new MissingFieldException(
                "DatabaseProvider configuration is missing from app.settings/environment variables");
        
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        switch (databaseProvider)
        {
            case "PostgreSQL":
                {
                    var connectionString = configuration.GetConnectionString("DefaultConnection");
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new MissingFieldException(
                            "DefaultConnection (connection string) is missing from app.settings/environment variables");

                    services.AddDbContext<ApplicationDbContext>((sp, options) =>
                    {
                        options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                        options
                            .UseNpgsql(connectionString, o=> 
                                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                            .UseSnakeCaseNamingConvention();
                    });
                    break;
                }
            case "InMemory":
                {
                    services.AddDbContext<ApplicationDbContext>((sp, options) =>
                    {
                        options.UseInMemoryDatabase("InMemoryDb");
                        options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                        options.LogTo(Console.WriteLine);
                    });
                    break;
                }
            default:
                throw new NotSupportedException(
                    $"Database provider '{databaseProvider}' is not supported. " +
                    "Supported values are 'PostgreSQL' and 'InMemory'.");
        }
        
        #endregion
        
        #region Repositories
        
        services.Scan(scan => scan
            .FromAssemblyOf<ApplicationDbContext>()
            .AddClasses(classes => classes.Where(type =>
                type.Namespace?.StartsWith(
                    "Infrastructure.Data.Repositories",
                    StringComparison.Ordinal) == true &&
                type.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .AsMatchingInterface()
            .WithScopedLifetime());

        #endregion
        
        #region Auth
        
        services.TryAddSingleton<IProviderTokenProtector, AesGcmProviderTokenProtector>();
        services.TryAddScoped<ITokenService, TokenService>();
        services.TryAddScoped<IGoogleAuthService, GoogleAuthService>();
        services.TryAddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.TryAddSingleton<IValidateOptions<GoogleAuthOptions>, GoogleAuthOptionsValidator>();
        services.TryAddSingleton<IValidateOptions<AppleAuthOptions>, AppleAuthOptionsValidator>();

        services.TryAddSingleton(_ =>
            new ConfigurationManager<OpenIdConnectConfiguration>(
                AppleAuthConstants.MetadataOpenIdUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever()));
        services.AddHttpClient<IAppleAuthService, AppleAuthService>();
        
        #endregion

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
