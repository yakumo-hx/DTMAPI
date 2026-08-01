# ActionSpeed Third Product Admission Review

**Review ID:** `20260722-0002`
**Date:** 2026-07-22
**Status:** accepted — exactly one third real Advanced product
**Scope:** ActionSpeed admission only; no fourth product, general Advanced authoring lane, Content Host, or `0.5.5` release

## Decision

Admit `Yuuka.DTMAPI.ActionSpeed` as the sole third real managed Advanced CodeMod. The migration may move its native execution out of mandatory GameBridge, keep the old experimental ABI as frozen compatibility, and use the existing Catalog-driven Advanced builder, SDK policy registry, package receipt, Doctor/Manager, and zero-leftover checks.

The admission does not promote any ActionSpeed implementation to SharedNative. ActionSpeed still has one real consumer, and its action timing, animator acceleration, restoration markers, continuous-use policy, animal interaction marker, and configuration are ProductNative.

## Bound Identity

| Field | Value |
| --- | --- |
| Unique ID | `Yuuka.DTMAPI.ActionSpeed` |
| Workshop item | `3742763309` |
| Product version | `1.3.4-dtmapi` |
| Game build | `23762374` |
| Advanced policy | `doloctown-23762374-actionspeed-v1` |
| Product Harmony owner | `dtmapi.mod.yuuka.dtmapi.actionspeed` |
| Compatibility owner | `dtmapi.gamebridge.doloctown` |
| Native owner | `AgentControllerState` interaction timers plus the reviewed tool, plant, animal, electric, fuel, water, bottle, feed, and resin action owners |

## Why This Product Is Next

1. The July 12 product roadmap orders ActionSpeed after OneActionComplete.
2. The Batch 6 ownership baseline already classified ActionSpeed as ProductNative and named its target managed Advanced product.
3. AutoFishing and OneActionComplete now prove the single-product lifecycle, SDK/package identity, compatibility ABI, atomic install, fail-closed collision, zero-leftover, and short game-acceptance template.
4. The generic Catalog-driven Advanced builder and validator already exist, so this admission does not justify a third product-specific implementation family.
5. The pre-migration ActionSpeed baseline includes the third-save Tool, Interact, Eat, and ContinuousUse behavior families and the independent 24-child L0–L5 GC ladder. Those receipts remain historical baseline evidence, not proof for the migrated DLL.

## Native and Hook Boundary

The product owns one atomic nine-target Hook set:

1. `AgentStateTool.OnEnter` postfix;
2. `AgentStateTool.OnExit` postfix;
3. `AgentStateInteract.OnEnter` postfix;
4. `AgentStateInteract.OnExit` postfix;
5. `AgentStateEat.OnEnter` postfix;
6. `AgentControllerState.UseItemContinues(float)` prefix;
7. `AgentControllerState.InteractContinues(float)` prefix;
8. `AnimalRenderer.OnInteract` prefix;
9. `AgentStateBase.OnExit` postfix.

All nine targets must resolve before the first patch is installed. Partial installation is forbidden. Entry failure, disable, title return, save transition, or unload must restore product-held timers, animator speeds, animal markers, and Harmony patches.

The `AgentStateInteract.OnExit` method is also used by OneActionComplete, but the two products do not share state or a write invariant. Independent exact Harmony owners remain the correct boundary; a common method alone is not evidence for SharedNative promotion.

## Dual-Owner Compatibility Rule

Both load orders are fail-closed:

- If the product owner is already installed, the frozen GameBridge compatibility API refuses ActionSpeed demand before installing compatibility hooks.
- If an old Strict compatibility consumer requested ActionSpeed first and the product loads later, GameBridge reconciles the pending demand at the next install boundary and removes ActionSpeed compatibility demand while preserving unrelated GameBridge demand.
- If ActionSpeed compatibility hooks are already physically installed in the current process, product entry refuses duplicate ownership and requires a clean restart. It never layers a second ActionSpeed implementation over the old owner.

The new Advanced product must not consume `IActionSpeedApi`. That API remains exact-ABI frozen solely for retained old Strict binaries during the `0.5.5` window.

## Admission Gates

The migration must pass:

- exact SDK-generated manifest and receipt validation;
- Author SDK-only game/native references and `netstandard2.0` output;
- Catalog/package/hash/Doctor/Manager checks;
- Catalog-driven live zero-leftover checks with only the explicit compatibility boundary allowed in mandatory Runtime;
- focused source/unit/QA checks for nine-target atomicity, both owner orders, lifecycle restoration, and configuration reuse;
- one bounded third-save game acceptance covering Tool, Interact, Eat, ContinuousUse, disable, title return, reload, configuration menu, and clean exit.

The migration does not rerun the complete Release suite, the historical L0–L5 ladder, or a new long GC soak. Those remain separate release-boundary work.

## Result

**PASS.** ActionSpeed is admitted as the one and only third real Advanced product. The next product remains blocked until this migration is frozen and separately reviewed.
