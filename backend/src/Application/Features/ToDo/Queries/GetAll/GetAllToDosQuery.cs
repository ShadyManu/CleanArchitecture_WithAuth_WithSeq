using Application.Common.Interfaces.CQRS;
using Application.Common.Interfaces.Repositories;
using Application.Common.Result;
using Application.Dtos.ToDo.Response;
using Application.Mapper;

namespace Application.Features.ToDo.Queries.GetAll;

public record GetAllToDosQuery : IQuery<IReadOnlyList<ToDoResponse>>;

internal sealed class GetAllToDosQueryHandler : IQueryHandler<GetAllToDosQuery, IReadOnlyList<ToDoResponse>>
{
    private readonly IToDoRepository _toDoRepository;

    public GetAllToDosQueryHandler(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository;
    }

    public async Task<Result<IReadOnlyList<ToDoResponse>>> Handle(GetAllToDosQuery request, CancellationToken cancellationToken)
    {
        var entities = await _toDoRepository.GetAllOrderedByPriorityAsNoTrackingAsync(cancellationToken);
        if (entities.Count is 0)
        {
            return Result<IReadOnlyList<ToDoResponse>>.Success([]);
        }

        var dtos = entities.ToDtoList();
        return Result<IReadOnlyList<ToDoResponse>>.Success(dtos);
    }
}
