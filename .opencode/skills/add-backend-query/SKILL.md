---
name: add-backend-query
description: Create or modify a read-only CQRS query in this backend, including optional pure validation, no-tracking repository access, DTO mapping, Carter routing, pagination/bounds, and unit/integration tests. Use for get-by-id, lists, searches, lookups, and any operation that must not mutate state.
---

# Add a backend query

Read `../dotnet-conventions/SKILL.md` and inspect the closest query, repository method, mapper, endpoint, and tests.

## Workflow

1. Define response shape, empty-result behavior, not-found behavior, authorization, filters, ordering, and bounds.
2. Reuse or add a Response DTO under `Application/Dtos/<Feature>/Response`.
3. Add a record named `<Action><Subject>Query` under `Application/Features/<Feature>/Queries`.
4. Override `Validate()` only when the query accepts values requiring shape checks. Use `DbConstraints`, `ValidatorMessage`, and `nameof`.
5. Add the matching `internal sealed ...QueryHandler` in the same file.
6. Read only through an Application repository interface.
7. Use an existing no-tracking method or add an intention-revealing `...AsNoTrackingAsync` method.
8. Apply filters, ordering, projection choices, and pagination before materialization.
9. Return a Response DTO or `IReadOnlyList<Response>` through `Result<T>`.
10. Use the repository's established not-found/empty-list semantics consistently.
11. Add or update the route in the feature's Carter module.
12. Add validation and handler unit tests with `$add-unit-tests`.
13. Add HTTP tests with `$add-integration-tests` for a new/changed route or persistence query.
14. Build and run affected tests.

## Guardrails

- A query never adds, updates, deletes, saves, revokes, or triggers an external side effect.
- Do not use a tracked repository method for a read-only query.
- Do not return entities.
- Do not materialize before filtering or ordering.
- Bound results that can grow; do not add an unbounded list endpoint by default.
- Verify ownership/authorization for user-scoped records.
- Do not register handlers, repositories, EF configurations, or Carter modules manually when automatic discovery applies.
