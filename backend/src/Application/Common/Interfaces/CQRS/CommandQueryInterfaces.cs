using Application.Common.Result;

namespace Application.Common.Interfaces.CQRS;

public interface IValidatable
{
    (bool IsValid, string? ErrorMessage) Validate() => (true, null);
}

public interface ICommand<TResponse> : IValidatable;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQuery<TResponse> : IValidatable;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
