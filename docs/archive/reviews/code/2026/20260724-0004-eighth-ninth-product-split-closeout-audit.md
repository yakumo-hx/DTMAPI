# Eighth And Ninth Product Split Closeout Audit

Status: recorded; later save-commit correction independently reaccepted;
exact nine-product baseline restored

Date: 2026-07-24

## Later Correction — 2026-07-24

The user-confirmed no-save rollback semantics expose a P1, not an acceptable
P2 performance choice: shield damage/break commits sidecar state immediately,
and ordinary equip/replace/unequip prepared intent can survive without a
matching successful native save. The old-ABI Host has the same conditional
ordinary-operation risk. The owning Update is reopened `in-progress`; exact
root cause and acceptance now belong to
[the manual-QA save-commit Review](../../manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md).

This correction supersedes the original Verdict's “no P1”, the P2
measure-or-accept choice, the blanket eighth-product data-safety closure and
Route items 1–2 below. It does not retract the physical ProductNative split,
default-loaded Runtime measurements, tested management recovery/owner evidence
or StrongPlantingGun's independent verified state.

## Later Reacceptance — 2026-07-24

The reopened P1 is fixed. Explicit Working/Committed state, typed
GameplayMutation versus Owner/OrphanRecovery, native-success promotion and
exactly-one-item crash-window handling pass through both ProductNative and the
real frozen Compatibility Host. `155216`/`155344` are accepted metadata-only
NoNativeSave title/cold evidence; `161422`/`161536` are accepted disposable
Steam-AutoCloud-isolated native-save/promotion/cold evidence.

`154906`, `155811` and `161030` remain non-acceptance diagnostics. Historical
`PlayerSaveRestored=Passed` is not reinterpreted as no-save proof. An
independent source/test/evidence review found no remaining P0/P1/P2, so the
eighth product and exact nine-product aggregate return to verified/closed.
The prior synchronous-per-hit I/O finding is eliminated by the same
Working/Committed change rather than accepted as performance debt.

## Scope

This independent audit reviews the final eighth- and ninth-product boundaries:

- MoreEquipmentSlots from admission at `858bd607` through the corrected and
  frozen eight-product baseline at `3db3b5e1`;
- StrongPlantingGun from admission at `93723636` through the final
  save-reentry correction at `750e6513`;
- the resulting nine-product mandatory Runtime, ProductNative,
  Compatibility Host, API and runtime-evidence boundaries.

It follows the earlier detailed eighth-product commit-range Review
`20260724-0001` rather than repeating its transaction and native-behavior
analysis. Unrelated local portable reverse-capture work is outside scope and
was not changed.

## Verdict

There is no remaining P0 or P1 in either split.

Both products are physically owned by their managed Advanced assemblies, the
mandatory Runtime no longer contains their heavy executors, and neither split
invented a SharedNative owner or a new public API. The exact nine-product
`verified/closed` baseline is therefore valid.

StrongPlantingGun has no remaining P2. Its five-Hook transaction, capacity
rollback, title/SaveLoaded re-entry and Loader cleanup are supported by
focused executable checks and the current-DLL
`GAME-SMOKE/20260724-101757` evidence.

MoreEquipmentSlots remains `verified/closed` for migration correctness,
data safety, Hook ownership and lifecycle. This audit does find one new P2
performance debt: each real product-shield consumption synchronously performs
a durable JSON write, flush, read-back validation and replacement on the
Unity main thread. That is not an idle or per-frame Runtime cost and does not
invalidate the split, but the earlier blanket statement that no P2 remained
was too broad.

The focused MoreEquipmentSlots Hook map was also stale. It still described the
product as awaiting independent review and cited superseded evidence. This
audit corrects that canonical Hook record; it does not rewrite the historical
Update or evidence receipts.

## Findings

### P2 — Product-shield damage performs synchronous durable I/O

`MoreEquipmentSlotsNativeRuntime.HandleAttackPrefix` clones the current slot
record and, whenever `blocked > 0`, calls
`PersistDocument("shield state changed")` before the native attack tail can
finish. It can then redraw the current UI.

`EquipmentSlotDocumentStore.WriteAtomic` performs all of the following in that
same call:

1. validates the current live or previous document;
2. creates a serializer and writes a temporary JSON document;
3. calls `FileStream.Flush(flushToDisk: true)`;
4. reopens and deserializes the temporary document for round-trip validation;
5. replaces the live file with `File.Replace`.

This is a correctness-oriented durable transaction, not file polling. It is
also synchronous disk I/O plus allocation in a Harmony attack Prefix.
Continuous combat can therefore turn repeated shield changes into a player-
visible hitch or allocation source. The current focused tests prove rollback
and data correctness but do not quantify per-hit time, bytes or I/O.

This P2 does not block the accepted product split. Before another product is
admitted, the project should either:

- measure and explicitly accept the per-hit cost; or
- batch/dirty the mutation and persist it at a defined safe lifecycle or
  bounded checkpoint while preserving shield durability, crash recovery and
  the existing no-loss/no-duplication rules.

The smallest verification is a focused per-hit timing/allocation/I/O check and,
if behavior changes, one bounded real shield-hit test. It does not require a
complete Release, L0-L5, a GC ladder or a long test.

### P3 — Strong SaveLoaded repair is deliberately backpack-top-level only

The current StrongPlantingGun contract scans the loaded backpack once at
`SaveLoaded`. The final smoke proves an already-deserialized backpack Farming
Gun is restored to `3/3/3`.

It does not prove immediate SaveLoaded repair for a unique gun stored in a
nested container or warehouse. Exact tool/UI paths still prepare a gun when it
is later used. This is not a defect against the admitted contract, but future
documents must not generalize the current evidence to every storage location.
If immediate nested-storage repair becomes a product requirement, it needs a
separate product-owned design and focused fixture.

## Eighth Product Review

### Physical and compatibility ownership

- MoreEquipmentSlots owns fixed-three gameplay, its protected sidecar and
  journal, UI clones/listeners, configuration and four exact native Hooks.
- The mandatory GameBridge retains only the thin frozen-ABI demand route.
- The legacy arbitrary-owner `0..24` executor and cold recovery remain in the
  single dormant-shipped Compatibility Host.
- The new product does not consume `IEquipmentSlotsApi`.
- No MoreEquipmentSlots executor, callback or ProductNative Hook owner remains
  in mandatory Runtime.

The two commits after the final corrective source candidate did not change
the product, Host or mandatory production implementation. They froze
documentation and added a no-demand QA fixture.

### Runtime shape

The product has no `UpdateTicked` listener, thread, timer,
`FileSystemWatcher`, or per-frame file probe. Its work is driven by save/title
lifecycle and four exact native actions. Compatibility cold recovery performs
a cached, at-most-once SaveLoaded path probe when needed; ordinary startup
does not load the Host.

The new shield-write P2 is therefore an enabled, event-frequency cost. It is
not evidence that ordinary players or disabled-product sessions acquired a
new idle workload.

### Evidence precision

The final accepted eighth-product evidence is:

- focused `moreequipment-product` and `compatibility-host` executable checks;
- current Catalog validation;
- `GAME-SMOKE/20260724-053248` for the corrected enabled product;
- `GAME-SMOKE/20260724-053342` for exact ProductNative-v3 cold recovery with
  the product disabled.

The clean complete Release PASS belongs to the earlier `d38af17a` candidate.
The final corrected candidate ran from the beginning only to the stale
evidence-allowlist stop; after the allowlist correction, the exact previously
unreached tail passed. Per the user instruction and the product's original
admission boundary, the complete suite was not run again. This is sufficient
for the accepted split, but it is not a clean final-candidate complete Release
PASS.

Historical `GAME-SMOKE/20260724-031003` remains evidence for legacy-ABI orphan
recovery, not final ProductNative-v3 cold recovery.

## Ninth Product Review

### Physical and API ownership

- StrongPlantingGun owns fixed seed/film/fertilizer slots, capacity snapshots,
  reflected native access, configuration and five exact Harmony patches.
- The game retains Farming Gun inventory serialization, backpack/container
  objects, PlantBasin behavior and native UI state.
- The mandatory executor, Hook bridge, callbacks, provider and demand route
  were removed.
- No retained Strong consumer binary exists. `IStrongPlantingGunApi` and its
  three DTOs therefore remain a fail-closed Frozen warning shell without a
  Compatibility Host executor.
- The product does not consume the frozen API and adds no public API.

The Core supervisor correction permits only the exact already-observed owner,
target, patch kind and patch method to reappear within its original count
after the title lifecycle. Duplicate or different patches still fail closed.
Focused tests and the final real-owner smoke support this as a bounded generic
Advanced-owner lifecycle correction, not ProductNative gameplay moved into
Core.

### Hook, rollback and lifecycle

The two constructors, tool action and two Farming Gun UI methods install as
one five-Hook transaction. Resolution or partial-install failure restores
capacity snapshots and clears the exact owner, callbacks, reflected member
cache and product roots while preserving unrelated Harmony owners.

The final evidence reports:

- native save behavior and seed/film/fertilizer state;
- one title return and third-save reload;
- one already-deserialized backpack gun restored to `3/3/3`;
- all five real Harmony patches;
- Loader deactivation to zero listeners, callbacks, Hooks, cached objects,
  cached members, capacity snapshots and roots;
- current Player Doctor, save/config/source restoration and clean process
  exit.

The earlier `101553` launch used a stale product package and is correctly
classified as non-acceptance. The accepted `101757` package and DLL identities
match the final Update.

## Runtime Weight And Total-Cost Recalculation

The mandatory Runtime reduction is real:

| Frozen baseline | Five mandatory projects, physical / non-empty | Mandatory GameBridge, physical / non-empty | Five mandatory DLLs |
| --- | ---: | ---: | ---: |
| Seven products, `858bd607` | 69,948 / 62,613 | 31,336 / 27,762 | 2,240,000 bytes |
| Eight products, `3db3b5e1` | 67,224 / 60,195 | 28,592 / 25,324 | 2,155,520 bytes |
| Nine products, `750e6513` | 66,137 / 59,241 | 27,476 / 24,341 | 2,132,992 bytes |

Therefore:

- seven to eight removes 2,724 physical lines, 2,418 non-empty lines and
  84,480 mandatory DLL bytes;
- eight to nine removes another 1,087 physical lines, 954 non-empty lines and
  22,528 mandatory DLL bytes;
- seven to nine removes 3,811 physical lines, 3,372 non-empty lines and
  107,008 mandatory DLL bytes from the default-loaded five-project Runtime.

That is not total-product or repository slimming:

| Boundary | Mandatory plus product sources, physical / non-empty | Mandatory plus product DLLs | Compatibility Host |
| --- | ---: | ---: | ---: |
| Seven products | 81,711 / 73,336 | 2,583,552 bytes | 209,408 bytes |
| Eight products | 84,897 / 76,413 | 2,592,256 bytes | 364,032 bytes |
| Nine products | 86,670 / 78,135 | 2,616,320 bytes | 364,032 bytes |

The eighth step increases mandatory-plus-products plus Host bytes by 163,328.
The ninth adds a further 24,064 bytes. Tracked Git text grows by a net 15,459
lines from seven to eight and another 5,518 from eight to nine, principally
because ProductNative, Host compatibility, tests and evidence remain in the
repository.

The correct claim is therefore:

> The eighth and ninth splits reduce the default-loaded DTMAPI Runtime and
> move optional work behind product/compatibility demand. They do not reduce
> repository size, download/install size, total shipped code or the cost when
> those products are enabled.

## Reuse And Shared-Boundary Review

Both products reuse the generic Advanced Catalog, SDK/package builder,
Loader/owner lifecycle, Doctor/Manager, configuration menu and diagnostic
infrastructure. This is real Platform reuse.

They do not share a native owner with each other or with a second real product:

- Equipment owns four equipment/UI targets plus its product sidecar;
- Strong owns five Farming Gun construction/action/UI targets;
- neither consumes its historical gameplay API.

The comparison therefore provides no basis for a new GameBridge SharedNative
adapter, a new public API, or a common gameplay Host. Repeated Hook-transaction,
reflection-cache or cleanup mechanics may later be compared as SDK/Platform
tooling, but repeated implementation shape alone is not native-owner evidence.

## Validation Performed

On current HEAD `750e6513`:

- `moreequipment-product` focused Unit: PASS;
- `compatibility-host` focused Unit: PASS;
- `strongplantinggun-product` focused Unit: PASS;
- `batch6-advanced-core` focused Unit: PASS;
- `batch5-gamebridge-demand` focused Unit: PASS;
- `api-metadata` focused Unit: PASS;
- Product Catalog: PASS, `27 / 11 / 21 / 48`;
- Phase 0 contract: PASS;
- Strong Advanced SDK/package build: PASS; its package SHA-256 matches the
  owning Update;
- `git diff --check 034ea5e6..3db3b5e1`: PASS.

This audit inspected existing accepted game evidence but did not launch the
game. It did not run a complete Release, L0-L5, GC validation, a long test,
installation, Workshop mutation or publication.

## Route After This Audit

1. Keep the exact nine-product baseline frozen.
2. Measure and resolve or explicitly accept the MoreEquipmentSlots shield
   persistence P2 without adding a new receipt or broad test suite.
3. Compare repeated product-local Hook installation, reflection cache,
   cleanup and logging glue. Extract only genuinely generic SDK/Platform
   mechanics; do not put them in GameBridge without a shared native owner.
4. Require a separate bounded admission Review before any tenth product.
   Mine remains `PrototypeBlocked`; this audit does not open Mine, G7,
   CustomAnimals or the 0.5.5 release.
