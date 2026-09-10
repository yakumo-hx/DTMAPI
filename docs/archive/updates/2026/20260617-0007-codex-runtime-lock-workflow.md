# 20260617-0007 Codex Runtime Lock Workflow

## Status

verified-static

## Source Request

User asked whether multiple Codex sessions can queue for the single local Doloc Town runtime environment, then requested the project to complete that function before creating new feature branches.

## Summary

Added a shared runtime lock workflow for parallel Codex worktrees. Code edits can remain parallel, while shared game-runtime operations such as installing DTMAPI, launching Doloc Town, running smoke, and writing live local `MODS`/Workshop upload folders must acquire one lock stored in the Git common directory.

## Changed Files

- `AGENTS.md`
- `docs/workflows/codex-runtime-lock.md`
- `docs/updates/INDEX.md`
- `tools/scripts/README.md`
- `tools/scripts/common.ps1`
- `tools/scripts/acquire-runtime-lock.ps1`
- `tools/scripts/wait-runtime-lock.ps1`
- `tools/scripts/release-runtime-lock.ps1`
- `tools/scripts/runtime-lock-status.ps1`
- `tools/scripts/invoke-runtime-locked.ps1`
- `tools/scripts/status.ps1`

## Validation

- PowerShell parser checks passed for `common.ps1`, the new runtime-lock scripts, and `status.ps1`.
- `runtime-lock-status.ps1` reported a free lock.
- `acquire-runtime-lock.ps1` created the shared lock at `.git/dtmapi-runtime.lock.json`.
- Status output reported the correct owner/worktree/branch/reason and an accurate lock age.
- A second acquire from the same worktree with `-AllowCurrentWorktreeReuse` reused the lock.
- `wait-runtime-lock.ps1 -AllowCurrentWorktreeReuse` reused the lock.
- `release-runtime-lock.ps1` removed the lock.
- `tools/scripts/status.ps1` prints the shared runtime lock state.
- Release `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`.

No game smoke is required for this change because it does not alter runtime code, BepInEx files, Harmony hooks, mod loading behavior, or installed game files.

## Evidence

- `tools/scripts/runtime-lock-status.ps1`: free/locked/free validation.
- `tools/scripts/status.ps1`: prints `[FREE] Runtime lock is free` with the Git-common lock path.

## Related Records

- `docs/workflows/codex-runtime-lock.md`

## Rollback

Remove the runtime lock scripts, remove the lock functions from `tools/scripts/common.ps1`, remove the lock line from `tools/scripts/status.ps1`, and remove the AGENTS workflow requirement.

## Follow-up

If Codex sessions still forget to lock before game validation, add automatic lock integration to the highest-risk entry scripts with careful `try/finally` coverage.
