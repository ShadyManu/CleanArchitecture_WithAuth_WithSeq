using Domain.Common.Interfaces;

namespace Domain.Common.Models;

public class BaseGuidEntity : IBaseGuidEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset? Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
