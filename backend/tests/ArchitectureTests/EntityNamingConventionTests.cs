using Xunit;

namespace ArchitectureTests;

public sealed class EntityNamingConventionTests
{
    [Fact]
    public void DomainEntities_ShouldHaveNamesEndingWithEntity()
    {
        var violations = typeof(Domain.Entities.ToDoEntity).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.Namespace?.StartsWith("Domain.Entities", StringComparison.Ordinal) == true &&
                           !type.Name.EndsWith("Entity", StringComparison.Ordinal))
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Domain entity names must end in 'Entity'.{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }
}
