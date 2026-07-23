---
name: dotnet-conventions
description: Project-specific .NET 10 and C# conventions for this Clean Architecture backend. Use whenever creating, changing, testing, or reviewing Domain, Application, Infrastructure, Presentation, or Web code. Covers the repository's custom CQRS and Result contracts, validation, entities, EF Core, repositories, Carter endpoints, security, tests, and migrations.
---

# .NET conventions

Read `../codebase-map/SKILL.md` for locations. Inspect the closest existing feature before choosing a shape. The current filesystem and `backend/tests/ArchitectureTests` are authoritative.

## Architecture

Enforce these dependencies:

```text
Domain <- Application <- Infrastructure
                     <- Presentation
Web is the composition root.
```

- Domain must not reference Application, Infrastructure, Presentation, or Web.
- Application must not reference Infrastructure, Presentation, Web, EF Core, or ASP.NET Core.
- Infrastructure must not reference Presentation or Web.
- Presentation must not reference Infrastructure or Web.
- Persistence mapping belongs in Infrastructure, not on domain entities.
- Endpoints return Application DTOs wrapped in `Result<T>`, never entities.
- Keep package versions in `backend/Directory.Packages.props`; omit versions from project files.
- Preserve nullable reference types, implicit usings, and warnings-as-errors.

Do not "fix" a failing architecture test by weakening it unless the requested change explicitly changes the architecture.

## Existing primitives

Reuse instead of duplicating:

- `ICommand<TResponse>` and `IQuery<TResponse>` extend `IValidatable`.
- Requests are records.
- The matching handler is `internal sealed`, lives in the same file, accepts a `CancellationToken`, and returns `Task<Result<TResponse>>`.
- `Result<T>.Success` and `Result<T>.Failure` are the only result factories.
- `DbConstraints`, `ValidatorMessage`, `ErrorMessage`, and `DatabaseConstants` own shared limits and messages.
- Static mapper extensions own entity/DTO mapping.
- `BaseGuidEntity` supplies UUIDv7 identity and audit fields.
- `AuditableEntityInterceptor` owns audit timestamps and actors.

Do not introduce a second mediator, result type, validator framework, mapper framework, or repository abstraction.

## Commands and queries

- Put feature requests under `backend/src/Application/Features/<Feature>/Commands` or `Queries`.
- Existing features may be flat or split into action folders. Match the feature being changed.
- Name requests `<Action><Subject>Command` or `<Action><Subject>Query`.
- Name handlers with the matching `CommandHandler` or `QueryHandler` suffix.
- Inject Application abstractions only. Do not resolve services through `IServiceProvider`.
- Propagate the received cancellation token to every cancellable async call.
- Queries are read-only and use no-tracking repository methods.
- Commands use tracked entities or set-based writes and explicitly inspect persistence outcomes when applicable.
- Do not call `SaveChangesAsync` in a loop.
- Return expected failures through `Result<T>.Failure` with `ErrorMessage`; do not throw for expected control flow.

Handlers are discovered automatically. Do not register them individually.

## Validation

- Override `Validate()` for every command/query that accepts data needing shape validation.
- Keep validation pure and synchronous: no database, network, time-dependent work, or exceptions.
- Reject empty identifiers with `ValidatorMessage.InvalidGuid`.
- Reject null, empty, or whitespace values where the contract requires content.
- Use the matching constant from `DbConstraints`.
- Use `ValidatorMessage.MinLength`/`MaxLength` for strings and `MinValue`/`MaxValue` for numbers.
- Pass `nameof(Property)` to message factories.
- Return the exact canonical message that tests and callers expect.
- Put existence, ownership, uniqueness, and state-transition checks in the handler as Result failures.
- Do not repeat in the handler what `Validate()` already guarantees.
- Keep EF max lengths synchronized with validation constants.

## Decorator pipeline

Handlers are scanned and decorated in `ApplicationDependencyInjection`. Registration order currently makes logging outermost, then validation, then unhandled-exception handling around the handler. Preserve this deliberately established order.

- Never log command/query payloads, tokens, credentials, authorization codes, or personal data.
- Log with structured templates.
- Treat the unhandled-exception decorator as a safety net, not as a business-error channel.

## Domain entities

- Persisted entities are classes named with the `Entity` suffix; the architecture suite enforces this.
- New Guid entities normally inherit `BaseGuidEntity`.
- Use `required` for mandatory properties and make nullability agree with the database.
- Use `init` only for genuinely set-once values; use setters where existing update handlers require mutation.
- Do not assign audit fields manually.
- Do not assign `Guid.NewGuid()` to entity identifiers; `BaseGuidEntity` uses `Guid.CreateVersion7()`.
- Do not add EF attributes or Infrastructure types to Domain.
- Add reusable string/range constraints as `const short` values in `DbConstraints`.

Follow the repository's current entity model; do not impose factories, aggregate roots, domain events, or private setters unless the task explicitly introduces that architectural change.

## Repositories and EF Core

- Declare repository interfaces in Application and implement them in Infrastructure.
- For Guid entities, extend `IBaseGuidRepository<TEntity>` and `BaseGuidRepository<TEntity>` when their semantics fit.
- Name the pair `IFooRepository` / `FooRepository`; architecture tests verify the match.
- Place implementations under `Infrastructure.Data.Repositories`; matching repositories are registered automatically.
- Repositories never hide a save. Commands explicitly call `SaveChangesAsync` when using tracked changes/additions.
- Read-only methods use `AsNoTracking()` and an `AsNoTracking` suffix when exposed by a repository.
- Do not use a no-tracking entity for a tracked update.
- Filter and order in the database before materialization.
- Avoid N+1 queries, unbounded reads of growing tables, and `Count()` when only existence matters.
- Parameterize raw SQL. Never concatenate or interpolate untrusted values into SQL text.

For each persisted entity:

- Add a `DbSet<TEntity>` to `ApplicationDbContext`.
- Add one `IEntityTypeConfiguration<TEntity>` implementation named `*Configuration`.
- Inherit the applicable shared configuration and call `base.Configure(builder)` first.
- Use `DatabaseConstants` for table and schema names.
- Configure every property, requiredness, max length, conversion, relationship, delete behavior, uniqueness, and useful indexes.
- Ensure every string length uses the constant for that exact property.

Configurations are applied automatically. Do not register them manually.

## Migrations

- Generate migrations with the Infrastructure project and Web startup project.
- Never hand-author a migration or edit the model snapshot as a substitute for generation.
- Inspect generated `Up`, `Down`, and snapshot changes for exact names, nullability, lengths, indexes, relationships, and delete behavior.
- Do not run `database update` against an unspecified or non-test database.
- A persisted model change is incomplete without a migration unless the user explicitly defers it.

## DTOs, mapping, and endpoints

- Put DTOs below `Application/Dtos/<Feature>/Request|Response`.
- DTO names must end in `Request` or `Response`; they may be records or classes.
- Keep mapping in static `<Feature>Mapper` extension methods and map every property.
- Carter modules live in Presentation, are sealed, and end in `Endpoints`.
- Their private `EndpointTag` constant must equal the class name without `Endpoints`.
- Build a route group with tags, rate limiting, and OpenAPI inclusion.
- Add authorization when the route accesses user-owned or privileged state; keep intentionally anonymous routes explicit.
- Use route constraints such as `{id:guid}`.
- Endpoint lambdas only bind inputs, construct a command/query, resolve its handler, pass the cancellation token, and return the handler result.
- Carter discovers modules automatically.

Do not invent a different HTTP error envelope: this repository currently exposes `Result<T>` directly.

## Web and security

- Keep composition in the layer-specific dependency injection extensions.
- Preserve middleware order unless the task specifically requires and verifies a change.
- Never commit real secrets, credentials, provider tokens, private keys, connection strings, or personal data.
- Validate issuer, audience, signing key, and lifetime for JWTs.
- Require ownership checks before mutating user-owned resources.
- Bound collection sizes and result sizes for expensive routes.
- Keep rate limiting on route groups.
- Use `IHttpClientFactory`/typed clients for external calls.
- For external identity providers, verify provider subject consistency across every token/code exchange and fail closed on revocation.

## Tests

The stack is xUnit v3, Moq, ASP.NET Core `WebApplicationFactory`, EF Core, and PostgreSQL Testcontainers.

- Unit tests mirror feature folders under `backend/tests/Application.UnitTests/Tests`.
- Integration tests mirror endpoint areas under `backend/tests/Infrastructure.IntegrationTests/Tests`.
- Use Arrange/Act/Assert and descriptive scenario names.
- Test validation boundaries, every meaningful handler failure, success data/state, and critical collaborator interactions.
- Build boundary strings with `new string('*', DbConstraints.SomeLength + 1)` or the exact boundary value. Do not paste huge literals.
- Compare the exact `ValidatorMessage` or `ErrorMessage` that production must return.
- Use `MemberData`/`TheoryData` when cases need computed values or clearer expected messages; use `InlineData` for simple compile-time data.
- Verify repositories with Moq where the interaction is part of behavior.
- Integration tests exercise real HTTP and observable PostgreSQL state.
- Pass `TestContext.Current.CancellationToken` in integration tests.
- Do not weaken, skip, or delete a valid test to get a green build.

## Verification

Build before using `--no-build` test commands. Report exact passed, failed, and skipped counts. Run the smallest relevant project first, then all affected suites. Integration tests require Docker.
