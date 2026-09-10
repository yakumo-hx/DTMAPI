# 20260710-0003 First-Party AutoFishing Cutover

## Summary

Move the AutoFishing product state machine to the first-party mod and leave `IFishingAutomationApi` as a compatibility adapter over the internal primitives boundary.

## Scope

- Own configurable F6/`None`, enable/disable, native movement cancel, 0.25-second recast timing, option policy, input decisions, and sequence de-duplication in AutoFishingMod.
- Keep InstantBite, FastAnimations, and SkipMiniGame independent.
- Use native `HorizontalMoveFactor` at threshold `0.001`, with one-time legacy snapshot fallback only when unavailable.
- Apply FastAnimations as hook velocity `×m`, gravity `×m²`, Pull duration `÷m`, and Pull animator `×m`; never change Ready/charge.
- Ignore old first-party `CastChargeRatio`; retain it only in the compatibility DTO/private Ready path.
- Release all sessions and leases on F6 close, title/save boundary, owner cleanup, or exception. Never call Harmony `UnpatchSelf` from product disable.

## Acceptance

- Unit tests cover state flow, stale sequence rejection, three-option matrix, multipliers 1/2/3/4, minigame Hold/Release/TapBonus, skip without input lease, and every release boundary.
- Fifth-save runtime smoke passes DefaultLoop, independent FastAnimations, independent InstantBite, independent SkipMiniGame, report export, close cleanup, and process exit.
- Current custom-key/`None` and physical native movement-cancel runtime checks remain explicit gates if not rerun.

## Outcome

Implemented with default `FirstParty` execution. Fifth-save evidence passed for DefaultLoop/short soak (`GAME-SMOKE/20260710-102651`), independent FastAnimations (`20260710-103205`), independent InstantBite (`20260710-103350`), and independent SkipMiniGame (`20260710-105201`). Current-build physical movement cancel and a fresh custom-key/`None` run were not completed; historical input evidence remains available but is not promoted to current-build proof.

## Rollback

Switch product execution back to the compatibility controller while keeping the primitives implementation dormant. Do not remove the compatibility API, split Harmony IDs, or add runtime unpatching as part of rollback.
