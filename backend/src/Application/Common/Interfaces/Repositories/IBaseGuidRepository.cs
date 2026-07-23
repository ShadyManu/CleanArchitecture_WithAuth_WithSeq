using Domain.Common.Interfaces;

namespace Application.Common.Interfaces.Repositories;

public interface IBaseGuidRepository<TEntity> : IBaseRepository<TEntity, Guid>
    where TEntity : class, IBaseGuidEntity;
