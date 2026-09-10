# Mine Independent Admission Prerequisite Review

**Review ID:** `20260726-0001`

**Date:** 2026-07-26

**Status:** recorded — correction plan ready; Mine admission remains blocked

**Scope:** Mine native-owner, configuration truth, activation/cleanup and save-mode review only; no eleventh-product admission, Advanced manifest/package, Runtime implementation, public API change, game launch, Release suite or 0.5.5 publication

## Source Request

Prepare the independent Mine admission prerequisite by directly investigating
and producing a correction plan for:

1. the fictitious configurable power value;
2. the relationship between `Enabled` and native/runtime registration;
3. the unconsumed runtime-mineral switch;
4. original-value capture, failure rollback and disable restoration;
5. native save mode, physical owner and the acceptance matrix.

This Review is the bounded prerequisite requested before a separate Mine
admission decision. It does not perform or authorize that decision.

## Admission Result

**NO-GO for admission from the current source. GO for the correction plan
below.**

The G1 authority remains unchanged:

- official JSON owns the static `dtmapi_mine` item, case equipment, recipe and
  recipe-group rows;
- a future separately admitted Mine Advanced CodeMod owns the single-product
  scheduler, RNG, due state, output policy, configuration, recipe/tech
  overrides and renderer lifecycle;
- the mandatory GameBridge gains no Mine executor, generic machine host,
  public DTO or speculative shared adapter.

The current prototype has one real consumer. Its 1,468-line
`IMachineProductionApi` implementation is therefore not SharedNative proof.
Extraction must remove the Mine execution body from the ordinary mandatory
Runtime path rather than copy it and leave both paths live.

## Evidence Inspected

- `PROJECT.md`, especially the canonical
  Platform/SharedNative/ProductNative/ContentOwner boundary and save-commit
  semantics;
- `docs/workflows/codex-api-rebuild.md`;
- `docs/architecture/batch6-managed-mod-identity-contract.md`;
- `docs/api/public-api-matrix.md`;
- G1 Review `20260722-0002` and Update `20260723-0002`;
- current Mine source, manifest and official JSON under
  `products/first-party/Mine`;
- current MachineProduction API, owner facade and cleanup path under
  `src/DTMAPI.GameBridge.DolocTown`;
- tracked decompile
  `references/doloc-town/reverse/builds/24256979_test_7A1907/decompiled/Assembly-CSharp`,
  including `EComProtoAppliance`, `ElectronicComponentAppliance`,
  `ElectricSystem`, `Equipment`, `EquipmentFuncCase` and `EquipmentWorker`;
- the current smoke runner save modes and active smoke matrix.

## 1. Fictitious Configurable Power

### Current product claim

`MineModConfig.ElectricPowerCostPerCycle` defaults to 10 and GMCM exposes a
0–100 “Power per cycle” option. `BuildMachineDefinition()` copies the value to
`MachineDefinition.ElectricModePowerCostPerCycle`, and status/log text repeats
it as if it were authoritative.

### Native and implementation facts

- official `equipment_tbequipment.json` creates an
  `EComProtoAppliance` with immutable public `Threshold=10`;
- native `ElectronicComponentAppliance.power` is the save-bound working
  buffer, while `RatedPowerConsumption`, `PowerGap`, `PowerProgress`,
  `ElectricSystem` charging and the parameterless `Launch()` all use
  `proto.Threshold`;
- the current GameBridge runtime resolves `IElectronicComponent` and invokes
  parameterless `Launch()` with zero arguments;
- `ElectricModePowerCostPerCycle` is used for telemetry and an early zero-cost
  bypass, but it is not written to the native prototype or component;
- native `ElectronicComponentAppliance` happens to expose
  `Launch(float customThreshold)`, but the native interface does not. Calling
  this overload alone would change the amount removed while the electric
  network would still advertise, allocate and refill against prototype
  threshold 10. It is not a coherent configurable-power contract.

### Correction decision

The initial Mine admission must use fixed native power 10:

- remove the editable power option from Mine GMCM;
- remove the field from the active ProductNative policy and migrate/ignore the
  stale serialized field with one clear non-fatal warning;
- show a read-only `Native power per cycle: 10` value obtained from the actual
  Mine equipment prototype/component after load;
- fail closed if the expected native prototype is absent, not an appliance, or
  has a threshold other than the tracked policy value;
- do not mutate the global equipment prototype and do not call
  `Launch(float)` as a shortcut.

A later independently reviewed economy feature may make power configurable
only if it owns a coherent native electric-system model: rated load, charging,
deduction, UI/telemetry, existing placed components, save/load and restoration.
That is outside this admission prerequisite.

### Acceptance gate

For a charged Mine, one successful cycle must reduce the native appliance
buffer by exactly 10 and native `PowerInfo`, `Threshold`,
`RatedPowerConsumption`, `PowerGap` and product diagnostics must agree. With
less than 10 available, production and output placement must not occur.

## 2. `Enabled` And Native Registration

### Current product claim

GMCM says `Enabled` “Registers the DTMAPI mine machine definition.”

### Code and owner facts

- `config.Enabled` is never read by `Entry`, `BindMachineApi`,
  `BuildMachineDefinition` or `SaveConfig`;
- `Entry` and every `SaveLoaded` call register the definition regardless of
  the configured value;
- official JSON registers the static item/equipment/recipe/group before the
  product runtime. A placed Mine is a native electric `Case`, and native room
  ownership registers its electronic component with `ElectricSystem`;
- product disable must not pretend to unregister that official content or
  remove a placed native component from its room electric system;
- current GameBridge owner cleanup deletes only DTMAPI definition/state/runtime
  dictionaries and demand roots. It does not restore recipe/tech mutations.

### Correction decision

Rename the semantic option to **Runtime production enabled** and give it this
exact contract:

- cold `false`: load configuration and diagnostics only; install no Mine
  Harmony patch/update callback, publish no scheduler/demand root, mutate no
  recipe/tech row and create no per-machine runtime state;
- cold `true`: activate the ProductNative transaction only after native
  content, owner and every snapshot prerequisite validate;
- hot `false`: stop new cycles, finish no queued output, restore every
  product-owned native mutation and remove every Mine callback/patch/cache/root
  atomically. Static content, placed cases, their inventories and native
  electric registration remain;
- hot `true`: perform the same all-or-none activation as cold start;
- if activation or deactivation cannot complete and reverse cleanly, restore
  the previous configuration and active state, report the exact failing
  resource, and require restart. Never publish a half-active or half-disabled
  success.

GMCM save must not write the new value as accepted before the transition
succeeds. It stages the normalized candidate, applies the transaction, then
persists on success. Failure restores and rewrites the prior value.

### Acceptance gate

Cold-disabled, hot-disabled, re-enabled, returned-to-title and Loader owner
deactivation must each end with zero Mine ProductNative patches, callbacks,
runtime entries, scheduler work and owner roots. Static `dtmapi_mine` content
and any already placed case remain native-owned and intact.

## 3. Unconsumed Runtime-Mineral Switch

### Current product claim

`MineModConfig.IncludeRuntimeModMinerals` defaults to `true` and is copied into
`MachineDefinition.IncludeRuntimeModMinerals`.

### Code facts

The repository has only three active source references:

1. the Mine config/definition write;
2. the public experimental DTO property;
3. MachineProduction normalization copying that property.

No output discovery, item registry, resolver or production reader consumes it.
The actual output list is the three hard-coded vanilla minerals plus optional
Oil.

### Correction decision

Remove the switch from Mine UI and ProductNative policy for the first
admission. Ignore/migrate the stale Mine config value and describe the actual
fixed output set truthfully. Do not expand the old public experimental
MachineProduction DTO during this task and do not build a generic mineral
registry for one product.

If runtime-discovered minerals are wanted later, they need their own product
economy decision and resolver contract covering source identity, item
eligibility, duplicate IDs, ordering, weights, load/unload, missing content and
save compatibility. Until that review passes, no diagnostic or status text may
claim automatic discovery.

### Acceptance gate

Source and built-artifact scans must find no active Mine read or UI label for
`IncludeRuntimeModMinerals`. A stale config containing either Boolean value
must load without changing the fixed output candidates and must emit at most
one migration warning.

## 4. Original Values, Failure Rollback And Disable Restore

### Current mutation defects

`RegisterMachine()` currently:

1. replaces the in-memory definition and publishes demand;
2. injects or mutates native tech state;
3. replaces global `TbRecipe.InputItems`;
4. publishes a successful registration even when a native helper returns a
   `failed=` or `pending=` summary.

There is no all-or-none commit. Recipe input arrays are not snapshotted.
Injected `TbTechNode` map/list entries, graph node payloads and graph edges are
not recorded for exact removal. Owner cleanup drops only managed dictionaries
and demand roots. Repeated registration on `SaveLoaded` can therefore operate
over already-mutated globals without a trustworthy original.

### Required ProductNative transaction

Use one owner-bound transaction ledger, not a second platform receipt family.
Before the first write it must capture:

- exact native recipe row identity and the original input-array reference plus
  a value copy;
- whether the tech information and graph node existed;
- for an existing node, its payload, costs, parent/child adjacency and order;
- for a newly inserted node, every map/list/graph/edge insertion needed for
  exact deletion;
- every renderer/preview transform original scale touched in that lifetime;
- every installed patch, callback, subscription, per-machine cache and owner
  root;
- prior accepted product configuration and activation state.

The activation sequence is:

1. validate official content, native types/methods and expected original
   shapes without mutation;
2. prepare all replacement objects and install plans;
3. apply native writes and owner resources while recording each completed
   step;
4. verify recipe, tech, fixed power, scheduler and renderer readback;
5. publish active state and persist configuration only after all checks pass.

Any exception, false return, missing readback or injected test failure restores
completed steps in strict reverse order. Restoration must be idempotent and
must compare current ownership/value before writing so Mine cannot overwrite a
third party's later change. An ownership conflict fails closed and leaves a
visible restart-required diagnostic; it does not silently “restore” foreign
state.

Disable, Loader owner cleanup, entry failure, returned-to-title and process
shutdown use the same restoration ledger. SaveLoaded begins from a clean
per-save session and must not recapture an already Mine-mutated value as the
original. A failed restore keeps an explicit dirty owner root until retry or
restart; dictionary deletion alone is not cleanup proof.

### Acceptance gate

Inject a failure after every individual mutation/resource acquisition. Every
case must prove:

- the exact original recipe array and tech map/list/graph topology;
- original renderer scales;
- zero product patches, callbacks, caches and roots;
- prior accepted config value;
- no output, power or inventory mutation from a cycle that did not commit.

Also test a foreign post-activation write: Mine must report an ownership
conflict and must not overwrite it during cleanup.

## 5. Native Save Mode And Physical Owner

### State classification

| State | Authoritative owner | Persistence classification |
| --- | --- | --- |
| Item/equipment/recipe/group definitions | official JSON / native config tables | ContentOwner; static package content |
| Placed Mine identity, case inventory and appliance power buffer | native room/equipment/archive JSON | save-bound native gameplay state |
| Per-machine next-due anchor and cycle bookkeeping | Mine ProductNative | first 1.0.0 uses volatile session-derived state; it is not a durable economy promise |
| Cycle time, output weights, Oil recipe policy and runtime-enabled setting | Mine ProductNative config | configuration, not per-save gameplay commit |
| Tech/recipe override and renderer/cache lifetime | Mine ProductNative session ledger | reversible process/session mutation; never a durable gameplay commit by itself |
| Logs, diagnostics and admission/package receipts | Platform/diagnostic owners | non-gameplay state |

The current `MachineRuntimeEntry.NextDueTotalTus`, process `Random` and
production count are memory-only, but they are not reset on `SaveLoaded` or
`ReturnedToTitle`. The defect is cross-slot/session leakage, not the absence of
a sidecar.

The first 1.0.0 decision is a **session-derived scheduler**:

- every successful `SaveLoaded` clears all prior scheduler entries and starts
  each newly observed Mine at `current native TotalTUs + configured cycle`;
- returned-to-title, product disable, Loader owner cleanup and shutdown clear
  the scheduler, RNG/session counters and machine-identity cache;
- no Mine sidecar, candidate generation, journal, archive fingerprint or
  interrupted-promotion repair is created;
- native case inventory and appliance power are the only durable production
  results, and remain owned by the official archive/save system;
- loading or reloading a save intentionally starts a fresh Mine cycle. The
  first release does not promise anti-reload-reroll protection or exact
  cross-session cadence.

An exact save-bound scheduler remains a later, separately reviewed product
choice. It may be selected only if the product deliberately promises
anti-reload refresh protection or precise cross-save cycle continuity; that
choice would then require the existing Working/Committed and interruption
semantics. It is not inferred from native ownership and is not part of this
admission.

### Test save modes

- UI, activation, recipe/tech restoration, renderer lifecycle, owner cleanup,
  disabled/re-enabled behavior, session reset and no-save rollback use
  `NoNativeSave` on the authoritative third slot. Archive current/prev/bak
  length/hash/mtime are compared before any cleanup or external restoration;
  there is no Mine committed sidecar to inspect.
- one ordinary native-save/cold-reload check may use an AutoCloud-isolated
  disposable fixture with `NativeSaveExpected` to prove only that native
  inventory and appliance power persist while the scheduler restarts from the
  loaded native clock.
- there is no Mine candidate-generation, save-failure promotion,
  interrupted-notification or cold sidecar-repair matrix in the first release.
- InstantSave or a forged callback is diagnostic evidence only and cannot
  replace the selected normal-save and no-save behavior checks.

## Focused Acceptance Matrix

| Gate | Setup/action | Required proof | Save mode |
| --- | --- | --- | --- |
| Cold disabled | stale config plus `Runtime production enabled=false` | static content present; zero Mine ProductNative roots/mutations/cycles | `NoNativeSave` |
| Atomic enable | disabled to enabled, then one charged cycle | one owner; fixed native 10; exactly one weighted output placed in Mine storage | `NoNativeSave` |
| Low power | native buffer below 10 at due time | no output; buffer/inventory unchanged; due policy remains explicit | `NoNativeSave` |
| Hot disable/re-enable | disable while loaded, inspect, re-enable | exact recipe/tech/renderer restoration and zero roots before one clean reactivation | `NoNativeSave` |
| Failure ladder | source/Unit fault injection after every apply and restore step | reverse rollback, prior config/active state, exact originals or explicit dirty/restart failure | source/unit |
| Runtime-mineral migration | old configs with `true` and `false` | identical fixed output set; one warning maximum; no active switch | source/unit |
| Recipe policy | Oil absent/present; fallback/Oil selection | exact intended recipe while active; exact original after disable/failure/title | `NoNativeSave` |
| Machine identity | two Mines, move/room transition, dismantle/index reuse | independent session schedules; no stale entry transfer, duplicate cycle or leaked renderer state | `NoNativeSave` |
| Session reset | observe due anchors, return to title/reload the slot | all old scheduler/RNG/cache state is zero; new due anchors equal loaded native time plus one cycle | `NoNativeSave` |
| No-save rollback | produce/consume power, then return to title without native save and reload | native inventory and power return to their prior official commit; scheduler starts one fresh cycle; no duplicated/lost output | `NoNativeSave` |
| Normal native commit | produce once, complete ordinary native save, cold reload | new native inventory/power persist; scheduler starts from loaded native time rather than restoring an old due anchor | `NativeSaveExpected`, disposable fixture |
| Owner cleanup | Loader deactivate while active, then title/re-entry | exact owned patch/callback/cache/root zero; static content and native inventory intact | `NoNativeSave` |
| Package boundary | SDK-generated candidate, Doctor/Manager/Catalog checks | exact Mine identity/policy/receipt; no hand-authored Advanced value; no mandatory Runtime expansion | source/package |

The failure ladder is a source/Unit transaction test, not one giant game-test
atom. The smallest runtime acceptance exercises cold/hot enablement, one
charged and one low-power cycle, restoration, session reset, no-save rollback,
and the single native-save case above. It does not authorize a complete
Release, L0–L5, GC gradient or long test. A separate admission Review must
select Mine explicitly before implementation can create an Advanced package
or change Catalog status.

## Implementation Boundary

The correction may later reuse existing Platform Loader, Advanced Harmony
supervision, ConfigMenu, Catalog, SDK/package, Doctor/Manager and generic
save-event infrastructure. Reuse does not move Mine scheduling, RNG, recipe
policy, tech mutation, visual handling or save state into Platform or
GameBridge.

The old `IMachineProductionApi` ABI remains an Experimental compatibility
question outside this prerequisite. Mine must not consume it after extraction,
and the mandatory implementation must retain no hidden Mine execution path.
Removing, freezing or relocating the public compatibility surface requires its
own consumer/ABI review.

## Required Safety Clause

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

This Review found the relevant owners for the bounded plan, but runtime
evidence and independent admission still remain outstanding.

## Disposition

**GO** to implement the bounded correction only after a separate Review admits
Mine as the next exact product.

**NO-GO** to admit the current prototype, retain configurable power, describe
runtime-mineral discovery, treat dictionary cleanup as native restoration, or
add Mine scheduler persistence without a later explicit economy decision.

**NO-GO** to add a generic machine Host/API, a Mine Content Host, a new
SharedNative adapter or any Mine executor to the five default-loaded Runtime
assemblies.
