---
name: add-unit-tests
description: Add or improve xUnit v3 and Moq unit tests for this backend's command/query validation, handlers, mappers, provider services, and domain behavior. Use when functionality changes, coverage is missing, a regression needs locking down, or validation/error contracts must be tested precisely.
---

# Add unit tests

Read `../dotnet-conventions/SKILL.md`, the production path, and the nearest analogous test class.

## Derive the test matrix

List before coding:

- every validation boundary and valid boundary;
- every explicit `Result<T>.Failure` branch;
- the success result and mapped fields;
- every material repository/provider interaction;
- cancellation propagation when behavior depends on it;
- security-sensitive negative paths.

## Validation tests

- Test below-minimum, exact minimum, exact maximum, and above-maximum where applicable.
- Create boundary text with `new string('*', DbConstraints.X)` and `new string('*', DbConstraints.X + 1)`.
- Prefer `TheoryData`/`MemberData` for computed values and expected messages.
- Compare the exact production contract:

```csharp
Assert.Equal(
    ValidatorMessage.MaxLength(nameof(SomeCommand.Value), DbConstraints.SomeMaxLength),
    errorMessage);
```

- Do not assert only `IsValid == false`; assert the canonical error message.

## Handler tests

- Mock Application interfaces with Moq, not the handler itself.
- Cover success and every meaningful failure separately.
- Assert both `Data` and `Error` semantics.
- Compare exact `ErrorMessage` values.
- Verify important calls with the correct arguments, cancellation token, and `Times.Once`/`Times.Never`.
- For failed preconditions, prove no mutation/save/external side effect occurred.
- For created/updated entities, inspect the object passed to the repository.

## Structure and run

- Mirror the production feature under `backend/tests/Application.UnitTests/Tests`.
- Follow existing namespace, xUnit, regions, Arrange/Act/Assert, and naming style.
- Run:

```powershell
dotnet test backend/tests/Application.UnitTests/Application.UnitTests.csproj
```

Report exact passed, failed, and skipped counts. Never weaken, skip, or delete a valid test to make the suite pass.
