# AnimalHusbandryProgress Viewer Rendering

Status: `source/unit/package and migrated-DLL third-save runtime-verified`

## Native Boundary

- Game build: `23762374_public_C416D4`.
- `DolocTown.UI.AnimalFullInfoData::.ctor(DolocTown.Animal)`, token `0x06004C94`: ProductNative Postfix derives read-only husbandry rows.
- `DolocTown.UI.AnimalViewer::Show(DolocTown.UI.AnimalFullInfoData)`, token `0x06004CA5`: ProductNative Prefix clears stale clones and Postfix renders current clones.
- `DolocTown.AnimalPanelUiState::Unregister()`, token `0x060042B3`: ProductNative Postfix releases clones, rows and caches at the real panel-close boundary.
- `AnimalPanel.RefreshViewer` remains deliberately unpatched; screenshot/evidence timing belongs to optional QA.

Signature authority is `references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/methods.csv`.

## Owner And Lifecycle

- Current owner: `dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress`, four atomic patches across three targets.
- The product owns husbandry threshold reads, cached derived rows, cached cloned native mood-progress targets, the one-shot next-frame guard, colors and exact session cleanup. It does not mutate animals, breeding, AI, membership or save data.
- Output-item titles use owner-bound `IItemDisplayNameApi`; GameBridge owns only the shared read-only `DolocAPI.QueryItemProto(itemId).Title` adapter and no Animal Hook/state.
- Save load, title return, native unregister, environment reset and product deactivation clear product rows, clones and caches. Exact-owner unpatch is attempted independently even if state cleanup throws.
- Final Loader deactivation must observe one instance, four actual patches/three targets and one callback before cleanup, then zero instance/patch/target/callback/Core roots.

## Compatibility

The unchanged `IAnimalViewerApi` executor is frozen under `GameBridge/Compatibility/AnimalViewer`, demand-inactive for old consumers only. Product-first requests fail before retaining demand. Pending compatibility-first demand is reconciled before the next deferred GameBridge install. A physically installed compatibility owner makes the product fail closed until restart.

## QA And Evidence

- Optional QA reflects into the loaded product callback; it does not read Compatibility service state or treat internal counters as a real Harmony owner.
- A staged G4 acceptance requires at least one derived row, `state=visible`, a positive overlay-row count and bounded screenshot evidence before it may pass.
- Focused source, GameBridge Unit, QA Unit, SDK, Doctor, Catalog and package checks pass.
- Historical `GAME-SMOKE/20260718-000232` and `005546` are pre-migration GameBridge behavior baselines, not proof of the Advanced DLL.
- `GAME-SMOKE/20260722-161022` is the pre-correction shared-adapter baseline. `GAME-SMOKE/20260722-180502` verifies the current four-patch/three-target DLL, 20 constructed animals, one visible localized read-only row and native-style clone, `mutation=false`, bounded screenshot, native close with rows/clones zero, and real Loader deactivation to zero instance/patch/callback/Core roots. Restoration and clean exit passed. Focused Units later close the shared adapter's EnvironmentReset fanout, callback gate, Hook-ready caching and Camera-disabled ownership. `GAME-SMOKE/20260723-115821` additionally verifies the product-local refresh optimization across three sequential animal selections: each selection observes at least one newer render receipt and completes one next-frame guard; each `RenderAfterShow` performs its initial hidden write and rearms at most one guard, with final receipt sequence 6. Panel close/title return leaves native data, overlays and rows at zero and exact-owner deactivation moves `4 patches / 3 targets / 1 callback` to zero. `153320`/`153845` remain Steam cloud-conflict infrastructure evidence only.

## Relations

- Admission Review: `docs/reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md`.
- Shared-boundary Review: `docs/reviews/code/2026/20260722-0008-fish-animal-shared-boundary-review.md`.
- Update: `docs/updates/2026/20260722-0003-animalhusbandryprogress-fifth-advanced-product.md`.

## Rollback

Remove the Animal Advanced policy/product/Catalog admission and restore the former Strict shell plus frozen executor route as one unit. Do not move ProductNative rows or Hooks back into mandatory Runtime as a partial rollback.
