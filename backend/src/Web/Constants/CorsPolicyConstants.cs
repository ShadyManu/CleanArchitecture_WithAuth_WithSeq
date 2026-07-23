namespace Web.Constants;

public static class CorsPolicyConstants
{
    public const string LocalPolicy = "LocalPolicy";
    public const string ProdPolicy = "ProdPolicy";

    public static readonly string[] LocalAllowedUrls =
    [
        "http://localhost:8100",
        "http://host.docker.internal:8100",
        "http://localhost:4200",
        "http://host.docker.internal:4200",
        "capacitor://localhost", // Capacitor iOS
        "https://localhost", // Capacitor android
        "http://localhost" // Capacitor android
    ];

    public static readonly string[] ProdAllowedUrls =
    [
        "capacitor://localhost", // Capacitor iOS
        "https://localhost" // Capacitor android
    ];
}
