# Title Idle Input Pressure Debug Protocol

Use this protocol when a long-running title/main-menu idle route, followed by save entry, reaches Unity/Mono native failure such as `Fatal error in GC / Unexpected mark stack overflow`.

Current ISSUE-010 status: the title/main-menu input-pressure path is mitigated and has passed the FullKnown validation route. The broader "arbitrary long gameplay/native GC crash" class remains open. Do not use this protocol to claim all long-term gameplay GC failures are solved.

## Source Records

- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Initial SaveLoad/root review: `docs/reviews/code/2026/20260705-0001-saveload-lifecycle-root-retention-review.md`
- Synthetic input pressure reproducer: `docs/updates/2026/20260707-0003-input-polling-virtual-pressure.md`
- Hotkey rebuild: `docs/updates/2026/20260707-0004-hotkey-rebuild.md`
- Edge sampling and final input follow-up: `docs/updates/2026/20260707-0007-hotkey-edge-followup.md`
- FullKnown title-idle validation: `docs/updates/2026/20260708-0002-fullknown-long-title-cycle-validation.md`

## Problem Shape

Name the problem as a window, condition, and failure point:

- Window: title/main-menu continuous idle, then native save entry.
- Condition: larger stable root set plus per-frame input polling pressure.
- Failure point: native `DolocAPI.LoadGame` / terrain / dungeon activation window, not a managed C# exception.

Do not reduce this to "memory leak" without proving what grows. For this class, the important distinction was:

- Static roots: code, config, owner records, event handlers, input button registrations.
- Active roots: objects or registrations that also cause work every idle frame.

The old input model was dangerous because registered string buttons were not only roots; they were also a per-frame polling workload.

## Known Final Classification

The old registered-string input polling path was not proven to be the only possible native GC root cause, but it was proven to be a sufficient pressure amplifier for the title-idle route.

Old behavior:

- Owners registered string buttons.
- Bootstrap polled the registered button union during ordinary idle/update.
- The title page repeatedly called backend key checks such as `GetKeyDown` and `GetKey` for every registered button.
- YConsole, Zoom, AutoFishing, and synthetic virtual keys shared this pressure surface.

New behavior:

- Input is owner-bound and typed through `DtmKeybind`, `DtmKeybindList`, and scope-aware sampling.
- Bootstrap samples only the buttons needed by the current scope.
- One frame produces `InputButtonSample(IsDownNow, PressedEdge, ReleasedEdge)`.
- Core dispatches typed `KeybindPressed` / `KeybindReleased`.
- Ordinary title idle no longer uses the old registered-string polling path.

## Triage Checklist

1. Confirm the exact route.
   - Is it continuous title idle, interrupted title idle, active gameplay, or save/load cycling without idle?
   - Record exact idle length, cycle count, launch mode, enabled profile, save slot, and fatal window.

2. Run negative controls before deep fixes.
   - No-idle save-load cycle.
   - Interrupted-idle cadence.
   - Core-only or reduced profile.
   - Suspect owner removed.
   - Suspect pair split.

3. Separate owner count from owner activity.
   - Count roots by owner: event handlers, config pages, input buttons, code mods, UI roots.
   - For every root type, ask whether it does per-frame work during the failing window.

4. Split by root type before splitting endlessly by mod.
   - Keep code/event/config roots.
   - Remove only input roots.
   - If the route passes, input is a pressure island even when the owning mods remain loaded.

5. Add counters for the active path.
   - Registered button max.
   - Title buttons polled.
   - `GetKeyDown` calls.
   - `GetKey` calls.
   - Legacy registered-string path flag.
   - Input backend path used by each scope.

6. Build a pressure amplifier.
   - Use synthetic virtual keys or another controlled multiplier when the suspected path scales by count.
   - The goal is to compress a one-hour route into a shorter, more informative reproduction.

7. Keep native-window evidence.
   - Fatal window.
   - Native LoadGame enter/return breadcrumbs.
   - SaveLoaded breadcrumbs.
   - Fresh crash dump or fatal popup evidence when available.

## Fix Principles

Fix the mechanism, not just one consumer.

Preferred fix:

- Move from registered-string global polling to owner-bound typed keybinds.
- Scope sampling to the current gameplay/UI/title context.
- Cache one input frame and dispatch typed edges from it.
- Preserve native-feeling short taps, held keys, and same-frame press/release.

Avoid:

- Disabling YConsole, Zoom, or AutoFishing as the "fix".
- Removing only one owner's keybinds while leaving the global polling model intact.
- Treating fewer registered keys as solved if the old per-frame path still exists.
- Claiming broad gameplay GC solved from a title-idle-only pass.

## Validation Ladder

A title-idle input-pressure fix needs three validation layers:

1. Pressure route.
   - Synthetic high-key-count route should pass.
   - Input diagnostics should show `legacyRegisteredStringPath=false`, no title-idle polling, and bounded registered button count.

2. Original failure route.
   - The original owner combination, such as YConsole + Zoom, should pass under the same long-idle shape that previously reproduced.

3. Full near-real route.
   - FullKnown or equivalent broad enabled profile should pass title idle plus repeated save entry.
   - Keep fatal-window, input diagnostics, and save/load breadcrumbs enabled.

Passing these layers supports "main-menu/title-idle input-pressure mitigated." It does not close long-term gameplay/native GC unless the gameplay route itself is covered.

## Common Wrong Turns

Lifecycle review is useful, but it is not always the determinant. It can prove that listeners, Harmony patches, UI binders, SaveLoad requests, and dynamic roots are bounded. It cannot by itself expose a per-frame pressure amplifier unless the review asks "what runs every frame?"

Native continuation probes and dumps are useful, but they should not delay active-path counters once per-owner deltas point at an active root type. If `InputButton` counts stand out, add input polling counters early.

Mod-owner bisection is less efficient than root-type bisection once a suspect owner pair is found. The breakthrough pattern is "keep the mods loaded, remove only one root type."

## Reusable Heuristic

When a large stable root set correlates with a native GC failure, ask four questions in order:

1. Which boundary fails?
2. Which condition makes it fail?
3. Which roots are stable versus actively worked?
4. Can the active work be multiplied synthetically?

For ISSUE-010's title-idle branch, this led from generic lifecycle suspicion to input-root pressure, then to a reproducible 50-key amplifier, then to the typed owner-bound input rebuild.

