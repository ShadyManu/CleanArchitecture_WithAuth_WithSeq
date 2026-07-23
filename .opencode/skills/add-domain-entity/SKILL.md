---
name: add-domain-entity
description: "Add a persisted domain entity through the complete Clean Architecture chain used by this backend: Domain entity and DbConstraints, Application repository contract, Infrastructure repository and EF configuration, DbContext DbSet, table constants, generated migration, and relevant tests. Use whenever adding a new table-backed entity or aggregate."
---

# Add a domain entity

Read `../dotnet-conventions/SKILL.md`, `../codebase-map/SKILL.md`, and the closest existing entity/configuration/repository before editing.

## Required chain

1. **Domain entity**
   - Create `backend/src/Domain/Entities/<Area>/<Name>Entity.cs` when an area folder exists, otherwise use the established feature location.
   - Use a class ending in `Entity`.
   - Inherit `BaseGuidEntity` for a normal Guid-keyed persisted entity.
   - Model requiredness and mutability intentionally.
   - Keep EF Core and persistence attributes out of Domain.
2. **Constraints**
   - Add reusable string/range limits to `DbConstraints`.
   - Name constants for the exact entity/property.
3. **Application repository**
   - Add `I<Name>Repository : IBaseGuidRepository<<Name>Entity>` when the base contract fits.
   - Add only feature-specific operations actually required by current use cases.
4. **Infrastructure repository**
   - Add `<Name>Repository : BaseGuidRepository<<Name>Entity>, I<Name>Repository`.
   - Place it below `Infrastructure.Data.Repositories`.
   - Use no-tracking reads for read-only operations.
   - Do not add manual DI registration: matching repositories are assembly-scanned.
5. **DbContext**
   - Add `DbSet<<Name>Entity>` to `ApplicationDbContext`.
6. **Database constants**
   - Add table/schema names to `DatabaseConstants`.
7. **EF configuration**
   - Add `<Name>Configuration : BaseGuidEntityConfiguration<<Name>Entity>`.
   - Call `base.Configure(builder)` first.
   - Configure the table, every property, nullability, exact max-length constant, conversions, relationships, indexes, uniqueness, and delete behavior.
   - Do not register the configuration manually; the assembly scan applies it.
8. **Migration**
   - Build first.
   - Use `$add-ef-migration` to generate and inspect the migration.
9. **Tests**
   - Add domain unit tests only for real behavior/invariants.
   - Add handler tests for use cases consuming the entity.
   - Add integration coverage that proves mapping, constraints, relationships, and persistence.
10. **Verification**

- Run architecture tests, affected unit tests, affected integration tests, and a solution build.

## Completeness check

Before finishing, search the entity name across Domain, Application, Infrastructure, tests, and the generated migration. Confirm repository/configuration automatic registration conventions and ensure no property is missing from mapping or persistence configuration.
