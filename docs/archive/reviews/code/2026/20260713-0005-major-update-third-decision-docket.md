# 20260713-0005 Major Update Third Decision Docket

Status: recorded / third-round decisions closed
Date: 2026-07-13
Scope: CustomEntity promise, authoritative product roster, player source precedence, staged 0.5.5 rollout, and public API warning windows
Related Update: `docs/updates/2026/20260713-0002-second-round-closure-third-decision-docket.md`
Follows: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`

## Source Request

The user supplied `D:/下载/第二轮小讨论.md`, closed the second round as player A1 plus Author-SDK A2, player B0 plus SDK B1/later B2, C2 migration toward a fully classified tree, and D1 unified Author SDK, then asked to continue with a third decision round.

The user then supplied `D:/下载/第三轮.md`, accepted E1/F1/I1 directly, accepted G1 with explicit Author-SDK source modes and per-Mod overrides, and refined H1 into a Runtime-first Canary rollout before other 1.0.0 product updates.

This docket preserves the second-round A/B/C/D order in the preceding record and starts the remaining choices at E. It is docs/source analysis only and does not implement any option.

## Closed Inputs, Not Third-Round Choices

The following are already decided or required for correctness:

- public DTMAPI is `0.5.5`; file metadata is `0.5.5.0`; assembly compatibility identity is separately gated;
- each Mod declares its real `MinimumDTMApiVersion`; packaging does not rewrite it to the current framework version;
- minimum-version rejection occurs before `Assembly.LoadFrom`, construction, or `Entry()`;
- status distinguishes subscribed installer package, actually installed/running DTMAPI, selected Mod source/version, and required minimum;
- 0.5.5 restores `StopOnManualMove` compatibility and proves representative old DLLs under Unity Mono;
- bare Workshop-directory presence is not subscription authority;
- official enablement refresh occurs after the authoritative save or from a native in-memory snapshot;
- unknown external BepInEx plugins and unverified package directories are read-only diagnostics;
- `dtmapi-package.json` has no destructive authority;
- the player Doctor must have both an in-game summary/export when DTMAPI loads and an offline read-only check/log path when it does not. Full validation/packaging/repair remains Author SDK work.

## E - C# CustomEntity Product Promise

Focused source review: `docs/reviews/api/2026/20260713-0001-customentity-public-promise-review.md`.

Current facts:

- the C# umbrella contains 97 public types and mixes registry/query with unimplemented runtime verbs;
- all native creation requests remain deliberately blocked;
- the working JSON+PNG+WAV animal route is separate and does not validate the generic C# definitions;
- no ordinary known consumer was found, but 0.5.5 still has a zero-deletion ABI gate.

| Option | Meaning | Main consequence |
| --- | --- | --- |
| E1 - JSON animal route is formal | Support custom husbandry animals through official JSON plus the reviewed PNG/WAV bridge; freeze/deprecate the generic C# umbrella and retire it only after compatibility gates. | Small truthful platform; future families remain independent projects. |
| E2 - permanent placeholders | Keep the four blocked C# families and 97 types indefinitely. | Avoids removal but permanently owns speculative save/AI/combat/drone promises. |
| E3 - implement the umbrella | Commit now to native Animal, Monster, Attack/Projectile, and Drone runtime creation behind the current model. | Four large native projects become one platform commitment before demand/owners are proven. |

Final decision: **E1**. Treat E2 only as the 0.5.5 compatibility transition. Future E3 work must be four independent native-owner projects; E1 does not prevent one from being approved later.

## F - Product Catalog Roster

Focused fact review: `docs/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md`.

Eleven Workshop-distributed identities are already `PublishedProduct` facts, including AutoFishing, MoreEquipmentSlots, and Manbo despite their stale `DeveloperOnly` script labels. Catalog must separate role, type, distribution, release eligibility, and maturity instead of forcing one status enum.

| Option | StrongPlantingGun | Oil | Mine | Current AutoHarvest |
| --- | --- | --- | --- | --- |
| F1 - fact-first conservative | PlannedProduct / rebuild blocked | Prototype JSON content, promotion target planned | Prototype, promotion target planned | ApiDemandSample / never publish |
| F2 - roadmap-forward | PlannedProduct | PlannedProduct, prototype blocked | PlannedProduct, prototype blocked | ApiDemandSample / never publish |
| F3 - minimum commitment | Prototype until rebuild | Prototype | Prototype | ApiDemandSample / never publish |

Final decision: **F1**. It records StrongPlantingGun's bounded product intent without treating Oil/Mine design work as release eligibility. The old `None.AutoHarvest` Workshop item remains a distinct predecessor/legacy identity and is never assigned to the current sample.

## G - Published Product Source Precedence

This choice applies when a catalogued published first-party product has both an enabled current Workshop subscription and an OfficialLocal/local duplicate.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| G1 - Workshop player authority | Player mode selects the subscribed Workshop source; OfficialLocal/local may override only through explicit developer mode. If no subscribed source exists, a valid sole OfficialLocal source may still load. | Matches automatic product updates and keeps deliberate local testing. |
| G2 - prompt every duplicate | Manager blocks or asks the player to persist a source choice. | Explicit, but makes ordinary players understand implementation sources and adds startup state. |
| G3 - keep OfficialLocal priority | Preserve current fixed order. | Old copied packages continue to shadow updates. |
| G4 - highest numeric version | Select whichever source compares newest. | Fails across the 1.0.0 epoch, legacy suffixes, and development builds. |

Final decision: **G1**, refined through the Author-SDK source modes recorded below.

Regardless of selection, DTMAPI must report `[SHADOWED]` with both versions and paths, never delete unknown duplicates, use the authoritative subscription snapshot, and require restart after a loaded code source changes.

## H - New DTMAPI And Functional-Mod Rollout

| Option | Meaning | Player impact |
| --- | --- | --- |
| H1 - Runtime-first staged rollout | Publish the 0.5.5 installer/update diagnosis first; after an observed public stabilization window, publish 1.0.0 Mod builds that truly require 0.5.5. | Reduces the window where auto-updated Mods are blocked by stale installed DTMAPI. |
| H2 - simultaneous rollout | Publish 0.5.5 and requiring Mods together. | Faster, but many subscribed Mods can stop until players manually rerun the installer. |
| H3 - permanent dual implementations | Keep every new Mod compatible with old DTMAPI through parallel code paths. | Avoids some blocks but contradicts the selected clean rebuild and grows product complexity. |

Final decision: **H1**, refined into a Runtime-first Canary sequence recorded below.

Status severity is an engineering consequence:

- a newer installer exists while current Mods remain compatible: non-blocking `[UPDATE]`;
- an enabled Mod is rejected by its truthful minimum: prominent `[UPDATE REQUIRED]`, block that Mod only, keep the game available;
- never execute the installer in-process; instruct the player to exit and run the current `1_install_dtmapi.bat`.

The stabilization window should close by evidence—clean installer matrix, old-DLL matrix, no release-blocking player failures—not by an arbitrary date alone.

## I - Public API Compatibility Warning Window

| Option | Meaning | Trade-off |
| --- | --- | --- |
| I1 - govern by stability tier | Stable contracts do not break before 1.0; shipped public Experimental contracts require at least one published warning-bearing cycle and an explicit breaking minor epoch such as 0.6.0; Internal/QA has no public promise. | Protects real consumers without freezing every experiment forever. |
| I2 - freeze every public API through 1.0 | Stable and Experimental both become non-breaking. | Strongest compatibility, but makes process artifacts permanent architecture. |
| I3 - one patch warning is enough | Deprecate in one patch and delete in the next. | Fast cleanup, but unsafe for auto-updated Mods plus manually installed DTMAPI. |

Final decision: **I1**. Author SDK also owns Deprecated-use scanning and migration guidance.

For a public Experimental contract with a known consumer, removal also requires a migration/facade decision and renewed consumer scan. For an unadopted surface such as current CustomEntity, 0.5.5 still preserves the binary surface, publishes the warning, and defers physical removal to the explicit breaking epoch.

## Final Answer Set

```text
E = E1  formal JSON husbandry-animal route; C# umbrella freezes then retires
F = F1  fact-first roster; StrongPlanting planned, Oil/Mine prototypes with targets
G = G1  Workshop authority in player duplicate conflicts; explicit dev override
H = H1  Runtime-first staged rollout
I = I1  stability-tier warning and breaking-version policy
```

## User Decision Resolution - 2026-07-13

This section preserves the order of the supplied third-round feedback: G, H, then the directly accepted E/F/I group.

### 1. G1 - Workshop Player Authority With Explicit Author-SDK Overrides

#### User-confirmed direction

```text
ordinary player mode
  enabled current Workshop source wins
  old OfficialLocal/Local copies cannot shadow it

no valid Workshop source
  one valid OfficialLocal source may load

developer mode
  Author SDK explicitly selects a local override for one UniqueID
  mere local-directory presence never overrides Workshop
```

Author SDK provides four explicit source modes:

| Mode | Meaning |
| --- | --- |
| `Player / Workshop` | Default player behavior; current enabled subscription wins. |
| `Local Development` | One named Mod uses its selected local development source. |
| `Workshop Validation` | Force the Steam-downloaded source and show version, path, and hash. |
| `Player Reproduction` | Temporarily disable every local override to reproduce an ordinary player environment. |

It must support per-Mod switching, one action to clear all overrides, selected/shadowed display, and no automatic movement/deletion/overwrite of unverified copies. A loaded CodeMod DLL never hot-switches; a source change is pending until restart.

Expected diagnosis shape:

```text
[SELECTED] Workshop AutoFishing 1.0.0
[SHADOWED] OfficialLocal AutoFishing 1.4.3-dtmapi
Reason: player mode uses Workshop authority
```

#### Analysis immediately following issue 1

This refinement keeps source selection out of ordinary player configuration while making the development override reproducible rather than directory-order dependent. The override state belongs to Author SDK/developer configuration and may reference only a specific `UniqueID` plus selected source identity; it is not a new ownership receipt and grants no file mutation authority.

`Workshop Validation` and `Player Reproduction` are also release/QA inputs. They must consume the same authoritative Steam subscription snapshot as player mode, not raw directory presence. Selected-source changes after assembly load remain restart-required because Mono cannot unload the resident DLL.

### 2. H1 - Runtime-First Canary Rollout

#### User-confirmed direction

The release sequence is:

1. publish the DTMAPI 0.5.5 Workshop installer/status/update path while existing functional Mods remain unchanged;
2. publish one low-risk rebuilt 1.0.0 Canary which genuinely requires 0.5.5;
3. validate the real old-installed-DTMAPI player update/recovery path;
4. update other functional Mods one at a time after the Canary gate passes.

The Canary is chosen by actual completion order between AutoFishing and OneActionComplete. MoreEquipmentSlots is excluded because minimum-version blocking intersects protected-item/recovery behavior.

The Canary manifest must truthfully declare:

```json
"MinimumDTMApiVersion": "0.5.5"
```

An old installed DTMAPI must reject before loading the DLL and show the installed/required versions plus an instruction to exit the game and rerun the current Workshop `1_install_dtmapi.bat`.

#### Analysis immediately following issue 2

The Canary gate must prove:

- no Canary `Entry()` or constructor executes under the old DTMAPI;
- no `MissingMethodException`, type-load error, or assembly-load side effect occurs;
- the game and other compatible Mods remain available;
- package version and actually installed/running DLL version are reported separately;
- the manual installer update restores the Canary;
- installer and old-DLL matrices pass;
- no release-blocking player feedback remains.

Only a Mod which consumes a 0.5.5 contract declares that minimum. H1 compatibility covers current public functional Mods, known external consumers, and published Abstractions/provider/manifest identities. It does not preserve every historical developer build or create permanent old/new implementations.

#### Sixth-round V1 revised refinement

`docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` preserves Runtime-first rollout but replaces this historical dynamic-Canary detail:

- DTMAPI 0.5.5 uses internal RCs and one public player update;
- AutoFishing 1.0.0 is the fixed old-Runtime block/update/recovery Canary and releases alone;
- OneAction is the first structural product-ownership split template, not the alternate Canary;
- ActionSpeed follows only after the focused active-gameplay GC/native-owner gate;
- low-user products may share a publication window but keep independent WorkshopID, UniqueID, disable and rollback boundaries;
- MoreEquipment releases alone;
- every formal first-party product in the selected 1.0.0 epoch explicitly declares `MinimumDTMApiVersion = 0.5.5`; packaging validates and never silently inserts or raises it.

The original H1 text remains the historical third-round decision input. V1 revised is authoritative for current public ordering.

### 3. E1, F1, And I1 - Direct Acceptance

#### User-confirmed direction

- **E1:** formally support official JSON plus PNG/WAV husbandry animals; freeze/warn the generic C# CustomEntity umbrella in 0.5.5 and retire it through I1.
- **F1:** use the fact-first roster; StrongPlantingGun is PlannedProduct, Oil/Mine are Prototype with promotion targets, and AutoHarvest is a never-published ApiDemandSample.
- **I1:** govern compatibility by API stability; Stable remains strict, public Experimental receives at least one published warning cycle and an explicit breaking boundary, and Internal/QA creates no ordinary-author promise. Author SDK owns Deprecated scanning and migration hints.

#### Analysis immediately following issue 3

These choices align the working content route, factual public distribution history, and compatibility lifecycle. They do not authorize signature removal in 0.5.5, publish Oil/Mine, or promote Experimental APIs. Each implementation still requires its source/API/release gate.

## Work-Order Effect

These choices do not reorder the first implementation batches. They make Batch 0-3 gates more precise:

```text
Batch 0  Catalog + 11 public identities + API/CustomEntity compatibility freeze
Batch 1  player Runtime-only uninstaller + Oil GameBridge removal/official JSON
Batch 2  source authority + Author-SDK override schema + 0.5.5 binary/version/update contract
         + Runtime-first Canary rollout gates
Batch 3A unified Author SDK
Batch 3B optional QA extraction
Batch 4  hot-path and lifecycle-driven content refresh
Batch 5  one 1.0.0 product rebuild at a time
Batch 6  warning-window/API retirement governance
```

The fourth-round Manager/MoreSaves/Y-console options continue in `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`.

## Validation Boundary

This docket cross-checked the supplied discussion, current release definitions, manifests/publish metadata, local Workshop/upload identities, Core CustomEntity registry/facades, Abstractions public types, GameBridge registration/status, public API matrix, 0.5.5 compatibility review, and source-management review. It did not modify runtime/API/product/catalog behavior, inspect the live Steam web service, launch the game, acquire the runtime lock, or mutate game/Workshop files.
