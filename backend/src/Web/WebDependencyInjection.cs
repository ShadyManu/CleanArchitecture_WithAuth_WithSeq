using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Application.Common.Interfaces.Auth;
using Application.Common.Options;
using Infrastructure.Auth.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.Constants;
using Web.Constants;
using Web.Services;

namespace Web;

public static class WebDependencyInjection
{
    public static void AddWebDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<AppleAuthOptions>()
            .Bind(configuration.GetSection(AppleAuthOptions.SectionName))
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });
        services.AddAuthorization();
        services.AddScoped<IUser, CurrentUser>();

        // CORS policy
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyConstants.LocalPolicy, b => b
                .WithOrigins(CorsPolicyConstants.LocalAllowedUrls)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
            );
            options.AddPolicy(CorsPolicyConstants.ProdPolicy, b => b
                .WithOrigins(CorsPolicyConstants.ProdAllowedUrls)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
            );
        });

        // Rate limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimiterConstants.AnonymousUserPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterConstants.AnonymousUserPermitLimit,
                        Window = TimeSpan.FromSeconds(RateLimiterConstants.AnonymousUserWindowSeconds),
                    }));

            options.AddPolicy(RateLimiterConstants.AuthenticatedUserPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                  httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimiterConstants.AuthenticatedUserPermitLimit,
                        Window = TimeSpan.FromSeconds(RateLimiterConstants.AuthenticatedUserWindowSeconds),
                    }));
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        services.AddHttpContextAccessor();

        services.AddEndpointsApiExplorer();
    }
}
