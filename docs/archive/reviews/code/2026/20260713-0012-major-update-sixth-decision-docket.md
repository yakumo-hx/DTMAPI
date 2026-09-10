# 20260713-0012 Major Update Sixth Decision Docket

Status: recorded / sixth-round decisions closed / implementation routed to focused Updates
Date: 2026-07-13
Scope: single-consumer public gameplay API governance, migration/Canary role separation, and transition from global decisions to implementation
Proposal Update: `docs/updates/2026/20260713-0006-sixth-round-scope-reclassification.md`
Closure Update: `docs/updates/2026/20260713-0007-sixth-round-closure-and-active-gc-gate.md`
Follows: `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
Reclassified supporting draft: `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md`
Focused GC Review: `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`

## Source Request

The user supplied four screenshots and asked to recalibrate the decision scope against the full audit and first-through-fifth-round problem scale. Detailed animal outputs, economy, equipment naming/unlock and Oil linkage are deferred to the relevant AnimalPack rebuild. The official ordinary-recipe versus dismantle-machine finding receives a brief technical resolution, while the mainline sixth round returns to DTMAPI Runtime/public-contract/release-execution boundaries.

The user then supplied `D:/下载/第六轮正式.md`, selected U1, revised V1 and W1, defined the DTMAPI 0.5.5 release baseline, corrected the active-gameplay GC priority, and ended the global questionnaire. The selected resolution below is authoritative over the proposal wording where it is more specific.

This is docs/source analysis. It does not change public APIs, products, packages, content JSON, game/runtime files, Workshop state or the implementation program.

## Reclassified AnimalPack Input

`docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md` is no longer a mainline decision docket. It preserves non-binding AnimalPack design alternatives and the following brief conclusions:

- ordinary `recipe_tbrecipe` has one item type as output;
- the separate native shredder route is `EquipmentFuncGarbageShredder -> dismantle group -> dismantle recipe -> item-spawn LUT` and can return multiple item types across a configured draw count;
- the current public-build Mod loader is source-proven to merge the required equipment/dismantle/group/spawn tables, but a real AnimalPack package still needs one focused native-JSON smoke;
- the future processor, recipes, probabilities, quantities, power, UI and economy belong to AnimalPack, not Runtime/GameBridge and not a new public machine API.

Animal names, roles, exact output composition, probabilities, prices and Oil integration are not sixth-round questions.

## U - Default Governance For Single-Consumer Public Gameplay APIs

The current API matrix contains several interfaces whose only known real consumer is the matching first-party product and whose methods mirror that product's configuration or UI policy. This does not apply to naturally general Framework, manifest, config, content, event or diagnostics contracts.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| U1 - first-party internal by default | Product rebuilds consume first-party internal capabilities. Existing public ABI remains in 0.5.5 and follows I1 freeze/warning/consumer-scan/conditional-removal. A new/stable public API requires real external demand, native-owner semantics, multi-owner/conflict policy and disable/unload lifecycle proof. | Shrinks long-term Runtime promises without breaking 0.5.5 compatibility or mechanically deleting useful generic APIs. |
| U2 - retain every current gameplay API indefinitely | Keep product-mirror interfaces permanently public Experimental even when only the product consumes them. | Lowest migration pressure, but Runtime/provider/DTO surface remains coupled to first-party product policy. |
| U3 - decide independently during each Mod rebuild | Apply no common default and reopen public-versus-internal ownership for every product. | Flexible, but recreates inconsistent promises and makes 0.5.5 retirement guidance unpredictable. |

Selected resolution: **U1**.

If renewed Workshop/local/external scanning finds a real second consumer, retirement pauses and a narrow demand-driven API is designed rather than deleting the old surface. Zero known consumers is never sufficient deletion evidence.

### 0.5.5 External Compatibility Scope

Before 0.5.5 release, scan every publicly obtainable Workshop Mod which claims DTMAPI as a prerequisite or API provider. The expected ecosystem is small enough for a complete public inventory.

| Observed product | 0.5.5 treatment |
| --- | --- |
| Publicly obtainable and actually consumes a DTMAPI API | Add to the formal binary/behavior compatibility matrix. |
| Publicly obtainable but only declares the prerequisite or uses JSON/content | Verify manifest, minimum-version and content compatibility; do not invent API use. |
| External plugin installed directly under `BepInEx/plugins` | Diagnose through Doctor; do not claim DTMAPI enable/disable/uninstall ownership. |
| Unpublished, unobtainable, or not supplied by its author | Exclude from the default release blocker set. |

The public compatibility statement is:

> DTMAPI 的兼容承诺覆盖公开可取得的 Workshop Mod，以及主动登记并提供测试版本的外部 Mod。未公开或无法取得的二进制不属于默认兼容范围。

The future research item described by the user as “U3” is not a selection of proposal option U3. It is a clean-room architecture study of API addition/deprecation, warnings, demand-driven promotion, multi-owner/event/query/lifecycle contracts, minimum versions and binary compatibility using the historical SMAPI material and Mods under `E:/Python_project/SMAPIlearning`. It may inform a later public-platform review, but no SMAPI implementation is copied.

## V - Separate Compatibility Canary, Structural Template And Protected Migration

The earlier rounds use “Canary” for several different proofs. One product cannot truthfully prove every release, content, architecture and behavior-equivalence boundary.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| V1 - explicit independent roles | Runtime 0.5.5 has internal RCs and one public release. AutoFishing is the old-Runtime/update/recovery Canary; Manbo is the data-only JSON/WAV Canary; OneAction is the structural split template; AutoFishing/Zoom use protected migrations; ActionSpeed follows its active-GC gate. | Each proof remains attributable; low-user products may share a publication window without merging identity or rollback. |
| V2 - one universal Canary | Select AutoFishing, Zoom or OneAction to prove compatibility, structure, content and protected behavior simultaneously. | Fewer labels, much wider blast radius and ambiguous regression ownership. |
| V3 - one combined product migration train | Publish several first-party 1.0.0 migrations as one coordinated milestone. | Faster visible batch when perfect, but one failure blocks all products and obscures old-Runtime recovery evidence. |

Selected resolution: **V1 revised**.

V1 revised refines rather than reopens H1. DTMAPI 0.5.5 may have multiple internal release candidates, but players receive one public Runtime update. Later first-party products declare `MinimumDTMApiVersion = 0.5.5` and use their independent Workshop update paths.

The public waves are:

1. DTMAPI 0.5.5;
2. AutoFishing 1.0.0 alone;
3. ActionSpeed only after the focused active-gameplay GC/native-owner gate;
4. other low-user-count Mods in one publication window;
5. MoreEquipment 1.0.0 alone.

A combined publication window shares the 0.5.5 compatibility announcement and timing only. Each Mod retains its WorkshopID, UniqueID, enable/disable boundary and independent rollback; physical package merging is not implied.

The validation roles remain distinct:

- Runtime 0.5.5 owns install/version/ABI/Doctor/uninstall/compatibility proof;
- AutoFishing is the old-Runtime block/update/recovery Canary and the active-fishing GC sample;
- Manbo is the CodeMod-to-JSON/WAV ContentPack Canary;
- OneAction is the first product-ownership and CodeMod structural-split template after Oil decoupling;
- AutoFishing and Zoom retain behavior-equivalent protected migration gates;
- ActionSpeed receives its own native-action classification, animation-speed and GC-risk project.

## W - End The Global Product Questionnaire And Start Work

The five completed rounds already freeze the architecture, ownership, product/source/version, Author SDK, QA/hot-path, UI, animal-route and short-SFX/BGM directions needed to begin. Remaining product details are better decided by task-specific native-owner/Product Reviews when that Mod is actually rebuilt.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| W1 - close global rounds after U/V/W | Start `Batch 0 Catalog/identity/version projection/API-ABI freeze`, then the player-uninstaller ownership P0 and Oil/OneAction ownership P0 under independent Updates; continue through version/release authority, Author SDK, QA extraction and hot-path/demand work. | Converts agreed boundaries into small attributable implementation streams while later product detail remains available at the right time. |
| W2 - design every first-party 1.0.0 before implementation | Continue global rounds through AnimalPack, ActionSpeed, MoreEquipment, Mine and UI product details. | Delays two confirmed P0 risks and decides products without their focused native evidence. |
| W3 - skip Batch 0 and start changing P0 code immediately | Implement ownership fixes before Catalog/API/identity freeze. | Fastest first edit, but migration, compatibility and release evidence lack a stable baseline. |

Selected resolution: **W1**.

The two P0 streams are independent after Batch 0. If only one can run, the player uninstaller goes first because it is a content-ownership/deletion risk; Oil/OneAction follows under a separate Update. Live Workshop facts may remain `PendingLiveVerification` in the initial Catalog, but they block release/upload until verified rather than blocking the Catalog or P0 code.

This closes the global cross-product questionnaire. AnimalPack economy, ActionSpeed scope, MoreEquipment rules, uninstaller implementation, Manager/Y UI and future platform projects go to their focused Review/Update when scheduled. The desired end state remains a coherent rebuild, but execution uses reversible batches because public identities, old ABI, Workshop products and protected player behavior already exist.

## Post-Closure DTMAPI-Wide Decision Audit

No additional DTMAPI-wide decision is currently required before Batch 0. The remaining apparent unknowns already have an evidence route:

- Catalog identities, live Workshop versions, ABI diff and AssemblyVersion binding are fact verification/release gates;
- AutoFishing/ActionSpeed GC, AnimalPack economy, MoreEquipment recovery, Mine, Manager/Y and other products belong to focused native-owner/Product Reviews;
- strict SemVer, a broad public content pipeline, future API promotion, BGM, multiplayer, pets and vehicles are separate future projects, not 0.5.5 promises;
- discovery of a real external API consumer automatically pauses retirement under U1 and does not require a new global vote.

A new top-level decision is requested only if evidence exposes a contradiction which the selected rules cannot resolve—for example, real Unity Mono proof that 0.5.5 cannot preserve the frozen ABI/identity, an unavoidable public identity collision, or either gameplay-domain GC result which requires changing promised player behavior rather than fixing/isolating implementation. Otherwise implementation continues under the owning focused Review and Update.

## Final Decision Set

```text
U = U1  single-consumer product gameplay capabilities become first-party internal by default;
        0.5.5 keeps old ABI; I1 warnings/scanning govern later retirement;
        the public Workshop ecosystem is scanned before release

V = V1 revised
        one public DTMAPI 0.5.5 update, then independent product waves;
        AutoFishing alone, ActionSpeed after the active-GC gate,
        low-user products in one window, and MoreEquipment alone

W = W1  finish global decision rounds and start Batch 0 -> two P0s -> release authority

GC strategy = staged per-product certification (formal response: G2 revised)
              AutoFishing active play and ActionSpeed are parallel first-priority gameplay gates;
              all other Mods receive their own profile before their 1.0.0 release
```

## DTMAPI 0.5.5 Release Baseline

DTMAPI 0.5.5 is:

> 第一个统一身份、版本、内容所有权、兼容提示、轻量玩家包和 GC 验证规则的正式发布基线。

It is not release-ready until all ten gates are complete:

1. Catalog, product identity and version projections;
2. player-uninstaller content-ownership P0;
3. Oil/OneAction ownership P0;
4. unified current, minimum and installed version semantics;
5. complete compatibility scan of the publicly obtainable external DTMAPI ecosystem;
6. zero old-ABI removal in 0.5.5, with U1 candidates frozen and warned;
7. no QA/Smoke in the player package and no meaningless player file polling;
8. demand activation so a feature with no consumer installs no dedicated Hook/work;
9. the parallel AutoFishing and ActionSpeed active-gameplay GC gates;
10. clear old-Runtime block, update and recovery messages.

The GC priority is a release-risk ordering, not a root-cause finding. AutoFishing and ActionSpeed independently accelerate different gameplay domains and may amplify the same class of Unity/Mono pressure; no current evidence shows shared Animator ownership. The current issue has Fatal evidence with AutoFishing disabled, and inactive AutoFishing sustained retention has been cleared for the current build. The separate per-domain speed ladders, corrected GC-H1 through GC-H4 hypotheses and safe claim wording are owned by `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md` and `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`.

## Work-Order Effect If Selected

```text
Batch 0   Catalog, product/source identities, version projections, API/ABI/status freeze
P0-A      player uninstaller content-ownership correction
P0-B      Oil official JSON ownership and OneAction decoupling
0.5.5     version/release authority, QA extraction, recurring-work/demand activation,
          public ecosystem scan and active-GC gate
Program   the Author SDK continues on the selected D1 lane but is not silently added
          as an eleventh 0.5.5 release gate
Wave 2    AutoFishing 1.0.0 alone
Wave 3    ActionSpeed after its focused native-owner/GC gate
Wave 4    low-user products in one publication window with independent rollback
Wave 5    MoreEquipment 1.0.0 alone
Products  every Mod rebuild receives its own Review, Update, compatibility and runtime gates
```

AnimalPack processing research, Mine economics, Manager/Y UI details, BGM, pets, vehicles and multiplayer remain separate later projects and do not delay these steps.

## Validation Boundary

This docket cross-checked the formal sixth-round response, full boundary audit, first-through-fifth decision records, current product Catalog facts, public API matrix, first-party consumer evidence, Oil/OneAction ownership review, AnimalPack/dismantle native route, current AutoFishing/ActionSpeed source boundaries, ISSUE-010 evidence and the ordered implementation program. It did not build, launch the game, acquire the runtime lock or change Runtime/API/product/package/Workshop behavior.
