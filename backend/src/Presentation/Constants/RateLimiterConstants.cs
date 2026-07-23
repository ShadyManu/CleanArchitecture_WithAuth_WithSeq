namespace Presentation.Constants;

public static class RateLimiterConstants
{
    public const string AnonymousUserPolicy = "anonymous";
    public const short AnonymousUserPermitLimit = 30;
    public const short AnonymousUserWindowSeconds = 10;
    
    public const string AuthenticatedUserPolicy = "authenticated";
    public const short AuthenticatedUserPermitLimit = 30;
    public const short AuthenticatedUserWindowSeconds = 10;
}
