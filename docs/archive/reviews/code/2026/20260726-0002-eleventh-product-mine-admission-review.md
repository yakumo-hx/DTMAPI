# Eleventh Product Mine Admission Review

**Review ID:** `20260726-0002`

**Date:** 2026-07-26

**Status:** accepted and closed after corrective Review `20260726-0003`

**Scope:** independent admission of `DTMAPI.MineMod` after the ten-product
closeout; native-owner/body review, compatibility disposition, persistence
boundary and minimum acceptance only

## Source Request

After closing the Catalog, C1 and Manager audit debt, admit and split Mine as
an independent ProductNative product. The correction explicitly rejects the
earlier assumption that Mine's next-cycle scheduler must persist across exit.

## Verdict

**GO: admit exactly `DTMAPI.MineMod` as the eleventh Advanced ProductNative
product, with its existing official JSON remaining ContentOwner.**

This Review authorizes one implementation Update. It does not admit a generic
machine API, SharedNative machine host, new Compatibility Host executor,
Mine sidecar, scheduler journal, candidate generation or a twelfth product.

Safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The current `24256979_test_7A1907` bodies were inspected before this decision:

- `ElectronicComponentAppliance.Launch()` consumes the prototype threshold
  from the native `[JsonProperty] power` field;
- `Case` owns its `[JsonProperty] LinearInventory inventory`, validates it
  against the official `EquipmentFuncCase` capacity and exposes native
  `ContentFilter`;
- `LinearInventory.PlaceItemAt`, `Take`, `Copy` and `Overwrite` own item
  placement and rollback mechanics;
- `EquipmentManager.CreateEquipment` maps `EquipmentFuncCase` to native
  `Case`;
- `ArchiveDataHandle.DateNow.TotalTUs` is the observed game-time clock, while
  `PassTimeNoControl` advances native time and native equipment updates.

These bodies establish a usable native owner. Mine may therefore be admitted
without inventing a platform machine abstraction.

## Frozen Identity

| Fact | Value |
| --- | --- |
| UniqueID | `DTMAPI.MineMod` |
| Catalog ID | `mine` |
| Workshop / retained published version | none |
| Current prototype version | `0.5.1-alpha-dtmapi` |
| Admitted target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Official folder | `DTMAPI_Mine` |
| Package DLL | `DTMAPI.Mine.dll` |
| Canonical config | `DTMAPI/config/DTMAPI.MineMod.json` |
| Canonical Harmony owner | `dtmapi.mod.dtmapi.minemod` |
| Product save sidecar | none |

The existing Catalog-driven Author SDK must generate policy, receipt and
package. No hand-authored Advanced manifest or receipt is authorized.

## Physical Ownership

| Responsibility | Owner |
| --- | --- |
| item, equipment, 16-slot case, recipe and workbench group declarations | official JSON ContentOwner |
| placed equipment identity and serialization | native `Equipment` / `Case` |
| stored outputs and slot state | native `LinearInventory` |
| electric charge and fixed cost | native `ElectronicComponentAppliance` with JSON threshold `10` |
| game clock | native `ArchiveDataHandle.DateNow.TotalTUs` |
| cycle interval, weighted output choice and session due entries | Mine ProductNative |
| optional Oil recipe override and Mine tech-node mutation | Mine ProductNative transaction |
| placed renderer and builder-preview 2x scale | Mine ProductNative transaction and exact Harmony owner |
| package, config menu, lifecycle roots and owner cleanup | existing Platform contracts |

There is one product consumer. Nothing in this table proves SharedNative.

## Corrected Scheduler And Save Mode

Mine uses a **session-derived scheduler**:

1. Every `SaveLoaded` clears every due entry and discovers each current Mine
   from the current native `TotalTUs`; its first due time is
   `current TotalTUs + configured cycle`.
2. Returning to title, config disable, Loader owner cleanup and process
   shutdown clear scheduler entries, RNG state and cached native objects.
3. Reloading a save may therefore restart the current cycle. The first
   release does not promise reload-refresh prevention or exact cross-save
   cycle continuity.
4. Only native case inventory and native electric charge persist through the
   official save.

Consequently there is no Mine sidecar, Working/Committed state, candidate
generation, journal, archive fingerprint or interrupted-notification
reconciliation matrix. A future request for exact cross-exit cycle continuity
must reopen this Review before adding any persistence.

## Configuration Corrections

- `Enabled` means **runtime production enabled**. Cold `false` installs no
  Mine Harmony owner, runs no production polling or scheduler, and leaves no
  recipe/tech/scale mutation.
- Power is fixed at native JSON threshold `10`. The old editable
  `ElectricPowerCostPerCycle` is removed and stale config input is ignored.
- `IncludeRuntimeModMinerals` is removed. Runtime output is only the explicit
  coal/copper/iron rules plus Oil when that exact product is loaded.
- Cycle duration, explicit output weights, optional Oil recipe choice and
  bounded diagnostics remain product configuration.

## Original-Value And Failure Contract

Activation must be transactional:

- capture the exact original recipe input array before optional replacement;
- capture the exact original tech table/list and tree-map values before
  inserting or updating the Mine node;
- capture every renderer/preview `localScale` before applying 2x;
- pre-resolve all three visual hooks and install all-or-none;
- if activation fails, reverse every completed step and publish failure
  instead of a partially active product;
- disable, title and Loader cleanup restore exact originals in reverse order,
  clear all scheduler/RNG/cache/callback roots and remove only the Mine owner.

A production cycle must preflight the native case, output item and capacity.
If a failure occurs after native power or inventory mutation, restore the
exact pre-cycle power and inventory snapshot. Failed work remains due rather
than advancing the scheduler.

## Compatibility Disposition

`IMachineProductionApi` has one tracked source consumer: this Mine prototype.
Catalog records no Workshop ID, published version, retained artifact or
independent binary consumer. The external universe cannot be proved empty, so
the interface and DTO member shape remain loadable with warning-bearing
Experimental/Deprecated/Frozen metadata, but:

- the admitted Mine product must not consume it;
- mandatory GameBridge and the existing Compatibility Host must contain no
  MachineProduction provider or executor;
- no synthetic compatibility consumer may justify a new Host body;
- discovery of an exact retained binary before cutover stops implementation
  and reopens this disposition.

### Author SDK compatibility payload re-sign

Adding the warning-bearing `DtmApiDisposition(Frozen)` and
`Obsolete(..., false)` metadata changes the deterministic bytes of
`DTMAPI.Abstractions.dll`, although the interface and DTO type/member shape,
assembly version `0.5.3.0` and file version `0.5.5.0` remain unchanged. The
tracked Author SDK contract correctly rejected the new bytes.

Two consecutive deterministic Release builds produced the same candidate
SHA-256:
`2EEB43D85D8CF22001C08EC082C0A4600F6964B5BBA60FAD6FB4A60B8160C335`.
This Review therefore authorizes the same minimal re-sign procedure already
used for ProductNative boundary cleanup: update only
`author-sdk/compatibility/0.5.5/compatibility.contract.json`'s
`abstractionsSha256`, then require the ABI harness, Author SDK build/check and
product package build to pass. SDK/runtime versions, author props,
NETStandard inventory and public members may not change to absorb the hash.

## Minimum Acceptance Matrix

### Source, ABI And Package

- exact Advanced identity/policy/package generated by the existing SDK;
- `netstandard2.0`, only approved `Assembly-CSharp` / `0Harmony` native
  references;
- frozen Machine interface/DTO member shape with zero Runtime providers;
- zero MachineProduction definition/state/executor, demand route or visual
  callback body in mandatory GameBridge/Compatibility;
- no Mine sidecar or scheduler persistence token.

### Lifecycle And Transactions

- Enabled false from cold start: zero hooks, callbacks, scheduler, recipe,
  tech and scale mutations;
- enable success: exact recipe/tech state, three exact visual hooks and
  session scheduler;
- injected activation failure: exact reverse rollback and truthful failure;
- disable/re-enable, title/re-entry and real Loader owner cleanup: exact
  originals restored, all roots zero, one clean reinstall only from zero.

### Runtime Behavior

- fixed native power cost `10`, with low power producing nothing;
- coal `18`, copper `10`, iron `6`, optional Oil `2`; counts remain
  `1–2`, `1–2`, `1`, `1`;
- outputs go only to the Mine-owned native 16-slot case;
- full/rejected/failing storage conserves power and items;
- SaveLoaded restarts due time from current native `TotalTUs`;
- official native save persists case inventory/electric charge only;
- 2x placed and preview scale never contaminates non-Mine pooled renderers.

## Runtime Acceptance

One bounded game acceptance is required. The default protected third slot is
used unless the existing Mine fixture authority requires another isolated
fixture. `NoNativeSave` may prove runtime, restart-derived scheduling,
disable/title cleanup and visual containment. If native inventory/power
durability is intentionally tested, classify it as `NativeSaveExpected` and
use a disposable fixture isolated from live Steam AutoCloud.

No complete Release, L0–L5 ladder, GC gradient or long test is required for
this bounded migration.

## 2026-07-26 Runtime Root-Cause Note

Three bounded `NoNativeSave` attempts were non-acceptance and are retained as
diagnostic evidence:

1. `GAME-SMOKE/20260726-195704` stopped before product behavior because QA
   Host settings had not admitted `DTMAPI.MineMod` as an allowed requested
   owner.
2. `GAME-SMOKE/20260726-200316` used the `Current` official profile, so the
   Mine ContentOwner JSON was intentionally absent and `dtmapi_mine` could
   not be created.
3. `GAME-SMOKE/20260726-200534` loaded the official JSON and transiently
   installed the expected three Mine patches, but Core then reported
   `advanced-harmony-late-owner-drift` and deactivated the owner.

The third failure is an implementation lifecycle defect. Mine installed its
first Harmony patch set from `SaveLoaded`, after Advanced `Entry` supervision
had frozen an empty patch inventory. The product-local `hooks=3` activation
log was therefore true only before Core's next audit; QA's later `0/3`
observation truthfully saw the already-quarantined owner.

Rejected hypotheses are an observer owner/target mismatch, an official JSON
reload failure and hook-target resolution failure. The accepted correction is
to install the exact all-or-none visual patch set during cold `Entry` when
`Enabled=true`. `SaveLoaded` may then apply recipe/tech state and start the
session-derived scheduler. Title/config disable may remove that exact
Entry-observed set, and save re-entry may reinstall only those same identities
through the platform's existing allowed-reactivation contract. Cold
`Enabled=false` observes no Entry patch set, so enabling it later in that
process is restart-required and must not add late patches. Entry-installed
callbacks remain inert until a save-scoped activation succeeds.

After that correction, `GAME-SMOKE/20260726-201948` proved the supervised
Entry inventory as `3/3` and the exact native Mine item, case geometry,
electric threshold, recipe, technology route and generated item. It stopped
before production because the QA assertion also required a Content Registry
row and indexed icon. An unrelated enabled Workshop package had already made
the platform's all-content candidate fail closed, so the optional index was
unavailable even though the native ContentOwner data was complete.

That is a fixture-ownership defect, not a Mine product failure. This
admission's ContentOwner authority is the official native tables; the
platform Content Registry projection is supplemental and cannot become a
second admission gate for ProductNative behavior. The correction retains the
native item/equipment/recipe/group/tech and owner assertions, records any
available index metadata, and no longer fails Mine because an unrelated
third-party content candidate suppresses that projection.

`GAME-SMOKE/20260726-202207` then passed those native/owner checks and captured
the official technology UI, but timed out before production. The fixture
advanced native time roughly `0.36s` after creating its transient Mine while
the ProductNative discovery poll is deliberately bounded to `0.5s`. Depending
on event order, the scheduler therefore first discovered the Mine only after
the time jump and correctly derived its first due time from that later native
clock. The rejected hypothesis is an inactive Update subscription: owner
roots showed the third Mine handler registered without failures. The fixture
must observe a real scheduler entry before advancing time; counting an
arbitrary number of QA frames is not proof that the product's bounded poll has
run.

`GAME-SMOKE/20260726-202535` observed that entry and still retained the due
cycle with no output. The transient native appliance starts with zero charge;
the fixture had never prepared its electric precondition. This rejects a
product-side bypass or test-only force-production hook: the native
`ElectronicComponentAppliance.Launch()` must remain the only consumption
owner and must continue to refuse low power. The corrected QA setup calls the
temporary appliance's native `ChargeToFull()` before advancing time, verifies
the reviewed threshold `10`, and then exercises ordinary ProductNative
production. The transient equipment and its charge remain covered by the
existing case-local cleanup and `NoNativeSave` boundary.

The first charged-fixture attempts, `GAME-SMOKE/20260726-202944` and
`GAME-SMOKE/20260726-203154`, failed before calling that native method because
QA first used an integer-only helper and then a generic member helper that did
not resolve the runtime component's native `float Threshold` getter. The
Review's inspected body already records the floating-point native owner; the
next attempt, `GAME-SMOKE/20260726-203429`, also showed that compiler-generated
property accessors are not exposed by this runtime reflection surface. The
fixture therefore invokes only the exact public native `ChargeToFull()`
method. The preceding official-proto check and the ProductNative cycle's own
runtime threshold guard remain the two independent assertions that the
threshold is `10`; successful ordinary production proves that the native
charge was accepted without adding a product bypass.

`GAME-SMOKE/20260726-203638` completed the native charge and scheduler setup
but still timed out without a cycle status line. At this point low power is no
longer an accepted explanation, and changing ProductNative semantics would be
premature. The next diagnostic adds the product's existing
`BuildStatusSummary` to the QA timeout so the retained due state, native
preflight result and Update subscription can be distinguished from fixture
timing without a force-due or force-poll hook.

`GAME-SMOKE/20260726-203908` reported `active=True`, `hooks=3/3`,
`placed=1` and `scheduler=1`, but no cycle message. This proves the update
subscription, discovery and session entry while narrowing the missing fact to
the scheduler clock comparison. The product's bounded status summary now also
reports its already-owned `LastObservedTotalTUs` and `NextDueTotalTUs`; this
is diagnostic visibility only and does not alter the scheduler.

`GAME-SMOKE/20260726-204231` exposed the first scheduler defect:
`observedTU=39243` while `nextDueTU=39218`, with one visible placed Mine and
one scheduler entry. The global `newTu` gate could consume a time change while
native pass-time temporarily made that equipment unavailable to enumeration;
when the equipment became visible again in the same TU, the overdue entry was
permanently skipped. Moving that guard onto each machine let late-visible
equipment attempt in the same TU, but `GAME-SMOKE/20260726-204535` showed that
the per-entry TU guard still stranded an overdue cycle after a resource
failure: the scheduler remained due with no output.

Inspection found two distinct causes. First, `BuildStatusSummary` displayed
the runtime activation message instead of the machine state's failure
message, hiding whether native power or inventory rejected the attempt.
Second, game TU is not a valid retry key for a resource failure: power can be
connected and storage can be freed while game time remains unchanged. The
existing `0.5s` product poll is already the retry rate limit, and each failed
catch-up loop breaks immediately while retaining its due time. The accepted
correction therefore removes TU-based failure suppression and exposes the
machine message in diagnostics. The QA fixture also calls the exact native
`ChargeToFull()` after `PassTimeNoControl`, because the native time transition
may legitimately refresh a transient unnetworked appliance before the
ProductNative cycle executes. No force-poll, force-due, direct production
callback, sidecar or product-side power bypass is introduced.

`GAME-SMOKE/20260726-205254` then exposed the formerly hidden machine message.
The official content-table assertion had already proved
`EComProtoAppliance/10`, but reflection against the live component's
`Threshold` property returned unavailable (`-1`). Treating an unavailable
diagnostic getter as a mismatched native contract was a product defect. Mine
now forces even its internal normalized definition to
`FixedPowerCost=10` and invokes only the official parameterless `Launch()`;
that native owner consumes its own prototype threshold and returns the actual
low-power decision. The product retains the exact pre-cycle power/inventory
snapshot and restores it on any later failure. It does not read a configurable
power value, call the custom-threshold overload, or manufacture an alternate
power state.

## Inspected Authorities

- `PROJECT.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md`
- `docs/reviews/api/2026/20260726-0001-mine-productnative-admission-prerequisite.md`
- `docs/api/public-api-matrix.md`
- `tools/release/dtmapi-product-catalog.json`
- current Mine source and MachineProduction GameBridge executor
- reverse build `24256979_test_7A1907` method bodies named above

The diagnostic attempts above remain non-acceptance evidence. Final
`GAME-SMOKE/20260726-205813` is accepted only for the behavior it actually
observed: official JSON and native tech UI, exact `3/3` Harmony owner, one
charged Mine's session-derived due cycle, native parameterless `Launch()`,
fixed contract `10`, `coal x1` in the Mine-owned `16/4` inventory, title and
Loader cleanup to zero, `NoNativeSave` preservation, no fatal window and
clean process exit.

Independent Review `20260726-0003` supersedes the earlier claim that this one
run passed the complete bounded matrix. Low power, full/rejected storage, two
Mines, move/removal/index reuse, cold-disabled startup, re-enable and injected
activation failure therefore became acceptance gates while the owning Update
was in progress. The admission/physical-owner decision remained accepted without
widening SharedNative, Compatibility Host or public API ownership.

## 2026-07-26 Corrective Acceptance

The missing source and evidence gates are now closed:

- focused deterministic fixtures prove object-identity scheduling,
  authoritative prune, two-Mine independence, move/remove/index reuse, cold
  enable decisions, injected activation rollback, low/full/rejected
  non-mutating preflight and exact post-Launch rollback;
- `GAME-SMOKE/20260726-223105` proves a real cold-disabled Entry with
  `active=False`, `hooks=0/3` and an empty session scheduler;
- after exact config restoration, final
  `GAME-SMOKE/20260726-223236` proves the real two-Mine low-power/full-storage/
  independent-success matrix, hot disable/re-enable, stable move, dismantle
  prune, reused-index fresh identity, title cleanup and Loader owner/root zero.

The real full-storage path remains at `16/16` and consumes no native power;
the low-power paths construct no output. A replacement at the removed Mine's
old index reports a new first-observed TU and a fresh due TU. Protected
NoNativeSave state, config and deployment transactions restore exactly and
the process exits cleanly.

This closes implementation acceptance without adding a Mine sidecar,
Compatibility executor, SharedNative owner, public Machine API provider or
publication promise for the borrowed well art/2x prototype.
