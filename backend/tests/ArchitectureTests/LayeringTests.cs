using System.Reflection;
using Xunit;

namespace ArchitectureTests;

public sealed class LayeringTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.ToDoEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationDependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureDependencyInjection).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(Presentation.PresentationDependencyInjection).Assembly;

    [Fact]
    public void Domain_ShouldNotReferenceOuterLayers() =>
        AssertDoesNotReference(
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            PresentationAssembly);

    [Fact]
    public void Application_ShouldNotReferenceInfrastructurePresentationOrWeb() =>
        AssertDoesNotReference(
            ApplicationAssembly,
            InfrastructureAssembly,
            PresentationAssembly,
            typeof(Web.Program).Assembly);

    [Fact]
    public void Infrastructure_ShouldNotReferencePresentationOrWeb() =>
        AssertDoesNotReference(
            InfrastructureAssembly,
            PresentationAssembly,
            typeof(Web.Program).Assembly);

    [Fact]
    public void Presentation_ShouldNotReferenceInfrastructureOrWeb() =>
        AssertDoesNotReference(
            PresentationAssembly,
            InfrastructureAssembly,
            typeof(Web.Program).Assembly);

    private static void AssertDoesNotReference(Assembly source, params Assembly[] forbidden)
    {
        var references = source.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        var violations = forbidden
            .Select(assembly => assembly.GetName().Name)
            .Where(name => name is not null && references.Contains(name))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{source.GetName().Name} references forbidden layer(s): {string.Join(", ", violations)}");
    }
}
