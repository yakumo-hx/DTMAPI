# ActionSpeed Manual Watering Tool Speed

## Metadata

- Update ID: `20260806-0002`
- Date: `2026-08-06`
- Lifecycle Status: `implemented`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Area: actionspeed/productnative/watering/tool-animation/hooks

## Source Request

The user reported that manually watering with a watering can is not animation-accelerated and explicitly classified it under the existing tool-speed setting. They requested a code-level implementation review followed by the bounded fix.

## Owning Review

- [ActionSpeed Manual Watering Animation Review](../../archive/reviews/manual-qa/2026/20260806-0002-actionspeed-manual-watering-animation.md)

## Scope

- Cover current native `AgentStateWater` enter/exit with the ActionSpeed product's exact Harmony owner.
- Reuse `ToolSpeedEnabled` and `ToolMultiplier`; add no configuration or public API.
- Reuse the existing Animator snapshot/restore path and preserve the animal-interaction marker across the water-state exit transition.
- Synchronize the optional QA inventory, focused source contract, Hook map and player-facing tool tooltip.

This Update does not change frozen `IActionSpeedApi` compatibility behavior, GameBridge ownership, save data, product version, Workshop identity, Runtime loader behavior, or publication state.

## Changed Files

- `products/first-party/ActionSpeed/src/Native/ActionSpeedHookInstaller.cs` and `ActionSpeedCallbacks.cs`: add exact-owner `AgentStateWater.OnEnter/OnExit` Postfixes and expand the atomic inventory from nine to eleven targets.
- `products/first-party/ActionSpeed/src/Native/ActionSpeedEngine.cs`: recognize `waterCan`/`ItemWaterCan`, reuse the existing tool Animator acceleration path, and preserve a pending animal-interaction marker across water-state exit.
- `products/first-party/ActionSpeed/src/ModEntry.cs`, `i18n/*.json` and `README.md`: describe manual watering as part of the existing tool-speed option and current Hook inventory.
- `products/first-party/ActionSpeed/qa/batch6/Batch6ActionSpeedReflectionObserver.cs` and the two GameBridge QA fixtures: project the eleven-target physical-owner contract.
- `tools/scripts/test-batch6-actionspeed-advanced-product.ps1`: require both water-state targets, their engine classification/restoration tokens and the two additional native authorities.
- The owning Review, focused Hook map, Batch 6 identity contract, monthly ledger and 0.6.0 route: record the native boundary, validation state and candidate invalidation.

## Validation

- `tools/scripts/test-batch6-actionspeed-advanced-product.ps1`: PASS (`source-files=6`, `hooks=11`, `policies=1`, `native-authorities=9`).
- Exact Advanced reference fixture build for `doloctown-23762374-actionspeed-v1`: PASS.
- `tools/scripts/build-batch6-actionspeed-advanced-pilot.ps1`: PASS with zero product compilation warnings/errors; temporary product package SHA-256 `019C62358BBE12A3F6BC3B0649220406F2E4FD967DF290CF83560C19CAD8382E` and Author SDK ZIP SHA-256 `8CA3E7A350A8A0C5E9061C26C8B664F8C3239B3BAF9F346D3F12D7CB60766559`.
- Focused repository-local Release builds: `DTMAPI.GameBridge.DolocTown.QA` PASS (`0` warnings / `0` errors), `DTMAPI.QaUnitTests` PASS (`0` warnings / `0` errors), and `DTMAPI.UnitTests` PASS (`0` errors; the existing ten DebugConsole nullable warnings remain).
- Direct test executables: `DTMAPI.QaUnitTests: OK` and `DTMAPI.UnitTests: OK`.
- `tools/scripts/test-batch6-phase0-contract.ps1`: PASS with the historical nine-Hook admission receipt unchanged; the current eleven-Hook correction is owned by this Update and the live Hook map rather than rewriting historical evidence.
- `tools/scripts/check-product-catalog.ps1`: PASS (`products=27`, `public=11`, `workshop-items=22`, `api-rows=48`).
- `tools/scripts/check-doc-governance.ps1`: PASS (`6338` checks) after the final status and 0.6.0 route projection.
- A broad `tools/scripts/build.ps1 -Configuration Release` attempt exceeded the command wrapper's time limit and produced no authoritative result; its orphaned repository-local build nodes were identified and stopped before the focused builds above. It is neither recorded as PASS nor as a product failure.
- No game process was launched, no Runtime lock was acquired, and no install, official upload directory, profile or save bytes were changed. Runtime validation remains `not-run`.

## Official Upload Preparation

- After commit `af4f6bb54a8e`, the exact `doloctown-23762374-actionspeed-v1` reference fixture and Catalog-driven Author SDK builder reproduced package SHA-256 `019C62358BBE12A3F6BC3B0649220406F2E4FD967DF290CF83560C19CAD8382E`; product compilation remained `0` warnings / `0` errors and the entry DLL SHA-256 is `4021D2AB341D6357369328B85631D12D318278CBBCBCEBA1A82D060F9C0C479F`.
- With `DolocTown.exe` absent and the shared Runtime lock held, only `MODS/Yuuka_DTMAPI_ActionSpeed` was exchanged through same-volume `Directory.Move`. The prior upload tree is recoverable at `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/.dtmapi-actionspeed-upload-af4f6bb54a8e-20260806-154641984/before/Yuuka_DTMAPI_ActionSpeed`.
- The old tree was `10` files / `368,423` bytes / tree SHA-256 `3B29819589A3180DDD63E3E09078EE2976057673AE1BD21B65BAE4BEDBECCA3F`; the independently re-read live tree is `10` files / `369,405` bytes / tree SHA-256 `772DABCFCAA6C5174CB38E4096C63F1B567E3CD621F33D38FFD9614478F22824`.
- The original `workshop.json` SHA-256 `857D11BCD53C7118B68A20F9B74649D6E1F92C1B1C6DDB5F3E0A8F30493246EF` was preserved. `Local.Yuuka_DTMAPI_ActionSpeed` remains enabled at priority `5`; `mod_infos.json` stayed at SHA-256 `B169207CA885CC08D7E2F1677396B31B213E66B642DA119F251AB975B12F2F3C` with unchanged mtime.
- Complete pre/post snapshots proved the entire `SAVE` tree byte/mtime unchanged and every other official `MODS` root byte-identical. Independent postflight revalidated `1.0.0`, minimum DTMAPI `0.5.5`, `netstandard2.0`, the exact policy/Harmony owner, both watering tooltip projections, no game process and a free Runtime lock.
- This is upload preparation, not a game run or Steam publication. Runtime validation remains `not-run` pending the user's cold-start manual watering check.

## Rollback

Remove the two `AgentStateWater` product patches and their callback/QA/source-contract projections together. The existing nine-target ActionSpeed behavior remains the rollback baseline.

## Follow-up

The official upload directory now contains the rebuilt ActionSpeed package. Run one current-build manual-watering acceptance with tool speed enabled and disabled plus exact-owner cleanup before Steam submission. The prior `566467f0` ActionSpeed candidate bytes remain historical and do not contain this correction. Do not claim player/runtime verification from package/deployment checks alone.
