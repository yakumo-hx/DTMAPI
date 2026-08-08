# Eighth-Product Admission, Runtime Weight And API Reuse Audit

**Review ID:** `20260723-0010`

**Date:** 2026-07-23

**Status:** recorded — MoreEquipmentSlots owner admission retained, but
implementation remains NO-GO until the four pre-implementation corrections
below are closed

**Scope:** audit the corrections after the seventh-product split, independently
recheck the MoreEquipmentSlots eighth-product admission, compare current DTMAPI
weight with the post-AutoFishing-split/pre-D.5 baseline, distinguish source
movement from player Runtime relief, classify current public API reuse, define
the eighth-product acceptance metrics and route the work after that product;
no eighth-product implementation, package mutation, game launch, complete
Release, L0-L5, GC gradient or long test

## Source Request

Audit the previous corrections and eighth-product admission. Confirm whether
DTMAPI and ordinary player work have genuinely become lighter than the point
after AutoFishing was split but before AutoFishing D.5 slimming. Check whether
the existing APIs are all reusable. Recheck the eighth-product admission,
identify what must be completed before its split may begin, define its
post-split acceptance metrics, and state the following roadmap.

The untracked portable reverse-capture research and package files are unrelated
user work and were deliberately excluded.

## Verdict

The seventh-product source corrections are materially useful. The Chest
executor is outside mandatory GameBridge compilation, its query path now
avoids the prior unused-value boxing, reuses reflection/scratch state, bounds
successful-state logging and has focused allocation coverage. Catalog-derived
transaction checks and the PowerShell 5.1 correction also pass focused review.

The broader result is a real **default-loaded Runtime reduction**, not a
repository deletion:

- current mandatory five-DLL source is about 7.86% smaller than the selected
  pre-D.5 baseline;
- the corresponding five default-loaded DLLs are about 5.36% smaller;
- even when the same seven products are all included, source and DLL totals
  are slightly smaller;
- repository source, shipped optional Host and total installation/download
  size have grown.

The MoreEquipmentSlots ProductNative classification, exact four-target native
boundary and selection as the sole eighth product remain correct. The current
admission is **not yet safe to hand directly to implementation**, because it
misreads native mail-overflow success, does not close the cross-file save
transaction, does not explicitly preserve the frozen ABI's variable-slot
semantics, and rests on a final seven-product evidence root whose Player Doctor
reported findings.

Public DTMAPI APIs are not one homogeneous reusable surface. Stable Platform
contracts are ordinary-mod APIs; StableCandidate and Experimental contracts
have narrower promises; Diagnostic, Frozen, Disabled, Proposed and
product-mirror contracts are not recommended general authoring surfaces.

## Previous Correction Audit

### Confirmed

- `ChestLocatorEnhancer` mandatory source moved from 542 physical / 475
  non-empty lines to 243 / 206; the heavy executor is in the existing optional
  Compatibility Host.
- The corrected product query path removes the unused `Vector2Int` callback
  argument materialization, caches reflection metadata, reuses thread-local
  traversal scratch state and creates observation strings only when the log
  gate accepts them.
- Healthy repeated-query logging is transition/first-observation based instead
  of unconditional.
- Focused `chestlocator-product` and `compatibility-host` tests pass.
- Catalog checks pass in PowerShell 7 and Windows PowerShell 5.1.
- Developer OfficialLocal and Runtime-only uninstall transaction checks pass.
- The tracked tree was clean before this audit. The three unrelated untracked
  portable reverse-capture paths were not changed.

### Evidence precision correction

`GAME-SMOKE/20260723-224200` remains valid evidence for the corrected Chest
product behavior, Hook owner, third-save Count/Cost path, title recovery,
Loader deactivation and process exit. It is not a completely clean
seven-product player-diagnostic baseline:

- `collect-summary.txt` records `PlayerDoctorStatus=findings` and
  `PlayerDoctorExit=2`;
- `player-doctor.txt` reports the current and recovery-copy Chest packages as
  `ReferenceReceiptMismatch`, because that Doctor binary did not contain the
  Chest reference-policy ID;
- current tracked policy sources do contain the Chest policy, so the evidence
  is consistent with a stale or unsynchronized Doctor build rather than an
  invalid Chest package.

This does not require another game run. Before the eighth product starts, build
the current Player Doctor and run the existing focused package/installed scan
against all seven implemented Advanced policies. Add one generic equality gate
between the Doctor-embedded policy set and the Catalog's implemented Advanced
set so a stale Doctor cannot become another nominally final smoke artifact.

The Chest dual-owner proof is also a combined proof rather than a literal
end-to-end run of both real production executors on the native target: the
physical fixture uses real Harmony and exact owner strings on fixture targets,
while Compatibility tests inject its owner state. Existing game/product-first
evidence is sufficient to keep Chest closed; documentation should not inflate
that focused proof into a complete real dual-executor native integration run.

## Runtime And Product Weight

### Comparison boundary

The selected baseline is commit `23002c8f`, where AutoFishing had already been
split into ProductNative but D.5 product slimming had not begun. Current source
is `858bd607`; its two last commits are documentation-only, so the relevant
source is the same as the corrected seventh-product commit.

Line counts use tracked Git blobs. Current mandatory GameBridge input excludes
the ten Compatibility executor files removed by its project file and linked
only into the optional Host. Counting the physical GameBridge directory without
respecting those `Compile Remove` entries would incorrectly report optional
compatibility source as default Runtime source.

| Measure | Post-split, pre-D.5 | Current seven-product tree | Delta |
| --- | ---: | ---: | ---: |
| Mandatory five Runtime source files | 149 | 151 | +2 |
| Mandatory five Runtime physical lines | 75,911 | 69,948 | -5,963 (-7.86%) |
| Mandatory five Runtime non-empty lines | 67,874 | 62,613 | -5,261 (-7.75%) |
| Mandatory GameBridge physical compile input | 37,324 | 31,336 | -5,988 (-16.04%) |
| Mandatory five Runtime DLL bytes | 2,366,976 | 2,240,000 | -126,976 (-5.36%) |
| Mandatory GameBridge DLL bytes | 1,073,152 | 937,984 | -135,168 (-12.60%) |

The historical DLL baseline was rebuilt from that exact Git tree with the
repository .NET 8 toolchain. Current DLL sizes match the corrected product
candidate.

### Same seven functions, all products present

ProductNative code grows because each product now owns native lifecycle,
rollback, identity and recovery instead of being a tiny Strict entry into a
mandatory Runtime executor:

| Measure | Baseline products | Current seven Advanced products | Delta |
| --- | ---: | ---: | ---: |
| Product physical lines | 7,161 | 11,763 | +4,602 (+64.26%) |
| Product non-empty lines | 6,552 | 10,723 | +4,171 |
| Product DLL bytes | 241,664 | 343,552 | +101,888 (+42.16%) |

Combining mandatory Runtime with the same seven enabled product functions gives
the fair “everything enabled” comparison:

| Measure | Baseline total | Current total | Delta |
| --- | ---: | ---: | ---: |
| Physical lines | 83,072 | 81,711 | -1,361 (-1.64%) |
| Non-empty lines | 74,426 | 73,336 | -1,090 (-1.46%) |
| DLL bytes | 2,608,640 | 2,583,552 | -25,088 (-0.96%) |

This is small but genuine net reduction, so the work is not only source
movement. Its primary benefit is still conditional loading: an ordinary player
does not load ProductNative assemblies for absent products.

### What did not become lighter

- `src + products/first-party` grew by about 8,666 physical lines (7.52%).
- The optional Compatibility Host is currently 209,408 bytes. Modern product
  startup keeps it dormant, but it is still shipped and will remain loaded in
  Mono after a real frozen-ABI call.
- Runtime plus Host is about 3.48% larger than the old mandatory Runtime alone.
- Runtime plus Host plus seven product DLLs is about 7.07% larger than the old
  Runtime plus the equivalent old product DLLs.
- Therefore this work must not be advertised as repository, download,
  installation or total-shipped-size reduction.

### Player running burden

Structural running burden has genuinely improved:

- AutoFishing D.5 alone removed 873 physical / 801 non-empty player-source
  lines and 24,064 DLL bytes while keeping its accepted product behavior.
- The optional Host is not loaded on ordinary modern startup and only performs
  its receipt/hash work on first frozen-ABI demand.
- MoreSaves now uses bounded demand retry rather than a healthy permanent
  updater.
- Animal no longer performs the former periodic reflective/LINQ UI rewrite.
- Chest avoids the prior repeated metadata, scratch collection, boxing and
  healthy-state log work.
- No migrated production product performs direct per-frame file polling.

Four current product `UpdateTicked` subscriptions remain demand-scoped or
cheap: AutoFishing only while automation is enabled; ActionSpeed only for
enabled auto-fill-bottle; MoreSaves only during bounded manager-readiness
retry; Animal retains one callback whose normal frame immediately exits unless
the one-frame guard is armed.

The same enabled functionality still owns the same native work. Across the
seven products there are 39 Hook targets, and a product whose configuration is
disabled can retain a fast-return Hook until actual Loader deactivation.
Exact Loader deactivation, not a config boolean, proves owner unpatch.

The evidence therefore supports “smaller default-loaded code, fewer known hot
path operations and more demand-scoped roots.” It does **not** quantify a
working-set reduction or prove the long-session Unity/Mono GC issue solved.

## Public API Reuse Audit

### Reusable Platform surface

Legitimate reusable DTMAPI capabilities, subject to their matrix status, are:

- `DtmMod`, manifest/helper container, minimum-version policy;
- Monitor and owner-bound config read/write/path;
- ModRegistry/API exchange, translation and config migration as
  StableCandidate surfaces;
- owner-bound event/input and UI helpers as Experimental lifecycle surfaces;
- Workshop/content read-only publication queries;
- owner-bound ConfigMenu registration and optional keybind-reset capability.

`IDtmConfigMenuApi` is already one shared registration path. AutoFishing,
OneActionComplete, ActionSpeed, FishBreedingAssistant,
AnimalHusbandryProgress, MoreSaves and ChestLocatorEnhancer all consume it.
The eighth product must reuse that service instead of adding a product settings
framework.

### Proven SharedNative gameplay reuse

`IItemDisplayNameApi` is the only current gameplay/native adapter with two
independent real product consumers and the same narrow native owner:
FishBreedingAssistant and AnimalHusbandryProgress query
`DolocAPI.QueryItemProto(itemId).Title`. It remains Experimental because this
proof covers only that read-only responsibility.

### Open but not generally proven

- `ICameraViewApi` has real lease/owner arbitration but primarily one product
  consumer and intentionally excludes panorama/background/fog promises.
- `ICropHarvestingApi` is a narrow single-target PlantBasin operation, not a
  universal harvest or container enumeration API.
- `IAudioReplacementApi` exposes a limited allowlisted route, not a general
  BGM/audio-host promise.
- `IMailDeliveryApi` is native-template based and does not prove arbitrary
  title/content/sender behavior.

### Product mirrors and compatibility surfaces

`IMachineProductionApi`, `IEquipmentSlotsApi` and
`IStrongPlantingGunApi` mainly mirror one product's policy/sidecar. They do not
have two independent real consumers or a SharedNative ownership proof.

The migrated ActionCompletion, FishingAutomation, ActionSpeed, ItemTooltip,
AnimalViewer, SaveSlots and ChestLocatorEnhancer APIs are frozen old-ABI
facades. The CustomEntity interfaces are frozen registry-only contracts;
native creation remains blocked. Lamp is a disabled resolution shell.
CameraZoom is obsolete compatibility. These exist so old DLLs continue to
resolve; they are not recommendations for new mods.

DebugConsole, Inventory/Weather/Teleport/InstantSave/Time/Movement/Advanced
debug APIs are Diagnostic. Proposed APIs and gaps, including Panorama, a
general content-edit pipeline, generic save-data helper, custom HUD/menu and
multiplayer, must not be presented as implemented authoring capabilities.

### Metadata truth gap

The matrix communicates these distinctions, but source metadata defaults any
declaration without `DtmApiDispositionAttribute` to `Open`. Current source does
not consistently project Diagnostic, Disabled or old CameraZoom dispositions,
so reflection/Author SDK tooling can describe a contract as open when the
matrix says otherwise. `DTMAPI.Abstractions/README.md` also describes the
assembly too broadly as stable.

This is a bounded API-metadata/author-documentation correction after the eighth
product; it does not require game testing or ABI signature changes. The
eighth-product exception is `IEquipmentSlotsApi`: its interface and six public
DTOs must be marked Experimental/Deprecated/Frozen with
`Obsolete(..., false)` and exact retained MemberRef coverage before the visible
product switch.

## Eighth-Product Recheck

### Boundary retained

- `DTMAPI.MoreEquipmentSlotsMod` is the only real product consumer.
- Extra slots are a DTMAPI product sidecar and reflected UI clone policy, not
  an official variable-slot model.
- ProductNative owns the fixed three product slots, ordering, protected state,
  configuration, reflected clone/listener lifecycle and four exact Hooks.
- Native code owns the equipment manager/function list, committed effects and
  parameters, backpack/mail data, base AccessoriesBar, native hat visual and
  vanilla shield-first behavior.
- The four product targets remain
  `AgentEquipmentManager.ReloadParams`,
  `BodyController.OnAttacked(float,bool,Vector2,out bool)`,
  `AccessoriesBar.__Init` and `AccessoriesBar.OnStartShow`.
- No second product shares this sidecar, UI or shield fallback, so the
  extraction adds zero SharedNative and no new Host.
- The gross 2,814 physical / 2,484 non-empty mandatory source boundary is a
  credible opportunity, not a promised net reduction.

### Four corrections required before implementation

1. **Close the seven-product Doctor baseline.** Rebuild the current Doctor,
   prove all seven implemented Advanced policies with an existing focused
   package/installed route, and add one Catalog-to-Doctor policy-set equality
   check. No game rerun is needed.
2. **Replace ambiguous mail-overflow handling with a three-state native
   result.** Official `TryPlaceInBackpack(item, true)` calls
   `SendItemAsEmail` for overflow and still returns `false`. The old executor
   treats that value as failure, retains the sidecar entry and can duplicate
   the mailed item. Distinguish backpack success, `SendItemAsEmail` success and
   true failure; do not infer mail failure from the ambiguous combined return.
3. **Define a durable cross-store transaction, not only atomic JSON.** A temp
   file/replace prevents truncation but cannot atomically commit the native
   save and product sidecar. Before code movement, define a versioned
   product-data prepared/committed journal or equivalent persistent two-phase
   state, bind it to exact save identity/generation, and specify restart
   reconciliation. Native backpack/mail, committed sidecar and prepared
   transaction must represent exactly one logical item, never zero or two.
   This is product data, not a new assurance receipt family.
4. **Freeze and separate the old ABI before the product-visible switch.**
   ProductNative exposes fixed three-slot gameplay. The frozen old ABI
   historically normalizes `ExtraAttributeSlots` over `0..24`; Compatibility
   Host must retain that behavior for arbitrary old owners. The new product
   must not consume the frozen API. Freeze the interface and six DTOs, capture
   exact MemberRefs, and define pending-recovery result semantics.

After these corrections are recorded and their focused scaffolding is green,
one MoreEquipmentSlots implementation Update may begin. The first atomic slice
should be ABI freeze plus hidden ProductNative/Host routing; do not make the
product visible until transaction, owner, UI and cold-recovery gates pass.

## Eighth-Product Acceptance Metrics

### Structure and weight

- Catalog contains exactly eight implemented Advanced product identities.
- SDK-generated game-loaded product remains `netstandard2.0`.
- Mandatory Runtime contains no MoreEquipmentSlots executor, sidecar policy,
  product UI clone implementation or four product Hook bodies.
- Record gross and net source movement, mandatory GameBridge/five-DLL bytes,
  optional Host change and product DLL size.
- Keep one small live zero-leftover source/symbol gate.
- No sidecar, product enablement or old-ABI demand means no Host load, product
  updater, Hook, file polling, session or callback root.

### ABI and owner

- `IEquipmentSlotsApi` and all six DTOs preserve exact retained MemberRefs and
  carry Experimental/Deprecated/Frozen metadata plus non-error Obsolete
  guidance.
- The product installs all four Hooks or none under one exact Harmony owner.
- Product-first, compatibility-first, mixed residual, partial-install failure,
  config disable/re-enable and real Loader deactivation fail closed without
  unpatching the other owner.
- Compatibility retains old `0..24` behavior; the first-party product exposes
  exactly three slots and never calls the frozen API.

### Item, save, gameplay and UI

- Exactly-once `CostItem`, failed-cost rollback and replacement rollback.
- Backpack success, mail success and true failure are separate states.
- Every failure leaves exactly one logical item across native storage,
  committed sidecar and transaction journal.
- Fault injection covers interruption before native save, prepared journal
  with native-save failure, native save committed before sidecar promotion,
  failed promote/replace and interruption after commit.
- Cold restart reconciles every window without duplication or loss.
- Current-save/archive/player/clock isolation, legacy adoption/archive,
  corrupt/truncated fallback, no cross-save adoption and tail-first recovery.
- Passive, defense-only hat and shield hat apply/remove exact native effects;
  vanilla shield stays first, extra shields consume tail-first, and native
  visual `hatItem` remains unchanged.
- Exactly one three-clone/listener set exists; disable, title, re-entry and real
  Loader deactivation leave zero clone, listener, function, callback, Harmony
  owner and product root.

### Diagnostics and bounded game acceptance

- Doctor, Manager, package/install/uninstall and policy-set checks are green
  for the exact eight-product set.
- One protected third-save transaction with two short launches is sufficient:
  first enabled behavior/save/reload, then cold product-disabled recovery.
- The cold launch has no product Entry or Harmony owner; the existing Host
  activates only for sidecar recovery demand.
- After recovery, sidecar pending/journal state, Host transient, UI/callback
  roots and process state are zero, and protected external state is restored.
- No complete Release, L0-L5, GC gradient or long test is required.

## Route After The Eighth Product

1. Independently audit the eighth-product commit range and repair only concrete
   findings.
2. Compare all eight products and rebaseline mandatory Runtime against both
   the pre-D.5 point and the seven-product point. Promote no SharedNative
   capability without two real consumers and one common native owner.
3. Correct API disposition metadata and the Author-facing API guide so
   Stable, Experimental, Frozen, Diagnostic, Disabled and Proposed are not
   flattened into “public/open.”
4. Re-measure Phase 5 GameBridge capability/inactive boundaries. A ninth
   product is not automatically admitted.
5. If another migration is still worthwhile, compare StrongPlantingGun and
   Mine through an independent owner/consumer/budget Review. Mine remains
   design/economy/asset immature; Zoom remains the existing Strict consumer of
   the shared Camera boundary.
6. Continue Phase 6 Bootstrap/diagnostic UI reduction: productize the
   DebugConsole UI while retaining only justified Platform command/log seams.
7. Treat G7 Content Host, official JSON/PNG/WAV animals and the later AnimalPack
   rebuild as a separate declarative-content architecture.
8. Finish ecology/author documentation, Manager/Doctor presentation and
   install-path guidance before reopening the user-paused 0.5.5 release.

The long-run AutoFishing/ActionSpeed GC campaign remains a release-risk track,
not a prerequisite for this bounded product split.

## Validation Performed

- Read current project, route, debug, API-rebuild, identity, review and
  document-governance authorities.
- Inspected current/historical Git trees, compile inclusions and exact counts.
- Rebuilt the historical five-DLL baseline with repository-local .NET 8:
  zero warnings and zero errors.
- Inspected current DLL sizes, optional Host topology, official backpack/mail
  method bodies and current EquipmentSlots save/recovery implementation.
- Focused Chest product/Host, Catalog PS7/PS5.1 and transaction checks passed.
- Inspected `GAME-SMOKE/20260723-224200` behavior and Doctor artifacts.
- Did not launch the game or run complete Release, L0-L5, GC or long tests.

## Inspected Authorities

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/workflows/document-governance.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/README.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/api/public-api-matrix.md`
- Reviews `20260723-0007`, `20260723-0008` and `20260723-0009`
- AutoFishing slimming and Chest seventh-product Updates
- current Runtime, Compatibility Host, product and legacy EquipmentSlots source
- reverse build `23762374_public_C416D4`
- evidence root `docs/debug/evidence/GAME-SMOKE/20260723-224200`
