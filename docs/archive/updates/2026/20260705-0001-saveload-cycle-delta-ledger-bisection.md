# 20260705-0001 SaveLoad Cycle Delta Ledger And Feature Bisection

Date: 2026-07-05 +08:00

Status: source verified / short and feature bisection smokes run / ISSUE-010 open

## Source Request

Phase 8.6: determine whether ISSUE-010 is driven by one-hour title idle or repeated SaveLoad cycles, add per-cycle object deltas, run feature bisection, fix only definite DTMAPI-owned residues, and update durable evidence.

## Changed Files

- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/code/2026/20260705-0001-phase86-saveload-cycle-accumulation-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0001-saveload-cycle-delta-ledger-bisection.md`
- `docs/updates/INDEX.md`

## Implementation

- Added bounded `TitleReturnObjectGraphDeltaEntry` records and metric extraction for Bootstrap, GameBridge, ModOwner, ResourceLifecycle, SaveLoad, and runtime snapshot sections.
- Published `Refactor.SaveLoadCycleObjectDeltaLedger` and added smoke result fields for object-delta evidence.
- Added Bootstrap and GameBridge lifecycle summaries to object graph snapshots.
- Added official mod profile support to the smoke script for `CoreOnly`, `CoreUi`, `CoreCustomAnimals`, `CoreAutoFishing`, and extra enabled IDs.
- Pruned stale DTMAPI-owned `SaveLifetime` diagnostic records from older save generations.

## Validation

- `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1` passed.
- `git diff --check` passed after source changes; final diff check rerun is recorded in the turn summary.
- Runtime lock was used for game smoke operations and released after each run.

## Evidence

- No-idle current/default 20-cycle pass: `docs/debug/evidence/GAME-SMOKE/20260704-184517`.
- One-hour-idle current/default fail before resource prune: `docs/debug/evidence/GAME-SMOKE/20260704-185211`.
- `CoreOnly` one-hour-idle pass: `docs/debug/evidence/GAME-SMOKE/20260704-201341`.
- `CoreCustomAnimals` one-hour-idle pass: `docs/debug/evidence/GAME-SMOKE/20260704-214043`.
- `CoreAutoFishing` one-hour-idle pass: `docs/debug/evidence/GAME-SMOKE/20260704-225729`.
- `CoreUi` one-hour-idle pass: `docs/debug/evidence/GAME-SMOKE/20260705-001613`.
- ResourceLifecycle prune quick pass: `docs/debug/evidence/GAME-SMOKE/20260705-013446`.
- Current/default one-hour-idle fail after resource prune: `docs/debug/evidence/GAME-SMOKE/20260705-013648`.
- Action/utility one-hour-idle pass: `docs/debug/evidence/GAME-SMOKE/20260705-023843`.
- CustomAnimals plus action/utility pass: `docs/debug/evidence/GAME-SMOKE/20260705-034457`.
- CustomAnimals plus action/utility plus Manbo pass: `docs/debug/evidence/GAME-SMOKE/20260705-045059`.
- CustomAnimals plus action/utility plus Manbo plus UI fail: `docs/debug/evidence/GAME-SMOKE/20260705-055655`.

## Conclusions

- SaveLoad cycle count alone is not enough: 20 no-idle cycles passed.
- Core runtime plus one-hour title idle is not enough: `CoreOnly` passed repeated loads.
- The latest minimal reproduced feature profile is `CustomAnimals/AnimalVoice + action/utility + Manbo audio + UI`; AutoFishing was disabled in that failing run.
- DTMAPI-owned title/save transients are bounded in the latest ledgers. The remaining suspect is stable process/title graph pressure, especially UI owners/event/input roots combined with custom animals/audio/action utilities after title idle.

## Rollback

Revert the changed source/script files above. The runtime behavior changes are diagnostic-only except for pruning stale DTMAPI-owned resource-ledger records. No native Unity object, content pack asset, `AudioClip`, `AssetBundle`, or controller destruction was added.

## Follow-Up

- Split the UI group in the failing profile: Zoom, MoreSaves, MoreEquipmentSlots, and YConsole separately.
- Add per-owner object-delta summaries for event/input/config page roots.
- If UI split does not isolate a DTMAPI owner, escalate to native crash dump or external Unity object graph/root-set analysis.
