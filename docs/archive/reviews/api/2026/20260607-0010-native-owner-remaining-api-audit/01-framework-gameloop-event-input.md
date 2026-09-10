# 01 - Framework, GameLoop, Event, And Input Remaining APIs

## Scope

Audited remaining framework event/input APIs not already covered by the 0008 `IInputHelper.Suppress` special audit:

- `IGameLoopEvents.GameLaunched`
- `IGameLoopEvents.UpdateTicked`
- `IGameLoopEvents.OneSecondUpdateTicked`
- `IGameLoopEvents.ReturnedToTitle`
- `IInputEvents.ButtonPressed`
- `IInputEvents.ButtonReleased`
- `IInputHelper.RegisterButton`
- `IInputHelper.UnregisterButton`
- `IInputHelper.GetRegisteredButtons`
- `IInputHelper.IsDown`
- `IInputHelper.WasPressed`

`IInputHelper.Suppress` is intentionally excluded here because `20260607-0008` already found it blocked: Core stores a suppressed set, but no sampler or native action owner consumes it.

## Files read

- `docs/api/public-api-matrix.md`: lines 9-17.
- `src/DTMAPI.Abstractions/Events.cs`: lines 5-30 and 60-83.
- `src/DTMAPI.Abstractions/Helpers.cs`: lines 6-22 and 152-162.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`: lines 65-144 and 179-193.
- `src/DTMAPI.Core/Services/EventManager.cs`: lines 12-61, 83-96, and 111-185.
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`: lines 373-410.
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`: lines 43-80 and 303-317.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`: lines 207-220 and 871-880.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`: lines 39-45.
- `testmods/DebugConsoleMod/ModEntry.cs`: lines 23-30 and 97-118.
- `docs/hook-map/README.md`: lines 339-388.
- `docs/debug/regressions/smoke-matrix.md`: lines 47 and 53.

## Functions read

- `DTMAPI.Abstractions.IGameLoopEvents.GameLaunched`: `src/DTMAPI.Abstractions/Events.cs:18`.
- `DTMAPI.Abstractions.IGameLoopEvents.UpdateTicked`: `src/DTMAPI.Abstractions/Events.cs:19`.
- `DTMAPI.Abstractions.IGameLoopEvents.OneSecondUpdateTicked`: `src/DTMAPI.Abstractions/Events.cs:20`.
- `DTMAPI.Abstractions.IGameLoopEvents.ReturnedToTitle`: `src/DTMAPI.Abstractions/Events.cs:21`.
- `DTMAPI.Abstractions.IInputEvents.ButtonPressed`: `src/DTMAPI.Abstractions/Events.cs:27`.
- `DTMAPI.Abstractions.IInputEvents.ButtonReleased`: `src/DTMAPI.Abstractions/Events.cs:28`.
- `DTMAPI.Abstractions.IInputHelper.RegisterButton`: `src/DTMAPI.Abstractions/Helpers.cs:154`.
- `DTMAPI.Abstractions.IInputHelper.UnregisterButton`: `src/DTMAPI.Abstractions/Helpers.cs:155`.
- `DTMAPI.Abstractions.IInputHelper.GetRegisteredButtons`: `src/DTMAPI.Abstractions/Helpers.cs:156`.
- `DTMAPI.Abstractions.IInputHelper.IsDown`: `src/DTMAPI.Abstractions/Helpers.cs:157`.
- `DTMAPI.Abstractions.IInputHelper.WasPressed`: `src/DTMAPI.Abstractions/Helpers.cs:158`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.Start`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:65`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.Update`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:104`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputPressed`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:119`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputReleased`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:133`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.NotifyReturnedToTitle`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:179`.
- `DTMAPI.Core.Services.InputService.RegisterButton`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:380`.
- `DTMAPI.Core.Services.InputService.UnregisterButton`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:386`.
- `DTMAPI.Core.Services.InputService.SetPressed`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:394`.
- `DTMAPI.Core.Services.InputService.SetReleased`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:400`.
- `DTMAPI.Core.Services.InputService.ClearFrame`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:405`.
- `DTMAPI.BepInExBootstrap.BootstrapPlugin.PollRegisteredInputButtons`: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303`.
- `DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks.ReturnHomePostfix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:39`.
- `DebugConsoleMod.ModEntry.OnButtonPressed`: `testmods/DebugConsoleMod/ModEntry.cs:97`.

## Call graph

```text
GameLoop.GameLaunched
  DtmApiRuntime.Start (65)
    LoadMods / Register APIs
    runtime.SetHookStatus("GameLoop.GameLaunched")
    Events.GameLoop.DispatchGameLaunched
      EventManager.EventSlot.Dispatch
      mod handlers

UpdateTicked / OneSecondUpdateTicked
  BootstrapPlugin.Update loop
    DtmApiRuntime.Update (104)
      Events.GameLoop.DispatchUpdateTicked
      if elapsed >= 1 second -> DispatchOneSecondUpdateTicked
      Input.ClearFrame

ReturnedToTitle
  Harmony Postfix: DolocAPI.ReturnHome
    DolocTownHookCallbacks.ReturnHomePostfix (39)
      camera/equipment/motor cleanup
      DtmApiRuntime.NotifyReturnedToTitle (179)
        Save.ClearCurrentSlot
        CustomEntities.OnReturnedToTitleBoundary
        Events.GameLoop.DispatchReturnedToTitle

Input hotkeys
  ordinary mod helper.Input.RegisterButton("F6"/"Y"/etc.)
    DtmHelper.Input -> InputService.RegisterButton (380)
  BootstrapPlugin.PollRegisteredInputButtons (303)
    ReflectedUnityInput.GetKeyDown / GetKey
    DtmApiRuntime.RecordInputPressed/Released
      InputService.SetPressed/SetReleased
      Events.Input.DispatchButtonPressed/Released
      mod handlers
```

## Function body findings

- `DtmApiRuntime.Start` registers services/APIs, loads mods, then dispatches `GameLaunched` after mod entry has run. This is a DTMAPI lifecycle owner, not a native Doloc Town scene-ready callback (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:65-102`).
- `DtmApiRuntime.Update` increments an internal tick and dispatches `UpdateTicked` unless `runtime.UI.BlocksModUpdates` is true. It still checks the one-second timer and clears the input pressed-frame set each update (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:104-117`).
- `RecordInputPressed` returns early when DTMAPI UI blocks gameplay hotkeys. Otherwise it records the button in Core input state and dispatches `ButtonPressed`; no native action owner is called from this path (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:119-131`).
- `RecordInputReleased` clears Core down-state and dispatches `ButtonReleased` unless UI blocks hotkeys. It does not inspect the suppressed set (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:133-140`).
- `InputService` is a pure Core state holder: `registered`, `down`, `pressedThisFrame`, and `suppressed`. Register/unregister mutate the registration set; `SetPressed`, `SetReleased`, `ClearFrame`, `IsDown`, and `WasPressed` only maintain DTMAPI state (`src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:373-410`).
- `BootstrapPlugin.PollRegisteredInputButtons` is the only sampler found for registered buttons. It iterates `runtime.GetRegisteredInputButtons()`, calls reflected Unity input, and forwards transitions into `RecordInputPressed/Released` (`src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303-317`).
- `ReturnedToTitle` reaches a native-ish boundary only through `DolocAPI.ReturnHome` postfix in GameBridge. The callback also cleans DTMAPI camera/equipment/motor state before dispatching the event (`src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:39-45`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:179-193`).
- Y-console input is not a general `Suppress` consumer. `DebugConsoleMod` registers `Y` and `Escape`, toggles the debug host after SaveLoaded, and uses the host/native isolation path while open (`testmods/DebugConsoleMod/ModEntry.cs:23-30`, `testmods/DebugConsoleMod/ModEntry.cs:97-118`).

## Native owner verdict

| API group | Verdict | Native owner reached |
| --- | --- | --- |
| `GameLaunched` | DTMAPI-only | No native owner required; dispatch occurs from runtime startup after mod load. |
| `UpdateTicked` / `OneSecondUpdateTicked` | DTMAPI-only | No native simulation owner; bootstrap update pump drives the events. |
| `ReturnedToTitle` | Partial | `DolocAPI.ReturnHome` postfix is reached, but alternative title/exit flows were not proven in this pass. |
| `ButtonPressed` / `ButtonReleased` | DTMAPI-only | Reflected Unity input polling feeds Core events; native Doloc Town gameplay actions are not part of this API. |
| `RegisterButton` / `IsDown` / `WasPressed` | DTMAPI-only | Core `InputService` state only. |

## Ordinary mod usability

- `GameLaunched`: 普通 mod 可用; keep `stable`.
- `UpdateTicked` / `OneSecondUpdateTicked`: 普通 mod 可用 with caution; keep `experimental` until documented as DTMAPI pump events rather than native gameplay ticks.
- `ReturnedToTitle`: 普通 mod 可用 for cleanup; keep `experimental`.
- `ButtonPressed` / `ButtonReleased` and `RegisterButton` / `IsDown` / `WasPressed`: 普通 mod 可用 for hotkeys; keep `experimental`.
- `Suppress`: 禁止依赖; inherited from 0008, not reclassified here.

## Concrete failure modes

1. A mod that treats `UpdateTicked` as a native simulation tick can drift from game systems because the event is emitted by DTMAPI's bootstrap pump and can be blocked while DTMAPI UI blocks mod updates.
2. A mod that assumes `ReturnedToTitle` catches every title transition may leave sidecar state behind if a future build uses a path other than patched `DolocAPI.ReturnHome`.
3. A hotkey mod that relies on `RegisterButton` for native controller/action semantics only receives reflected button transitions; it does not know whether Doloc Town consumed the same key for a tool, item, menu, or movement.
4. A mod that combines `WasPressed` with long-running frame work can miss inputs because `pressedThisFrame` is cleared by `InputService.ClearFrame` inside the runtime update cycle.
5. A mod that uses `Suppress` for `B`, `Mouse0`, or tool buttons can still open backpack/use tools/items because the suppressed set is not wired into the native `AgentControllerState` action owners.

## Minimal rebuild direction

- Keep lifecycle/hotkey APIs as DTMAPI-owned framework contracts.
- Document `UpdateTicked` as "DTMAPI update pump" and not as a physics/game simulation event.
- Add a future title-boundary audit if native code exposes additional exit/title methods beyond `DolocAPI.ReturnHome`.
- Split input into two future contracts: `HotkeyInput` for current reflected Unity polling and `GameplayActionSuppression` for a real native owner adapter. That second contract must be separate from `IInputHelper.Suppress`.
- If controller/gamepad support is desired, add a sampled-device map and native action-context reporting rather than overloading the current string key helper.

## Evidence gaps

- No decompiled/native candidate was found in this pass for a general "all title exits" owner beyond the existing `DolocAPI.ReturnHome` postfix.
- No consumer was found for `InputService.suppressed`; 0008 already searched Core sampler, runtime dispatch, bootstrap input sampling, and GameBridge input hooks.
- No smoke was run in this pass. Existing evidence comes from public matrix lines 9-17, hook-map Y-console lines 339-388, and smoke-matrix input/Y-console rows 47 and 53.
