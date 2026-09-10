# 20260628-0002 AutoFishing Diagnostic Log Throttle

## Status

verified / local DTMAPI runtime and upload package synced

## Source Request

User reviewed a longer local run with frequent Y-console usage and many AutoFishing catches. The run showed no fatal DTMAPI errors, but AutoFishing success diagnostics dominated `latest.log`: Ready charge speed, fast-animation, auto-cast, minigame, and restore evidence were repeated per frame or per catch. The user asked to optimize AutoFishing diagnostic log noise before long AFK sessions such as 1000-fish runs.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Summary

- Added a separate AutoFishing success-diagnostic throttle.
- Repeated success/experimental hook statuses and routine runtime logs now publish the first three samples, then interval samples every 30 seconds.
- `failed` hook statuses and `Fishing.Automation.AutoCastWatchdog=needs-review` remain immediate.
- Runtime reset clears the success-diagnostic throttle state after restore evidence is emitted.
- Unit coverage verifies repeated success diagnostics stop rewriting hook status after the first three samples, while status changes still publish immediately.

## Validation

- Passed: `git diff --check` (line-ending warnings only)
- Passed: `tools/scripts/build.ps1 -Configuration Release` (`NU1900` package-vulnerability index warnings only)
- Passed: `tools/scripts/test.ps1 -Configuration Release` (`DTMAPI.UnitTests: OK`, `NU1900` package-vulnerability index warnings only)
- Passed: local DTMAPI runtime install via `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -SkipOfficialLocalMods`
- Passed: local DTMAPI status check; required install files present.
- Passed: local official upload package sync via `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -OutputRoot C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS -SkipBuild -RuntimeOnly`
- Passed: source Release output, local game install, and local upload package hashes match for the five DTMAPI runtime DLLs and `assets/branding/dtmapi-icon.png`.
- No long-run game soak was run in this change. Long-run crash issue `ISSUE-010` remains open/evidence-improved.

## Evidence Notes

The latest user-observed run had no managed DTMAPI fatal errors, but logged thousands of successful AutoFishing diagnostic lines during a short fishing session. The fix reduces those success lines without changing native AutoFishing behavior or public API contracts.

## Rollback

Revert the success-diagnostic throttle helper and replace affected `PublishFishingAutomationHookStatus` / `PublishFishingAutomationLog` calls with direct `runtime.SetHookStatus` / `runtime.RuntimeMonitor.Log` calls.

## Follow-Up

- Run a fifth-save AutoFishing smoke or a longer local soak when runtime time is available.
- If another player crash package appears, classify it with Unity crash evidence and lifecycle counters before treating log volume as a crash cause.
