using Application.Common.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.ToDo;

public class ToDoRepository(ApplicationDbContext context)
    : BaseGuidRepository<ToDoEntity>(context), IToDoRepository
{
    private readonly ApplicationDbContext _context = context;
    
    public async Task<IReadOnlyList<ToDoEntity>> GetAllOrderedByPriorityAsNoTrackingAsync(CancellationToken cancellationToken) =>
        await _context.ToDos
            .AsNoTracking()
            .OrderBy(t => t.Priority)
            .ToListAsync(cancellationToken);
}
