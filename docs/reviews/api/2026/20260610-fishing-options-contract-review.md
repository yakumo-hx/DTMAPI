# Fishing Options Contract Review - 2026-06-10

## Scope

Public symbols: `IFishingAutomationApi`, `FishingAutomationOptions`, `FishingAutomationState`.

Current matrix status: `Experimental`.

Recommended status after this review: keep `Experimental`. This review is docs-only and does not change runtime behavior, public members, hook IDs, smoke fields, migrated AutoFishing config UI, game files, official DLLs, Workshop content, or reverse/decompiled reference material.

## Files Read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/AutoFishingMod/README.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`

## Current Contract Shape

`FishingAutomationOptions` exposes:

- `AutoRecast`
- `RequireSelectedFishingRod`
- `InstantBite`
- `AutoCompleteMiniGame`
- `SkipMiniGame`
- `StopOnManualMove`
- `FastAnimations`
- `FastAnimationMultiplier`
- `CastReleaseProgress`
- `RecastDelaySeconds`
- `VerboseLogging`

The migrated `AutoFishingMod` reads config values into these options, but the GameBridge currently normalizes two fields:

```text
NormalizeFishingAutomationOptions:
  AutoRecast = true
  RequireSelectedFishingRod = true
```

That means a caller can pass `AutoRecast=false` or `RequireSelectedFishingRod=false`, but the active runtime policy still behaves as auto-recast enabled and selected-rod-required.

## Findings

1. `AutoRecast` is public-shaped but not caller-controlled today.

   The current service forces `AutoRecast=true` during normalization. The migrated mod also resets config to `true`, so current smoke evidence proves the migrated route rather than a general caller-controlled recast policy.

2. `RequireSelectedFishingRod` is public-shaped but not caller-controlled today.

   The current service forces `RequireSelectedFishingRod=true`. This keeps the automation conservative: it only casts with the selected rod and avoids scanning backpack rods during ordinary runtime, even though the resolver has a non-selected-rod path for a future policy.

3. The forced values are safer than silently honoring them without coverage.

   Respecting `AutoRecast=false` or `RequireSelectedFishingRod=false` would broaden behavior into manual-recast/no-recast and backpack-rod selection paths. Those paths need separate smoke/manual evidence for selected item changes, wrong selected item, missing rod, inventory rod discovery, title/save reload, and player-facing feedback.

4. The current DTO should remain Experimental.

   The field names exist in public abstractions, but their semantics are not stable. The current contract is best documented as "accepted by the DTO, normalized by the experimental FishingAutomation service."

## Decision

- Do not change runtime behavior in this branch.
- Do not add, remove, or rename public API members.
- Do not mark any member obsolete in code yet; that would be a public-surface decision and should come with a migration note.
- Keep `IFishingAutomationApi`, `FishingAutomationOptions`, and `FishingAutomationState` Experimental.
- Document `AutoRecast` and `RequireSelectedFishingRod` as semantically unstable fields whose caller-provided values are currently normalized to safe defaults.

## Future API Choices

Future work should pick one of these directions before promoting the API:

1. Respect the fields.

   This requires new smoke/manual evidence for disabled recast, non-selected rod lookup, inventory order, selected-slot restoration, no-rod/no-water feedback, save/title reload, and multiple owner policy.

2. Deprecate the fields.

   If the intended product policy is always auto-recast with selected rod only, keep runtime behavior as-is and mark the fields as experimental legacy surface in a later compatibility note.

3. Replace them with explicit policy names.

   A future experimental revision could use names that match the real behavior, such as selected-slot-only casting and migrated-mod auto-cast policy, but that would be a public contract revision and needs a dedicated compatibility branch.

## Evidence

Retained runtime evidence:

- `GAME-SMOKE/20260610-170839`: FishingAutomation feature split smoke.
- `GAME-SMOKE/20260610-202047`: service failure throttle smoke.
- `GAME-SMOKE/20260610-203301`: lifecycle runtime reset smoke.
- `GAME-SMOKE/20260610-204134`: native helper dependency cleanup smoke.

These smokes prove the migrated AutoFishing route with the current normalized policy. They do not prove caller-controlled `AutoRecast=false` or `RequireSelectedFishingRod=false` semantics.

## Follow-Up

- Keep public API matrix wording explicit that these two fields are not stable caller-controlled behavior.
- If a future branch changes normalization, add dedicated tests and third-save smokes before updating the API matrix.
