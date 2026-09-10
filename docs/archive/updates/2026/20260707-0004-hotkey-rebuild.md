# 20260707-0004 - Hotkey Rebuild

Status: source-and-runtime-smoke-verified / 50-key pressure passed 20m / issue-010-open

## Source Request

The user asked to rebuild the DTMAPI hotkey/input layer before physically splitting product mod boundaries, migrate YConsole, AutoFishing, and Zoom as the first consumers, then validate with a 5-minute input counter smoke, a 2-minute 50-virtual-key pressure confirmation, a short functional smoke, and a 20-minute 50-virtual-key pressure smoke before user manual testing.

## Changed Files

- `src/DTMAPI.Abstractions/Input.cs`
- `src/DTMAPI.Abstractions/Events.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuItems.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/DebugConsoleMod/ModEntry.cs`
- `testmods/ZoomMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260707-0004-hotkey-rebuild.md`

## Implementation

- Added typed input abstractions: `DtmButton`, `DtmKeybind`, `DtmKeybindList`, `DtmInputScope`, and owner-bound `IInputRegistration`.
- Added `IInputEvents.KeybindPressed` / `KeybindReleased` while preserving legacy `ButtonPressed` / `ButtonReleased`.
- Reworked Core input state around owner-bound registrations, scoped dispatch, dirty cached button sampling, and frame-level pressed/released state.
- Changed Bootstrap ordinary input processing to ask Core for scoped buttons to sample, sample each button once through cached InputSystem/Legacy/Win32 drivers, and record a single input frame. Ordinary hotkeys no longer call the old `PollRegisteredInputButtons()` registered-string reflection loop.
- Kept `RegisterButton` / `UnregisterButton` as compatibility wrappers backed by the new registry instead of a separate registered-string polling path.
- Kept reflected Unity input available for title key capture, diagnostic hotkeys, and explicit fallback behavior.
- Canonicalized physical keys so `Plus`, `Equals`, and `+` refer to the same physical `Equals` key, while `KeypadPlus` remains distinct and `None` means unbound.
- Updated config keybind parsing and conflict detection to use canonical physical key/chord identity instead of raw string comparison.
- Migrated `DebugConsoleMod` to typed Y/Escape save-loaded keybinds, `ZoomMod` to typed keybind lists without duplicate `Plus`/`Equals` registration, and `AutoFishingMod` to a typed F6 toggle with manual cancel keys read from the current snapshot only while automation is enabled.
- Left product mod physical packaging untouched. These mods are consumers/evidence for the new input layer, not proof that their product boundaries are Core APIs.

## Validation

- PowerShell parser check passed for `tools/scripts/run-game-smoke.ps1`.
- `dotnet build DTMAPI.sln -c Release` passed with 0 warnings and 0 errors.
- `dotnet test tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj -c Release` exited successfully.
- The console unit runner passed with `DOTNET_ROLL_FORWARD=Major` because the local machine has .NET 6 and .NET 9 runtimes but not .NET 8.
- `git diff --check` exited successfully with line-ending normalization warnings only.
- Runtime lock was acquired before local game operations and released after the smoke sequence.
- Final process checks found no leftover `DolocTown.exe`.

## Runtime Evidence

### 5-minute input counter smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-172738`.

The existing long-title/save-load result gates mismatched, but the input diagnostic and process/fatal checks passed:

```text
SmokeInputPollingDiagnostics=Passed
legacyRegisteredStringPath=false
elapsedSeconds=292.9
frames=1052
registeredButtonsMax=8
totalButtonsPolled=128
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=128
titleFrames=1036
titleButtonsPolled=0
gameplayFrames=16
gameplayButtonsPolled=128
NoFatalInstanceWindow=Passed
ProcessExited=Passed
```

### 2-minute 50-key pressure confirmation

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-173334`.

The existing result gates mismatched, but the virtual pressure route and input diagnostics passed:

```text
SmokeVirtualInputPressure=Passed
requestedKeys=50
registeredKeys=50
ownerButtonCount=50
registeredButtonUnion=56
pressureSucceeded=True
SmokeInputPollingDiagnostics=Passed
legacyRegisteredStringPath=false
totalButtonsPolled=896
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=896
titleButtonsPolled=0
NoFatalInstanceWindow=Passed
ProcessExited=Passed
```

### Third-save functional smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-173636`.

The short functional smoke passed:

```text
RunStatus=Passed
SaveLoaded=Passed
DebugConsoleOpenY1=Passed
DebugConsoleCloseEscape=Passed
DebugConsoleOpenY2=Passed
DebugConsoleCloseY=Passed
Zoom=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
```

The `AutoFishingHotkey` result field was skipped by the existing harness gate, but logs confirm the rebuilt keybind path dispatched F6 and enabled the automation:

```text
Input F6 pressed dispatched
Fishing automation state Yuuka.DTMAPI.AutoFishing enabled=True reason=hotkey F6
[Yuuka.DTMAPI.AutoFishing] AutoFishing automation enabled reason=hotkey F6
```

### 20-minute 50-key pressure smoke

Evidence: `docs/debug/evidence/GAME-SMOKE/20260707-173940`.

The comparable pressure route passed after 1200 seconds of title idle plus a save entry:

```text
RunStatus=Passed
SaveLoadCycle=Passed
SmokeInputPollingDiagnostics=Passed
SmokeVirtualInputPressure=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
requestedKeys=50
registeredKeys=50
registeredButtonUnion=56
legacyRegisteredStringPath=false
elapsedSeconds=1193.2
frames=4656
registeredButtonsMax=56
totalButtonsPolled=1008
getKeyDownCalls=0
getKeyCalls=0
cachedStateSamples=1008
titleFrames=4638
titleButtonsPolled=0
gameplayFrames=18
gameplayButtonsPolled=1008
```

No `Fatal error in GC / Unexpected mark stack overflow` window was detected, and no live dump was captured because the process exited cleanly.

## Documentation

- Updated `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md` with the rebuilt input-layer evidence and classification.
- Added regression matrix row `INPUT-HOTKEY-REBUILD-50-VIRTUAL-PRESSURE-20260707`.
- Updated `docs/api/public-api-matrix.md` to keep Input `Experimental` while documenting the typed keybind direction and the explicit lack of Doloc Town native input suppression.
- Preserved the API boundary review conclusion that product mods are consumers/evidence, not Core stable API proof.

## Manual Test Follow-Up

User manual testing after this rebuild is classified as **half-pass**:

- Functional behavior passed: AutoFishing toggle, Y console open/close and internal input, and Zoom controls were usable.
- Input feel did not pass native-quality expectations: short/soft Y taps could be missed, and rapid repeated taps did not always produce repeated responses.
- Code review attributed the feel gap to this rebuild sampling only current down-state and deriving edges in Core. That can miss `down -> up` transitions between DTMAPI frames and can swallow a repeated tap when release was missed.
- Superseded follow-up: `docs/updates/2026/20260707-0005-hotkey-edge-sampling.md` upgrades the frame contract to `IsDownNow` / `PressedEdge` / `ReleasedEdge`.

## Classification

The old registered-string polling path was a verified amplifier in Phase 8.22. This rebuild removes that amplifier from ordinary title idle: 50 virtual keybinds can remain registered as owner-bound roots without producing title-idle registered-string `GetKeyDown` / `GetKey` reflection calls.

This did not close ISSUE-010 at the time of the first rebuild. It showed the first mitigation worked for the comparable 50-key pressure route; the later 20260707-0007 edge follow-up supplied the original unsuppressed YConsole+Zoom route and no-virtual long-route evidence and reclassified the main-menu input-pressure path as mitigated.

## Rollback Notes

- Revert the typed input API and Core/Bootstrap input-frame changes together; partial rollback would leave consumers on interfaces the runtime no longer dispatches.
- If only a consumer regresses, temporarily move that consumer back to the `RegisterButton` compatibility wrapper while preserving the scoped cached input frame.
- If the cached input drivers regress on a specific machine, `ReflectedUnityInput` still contains the explicit fallback routes for diagnostics and title key capture.

## Follow-Up

- Rebuild the input frame around native/cached pressed and released edges, not only current down-state.
- Superseded by `20260707-0005` and `20260707-0007`: edge sampling, manual retest, 50-key pressure, original YConsole+Zoom route, and no-virtual long route all passed.
