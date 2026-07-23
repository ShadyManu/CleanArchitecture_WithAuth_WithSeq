using System.Diagnostics;
using Application.Common.Interfaces.CQRS;
using Application.Common.Result;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviours;

/// <summary>
/// Wraps every Command/Query handler with structured start/completion logging (name, duration, outcome).
/// Registered as the outermost decorator so it also captures validation failures, not just unhandled exceptions.
/// Payloads are intentionally not logged here (some commands/queries carry credentials or tokens);
/// log business-relevant details from inside the handler instead.
/// </summary>
internal static partial class LoggingDecorator
{
    internal sealed partial class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<TQuery> logger)
            : IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var queryName = typeof(TQuery).Name;
            var stopwatch = Stopwatch.StartNew();

            var result = await innerHandler.Handle(query, cancellationToken);

            if (result.Error is null)
            {
                logger.QueryHandled(queryName, stopwatch.ElapsedMilliseconds);
            }
            else
                logger.QueryFailed(queryName, stopwatch.ElapsedMilliseconds, result.Error.Message, result.Error.InnerException);

            return result;
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<TCommand> logger)
            : ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var commandName = typeof(TCommand).Name;
            var stopwatch = Stopwatch.StartNew();

            var result = await innerHandler.Handle(command, cancellationToken);

            if (result.Error is null)
                logger.CommandHandled(commandName, stopwatch.ElapsedMilliseconds);
            else
                logger.CommandFailed(commandName, stopwatch.ElapsedMilliseconds, result.Error.Message, result.Error.InnerException);

            return result;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled query {QueryName} in {ElapsedMilliseconds}ms")]
    private static partial void QueryHandled(this ILogger logger, string queryName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query {QueryName} failed in {ElapsedMilliseconds}ms: {Error}. Inner Exception: {InnerException}")]
    private static partial void QueryFailed(this ILogger logger, string queryName, long elapsedMilliseconds, string error, string? innerException);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled command {CommandName} in {ElapsedMilliseconds}ms")]
    private static partial void CommandHandled(this ILogger logger, string commandName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Command {CommandName} failed in {ElapsedMilliseconds}ms: {Error}. Inner Exception: {InnerException}")]
    private static partial void CommandFailed(this ILogger logger, string commandName, long elapsedMilliseconds, string error, string? innerException);
}
