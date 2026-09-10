# 20260707-0005 - Hotkey Edge Sampling

Status: source-and-runtime-smoke-verified / 50-key pressure passed 20m / autofishing-manual-regression-superseded-by-20260707-0006 / issue-010-open

## Source Request

After the first hotkey rebuild, user manual testing passed the visible features but found that short/soft taps and rapid repeated taps felt less reliable than native input. The user asked to keep the scoped active-button design, upgrade the input frame from `button -> isDown` to `IsDownNow` / `PressedEdge` / `ReleasedEdge`, mark the prior manual test half-pass, and rerun the short smokes plus the 50-key pressure route.

## Changed Files

- `src/DTMAPI.Abstractions/Input.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/2026/20260707-0004-hotkey-rebuild.md`
- `docs/updates/2026/20260707-0005-hotkey-edge-sampling.md`
- `docs/updates/INDEX.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`

## Implementation

- Added internal `InputButtonSample` frames carrying canonical button id, current down-state, pressed edge, and released edge.
- Bootstrap now reuses a per-frame sample list and passes samples directly to Core instead of allocating a per-frame dictionary for ordinary hotkeys.
- `ReflectedUnityInput.SampleButtonCached` now samples cached edge state: Input System `wasPressedThisFrame` / `isPressed` / `wasReleasedThisFrame`, Win32 transition/current state with local release repair, and legacy `GetKeyDown` / `GetKey` / `GetKeyUp` fallback.
- Core dispatch now prefers sampled edges: button press is `PressedEdge || IsDownNow && !wasDown`; release is `ReleasedEdge || !IsDownNow && wasDown`.
- Keybind press now follows native-like rules: all buttons down plus any member pressed this frame; a single-key bind can dispatch from `PressedEdge` even if the key was already released by the time Core receives the frame.
- Registration update no longer clears held keybind state for unchanged keybinds, and changed registrations preserve physical button state when the button is still owned elsewhere.
- Generic `Control`, `Shift`, and `Alt` are logical canonical modifiers that match either physical side; side-specific `LeftControl` / `RightControl` bindings remain distinct. Conflict detection expands generic modifiers to physical chords, so generic conflicts with either side while left/right specific chords do not conflict with each other.

## Validation

- PowerShell parser check passed for `tools/scripts/run-game-smoke.ps1`.
- `dotnet build DTMAPI.sln -c Release` passed with 0 warnings and 0 errors.
- `dotnet test tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj -c Release --no-build` exited successfully.
- The console unit runner passed with `DOTNET_ROLL_FORWARD=Major` because the local machine has .NET 6 and .NET 9 runtimes but not .NET 8.
- `git diff --check` exited successfully with line-ending normalization warnings only.
- Runtime lock was acquired before local game operations and released after the smoke sequence.
- Final process checks found no leftover `DolocTown.exe`.

## Runtime Evidence

### 5-minute input counter smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-185110`.

The first attempt `20260707-184940` is retained as invalid because an early `AutoExitAfterSeconds=20` setting preempted the intended 5-minute title idle. The valid diagnostic run still hit the existing long-title/save-load result-gate mismatch, but the input diagnostic and process/fatal checks passed:

```text
SmokeInputPollingDiagnostics=Passed
legacyRegisteredStringPath=false
elapsedSeconds=288.7
frames=1054
registeredButtonsMax=8
totalButtonsPolled=144
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=144
cachedDownSamples=0
cachedPressedEdges=0
cachedReleasedEdges=0
titleFrames=1036
titleButtonsPolled=0
gameplayButtonsPolled=144
NoFatalInstanceWindow=Passed
ProcessExited=Passed
```

### 2-minute 50-key confirmation

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-185717`.

The existing save-load result gates mismatched, but virtual registration, edge-frame sampling, and process/fatal checks passed:

```text
SmokeVirtualInputPressure=Passed
requestedKeys=50
registeredKeys=50
ownerButtonCount=50
registeredButtonUnion=56
pressureSucceeded=True
SmokeInputPollingDiagnostics=Passed
legacyRegisteredStringPath=false
elapsedSeconds=110.7
frames=334
registeredButtonsMax=56
totalButtonsPolled=952
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=952
titleButtonsPolled=0
NoFatalInstanceWindow=Passed
ProcessExited=Passed
```

### Third-save functional smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-190231`.

The functional paths passed, while the script-level `RunStatus` failed only because `DiagnosticsReportExport=Failed`. Process and fatal checks were clean:

```text
SaveLoaded=Passed
DebugConsoleOpenY1=Passed
DebugConsoleCloseEscape=Passed
DebugConsoleOpenY2=Passed
DebugConsoleCloseY=Passed
DebugConsoleTenYShortTaps=Passed
DebugConsoleHoldYNoFlicker=Passed
Zoom=Passed
AutoFishingInputLog=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
DiagnosticsReportExport=Failed
```

Logs confirm the rebuilt keybind path dispatched F6 and enabled AutoFishing:

```text
Input F6 pressed dispatched to DTMAPI mods. context=Gameplay menuOpen=False.
Keybind Yuuka.DTMAPI.AutoFishing/auto-fishing.toggle pressed trigger=F6 context=Gameplay.
AutoFishing automation enabled reason=hotkey F6
```

Correction after user manual testing: these repeated F6 keybind log lines were not just harmless harness noise. The user later reproduced AutoFishing repeatedly toggling after F6, so this evidence is superseded for AutoFishing by `20260707-0006-autofishing-hotkey-regression.md`.

### 20-minute 50-key pressure smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-191553`.

The comparable Phase 8.22 pressure route passed after 1200 seconds of title idle plus a save entry:

```text
RunStatus=Passed
SaveLoaded=Passed
SaveLoadCycle=Passed
SmokeInputPollingDiagnostics=Passed
SmokeVirtualInputPressure=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
SmokeVirtualInputKeyCount=50
```

Final input diagnostic summary:

```text
legacyRegisteredStringPath=false
elapsedSeconds=1193.5
frames=4652
registeredButtonsMax=56
totalButtonsPolled=840
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=840
cachedDownSamples=0
cachedPressedEdges=0
cachedReleasedEdges=0
titleFrames=4637
titleButtonsPolled=0
gameplayFrames=15
gameplayButtonsPolled=840
```

The run reached `SaveLoaded hook dispatched` and `NativeContinuation.Step=DolocAPI.LoadGame.Exit`. No `Fatal error in GC / Unexpected mark stack overflow` window was detected, and no live dump was captured because the process exited cleanly.

## Documentation

- Marked `20260707-0004-hotkey-rebuild.md` manual follow-up as half-pass: feature behavior passed, but short/soft and rapid taps did not feel native enough.
- Updated ISSUE-010 with the edge-sampling fix and new short/pressure evidence.
- Added regression matrix row `INPUT-HOTKEY-EDGE-SAMPLING-50-VIRTUAL-PRESSURE-20260707`.
- Updated `docs/api/public-api-matrix.md` to keep Input `Experimental` while documenting frame-edge consumption and generic modifier behavior.

## Classification

The edge-sampling fix addresses the user-observed native-feel regression from the first rebuild. It keeps the scoped active-button union and avoids restoring the old title-idle registered-string polling path. The 20-minute 50-key pressure route still passes, so this remains a mitigation improvement for the verified input-polling amplifier.

This did not close ISSUE-010 at the time of the edge-sampling update. The later 20260707-0007 runtime closure supplied the manual retest, original unsuppressed YConsole+Zoom route, and no-virtual long-route evidence; ISSUE-010 remains open only for the broader long-term gameplay/native GC class.

## Rollback Notes

- Revert `InputButtonSample` boundary changes and `ReflectedUnityInput.SampleButtonCached` together; Core and Bootstrap now share the edge-frame contract.
- If generic modifiers regress, temporarily parse `Ctrl` / `Shift` / `Alt` back to left-side physical aliases while preserving edge sampling.
- If a cached driver is machine-specific, keep the scoped sampling frame and route only that backend through the existing fallback/diagnostic paths.

## Follow-Up

- Superseded by `20260707-0007`: user manual retest, short functional smoke, original YConsole+Zoom long route, and no-virtual long route all passed.
