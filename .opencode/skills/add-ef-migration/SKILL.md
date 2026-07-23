---
name: add-ef-migration
description: Generate, inspect, and verify an EF Core PostgreSQL migration for this backend after a model/configuration change. Use when adding or changing entities, properties, constraints, indexes, relationships, schemas, or table mappings, or when a pending persistence change lacks a migration.
---

# Add an EF Core migration

Read `../dotnet-conventions/SKILL.md` and inspect the model/configuration diff first.

## Workflow

1. Confirm the model is complete:
   - Domain property/nullability;
   - `DbConstraints`;
   - `DbSet`;
   - `DatabaseConstants`;
   - entity configuration;
   - relationships and indexes.
2. Choose a specific PascalCase migration name describing the schema change.
3. Build:

```powershell
dotnet build backend/src/Web/Web.csproj
```

4. Generate from the repository root:

```powershell
dotnet ef migrations add <MigrationName> --project backend/src/Infrastructure/Infrastructure.csproj --startup-project backend/src/Web/Web.csproj --output-dir Data/Migrations --no-build
```

5. Inspect the new migration, designer, and `ApplicationDbContextModelSnapshot`.
6. Verify:
   - correct schema/table/column names;
   - types, nullability, lengths, defaults;
   - keys, indexes, uniqueness;
   - foreign keys and delete behavior;
   - `Down` reverses `Up`;
   - no unrelated model drift.
7. If any error, never edit the migration/snapshot manually, but fix the issue and regenerate the migration.
8. Build Infrastructure/Web and run architecture plus affected integration tests.

## Guardrails

- Generate migrations; do not hand-write them or patch the snapshot to imitate generation.
- Do not remove or rewrite prior migrations unless explicitly requested and safe.
- Do not run `dotnet ef database update` against an unspecified database.
- If generation reveals unrelated snapshot drift, stop and explain it instead of silently publishing it.
- Never claim the migration applies cleanly unless a disposable/test database path actually verified it.
