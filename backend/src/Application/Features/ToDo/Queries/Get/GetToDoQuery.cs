using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories;
using Application.Common.Result;
using Application.Dtos.ToDo.Response;
using Application.Mapper;

namespace Application.Features.ToDo.Queries.Get;

public record GetToDoQuery(Guid Id) : IQuery<ToDoResponse?>
{
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        return Id == Guid.Empty
            ? (false, ValidatorMessage.InvalidGuid)
            : (true, null);
    }
}

internal sealed class GetToDoQueryHandler : IQueryHandler<GetToDoQuery, ToDoResponse?>
{
    private readonly IToDoRepository _toDoRepository;

    public GetToDoQueryHandler(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository;
    }
    
    public async Task<Result<ToDoResponse?>> Handle(GetToDoQuery request, CancellationToken cancellationToken)
    {
        var entity = await _toDoRepository.GetByIdAsNoTrackingAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result<ToDoResponse?>.Failure(ErrorMessage.NotFound);
        }

        var dto = entity.ToDto();
        return Result<ToDoResponse?>.Success(dto);
    }
}
