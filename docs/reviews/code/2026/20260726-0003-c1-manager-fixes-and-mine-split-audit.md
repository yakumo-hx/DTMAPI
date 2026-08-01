# C1/Manager Fixes And Mine Split Audit

**Review ID:** `20260726-0003`

**Date:** 2026-07-26

**Status:** accepted and closed — all three Mine P1 findings and both P2
dispositions were corrected and revalidated

**Reviewed range:** `e7800e14..6af0adbc`

**Scope:** independent review of `96d4b0e0` (`close catalog C1 and manager
audit debt`) and `6af0adbc` (`admit Mine as eleventh Advanced product`). This
Review does not change runtime source, rerun the game, run a complete Release,
L0-L5, GC or long-test ladder, admit a twelfth product, authorize Content Host
G7, or publish 0.5.5.

## Verdict

The preceding C1 and Manager findings are closed correctly:

- the product Catalog is green again and the remaining active references to
  deleted `testmods` roots are either explicit retired-root assertions,
  synthetic-fixture authority, or frozen historical baselines;
- the Manager's localized player interactions have current third-save
  `NoNativeSave` evidence at `GAME-SMOKE/20260726-183020`, including page
  navigation, partial last page, warning/error details and feature navigation.

Mine also makes a real architecture correction. The mandatory
GameBridge executor, provider and demand route are gone. The frozen
`IMachineProductionApi` declaration remains only as an obsolete, no-provider
ABI facade, and the implementation is physically owned by
`products/first-party/Mine`. Catalog, SDK package, synthetic retained ABI,
focused Mine Unit and current-DLL owner cleanup evidence pass. This is a real
reduction of default-loaded mandatory Runtime, not merely a rename inside the
same mandatory assembly.

The current `verified/closed` admission conclusion is nevertheless too broad.
Mine has two unresolved P1 implementation defects and its final game run
proves only one charged production path plus cleanup. The admitted boundary
may stay, but the product lifecycle state should return to
`implemented/open`. No twelfth product should start until the two P1 findings
and the smallest missing Mine acceptance matrix are closed.

No P0 was found. Three P1 findings and two P2 findings remain.

## Findings

### P1 — removed, moved or index-reused Mines retain stale scheduler entries

`UpdateMachineProduction` adds a `MachineRuntimeEntry` for every observed Mine
but never removes entries that are no longer observed. The dictionary is
cleared only at a whole-session boundary:

- `MineNativeRuntime.Engine.cs:736-768` enumerates current equipment and adds
  missing keys;
- `MineNativeRuntime.Engine.cs:850-854` can continue selecting any retained
  owner/machine entry;
- `MineNativeRuntime.Engine.cs:1331-1338` derives the key from
  `owner|machine|equipment|room|index`;
- `MineNativeRuntime.cs:485-490` is the only current
  `machineRuntimeEntries.Clear()` path.

Consequences within one save session:

- dismantling a Mine leaves its scheduler/RNG/output state rooted;
- moving a Mine to another room creates a second key and leaves the first;
- a later equipment object that reuses the same room/index can inherit the old
  due time and entry state;
- repeated place/move/dismantle activity can grow the dictionary for the
  remainder of the session.

This directly misses the prerequisite's multi-Mine, room transition,
dismantle and index-reuse invariant. Existing Unit coverage checks session
clear and isolated snapshot restoration, but does not exercise a live
enumeration/prune/reuse sequence.

Required correction:

1. maintain a per-poll set of observed stable equipment identities;
2. remove or retire entries absent from the completed authoritative
   enumeration;
3. do not use room/index alone as object identity when those values can be
   reused;
4. add focused deterministic cases for two Mines, move, dismantle, index reuse
   and re-place without due-state transfer.

### P1 — capacity and resource checks are rollback-after-mutation, not the promised preflight

The accepted admission contract says full/rejected storage must conserve
power/items and describes capacity as preflighted. The current cycle instead
performs these operations:

1. select a weighted output and allocate a native item only to discard it
   (`MineNativeRuntime.Engine.cs:1092-1111`);
2. copy the whole native inventory and capture power
   (`MineMutationTransaction.cs:222-264`);
3. invoke the native electric component's `Launch()`
   (`MineNativeRuntime.Engine.cs:1113-1122`, `1153-1183`);
4. only then read storage capacity/empty slots
   (`MineNativeRuntime.Engine.cs:1124-1129`, `1194-1211`);
5. on failure, overwrite the inventory and power from the snapshot.

The rollback is useful as a final transaction guard, but it does not replace
preflight. On a full Mine, the code calls native `Launch()` before discovering
the capacity failure. On low power, it has already selected output, generated
an item and copied the entire inventory. Because the overdue due time is
retained, either failure is retried on every 0.5-second poll.

The focused fixture proves that the two copied fields can be restored in its
model. It does not prove that every side effect of real native `Launch()` is
captured, nor does it prove that repeated full/low-power retries are allocation
safe. Correct the ordering so storage capacity and output admissibility are
checked before native power mutation; retain the snapshot only as the final
atomic rollback guard. Add counters/fakes proving:

- full/rejected storage does not invoke `Launch()`;
- low power does not clone or overwrite inventory;
- one failed due cycle does not construct throwaway output objects every
  0.5 seconds;
- failed and successful cycles conserve exactly the expected power and items.

### P1 — final runtime evidence does not cover the Review's own acceptance matrix

Review `20260726-0002` requires cold-disabled activation, injected activation
failure, disable/re-enable, low power, full/rejected storage, session reset,
multi-Mine move/dismantle/index reuse and visual containment. Its final
paragraph then says `GAME-SMOKE/20260726-205813` “passes the bounded matrix”.

That run proves a narrower and useful set:

- current SDK-generated Mine loads as the exact Advanced owner;
- official JSON/tech UI and exact `3/3` Harmony owner are present;
- one charged transient Mine produces `coal x1` into its native `16/4`
  inventory through parameterless `Launch()`;
- title and Loader cleanup reach zero;
- the third-slot `NoNativeSave` archives/sidecars remain unchanged and the
  process exits cleanly.

It does not prove low power, full/rejected storage, two Mines, move,
dismantle/index reuse, cold-disabled startup, hot disable/re-enable or injected
activation rollback in the real runtime. Earlier attempts are explicitly
classified as non-acceptance. Unit coverage is mostly source-shape assertions,
Harmony ownership and isolated mutation snapshots, so it cannot substitute
for the missing native behavior.

The evidence itself should remain accepted for what it proves. The overclaim
should be corrected by reopening the Mine Update/admission state, not by
discarding the run or starting a complete Release. After the two source
findings are fixed, run one bounded Mine acceptance that combines the smallest
missing native cases. A native save is not mandatory for the session-derived
scheduler; use `NativeSaveExpected` only if official inventory/power durability
is intentionally tested in an isolated disposable fixture.

### P2 — the ProductNative engine remains much heavier and more generic than Mine needs

Mine contains 11 C# files, 3,793 physical / 3,507 non-empty source lines and a
101,888-byte DLL. Its 1,447-line engine is an 87% rename of the old
GameBridge MachineProduction implementation. It still carries generalized
multi-owner/multi-machine registration, fuel mode, probability overrides,
recipe inputs and broad state/diagnostic models even though the admitted
product has one fixed Mine identity, fixed power cost and a fixed official
JSON route.

The 0.5-second loop also repeatedly allocates arrays and collections, rescans
types/equipment, calls `EnsureNativeMachineTechRoute`, reapplies visual-scale
inspection and builds status strings
(`MineNativeRuntime.Engine.cs:715-865`). Weighted output selection creates a
new list and LINQ sum for each attempted cycle
(`MineNativeRuntime.Engine.cs:1271-1301`), with catch-up permitting up to 96
cycles in one poll.

This is not proof that Mine causes the known long-session Unity/Mono GC issue,
but it is contrary to the lightweight-product direction and creates avoidable
main-thread allocation pressure. After P1 correctness is restored, specialize
the engine to the one admitted product, cache native lookups/tech setup at
activation, reuse scan buffers where safe, and keep the periodic path
allocation-bounded. Do not move this generic engine back into GameBridge or
promote it as a reusable public Machine API.

### P2 — player text and temporary well art still describe the retired design

`products/first-party/Mine/Content/item_tbitem.json` still calls Mine an
“experimental” device whose “Machine API” owns production. There is no
runtime Machine API provider after this split. The item should describe player
behavior, not an obsolete implementation route.

Mine also still uses `sprite_equipment_well`, an `8x6` footprint and three
runtime 2x visual Hooks. This is allowed for the local Developer/RebuildBlocked
prototype, but the already recorded product decision requires dedicated Mine
art and removal of runtime 2x scaling before formal publication. Therefore
“eleventh product admitted” must not be read as “release-ready”.

## Weight And Boundary Result

The current measurements support only the following narrow claims:

| Boundary | Tenth-product baseline | Current eleven-product tree | Delta |
|---|---:|---:|---:|
| Mandatory Runtime physical lines | 65,534 | 64,738 | -796 |
| Mandatory Runtime non-empty lines | 58,710 | 58,034 | -676 |
| Mandatory Runtime DLL bytes | 2,111,488 | 2,089,984 | -21,504 |
| Mandatory GameBridge physical lines | 26,658 | 25,050 | -1,608 |
| Mandatory GameBridge DLL bytes | 798,208 | 743,936 | -54,272 |
| ProductNative physical lines | 23,347 | 27,139 | +3,792 |
| Mandatory plus products physical lines | 88,881 | 91,877 | +2,996 |
| Mandatory plus products DLL bytes | 2,642,944 | 2,723,328 | +80,384 |

The preceding Manager correction is also present between the two snapshots,
so the table is not a pure per-commit Mine delta. It still proves the important
ownership fact: the default-loaded mandatory Runtime and GameBridge are
smaller, and no MachineProduction executor remains there. It equally proves
that repository/all-product weight increased. A player who enables Mine loads
a large ProductNative assembly and its half-second scheduler; no
all-enabled-process or GC reduction may be claimed yet.

## Validation

No game, complete Release, L0-L5, GC or long test was run by this audit.

Passed:

- Release build of `DTMAPI.UnitTests`, including Mine native/Harmony fixtures:
  zero warnings and zero errors;
- focused Unit `mine-product`;
- focused Unit `manager-ui`;
- `tools/scripts/check-product-catalog.ps1`: OK, 27 products, 11 public,
  21 Workshop items and 48 API rows;
- Author SDK Mine package build;
- synthetic retained ABI, including the frozen obsolete Machine types;
- source scan: no mandatory GameBridge MachineProduction provider, executor or
  demand route remains.

Not independently completed:

- the broad Unit entry exceeded the audit's 60-second bounded observation
  without output and was stopped;
- `test-batch4-qa-semantic-inventory.ps1` likewise did not complete within two
  bounded attempts.

These two timeouts are not recorded as product regressions because their
focused children and Catalog projection pass. They are runner
cost/observability debt and do not fill any Mine acceptance gap.

Existing evidence inspected:

- `GAME-SMOKE/20260726-183020` for Manager player interactions;
- `GAME-SMOKE/20260726-205813` for the exact Mine behavior listed above.

## Required Closeout And Route

1. Keep `96d4b0e0`; C1 and Manager need no new game run.
2. Keep the Mine physical ownership switch, but return Update
   `20260726-0004`, admission Review `20260726-0002` and the Batch 6 contract
   from `verified/closed` to `implemented/open`.
3. Fix scheduler pruning/stable identity and preflight ordering. Add focused
   deterministic tests for low/full storage, two Mines, move/dismantle/index
   reuse and bounded failed-due work.
4. Run one smallest combined Mine game acceptance after the final source
   candidate is frozen. Do not run a complete Release, ladder, GC gradient or
   long test for this closeout.
5. Correct the player description now. Keep dedicated Mine art and removal of
   runtime 2x scaling as an explicit pre-publication blocker.
6. After these gates pass, freeze Mine again and then resume the roadmap at
   the DebugConsole prerequisite or another separately admitted product.

## Corrective Resolution

The owning implementation completed this route without widening the admitted
boundary:

- scheduler state now uses live object identity and authoritative
  enumeration/prune; focused tests and final native QA cover two Mines,
  movement, dismantle and reused indices without state transfer;
- full, rejected and low-power states are checked before output construction,
  inventory copy or native `Launch()`, while the rollback snapshot remains for
  the only mutation window after successful Launch;
- stable blocked-state fingerprints prevent repeated throwaway allocation on
  the half-second poll;
- the copied general engine was replaced by fixed Mine definitions,
  `MineSessionScheduler` and a Mine-only production path. The largest engine
  file falls from 1,447 to 647 lines and the built DLL from 101,888 to 86,016
  bytes;
- player text describes the Mine behavior instead of a retired Machine API;
  README/package status now names the well sprite and 2x Hooks as unpublished
  `RebuildBlocked` prototype debt.

Focused Unit `mine-product`, the QA Host build and SDK package checks pass.
`GAME-SMOKE/20260726-223105` proves cold disable (`active=False`,
`hooks=0/3`, scheduler zero). After byte-exact config restoration,
`GAME-SMOKE/20260726-223236` proves the combined real native matrix and final
owner cleanup:
`actual3+callback1+sessionDerived -> actual0+callback0 ->
instance0+actual0+callback0+roots0`. The run preserves the protected third
save under `NoNativeSave`, restores profile/source/config/QA state and exits
without a fatal window or remaining process.

The earlier `205813` remains narrow historical evidence. The final two runs,
not that earlier run, own the corrective acceptance. No complete Release,
L0-L5, GC or long test was run or required.
