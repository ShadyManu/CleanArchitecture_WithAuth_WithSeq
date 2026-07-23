namespace Domain.Common.Constants;

public static class DbConstraints
{
    public const short CreatedByMaxLength = 50;
    public const short LastModifiedByMaxLength = 50;
    
    public const short MinToDoNameLength = 1;
    public const short MaxToDoNameLength = 500;
    public const short MaxToDoNoteLength = 500;

    // Shared length for all snake_case string-backed enum columns
    public const short EnumMaxLength = 50;
    
    // User
    public const short EmailMinLength = 4;
    public const short EmailMaxLength = 256;
    public const short UserUsernameMinLength = 3;
    public const short UserUsernameMaxLength = 20;

    public const short ProviderIdMaxLength = 128;
    public const short TokenHashMaxLength = 512;
    public const short ProviderRefreshTokenMaxLength = 2048;
    public const short DeviceIdMaxLength = 128;
}
