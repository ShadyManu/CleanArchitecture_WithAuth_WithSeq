using Application.Common.Interfaces.CQRS;
using Application.Dtos.Auth.Request;
using Application.Dtos.Auth.Response;
using Application.Features.Auth.Commands;
using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Presentation.Constants;

namespace Presentation.Endpoints;

public sealed class AuthEndpoints : ICarterModule
{
    private const string EndpointTag = "Auth";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var anonymousGroup = app.MapGroup($"/api/{EndpointTag.ToLower()}")
            .WithTags(EndpointTag)
            .RequireRateLimiting(RateLimiterConstants.AnonymousUserPolicy)
            .IncludeInOpenApi();

        anonymousGroup.MapPost("/sign-in", async (
                SignInRequest signInRequest,
                ICommandHandler<SignInCommand, AuthTokenResponse?> handler,
                CancellationToken ct) =>
            await handler.Handle(new SignInCommand(
                signInRequest.Provider,
                signInRequest.IdToken,
                signInRequest.DeviceId,
                signInRequest.AuthorizationCode), ct));
        
        anonymousGroup.MapPost("/refresh-token", async (
                RefreshTokenRequest refreshTokenRequest,
                ICommandHandler<RefreshTokenCommand, AuthTokenResponse?> handler,
                CancellationToken ct) =>
            await handler.Handle(new RefreshTokenCommand(
                refreshTokenRequest.RefreshToken,
                refreshTokenRequest.DeviceId), ct));

        var authenticatedGroup = app.MapGroup($"/api/{EndpointTag.ToLower()}")
            .WithTags(EndpointTag)
            .RequireRateLimiting(RateLimiterConstants.AuthenticatedUserPolicy)
            .RequireAuthorization()
            .IncludeInOpenApi();

        authenticatedGroup.MapPost("/logout", async (
                LogoutRequest logoutRequest,
                ICommandHandler<LogoutCommand, bool> handler,
                CancellationToken ct) =>
            await handler.Handle(new LogoutCommand(
                logoutRequest.RefreshToken,
                logoutRequest.DeviceId), ct));

        authenticatedGroup.MapPost("/logout-all", async (
                ICommandHandler<LogoutAllCommand, bool> handler,
                CancellationToken ct) =>
            await handler.Handle(new LogoutAllCommand(), ct));

        authenticatedGroup.MapDelete("/delete-user", async (
                ICommandHandler<DeleteUserCommand, bool> handler,
                CancellationToken ct) =>
            await handler.Handle(new DeleteUserCommand(), ct));
    }
}
