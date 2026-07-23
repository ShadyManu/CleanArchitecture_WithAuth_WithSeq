using Domain.Entities;

namespace Application.Common.Interfaces.Repositories;

public interface IToDoRepository : IBaseGuidRepository<ToDoEntity>
{
    Task<IReadOnlyList<ToDoEntity>> GetAllOrderedByPriorityAsNoTrackingAsync(CancellationToken cancellationToken);
}
