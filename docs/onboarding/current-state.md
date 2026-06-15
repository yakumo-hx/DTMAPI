# DTMAPI Current State Handoff

Status date: 2026-06-15

This file is a short orientation entry for fresh Codex sessions. It is not a task ledger and does not replace `AGENTS.md`, update records, debug records, API reviews, or goal files.

## Start Here

Read these first, in this order:

1. `AGENTS.md`
2. `PROJECT.md`
3. `docs/onboarding/current-state.md`
4. `docs/api/public-api-matrix.md`
5. `docs/debug/INDEX.md`
6. The latest relevant `docs/updates/YYYY/...` record for the area being changed
7. The latest relevant `docs/reviews/...` record when doing review, root-cause, API, GameBridge, hook, input, save/load, UI, or lifecycle work

For hook or GameBridge changes, also read `docs/hook-map/README.md`, `docs/debug/regressions/smoke-matrix.md`, and the task-specific reverse/reference notes named by the review or goal.

## Active Branch Discipline

`Refactor` is the current first-level integration branch. Larger cleanup or refactor work should happen on a second-level `codex/...` branch and should not be merged back to `Refactor` without the user's explicit decision.

Keep each meaningful cleanup/refactor round in its own commit so the branch can be rolled back by round.

## Current Product Shape

DTMAPI is a Doloc Town modding API built as:

```text
BepInEx bootstrap -> DTMAPI Core -> DolocTown GameBridge -> stable public API -> DTMAPI mods
```

Only the bootstrap belongs in `BepInEx/plugins`. Ordinary DTMAPI mods belong in DTMAPI/local or Workshop mod folders and must be loaded through DTMAPI manifest/runtime paths.

## Current API Reality

The project has many smoke-verified paths, but most gameplay-facing GameBridge APIs are still Experimental. Do not promote an API to Stable from UI success, registry success, hook fire, or smoke helper success alone.

Important current boundaries:

- `ICameraZoomApi` 0.4.2 is failed/obsolete compatibility. Use `ICameraViewApi` for playable zoom work, and do not claim background/panorama/fog sync is solved from orthographic-size evidence.
- `IMotorVehicleApi` is Experimental research only after the active `SecondMotorMod` sample was archived on 2026-06-15. Archived SecondMotor smoke evidence is not current completion proof.
- Custom entity runtime creation remains blocked unless a family-specific native adapter is reviewed and verified.
- Manager/title UI is internal product UI. It does not promote diagnostics or GameBridge surfaces to public stable API.

## Archived Or Historical Areas

`testmods/SecondMotorMod` is archived under `archive/second-motor-20260615`. Active build, release, and smoke scripts must not reinstall or validate `DTMAPI_SecondMotor` as a current player-facing package.

Vehicle research may continue later, but it must start from a smaller native-owner slice and fresh manual QA. Do not restore old SecondMotor code or local packages as a release candidate.

## Evidence Space Policy

`docs/debug/evidence` is intentionally ignored by Git. It can become huge locally and should be treated as local artifact storage, not project source.

Default evidence collection should keep compact logs, result files, process/fatal-window checks, startup analysis, and explicit pointers to heavy runtime evidence. Full screenshot/runtime evidence directories should be copied only when the report or review truly needs the payload.

## Current Cleanup Priorities

Near-term cleanup should stay round-based:

1. Low-risk workspace cleanup: stale solution references, dead UI routes, evidence collection policy, and onboarding docs.
2. SecondMotor/MotorVehicle audit: distinguish archived sample code from any retained vehicle research API before deleting public or GameBridge surfaces.
3. Maintainability refactor: split runtime loader/service/diagnostics responsibilities, split GameBridge hook installers and smoke/status ownership, and reduce monolithic config UI behavior.
4. Semantic fixes: make content helper enabled-state boundaries explicit, correct or rename misleading input suppression, and warn clearly on duplicate `UniqueID` resolution.

## Validation Reminder

Build success is not enough for runtime/hook/API changes. Follow the evidence rules in `AGENTS.md`: startup log, HookProbe or feature log line, third-save evidence when relevant, clean exit, no fatal instance window, and updated debug/update records.
