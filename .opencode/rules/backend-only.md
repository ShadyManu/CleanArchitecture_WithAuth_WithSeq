# Repository operating rule

All implementation, review, tests, commands, paths, and examples must stay within `backend/**` and `.opencode/**`.

Use these authorities in order:

1. The current filesystem and source code.
2. `backend/tests/ArchitectureTests`.
3. `.opencode/skills/dotnet-conventions/SKILL.md`.
4. `.opencode/skills/codebase-map/SKILL.md`.

Before changing code, inspect the nearest working feature. Preserve unrelated user changes. Never publish secrets or generated credentials. Never claim a build, test, migration, or external-provider flow works unless the corresponding verification actually ran; state environmental gaps explicitly.

Keep `.opencode` repository-specific and public-safe. Do not add instructions, paths, tools, or examples for code outside the allowed scope.
