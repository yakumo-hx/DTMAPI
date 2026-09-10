# Strong Planting Gun Ninth Advanced Product

## Metadata

- Update ID: `20260724-0001`
- Date: `2026-07-24`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request And Authority

After freezing the eight-product baseline and independently comparing
StrongPlantingGun with Mine, admit at most one ninth product, commit that
decision separately, then implement and independently review the admitted
product.

Admission authority is Review
[`20260724-0003`](../../reviews/code/2026/20260724-0003-ninth-product-strong-planting-gun-admission-review.md):

- only `DTMAPI.StrongPlantingGunMod` is admitted;
- Mine remains `split-decided` / `PrototypeBlocked`;
- the implemented/verified baseline remains eight products until this Update
  closes;
- no new SharedNative, Compatibility Host executor, public API, receipt or
  checker family is authorized.

The user explicitly prohibits repeatedly running the complete Release suite.
This Update may run only focused source/build/Unit/Catalog/SDK/package/Doctor/
Manager/install checks and one protected third-save short smoke. It must not
run L0-L5, a GC gradient or a long test.

## Intended Atomic Switch

- Move the fixed-three seed/film/fertilizer policy, native capacity
  snapshot/restore, reflection cache, configuration and exact Harmony
  lifecycle into an SDK-generated `netstandard2.0` Advanced product under
  `products/first-party/StrongPlantingGun`.
- Own five concrete patches atomically: both `ItemFarmingGun` constructors,
  `OnUseAsTool`, `FarmingGunUiState.HandlePlaceToOtherSide` and
  `HandleSwapOneItem`.
- Preserve native `ItemFarmingGun.inventory` serialization and native
  backpack/container/PlantBasin/UI ownership; add no product sidecar.
- Keep `IStrongPlantingGunApi` and its three DTOs loadable with exact member
  shape and warning-bearing Experimental/Deprecated/Frozen metadata. The only
  tracked consumer is legacy/no-Type source and there is no Workshop,
  published version or retained binary authority, so no ninth Compatibility
  Host executor is added unless a real binary is found before cutover.
- Remove the mandatory StrongPlantingGun executor, Hook bridge, callbacks,
  demand/provider route and the legacy product source only in one buildable,
  Catalog-consistent switch.

## Baseline

Before implementation:

- mandatory StrongPlantingGun feature directory: 1,019 physical / 895
  non-empty lines;
- legacy/no-Type product `ModEntry.cs`: 119 physical / 106 non-empty lines;
- current verified product count: eight;
- current Compatibility Host executor count: eight;
- current concrete Strong native patch opportunities: five;
- current public product contract: fixed three slots, seed/film/fertilizer,
  water disabled.

These are source/ownership facts, not a promised net DLL or download delta.

## Changed Areas

- `products/first-party/StrongPlantingGun/**`;
- deleted
  `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/**` and removed
  the mandatory callback, demand, feature, owner-cleanup and API-provider
  routes;
- retired `testmods/StrongPlantingGunMod` and its solution/build projection;
- added warning-bearing Frozen/Obsolete metadata without changing the
  `IStrongPlantingGunApi` or three DTO member shapes;
- added one tracked Advanced reference policy and reused the existing
  Catalog-driven SDK/package, Doctor and install projections;
- extended existing Unit, ABI and QA fixtures for the five-target owner,
  native item/UI/save and Loader-deactivation boundary;
- updated the current Phase 0 contract while retaining its historical source
  projection separately.

## Validation

Focused validation passed on the integrated candidate:

1. `build-batch6-advanced-product.ps1 -CatalogId
   strong-planting-gun -Configuration Release` built through the generic
   Author SDK, produced a dependency-clean package and passed its package
   checks.
2. Central Unit focuses `strongplantinggun-product`,
   `batch5-gamebridge-demand`, `api-metadata` and `batch6-advanced-core`
   passed. The Strong focus now builds a fake `Assembly-CSharp` and physically
   executes the production Hook installer, callbacks, native runtime and
   `ModEntry.Dispose` against all five Harmony targets. It covers a retained
   seven-slot JSON inventory with a three-slot function followed by an
   ordinary three-slot gun, compatibility-first rejection, partial-install
   rollback, exact-owner cleanup, unrelated-owner preservation and truthful
   non-zero residue after both install and deactivation unpatch failures. Tool
   and UI fixtures inject `TryCostAtIndex`, `Take`, `PlaceItem`, `DoInteract`
   and receipt/log exceptions after native mutation; every case restores the
   exact item/count/slot-lock state, resynchronizes later native receivers even
   if another receiver throws, and suppresses the original method. The focus
   also covers bounded logging, cached reflection metadata, the short
   allocation gate and a JSON gun constructed while the product is suspended
   at title, then repaired through the real static archive/backpack route at
   `SaveLoaded`. A direct failure fixture throws after the first prepared gun
   and proves exact function-capacity restoration plus zero Hooks, callbacks,
   cached metadata, snapshots and roots.
3. QA and QA Unit Release builds passed with zero warnings/errors. The
   positive third-save route parsed, while missing the required single
   save/title/re-entry cycle and requesting two cycles were both rejected.
4. The Phase 0 contract and product Catalog passed. Direct source scanning
   found zero StrongPlantingGun executor, Hook, callback, demand or native-type
   tokens in mandatory GameBridge and the Compatibility Host.
5. InstallDoctor Unit passed its exact Catalog policy-set check; the current
   Player Doctor rebuilt and passed its focused package gate.
6. Reflection against the built Abstractions DLL confirmed the exact
   interface/DTO member names plus Obsolete, Experimental and Frozen
   metadata. The existing exact retained AutoFishing script was not an
   applicable Strong consumer check: one exploratory invocation was rejected
   by mandatory artifact parameter binding before execution. No retained
   Strong binary exists, and the ABI harness containing the exact Strong shape
   checks builds successfully.
7. The first post-fix launch at `GAME-SMOKE/20260724-101553` was a
   non-acceptance diagnostic: the installer had reused the prior product
   package, and the loaded DLL still had SHA-256 `39F4...`. The current package
   was then deployed through the existing Author SDK `update` transaction plus
   exact Local source selection; no DLL was copied by hand.
8. `GAME-SMOKE/20260724-101757` passed the protected third-save route with the
   current DLL. It proved startup/HookProbe, seed/film/fertilizer behavior and
   native save, return to title, one bounded third-save reload, the already-
   deserialized gun repaired to `inventory/total/line=3/3/3`, all five real
   Harmony owners, then real Loader deactivation to
   `listeners/callbacks/hooks/cachedObjects/cachedMembers/capacitySnapshots/
   roots=0`. Title lifecycle, QA host lifecycle, save/config/external-state
   restoration, the post-run Player Doctor check and clean process exit passed.

The independent closeout review found no remaining P0/P1/P2 after the direct
SaveLoaded failure fixture was added and independently passed the focused
Strong Unit. This Update is therefore `verified/closed`. No complete Release,
L0-L5, GC gradient or long test was run.

## Evidence

Focused artifacts:

- Advanced product package SHA-256:
  `05427553A1CE9ED6FCBCF58879654A3D1F0627F3859CB21A5DEE303D8A775283`;
- Advanced entry DLL SHA-256:
  `7FC7AA3592166A486048F120AB096FF3747F7230C8473BB09DE775DF8B93B023`;
- rebuilt Player Doctor SHA-256:
  `4130D03EEF814E4038358688EC14641727C9D21BF1F1ADC473DFA5AD4E0968C9`.

The current candidate measures:

- five mandatory Runtime projects: 67,224 -> 66,137 physical lines,
  60,195 -> 59,241 non-empty lines and 2,155,520 -> 2,132,992 DLL bytes;
- mandatory GameBridge: 28,592 -> 27,476 physical lines,
  25,324 -> 24,341 non-empty lines and 851,456 -> 825,344 bytes;
- new Strong product: 11 C# files / 2,860 physical / 2,676 non-empty lines /
  46,592 DLL bytes;
- all nine products: 20,533 physical / 18,894 non-empty lines / 483,328 DLL
  bytes;
- mandatory plus those products: 86,670 physical / 78,135 non-empty lines /
  2,616,320 DLL bytes;
- the dormant-shipped Compatibility Host remains 364,032 bytes.

Relative to the eight-product baseline, mandatory Runtime is smaller by
1,087 physical lines, 954 non-empty lines and 22,528 DLL bytes. Counting the
new ProductNative assembly back in makes mandatory-plus-products larger by
1,773 physical lines, 1,722 non-empty lines and 24,064 DLL bytes.
Therefore this implementation proves only a smaller default-loaded Runtime.
It does not reduce repository source, download, install or total shipped
volume. Runtime evidence, if accepted, will be added to the existing active
smoke matrix and this same Update. No new receipt family is created.

## Rollback

The implementation must remain an atomic Catalog/source/identity switch.
Rollback reverts this Update's implementation commit and restores the prior
legacy product plus mandatory StrongPlantingGun route together. Do not restore
only one side, leave a ProductNative DLL with mandatory Hooks, add a
Compatibility executor without a real consumer, or use destructive Git
operations across unrelated work.

## Follow-Up

- The ninth-product horizontal comparison records only the measured
  default-loaded Runtime change; no tenth product is admitted here.
- Mine, G7, 0.5.5 publication and every tenth-or-later product remain blocked.
