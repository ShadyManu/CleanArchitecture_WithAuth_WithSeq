using Application.Common.Interfaces.CQRS;
using Application.Dtos.ToDo.Request;
using Application.Dtos.ToDo.Response;
using Application.Features.ToDo.Commands.Create;
using Application.Features.ToDo.Commands.Delete;
using Application.Features.ToDo.Commands.Update;
using Application.Features.ToDo.Queries.Get;
using Application.Features.ToDo.Queries.GetAll;
using Carter;
using Carter.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Presentation.Constants;

namespace Presentation.Endpoints;

public sealed class ToDoEndpoints : ICarterModule
{
    private const string EndpointTag = "ToDo";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup($"/api/{EndpointTag.ToLower()}")
            .WithTags(EndpointTag)
            .RequireRateLimiting(RateLimiterConstants.AnonymousUserPolicy)
            .IncludeInOpenApi();

        group.MapGet("/", async (
                IQueryHandler<GetAllToDosQuery, IReadOnlyList<ToDoResponse>> handler,
                CancellationToken ct) =>
            await handler.Handle(new GetAllToDosQuery(), ct));

        group.MapGet("/{id:guid}", async (
                IQueryHandler<GetToDoQuery, ToDoResponse?> handler,
                Guid id,
                CancellationToken ct) =>
            await handler.Handle(new GetToDoQuery(id), ct));

        group.MapPost("/", async (
                ICommandHandler<CreateToDoCommand, ToDoResponse?> handler,
                CreateToDoRequest dto,
                CancellationToken ct) =>
            await handler.Handle(new CreateToDoCommand(dto.Title, dto.Priority, dto.Note, dto.Reminder), ct));

        group.MapDelete("/{id:guid}", async (
                ICommandHandler<DeleteToDoCommand, bool> handler,
                Guid id,
                CancellationToken ct) =>
            await handler.Handle(new DeleteToDoCommand(id), ct));

        group.MapPatch("/", async (
                ICommandHandler<UpdateToDoCommand, ToDoResponse?> handler,
                UpdateToDoRequest dto,
                CancellationToken ct) =>
            await handler.Handle(new UpdateToDoCommand(
                dto.Id,
                dto.Title,
                dto.Priority,
                dto.Note,
                dto.Reminder), ct));
    }
}
