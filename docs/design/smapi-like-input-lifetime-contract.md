# DTMAPI 类 SMAPI 输入生命周期规范

Status: active design contract

## Shared frame

DTMAPI samples keyboard, mouse, and supported controller state into one coherent frame. Typed events and `IDtmInputHelper` queries read that same frame; a `DtmKeybindList` is a local configuration/query object and does not register itself with the platform.

Frame order is:

1. freeze the frame's input audience and sample its bounded active-button union plus held/rearm/settlement chord members;
2. publish current/down, pressed, and released edges;
3. dispatch scoped input events;
4. run mod update events, where local keybind queries read the same frame;
5. clear one-frame state and expire unused local watches by generation.

The audience is immutable for that frame. Opening or closing an overlay after sampling changes input eligibility on the next frame only:

- `Normal` preserves the existing Title, SaveLoaded, Gameplay and Always scope rules and ordinary broadcast compatibility.
- `OwnerModal` targets only the modal owner. In a loaded save it permits that owner's SaveLoaded/Always registrations; at title it permits Title/Always. Gameplay registrations remain ineligible.
- `PlatformModal`, including an official UI that blocks gameplay hotkeys, provides no new DTMAPI Mod input.

These modes do not stop `UpdateTicked`. The game world is not paused by a DTMAPI overlay, so all Mods keep the same update clock while Core isolates only its input audience.

## Edge settlement and rearm

Core records the exact event owners that received Pressed. Those owners receive the corresponding targeted Release even if a modal, official UI, scope, save/title boundary or keybind configuration change makes them ineligible before physical release. A configuration change retains the old binding snapshot until that release is settled. Owner cleanup drops that owner's pending receipt instead of calling dead code.

Any eligibility loss arms `requiresNeutralBeforePress`. Core keeps sampling held and rearm/chord members, but does not expose a new Pressed until the physical input has become neutral. This prevents a key pressed behind a modal, or held across save/title/config transitions, from toggling a Mod when eligibility returns.

Physical state and owner-visible state are separate. `Suppress()` hides a button only for the current DTMAPI frame; it does not delete physical down/edges, create a next-frame Pressed, or cancel an owed Release. In an owner modal only that owner may suppress; in a platform modal Mod suppression is inert. This is a DTMAPI delivery contract, not a promise to block direct Unity input, native game actions or third-party Harmony code.

## Ordinary mod choices

An ordinary mod with a configured action hotkey should prefer one bounded, owner-bound typed registration and `KeybindPressed`/`KeybindReleased`:

```csharp
using IInputRegistration increase = helper.Input.RegisterKeybind(
    "zoom.increase",
    DtmKeybindList.Parse(config.IncreaseKey),
    DtmInputScope.Gameplay);

helper.Events.Input.KeybindPressed += (_, e) =>
{
    if (e.KeybindId == "zoom.increase")
        StepZoom(1);
};
```

This removes a product-owned `UpdateTicked` callback and guarantees the first short edge. Keep the registration set fixed and tightly bounded, update it in place when config changes, and dispose/unsubscribe it when the feature is disabled if no toggle action must remain armed. Core still samples one shared bounded active-button union rather than polling once per registration.

Local snapshot queries remain valid inside an already-active session updater or for transient state where the one-frame arming delay is acceptable:

```csharp
if (manualCancelKeybind.JustPressed(helper.Input))
    CancelSession();
```

The local watch state is bounded, owner-cleaned, generation-expiring, and exists only to ensure the shared sampler knows which physical buttons a current consumer may query. Do not add an otherwise-unneeded frame updater just to evaluate local queries, and do not register large/transient key sets merely for convenience.

## Reliable-first-edge exception

An owner-bound registration is allowed when all of these are true:

- the product is continuously installed/present in the relevant scope;
- the first very short press after inactivity must be delivered reliably;
- one missed edge would make the product feel broken;
- the registration set is fixed and tightly bounded;
- scope and owner cleanup are explicit.

AutoFishing owns exactly one continuously armed `Gameplay` toggle registration because the disabled product must still receive the first short F6 edge. Its automation `UpdateTicked` subscription is separate and exists only while the automation session is active. Config changes update the toggle registration in place; title scope does not poll it; owner cleanup disposes it.

Zoom owns two `Gameplay` registrations for its fixed increase/decrease actions only while Zoom is enabled. The 2026-07-18 Batch 5 completion audit explicitly admitted this change because its former local-query model required a permanent product `UpdateTicked` callback. Config changes update the registrations in place; disabling Zoom disposes the registrations and its `KeybindPressed` subscription.

AutoFishing manual movement fallback remains a local snapshot query and must not register permanent A/D/Space/Shift roots. Native `HorizontalMoveFactor` remains the preferred cancellation signal.

## Boundary gates

- AutoFishing keeps exactly one owner-bound `RegisterKeybind` call and does not migrate to `JustPressed(helper.Input)` without a new short-edge design review.
- Zoom keeps exactly two dynamically active typed registrations and no product `UpdateTicked` input poll. Disabled Zoom must release both registrations and the keybind event subscription.
- Ordinary configured action hotkeys may use a small typed registration set; active-session/transient state may use local `DtmKeybindList` queries. Neither choice authorizes an otherwise-unneeded frame callback.
- DTMAPI Core owns sampling, scopes, owner cleanup, and frame state only; it contains no AutoFishing/Fishing product policy.
- A registration is not an excuse for per-registration native polling. The sampler still reads one shared bounded frame.

This contract adopts SMAPI's useful semantic shape—one shared input frame and local keybind objects—without copying Stardew-specific input implementation details.
