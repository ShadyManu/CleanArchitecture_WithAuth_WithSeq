namespace Application.UnitTests.Constants;

public static class ToDoTestsConstants
{
    public static readonly Guid InvalidToDoId = Guid.Empty;
    public static readonly Guid ValidToDoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    
    public const string ValidTitle = "Valid ToDo Title";
    public const int ValidPriority = 1;
    public const string ValidNote = "Valid ToDo Note";
    
    public const string UpdatedTitle = "Updated ToDo Title";
    public const int UpdatedPriority = 2;
    public const string UpdatedNote = "Updated ToDo Note";
}
