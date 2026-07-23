using Application.Dtos.ToDo.Response;
using Domain.Entities;

namespace Application.Mapper;

public static class ToDoMapper
{
    public static ToDoEntity ToEntity(this ToDoResponse dto) =>
        new()
        {
            Id = dto.Id,
            Title = dto.Title,
            Note = dto.Note,
            Reminder = dto.Reminder,
            Priority = dto.Priority,
        };

    public static ToDoResponse ToDto(this ToDoEntity entity) =>
        new(entity.Id, entity.Title, entity.Priority, entity.Note, entity.Reminder);
    
    public static IReadOnlyList<ToDoEntity> ToEntityList(this IReadOnlyList<ToDoResponse> dtos) =>
        dtos.Select(ToEntity).ToList();
    
    public static IReadOnlyList<ToDoResponse> ToDtoList(this IReadOnlyList<ToDoEntity> entities) =>
        entities.Select(ToDto).ToList();
}
