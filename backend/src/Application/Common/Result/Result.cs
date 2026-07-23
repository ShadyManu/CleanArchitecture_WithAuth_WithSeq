using Newtonsoft.Json;

namespace Application.Common.Result;

public class Result<TResult>
{
    public TResult? Data { get; init; }
    public ResultError? Error { get; init; }
    
    // This constructor is used just for Application.IntegrationTests
    [JsonConstructor]
    public Result() {}

    private Result(TResult? data)
    {
        Data = data;
        Error = null;
    }

    private Result(ResultError error)
    {
        Data = default;
        Error = error;
    }
    
    public static Result<TResult> Success(TResult value) =>
        new(value);
    public static Result<TResult> Failure(string error, string? innerException = null) =>
        new(new ResultError(error, innerException));
}

public record ResultError(string Message, string? InnerException = null);
