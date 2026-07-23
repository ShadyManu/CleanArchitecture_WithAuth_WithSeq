using System.Reflection;
using Xunit;

namespace ArchitectureTests;

internal static class ArchitectureTestUtilities
{
    public static IReadOnlyList<Type> TypesIn(Assembly assembly) =>
        assembly.GetTypes();

    public static bool ImplementsOpenGeneric(Type type, Type openGenericInterface) =>
        type.GetInterfaces().Any(candidate =>
            candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == openGenericInterface);

    public static string SimpleName(Type type)
    {
        var genericMarker = type.Name.IndexOf('`');
        return genericMarker < 0 ? type.Name : type.Name[..genericMarker];
    }

    public static void AssertNoViolations(IEnumerable<Type> violations, string rule)
    {
        var names = violations
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.True(
            names.Length == 0,
            $"{rule}{Environment.NewLine}{string.Join(Environment.NewLine, names)}");
    }
}
