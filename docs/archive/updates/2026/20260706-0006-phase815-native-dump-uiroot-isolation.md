# 20260706-0006 - Phase 8.15 Native Dump Triage And First UiRuntime Root Isolation

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / shifted-window / live-dump-captured / issue-010-open

## Source Request

External review requested Phase 8.15:

- analyze the 8.14 DbgHelp full live dump before running more service isolation;
- if the dump cannot be decoded, run one smoke-only `UiRuntime` root isolation sample;
- disable only DTMAPI-owned UI/runtime helper roots;
- keep no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, full known-failing feature profile, and AutoFishing disabled;
- do not run content/native-heavy isolation or broad service-disable in this phase.

## Changed Files

- `tools/scripts/analyze-process-dump.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/reviews/code/2026/20260706-0005-phase815-native-dump-uiroot-isolation.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

Added dump analysis helper:

- `tools/scripts/analyze-process-dump.ps1` records dump metadata and attempts debugger discovery.
- If no debugger is found, it writes `MISSING-WINDBG-README.txt` rather than failing the phase.

Added smoke-only root isolation:

- `-SmokeRootIsolationProfile None|UiRuntime`, default `None`.
- `UiRuntime` skips DTMAPI-owned `NativeUiLayoutDiagnostics`, `SaveSlots`, `EquipmentSlots`, and `Camera` service/root registration or update paths where safe.
- `DebugConsoleHost` is reported as unsupported, not silently disabled.
- Runtime logs publish `SmokeRootIsolationProfile`, `Disabled`, `Unsupported`, and `Active`.

Fixed a smoke-only dump packaging bug discovered by the 8.15 run:

- `Write-TooLargeDumpReadme` now falls back to .NET SHA256 hashing when `Get-FileHash` is unavailable.
- This prevents future large-dump README generation from aborting before `result.json` and profile restore.

## Evidence

Dump analysis:

- `docs/debug/evidence/GAME-SMOKE/20260706-113849/Dump-Analysis/dump-analysis-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-113849/Dump-Analysis/MISSING-WINDBG-README.txt`

Runtime evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-131450`

Key runtime result:

- no HookProbe;
- no PreLoad GC;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- profile valid and AutoFishing disabled;
- fatal occurred after `SaveLoaded.Step=Hook.Exit`;
- `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate requests `0`;
- DbgHelp full dump captured, size `4,972,791,628`, SHA256 `9E9780BF1258C670E307BFA6A36BBD59E72752CF554090ECFAD54CAA3CBF0ADE`.

Because the pre-patch smoke harness exited while writing `TOO-LARGE-DUMP-README.txt`, `result.json` was not generated for this run. Evidence includes:

- `RESULT-MISSING-README.txt`
- `manual-result-reconstruction.json`
- `manual-fatal-cleanup-note.txt`
- `manual-official-mod-profile-restore.txt`
- reconstructed `Process-Dumps/process-dump-summary.txt`
- reconstructed `Process-Dumps/TOO-LARGE-DUMP-README.txt`

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed before and after the hash fallback patch.
- `tools/scripts/test.ps1 -Configuration Release`: passed before and after the patch with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired for the smoke and manual cleanup/restore operations.
- No leftover `DolocTown.exe` after manual cleanup.
- Runtime lock free after cleanup.
- Profile restored manually from the smoke backup after the harness abort.

## Rollback

To roll back this phase:

- remove `tools/scripts/analyze-process-dump.ps1`;
- revert the smoke-only `SmokeRootIsolationProfile` branches in GameBridge/Hooks/SmokeHarness/run-game-smoke;
- revert the SHA256 fallback in `run-game-smoke.ps1`.

No player runtime configuration or public API migration is needed because this phase added smoke-only controls and diagnostics.

## Follow-Up

Next phase should analyze the shifted post-SaveLoaded, pre-native-return LoadGame continuation before broad service-disable:

- decode the 8.15 DbgHelp dump if WinDbg/cdb becomes available;
- compare 8.11 / 8.13 / 8.15 post-SaveLoaded windows;
- inspect the fresh Unity crash `Crash_2026-07-06_061918817`;
- choose one narrow content/native-heavy isolation axis only after the shifted window is understood.

## Non-Changes

This phase did not:

- change public API;
- do a GameBridge large refactor;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- resume UI owner/pair split;
- run a broad service-disable matrix;
- destroy unknown native Unity objects;
- change ordinary player runtime behavior.
