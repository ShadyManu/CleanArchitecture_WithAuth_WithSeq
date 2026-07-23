namespace Application.Common.Result;

public static class ErrorMessage
{
    public const string NotFound = "Resource not found";
    public const string InvalidUser = "Invalid User";
    public const string UnauthorizedAction = "Unauthorized Action";
    public const string InvalidUserInputs = "Invalid User Inputs";
    
    public const string EmailAlreadyExists = "Email already exists";
    public const string AppleReauthorizationRequired = "Sign in with Apple reauthorization is required";
    public const string AppleRevokeFailed = "Unable to revoke Sign in with Apple authorization";
    
    // Generic
    public const string SomethingWentWrong = "Something went wrong";
}
