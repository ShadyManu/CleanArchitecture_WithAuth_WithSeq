---
name: add-backend-command
description: Create or modify a state-changing CQRS command in this backend, including pure request validation, Result-based handler outcomes, persistence, mapping, Carter routing, and appropriate unit/integration tests. Use for create, update, delete, logout, refresh, sign-in, revoke, or any use case that mutates state.
---

# Add a backend command

Read `../dotnet-conventions/SKILL.md` and inspect the closest command, DTO, endpoint, and tests before editing.

## Workflow

1. Define the observable success response and every expected failure.
2. Reuse or add Request/Response DTOs under `Application/Dtos/<Feature>`.
3. Add a record named `<Action><Subject>Command` under `Application/Features/<Feature>/Commands`.
4. Implement pure `Validate()` rules only for request shape:
   - use `DbConstraints`;
   - return exact `ValidatorMessage` values;
   - use `nameof`;
   - do no I/O.
5. Add the matching `internal sealed ...CommandHandler` in the same file.
6. Inject only Application abstractions and propagate the cancellation token.
7. Enforce data-dependent existence, uniqueness, ownership, and state rules in the handler.
8. Persist through the repository:
   - add or load tracked entities for normal changes;
   - use set-based delete/update methods where appropriate;
   - call `SaveChangesAsync` for tracked changes;
   - treat an unexpected zero-row save as the established `ErrorMessage` failure.
9. Map entities with the feature mapper and return only `Result<T>.Success`/`Failure`.
10. Add or update the route in the feature's sealed Carter `*Endpoints` module. Keep the lambda to bind -> construct -> handle -> return.
11. Add validation and handler unit tests with `$add-unit-tests`.
12. Add HTTP/persistence tests with `$add-integration-tests` when routing or persistence behavior changes.
13. Build and run every affected test project.

## Guardrails

- Do not register the handler manually; Application scans it.
- Do not put repository calls or business rules in Presentation.
- Do not throw for expected failures.
- Do not return an entity from the command.
- Do not accept server-controlled identity/audit fields in payloads.
- Do not omit authorization or ownership checks for user-owned state.
- Never log credentials, tokens, authorization codes, or request payloads.

Before finishing, enumerate the implemented success/failure paths and map each to a test.
