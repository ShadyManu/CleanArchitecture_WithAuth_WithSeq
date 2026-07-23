using System.Reflection;
using Application.Common.Interfaces.CQRS;
using Carter;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchitectureTests;

public sealed class NamingConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationDependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureDependencyInjection).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(Presentation.PresentationDependencyInjection).Assembly;

    [Fact]
    public void Endpoints_ShouldFollowCarterConvention()
    {
        var endpoints = ArchitectureTestUtilities.TypesIn(PresentationAssembly)
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           typeof(ICarterModule).IsAssignableFrom(type))
            .ToArray();

        ArchitectureTestUtilities.AssertNoViolations(
            endpoints.Where(type => !type.Name.EndsWith("Endpoints", StringComparison.Ordinal)),
            "Carter modules must have names ending in 'Endpoints'.");
        ArchitectureTestUtilities.AssertNoViolations(
            endpoints.Where(type => !type.IsSealed),
            "Endpoint classes must be sealed.");
        ArchitectureTestUtilities.AssertNoViolations(
            endpoints.Where(type => !HasMatchingEndpointTag(type)),
            "EndpointTag must be a private const string equal to the class name without 'Endpoints'.");
    }

    [Fact]
    public void Repositories_ShouldMatchTheirInterfaces()
    {
        var applicationTypes = ArchitectureTestUtilities.TypesIn(ApplicationAssembly);
        var repositories = ArchitectureTestUtilities.TypesIn(InfrastructureAssembly)
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.Namespace?.Contains(".Repositories", StringComparison.Ordinal) == true &&
                           type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToArray();

        var violations = repositories.Where(repository =>
        {
            var expectedInterfaceName = $"I{repository.Name}";
            return !applicationTypes.Any(candidate =>
                candidate.IsInterface &&
                candidate.Name == expectedInterfaceName &&
                candidate.IsAssignableFrom(repository));
        });

        ArchitectureTestUtilities.AssertNoViolations(
            violations,
            "Each concrete FooRepository must implement IFooRepository.");
    }

    [Fact]
    public void EntityFrameworkConfigurations_ShouldEndWithConfiguration()
    {
        var configurations = ArchitectureTestUtilities.TypesIn(InfrastructureAssembly)
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           ArchitectureTestUtilities.ImplementsOpenGeneric(
                               type,
                               typeof(IEntityTypeConfiguration<>)));

        ArchitectureTestUtilities.AssertNoViolations(
            configurations.Where(type =>
                !ArchitectureTestUtilities.SimpleName(type)
                    .EndsWith("Configuration", StringComparison.Ordinal)),
            "EF Core configuration types must have names ending in 'Configuration'.");
    }

    [Fact]
    public void CommandsAndQueries_ShouldBeRecordsWithMatchingNames()
    {
        var requests = ArchitectureTestUtilities.TypesIn(ApplicationAssembly)
            .Where(type =>
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(ICommand<>)) ||
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(IQuery<>)))
            .ToArray();

        ArchitectureTestUtilities.AssertNoViolations(
            requests.Where(type =>
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(ICommand<>)) &&
                !type.Name.EndsWith("Command", StringComparison.Ordinal)),
            "ICommand implementations must have names ending in 'Command'.");
        ArchitectureTestUtilities.AssertNoViolations(
            requests.Where(type =>
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(IQuery<>)) &&
                !type.Name.EndsWith("Query", StringComparison.Ordinal)),
            "IQuery implementations must have names ending in 'Query'.");
        ArchitectureTestUtilities.AssertNoViolations(
            requests.Where(type => !IsRecord(type)),
            "Commands and queries must be records.");
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldFollowConvention()
    {
        var handlers = ArchitectureTestUtilities.TypesIn(ApplicationAssembly)
            .Where(type => type.Namespace?.StartsWith(
                               "Application.Features",
                               StringComparison.Ordinal) == true &&
                           (
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(ICommandHandler<,>)) ||
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(IQueryHandler<,>))))
            .ToArray();

        ArchitectureTestUtilities.AssertNoViolations(
            handlers.Where(type =>
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(ICommandHandler<,>)) &&
                !type.Name.EndsWith("CommandHandler", StringComparison.Ordinal)),
            "ICommandHandler implementations must have names ending in 'CommandHandler'.");
        ArchitectureTestUtilities.AssertNoViolations(
            handlers.Where(type =>
                ArchitectureTestUtilities.ImplementsOpenGeneric(type, typeof(IQueryHandler<,>)) &&
                !type.Name.EndsWith("QueryHandler", StringComparison.Ordinal)),
            "IQueryHandler implementations must have names ending in 'QueryHandler'.");
        ArchitectureTestUtilities.AssertNoViolations(
            handlers.Where(type => type.IsPublic || !type.IsSealed),
            "Command and query handlers must be internal sealed.");
    }

    [Fact]
    public void Dtos_ShouldEndWithRequestOrResponse()
    {
        var dtos = ArchitectureTestUtilities.TypesIn(ApplicationAssembly)
            .Where(type => type.Namespace?.StartsWith("Application.Dtos", StringComparison.Ordinal) == true);

        ArchitectureTestUtilities.AssertNoViolations(
            dtos.Where(type =>
                !type.Name.EndsWith("Request", StringComparison.Ordinal) &&
                !type.Name.EndsWith("Response", StringComparison.Ordinal)),
            "DTO names must end in 'Request' or 'Response'.");
    }

    private static bool HasMatchingEndpointTag(Type endpoint)
    {
        var field = endpoint.GetField(
            "EndpointTag",
            BindingFlags.Static | BindingFlags.NonPublic);
        var expected = endpoint.Name[..^"Endpoints".Length];

        return field is { IsLiteral: true, FieldType: not null } &&
               field.FieldType == typeof(string) &&
               string.Equals(field.GetRawConstantValue() as string, expected, StringComparison.Ordinal);
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
}
