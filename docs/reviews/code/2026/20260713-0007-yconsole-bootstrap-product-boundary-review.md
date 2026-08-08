# 20260713-0007 Y-Console Bootstrap Product Boundary Review

Status: recorded / M1 and N1 selected / implementation open
Date: 2026-07-13
Scope: published Y-console identity, Bootstrap UI ownership, Diagnostic APIs, optional UI host, compatibility, and rewrite sequencing
Related decision docket: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0003-third-round-closure-fourth-decision-docket.md`

## Source Request

The user wants the Y-key console to receive an overall UI rewrite and feature improvements. The major-update boundary audit also found that Bootstrap is dominated by product/framework UIs. The fourth round must decide whether Y console remains an optional product or becomes permanent Runtime behavior, and whether ownership extraction is separated from visual redesign.

This is a read-only source/product review. It does not change the console, hotkey, provider, public API, Bootstrap, package, or Workshop product.

## Current Product And Runtime Split

Y console is already an independently distributed published Diagnostic product:

```text
UniqueID: DTMAPI.DebugConsoleMod
Workshop: 3742714442
```

The ordinary CodeMod owns Y/Escape input, save/title gating, language/config, and acquisition of `IDebugConsoleApi` from provider `DTMAPI.DebugConsoleHost`.

The actual product UI is `ReflectedDebugConsoleUi.cs`, 2,432 lines inside `DTMAPI.BepInExBootstrap`. Bootstrap constructs it and registers the provider at startup, then invokes its `Update()` on every Bootstrap frame. With no owner it can fast-return, but its object/provider/product branches remain resident.

Native inventory, weather, teleport, time, movement, save, and advanced debug operations live behind GameBridge APIs classified `Diagnostic`. They are not stable ordinary gameplay APIs.

Current architecture therefore makes a disabled/unsubscribed optional Workshop product keep most of its UI implementation inside every player's base Runtime. This conflicts with lightweight ownership and with the product's enable/disable meaning.

## Desired Boundary

| Layer | Responsibility |
| --- | --- |
| Y product Mod | identity, hotkey, pages/tabs/filtering/paging, localization, view model, user interaction policy |
| optional first-party Diagnostic UI host | reflected Unity Canvas/EventSystem/focus/modal implementation, loaded only with product/legacy demand and delivered outside base Bootstrap |
| GameBridge | allowlisted native debug operations and input isolation only while a modal lease is active |
| Bootstrap | platform startup/frame/lifecycle handoff; no Y-product UI object/provider/update in the normal no-product path |

Moving 2,432 reflected Unity lines verbatim into an ordinary Mod is not the goal. A reviewed optional first-party host/companion boundary may remain product-specific without exposing raw Unity/game types or turning the current Y UI into a general public UI API.

A future SMAPI-like command helper is a separate text-command platform project. It must not be claimed as complete by stabilizing the current reflected Y-console UI or its Diagnostic gameplay operations.

## Compatibility

Existing Workshop binaries consume `IDebugConsoleApi` and `DTMAPI.DebugConsoleHost`. Under I1, DTMAPI 0.5.5 retains that ABI/provider and current behavior. The compatibility implementation should be lazy: construct the old host only for a legacy consumer, warn once, and keep the base no-consumer path free of product UI/update work.

New Y console 1.0.0 declares its truthful minimum when it moves to the optional product host. Physical provider/API retirement occurs only after its warning/breaking window.

## M - Product Identity

### M1 - optional published Diagnostic product

Keep Y console as `PublishedProduct / ProductType=Diagnostic`. The normal DTMAPI player Runtime does not treat it as a built-in feature; it loads the product UI host only on demand and keeps a lazy legacy compatibility island.

Selected. This matches existing Workshop identity, makes disable/unsubscribe meaningful, and permanently rejects making Y console a base-Runtime product.

### M2 - permanent built-in DTMAPI console

Move product identity back into the base Runtime while the Workshop item becomes a thin switch or is retired.

This makes every player carry high-risk debug UI/operations, conflicts with lightweight goals, and creates a migration/sunset problem for an already published product.

## N - Rewrite Sequence

### N1 - extract equivalently, then redesign

First move ownership out of Bootstrap and establish on-demand host/legacy compatibility with behavior-equivalent hotkey, focus, same-key close, right-click item actions, modal input isolation, EventSystem, save/title cleanup, and disable/restart behavior. Only after that gate passes, rewrite layout, navigation, search/history, commands, and other UX in a separate Update.

Selected. It keeps ownership and UI regressions attributable; extraction and full UI rewrite may not share one Update.

### N2 - extraction and full rewrite together

Move and redesign all behavior in one migration.

This removes an intermediate host but makes every known input/focus/lifecycle path change simultaneously and weakens rollback diagnosis.

### N3 - keep the product UI in Bootstrap and only redesign it

This preserves packaging but leaves the central ownership problem and no-product resident path intact.

## Acceptance Boundary

M1/N1 do not authorize API deletion or a new public UI framework. Implementation requires an optional product-host/package design, no-product zero-work source/unit gate, old provider/DLL compatibility, behavior-equivalent runtime/manual regression, and only then a separate UI redesign record.
