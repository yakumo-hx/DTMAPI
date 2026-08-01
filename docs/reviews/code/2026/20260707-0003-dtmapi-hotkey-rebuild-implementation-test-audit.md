# DTMAPI Hotkey Rebuild - Implementation, Test, Audit Route

Date: 2026-07-07
Status: Proposed rebuild route
Scope: DTMAPI-owned hotkey service only. Do not replace official `DolocAPI.UserInput`.

## Decision

DTMAPI can provide a stable, SMAPI-like hotkey experience for DTMAPI mods, but it must not copy SMAPI's XNA `Game1.input` replacement mechanism.

The rebuild target is:

1. DTMAPI owns its own bounded hotkey state.
2. Unity/InputSystem is sampled once per frame through a cached driver.
3. Mods use typed keybinds and input events instead of forcing Bootstrap to poll registered strings.
4. DTMAPI hotkeys are independent from native Doloc Town actions.
5. Native action suppression is not part of the ordinary hotkey layer unless a separate GameBridge-owned native action API is proven.

Large DTMAPI core refactors are allowed. The stability target is higher priority than preserving the current `RegisterButton` implementation shape.

## Current Code Findings

Current public surface:

- `src/DTMAPI.Abstractions/Helpers.cs:229` exposes `IInputHelper.RegisterButton`, `UnregisterButton`, `GetRegisteredButtons`, `IsDown`, `WasPressed`, `Suppress`.
- `src/DTMAPI.Abstractions/Events.cs:25` exposes `IInputEvents.ButtonPressed` and `ButtonReleased`, but event args only carry a string button at `Events.cs:74`.

Current runtime state:

- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:399` stores `registered`, `ownersByButton`, `buttonsByOwner`, `down`, `pressed`, and `suppressed`.
- `WorkshopContentInputUi.cs:442` builds a new union and sorted array in `GetRegisteredButtons()`.
- `WorkshopContentInputUi.cs:544` records an owner-bound registration even when the owner/button pair already existed.
- `WorkshopContentInputUi.cs:524` implements `Suppress` only inside DTMAPI helper state. It does not alter native Doloc Town input consumption.

Current frame path:

- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:262` enters `TickFromUnity`.
- `BootstrapPlugin.cs:331` skips registered hotkey polling only while the debug console consumed input.
- `BootstrapPlugin.cs:377` loops over `runtime.GetRegisteredInputButtons()`.
- Each registered button calls `ReflectedUnityInput.GetKeyDown(button)` and sometimes `GetKey(button)`.
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs:61` tries legacy Unity reflection, InputSystem reflection, then Win32 fallback for every queried key.
- `ReflectedUnityInput.cs:138` still calls `Enum.Parse` inside the legacy path.
- `ReflectedUnityInput.cs:42` and `ReflectedUnityInput.cs:288` map both `Plus` and `Equals` to the same physical key.

Current first-party product pressure:

- `testmods/DebugConsoleMod/ModEntry.cs:23` registers `Y` and `Escape` during `Entry()`, even though `ModEntry.cs:101` ignores `Y` outside a save.
- `testmods/ZoomMod/ModEntry.cs:149` registers five roots, including `Equals`, hardcoded `Plus`, `KeypadPlus`, and `KeypadMinus`; `ModEntry.cs:105` can react to physical aliases more than once.
- `testmods/AutoFishingMod/ModEntry.cs:151` registers the toggle key and all manual-cancel keys during `Entry()` / config save; manual-cancel keys matter only while automation is enabled.

Existing tests verify owner cleanup and event blocking, but not the failure mode we need to retire:

- `tests/DTMAPI.UnitTests/Program.cs:2143` verifies owner-bound registration cleanup.
- `Program.cs:3005` verifies UI boundaries block gameplay hotkeys.
- `Program.cs:3059` verifies one-frame DTMAPI-only suppression.
- Missing: canonical physical-key parsing, duplicate alias dispatch prevention, one-frame input sampling, no per-frame allocations for idle hotkeys, scope enter/exit, and long title-idle accumulation checks.

## Non-Goals

- Do not replace `DolocAPI.UserInput`.
- Do not replace official `DolocInputSource` or official `InputActionAsset`.
- Do not claim that DTMAPI hotkey `Suppress` blocks native game input.
- Do not keep YConsole, AutoFishing, or Zoom as DTMAPI core responsibilities. They are first-party experimental products and should be migrated out of the runtime/bottom-layer source boundary.

## Target Architecture

### Abstractions

Add typed input concepts in `DTMAPI.Abstractions`:

- `DtmButton`: canonical physical button identity.
- `DtmButtonState`: `Down`, `Pressed`, `Released`, `Suppressed`.
- `DtmKeybind`: one chord such as `LeftShift + F6`.
- `DtmKeybindList`: one or more alternative chords, SMAPI-style.
- `DtmInputSnapshot`: current-frame immutable view for helper queries.
- `DtmInputScope`: `Always`, `Title`, `SaveLoaded`, `Gameplay`, `WhileLeaseActive`.

Keep old string event args during migration, but add typed properties:

- `Button`: compatibility display/canonical name.
- `PhysicalButton`: canonical `DtmButton`.
- `IsSuppressed`: DTMAPI hotkey-layer suppression only.

Mark old `RegisterButton`/`UnregisterButton` as compatibility. New mods should not need to register ordinary buttons to receive keybind state.

### Core Runtime

Replace `InputService` with a two-part service:

1. `DtmInputState`
   - Bounded sets: `down`, `pressed`, `released`, `suppressed`.
   - No owner/button registry in the hot path.
   - No per-frame sorting, LINQ, or array allocation.
   - Release tracking must clear stale down state even when UI blocks dispatch.

2. `DtmHotkeyRegistry`
   - Optional owner-bound hotkey leases for diagnostics, scopes, and conflict checks.
   - Dirty cached owner snapshot, rebuilt only when registrations change.
   - Duplicate owner/key registrations are idempotent and must not record repeated ledger roots.

Runtime frame ordering should become:

1. Bootstrap samples physical input once.
2. Runtime begins input frame with `DtmInputFrame`.
3. Runtime updates DTMAPI state and dispatches input events if UI scope allows.
4. `UpdateTicked` and `OneSecondUpdateTicked` run.
5. Runtime ends input frame and clears one-frame state.

This preserves SMAPI's important property: one coherent input frame, then events, then cleanup.

### Bootstrap Driver

Add an internal `IDtmInputDriver`:

```csharp
internal interface IDtmInputDriver
{
    bool TrySampleFrame(DtmInputFrameBuilder builder);
}
```

Primary implementation:

- `UnityInputSystemDtmInputDriver`
- Resolve `Keyboard.current`, `Mouse.current`, and supported controls once.
- Cache delegates or `PropertyInfo` handles once during initialization.
- During a frame, read known controls into a reusable builder.
- Do not call `ReflectedUnityInput.GetKeyDown` per registered key.

Compatibility/fallback implementation:

- Keep `ReflectedUnityInput` for title key-capture, diagnostics hotkey, and emergency smoke fallback only.
- It must not be the ordinary registered-hotkey polling path after the rebuild.

### Key Parsing And Canonicalization

Create one parser used by runtime, config menu, and tests.

Rules:

- `None` means unbound.
- Empty input normalizes to `None`.
- `Equals` and `Plus` normalize to one physical keyboard key for dispatch/conflict purposes.
- `KeypadPlus` remains distinct.
- Left/right modifiers remain distinguishable, with optional aliases for `Shift`, `Ctrl`, `Alt`.
- Chords are order-insensitive for equality and conflicts.
- Invalid key names return structured parse errors with suggestions.

Config menu keybind conflict detection must use this parser instead of string trimming. `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs:36` should stop treating keybinds as raw strings.

### Product Split

The first three consumers are experimental products, not bottom-layer runtime features:

1. `DTMAPI.DebugConsoleMod`
   - Move out of runtime/bottom-layer source ownership.
   - Use `DtmKeybindList`.
   - Activate `Y`/`Escape` only in save/gameplay scope, or keep the keybind unregistered and let the handler ignore it with no polling cost.

2. `AutoFishing`
   - Keep native fishing automation in GameBridge where native phase ownership is already proven.
   - Move the toggle/cancel hotkeys to the new DTMAPI hotkey layer.
   - Register manual-cancel hotkeys only while automation is enabled, or evaluate them from the bounded input snapshot without adding permanent owner roots.

3. `Zoom`
   - Move out of runtime/bottom-layer source ownership.
   - Replace hardcoded `Plus`/`Equals` duplication with canonical `DtmKeybindList`.
   - Verify one physical `+` press causes one zoom step.

## Implementation Route

### Phase 0 - Freeze And Measure

- Add diagnostics counters for current frame polling count, registered union rebuild count, and input dispatch count.
- Record baseline for YConsole + Zoom + AutoFishing title idle.
- Keep current root-isolation profiles as historical evidence only, not as the final fix.

### Phase 1 - Typed Model And Parser

- Add `DtmButton`, `DtmKeybind`, `DtmKeybindList`, and parse result types.
- Add alias/canonical tables.
- Add display-name helpers independent of raw Unity strings.
- Add unit tests before replacing runtime behavior.

### Phase 2 - Bounded Input State

- Replace `InputService` internal state with `DtmInputState`.
- Keep `IInputHelper.IsDown` and `WasPressed` as compatibility wrappers over canonical state.
- Make duplicate owner/hotkey registration idempotent.
- Make owner snapshots dirty-cached and never rebuilt per frame.

### Phase 3 - Unity Input Driver

- Add cached `UnityInputSystemDtmInputDriver`.
- Wire Bootstrap to call `runtime.RecordInputFrame(frame)` once per Unity frame.
- Remove `PollRegisteredInputButtons()` from the ordinary path.
- Leave `ReflectedUnityInput` only for key capture and emergency fallback.

### Phase 4 - Config Menu Integration

- `AddKeybindOption` validates with `DtmKeybindList.Parse`.
- Conflicts use canonical physical keys and chords.
- Runtime hotkey leases can participate in conflict display.
- Save should reject invalid keybinds before applying mod config callbacks.

### Phase 5 - Product Migration

- Migrate DebugConsoleMod, AutoFishingMod, and ZoomMod first.
- Move their source/package ownership outside DTMAPI core/bottom-layer folders.
- Keep them as first-party experimental products for evidence gathering.
- Delete or obsolete product-specific root-isolation hacks once smokes pass without suppressing input roots.

### Phase 6 - Public Contract Cleanup

- Mark old `RegisterButton` as compatibility/obsolete if new keybind APIs pass smoke.
- Update `docs/api/public-api-matrix.md`: Input can move toward StableCandidate only after native-scope claims are removed and long-run evidence passes.
- Document that DTMAPI hotkey suppression is DTMAPI-only unless a future native action bridge says otherwise.

## Test Plan

### Unit Tests

Required new tests:

- `DtmButtonParser_NormalizesAliases`: `Equals` and `Plus` compare equal; `KeypadPlus` remains distinct.
- `DtmKeybind_ChordOrderInsensitive`: `LeftShift+F6` equals `F6+LeftShift`.
- `DtmKeybindList_AnyJustPressed`: multi-bind alternatives work without registration.
- `DtmInputState_BoundedFrameLifecycle`: pressed/released clear after one frame; down survives until release.
- `DtmInputState_UiBlockedReleaseClearsDown`: blocked UI does not leave stuck keys.
- `DtmHotkeyRegistry_DuplicateRegistrationIdempotent`: duplicate owner/key does not create duplicate ledger entries.
- `DtmHotkeyRegistry_ScopeEnterExit`: title/save/gameplay scopes change active hotkeys without stale roots.
- `ConfigMenu_KeybindConflictsCanonical`: `Equals` vs `Plus` conflict, `Equals` vs `KeypadPlus` does not.
- `Zoom_OnePhysicalPlusOneStep`: canonical duplicate aliases cannot dispatch two zoom steps.

Existing tests to adapt:

- `OwnerBoundInputTracksAndCleansPerOwner`
- `FailedCodeModCleanupRemovesOwnerBoundRuntimeState`
- `RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates`
- `Suppress_OneFrame_ClearsAfterUpdate`

### Integration Tests

Add a fake input driver for runtime-level tests:

- Feed frames directly into runtime.
- Assert event ordering: input frame -> ButtonPressed/ButtonReleased -> UpdateTicked -> clear.
- Assert no registered-button polling is required for ordinary `KeybindList` checks.

### Game Smoke

Required smokes before declaring this fixed:

1. Title idle with DebugConsole + Zoom + AutoFishing enabled for at least one hour.
2. LoadGame twice after title idle.
3. Repeat without root-isolation/suppress profiles.
4. Save gameplay: Y console opens/closes, Zoom increments/decrements once per physical key press, AutoFishing toggles and manual cancel works.
5. Config menu: changing keybinds updates behavior, detects canonical conflicts, and never requires restart for ordinary hotkeys.
6. ReturnToTitle then reload: no stale down/pressed/hotkey-owner state.

Long-run evidence should be attached to `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`.

## Audit Gates

Implementation is not acceptable unless all of these are true:

- No ordinary hotkey path calls `runtime.GetRegisteredInputButtons()` per frame.
- No ordinary hotkey path calls `ReflectedUnityInput.GetKeyDown` per registered button.
- No per-frame `OrderBy`, `ToArray`, `new HashSet`, or LINQ union in the hotkey polling path.
- No duplicate physical alias can dispatch duplicate `ButtonPressed` events.
- No product mod keeps title-idle input roots for save-only behavior.
- No public docs imply DTMAPI hotkey suppression blocks native Doloc Town actions.
- DebugConsole, AutoFishing, and Zoom are treated as first-party experimental products outside core runtime ownership.
- Unit tests and game smoke both pass without owner root isolation.

## Review Checklist

Before merging a hotkey rebuild PR:

- Search for `PollRegisteredInputButtons`, `GetRegisteredInputButtons`, and `ReflectedUnityInput.GetKeyDown`; any remaining ordinary hotkey use is a blocker.
- Search for `RegisterButton(` in first-party products; each use must be compatibility-only, scoped, or replaced with typed keybinds.
- Confirm config keybind validation uses the shared parser.
- Confirm diagnostics include active hotkey count, frame input count, stale down count, and driver mode.
- Confirm `docs/api/public-api-matrix.md` still marks any native suppression claim accurately.

## Done Definition

The rebuild is done when:

- DTMAPI hotkeys are sampled once per frame through a bounded state object.
- YConsole, AutoFishing, and Zoom use the new interface as first experimental products.
- The one-hour title-idle plus repeated LoadGame smoke passes without input-root suppression.
- The code audit finds no per-frame registered-string reflection polling path.
- The public API wording clearly separates DTMAPI hotkeys from native Doloc Town input actions.
