# 20260620-0001 Crash Log Export And GC Audit

Date: 2026-06-20

Area: diagnostics export, offline log collection, long-run Unity/Mono GC crash review

## Source Request

Player feedback and logs showed long-play crashes, including Unity fatal windows and `Fatal error in GC / Unexpected mark stack overflow`. The request was to run parallel code-level research, assume DTMAPI is likely involved until proven otherwise, and optimize log export so future player reports include enough evidence.

## Changed Files

- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/README.md`
- `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/issues/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Summary

- Added a code-level audit for the long-run Unity/Mono GC crash class.
- Improved offline `collect-logs.ps1`:
  - collects `Player-prev.log` when present;
  - checks both `RedSawGames\DolocTown` and `RedSawGames\Doloc Town` Player log paths;
  - copies recent Unity native crash report directories from `%TEMP%\RedSawGames\DolocTown\Crashes`;
  - writes `Unity-Crashes/summary.txt`;
  - bounds copied crash directories, file count, and large-file inclusion.
- Improved game-internal `IDiagnosticsHelper.ExportLogs()` report zip:
  - includes recent Unity crash reports;
  - includes `Unity-Player-prev.log` when present;
  - includes recent `install-state.failed-*.json` and `uninstall-state-*.json` files.
- Recorded that this is an evidence/export improvement, not a claim that the GC crash is fixed.

## Code-Level Audit Result

Confirmed DTMAPI-owned risks:

- SaveSlots can retain native save-panel/pager state and UnityEvent callbacks because `ReturnedToTitle` and `EnvironmentReset` do not clear `officialSaveUiStates`.
- EquipmentSlots can retain cloned slot UI objects and event binders because data-session reset does not unconditionally clear UI roots.
- `SetEnvCamera -> EnvironmentReset` is a high-frequency fanout path that can amplify allocation/scanning pressure.
- AutoFishing can log `auto-cast invoked` without proving native fishing-state progression.

Not proven:

- No reviewer found a direct infinite managed recursion path.
- Current logs do not prove which retained object graph caused the Mono GC mark stack overflow.

## Validation

- Windows PowerShell 5.1 parser check passed for `tools/scripts/collect-logs.ps1`.
- Fake Unity crash report collection test:
  - created `%TEMP%\RedSawGames\DolocTown\Crashes\DTMAPI-collector-test-*`;
  - `collect-logs.ps1 -OutputDirectory <temp>` copied `error.log` and `crash.dmp`;
  - `Unity-Crashes/summary.txt` was written.
- `git diff --check` passed with line-ending warnings only.
- `src/DTMAPI.Core/DTMAPI.Core.csproj` Release build passed with 0 warnings/errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`.

No game smoke was run because this change does not install/runtime-test DTMAPI or claim to fix gameplay behavior.

## Evidence Links

- Review: `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`
- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Smoke/regression row: `RUNTIME-CRASH-EXPORT-GC-AUDIT-20260620`

## Rollback

Remove Unity crash report collection from `DiagnosticsService.ExportLogs()` and `collect-logs.ps1`, and restore the prior `collect-logs.ps1` Player-log copy list. Rollback is not recommended because the change is bounded and only improves support evidence collection.

## Follow-Up

- Implement EquipmentSlots cloned UI/event binder lifecycle cleanup.
- Implement SaveSlots official panel state cleanup.
- Add low-frequency long-run lifecycle counters.
- Add AutoFishing pending-cast stall watchdog.
- Rebuild/sync the DTMAPI Runtime upload package before telling players to collect logs with this improved collector.
