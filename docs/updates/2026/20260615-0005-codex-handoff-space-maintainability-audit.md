# 20260615-0005 Codex Handoff / Project Space / Maintainability Audit

## Status

Recorded.

## Source Request

User asked to switch back to the main worktree branch and run parallel sub-agents to research:

- how difficult it is for a new Codex to take over the project;
- whether the project space is worth optimizing;
- a clean-context scan of DTMAPI core code readability, maintainability, redundancy, and likely garbage code.

## Changed Files

- `docs/reviews/code/2026/20260615-0001-codex-handoff-space-maintainability-audit.md`
- `docs/updates/2026/20260615-0005-codex-handoff-space-maintainability-audit.md`
- `docs/updates/INDEX.md`

## Implementation

- Confirmed the main worktree branch is `Refactor`.
- Spawned three read-only sub-agents for handoff difficulty, project-space optimization, and code maintainability scanning.
- Performed local read-only scans for branch/worktree state, source size, largest files, evidence footprint, stale solution references, dead UI routes, and active archived-feature hook residue.
- Recorded the combined audit under `docs/reviews/code/2026/`.

## Main Findings

- New Codex onboarding is possible but medium-high difficulty because current truth is distributed across required context, update/debug/API ledgers, and historical smoke records.
- The biggest project-space issue is local `docs/debug/evidence`, measured at roughly `126 GB`, with repeated large `GAME-SMOKE` payloads.
- `DTMAPI.sln` still references archived `testmods\SecondMotorMod`, while active build scripts no longer do.
- `ReflectedImGuiOverlay` appears to be uninstantiated legacy UI with old first-N truncation behavior.
- MotorVehicle/SecondMotor code remains active Experimental runtime hook surface after the sample mod was archived; this should be explicitly gated or labelled rather than silently treated as current player-facing functionality.
- High-priority semantics risks remain around content helper enabled-state boundaries and `IInputHelper.Suppress`.

## Validation

- No runtime build/test/game smoke was run because the task was read-only audit plus documentation recording.
- `git diff --check` should be run after this record is written.

## Rollback

Remove this update record, remove the linked code review record, and remove this row from `docs/updates/INDEX.md`.

## Follow-Up

- Create a small cleanup goal for the stale solution reference, legacy ImGui overlay, and current-state handoff doc.
- Create a separate evidence retention/artifact-root goal before moving or deleting any local raw evidence.
- Create implementation goals for content helper enabled-state separation and `IInputHelper.Suppress` semantics.

