namespace Application.Dtos.ToDo.Response;

public record ToDoResponse(
    Guid Id,
    string Title,
    int Priority,
    string? Note,
    DateTimeOffset? Reminder);
