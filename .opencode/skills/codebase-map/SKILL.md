---
name: codebase-map
description: Orientation map for this repository's .NET backend. Use at the start of tasks that need to locate layers, features, persistence, endpoints, tests, configuration, or verification commands. Treat the filesystem and executable architecture tests as the final authority when the map becomes stale.
---

# Codebase map

Use repository-root-relative paths. Verify a concrete symbol before editing it.

## Repository root

```text
backend/
|-- CCTemplate.slnx
|-- Directory.Build.props
|-- Directory.Packages.props
|-- src/
|   |-- Domain/
|   |-- Application/
|   |-- Infrastructure/
|   |-- Presentation/
|   `-- Web/
`-- tests/
    |-- Application.UnitTests/
    |-- Infrastructure.IntegrationTests/
    `-- ArchitectureTests/
```

The dependency direction is:

```text
Domain <- Application <- Infrastructure
                     <- Presentation
Web is the composition root and references all layers.
```

The executable rules are in `backend/tests/ArchitectureTests`.

## Layer ownership

- `backend/src/Domain`: entities, base entity contracts/models, enums, and `DbConstraints`.
- `backend/src/Application`: CQRS contracts and decorators, feature commands/queries, DTOs, mappers, repository/provider interfaces and options.
- `backend/src/Infrastructure`: EF Core context/configurations/migrations/repositories, token and external-provider services, persistence and infrastructure DI.
- `backend/src/Presentation`: Carter modules and endpoint-specific constants.
- `backend/src/Web`: host, middleware, authentication, rate limiting, CORS, observability, configuration and composition.

## Feature entry points

### Authentication

- Commands: `backend/src/Application/Features/Auth/Commands`
- Request/response DTOs: `backend/src/Application/Dtos/Auth`
- Provider abstractions: `backend/src/Application/Common/Interfaces/Auth`
- Provider services: `backend/src/Infrastructure/Auth/Providers`
- Token service: `backend/src/Infrastructure/Auth`
- Repositories: `backend/src/Application/Common/Interfaces/Repositories/Auth` and `backend/src/Infrastructure/Data/Repositories/Auth`
- Endpoints: `backend/src/Presentation/Endpoints/AuthEndpoints.cs`
- Unit tests: `backend/tests/Application.UnitTests/Tests/Auth`
- Integration tests: `backend/tests/Infrastructure.IntegrationTests/Tests/Auth`

### ToDo

- Entity: `backend/src/Domain/Entities/ToDoEntity.cs`
- Commands/queries: `backend/src/Application/Features/ToDo`
- DTOs: `backend/src/Application/Dtos/ToDo`
- Mapper: `backend/src/Application/Mapper/ToDoMapper.cs`
- Repository contract: `backend/src/Application/Common/Interfaces/Repositories/IToDoRepository.cs`
- Repository: `backend/src/Infrastructure/Data/Repositories/ToDo/ToDoRepository.cs`
- EF configuration: `backend/src/Infrastructure/Data/Configurations/ToDoConfiguration.cs`
- Endpoints: `backend/src/Presentation/Endpoints/ToDoEndpoints.cs`
- Unit tests: `backend/tests/Application.UnitTests/Tests/ToDo`
- Integration tests: `backend/tests/Infrastructure.IntegrationTests/Tests/ToDo`

## Sources of truth

- Database limits: `backend/src/Domain/Common/Constants/DbConstraints.cs`
- Business failure messages: `backend/src/Application/Common/Result/ErrorMessage.cs`
- Validation messages: `backend/src/Application/Common/Result/ValidatorMessage.cs`
- Result contract: `backend/src/Application/Common/Result/Result.cs`
- CQRS interfaces: `backend/src/Application/Common/Interfaces/CQRS/CommandQueryInterfaces.cs`
- Handler registration/decorator order: `backend/src/Application/ApplicationDependencyInjection.cs`
- Table/schema names: `backend/src/Infrastructure/Data/TableNames/DatabaseConstants.cs`
- DbContext and configuration discovery: `backend/src/Infrastructure/Data/ApplicationDbContext.cs`
- Repository discovery: `backend/src/Infrastructure/InfrastructureDependencyInjection.cs`
- Endpoint discovery: `backend/src/Presentation/PresentationDependencyInjection.cs`
- Host pipeline: `backend/src/Web/Program.cs`
- Package versions: `backend/Directory.Packages.props`

## Automatic registration

- Command/query handlers are scanned and decorated in `ApplicationDependencyInjection`.
- Concrete repositories named `*Repository` under `Infrastructure.Data.Repositories` are registered as their matching interfaces.
- EF configurations implementing `IEntityTypeConfiguration<T>` are applied from the Infrastructure assembly.
- Carter modules implementing `ICarterModule` are discovered by Carter.

Do not add manual registrations for types covered by these scans.

## Persistence locations

- DbSets: `backend/src/Infrastructure/Data/ApplicationDbContext.cs`
- Shared configurations: `backend/src/Infrastructure/Data/Configurations/Common`
- Entity configurations: `backend/src/Infrastructure/Data/Configurations`
- Repositories: `backend/src/Infrastructure/Data/Repositories`
- Migrations: `backend/src/Infrastructure/Data/Migrations`
- Audit stamping: `backend/src/Infrastructure/Data/Interceptors/AuditableEntityInterceptor.cs`

## Verification commands

Run from the repository root:

```powershell
dotnet build backend/CCTemplate.slnx --no-restore
dotnet test backend/tests/Application.UnitTests/Application.UnitTests.csproj --no-build
dotnet test backend/tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj --no-build
dotnet test backend/tests/ArchitectureTests/ArchitectureTests.csproj --no-build
```

Integration tests require Docker because they use a PostgreSQL Testcontainer.

## Maintenance

Keep this map compact. Update it only for stable structural changes. If a listed path disagrees with the repository, trust the repository, complete the task using observed paths, and update this map in the same change.
