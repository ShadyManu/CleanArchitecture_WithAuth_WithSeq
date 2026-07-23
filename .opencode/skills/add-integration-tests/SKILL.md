---
name: add-integration-tests
description: Add or improve HTTP-to-PostgreSQL integration tests for this backend using xUnit v3, WebApplicationFactory, and Testcontainers. Use for new or changed Carter routes, EF mappings, repositories, authentication flows, persistence behavior, HTTP binding, authorization, or regression coverage across layers.
---

# Add integration tests

Read `../dotnet-conventions/SKILL.md`, the endpoint, handler, EF configuration, factory, base test class, and nearest test file.

## Workflow

1. Define observable scenarios:
   - successful HTTP flow;
   - binding/shape failure;
   - each important Result failure;
   - authentication/authorization failure where applicable;
   - persisted state or deletion;
   - database constraint/relationship behavior when changed.
2. Reuse `IntegrationTestWebAppFactory` and the applicable base test.
3. Exercise the route through `HttpClient`; do not call handlers directly.
4. Deserialize the real `Result<T>` envelope and compare exact `ErrorMessage`/`ValidatorMessage` values.
5. Verify state through a follow-up HTTP request when that is the public behavior, or through a scoped `ApplicationDbContext` when persistence detail is the subject.
6. Use `AsNoTracking()` for verification reads.
7. Pass `TestContext.Current.CancellationToken` to async operations.
8. Keep provider integrations deterministic with the Testing environment's established fakes/mocks; never call real identity-provider production endpoints in the test suite.
9. Run:

```powershell
dotnet test backend/tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj
```

## Guardrails

- Docker is required for the PostgreSQL Testcontainer. If unavailable, report the suite as unverified; do not claim success.
- Do not replace PostgreSQL behavior with EF InMemory for integration coverage.
- Make each test independent; do not depend on execution order.
- Do not use real secrets, provider credentials, or personal data.
- Assert database state for create/update/delete behavior, not only a successful status code.
- Preserve the repository's current HTTP envelope and status semantics unless the requested change explicitly alters them.

Report exact passed, failed, and skipped counts.
