# Batch 6 Managed Mod Identity And Phase 0 Contract

Status: `Phase 0/G2 and the admitted product work in the 0.5.5 release set are verified/closed; MoreEquipmentSlots ProductNative is implemented/publication-deferred and its open transaction findings remain next-version work; every later product and G7 remain blocked; Runtime 0.5.5 plus ten product trees are frozen as eleven exact existing-item updates and authorized only for those Workshop items by Update 20260801-0002; IEquipmentSlotsApi/Compatibility continues to support the retained Workshop 0.3.1-dtmapi package, and no Steam upload has been performed`

Date: 2026-07-27

This document projects the canonical identities and ownership categories from
[`PROJECT.md`](../../PROJECT.md) onto the manifest, Loader, Author SDK, Doctor,
Manager, package and UI surfaces. It does not redefine those identities. The
machine-readable companion is
[`batch6-g0-mod-identity-contract.json`](../../tools/release/contracts/batch6-g0-mod-identity-contract.json).

## Scope And Hard Stop

Phase 0 closed architecture authority, current-state classification, the G1
ownership baseline, the G2 atomic-slice design, Batch 5 route classification,
and the 0.5.5 consumer/compatibility plan. Its correction changed only pre-G2
fail-closed rejection across the schema, Author SDK, Core and Doctor so the
reserved `CodeModKind` field could not survive or be ignored; it did not itself
implement the Advanced discriminator, package, or Runtime path.

The later G2 atomic slice is now verified. Its implementation is frozen at
`c79306dfc7de0e85c74e24ece7d4c5cd47cb0822`; the formal runtime receipt file
has SHA-256 `715BDF227065009D5537613EC905F09A172345E05E5C1AB59E998F18DCCE1F60`
and matrix SHA-256 `EC26CED5E689A0839B438FC4C421BDD032AFD26FC308C17C20936812BC95BAD3`;
the ownership receipt file has SHA-256
`DC222D92FB3A21A6778D6E0377E1AD22F359275E82F2C0A64151925EEB6A64D8`
and source-diff SHA-256
`CF60A5BC51A49355C3A038A111092B0E5D4A16ACCA3152A2E12C3BC5D95695E6`.
This proves the managed lane only for the SDK-generated synthetic
`DTMAPI.AdvancedFixture` bound to tracked policy
`doloctown-23762374-g2-v1`. It does not open general Advanced authoring.

The active hard stops are:

- `SDK160` remains enforced for Strict CodeMods;
- `Type` remains limited to `CodeMod` and `ContentPack`, with `CodeModKind`
  orthogonal to `Type`;
- Advanced manifests, reference receipts and packages must be produced by the
  SDK; hand-authored or hand-packaged bypasses are unsupported;
- AutoFishing is the first runtime-verified real-product pilot and its corrected
  D.5 slimming/deep-state checkpoint is closed;
- OneActionComplete is the admitted second real product; `GAME-SMOKE/20260722-141220`
  closes partial-energy, ConfigMenu save/reload and actual owner-deactivation gaps;
- ActionSpeed is the admitted third real product and the same `141220` process
  verifies its corrected nine-target package, behavior, title lifecycle and exact owner cleanup;
- FishBreedingAssistant is the admitted fourth real product. Its original
  ProductNative fallback passed `145154`; after Animal established a second
  native-title consumer, only that lookup moved to `IItemDisplayNameApi`.
  `GAME-SMOKE/20260722-180502` verifies the frozen corrected package's one owner
  patch, shared native lookup, `鱼卵 (鱼)` result and exact owner deactivation;
- AnimalHusbandryProgress is the admitted fifth real product. Focused source,
  SDK/package, Compatibility, Catalog and QA owner gates pass. The same `180502`
  third-save run verifies four patches across three targets, a visible read-only
  `羊毛脂 0/100` row and native-style clone, native panel-close cleanup, exact
  owner deactivation, restoration and clean exit. The earlier `153320`/`153845`
  windows remain Steam cloud-conflict infrastructure evidence; `174252`/`175832`
  are retained non-acceptance runs that exposed the corrected QA-only
  native-UI-close/ReturnHome quiescence requirement;
- `IItemDisplayNameApi` positive-only caching, main-thread enforcement and
  save/title cleanup pass focused source/Unit checks. A successful lookup arms
  the shared `SetEnvCamera` lifecycle route without Camera updater demand, but
  no result is cached until the independent SharedNative physical Hook owner
  reports ready. The post-commit fanout, Camera-only gate, Hook-install window
  and Camera-disable gaps are source-closed; `180502` remains product-behavior/
  owner evidence, while current-DLL `GAME-SMOKE/20260723-074311` proves both
  product queries, shared Hook readiness, two real callback clear/releases,
  title recovery, re-query, restoration and clean exit;
- MoreSaves alone completed the bounded sixth-product SDK/policy/package switch
  authorized by Review `20260723-0002` after Update `20260723-0003` closed its
  Host, frozen ABI and both-order writer prerequisites. Current-DLL
  `GAME-SMOKE/20260723-150215` closes independent review with initial and
  post-title official twelve-slot panels, third-save load, zero actual Harmony
  patches, real Loader owner deactivation to native six/zero roots, exact
  36-path save restoration and byte-unchanged protected extra slots. Commit-range
  audit `20260723-0003` then reopened the baseline until focused corrections
  required the 0.5.5 Host in Doctor/Status, restored frozen SaveSlots state
  semantics, removed the healthy per-frame subscription, separated the three
  product-version axes and covered the two component move/place transaction
  windows. Those focused gates pass without changing the retained game evidence;
  Updates `20260723-0003` and `20260723-0004` are `verified/closed`;
- the independent seventh-product Review admits only ChestLocatorEnhancer.
  Its current Advanced package owns one exact
  `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`
  Postfix; the existing Host owns the frozen old ABI executor.
  `GAME-SMOKE/20260723-203127` passes native placement/CostItem behavior,
  title recovery, HookProbe and real Loader deactivation from one patch/
  callback to zero roots; `GAME-SMOKE/20260723-210424` adds two same-process
  third-save re-entry cycles with one retained exact owner and no duplicate
  install or handler failure. Commit-range corrections then pass real Harmony
  dual-order, executable traversal, allocation/log and Catalog-driven
  transaction checks; `GAME-SMOKE/20260723-224200` binds final Runtime commit
  `7ace68260cb1` and corrected product DLL provenance while re-proving behavior,
  title recovery, exact-owner cleanup and clean exit. Independent Review
  `20260723-0006` plus commit-range Review `20260723-0007` pass;
- Review `20260723-0008` selected only MoreEquipmentSlots for an independent
  eighth-product admission decision, and Review `20260723-0009` admitted
  exactly that identity. The commit-range findings are closed by
  `b0ef85a9`/`034ea5e6`. Final enabled
  `GAME-SMOKE/20260724-053248` and cold-recovery `053342` pass current Doctor
  and restoration gates. One from-start Release reached a stale allowlist
  after all earlier gates passed; the focused repair and exact unreached tail
  then passed without repeating the complete suite. A later user-confirmed
  save-semantics Review found a P1 that those cases did not cover. The fix now
  keeps ordinary shield/equip/replace/unequip state in Working memory until a
  successful native save promotes Committed state, while typed
  Owner/OrphanRecovery remains persistently retryable. Focused Product/Host
  fault tests, third-save `NoNativeSave` `155216`/`155344` and isolated
  native-save `161422`/`161536` pass for their exercised paths. Independent
  Review `20260724-0006` then reopened runner isolation and the missing real
  ProductNative damage/break/replace/unequip matrix. Those findings are closed
  by focused root/reparse rejection plus exact-final-candidate
  `GAME-SMOKE/20260724-202032`, cold `202146` and AutoCloud-isolated
  native-save `202526`. Review `20260724-0007` kept those covered paths but
  required a non-empty Committed rollback proof. Current-commit
  `GAME-SMOKE/20260724-222231` normally saves one durability-80 shield;
  `222329` and `222423` exercise no-save damage, break, replacement and
  unequip; final cold read `222516` proves the exact durability-80 committed
  shield, exactly one logical item, zero candidate/journal, unchanged archive/
  sidecar metadata before cleanup and disposable-fixture removal;
- Review `20260724-0003` now admits exactly
  `DTMAPI.StrongPlantingGunMod` for one ninth-product implementation. Update
  `20260724-0001` has completed that atomic source/Catalog switch and
  `GAME-SMOKE/20260724-101757` passes the current-DLL protected third-save
  native-save/title/JSON-reentry and real Loader owner-deactivation route.
  Direct SaveLoaded success/failure fixtures and the independent no-P0/P1/P2
  closeout also pass, so StrongPlantingGun itself is frozen verified/closed.
  The implementation binds the single tracked
  consumer, native saved `ItemFarmingGun.inventory`, shared function-capacity
  restoration and five concrete Harmony patches; it adds no Host or
  SharedNative. `DTMAPI.MineMod` remains
  `split-decided` / `PrototypeBlocked`;
- Review `20260724-0005` independently admits only `DTMAPI.ZoomMod` for one
  tenth-product switch. Update `20260724-0002` completes it: ProductNative owns
  scale/config, orthographic-size snapshot/restoration and one exact
  `SetEnvCamera` postfix; retained CameraView/CameraZoom ABI execution lives
  behind the existing Host. The final ABI gate locks all 35 MemberRefs from the
  exact retained Zoom DLL. Focused dual-owner/SDK/package/Doctor and Loader-zero
  gates pass. Review `20260724-0006` reclassified
  `GAME-SMOKE/20260724-180233` as non-acceptance because the real
  `SetEnvCamera` call compounded the already-scaled camera baseline. The
  corrected exact-final-candidate `GAME-SMOKE/20260724-202032` preserves native
  `16.875`, holds repeated real 4x callbacks at `67.5`, restores every 1x/title/
  Loader path for its covered orthographic-size state. Review
  `20260724-0007` then required native derived-state recovery and truthful
  maximum-to-1 failure. The final current-commit
  `GAME-SMOKE/20260724-221902` reproduces resolution/fullscreen refresh at
  2x/4x and proves exact orthographic, `camSize`, x-range and y-range recovery
  at 1x/config-disable/title/Loader cleanup, with transactional failure
  propagation and exact owner/root zero.
  Manual QA later found that actively refreshing derived controller state at
  4x changed the intended Zoom semantics. Update `20260801-0001` keeps the
  admitted ProductNative owner but narrows it back to orthographic presentation:
  one `SetEnvCamera` Postfix plus a `RefreshResolution` Prefix/Finalizer are
  installed atomically, native refresh runs against 1x, and no controller field
  is product-written. Final combined player QA passes and the exact accepted
  tree is verified/closed by Updates `20260801-0001` and `20260801-0002`.
  At that checkpoint DebugConsole remained rejected because its then-current
  Core-modal/GameBridge-input boundary had no existing Advanced seam. No new
  Host, public API or SharedNative was added by Zoom;
- Review `20260726-0002` admits exactly `DTMAPI.MineMod` as the eleventh
  product. Update `20260726-0004` moves its runtime cycle, weighted output,
  fixed native-power transaction, optional recipe/tech mutation and three
  visual hooks into one Advanced ProductNative assembly. Official JSON remains
  ContentOwner for the item, equipment, recipe/group, 16-slot native case and
  native power threshold `10`. The scheduler is deliberately session-derived:
  `SaveLoaded` starts from current native time and title, disable or Loader
  cleanup discards it. No Mine sidecar, Compatibility Host executor, public
  machine provider or SharedNative capability exists. Focused transaction,
  package and ABI gates pass. `GAME-SMOKE/20260726-205813` remains narrow
  one-charged-Mine evidence. Review `20260726-0003` then reopened scheduler
  identity/pruning, preflight ordering and the missing native matrix. Those
  findings are closed by a specialized object-identity scheduler and
  preflight-before-mutation path: cold-disabled `223105` records
  `active=False/hooks=0/3/scheduler=0`, and final restored-enabled `223236`
  passes two-Mine independence, low power, `16/16` full storage with no power
  consumption, hot disable/re-enable, move, dismantle, reused-index fresh
  identity, title cleanup and Loader
  `instance0+actual0+callback0+roots0`. The borrowed well sprite and runtime
  2x Hooks remain an unpublished `RebuildBlocked` prototype. Independent
  Review `20260727-0001` retains this game matrix but reopens Mine to
  `implemented/open`: one recipe/tech restoration exception discarded its
  closure and could make a later cleanup report a false zero. The focused
  correction retains only failed closures for retry. The rebuilt SDK package
  contains the corrected 86,528-byte entry DLL; independent Review
  `20260727-0004` accepts the lifecycle and exact bytes without another Mine
  game run, so the product is verified/closed. Its art/2x publication blocker
  remains separate;
- the DebugConsole prerequisite Review
  `20260726-0002-debugconsole-admission-prerequisite-review` and Update
  `20260726-0005` implement exactly `DTMAPI.DebugConsoleMod` as the twelfth
  product. ProductNative owns typed SaveLoaded Y/Escape input, the owner-bound
  modal/Canvas, three input-isolation Prefixes, bounded native actions and
  reversible leases under Harmony owner
  `dtmapi.mod.dtmapi.debugconsolemod`. The product consumes no frozen
  Diagnostic API and exposes no command parser. The exact old 0.3.1 provider,
  UI/action adapter and separate Harmony owner remain lazy in the existing
  optional Compatibility component. Mandatory Runtime retains only generic
  Platform input/modal/lifecycle coordination and a thin lazy proxy.
  Current ProductNative `GAME-SMOKE/20260727-083919`, isolated
  `NativeSaveExpected` `090702` and exact-current old 0.3.1/current-Host
  `132103` respectively pass real product actions/title cleanup, two-click
  Save here failure/success behavior, and all frozen Compatibility action
  groups with bounded Hook edges and title/Loader zero. Review
  `20260727-0004` independently accepts the final lifecycle ownership, so the
  product is verified.
  Update `20260801-0001` leaves that admission, ABI and executor ownership
  unchanged but hides the unfinished generator/monster/resource controls and
  their adjacent status text from the new player UI. The corrected combined
  candidate passes final player QA and is verified/closed; old Compatibility
  consumers remain callable under their existing author-managed semantics.
  Every thirteenth-or-later product remains blocked;
- the general `ContentPack -> Content Host` relationship remains a separate G7
  blocker. It neither expands nor revokes the byte-bounded 0.5.5 existing-item
  exceptions; every other release mutation remains blocked.

## Current Physical Topology

The current 0.5.5 acceptance package places exactly five default-loaded DTMAPI
Runtime assemblies in `BepInEx/plugins/DTMAPI`:

1. `DTMAPI.BepInExBootstrap.dll` — the only assembly containing the DTMAPI
   `[BepInPlugin]` entry;
2. `DTMAPI.Abstractions.dll`;
3. `DTMAPI.Core.dll`;
4. `DTMAPI.GameBridge.DolocTown.dll`;
5. `DTMAPI.ModConfigMenu.dll`.

The latter four are co-located Runtime dependencies, not External plugins.
Ordinary managed CodeMods and ContentPacks are discovered from DTMAPI-managed
game `Mods`, official local `MODS`, or native-verified Workshop sources; they
are never installed as extra BepInEx plugin entries. The five-DLL shape is the
current default-loaded 0.5.5 tree, not a permanent package-file-count promise.

The same player package additionally carries exactly one optional framework
component at
`DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll`.
It is `dormant-shipped`: it has no manifest, Workshop identity or
`BepInPlugin`, lives outside every BepInEx auto-scan path, and is not loaded by
an ordinary process with no frozen-ABI consumer. The mandatory GameBridge has
  no static AssemblyRef to it. On the first real call through one of the nine
frozen compatibility APIs, a single broker validates the Catalog-owned exact
relative path, length, SHA-256, simple assembly name, version and
`netstandard2.0` target before `Assembly.Load(byte[])` and one well-known
factory call. Missing/mismatched bytes or incomplete construction fail closed
before demand, callbacks or Hooks are published.

For `DTMAPIVersion >= 0.5.5`, `install-state.json` and
`release-manifest.json` must each project exactly this one Host row. Dual
omission is an inconsistent installation, not a legacy-compatible green state.

After activation, per-domain owner and lifecycle cleanup still releases all
product state, demand, callbacks and exact-owned Hooks. Unity Mono cannot
unload the byte-loaded assembly, so the truthful terminal state is
process-resident and dormant, never `unloaded`. Shipping this component may
reduce the default-loaded GameBridge/Runtime volume; because the same bytes
remain in the player package, it does not reduce Workshop download size.

## Classification Matrix

This table preserves the Phase 0 input distinctions and records the bounded G2
result. The synthetic PASS does not turn legacy provenance into SDK proof.

| Input or location | Current behavior | Active interpretation |
| --- | --- | --- |
| Explicit `Type=CodeMod`, explicit `CodeModKind=Strict` | The classifier selects Strict and Core loads only after Strict entry/reference/closure validation | Declared Strict. This does not prove the package was built by the Strict SDK. |
| Explicit `Type=CodeMod`, omitted `CodeModKind` | Core classifies it as `Third-party native compatibility CodeMod`; direct native references and private helper DLLs do not block cold-start | Transitional legacy Runtime classification, author-managed and restart-required; it is not an authorable fifth `CodeModKind`. |
| Missing or blank `Type` and omitted `CodeModKind` | Core normalizes it to a legacy `CodeMod` input and uses the same compatibility classification | Pre-Type legacy input with unverified provenance; never inferred Strict or Advanced. |
| `Type=ContentPack` with no DLL | Core publishes a non-code owner | Current ContentPack; generic host ownership remains G7-blocked. |
| `ContentPack` with an entry DLL or `CodeModKind` | SDK, schema and Core reject it before owner publication or assembly load | Invalid; Advanced remains a CodeMod kind, not a content identity. |
| Unknown non-empty `Type` or `CodeModKind` | Schema, SDK, Doctor and Core reject it before publication/load | Invalid. It is never an inferred Advanced declaration. |
| `Runtime` / `RuntimeApi` provider manifests | Internal process-lifetime providers | Internal Runtime types, not author package identities. |
| Third-party plugin entry under `BepInEx/plugins` | Not managed by the DTMAPI Mod loader | External BepInEx Plugin; read-only diagnostics only. |
| The five default-loaded DTMAPI Runtime DLLs under `BepInEx/plugins/DTMAPI` | Installer-owned Runtime | DTMAPI Runtime; only Bootstrap is the plugin entry. |
| `DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll` | Catalog-owned dormant-shipped optional framework component | Not a Mod, provider or plugin entry. It may be byte-loaded only after a real frozen-ABI call and exact package/identity validation; it remains resident-dormant after cleanup. |
| `Type=CodeMod`, `CodeModKind=Advanced` | Accepted only with the versioned SDK intent, exact Advanced reference receipt, compatible tracked game/reference policy and verified package identity | Bounded managed Advanced lane. G2 proves `DTMAPI.AdvancedFixture`; eleven admitted real products are verified/closed, while MoreEquipmentSlots remains admitted but implemented/publication-deferred with its transaction findings open for a later version. Product evidence is identity-specific and does not open another identity or general authoring lane. |

One tracked product manifest currently omits `Type`:
ManboCardboardAudio. MoreSaves, DebugConsole,
ChestLocatorEnhancer, MoreEquipmentSlots and StrongPlantingGun now explicitly
declare `Type=CodeMod`, `CodeModKind=Advanced`; Mine does too. The omission is an
observed author-managed compatibility input, not a template recommendation. Runtime must not infer
Advanced status from an assembly reference, source text, `EntryDll`, package
path or missing SDK receipt.

## G2 Wire And Bounded Live Contract

G2 keeps the existing `Type` axis orthogonal to execution risk:

```json
{
  "Type": "CodeMod",
  "CodeModKind": "Advanced"
}
```

The authorable discriminator is `CodeModKind` with exactly `Strict` and
`Advanced` values. Omission selects the transitional third-party native
compatibility classification, not Strict. The live parser, classifier, Doctor
and Manager project that author-managed/restart-required boundary consistently;
the SDK continues to author explicit Strict or policy-bound Advanced, and an author must select
Advanced through the versioned SDK project and reference-policy flow rather
than hand-writing the manifest value.

The Author SDK remains `projectKind=CodeMod` and schema 2 adds the orthogonal
`codeModKind` intent plus explicit `targetDtmApiVersion`. Schema-1 Strict
compatibility retains its historical `targetRuntimeVersion` field. The live
Loader and package projection bind schema-2 Advanced intent to the exact
reference receipt and policy; `SDK160` still rejects direct native references
from Strict projects.

Unknown `Type`, unknown `CodeModKind`, `ContentPack` plus code fields,
`CodeModKind` on a ContentPack, a missing Advanced reference receipt, or an
unverifiable game/reference build must fail before:

1. `Assembly.LoadFrom`;
2. owner-root or API publication;
3. ContentManifestRegistry publication;
4. Harmony patch installation;
5. `DtmMod.Entry`.

## Component Projection

| Surface | Phase 0 fact | Verified bounded G2 projection | Active guard |
| --- | --- | --- | --- |
| Manifest | `Type=CodeMod|ContentPack`; missing `Type` is legacy CodeMod input | Explicit Strict/Advanced retain their versioned meaning; omitted kind selects the non-authorable legacy native compatibility classification | Advanced is never inferred and cannot be declared on ContentPack |
| Classifier/Loader | Several predicates previously disagreed | One classifier produces identity, provenance, placement, compatibility and restart policy before any load/publication; legacy native inputs are cold-loaded with owner-attributed Entry isolation | Explicit Strict/Advanced remain fail-closed; legacy Hook/static/save cleanup remains third-party author-owned |
| Author SDK | `projectKind=CodeMod` was Strict and `SDK160` blocked direct native references | Schema 2 expresses Strict/Advanced intent; both remain `netstandard2.0` | `SDK160` remains for Strict; Advanced is limited to tracked receipt-bound policy |
| Native references | Strict uses the hash-fixed Abstractions/netstandard payload | Advanced resolves an explicit local, hash-receipted game/Harmony reference set; omitted-kind legacy inputs may directly reference native assemblies and carry private helper DLLs without receiving Advanced guarantees | G2 proof remains synthetic-specific; every admitted product has a separate product-bound policy for build `23762374` |
| Game compatibility | `MinimumGameVersion` can warn and continue for existing lanes | Advanced binds a verifiable game/reference build and rejects unknown/mismatch before load | The G2 proof is for game build `23762374`, not arbitrary future builds |
| Doctor | Previously one DTMAPI CodeMod artifact kind | Versioned report exposes managed identity, declaration/provenance, placement, native risk, reference/game compatibility and restart policy, including legacy author ownership | Direct native references/private helpers are informational for legacy, but remain errors for explicit Strict |
| Manager/UI | Previously raw `Type` without first-class identity/risk | Rows distinguish Strict, third-party native compatibility, Advanced native risk, ContentPack host state, and read-only External state | Legacy rows state author-managed cleanup and restart; UI does not claim hot unload |
| Package/deploy | Previously one code DLL plus manifest | Receipt/journal bind identity and exact reference/payload; bundled game/Harmony/native files and ambient probing are forbidden | SDK-generated receipt/package chain is mandatory for Advanced |
| Lifecycle | Loaded Mono CodeMods are restart-bound for assembly replacement | Advanced defaults to restart-required after load; omitted-kind legacy code is also restart-bound but has no DTMAPI promise of unknown Hook/static cleanup | G2 proves Advanced diagnostics and bounded cleanup, not a security sandbox; legacy side effects remain author-owned |
| Content Host | Current domain-specific scanners exist | `ContentPackFor`, host version and owner-scoped helpers belong to G7 | G2 must not conflate Advanced with Content Host |

`AdvancedAssetBundle` in the product Catalog is a historical ContentPack asset
category, not an Advanced CodeMod identity. Product type, source, placement,
status, capability and managed identity remain separate axes.

## Harmony And Native-Side-Effect Contract

The G2 fixture and every later Advanced product must use the canonical Harmony
owner:

```text
dtmapi.mod.<UniqueID lower-cased with invariant rules>
```

The SDK emits the expected value into build/package metadata. Runtime snapshots
Harmony ownership around `Entry`, rejects or quarantines attributable unexpected
owners, and publishes a restart-required diagnostic when cleanup cannot be
proven. Post-entry audits detect attributable later owner drift while leaving
unattributed sibling-owned patches isolated. A failed Entry must
attempt expected-owner unpatch and generic DTMAPI-root cleanup, retain the
failure, and require restart when arbitrary native state may remain.

This is management and diagnostics, not a security sandbox. The Advanced Mod
owns its statics, native objects, caches, patches and cleanup. DTMAPI never
claims that source scanning or Harmony unpatch alone reverses every side
effect.

## G2 Minimal Vertical Fixture

G2 used one synthetic `DTMAPI.AdvancedFixture`, never a real product. Its
focused Review named the harmless read-only native responsibility
`DolocAPI.Has087DemoData()` and fixed game build `23762374`. The verified
fixture proves the bounded lane end to end:

- versioned manifest and author-project intent;
- verified local native-reference receipt and `netstandard2.0` build;
- deterministic package with one entry DLL and no copied native dependencies;
- discovery from a DTMAPI-managed source, dependency sorting and pre-load
  classification;
- one minimal native reference and one patch under the canonical Harmony owner;
- `Entry`, DTMAPI log, config and Manager/Doctor identity rows;
- enabled cold start, cold-start disabled, post-load disable/restart-required,
  source update/restart-required and clean restart;
- malformed kind, unknown kind, reference mismatch, game-build mismatch,
  wrong Harmony owner, Entry failure and sibling-Mod isolation;
- report/log collection, package tree/hash receipt, cleanup receipt and no
  leftover `DolocTown.exe`.

The fixture used the third save. AutoFishing remains on its separate fifth-save
behavior/GC authority and was not part of any G2 synthetic case.

The nine accepted cold/lifecycle cases are bound by the runtime matrix receipt:
WrongOwner, DuplicatePatch, EntryFailure, LateOwnerDrift, disabled cold,
normal v1 cold, post-load disable, live SDK v1-to-v2 update, and final v2 clean
restart. The receipt excludes rejected or aborted exploratory runs; a successful
synthetic run is not evidence that AutoFishing behavior or GC is correct.

## AutoFishing Real-Product Pilot

The [AutoFishing Advanced Pilot Update](../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)
is now `verified`. Its independent policy and SDK package, self-contained
ProductNative owner, mandatory-Runtime zero-leftover gate, consolidated G4/G5/G6
migration evidence, Manager/behavior/lifecycle matrices, unchanged-package
L0-L5 composite, retained-binary Unity Mono canary, clean recovery and final
complete Release suite all pass. The exact package hashes, evidence roots,
baseline/implementation refs and validation commands remain owned by that
Update and its linked receipts.

This closes G3/G4 and only the AutoFishing-relevant parts of G5/G6. It does not
close G5/G6 globally. Independent acceptance promoted only this pilot to
`verified`; the result does not admit another real product, open general
Advanced authoring, implement G7 or authorize 0.5.5 publication. The later D.5
product-slimming checkpoint remains inside the same admitted product and does
not require replaying historical L0-L5 evidence unless its changed risk does.

The [D.5 slimming Update](../updates/2026/20260721-0004-autofishing-product-slimming.md)
later reduced the player source from 23 files / 6,266 physical lines / 5,765
non-empty lines to 22 files / 5,393 physical lines / 4,964 non-empty lines
(`-13.93%` physical and `-13.89%` non-empty), kept the 22-Hook inventory and frozen compatibility boundary, and passed
  one corrected non-authoritative short fifth-save start/stop/title/re-entry
  regression whose on-demand observer reported zero for every enumerated
  product-held input/Ready/Animator/Hook/Pull override or snapshot holder.
That bounded regression is not a replacement for the historical L0-L5 or release
evidence, does not claim a census of every game-owned native object, and did not
publish 0.5.5.

## OneActionComplete Second Real Product

The [admission Review](../reviews/code/2026/20260721-0005-oneactioncomplete-second-product-admission-review.md)
accepts exactly `Yuuka.DTMAPI.OneActionComplete` as the second real managed
Advanced product. It binds Workshop item `3742763540`, version
`1.1.2-dtmapi`, build `23762374`, policy
`doloctown-23762374-oneactioncomplete-v1` and canonical owner
`dtmapi.mod.yuuka.dtmapi.oneactioncomplete`.

The product now owns resource validation/remaining-hit energy accounting,
`DungeonResource._Fell`, fuel/feeder consumption and its atomic two-Postfix
inventory. It has no GameBridge dependency. `IActionCompletionApi` and its DTOs
remain a demand-inactive frozen compatibility executor for already-built Strict
consumers; the new product does not consume that API. Compatibility/product owner
reconciliation is fail-closed in both load orders before physical Hook install.
Focused source/native anchor, SDK validate/build/pack, Unit, Catalog and
install-transaction gates pass. The bounded third-save acceptance then loaded the
final Advanced package, observed exactly two product patches, passed resource,
wrong-tool, fuel/feed, ActionSpeed coexistence and foreground physical F11, and
restored title/save/QA/deployment/process state. The later corrected acceptance
`GAME-SMOKE/20260722-141220` also proves partial-energy behavior, ConfigMenu
save/reload and actual Loader owner deactivation from two exact patches plus the
callback/instance/Core roots to zero. The second-product boundary is verified.

The [two-product comparison](../reviews/code/2026/20260721-0006-autofishing-oneaction-platform-comparison.md)
found no common native owner between the fishing and action-completion domains.
Only the Catalog-driven Advanced builder/package/validation and live zero-leftover
projection were generalized as Platform; no SharedNative adapter or public API
was added.

## ActionSpeed Third Real Product

The [admission Review](../reviews/code/2026/20260722-0002-actionspeed-third-product-admission-review.md)
accepts exactly `Yuuka.DTMAPI.ActionSpeed` as the third real managed Advanced
product. It binds Workshop item `3742763309`, version `1.3.4-dtmapi`, build
`23762374`, policy `doloctown-23762374-actionspeed-v1`, and canonical owner
`dtmapi.mod.yuuka.dtmapi.actionspeed`.

The product owns action timing, continuous-use scaling, Animator acceleration,
animal-interaction markers, automatic bottle-fill scheduling and restoration. It
pre-resolves and atomically installs nine Hooks. The new product does not consume
`IActionSpeedApi`; that exact ABI and its executor remain frozen, demand-inactive
Compatibility for already-built Strict consumers. Product-first and pending
compatibility-first ownership are both rejected before duplicate installation;
an already physically installed compatibility owner makes product Entry fail
closed until restart.

Catalog-driven Advanced validation/build/packaging, Doctor/Manager projection,
install/uninstall ownership and live zero-leftover checking now have five real
consumers and remain Platform. ConfigMenu glue, lifecycle state, Harmony owners,
native state machines and QA observers remain product-specific. Although
OneActionComplete and ActionSpeed both patch `AgentStateInteract.OnExit`, they do
not share state or a write/restore invariant, so no SharedNative dispatcher or
new public API was introduced.

Focused source, Unit, SDK, Catalog, package and installer checks pass. The first
bounded third-save attempt at `GAME-SMOKE/20260722-100419` is explicitly
non-acceptance: source orchestration loaded the retained old Strict package, and
the G6 title cycle interrupted G5. After those harness corrections,
`GAME-SMOKE/20260722-125239` selected the actual receipt-verified Advanced DLL
but failed closed during atomic pre-resolution: `AgentStateBase` is a global
type in build `23762374`, while the product named `DolocTown.AgentStateBase`.
No product Hook had been installed; Core rollback completed with zero cleanup
failures, and save/config/source/profile/QA/process restoration passed. The
installer, QA inventory and Hook map now use the correct global name, and the
corrected SDK package passes focused checks. No corrected-package rerun had
occurred at that checkpoint. The later corrected run `GAME-SMOKE/20260722-141220`
passed Tool, ConfigApply, Interaction, title/restoration and exact real Loader
deactivation from nine actual patches/targets plus one callback and one loaded
instance to zero patches/targets/callback/instance/Core roots. It also closed the
three OneActionComplete continuation gaps. This third-product boundary is verified.

## FishBreedingAssistant Fourth Real Product

The [admission Review](../reviews/code/2026/20260722-0006-fishbreedingassistant-fourth-product-admission-review.md)
accepts exactly `Yuuka.DTMAPI.FishBreedingAssistant`, Workshop item `3742763706`,
version `1.1.3-dtmapi`, policy `doloctown-23762374-fishbreedingassistant-v1` and
owner `dtmapi.mod.yuuka.dtmapi.fishbreedingassistant`. The product owns one
`Item.get_title` Postfix, fish-roe formatting and duplicate protection. Its only
shared dependency is the Experimental, read-only `IItemDisplayNameApi` native
name lookup; it owns neither a copied native fallback nor a production fish-data
table. The empty generated lookup is not production data. The old three-Hook `IItemTooltipApi`
executor is frozen demand-inactive Compatibility with both-order fail-closed
coordination. `GAME-SMOKE/20260722-145154` accepted the historical product-local
  fallback; `GAME-SMOKE/20260722-161022` first proved the shared-adapter package.
  The corrected positive-only-cache/exception-safe package is frozen by
  `GAME-SMOKE/20260722-180502`, which proves `鱼卵 (鱼)` plus exact owner cleanup.

## AnimalHusbandryProgress Fifth Real Product

The [admission Review](../reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md)
accepts exactly `Yuuka.DTMAPI.AnimalHusbandryProgress`, Workshop item `3742763843`,
version `1.0.1-dtmapi`, policy `doloctown-23762374-animalhusbandryprogress-v1`
and owner `dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress`. It owns four patches
across the reviewed constructor, viewer-show and unregister targets plus cached
derived rows, cached native mood-row clone targets, a one-shot next-frame guard
and exact session cleanup. The old `IAnimalViewerApi`
executor remains frozen demand-inactive Compatibility with both-order fail-closed
coordination. The two bounded Steam attempts at `153320`/`153845` created no
process or fresh log and remain infrastructure evidence. After the Steam cloud
  choice was resolved, `GAME-SMOKE/20260722-161022` first verified the four patches
  over three targets, visible read-only `羊毛脂 0/100` row/native-style clone,
  native panel-close cleanup, exact owner deactivation, restoration and clean exit.
  The frozen exception-safe package and focused two-owner QA route are verified by
  `GAME-SMOKE/20260722-180502`; its one-second post-panel normal-Gameplay boundary
  also prevents the QA close and native ReturnHome from sharing one frame.
  `GAME-SMOKE/20260723-115821` then verifies the ProductNative refresh
  optimization without changing those four Hooks: stable rows and clone targets
  are cached per selected animal, all three sequential selections observe at
  least one newer render receipt and complete one next-frame guard, and each
  `RenderAfterShow` performs its initial hidden write and rearms at most one
  guard. The final receipt sequence is 6; native close/title return leaves
  data/clones/rows plus the exact Harmony owner at zero.

The [Fish/Animal comparison](../reviews/code/2026/20260722-0008-fish-animal-shared-boundary-review.md)
promotes exactly one SharedNative responsibility: owner-bound read-only
`IItemDisplayNameApi` over `DolocAPI.QueryItemProto(itemId).Title`. Fish formatting,
the empty/future breeding table, Animal rendering, ConfigMenu registrations,
lifecycle state, Harmony owners and QA assertions do not share a native owner and
remain outside that adapter. SDK/package/Doctor/Manager and live zero-leftover
machinery remain Catalog-driven Platform.

## ChestLocatorEnhancer Seventh Real Product

The [independent admission Review](../reviews/code/2026/20260723-0005-seventh-product-chestlocator-admission-review.md)
admits exactly `DTMAPI.ChestLocatorEnhancerMod`, Workshop item `3742765514`,
source version `1.0.0`, policy
`doloctown-23762374-chestlocator-v1` and owner
`dtmapi.mod.dtmapi.chestlocatorenhancermod`. ProductNative owns one exact
Postfix on
`ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`,
the inventory-widening policy, config, diagnostics and exact-owner lifecycle.
Native `LinearInventory` instances, CountItem/CostItem transactions, UI and
persistence remain game-owned.

The old `IChestLocatorEnhancerApi` provider and DTO ABI remain
Experimental/Deprecated/Frozen. Mandatory GameBridge contains only its thin
on-demand proxy; the heavy executor is the seventh domain in the existing
dormant-shipped Compatibility Host. Product-first and compatibility-first
orders reconcile before physical installation, and a residual owner makes the
request fail closed.

Focused Release builds, Unit, retained ABI, Catalog, policy, Author SDK,
Doctor, QA and release-contract checks pass. `GAME-SMOKE/20260723-203127`
loads the third save and current Advanced package, observes one exact product
patch, proves native Count/Cost behavior
`0 -> 3 -> 1`, returns to title, and deactivates the real Loader owner from one
actual patch/callback to zero instance/patch/callback/Core roots before exact
save/profile/source/QA restoration and clean exit. This runtime evidence is
accepted by the requested final independent Review `20260723-0006`.

Complementary `GAME-SMOKE/20260723-210424` completes two real third-save
cycles with coordinator totals
`requests=2; nativeEnter=2; nativeReturn=2; saveLoaded=2`. The product installs
its exact owner once at count `1`, keeps that process-lifetime Hook across
title/save observation resets without duplicate installation, and exits with
exact save/profile/source/QA restoration. `205205` and `210101` are restored
non-acceptance QA composition conflicts and are not positive product evidence.

The mandatory Chest boundary falls from 542 physical / 475 non-empty lines to
243 / 206; the mandatory GameBridge DLL falls from 966,656 to 937,984 bytes.
The 540-line frozen executor remains in the Host and the 34,304-byte product
DLL is separately shipped. Therefore the result reduces default-loaded
Runtime volume only; it is not a download, installed-footprint, combined-size
or total-source reduction claim.

At the seven-product Chest checkpoint, Advanced SDK/package, Catalog,
Doctor/Manager and live
zero-leftover machinery remain generic Platform. ConfigMenu glue and Loader
lifecycle conventions are reused, but no other real product shares the
Chest inventory-query native owner or widening invariant. No SharedNative API,
Host or dispatcher is introduced.

## MoreEquipmentSlots Eighth Product Admission

The [independent admission Review](../reviews/code/2026/20260723-0009-eighth-product-more-equipment-slots-admission-review.md)
admits exactly `DTMAPI.MoreEquipmentSlotsMod`, Workshop item `3744059735`, for
one bounded eighth-product implementation. The admission and independent
commit-range correction boundaries completed the physical migration. A later
[save-commit Review](../reviews/manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md)
reopened its behavior/data-safety gate. Reviews `20260724-0006` and
`20260724-0007` retained the implementation while narrowing final runtime
acceptance; the bounded current-commit reacceptance below closes both findings.

The product owns its protected extra-slot sidecar, slot ordering, reflected
AccessoriesBar clones, configuration and exact-owner lifecycle. Native
`AgentEquipmentManager.functions`, `AgentEquipmentFunction`, equipment effects,
backpack/mail overflow, shield-first damage handling and the base
AccessoriesBar remain game-owned. The current named mandatory implementation is
2,814 physical / 2,484 non-empty lines, compared with the current 92 physical /
81 non-empty-line Strict entry, so the migration has a material default-loaded
Runtime opportunity without implying a download or total-source reduction.

The admitted ProductNative owner may atomically install exactly four targets:
`AgentEquipmentManager.ReloadParams` postfix,
`BodyController.OnAttacked(float,bool,Vector2,out bool)` prefix,
`AccessoriesBar.__Init` postfix and `AccessoriesBar.OnStartShow` postfix.
The old `IEquipmentSlotsApi` and its six DTOs retain their exact signatures and
are marked Experimental/Deprecated/Frozen. Their legacy arbitrary-owner
`0..24` executor and cold-recovery backend live only in the existing single
Compatibility Host; the ProductNative implementation is independently fixed
at three slots. No new Host, public API, receipt family, toolchain or
SharedNative component is introduced.

The implementation has explicit Working/Committed projections and a
save-identity-bound durable journal rather than claiming a cross-store atomic
write. It distinguishes backpack placement, explicit mail delivery and true
failure. Ordinary shield/equip/replace/unequip changes stay in Working memory;
`SaveSaving` may persist only an explicitly uncommitted gameplay candidate,
and successful native-save evidence promotes Committed state. Title, shutdown
or cold restart without that evidence discards the gameplay candidate.
`OwnerRecovery`/`OrphanRecovery` remains a separate, persistently retryable
management transaction. The 2026-07-29
[legacy-sidecar Review](../reviews/manual-qa/2026/20260729-0001-moreequipment-legacy-sidecar-migration.md)
identified that these Product-v3 semantics did not cover historical flat
schema 2 or Compatibility Host flat schema 3. The correction now classifies
the JSON generation before ownership, atomically migrates exact legacy bytes
to Product v3, and routes proven flat documents to legacy cold recovery.
Audit `20260729-0004` then found four remaining authority gaps. Commit
`0331f56d` adds the exact schema-less public writer shape, flat schema 1-3
native save-clock validation, persisted Product revision, atomic retryable
legacy backup and scoped-owner precedence. Independent audit `20260729-0005`
found that the original source-hash archive was not yet a crash-safe
single-claim barrier. Commits `4a395398`/`3937628b` attempted that correction;
re-audit `20260729-0006` then found a publication TOCTOU, ignored
claim-only/source-missing state, incomplete completion validation and a
resident-Host SaveLoaded gap. Implementation `b2cb5430` plus coverage/execution
HEAD `c5fce186` serializes and revalidates claim publication, blocks empty
authority while a claim remains, verifies archive plus Product before claim
deletion, and starts recovery for an already resident backend. Compatibility
still links only the lightweight probe. Re-audit `20260729-0007` confirmed
those direct fixes but found that the static lock did not close a
cross-process late publisher and that the production Hook dispatched the
resident backend twice. Commits `9c0645bd`/`af455c7d` add pre/post-publication
source/archive/Product revalidation with exact self-withdrawal, make feature
fanout the single SaveLoaded/title owner, reject claim completion while global
exists, and cover the actual second-process and production Hook/SaveSaved
routes. The replacement package and independent source reacceptance remain
open; prior `52F5...`/`0E75...` package bytes are superseded.
Round-1 independent Review `20260729-0008` then found that a loser could crash
after late claim publication but before self-withdrawal, and that deleting the
winner claim retained a completion TOCTOU. Commits `6c5542ff`/`7adf2b7d`
replace deletion with an atomic, durable schema-1 `pending -> completed`
transition, preserve old state-less pending claims, keep the completed winner
as the game-root barrier, and cover child-process crash, completion
global-recreation and completed-publication restart. Round-2 Review
`20260729-0009` found historical no-claim, global capture and eligible previous
gaps. Commits `34f4d20c`/`12dd3c6d`/`8efea22e` add exact completed-barrier
backfill, same-volume atomic source capture, Product live/previous authority
selection and portable real-process coverage. Final Review `20260729-0010`
found four remaining source/claim/capture gaps. Commits
`574fb6f4`/`6270d521` add the source-independent game-root winner,
non-overwriting claim transition, interrupted-capture recovery, all-global
terminal validation and real dual-process/capture-exit coverage; `a5e36d2`
freezes the new recovery paths. Additional Review `20260730-0001` then found
that stale loser evidence could block the exact winner, pre-winner census
omitted eligible previous/different-hash Product authorities, and capture-only
residue allowed empty Product/no Host demand. Commit `0ffb6a70` makes
classification winner-first, treats per-hash loser bytes as retained optional
evidence, performs canonical live/previous census before winner publication,
blocks capture-only empty state and routes migration residue to explicit Host
demand. The old deterministic `4D8E...15B6` package is superseded by this
source correction; no replacement package is frozen yet.
Additional Review `20260730-0002` then found that existing pending evidence
without a winner bypassed that census before Product publication, and that
empty-state creation/Host demand omitted other PreSchema Product authorities,
deterministic archives and scoped backups. Commit `0b016b12` removes the early
return, runs empty census under the operation lock, blocks all those residue
classes and routes archive/backup residue to explicit Host demand/diagnostic.
Additional Review `20260730-0003` showed that the round-2 rule was too broad
and that a pending winner still bypassed Product census. Commit `dcecbf3e`
reruns census for pending winners, validates completed other-save authority
before allowing a new save's in-memory empty state, retains current-scope and
unbound recovery barriers, removes transient true-empty lock artifacts, and
suppresses healthy terminal evidence for a loaded Product while preserving
absent-owner Host demand.
Additional Review `20260730-0004` then found that the immutable winner's
migration-time clock expired against Product revision after a later normal
save. Commit `571e5d55` separates terminal identity/stamp/archive proof from
the normal current-save anti-ahead rule and covers a revision advance beyond
the tolerance plus stale-revision and identity-drift controls.
Additional Review `20260730-0005` found that A's own T1 Finalize/Complete path
still used exact T0 clock matching and that the other-save empty guard read
loser evidence before the completed winner. Commit `33772012` makes completed
winner handling identity-based and winner-first in both routes, while retaining
loser bytes as optional diagnostics and preserving incomplete-state barriers.
Post-freeze audit `20260730-0006` then found that identity-bearing
`GlobalFlat` terminal archives were not bound to their canonical Product,
optional evidence collision could still fall back from Product T1 to strict
winner T0 validation, Product v3 treated a missing native save clock as a
wildcard, and cold-demand negatives were cached for the process lifetime.
Commit `5a6c59b4` binds each legal GlobalFlat archive to exactly one eligible
canonical stamped Product, makes evidence collision use terminal winner
validation, requires current and stored Product clocks before authority
publication, re-probes cold demand at lifecycle boundaries, and isolates the
real child-process winner test from optional per-hash evidence. The prior
`1E840AA5...AC2B` package is superseded. The replacement 7-entry package is
byte-identical across two builds at source-and-test HEAD `730a4900`, with ZIP
SHA-256 `EDB7BF80240CE86C859CBD09BBBD9EE22131B38D9A865142EB3747C1767EEF70`
and Product DLL SHA-256
`03E3651C0E1F5258E4CE44EA38605F7AD047DD507C33B1E76DB0A29E5F639A60`;
independent source/package acceptance passes without closing the remaining
player gates.

Product-first and compatibility-first four-Hook orders, residue, install
rollback, configuration disable/restart and real owner deactivation still fail
closed, and focused cleanup tests require
clone/listener/function/callback/Hook/root zero. The real Host fixture covers
ordinary gameplay and recovery semantics directly. Final `202032`/`202146`
prove replacement, damage, break, post-break equip/unequip, title rollback and
cold Working=Committed with the live archive and committed sidecar unchanged
before cleanup. AutoCloud-isolated `202526` proves two real `SaveSaved`
boundaries, promotion and exactly one logical item. The runner root/reparse
checks also pass. Review `20260724-0007` retained those facts but required a
non-empty committed baseline. Final `222231` normally saves one durability-80
shield; `222329` and `222423` prove no-save damage/break/replace/unequip;
independent cold `222516` restores the exact durability-80 shield, exactly one
logical item, zero candidate/journal, unchanged archive/sidecar metadata before
cleanup and successful fixture deletion. Those runs remain valid for nested
Product-v3 data only. The legacy migration correction is `implemented` after
focused Product, physical Host, Compatibility, Catalog and package checks.
The bounded migration/cold route is now accepted: U2a/U2b/U2c and restart own
the physical conversion, corrected U1/U3/U4 own the old-to-Host transition,
targeted C0 owns only startup-time old-Runtime rejection, and the migrated
no-save/save chain plus final per-item cold observation
`GAME-SMOKE/20260730-161355` own Product save semantics. Review
`20260730-0010` defines this final reacceptance boundary; publication remains
blocked by the Catalog release stop.

## StrongPlantingGun Ninth Product Baseline

Review `20260724-0003` admits exactly `DTMAPI.StrongPlantingGunMod` for the
ninth-product switch. Update `20260724-0001` removes the mandatory executor,
Hook bridge, demand/provider route and legacy product source while adding one
SDK/Catalog-generated `netstandard2.0` ProductNative package. No retained
Workshop binary exists, so `IStrongPlantingGunApi` and its three DTOs remain
Experimental/Deprecated/Frozen warning shells without a ninth Compatibility
Host executor.

ProductNative owns the fixed seed/film/fertilizer three-slot policy, both
`ItemFarmingGun` constructor postfixes, the tool-use prefix, two farming-gun UI
transfer prefixes, reflection metadata, native function-capacity
snapshot/restore and config/lifecycle. Native JSON construction can occur while
the product is suspended at title, so `SaveLoaded` performs one backpack scan
to prepare already-deserialized guns before gameplay resumes. It is not a
per-frame scanner or SharedNative boundary. The game continues to own native
inventory serialization, backpack/container UI, equipment and PlantBasin
state.

Focused product/Core/QA/API/Catalog/SDK/package/Doctor checks pass, including
the real static archive route and a mid-scan failure that restores the first
prepared gun before clearing all ProductNative ownership.
`GAME-SMOKE/20260724-101757` binds the current DLL and proves native save,
title removal, one third-save JSON re-entry at `3/3/3`, all five real Harmony
owners, real Loader deactivation to exact zero, protected state restoration and
clean exit. The independent closeout found no remaining P0/P1/P2, so this is
  the frozen verified/closed ninth product. MoreEquipmentSlots' non-empty
  committed Product-v3 rollback and legacy transition were accepted
  separately; neither state changes StrongPlantingGun's status.

## Zoom Tenth Product Baseline

Review `20260724-0005` admits only `DTMAPI.ZoomMod`; Update `20260724-0002`
completes the atomic switch. ProductNative owns keybind/config scale state, one
vanilla `mainCamera.orthographicSize` snapshot, exact restoration and one
non-suppressing five-parameter `DolocAPI.SetEnvCamera` Postfix. Camera objects,
follow/clamp, environment data, background, fog, panorama, scanner and UI
remain game-owned.

The retained `0.4.2-dtmapi` Zoom DLL is the one real CameraView consumer. Its
SHA-256 is
`DFA74BDD3561A9E9647FEC01AB9B48F095826D2A752AE920566BE2C5063971B3`;
the final ABI gate locks the complete 35-member CameraView MemberRef set. The
new product consumes neither Camera API family. The old CameraView/CameraZoom
executors live in the existing Compatibility Host and are mutually exclusive
with ProductNative before native writes.

Focused build/Unit/API/Catalog/SDK/package/Doctor/ABI checks pass.
`GAME-SMOKE/20260724-180233` proves current DLL and HookProbe, Product Hook
`1` / compatibility Hook `0`, real Loader instance/callback/Hook/root zero,
pre-cleanup archive and committed-sidecar unchanged evidence, source/profile
recovery, Player Doctor and clean exit. It is non-acceptance for camera
behavior: `SetEnvCamera` compounded 4x `67.5` to `270`, and config disable
restored the polluted `67.5` rather than native `16.875`. `175944` remains
non-acceptance for the corrected QA-only post-`SaveLoaded` CurrentRoom timing
gap. Corrected `202032` restores the covered orthographic-size paths.
Review `20260724-0007` then required resolution/fullscreen-derived native state
and maximum-to-1 failure propagation. Final current-commit
`GAME-SMOKE/20260724-221902` passes those gates: native refresh is reproduced
at 2x/4x, while direct 1x, maximum-to-1, configuration disable, title and real
Loader deactivation restore the exact initial orthographic size,
`CameraController.camSize` and room x/y ranges; the Loader result reaches zero
instance/callback/Hook/roots. Zoom is verified/closed. No complete Release,
L0-L5, GC gradient or long test was run.

Update `20260801-0001` supersedes only the current product behavior/status, not
the admission or retained ABI evidence above. It removes product-initiated
`RefreshResolution`, owns only `orthographicSize`, and wraps genuine native
refresh with an exact Prefix/Finalizer so native derived state is calculated at
1x. The three-patch source/physical-owner gates pass. The combined player test
then proved 4x moving-player follow without fixed-center flicker; Update
`20260801-0002` verifies the current product and authorizes only its exact frozen
existing-Workshop update tree.

The tracked Advanced policy registry is the exact membership/hash/binding
authority projected into Core, Author SDK and Install Doctor through generic
embedded resources; the author-project JSON schema is structural and does not
duplicate a product-ID enum. Catalog checks bind every real registry row to one
product/release definition. A complete test run builds Author SDK once and reuses
those same-run tool bytes for every product's two deterministic package builds;
this is build reuse, not a cached-pass or second receipt authority.

### Required Validation Matrix

| Layer | Positive evidence | Negative evidence |
| --- | --- | --- |
| Static/source | Explicit Strict remains SDK160/closure-bound; omitted-kind legacy is separately classified; no raw native type enters public Abstractions | Unknown/mismatched explicit identity still rejected; legacy never inherits Advanced receipt guarantees |
| SDK/build | Strict still triggers `SDK160`; declared Advanced resolves only receipt-bound references | Strict bypass, missing receipt, wrong target framework and hash drift rejected |
| Package/deploy | Identity, payload and reference receipt survive pack/deploy/status/recover | Forged identity, extra native DLL, journal/receipt drift and path escape rejected |
| Loader | Classification and applicable compatibility checks finish before assembly load; legacy cold-load and private dependency resolution are exercised | Unknown Advanced game/reference build and dependency failure produce zero owner/API/Harmony roots; legacy Entry failures remain owner-attributed |
| Doctor/Manager | Stable machine codes plus player wording for native risk, author ownership and restart | Explicit Strict is never silently relaxed; legacy is never presented as verified Advanced or safely hot-unloaded |
| Runtime/game | Entry/log/config/patch/disable/restart/failure isolation and clean exit | Advanced wrong-owner/duplicate failures remain supervised; legacy Entry/sibling failures remain isolated and restart-visible without claiming third-party Hook cleanup |
| Save-bound ProductNative | Working gameplay state promotes after successful native `SaveGame`, normally through `SaveSaved`; native-success/notification-or-sidecar interruption reconciles exactly once | No-save return/crash without proof of native success discards ordinary gameplay intent; owner/orphan recovery is explicitly typed and cannot silently redefine gameplay rollback |

### Rollback

The G2 implementation is isolated at
`c79306dfc7de0e85c74e24ece7d4c5cd47cb0822` under one revertible Update.
Rollback removes the schema revision, SDK/reference contract, Loader classifier
branch, Doctor and Manager projections, package fields, fixture and tests
together, and must also revert the closure authority that declares G2 passed. Existing
explicit Strict CodeMods continue through the Strict boundary and explicit
Advanced remains receipt-bound. The later 0.5.5 legacy-native amendment
intentionally supersedes G2's original omitted-kind Strict default; reverting
G2 must not silently remove that separately owned compatibility decision. No
partial Advanced manifest is left accepted. The rollback must restore the
pre-G2 five-DLL/player package and author SDK exactly, without changing real
product packages.

## G1, G5 And G6 Receipts

The 23-domain G1 ownership contract is
[`batch6-phase0-domain-contract.json`](../../tools/release/contracts/batch6-phase0-domain-contract.json).
Its generated baseline records the source commit, capture time, include/exclude
rules, exact files, Git blobs and physical LOC for Core/platform, GameBridge,
product, QA and Compatibility scopes. ProductNative execution itself has zero
allowed placement in mandatory Runtime. Small identity, policy and owner-arbitration
deltas may still be necessary platform wiring and must be measured separately;
they do not become SharedNative merely because they are mandatory. A migration may
be called mandatory-Runtime slimming only when its same-unit physical delta is
negative and the relevant Compatibility body actually leaves the ordinary load
path. The current OneActionComplete, ActionSpeed, FishBreedingAssistant and
AnimalHusbandryProgress ownership rehomes retain their frozen Compatibility
executors and therefore are not Runtime-slimming claims.

The contract keeps three axes separate: Review disposition, zero-or-more
canonical physical-ownership candidates (`Platform`, `SharedNative`,
`ProductNative`, `ContentOwner`), and target deployment form. Its decision
state records decided, split, content-only, candidate, deferred or unresolved
rows without inventing a fifth ownership category. The reproducible G1
baseline/decision inventory passes Phase 0. The G1 ownership gate remains blocked
for candidate, deferred and unresolved domains until their focused native-owner
review reaches a final decision.

The [2026-07-22 focused G1 Review](../reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md)
and Update `20260723-0002` freeze Oil as pure official JSON and reconcile Mine
as a `split-decided` domain: official JSON owns static item/equipment/recipe/
group content, while runtime cycle, archive time, power/economy configuration,
recipe restoration, scheduler/cache/renderer and product policy belong to a
separately admitted Mine ProductNative assembly. Review `20260726-0002` and
Update `20260726-0004` now implement that exact split without a Mine Content
Host or public machine provider.

The negative/out-of-scope decisions for CustomEntities, Multiplayer and the
split Pet/Vehicle domains plus generic Audio/BGM deferral remain open outside
this Oil/Mine projection. Full G1 therefore remains blocked for those
candidate/deferred/unresolved domains. MoreSaves has completed its bounded
atomic implementation, independent review and corrected game acceptance. The
separately authorized frozen-ABI Compatibility Host below is not a G1 domain
host and does not alter that disposition.

The same contract classifies the Batch 5 event, generation, demand, reload,
performance, QA and compatibility paths and records the 0.5.5 consumer plans.
These facts mean:

- generic event/lifetime/generation machinery can remain Platform;
- product routes in shared files must move with the admitted product;
- product QA/performance orchestration stays optional QA;
- frozen translators remain Compatibility with an explicit warning/removal
  gate;
- demand-inactive evidence proves stopped work, not correct physical ownership.

No public or internal API is removed in Phase 0. The frozen fishing ABI keeps
its 0.5.5 bridge promise and earliest 0.6.0 breaking gate; the first-party
primitive friend seam can be removed only with the admitted AutoFishing pilot;
other product-shaped APIs retain their current compatibility until their own
consumer-backed migration Update.

The later [Phase 2 consumer scan](../reviews/code/2026/20260722-0010-frozen-abi-consumer-and-compatibility-host-review.md),
extended through the DebugConsole twelfth-product Update, binds eleven exact
retained published binaries across ten compatibility families. The current implementation uses one dormant-shipped optional
Compatibility Host outside the default-loaded five-DLL tree and migrates only
those ten heavy executors; StrongPlantingGun and Mine have no retained binary
and add no Host executor. The Host changes neither the provider ID nor any public
ABI. Catalog, release manifest, installer, rollback, status, Doctor, Manager
and log collection all project that one optional component. GameBridge has no
static Host reference and loads the exact receipt-
validated `netstandard2.0` assembly only on the first frozen-ABI call. A loaded
Host remains Mono-resident, but service-owned demand/callback/Hook/native state
must be clearable; unload is not promised. Pre-MoreSaves package evidence reduces the
default-loaded GameBridge from 1,084,928 to 966,656 bytes while the shipped
184,832-byte Host makes the 1,151,488-byte combined pair larger, so this is explicitly not a
download-package or total-shipped-size reduction. The [Phase 4 API audit](../reviews/api/2026/20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md)
is closed by the bounded Phase 1/4 Update: CustomEntity remains an
ABI-preserving Experimental/Frozen Core-only provider, AutoHarvest no longer
uses Diagnostic save state, and helper owner/thread/stale semantics are
explicit. The MoreSaves atomic switch separately removes 751
physical / 670 non-empty product-owned lines from the default-loaded SaveSlots
feature/proxy boundary (`887/781 -> 136/111`). Its frozen executor remains in
the shipped optional Host and the new ProductNative assembly is separately
shipped, so this is again only a default-load source-volume reduction, not a
download-package or total-shipped-size reduction.
The ChestLocator switch separately removes 299 physical / 269 non-empty lines
from the default-loaded Chest feature/proxy boundary (`542/475 -> 243/206`)
and reduces the mandatory GameBridge from 966,656 to 937,984 bytes. Its
frozen executor and new product remain shipped, so the same claim limit
applies.

At the corrected ninth-product baseline, the five mandatory Runtime projects
contain 66,341 physical / 59,437 non-empty compiled source lines and their five
DLLs total 2,135,552 bytes. Mandatory GameBridge is 27,492 / 24,356 lines and
825,344 bytes. The nine ProductNative `src` trees total 81 files / 21,828
physical / 20,114 non-empty lines and their DLLs total 501,248 bytes.
Mandatory Runtime plus those same products is
`88,169 / 79,551 / 2,636,800`; the Compatibility Host is 397,312 bytes.

At the final verified tenth-product baseline, the five mandatory Runtime projects are
65,534 physical / 58,710 non-empty compiled source lines and 2,111,488 DLL
bytes. Mandatory GameBridge is 26,658 / 23,602 lines and 798,208 bytes. The
ten ProductNative `src` trees total 91 files / 23,347 physical / 21,526
non-empty lines; their DLLs total 531,456 bytes, including the final
30,720-byte Zoom DLL. Mandatory Runtime plus those same products is
`88,881 / 80,236 / 2,642,944`; the Compatibility Host is 437,760 bytes.
Thus the tenth split changes default-loaded mandatory Runtime by
`-807 / -727 / -24,064`, but mandatory plus products by
`+712 / +685 / +6,144`. These measurements prove only a smaller
default-loaded Runtime. They do not prove a smaller repository, download,
install, total shipped package or all-products-enabled process.

The twelve-product horizontal ownership result adds no SharedNative capability.
Catalog, Advanced SDK/package, Doctor/Manager, ConfigMenu registration,
lifecycle and zero-leftover machinery remain Platform. All ten frozen
executors share one optional Compatibility component but not one native owner;
StrongPlantingGun has no Host executor, and Zoom shares neither its camera
state holder nor Hook target with another current product. `IItemDisplayNameApi`
remains the only gameplay/native adapter justified by two independent real
product consumers (FishBreedingAssistant and AnimalHusbandryProgress) and one
common native title owner.

At the final verified eleventh-product snapshot, after the separately
committed Catalog/C1/Manager audit repair, the five mandatory Runtime projects
contain 148 compiled source files / 64,738 physical / 58,034 non-empty lines
and their five DLLs total 2,089,984 bytes. Mandatory GameBridge is 73 files /
25,050 physical / 22,162 non-empty lines and 743,936 bytes. The eleven
ProductNative `src` trees total 102 files / 27,139 physical / 25,032 non-empty
lines; their DLLs total 633,344 bytes, including the 101,888-byte Mine DLL.
Mandatory Runtime plus those products is
`91,877 / 83,066 / 2,723,328`. These current-tree measurements must not be
treated as the pure Mine delta because the preceding Manager audit commit also
changed mandatory Runtime. The ownership conclusion is narrower: Mine removes
the mandatory MachineProduction executor and adds no SharedNative or Host
executor.

The twelfth-product implementation adds no SharedNative capability or public
API. DebugConsole's new UI, private action allowlist, native patches and
reversible leases are ProductNative. Core owns only its generic typed-input,
owner-bound modal and Loader cleanup machinery. The old provider/UI and all
seven action executors, including creative/input compatibility Hooks, remain a
separately owned frozen Compatibility route and are not loaded by the new
product. Mandatory GameBridge contains only their thin reflected broker.
Current-product `GAME-SMOKE/20260727-083919` proves ProductNative action
ownership, exact transient restoration and title/Loader zero; isolated
`NativeSaveExpected` `090702` proves confirmation, native failure propagation
and one successful confirmed save. Exact-current old 0.3.1/current-Host
`132103` proves every frozen action routes to Compatibility, warmed frames
produce no owner-wide Hook churn, and title/Loader state reaches zero.
Independent Review `20260727-0004` accepts these results and the final
transactional lifecycle implementation.

The 2026-08-01 player-UI correction does not remove any action body or frozen
member. It stops constructing only the generator, first-monster and first-
resource buttons plus their ambiguous status line. Reopening those world
actions requires the dedicated selection/search/count/scope/confirmation/save
review in `docs/planning/20260801-debugconsole-world-actions-roadmap.md`.

The MoreSaves active-owner retry is demand-driven: a missing manager while the
product remains active may temporarily subscribe `UpdateTicked` and removes the
subscription as soon as pending work clears. A final Loader deactivation
failure retains the exact writer lease and reports failure, but does not claim
automatic event delivery after Loader has removed the owner. Recovery waits for
restart or a later explicit cleanup pass.

## Current Admission State

```text
Phase 0 correction: PASS; schema-2 receipt, both-PowerShell-host gate, full Release suite and Runtime smoke verified
G0 authority + G2 design + Phase 0 G5/G6 inventory: PASS
G1 reproducible baseline: PASS; audited source delta and consumer evidence are receipt-bound
Full G1 ownership gate: BLOCKED for candidate/deferred/unresolved domains
G2 Advanced Runtime and synthetic fixture: PASS; implementation c79306dfc7de0e85c74e24ece7d4c5cd47cb0822
G2 proof boundary: tracked policy doloctown-23762374-g2-v1 + DTMAPI.AdvancedFixture only
AutoFishing G3/G4 + relevant G5/G6: VERIFIED; independent acceptance passed
AutoFishing D.5: CLOSED; 22 Hooks retained and one bounded fifth-save short regression passed
OneActionComplete: ADMITTED SECOND PRODUCT; corrected third-save runtime and exact owner deactivation passed
ActionSpeed: ADMITTED THIRD PRODUCT; corrected nine-target third-save runtime and exact owner deactivation passed
FishBreedingAssistant: ADMITTED FOURTH PRODUCT; frozen corrected shared-adapter package runtime-verified at 180502
AnimalHusbandryProgress: ADMITTED FIFTH PRODUCT; frozen corrected behavior/native-close/exact-owner package runtime-verified at 180502
ItemDisplayName shared lifecycle: VERIFIED; focused Unit and current-DLL third-save physical Hook/callback/clear-release/title/re-query acceptance pass at 074311
Phase 2 compatibility scan: PASS as consumer evidence; nine exact retained product binaries require the frozen ABI
Optional Compatibility Host: VERIFIED as one netstandard2.0 dormant-shipped component outside BepInEx scan; exact source/ABI/Catalog/package gates and post-review dormant-to-resident Unity Mono proof pass at 083503/092718, including resident Fishing owner dictionaries at zero
Phase 1 Core legacy seams: PASS; orphan string-input adapters and the unowned fatal-window notification chain are removed while public input ABI, typed input and external fatal detection remain
Phase 4 public API audit: PASS; CustomEntity/provider, AutoHarvest Diagnostic dependency and helper owner/thread/stale semantics are closed without API expansion or ABI deletion
G1 focused review: Oil/Mine decisions projected and Mine split implemented; remaining candidate/deferred/unresolved rows stay blocked
MoreSaves: ADMITTED SIXTH PRODUCT; independent review and corrected third-save initial/post-title/save-protection/owner-deactivation acceptance passed at 150215
MoreSaves audit correction: PASS; Doctor/Status Host topology, frozen ABI state semantics, demand-driven retry, version axes and exact component move/place rollback gates pass
ChestLocatorEnhancer: ADMITTED SEVENTH PRODUCT; VERIFIED/CLOSED by focused gates, 203127 native behavior/Loader cleanup, 210424 two-cycle re-entry, commit-range corrections and final committed-candidate smoke 224200
MoreEquipmentSlots: IMPLEMENTED/PUBLICATION-DEFERRED EIGHTH PRODUCT; the new 1.0.0 package, Product-v3 migration and Steam update are outside the 0.5.5 release set, while Review 0016's five P1 transaction findings remain open next-version work. Workshop 3744059735 stays at exact 0.3.1-dtmapi and is supported only through the frozen IEquipmentSlotsApi/demand-loaded Compatibility path. Earlier SaveLoaded/title ownership, claim, U1/U2/U3/U4, C0, migrated-save and 161355 evidence remains scoped to its exact source/package/runtime boundaries
StrongPlantingGun: VERIFIED/CLOSED NINTH PRODUCT; focused static-archive/failure-rollback gates and current-DLL protected third-save behavior/title/JSON-reentry/actual5/Loader-zero acceptance pass at 101757; independent closeout passes
Zoom: VERIFIED/CLOSED TENTH PRODUCT; admission and exact 35-MemberRef legacy ABI remain closed, Update 20260801-0001 restores orthographic-only Z1 ownership with three atomic camera patches, final player QA passes and Update 20260801-0002 freezes the exact authorized Workshop tree
Mine: VERIFIED/CLOSED ELEVENTH PRODUCT; object-identity, preflight, 223105/223236 behavior, retryable recipe/tech restoration and corrected SDK package bytes pass independent acceptance; borrowed well art and 2x presentation remain separate publication blockers
DebugConsole: VERIFIED/CLOSED TWELFTH PRODUCT; admission, ABI, native executors and prior lifecycle evidence remain closed, Update 20260801-0001 hides three unfinished player controls and replaces aggregate MoveScaler mutation with a Product-owned final player-speed factor, while final 2x/3x/4x/1x/native-Buff player QA and the exact authorized Workshop tree close under Update 20260801-0002
Thirteenth-or-later real products: BLOCKED; no general Advanced authoring lane
Content Host implementation: BLOCKED under G7
0.5.5 RC preparation: EXACT PLAYER-ACCEPTED UPLOAD TREES READY; Update 20260801-0002 supersedes the stale upload state with the exact d389da0f Runtime plus ten tested product trees, preserves MoreEquipmentSlots Workshop 0.3.1, restores every subscription to its pre-test hash and authorizes only the eleven frozen existing-item updates
0.5.5 complete Release: PASS at 20260730-145616 for its selected frozen artifact; later narrow source/unit/player corrections are accepted only through the exact-tree authorization of Update 20260801-0002. New Workshop items, identity/folder moves, mass edits, MoreEquipmentSlots 1.0 and every non-listed update remain blocked; Steam upload has not been performed
Generic protected storage API: DEFERRED; no IProtectedStorageApi is declared or admitted, and frozen IEquipmentSlotsApi compatibility is not a shared-storage promotion
```

The G2 synthetic PASS and the individual real-product states have the
acceptance status stated
above. These results still do not make Advanced generally available to
authors, automatically admit a thirteenth product, implement G7, or make 0.5.5
releasable.
The Phase 1 Core diagnostic/input seams and Phase 4 API
classification/owner/thread tails are closed. Review `20260724-0003` and
Update `20260724-0001` close StrongPlantingGun; the bounded findings from
Review `20260724-0007` are dispositioned by the MoreEquipmentSlots and Zoom
Updates plus final current-commit evidence. Mine and DebugConsole are the
independently accepted eleventh and twelfth identities; Review
`20260727-0004` closes their remaining deployable-byte and lifecycle-ownership
gates. Every later product still
requires its own bounded admission.
`SDK160` remains the Strict boundary; a manual `CodeModKind=Advanced`, forged
receipt, or hand-built package is not an admitted route.

## Safety Clause

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。
