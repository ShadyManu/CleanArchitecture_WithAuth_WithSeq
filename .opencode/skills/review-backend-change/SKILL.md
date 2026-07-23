---
name: review-backend-change
description: Review backend or .opencode changes before publication for correctness, regressions, Clean Architecture boundaries, validation/error contracts, security, EF Core behavior, provider flows, test quality, and public-repository safety. Use for audits, pre-commit checks, or requests to verify that a change is ready.
---

# Review a backend change

Do not modify code unless asked. Read `../dotnet-conventions/SKILL.md` and `../codebase-map/SKILL.md`.

## Establish scope

Inspect:

```powershell
git status --short
git diff -- backend .opencode
git diff --cached -- backend .opencode
```

Read untracked files listed by status; a diff alone does not include them. Review the complete affected call chain, not only changed lines.

## Checklist

1. Behavior: success path, failures, null/empty/boundary cases, cancellation, concurrency and external failures.
2. Architecture: project references, layer ownership, naming laws, automatic discovery conventions.
3. CQRS: read/write classification, validation placement, Result usage, save semantics, mapping completeness.
4. Persistence: tracking, query bounds, constraints, indexes, relationships, migration/snapshot coherence.
5. HTTP: binding, route constraints, authorization, rate limiting, response envelope.
6. Security: secret/PII leakage, token validation, ownership/IDOR, injection, overposting, provider subject/revocation fail-closed behavior.
7. Tests: every meaningful outcome, exact messages, computed boundary strings, real integration state, absence of weakened assertions.
8. Public safety: no local paths, credentials, private endpoints, personal data, stale project names, unfinished template markers, or instructions outside scope.

## Validation

Run the smallest relevant tests, architecture tests for structural changes, integration tests for HTTP/persistence changes, and a solution build when feasible. Report exact counts and any environment limitation.

## Output

Lead with findings ordered by severity. For each finding give `file:line`, concrete impact, and a precise fix. Distinguish proven defects from questions. If there are no findings, say so and state exactly what was executed and what remains unverified.
