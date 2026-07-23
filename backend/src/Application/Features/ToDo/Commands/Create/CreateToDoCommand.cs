using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories;
using Application.Common.Result;
using Application.Dtos.ToDo.Response;
using Application.Mapper;
using Domain.Common.Constants;
using Domain.Entities;

namespace Application.Features.ToDo.Commands.Create;

public record CreateToDoCommand(
    string Title,
    int Priority,
    string? Note = null,
    DateTimeOffset? Reminder = null)
    : ICommand<ToDoResponse?>
{
    private const short MinTitleLength = DbConstraints.MinToDoNameLength;
    private const short MaxTitleLength = DbConstraints.MaxToDoNameLength;
    private const short MaxNoteLength = DbConstraints.MaxToDoNoteLength;

    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return (false, ValidatorMessage.CannotBeEmpty(nameof(Title)));

        if (Title.Length < MinTitleLength)
            return (false, ValidatorMessage.MinLength(nameof(Title), MinTitleLength));

        if (Title.Length > MaxTitleLength)
            return (false, ValidatorMessage.MaxLength(nameof(Title), MaxTitleLength));

        if (Priority < 0)
            return (false, ValidatorMessage.MinValue(nameof(Priority), 0));

        return Note is not null && Note.Length > MaxNoteLength
            ? (false, ValidatorMessage.MaxLength(nameof(Note), MaxNoteLength))
            : (true, null);
    }
}

internal sealed class CreateToDoCommandHandler : ICommandHandler<CreateToDoCommand, ToDoResponse?>
{
    private readonly IToDoRepository _toDoRepository;

    public CreateToDoCommandHandler(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository;
    }

    public async Task<Result<ToDoResponse?>> Handle(CreateToDoCommand request, CancellationToken cancellationToken)
    {
        var entity = new ToDoEntity
        {
            Title = request.Title,
            Priority = request.Priority,
            Note = request.Note,
            Reminder = request.Reminder?.ToUniversalTime()
        };

        await _toDoRepository.AddAsync(entity, cancellationToken);
        var result = await _toDoRepository.SaveChangesAsync(cancellationToken);
        if (result is 0)
        {
            return Result<ToDoResponse?>.Failure(ErrorMessage.SomethingWentWrong);
        }

        var dto = entity.ToDto();
        return Result<ToDoResponse?>.Success(dto);
    }
}
