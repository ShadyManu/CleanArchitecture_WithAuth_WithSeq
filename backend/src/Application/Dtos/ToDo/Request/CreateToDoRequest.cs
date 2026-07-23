namespace Application.Dtos.ToDo.Request;

public class CreateToDoRequest
{
    public required string Title { get; init; }
    public required int Priority { get; init; }
    public string? Note { get; init; }
    public DateTimeOffset? Reminder { get; init; }
}
