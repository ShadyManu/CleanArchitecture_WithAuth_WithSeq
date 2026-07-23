using Domain.Common.Models;

namespace Domain.Entities;

public class ToDoEntity : BaseGuidEntity
{
    public required string Title { get; set; }
    public string? Note { get; set; }
    public required int Priority { get; set; }
    public DateTimeOffset? Reminder { get; set; }
}
