# ActionSpeed Owner Deactivation Platform Root Review

**Review ID:** `20260722-0005`
**Date:** 2026-07-22
**Status:** recorded — resolved by the corrected `GAME-SMOKE/20260722-141220` acceptance
**Scope:** ActionSpeed and OneActionComplete managed-product owner deactivation; no fourth-product admission, Release, L0-L5, long test, or 0.5.5 publication

## Observed Result

`GAME-SMOKE/20260722-140426` loaded the corrected ActionSpeed Advanced package from the Author SDK-managed destination and observed exactly nine `dtmapi.mod.yuuka.dtmapi.actionspeed` Harmony patches. `ActionSpeedTool`, `ActionSpeedConfigApply`, `ActionSpeedInteraction`, `OneActionResourceHit`, and `TitleButtonLifecycle` passed. Save, source, profile, configuration, process-exit, and deployment restoration gates also passed.

The run failed only when the real Manager deactivated ActionSpeed. Core logged `Mod instance Dispose failed` with the owner-lifecycle guard stating that an inactive owner cannot mutate registrations. The immediately following owner cleanup nevertheless proved `patches=0`, `targets=0`, and `callback=False`; the retained root was the failed product lifecycle instance rather than a native Hook or callback leak.

## Root Cause

Core deliberately calls `BeginDeactivation` before invoking a managed product's `IDisposable.Dispose()`. At that point owner-bound event proxies reject product-level `-=` mutations. ActionSpeed `Dispose()` called `UnsubscribeEvents`, so the first event removal threw after the owner had entered `Deactivating`. Its independent cleanup steps still disposed the input registration and removed the exact Harmony owner, but the aggregate exception caused Core to retain the lifecycle instance.

OneActionComplete has the same normal-disposal structure and therefore carries the same latent failure even though the failed run stopped before its final owner-deactivation assertion.

## Ownership Decision

- During normal Manager deactivation, Core owns removal of platform registrations after product disposal. Product code must not mutate owner-bound event proxies once the lifecycle is `Deactivating`.
- The product still owns local subscription flags, its disposable input registration, ProductNative state restoration, callback detachment, and exact-owner Harmony unpatch.
- During `Entry` failure the owner is still entering, so explicit event unsubscription remains part of product rollback.

## Rejected Hypotheses

- This is not an ActionSpeed behavior or nine-Hook installation failure; all configured product cases passed before shutdown.
- This is not a Harmony owner leak; the failed run observed zero remaining patches and targets and a detached callback.
- Suppressing the aggregate in Core would hide a real lifecycle contract violation and leave the product instance retry semantics ambiguous.

## Required Correction And Acceptance

1. In ActionSpeed and OneActionComplete normal `Dispose()`, clear product-local subscription flags without calling event `-=` proxies, dispose the input registration, and always run ProductNative deactivation.
2. Preserve explicit event unsubscription in `Entry` rollback.
3. Add focused source checks that reject `UnsubscribeEvents` inside the normal `Dispose()` body while requiring every local flag reset and native deactivation.
4. Rebuild and deploy both SDK packages, then rerun the same bounded third-save acceptance. Acceptance requires both real Manager owner deactivations to report no Dispose failure, no lifecycle instance root, no Harmony target/patch/callback residue, and clean process/deployment restoration.

## Resolution Link

Implementation and validation facts remain owned by [ActionSpeed Third Advanced Product](../../../updates/2026/20260722-0001-actionspeed-third-advanced-product.md). `GAME-SMOKE/20260722-141220` passed both real Loader owner deactivations with zero instance, callback, Harmony target/patch and Core-root residue and also closed OneActionComplete's partial-energy and configuration reload gaps.
