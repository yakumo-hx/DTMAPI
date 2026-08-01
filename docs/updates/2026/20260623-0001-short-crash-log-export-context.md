# 20260623-0001 Short Crash Log Export Context

## Summary

Analyzed two newly archived player log packages from old distributed `0.5.2-alpha` builds and improved DTMAPI's evidence chain for short-session Unity native crashes. This pass defers unsafe title/Y-console EventSystem fallback while Unity Input System is not initialized, adds runtime/mod failure context to report export, writes bounded Y-console give-item breadcrumbs, and extends offline log collection with support context and Temp Unity crash roots.

This does not claim the short-session native crash is fixed. The supplied logs predate the current lifecycle cleanup and this evidence pass, and no game smoke or upload package sync was run.

## Source Request

User request: implement the short-time crash and log export enhancement plan, fix current issues shown by the newly archived logs, and run source-level validation plus parallel safety review only.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `tools/scripts/collect-logs.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/debug/issues/README.md`
- `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Behavior

- Title settings and Y-console EventSystem fallback now probe Unity Input System readiness before choosing `InputSystemUIInputModule`. The title UI no longer tries to create a fallback EventSystem during Canvas initialization; it waits until the title homepage is visible. If the module is unavailable or the Input System is not ready, DTMAPI falls back to `StandaloneInputModule` when present or records `UI.TitleSettingsEventSystem=pending` / `UI.DebugConsoleEventSystem=pending` and retries later instead of permanently quarantining a startup race. DTMAPI-owned fallback EventSystems are scoped to the owning UI, track both their GameObject and EventSystem component owner, and are destroyed when the title entry/debug console is inactive or a separate native EventSystem appears.
- Report export can include `DTMAPI-runtime-context.txt` with runtime version, loaded/failed mod rows, mod failure lines, and `OnApplicationQuit` observation.
- Offline `collect-logs.ps1` writes `support-context.txt` with DTMAPI/latest-log tails, BepInEx tail, recent load/patch/Harmony/API failure lines, startup/quit markers, and Steam-tail evidence when available.
- Unity crash collection remains budgeted and now explicitly covers both `%TEMP%\RedSawGames\DolocTown\Crashes` and `%TEMP%\RedSawGames\Doloc Town\Crashes` in in-game report export and offline collection. Same-name crash directories from different roots get short hashed path segments; the newest crash directory from each Temp root and the newest crash facts are retained before dump-priority fill; file-level reparse points are skipped; and an accepted latest `crash.dmp` expands the budget so adjacent small text evidence is still copied.
- Y debug console give-item actions publish a bounded `DebugConsole.LastGive` status row and `debug-console-last-give.txt` support breadcrumb, preserving the last left/right give request and result without adding per-frame logs. Both in-game and offline report exports include this breadcrumb when present.
- Third-party Harmony/load failures such as `com.user.dolocnoweeds` are treated as mod failure context in exported reports, not as DTMAPI install failure. Report context now scans both the head and tail of DTMAPI/BepInEx logs so early startup failures are not lost in long logs. Loaded mods that log `Error` through their DTMAPI monitor are surfaced as `MOD-RUNTIME-DIAGNOSTIC`; failed code-mod loads clean owner-bound DTMAPI event/API registrations, config pages, and custom entity registrations where possible and report remaining partial-side-effect risk only for code-load failures instead of pretending Harmony/native side effects can be undone safely.
- EquipmentSlots hook-instance rendering now avoids destructive cleanup for first-frame inactive official hosts; stale global clones/binders are still cleared by lifecycle paths from the earlier 20260620 fixes.

## Validation

- `tools/scripts/test.ps1 -Configuration Release` passed 2026-06-23 with only restricted-network NU1900 package vulnerability index warnings.
- `git diff --check` passed with line-ending warnings only.
- Windows PowerShell 5.1 parser check for `tools/scripts/collect-logs.ps1` passed.
- Temporary fake crash-tree collector run passed with an intentionally incomplete fake `DTMAPI_GAME_DIR`; the script continued through the support path and produced `Unity-Crashes/summary.txt`, distinct hashed entries for same-leaf crash directories under both Temp spellings, `MISSING-CRASH-DUMP-README.txt` for a no-dump crash directory, and `support-context.txt` entries for `DebugConsole.LastGive` plus `com.user.dolocnoweeds`.
- Unit coverage verifies partial `Entry` failures clean owner-bound event/API/config/custom-entity state through the real mod load path, loaded mod monitor errors become `MOD-RUNTIME-DIAGNOSTIC`, title and debug-console EventSystem selection does not pick `InputSystemUIInputModule` before readiness, GameBridge `EnvironmentReset` no longer indirectly clears active EquipmentSlots UI, oversized Y-console give breadcrumbs are skipped with an explicit diagnostic note, and newest no-dump crash directories survive older dump-heavy directory budgets. EventSystem owned fallback lifecycle still needs real title/Y-console game-click smoke before this issue can advance beyond `evidence-improved`.
- Parallel static safety review was rerun after the P1/P2 fixes; no final game-runtime claim is made without title/Y-console/crash smoke evidence.

No game smoke was run by design. No local game runtime, official local upload package, or Workshop upload folder was modified.

## Evidence Links

- Debug issue: `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`
- Archived player logs: `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs`
- Short-crash analysis: `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs/ANALYSIS-20260623-short-crash.md`
- Smoke matrix row: `SHORT-RUN-NATIVE-CRASH-EVIDENCE-20260623`
- Related long-run issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Rollback Notes

If title-page DTMAPI entry stops receiving clicks in game smoke, first inspect `UI.TitleSettingsEventSystem` status. The intended rollback is to narrow the readiness probe or prefer the native existing EventSystem, not to restore repeated startup exceptions from forced `InputSystemUIInputModule` construction.

If report export becomes too large or too slow, reduce crash report budgets or Steam/log-tail counts while keeping `Unity-Crashes/summary.txt` and missing-dump instructions.

## Follow-Up

Run title homepage smoke, Y-console give/search smoke, and a fresh crash/no-crash package from the fixed build before uploading. ISSUE-011 stays `open/evidence-improved` until a fixed build's logs show whether the short-session native crash class remains.
