# 20260713-0001 Major Update Second Decision Docket

Status: recorded / second-round decisions closed
Date: 2026-07-13
Scope: integration of the follow-up Oil/version discussion, DTMAPI source-management semantics, remaining boundary decisions, and revised implementation ordering
Related Update: `docs/updates/2026/20260713-0001-major-update-second-decision-docket.md`
Follows: `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`

## Source Request

The user supplied `D:/下载/小讨论.txt` as a cross-thread decision summary. Oil should use official JSON, Mine should use official JSON as far as its native static boundary allows, and compatibility must emphasize automatically updated Workshop functional Mods meeting a manually installed, possibly stale DTMAPI Runtime.

The user accepted the first decision docket in substance and asked to continue the remaining full-audit issues as smaller option/reason discussions. The user also requested a mechanism analysis of DTMAPI enable/disable management, Workshop unsubscribe deletion, and Mods installed directly under BepInEx.

On 2026-07-13 the user supplied `D:/下载/第二轮小讨论.md` and closed this round in the original A/B/C/D order. The final model separates player Runtime safety from Author SDK capabilities: player uninstall is Runtime-only, receipt-authorized deployment cleanup belongs to Author SDK/internal tools, player content loading is lifecycle-driven with zero polling, explicit/automatic author reload belongs to the SDK, Catalog-first gradual migration remains the process, and the endpoint is one independent versioned Author SDK.

This is a docs/source decision record. It does not implement JSON, versions, loader rules, receipts, QA extraction, hot-path changes, source migration, author tools, API removal, or UI changes.

## 1. Follow-Up Decisions Integrated

### 1.1 Oil: official append-only baseline

The selected development baseline is now the smallest official extension:

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

This deliberately does not replace/redeclare coal or amber. With current base weights `990 + 10 + 25`:

- Oil is about `2.439%` per native draw;
- an unbuffed 3/4-draw coal node has about `7.14% / 9.40%`, average `8.27%`, chance of at least one Oil;
- `max_count=0` remains native unlimited count, so multiple Oil and collection-count bonuses are possible;
- amber becomes about `0.976%` per draw instead of exactly `1%`; its average 3/4-draw at-least-one chance changes from about `3.455%` to `3.372%`.

This accepts a small amber dilution in exchange for lower composition conflict: Oil appends only its own id and does not take ownership of base-game entries. The earlier exact amber-preserving rebalance remains research, not the selected first implementation. Final Oil/Mine economy is deferred.

The GameBridge Oil service, caches, Hook/status/smoke forcing, `crude_oil` hard-code, direct backpack `x1`, and OneAction callback remain selected for deletion.

### 1.2 Mine: two-layer boundary unchanged

```text
official JSON
  item/equipment/footprint/storage/recipe and other supported static content

Mine Mod
  cycle/output/economy/product policy

GameBridge
  only demand-activated, reviewed electricity/instance/storage/real-preview adapters
```

Electricity remains unverified Experimental work. The short-term real held-preview adapter is part of this two-layer continuation; dedicated Mine art and scale 1 are required before formal publication.

### 1.3 Compatibility: the primary deployed skew is new Mod + old installed Runtime

Two directions are required, but they are not equal in deployment frequency:

| Combination | Policy |
| --- | --- |
| old Mod + new DTMAPI | preserve public ABI/provider/manifest compatibility where practical; old first-party DLLs are regression samples, not permanent architecture owners |
| new Workshop Mod + old installed DTMAPI | manifest declares its real minimum; old Runtime must reject before loading the DLL and tell the player to rerun the installer |

AutoFishing 1.0.0 should declare `MinimumDTMApiVersion=0.5.5` only when its rebuilt code truly consumes a 0.5.5 contract. Do not maintain a complex dual implementation merely to let a newly rebuilt first-party product run on an older Runtime. Do not mechanically raise the minimum when no new contract is used.

Current source orders `CanLoadApiVersion` before `LoadCodeMod`/`Assembly.LoadFrom`, but the actual old-Runtime path has not been exercised. The release gate must prove no `Entry()`, no `MissingMethodException`, and a clear player instruction.

The status system must distinguish:

1. current subscribed DTMAPI installer package;
2. actual installed/running BepInEx DTMAPI DLL/version/hash;
3. functional Mod selected source/version and required minimum.

It must also detect old OfficialLocal copies which shadow a newer Workshop package. The detailed mechanism is recorded in `docs/reviews/code/2026/20260713-0003-mod-management-source-boundary.md`.

Mixed official-JSON plus CodeMod packages need an additional product check: DTMAPI can block `EntryDll`, but it does not own the native game's earlier/independent JSON ingestion. Static content must be safe when its code layer is blocked, or the package must be split/deferred. Oil is safe after becoming official-JSON-only; Mine remains a prototype until its incomplete-code behavior is deliberately defined.

## 2. What Is Not A Product Decision

The following are engineering correctness constraints:

- a public author marker cannot authorize overwrite/deletion;
- normal players must not perform a missing Smoke-file filesystem check every frame;
- QA must leave the normal five-DLL player package and must not require new public Abstractions APIs;
- `LoadedMods.ToArray`, LINQ, path/signature construction, `File.Exists`, and timestamp reads must not precede throttling on every frame;
- Audio/CustomAnimals definition discovery must become generation/lifecycle driven and demand activated;
- JSON+PNG+WAV animals remain a protected behavior baseline;
- that protected JSON route does not prove arbitrary C# CustomEntity runtime creation;
- external/unknown BepInEx plugins are never auto-moved, disabled, or deleted;
- source/package shrinking is not evidence that Unity/Mono GC is fixed.

The QA architecture may use an immediate opt-in guard as the first rollback-safe commit, but its endpoint is an optional QA assembly explicitly staged by developer/smoke tooling and excluded from player packages.

## 3. Second-Round Decisions - Closed

### Decision A - verified package cleanup or Runtime-only uninstall

| Option | Meaning | Benefit | Cost |
| --- | --- | --- | --- |
| A1 - Runtime only | DTMAPI uninstaller never removes official-local packages. | Simplest and strongest deletion safety. | Old local packages remain unless an owning Author-SDK receipt tool or the user cleans them separately. |
| A2 - transaction receipts | Retain optional package removal only for a package-local installer receipt matching external install state, exact identity/path/file hashes, and no unknown drift. | Complete cleanup remains possible without trusting author metadata. | More migration and failure-atomicity work. |

Final decision: **player Runtime uninstall uses A1**. It removes only DTMAPI Runtime and never scans a package marker to delete, overwrite, move, adopt, or change official-local enablement. **A2 moves to Author SDK and internal development/QA/deployment tooling**: a package deployed through that tool may be cleaned only through the same transaction receipt plus external-state verification. A player-facing cleanup entry may invoke that shared verified operation, but no receipt means preserve. `dtmapi-package.json` permanently loses destructive authority. Detailed gates are in `docs/reviews/code/2026/20260713-0002-installer-package-ownership-receipt-p0.md`.

### Decision B - author-time live reload of JSON/PNG/WAV

| Option | Meaning | Benefit | Cost |
| --- | --- | --- | --- |
| B0 - player lifecycle load | Load at startup and rebuild on an authoritative official-Mod/source lifecycle change; validate and display file/field errors. | Zero ordinary-game file polling and clear player failures. | Does not optimize the author's edit/test loop. |
| B1 - explicit author reload | Author SDK offers an explicit `reload-content` operation for JSON/PNG/WAV. | Deterministic errors and rollback with a small first implementation. | Author invokes reload after editing files. |
| B2 - dev-only watcher | Later Author/Dev mode may debounce file changes and trigger the B1 content-rebuild path. | Better author iteration. | Must handle duplicate/partial-write/rename/temp-file events and watcher lifecycle. |
| B3 - player polling | Always watch or periodically scan in normal play. | Automatic discovery. | Retained I/O/roots and less deterministic behavior conflict with the lightweight goal. |

Final decision: **player Runtime uses B0; Author SDK first provides B1 and may later add B2; B3 is permanently rejected**. B1/B2 cover content files only. CodeMod DLL replacement never receives a hot-reload promise and continues to require a game restart.

### Decision C - physical `testmods` migration pace

The authoritative catalog should distinguish at least `PublishedProduct`, `PlannedProduct`, `Prototype`, `ApiDemandSample`, `QaFixture`, `Example`, `NegativeFixture`, and `CompatibilityComponent`. `DeveloperOnly` is too broad to remain the release authority.

| Option | Meaning | Benefit | Cost |
| --- | --- | --- | --- |
| C1 - move everything now | Split every product/QA/sample/negative directory in one mechanical migration. | Clean tree immediately. | Large path/script/doc churn overlaps the 1.0.0 and behavior rebuild. |
| C2 - catalog first, migrate by product | Loader/package/release checks use one catalog now; each Mod moves when its 1.0.0 rebuild starts; QA/examples can move in one mechanical batch. | Separates identity control from behavior rewrites and improves regression attribution. | Mixed physical directories remain temporarily. |
| C3 - catalog only forever | Never clean physical layout. | Lowest short-term churn. | Humans and scripts remain likely to confuse fixtures and products. |

Final decision: **C2 is the migration process**. Establish the authoritative Catalog first, migrate each functional Mod with its 1.0.0 rebuild, and move QA/examples in bounded mechanical batches. The final state must have the fully classified physical layout described by C1; C1's rejected part is doing the whole move immediately. C3 is rejected. AutoHarvest is already fixed as `ApiDemandSample`, CropHarvestingQA as `QaFixture`, and neither may enter the player package.

### Decision D - Author SDK delivery

| Option | Meaning | Benefit | Cost |
| --- | --- | --- | --- |
| D1 - separate Author SDK | A versioned netstandard2.0 scaffold, Abstractions reference path, schemas, validator, packager, and Workshop-shaped output; player package keeps only a read-only doctor. | Keeps player install light and gives authors one reproducible route. | Requires a separate SDK artifact/version policy. |
| D2 - bundle all author tools with Runtime | Templates/docs/build tools are installed for every player. | Easy to find. | Couples author tooling to the manually installed Runtime and adds player weight. |
| D3 - docs and copied template only | No validator/packager/doctor. | Fastest initial writing. | Repeats the current misplaced DLL, missing dependency, and package drift failures. |

Final decision: **D1**. Build one unified, independent, versioned Author SDK rather than a collection of unrelated scripts. It owns templates, Abstractions references, schemas, validation, packaging, local deployment, A2 receipts/cleanup, B1 reload, later B2 watching, examples, and author documentation through one rule set. Public author output is Workshop-shaped `Content/DTMAPI`; local development mirrors it under game `Mods`. The player package retains only read-only Doctor/error/minimum-version/misinstallation diagnostics. Direct BepInEx plugins remain external and are never automatically moved, deleted, or adopted.

## 4. Deferred Decision - C# CustomEntity Product Promise

This should be its own later round because it changes the promised platform size:

| Option | Meaning |
| --- | --- |
| E1 - JSON creation is the formal route | Preserve content-pack definitions/query contracts as justified; freeze and retire unimplemented runtime verbs after compatibility review/warning. |
| E2 - permanent blocked placeholders | Keep the full Experimental C# surface indefinitely as a possible future promise. |
| E3 - commit to runtime creation | Create separate Animal, Monster, Attack/Projectile, and Drone native-owner projects with save/AI/collision/damage/lifecycle gates. |

Recommendation for the lightweight/public baseline: **E1**. Regardless of the later choice, 0.5.5 does not expand or promote these APIs, and JSON animal behavior remains protected.

The product roster for Mine, StrongPlantingGun, MoreEquipmentSlots, Manbo, and other prototypes is also deferred to a dedicated product-list round after the catalog exists.

## 5. Revised Work Order

The original order remains structurally correct with three refinements: compatibility needed for auto-updated Mods moves earlier, Author SDK and QA extraction may proceed in parallel after the release contract, and AutoHarvest/Oil leave the later productization list.

```text
0. Identity/catalog/protected behavior/API compatibility baseline

1A. Installer receipt P0
1B. Remove GameBridge Oil ownership; add append-only official Oil JSON
    (independent Updates; do installer P0 first if only one can start)

2. Version/release/source authority
   - one version authority
   - true per-Mod minimum
   - new Mod + old installed Runtime pre-load rejection
   - Workshop package vs installed DLL [UPDATE]
   - OfficialLocal shadow migration/player Workshop priority

3A. Author SDK/validator/packager/read-only doctor
3B. Optional QA host and staged Smoke extraction
    (parallel after Batches 0-2 contracts)

4. Hot-path cache/generation invalidation and demand activation

5. Functional Mod 1.0.0 rebuilds one by one
   - OneAction remains the first small template
   - AutoHarvest is absent
   - Oil is JSON content, not a late GameBridge product
   - Mine follows the two-layer prototype route

6. Broader external compatibility/API status and warning-window governance

7. GMCM/Manager UX, MoreSaves UX, Y-console rewrite, animal economy,
   Audio/BGM research, and future platform projects
```

No product migration changes `UniqueID`, Workshop item, config owner/path, content ids, or save sidecars without an explicit migration. No source/API-size reduction is called a GC fix.

## 6. Final Answer Set For This Round

```text
A = player A1; Author SDK/internal deployment A2
B = player B0; Author SDK B1 first, B2 later; reject B3
C = C2 process; fully classified physical tree as the endpoint
D = D1 unified independent versioned Author SDK; player read-only Doctor
```

These decisions close the second round. The third-round options for CustomEntity, the formal product roster, player source precedence, release/update policy, and compatibility warning windows are recorded in `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`.

## Validation Boundary

The review cross-checked the supplied discussion file, current decision/audit records, Core manifest discovery/load/deactivation paths, Workshop/official enablement state, current BepInEx and DTMAPI logs, official native ModManager/ModUiState method bodies, installer/release/uninstall/status scripts, author docs, and the current player/runtime package layout. It did not modify source/runtime packages, launch the game, acquire the runtime lock, change official enablement, remove/move files, or mutate Workshop subscriptions.
