namespace Domain.Common.Interfaces;

public interface IBaseGuidEntity : IBaseAuditableEntity
{
    public Guid Id { get; init; }
}
