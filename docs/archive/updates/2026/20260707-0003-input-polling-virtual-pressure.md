# 20260707-0003 - Input Polling Diagnostics And Virtual Pressure

Status: source-and-runtime-evidence-captured / virtual pressure reproduced 20m fatal / issue-010-open

## Source Request

The user asked to add input polling counters, run a 5-minute short test to confirm the counters, add 50 virtual keys for a 2-minute pressure confirmation, then run a 20-minute save-entry test and only continue to a long run if no GC fatal occurred.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

- Added smoke-only reflected input diagnostics, gated by `SmokeInputPollingDiagnostics`, that count `GetKeyDown` and `GetKey` calls plus legacy/InputSystem/Win32 attempts, misses, unavailability, and exceptions.
- Added smoke-only input polling summaries from `PollRegisteredInputButtons`, including frame count, registered button union, context buckets, pressed/released events, and total polled buttons.
- Added smoke-only `SmokeVirtualInputKeyCount` support that registers a synthetic owner `DTMAPI.Smoke.VirtualInput` with up to 50 non-special Unity keys through the normal owner-bound input API.
- Extended `run-game-smoke.ps1` to write the two smoke settings and gate on `Smoke.InputPollingDiagnostics` and `Smoke.VirtualInputPressure` status lines.

These changes are diagnostics/stress-harness only. They do not change player hotkey registration, gameplay behavior, public APIs, content registry behavior, or native load flow when the smoke flags are disabled.

## Validation

- PowerShell parser check passed for `tools/scripts/run-game-smoke.ps1`.
- `dotnet build DTMAPI.sln -c Release` passed with 0 warnings and 0 errors.
- `dotnet test tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj -c Release --no-build` exited successfully.
- `git diff --check` passed with line-ending normalization warnings only.
- Runtime lock was acquired before local game operations and released after the smoke sequence.
- Final process checks found no leftover `DolocTown.exe`.

## Runtime Evidence

### 5-minute count-only control

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-152226`.

The result file reported `RunStatus=Failed` because this `SaveSlot=0` title-only route did not publish the always-requested `SaveLoadRequestCoordinator` line. The input counter itself passed.

Final input summary:

```text
elapsedSeconds=324.9; frames=1169; registeredButtonsLast=9; registeredButtonsMax=9; registeredButtonsAverage=9.0; totalButtonsPolled=10521; getKeyDownCalls=10521; getKeyCalls=10521; pressedEvents=0; releasedEvents=0; titleFrames=1152; titleButtonsPolled=10368; gameplayFrames=17; gameplayButtonsPolled=153; reflected={getKeyDownCalls=10521; getKeyCalls=10521; legacyAttempts=21042; legacyExceptions=21042; inputSystemAttempts=21042; inputSystemUnavailable=18; win32Attempts=21042; win32ForegroundMisses=21042}
```

Interpretation: the idle title loop calls both `GetKeyDown` and `GetKey` once for each registered input button each frame. The baseline registered union was 9.

### 2-minute virtual pressure confirmation

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-153023`.

The script result failed on existing save-load gate mismatches, but `SmokeVirtualInputPressure=Passed`, the game loaded, no fatal window appeared, and the process exited.

Virtual pressure summary:

```text
owner=DTMAPI.Smoke.VirtualInput; requestedKeys=50; registeredKeys=50; ownerBoundEnabled=True; ownerButtonCount=50; registeredButtonUnion=57; pressureSucceeded=True
```

Final input summary:

```text
registeredButtonsLast=57; registeredButtonsMax=57; totalButtonsPolled=18924; getKeyDownCalls=18924; getKeyCalls=18924; titleButtonsPolled=18012; reflected={legacyAttempts=37848; inputSystemAttempts=37848; win32Attempts=37848}
```

Interpretation: the 50 virtual keys used the same registered input button polling path as YConsole and Zoom, and raised the active button union from 9 to 57.

### 20-minute virtual pressure SaveLoad reproducer

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-153322`.

The 20-minute route used the same light diagnostic profile plus `SmokeInputPollingDiagnostics`, `SmokeVirtualInputKeyCount=50`, and a single post-idle save entry. It reproduced `Fatal error in GC / Unexpected mark stack overflow` at the first post-idle native `LoadGame`.

Key result fields:

```text
RunStatus=Aborted
NoFatalInstanceWindow=Failed
SaveLoadCycle=Failed
SaveLoaded=Failed
SaveLoadBoundary=Failed
FatalWindowProcessDump=Captured:DbgHelpFull
UnityCrashFreshness=fresh
ProcessExited=Passed
```

Fatal window:

```text
FatalWindowDetectedAt=2026-07-07T15:53:31.1011441+08:00
Title=Fatal error in GC
Text=Fatal error in GC / Unexpected mark stack overflow
```

Last DTMAPI native breadcrumb:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Enter method=DolocAPI.LoadGame phase=Enter elapsedMs=6 gc0=256 gc1=256 gc2=256 totalMemory=351395840 requestId=SL-0001 boundaryId=TR-0001 slot=2 runtimePhase=Update threadId=1 nativeEnter=1 nativeReturn=0 saveLoaded=0
```

Final input summary before the native load:

```text
elapsedSeconds=1194.2; frames=4657; registeredButtonsLast=57; registeredButtonsMax=57; totalButtonsPolled=265449; getKeyDownCalls=265449; getKeyCalls=265449; titleFrames=4640; titleButtonsPolled=264480; gameplayFrames=17; gameplayButtonsPolled=969; reflected={getKeyDownCalls=265449; getKeyCalls=265449; legacyAttempts=530898; legacyExceptions=530898; inputSystemAttempts=530898; inputSystemUnavailable=28050; win32Attempts=530898; win32ForegroundMisses=530898}
```

DbgHelp dump metadata:

```text
DumpPath=E:\Python_project\DTMAPI\docs\debug\evidence\GAME-SMOKE\20260707-153322\Process-Dumps\DolocTown-17360-fatal-live-dbghelp.dmp
DumpSize=4946066230
DumpSha256=B9D5BEF2648843E0E5AFC9945AEAFA8CEEF0219B29379795435CE931D81F28F0
```

The long run with 50 virtual keys was intentionally not run because the 20-minute gate already reproduced the fatal.

## Classification

The input button polling/root path is now a verified pressure amplifier. A synthetic 50-key owner registered through the normal owner-bound input API follows the same polling path as YConsole/Zoom-style hotkeys and can pull the known native `LoadGame` GC fatal forward to a 20-minute repro.

This does not prove that YConsole/Zoom input roots are the only root cause. It proves that the registered input root count and per-frame polling pressure can move the process over the Mono GC mark-stack threshold earlier. The native crash still occurs inside the save-entry `DolocAPI.LoadGame` transition before `SaveLoaded`.

## Rollback Notes

- Disable the new smoke flags by omitting `-SmokeInputPollingDiagnostics` and leaving `-SmokeVirtualInputKeyCount 0`; normal runtime behavior returns to the previous path.
- If needed, revert only the smoke-only diagnostics and stress harness files listed above. No public API or player-facing config migration is involved.

## Follow-Up

- Implement a separate runtime mitigation goal for input polling:
  - avoid title polling for hotkeys that do not need title input, including YConsole and Zoom;
  - consider owner/context gating so disabled or non-current-context hotkeys do not stay in the per-frame union;
  - reorder release checks so idle keys do not call `GetKey` unless the runtime currently believes the button is down;
  - retain diagnostics long enough to prove the button union and backend call counts drop.
- After the mitigation, rerun the original unsuppressed YConsole+Zoom route and at least one no-virtual long route before claiming ISSUE-010 solved.
