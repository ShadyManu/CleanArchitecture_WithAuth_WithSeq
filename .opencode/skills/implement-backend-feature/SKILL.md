---
name: implement-backend-feature
description: Coordinate a complete backend feature across Domain, Application, Infrastructure, Presentation, Web configuration, migrations, and tests. Use when a request spans multiple layers or asks to implement an end-to-end capability rather than only one command, query, entity, or test.
---

# Implement a backend feature

Read `../codebase-map/SKILL.md` and `../dotnet-conventions/SKILL.md`. Inspect the nearest complete feature before designing files.

## Workflow

1. Define the contract:
   - inputs and outputs;
   - mutating commands and read-only queries;
   - validation boundaries;
   - expected failure messages;
   - authorization/ownership;
   - persistence and external side effects.
2. List affected layers and existing primitives to reuse.
3. If new persisted state is required, follow `$add-domain-entity`.
4. Implement each mutation with `$add-backend-command`.
5. Implement each read with `$add-backend-query`.
6. Generate persistence changes with `$add-ef-migration`.
7. Add focused unit coverage with `$add-unit-tests`.
8. Add real HTTP/persistence coverage with `$add-integration-tests`.
9. Run `$review-backend-change` against the complete diff.
10. Build the solution and run all affected suites.

## Completion criteria

- Every requested behavior has an observable test.
- Every expected failure uses the established exact message.
- No layer boundary or naming law fails.
- Automatic registration is used correctly.
- Persisted model and migration snapshot agree.
- Provider/security flows fail closed.
- No secret or machine-specific artifact is publishable.
- Exact build/test results and environmental gaps are reported.

If a requested feature would require changing a core primitive, HTTP envelope, dependency direction, or security model, surface that architectural decision before implementing a divergent pattern.
