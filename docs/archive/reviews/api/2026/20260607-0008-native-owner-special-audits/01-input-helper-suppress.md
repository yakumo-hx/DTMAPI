# 01 - IInputHelper.Suppress Special Audit

## 1. Scope

API under review: `DTMAPI.Abstractions.IInputHelper.Suppress(string button)` and its observable companion `GetSuppressedButtons()`.

Questions answered:

- Whether Core `InputService.Suppress/GetSuppressedButtons` has any consumer.
- Whether Bootstrap input sampling, `DtmApiRuntime.RecordInputPressed/Released`, or GameBridge input hooks read the suppressed set.
- Whether Y-console input isolation and ordinary-mod `Suppress` share the same mechanism.
- Why `Suppress("B")` can still open backpack or trigger native tool/item actions.

Result: `Blocked`. `Suppress` writes a DTMAPI-only set that is never consulted by native input or by DTMAPI's own polling path.

## 2. Files read

- `docs/api/public-api-matrix.md:17`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:52`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md:53`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/07-completion-audit.md:81`
- `docs/updates/2026/20260607-0007-native-responsibility-code-review.md:68`
- `docs/hook-map/README.md:375`
- `docs/debug/INDEX.md:25`
- `docs/debug/regressions/smoke-matrix.md:13`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- Reverse candidate search under `references/doloc-town/reverse/builds/` for `AgentControllerState`, `EnterUICheck`, `UseTool`, `UseItem`, and input map references.

## 3. Functions read

- `DTMAPI.Abstractions.IInputHelper.Suppress(string button)`: `src/DTMAPI.Abstractions/Helpers.cs:160`
- `DTMAPI.Abstractions.IInputHelper.GetSuppressedButtons()`: `src/DTMAPI.Abstractions/Helpers.cs:161`
- `DTMAPI.Core.Services.InputService.Suppress(string button)`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:408`
- `DTMAPI.Core.Services.InputService.GetSuppressedButtons()`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:409`
- `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputPressed(string button)`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:119`
- `DTMAPI.Core.Runtime.DtmApiRuntime.RecordInputReleased(string button)`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:133`
- `DTMAPI.BepInExBootstrap.BootstrapPlugin.Update()`: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:274`
- `DTMAPI.BepInExBootstrap.BootstrapPlugin.PollRegisteredInputButtons()`: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303`
- `DTMAPI.BepInExBootstrap.ReflectedDebugConsoleUi.Open()`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:160`
- `DTMAPI.BepInExBootstrap.ReflectedDebugConsoleUi.Close()`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:176`
- `DTMAPI.BepInExBootstrap.ReflectedDebugConsoleUi.Update()`: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:224`
- `DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks.AgentControllerStateUseToolPrefix()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:138`
- `DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks.AgentControllerStateUseItemPrefix()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:143`
- `DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks.AgentControllerStateEnterUiCheckPrefix(ref bool)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:148`
- `DTMAPI.GameBridge.DolocTown.DolocTownHookCallbacks.AllowNativeGameplayInput(string)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:192`
- Debug-console hook installation in `DolocTownGameBridge`: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:958`

## 4. Call graph

Ordinary mod path:

`mod -> IInputHelper.Suppress("B") -> InputService.Suppress("B") -> InputService.suppressed.Add("B") -> no consumer`

DTMAPI input event path:

`BootstrapPlugin.Update -> PollRegisteredInputButtons -> ReflectedUnityInput.GetKeyDown/GetKey -> DtmApiRuntime.RecordInputPressed/Released -> InputService.SetPressed/SetReleased -> Events.DispatchButtonPressed/Released`

This path never calls `GetSuppressedButtons()` and never tests `InputService.suppressed`.

Y-console native isolation path:

`ReflectedDebugConsoleUi.Open/Update/Close -> DolocTownHookCallbacks.DebugConsoleModalOpen -> AgentControllerState.EnterUICheck/UseTool/UseItem Harmony prefixes -> AllowNativeGameplayInput`

This path blocks native gameplay input only while the DTMAPI Y console is open. It does not read the public `Suppress` set and does not care which button was suppressed.

## 5. Function body findings

- `InputService.Suppress` at `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:408` only adds the string to a `HashSet<string>`. There is no duration, owner, frame clear, action mapping, or native callback.
- `InputService.GetSuppressedButtons` at `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:409` sorts and returns the set. Source search found no runtime consumer of this method beyond the API surface.
- `DtmApiRuntime.RecordInputPressed` at `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:119` checks `UI.BlocksGameplayHotkeys` before recording DTMAPI events, then calls `Input.SetPressed` and dispatches the DTMAPI button event. It does not check whether the button is suppressed.
- `DtmApiRuntime.RecordInputReleased` at `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:133` updates DTMAPI button state and conditionally dispatches release events. It does not check suppression.
- `BootstrapPlugin.PollRegisteredInputButtons` at `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:303` polls only registered buttons and dispatches DTMAPI pressed/released state. It has no bridge from suppressed DTMAPI button names to native input actions.
- `ReflectedDebugConsoleUi.Update` at `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:224` sets `ConsumedInputThisFrame` and `DebugConsoleModalOpen` based on the console UI state. This is a modal console guard, not a public suppression API.
- `DolocTownHookCallbacks.AgentControllerStateEnterUiCheckPrefix` at `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:148` returns a forced native UI-block result only when `DebugConsoleModalOpen` is true.
- `DolocTownHookCallbacks.AllowNativeGameplayInput` at `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:192` blocks native `UseTool`/`UseItem` only when the Y console is open.
- `DolocTownGameBridge` installs the modal hooks at `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:966`, `:971`, and `:976`, targeting `AgentControllerState.UseTool`, `UseItem`, and `EnterUICheck`. The hook source is the debug console, not `IInputHelper`.

## 6. Native owner verdict

Verdict: `Blocked`.

Reached owner: none for ordinary-mod `Suppress`.

Excluded paths:

- Core `InputService.suppressed` is excluded because it has no consumer and no native action mapping.
- Bootstrap input sampling is excluded because it dispatches DTMAPI events from registered buttons but never reads the suppressed set.
- `DtmApiRuntime.RecordInputPressed/Released` is excluded because it updates DTMAPI event state only.
- GameBridge Y-console hooks are excluded as the public `Suppress` owner because they key off `DebugConsoleModalOpen`, not the button string.

Native owner candidates still required:

- A native action-level owner for backpack/menu entry, likely near `AgentControllerState.EnterUICheck`.
- A native tool/use owner, likely near `AgentControllerState.UseTool`.
- A native item-use owner, likely near `AgentControllerState.UseItem`.
- A mapping from DTMAPI button names such as `"B"` to the game's real input/action source. This mapping was not found in the current DTMAPI code.

## 7. Ordinary mod usability

Ordinary mod usability: `禁止依赖`.

`Suppress` is safe only as a DTMAPI-side diagnostic marker. It is not safe as an input-cancel API for ordinary mods, because native Doloc Town gameplay input never reads the set.

## 8. Concrete failure modes

1. `Suppress("B")` can still open the native backpack/menu because the only menu-blocking native hook, `AgentControllerStateEnterUiCheckPrefix`, checks `DebugConsoleModalOpen`, not `"B"` suppression.
2. `Suppress("Mouse0")`, `Suppress("UseTool")`, or any equivalent string can still trigger native tool logic because `AgentControllerStateUseToolPrefix` only blocks while the Y console is open.
3. `Suppress("Mouse1")` or item-use-like strings can still trigger native item logic because `AgentControllerStateUseItemPrefix` ignores the suppressed set.
4. Suppression can become stale DTMAPI state because `Suppress` only adds to a set and there is no owner-scoped or frame-scoped release path in `InputService`.
5. A mod can observe its string in `GetSuppressedButtons()` and falsely conclude gameplay was suppressed, creating a "DTMAPI UI success, native gameplay failure" mismatch.

## 9. Minimal rebuild direction

- Split the current helper into a truthful registry/status API and a separate native action-suppression API.
- Define suppression by native action, not by arbitrary button string: menu/backpack, use tool, use item, movement, placement, and debug-console modal should be independent actions.
- Add owner and lifetime semantics: one frame, until disposed, modal-only, or scoped to a DTMAPI UI surface.
- Route suppression through GameBridge owners such as `AgentControllerState.EnterUICheck`, `UseTool`, and `UseItem`, with per-action hook evidence.
- Keep Y-console isolation as one consumer of the same owner map, rather than a separate hidden mechanism.

## 10. Evidence gaps

- Missing reverse evidence that maps the physical `"B"` key to the exact native backpack/menu action path in the current build.
- Missing native action inventory beyond the three current Y-console hooks; movement, placement, building, and UI selection may use other owners.
- Missing ordinary-mod smoke showing `Suppress("B")` fails, because this review was docs-only and did not run game smoke.
- Missing design for frame/owner lifetime; current code has no way to clear a suppressed button except replacing the whole service.
