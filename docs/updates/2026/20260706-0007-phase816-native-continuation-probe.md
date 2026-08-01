# 20260706-0007 - Phase 8.16 Native Continuation Probe

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / pre-SaveLoaded terrain-dungeon fatal / live-dump-captured / issue-010-open

## Source Request

External review requested Phase 8.16:

- do not continue broad service/root isolation yet;
- add a smoke-only `-SmokeNativeLoadContinuationProbe None|VersionPatcher` switch;
- log lightweight native continuation breadcrumbs after `LoadGame` enters and around `AfterLoadArchiveData`, `VersionPatcher`, and safe `MapManager.Init` targets;
- rerun one no-HookProbe, no-PreLoad, `Lite`, `UiRuntime` full-profile sample;
- preserve evidence and decide whether the shifted 8.15 window reaches `VersionPatcher` again.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260706-0006-phase816-post-saveload-native-continuation-probe.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Implementation

Added smoke-only native continuation probe support:

- `-SmokeNativeLoadContinuationProbe None|VersionPatcher`, default `None`.
- Runtime probe hook status and last breadcrumb evidence in `summary.txt`, `result.json`, and `native-continuation-probe-summary.txt`.
- Lightweight `NativeContinuation.Step` log lines containing method, phase, elapsed time, GC counts, total memory, request id, boundary id, thread id/name, and save-load counters.

The probe installed hooks for:

- `DolocAPI.AfterLoadArchiveData(bool)`
- `DolocTown.VersionPatcher.LoadAllVersionPatches()`
- `DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond(string)`
- `DolocTown.MapManager.Init(bool)`

`TextureUtils.DrawArea` is explicitly reported as skipped because it is high-frequency and unsafe for this probe's low-pressure logging goal.

## Evidence

Runtime evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-150952`

Key result:

- no HookProbe;
- no PreLoad GC;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- `SmokeNativeLoadContinuationProbe=VersionPatcher`;
- profile valid and AutoFishing disabled;
- fatal occurred before SaveLoaded and before any continuation target;
- only `NativeContinuation.Step=DolocAPI.LoadGame.Enter` was logged;
- `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate requests `0`;
- fresh Unity crash `Crash_2026-07-06_081133308` points through `TerrainLayer`, `Terrain.Create`, `Room.ResetTerrain`, `Dungeon.AfterLoadData`, `DataPersistenceManager.LoadGame`, and dynamic `DolocAPI::LoadGame`;
- DbgHelp full dump captured, size `4,891,602,600`, SHA256 `B99DAC880F82234DEB7B43B4E095CA3EDD9D6560A370C7DCD1AA022BEC3EEC78`.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only before runtime.
- Runtime lock was acquired and released.
- No leftover `DolocTown.exe`.
- Profile restored automatically after the run.

## Rollback

To roll back this phase, remove the smoke-only native continuation probe branches in GameBridge, HookCallbacks, SmokeHarness, and `run-game-smoke.ps1`.

No player configuration migration or public API rollback is needed because the new switch defaults to `None` and is smoke-only.

## Follow-Up

The next phase should use the Phase 8.16 stack and live dump for native LoadGame terrain/dungeon root-set analysis:

- decode the DbgHelp dump if a debugger becomes available;
- compare Phase 8.10, 8.14, and 8.16 pre-SaveLoaded terrain/dungeon stacks;
- do not continue UI owner/pair splits;
- do not treat `VersionPatcher` as sufficient for this recurrence because it was not reached.

## Non-Changes

This phase did not:

- change public API;
- do a GameBridge large refactor;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run a new broad service-disable matrix;
- destroy unknown native Unity objects;
- change ordinary player runtime behavior.
