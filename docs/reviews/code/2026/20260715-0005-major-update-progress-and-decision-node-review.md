# 20260715-0005 Major Update Progress And Decision-Node Review

Status: recorded
Date: 2026-07-15
Scope: reconcile the committed major-update mainline with the closed route, current regression evidence, new findings, and later product decisions which must reopen only at their scheduled implementation node
Related Update: `docs/updates/2026/20260715-0008-major-update-progress-and-decision-node-review.md`
Primary route: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
Decision closure: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
Prior progress review: `docs/reviews/code/2026/20260714-0001-major-update-progress-route-regression-review.md`

## Source Request And Exclusions

The user requested a new current-progress review answering, in order:

1. which implementation layer the project has reached;
2. whether the agreed route is still being followed;
3. which behavior deserves regression testing;
4. whether new problems appeared;
5. which later route nodes, especially AnimalPack, still require detailed decisions before implementation.

The user explicitly excluded separate local analysis and third-party non-DLL Mod repair work. At the review snapshot the worktree had 33 status entries: 7 tracked modifications and 26 untracked paths. The tracked changes were the official-document/third-party JSON-tool work's author indexes, reference index, smoke isolation support, unit assertions, smoke matrix, and July ledger rows. They are not counted as DTMAPI major-mainline progress.

This review does not evaluate or approve those excluded artifacts, change Runtime/API/product/package/Workshop behavior, launch Doloc Town, or reopen a global decision round.

## Snapshot And Executive Verdict

Branch: `codex/major-update-batch0-20260713`

HEAD: `866021b5695080af2b9f008d347df2a379043a4a` (`build: start Batch 2 version authority`)

The accurate position is:

```text
Batch 0                           verified and committed
player-uninstaller ownership P0 verified and committed
Oil/OneAction ownership P0      verified and committed
pre-Batch-2 recovery/regression verified and committed
Batch 2                          first slice committed; lifecycle still in-progress
Batch 3 through Batch 8         not started on the major mainline
```

This is a substantial improvement over the prior review: the mainline is no longer one uncommitted rollback stack. The route is still correct. The project is not at “version authority complete,” “0.5.5 RC,” “lightweight Runtime,” or “GC fixed.”

## 1. Current Progress Layer

| Mainline boundary | State | Commit / durable evidence |
| --- | --- | --- |
| Batch 0 Catalog, identity, API/behavior freeze | complete | `b1b978dd`; `docs/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md` |
| Player Runtime-only uninstall ownership P0 | complete | `f6eaf183`; `docs/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md` |
| Oil official JSON and OneAction decoupling P0 | complete | `3c1f803c`; `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md` |
| Pre-Batch-2 developer install recovery and focused regressions | complete | `d5abc8eb`; `docs/updates/2026/20260714-0003-pre-batch2-install-transaction-and-runtime-regressions.md` |
| Batch 2 first slice | complete as one slice; Batch 2 remains open | `866021b5`; Oil current version is coherently `0.3.1-dtmapi`, ContentOnly drift fails closed, and an enabled Workshop-shaped CodeMod requiring 0.5.5 is rejected on 0.5.3-alpha before assembly load/Entry/owner state. |
| Remaining Batch 2 | not complete | No single machine-readable Runtime version source or `check-release-contract`; three Runtime version authorities remain; minimum versions are still silently rewritten; retained ABI, full external scan, actual 0.5.5 package/update recovery, all-public-product package/hash parity, and installed-runtime update status remain open. |
| Batch 3 Author SDK/Doctor | not started | A2 receipts, real CodeMod scaffold, validator/packager, and non-destructive BepInEx misplacement diagnosis remain future work. |
| Batch 4 QA extraction | not started | Smoke remains inside the player GameBridge and ordinary player startup/update still carries QA coupling. |
| Batch 5 hot-path/demand activation | not started | No-consumer EveryFrame dispatch and the Audio/CustomAnimals recurring discovery paths remain. |
| Batch 6–8 product/API/UI work | not started | No major-mainline product split, compatibility retirement, Manager/UI rewrite, or AnimalPack implementation has been pulled forward. |

### 0.5.5 release-gate view

Of the ten selected release gates:

- gate 1 (Catalog/product identity/version projection contract), gate 2 (player uninstall ownership), and gate 3 (Oil/OneAction ownership) are complete;
- gate 4 (executable unified version semantics) and gate 10 (old-Runtime block/update/recovery) are partial;
- gate 5 (complete external ecosystem scan), gate 6 (zero old-ABI removal proof), gate 7 (QA-free player package), gate 8 (demand activation), and gate 9 (AutoFishing/ActionSpeed active-gameplay GC) remain open.

The first Batch 2 slice proves a pre-assembly `api-too-new` block. It does not prove the real stale-manual-Runtime to installed-0.5.5 recovery path.

## 2. Route Compliance

The committed order is:

```text
ec661e8d  progress/entrance review
b1b978dd  Batch 0
f6eaf183  player uninstaller P0
3c1f803c  Oil/OneAction P0
d5abc8eb  entrance recovery gap and regressions
866021b5  Batch 2 first slice
```

This follows final W1. The installer P0 correctly precedes the Oil P0 because the later binding decision gave deletion/content-ownership risk priority. Closing the newly found package/enablement recovery hole before Batch 2 was a route gate, not feature-work diversion.

No mainline evidence shows premature Batch 3 author tooling, QA extraction, product migration, public 1.0.0 promotion, UI redesign, AnimalPack implementation, or 0.5.5 artifact publication. The excluded dirty third-party/document work must remain isolated from future Batch 2 commits, especially because it currently touches `tests/DTMAPI.UnitTests/Program.cs`, `tools/scripts/run-game-smoke.ps1`, and the smoke ledger.

No additional DTMAPI-wide questionnaire is needed. Reopen a top-level decision only if evidence proves the frozen ABI/identity cannot coexist, exposes an unavoidable public identity collision, or a GC result requires changing promised player behavior rather than isolating/fixing implementation.

## 3. Regression Priorities

### Immediate evidence/governance follow-up

1. Make the Release gate's private-reference prerequisite explicit. A detached clean HEAD compiled with zero warnings/errors but failed `DTMAPI.UnitTests` because `OilOfficialJsonOwnershipIsolatedAndNativeProbabilityMatchesCurrentLut` directly reads ignored `references/doloc-town/reverse/.../item_tbitemspawn.json`. Mounting the configured workspace reverse root made the exact same HEAD pass the complete suite. The normal gate should either use a tracked synthetic semantic fixture plus an optional private-reference conformance test, or fail preflight with an explicit reference-root requirement; official source data must not be copied into the repository.
2. Correct and classify the frame-driver evidence. Both `GAME-SMOKE/20260714-034644` and `034949` contain one missing-frame fallback warning and two `Input System frame driver stalled` warnings, despite Update `20260714-0003` previously saying there were none. Both runs recovered and passed all final gates, and the pattern predates these commits. Run one normal Steam-client, no-HookProbe short-tap/focus route through title -> slot 3 -> title before deciding whether this is expected transition recovery or an input issue.
3. Keep the excluded dirty test/smoke changes out of the next Batch 2 commit and rerun the exact scoped tree after integration.

### Remaining Batch 2 / pre-0.5.5 gates

1. Project one Runtime version authority into MSBuild, Core constants, scripts, package/status/receipt metadata and actual DLL versions/hashes; add `check-release-contract` and old-installed-runtime `[UPDATE]` status.
2. Stop silent minimum-version rewriting. Validate each product's declared compatibility or apply an explicit audited pin policy.
3. Restore the obsolete-compatible `FishingAutomationOptions.StopOnManualMove` member, diff the retained public surface, and load the retained 0.5.1/0.5.2 AutoFishing DLL without recompilation under Unity Mono.
4. Run the real Canary matrix: old manually installed Runtime + new Workshop AutoFishing requiring 0.5.5 -> visible block -> Runtime update -> restart/load -> failed-update recovery. The unit fixture covers only the first block boundary.
5. Build/install/load/disable all 11 public product identities with dependency, Entry-count, state-restoration and clean-exit gates; include a normal Steam-client launch and ordinary no-HookProbe input path. The two latest mainline game runs used DirectExe.

### Later structural and GC gates

1. After QA extraction, rerun Oil-off/Oil-on/OneAction and ordinary product-owner refresh before deleting the embedded player harness.
2. After demand activation, compare no-consumer, CustomAnimals, AudioReplacement, AutoFishing and ActionSpeed dispatch/allocation profiles; do not call source/DLL shrinkage a GC result.
3. Keep AutoFishing and ActionSpeed independent. Each runs its approved `1x`, enabled/no acceleration, common speed, high speed, disabled recovery and title-cycle ladder with per-action and per-minute evidence. Add Animator arbitration only if real owner overlap is observed.
4. Keep ISSUE-010 and ISSUE-011 open. The focused sub-minute runs contain no Fatal/Error/crash/residual process, but they cannot close long-gameplay or the historical roughly 6.5-minute native-crash class.

## 4. New Findings

### P1 validation reproducibility: commit-only Release replay is not hermetic

The clean detached worktree failure proves that the current default unit suite has an implicit private reverse-data dependency. The second run proves the committed source is green under the configured DTMAPI workspace condition; it does not make a clean checkout independently reproducible. This is a validation-contract issue, not a Runtime regression.

### Evidence correction: frame-driver warnings were present

Update `20260714-0003` contained one inaccurate negative claim. Each mainline runtime log records the same recovered warning shape. No new Debug issue is opened by this review because:

- both final result/restore/exit gates passed;
- no Error, Fatal GC, `Crash!!!`, fresh dump or residual process appeared;
- the warning pattern exists in earlier YConsole and Oil evidence.

The open question is whether short player inputs can be lost inside the recovery window, not whether these two runs crashed.

### Known debt reconfirmed, not newly introduced

Core-only and ordinary evidence still show no-consumer feature hosts receiving recurring EveryFrame dispatch. Smoke/QA remains production-coupled. These are the planned Batch 4/5 problems and do not indicate route deviation.

No new major-mainline gameplay failure was found.

## 5. Scheduled Detailed-Decision Nodes

Not every Mod rebuild needs another user questionnaire. OneAction, ChestLocator, FishBreeding, AnimalProgress, StrongPlanting and similar low-user products may proceed through focused Review/Update as behavior-equivalent rebuilds unless evidence exposes mutually exclusive player policies or proposes new scope.

The route should print these actual “decide at the node” points:

| Trigger node | Detailed decision required then | Already frozen now |
| --- | --- | --- |
| ActionSpeed 1.0.0 / Wave 3 | Supported Tool/Interact/Eat/Continuous-use categories, defaults/caps, continuous-action policy, arbitration, disable recovery and config migration after native-owner plus GC evidence. | Separate from AutoFishing; no assumed shared Animator; 0.5.5 preserves old ABI. |
| Mine rebuild in the low-user product wave | Cycle/output/economy, electricity rewrite, storage/catch-up, preview seam, dedicated art, Oil integration, disable/update behavior. | Static content stays official-JSON-led; Mine owns policy/economy; GameBridge only demand-activated native adaptation; runtime 2x art is temporary. |
| MoreEquipment 1.0.0 / Wave 5 | Slot rules, UI/stats/shield stacking, save/downgrade/unsubscribe migration and rollback. | Release last and alone; orphan recovery must be separated and can never be demand-disabled. |
| Manager / MoreSaves / Y-console Batch 8 UX | Information hierarchy and ordinary/advanced wording; future save-name identity/count/scroll migration; later console history/search/danger confirmation/diagnostics. | J1 single player center/no second input store; MoreSaves first preserves fixed 12; Y-console first extracts behavior-equivalently and remains optional. |
| AnimalPack 1.0.0 | Species roles, processor, hidden products, Oil relation, sale/processing rule, content IDs, complete economy and migration. | See the dedicated node below. |
| Manbo after JSON/WAV Canary | Keep the small content product or retire it with notice based on real Workshop migration evidence. | Preserve Workshop/UniqueID for the Canary; JSON author route; C# API retirement still follows warning/consumer evidence. |
| Any public API removal / breaking epoch | Facade/internalize/remove only after public consumer rescan, warning window, migration notes and version gate. | No public ABI deletion in 0.5.5; one local consumer does not justify a stable public promise. |
| BGM, multiplayer, pets, independent vehicles | Each needs an independent charter and native-owner/lifecycle/product decision. | None is an AnimalPack or 0.5.5 promise; short-SFX/animal success is not proof. |

## 6. AnimalPack Decision Node

### Trigger

Open the AnimalPack product Review after the 0.5.5 structural/release gates are stable and before freezing any new item, recipe, processor, icon or public asset. The Catalog already reserves:

```text
UniqueID:       DTMAPI.AnimalPack
OfficialFolder: DTMAPI_AnimalPack
PackageName:    DTMAPI-AnimalPack
TargetVersion:  1.0.0
TargetMinimum:  0.5.5
```

The old draft's `U–Y` labels must be printed as `Animal-U` through `Animal-Y`; mainline sixth-round U/V/W are already closed and mean different things.

### Frozen boundary

- one official-JSON-led ContentPack containing Hatch, Mole, Drecko and Oilfloater;
- Shell Crab remains a separate unpublished AdvancedAssetBundle prototype;
- Lightning Chicken remains NeverPublish retired research;
- all ordinary/hidden outputs are custom items, then official JSON processing/crafting converts them into fixed native resources and recipes;
- GameBridge stores no species probability, quantity, yield or recipe table;
- the new pack preserves or explicitly migrates species/item/LUT/animator/sound identities, and old/new packs cannot be enabled together;
- player Runtime never deletes old packages;
- public PNG/WAV/names/designs must be original or have explicit cross-project permission, with a per-asset provenance/hash inventory;
- the working economy band is stable return around 2–3x a vanilla comparison and long-run return including hidden products around 3–5x, but no exact number is frozen.

### Facts required before choices

1. Create a repository-owned canonical source; reconcile the Hatch external/LocalLow drift and update Oilfloater's old document schema.
2. Run one focused native-JSON processor smoke covering equipment/item, `EquipmentFuncGarbageShredder`, dismantle group/recipe, item-spawn LUT, crafting, placement/preview, UI, power, batch, outputs, capacity/mixed species, save/reload, disable and update.
3. Prove weighted ordinary-product LUT behavior, the hidden-product route, special-feed/trigger semantics and fixed multi-result/count behavior. If there is no independent hidden route, giant egg becomes a low-weight ordinary candidate rather than a species Hook.
4. Prove asset provenance and old/new duplicate preflight. If collision cannot be stopped before native content ingestion, fail visibly before safe play.

### Decisions to request at that time

| Label | Recommended working direction | Alternatives / reason to defer |
| --- | --- | --- |
| Animal-U Hatch | U1 food ladder: normal, mutated and giant eggs form reliable/mid/rare tiers. | U2 breeding needs unproved incubation/animal creation; U3 research/industry widens the first release. |
| Animal-V Mole | V1 soil/agriculture specialist; mineral nodule only as a later candidate. | V2 overlaps Mine; V3 compost/feed risks a self-amplifying loop. |
| Animal-W Drecko | W1 textile-led with controlled meat secondary value. | Depends on multi-result proof; W2 is simpler mechanically but more variable; W3 loses the selected meat use. |
| Animal-X Oilfloater | X1 self-contained fuel animal for 1.0.0; any later crude-oil integration is Oil-owned. | X2 creates dependency/load-order/absence policy; X3 creates a shared package for one prototype identity. |
| Animal-Y sale rule | Y2 modest direct-sale floor plus a tested processing premium. | Y1 is harsh before processor access; Y3 maximizes economy/test variance. |

Then decide the public names/IDs/icons/descriptions, every primary/hidden product and direct recipe, processor name/unlock/power/capacity/batch, probabilities/quantities/prices/bag cost/cycle/occupied-space return/payback, Oil relation, migration/rollback and final assets.

### Recommended AnimalPack sequence

```text
canonical source + provenance
-> native processor/LUT/hidden/multi-result evidence
-> Animal-U/V/W/X species roles
-> processor and Animal-Y sale rule
-> freeze item/recipe/equipment identities
-> economy model and simulation
-> third-save old/new migration, disable/update/save regressions
-> public-asset and 1.0.0 release gate
```

Animal decisions do not block current Batch 2–5 work and should not be answered prematurely.

## Validation

Mainline validation used a temporary detached worktree at exact HEAD `866021b5`, excluding all user-designated dirty work:

1. clean Git-only worktree: build completed with zero warnings/errors, then UnitTests failed because the ignored private reverse item-spawn JSON was absent;
2. exact same worktree with the configured workspace `references/doloc-town/reverse` mounted: `tools/scripts/test.ps1 -Configuration Release` passed in 220.4 seconds;
3. passing gates included `DTMAPI.UnitTests: OK`, runtime-evidence retention, player Runtime-only uninstall ownership, developer official-local transaction tests under both PowerShell hosts, and Catalog checks (`26 products`, `11 public`, `21 Workshop items`, `45 API rows`).

After adding this Review, its Update, the July-ledger row and the two dated evidence qualifications, document governance passed 4,810 checks and `git diff --check` passed.

No game process was launched. Runtime conclusions come from preserved mainline evidence `GAME-SMOKE/20260714-034644` and `GAME-SMOKE/20260714-034949`. The temporary worktree and its junction were removed after validation.

## Handoff

Continue the existing `20260714-0004` Batch 2 lifecycle rather than opening another global design round. First make version/release projection and validation prerequisites explicit, then close minimum policy, ABI, stale-Runtime recovery and all-public-product gates. Keep later product decisions attached to the scheduled nodes above; open AnimalPack only after its required native/content/provenance facts are available.
