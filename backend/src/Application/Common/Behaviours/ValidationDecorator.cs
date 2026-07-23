using Application.Common.Interfaces.CQRS;
using Application.Common.Result;

namespace Application.Common.Behaviours;

/// <summary>
/// Everytime a Command/Query handler is invoked, these classes will validate the request using the Validate method
/// Coming from IValidatable interface
/// If the request is not valid, it will return a Result with the validation errors
/// If the request is valid, it will continue with the next behavior in the pipeline
/// </summary>
internal static class ValidationDecorator
{
    const string DefaultErrorMessage = "Validation failed for the request.";
    
    internal sealed class QueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> innerHandler)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            // Validate the query
            (bool isValid, string? errorMessage) = query.Validate();
            if (isValid)
                return await innerHandler.Handle(query, cancellationToken);
        
            return Result<TResponse>.Failure(errorMessage ?? DefaultErrorMessage);
        }
    }
    
    internal sealed class CommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> innerHandler)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            // Validate the command
            (bool isValid, string? errorMessage) = command.Validate();
            if (isValid)
                return await innerHandler.Handle(command, cancellationToken);
        
            return Result<TResponse>.Failure(errorMessage ?? DefaultErrorMessage);
        }
    }
}
