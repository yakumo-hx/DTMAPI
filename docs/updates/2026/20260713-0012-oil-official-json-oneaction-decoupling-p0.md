# 20260713-0012 Oil Official JSON And OneAction Decoupling P0

## Metadata

- Update ID: `20260713-0012`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Area: gamebridge/oil/oneaction/official-json/hook/api/manifest
- Source: user requested the next Full Boundary Audit construction step after the verified player Runtime-only uninstall P0

## Source Request And Ordering

The user requested one more step along the Full Boundary Audit construction plan. Batch 0 is verified by `docs/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md`, P0-A is verified by `docs/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md`, and the work order in `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` therefore selects this independent P0-B boundary:

1. remove GameBridge ownership of the Oil product rule;
2. make the Oil prototype official-JSON-only;
3. remove the OneAction-to-Oil callback while preserving the generic ActionCompletion native adapter;
4. prove Oil-off, Oil-on, and OneAction coexistence through source/unit/package checks and third-save runtime evidence.

The retained subscription packages recorded by `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md` remained an immutable comparison set. This work did not rebuild, rewrite, install, publish, or otherwise mutate those retained artifacts.

## Native-Owner Recheck And Final Decision

The current reference build is `references/doloc-town/reverse/builds/23762374_public_C416D4`. Its method bodies and content tables confirm:

- `coal_mine` owns `drop_spawn_entry=coal_mine_drop` and the unbuffed `count_range=3..4`;
- `GuaranteedManager.SpawnResourceDropItems` applies the native collection count, calls `ItemSpawnInfo.SpawnItems`, and creates world drops through `IDropItemHost.CreateDropItem`;
- `Tables.HandleModItemSpawnExtension` removes matching `SpawnId` entries and appends the extension entry to both ordered spawn lists;
- `ISpawnLut.SpawnInternal` uses the resulting cumulative order; a selected amber interval which is already at `max_count=1` does not break or reroll, so scanning may fall through to the later Oil interval;
- `SpawnData.Unlimited` treats `max_count=0` as unlimited.

The implemented official extension is append-only:

```json
[
  {
    "id": "coal_mine_drop",
    "extra_items": [
      {
        "spawn_weight": 25,
        "min_count": 0,
        "max_count": 0,
        "item_name": "crude_oil"
      }
    ]
  }
]
```

The final merged order is coal `990` unlimited, amber `10` maximum one, then Oil `25` unlimited. Oil occupies `25/1025 = 2.43902439%` of the initial cumulative interval and amber occupies `10/1025 = 0.97560976%`. The earlier independent-Bernoulli illustration is not the exact current algorithm because capped amber can fall through to later Oil. The exact source-anchored probabilities of at least one Oil are:

- 3 draws: `0.0716773117047055` (`7.16773117%`);
- 4 draws: `0.09458570516048614` (`9.45857052%`);
- equal 3/4-draw mean: `0.08313150843259582` (`8.31315084%`).

Deterministic tests also cover the strictly increasing 5-8 draw paths. This correction does not change the selected weight `25`. A future Oil economy/product review may choose another distribution; that is not part of this ownership P0.

## Implemented Boundary

### Oil package and publication projection

- Oil is now a pure official `ContentPack`; its manifest has no `EntryDll`, `MinimumDTMApiVersion`, or `Dependencies`.
- `Content/mod_tbmoditemspawnextension.json` adds exactly the selected Oil row and does not redeclare coal or amber.
- The empty Oil CodeMod entry point/project and code-menu localization were removed from source, the solution, and the canonical build.
- Catalog and release definitions project Oil as `OfficialJsonContentPack` / `ContentOnly` with no DLL/project contract. Its frozen identity, manifest/source version `0.3.1-dtmapi`, `Prototype` maturity, and `PrototypeBlocked` release eligibility are unchanged. The native `official-info.json` display/projection version and catalog target version remain `1.0.0`; these are separate version axes, not a claim that the source manifest was promoted.
- The installer validates the content-only manifest, recursively rejects source or staged DLLs and nested/duplicate manifests, writes the validated canonical manifest after content copy, stages the complete directory outside `MODS`, publishes by atomic directory move, and retains fail-closed no-overwrite behavior.

### GameBridge and OneAction ownership

- `OilCoalDropFeature` and `OilCoalDropService` were removed, including the managed random roll, pending-hit cache, direct `crude_oil x1` backpack grant, feature/Hook status routes, and smoke forcing.
- `ToolColliderHitHookBridge` now installs only the generic ActionCompletion Postfix; there is no Oil Prefix or Oil callback branch.
- ActionCompletion construction/service no longer accepts or invokes an Oil delegate after synthetic OneAction completion.
- The generic OneAction resource, wrong-tool, fuel/feed, and vegetation paths remain GameBridge-owned and use the reviewed native adapters.
- Smoke coordination now supports a strict Oil-only cold-start mode, an Oil-absent mode, combined Oil plus all four OneAction scenarios, a unified completion gate, and verified cleanup on success and exception paths. Official Mod profile selection now fails before launch when application, requested IDs, or Oil-disabled state is wrong; apply failures roll back immediately, and the entire applied-profile run is enclosed by a restoration `finally`.

### Manifest dependencies

ActionSpeed, AutoHarvest, and CropHarvestingQA now each declare exactly one required `DTMAPI.GameBridge.DolocTown` dependency with minimum `0.5.1-alpha`. Their identities, maturity, and behavior were not changed.

## Changed Files

Implementation and package boundary:

- `DTMAPI.sln`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionService.cs`
- deleted `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropFeature.cs`
- deleted `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/ToolColliderHitHookBridge.cs`
- `testmods/OilMod/manifest.json`
- `testmods/OilMod/Content/mod_tbmoditemspawnextension.json`
- `testmods/OilMod/README.md`
- deleted `testmods/OilMod/ModEntry.cs`
- deleted `testmods/OilMod/OilMod.csproj`
- deleted `testmods/OilMod/i18n/english.json`
- deleted `testmods/OilMod/i18n/schinese.json`
- `testmods/README.md` (Oil entry only for this Update)
- `testmods/ActionSpeedMod/manifest.json`
- `testmods/AutoHarvestMod/manifest.json`
- `testmods/CropHarvestingQaMod/manifest.json`
- `tools/release/dtmapi-product-catalog.json` (Oil projection only for this Update)
- `tools/scripts/build.ps1`
- `tools/scripts/check-product-catalog.ps1` (Oil contracts only for this Update)
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/release-common.ps1`

Tests and evidence harness:

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/OilCoalDropSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`

Governance records:

- `docs/hook-map/README.md`
- `docs/hook-map/focused/ActionCompletion.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence-retention-allowlist.json`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md` (dated resolution and acceptance-scope qualification only)
- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- `docs/updates/INDEX-2026-07.md`

Other dirty-worktree files belong to the preceding Batch 0/P0-A work and are not claimed by this Update.

## Acceptance Matrix

| Gate | Evidence | Result |
| --- | --- | --- |
| Current native owner | Build `23762374_public_C416D4` method bodies plus source-anchored unit checks for owner, 3-4 count, remove/append order, capped-amber fall-through, and unlimited semantics. | passed |
| Official JSON package | Exact one-row extension; content-only isolated installs under PowerShell 7 and Windows PowerShell 5.1; installed Oil tree contains manifest/item/extension/assets and zero DLLs; recursive DLL, nested manifest, staged duplicate manifest, and existing-target cases all fail closed without residue or overwrite. | passed |
| Deterministic distribution | Exact `990/10/25` merged order, min/max rules, 3/4 probabilities, equal mean, and strictly increasing 5-8 probabilities. | passed |
| GameBridge ownership removal | Type/source scans find no non-smoke Oil feature/service/cache/direct-grant/status/callback ownership; ToolCollider is Postfix-only for ActionCompletion. | passed |
| OneAction decoupling | No Oil delegate/callback; resource, wrong-tool, fuel/feed, and vegetation runtime scenarios all pass in the Oil-enabled process. | passed |
| Manifest dependencies | All three selected manifests have exactly one required GameBridge `0.5.1-alpha` dependency. | passed |
| Oil absent/disabled | `GAME-SMOKE/20260713-221443`: third save, base coal/amber LUT, no native/indexed Oil, no retired Hook/feature status. | passed |
| Oil enabled | `GAME-SMOKE/20260713-221610`: final merged LUT and natural native world drop on attempt 8; no direct backpack helper. | passed |
| OneAction coexistence | The same positive process passes all four OneAction scenarios with Oil enabled and the generic shared Postfix. | passed |
| Lifecycle and exit | Both runs pass startup/GameLaunched, HookProbe, SaveLoaded, profile restoration, no-fatal, forced-close, and process-exit gates; no `DolocTown.exe` remains. Preserved profile summaries also pass the exact post-run fail-closed gate replay. | passed |

The runtime run validates native ownership and placement, not a final economy distribution. Statistical economy/tolerance sampling, final Oil/Mine balance, and Oil product promotion remain deferred; deterministic exact-algorithm checks own this P0's probability regression gate.

## Validation

Automatic validation:

- repository-selected Release build: passed, `0` warnings / `0` errors; all game-loaded assemblies remain `netstandard2.0`;
- `tools/scripts/test.ps1 -Configuration Release`: passed on the final tree in 114.2 seconds;
  - `DTMAPI.UnitTests: OK`;
  - runtime evidence retention and allowlist checks: OK;
  - player Runtime-only uninstall ownership matrix: OK;
  - Batch 0 product catalog checks: OK (`products=26`, `public=11`, `workshop-items=21`, `api-rows=45`);
- Oil content-only isolated install matrix under PowerShell 7 and Windows PowerShell 5.1: passed; the legal package had zero DLLs and one canonical manifest, recursive source DLL/nested manifest/staged duplicate-manifest fixtures were rejected with staging cleanup, and an existing destination left both its sentinel and enablement bytes unchanged;
- `run-game-smoke.ps1` AST parse under PowerShell 7 and Windows PowerShell 5.1: passed;
- post-run smoke-profile hardening: source/unit checks prove apply-time rollback, requested-ID and Oil-disabled selection gates, result gating, and `finally` restoration; the preserved negative and positive profile summaries replay the current predicates as passed, so no second game run was needed for this control-flow-only hardening;
- source scans for former Oil service/cache/Prefix/force symbols: no implementation residue; only explicit negative-test names remain;
- `git diff --check`: passed;
- document governance: passed.

Runtime validation used the shared lock and the required third save (`SaveSlot 3`, native archive index `2`):

1. `GAME-SMOKE/20260713-221443`, `CoreOnly`, Oil disabled:
   - `RunStatus`, GameLaunched, HookProbe GameLaunched/SaveLoaded, SaveLoaded, `NewContentApis`, and `NewContentOilAbsent` passed;
   - `crude_oil` was absent from the native item table and active DTMAPI content index;
   - `coal_mine_drop` was exactly coal `990` unlimited then amber `10` maximum one;
   - no retired Oil Hook or feature status existed;
   - profile restoration, no-fatal, clean process exit, and no forced close passed.
2. `GAME-SMOKE/20260713-221610`, `CoreOnly` plus current Oil and OneAction:
   - `NewContentOilOnly`, Oil metadata, Oil coal drop, OneAction resource, wrong-tool, fuel/feed, and vegetation all passed;
   - merged LUT was exactly coal `990`, amber `10`, Oil `25` in that order;
   - attempt `8/256` created `coal|coal|coal|crude_oil` in `CurrentRoom.DM_dropitem.AllDatas` through the native resource path;
   - cleanup removed all four new drops (`remainingInDM=0`) and verified the transient resource no longer registered;
   - profile restoration, no-fatal, clean process exit, and no forced close passed.

Before release of the runtime lock, the prior local Oil and OneAction directories were restored. The retained before-images for `mod_infos.json` and OneAction config are byte-identical to the restored current files. The session's pre/post SHA-256 comparison also matched smoke settings and all three third-save archive files; the compact restore summary records those hashes and the save files' pre-run write times. The third save was not modified. The lock was released and the final `DolocTown.exe` count was zero. Steam subscription packages were never touched.

## Evidence

- Negative cold-start run: `docs/debug/evidence/GAME-SMOKE/20260713-221443`
- Positive Oil + OneAction run: `docs/debug/evidence/GAME-SMOKE/20260713-221610`
- Post-run profile-gate replay: `docs/debug/evidence/GAME-SMOKE/20260713-221610/official-mod-profile-gate-replay.json`
- Runtime-state restore hash summary: `docs/debug/evidence/GAME-SMOKE/20260713-221610/runtime-state-restore-summary.json`
- Focused Hook owner: `docs/hook-map/focused/ActionCompletion.md`
- Public API implementation row: `docs/api/public-api-matrix.md` (`IActionCompletionApi`)
- Active runtime rows: `docs/debug/regressions/smoke-matrix.md`
- Native-owner reasoning and resolution link: `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`

## Rollback

Source rollback is one attributable boundary: restore the Oil project/entry point and previous package projection, restore the Oil GameBridge feature/service/Prefix/callback/delegate, remove the official spawn extension and the three new provider dependencies, and revert the focused tests/docs together. That rollback would deliberately reintroduce the ownership defect and requires a new Review before use.

Runtime validation itself is already rolled back: the pre-run local Oil/OneAction packages and byte-identical enablement/config/save state were restored under the shared lock. No Workshop rollback is applicable.

## Follow-Up

P0-B is complete. Continue with the next Full Boundary Audit/0.5.5 workstream: unified version/release/source authority and its focused Review/Update. The later OneAction structural product split, general demand activation, Author SDK, QA extraction, active-gameplay GC gates, final Oil economy/product promotion, and Workshop publication remain independently scoped work.
