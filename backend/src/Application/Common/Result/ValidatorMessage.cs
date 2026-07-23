namespace Application.Common.Result;

public static class ValidatorMessage
{
    public const string InvalidGuid = "You have provided wrong Id(s).";
    
    public static string MinValue(string propertyName, short minValue) =>
        $"The field '{propertyName}' must be more than {minValue}.";
    public static string MaxValue(string propertyName, short maxValue) =>
        $"The field '{propertyName}' must be less than {maxValue}.";
    public static string MinLength(string propertyName, short minLength) =>
        $"The field '{propertyName}' must be more than {minLength} characters.";
    public static string MaxLength(string propertyName, short maxLength) =>
        $"The field '{propertyName}' must be less than {maxLength} characters.";
    public static string MinCount(string propertyName, short minCount) =>
        $"The field '{propertyName}' must have more than {minCount} entries.";
    public static string MaxCount(string propertyName, short maxCount) =>
        $"The field '{propertyName}' must have less than {maxCount} entries.";
    
    public static string CannotBeEmpty(string propertyName) =>
        $"The field '{propertyName}' cannot be empty.";
}
