using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories;
using Application.Common.Result;

namespace Application.Features.ToDo.Commands.Delete;

public record DeleteToDoCommand(Guid Id) : ICommand<bool>
{
    public (bool IsValid, string? ErrorMessage) Validate() =>
        Id == Guid.Empty
            ? (false, ValidatorMessage.InvalidGuid)
            : (true, null);
}

internal sealed class DeleteToDoCommandHandler : ICommandHandler<DeleteToDoCommand, bool>
{
    private readonly IToDoRepository _toDoRepository;

    public DeleteToDoCommandHandler(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository;
    }

    public async Task<Result<bool>> Handle(DeleteToDoCommand request, CancellationToken cancellationToken)
    {
        var result = await _toDoRepository.DeleteAsync(request.Id, cancellationToken);
        return result is 0
            ? Result<bool>.Failure(ErrorMessage.NotFound)
            : Result<bool>.Success(true);
    }
}
