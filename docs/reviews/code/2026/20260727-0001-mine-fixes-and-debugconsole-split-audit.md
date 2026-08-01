# Mine Fixes And DebugConsole Split Audit

**Review ID:** `20260727-0001`

**Date:** 2026-07-27

**Status:** recorded — Mine corrections are materially valid but one cleanup
failure path remains open; DebugConsole extraction is real but independent
admission fails

**Reviewed range:** `6af0adbc..2f11115e`

**Reviewed commits:**

- `154b604a` — `fix: close Mine scheduler and preflight debt`
- `2f11115e` — `feat: admit DebugConsole as twelfth Advanced product`

**Scope:** independent source, current evidence, ownership, ABI, lifecycle,
save-mode and focused-check audit. This Review does not change runtime source,
existing lifecycle authorities or release state; launch Doloc Town; run a
complete Release, L0-L5, GC or long test; admit a thirteenth product; authorize
G7; or publish 0.5.5.

## Verdict

No P0 was found.

The two originally reopened Mine defects are genuinely corrected:

- the scheduler now follows object identity, prunes absent equipment only after
  an authoritative scan, and does not transfer due state across move,
  dismantle or index reuse;
- low power and full storage are checked before output construction, inventory
  copy and native `Launch()`;
- `GAME-SMOKE/20260726-223105` and `223236` provide the missing cold-disabled,
  two-Mine, low-power, full-storage, hot-toggle, move, dismantle, index-reuse,
  title and Loader evidence without modifying the selected save.

Mine nevertheless cannot remain unconditionally `verified/closed`: a newly
found failure path discards the only exact-native restoration ledger after a
restore exception. This needs one focused fault-injection correction; the
successful real-game matrix does not need to be replayed merely for that
source-only failure path.

The DebugConsole split also has a real positive result. The Bootstrap Canvas
body moved into a managed Advanced product, the new product owns typed
Y/Escape and its exact 18-patch Harmony owner, and the five mandatory Runtime
projects lost 2,200 physical / 2,002 non-empty compiled source lines. This is a
real reduction for a player who does not load the product.

It is not yet a complete ownership extraction or an accepted twelfth product.
The frozen old action executors are still in mandatory GameBridge, the current
legacy Compatibility route has no current-candidate game proof, transient
leases do not preserve exact native originals, a returned-to-title run retains
the product UI graph, and `Save here` performs an unconfirmed native commit.
The Update must remain `implemented`; the thirteenth product should not start.

## 1. Mine Corrections

### Confirmed correction — scheduler identity and preflight ordering

`MineSessionScheduler` keys entries by stable object reference, marks observed
identities during the complete scan and removes unobserved identities only
after the scan. The focused Unit fixture and `223236` agree on the two-Mine,
move, dismantle and index-reuse behavior.

`MineNativeRuntime.Production` now resolves inventory, capacity, native power
and callable methods before output selection or transaction capture. The
low-power and full-storage branches do not construct an output, copy inventory
or call `Launch()`. A transaction snapshot remains as the final guard after
preflight, which is the correct role for rollback.

The narrower performance statement is: blocked cycles avoid output
construction, inventory copy and `Launch()`. It is not yet evidence of a
zero-allocation 0.5-second poll because the room/equipment collections,
preflight object, reflection enumeration, visual inspection and status strings
are still rebuilt.

### P1 — a failed native restoration is removed from the retry ledger

`products/first-party/Mine/src/Native/MineMutationTransaction.cs:95-129`
collects restoration exceptions but unconditionally clears
`nativeRestores` and `nativeRestoreOrder` before throwing. Therefore:

1. recipe/tech restoration may fail and correctly make the first cleanup fail;
2. the exact closure and its captured original are then lost;
3. a later title, Loader or shutdown cleanup has no restoration item to retry
   and may report success;
4. an in-process reactivation can capture the still-mutated value as its new
   “original”.

The current fixture at
`tests/DTMAPI.UnitTests/Fixtures/MineHarmonyOwnerFixture/Program.cs:110-129`
injects Hook-install failure before native mutation. Its restoration cases at
`:153-210` cover successful standalone primitives. They do not inject failure
after one or more recipe/tech mutations and then exercise
`ActivateRuntime -> reverse rollback -> cleanup retry`.

Required correction:

- remove successful restore entries as they complete but retain failed entries
  and their original order for a later cleanup attempt;
- prevent same-process activation while an exact restoration remains pending,
  or truthfully hold the product in `cleanup-failed/restart-required`;
- add one integrated fixture that mutates at least two native values, fails one
  reverse step, proves the unresolved original remains queued, then proves a
  later cleanup restores it exactly and reaches zero.

This is a focused source/Unit repair. It does not require another Mine game run
unless the correction changes normal activation, Hook ownership or production.

### P2 — Mine authorities still describe the pre-correction implementation

`docs/hook-map/focused/Mine.md:37-51` says `SaveLoaded` installs the Hook set and
that every production attempt snapshots power/inventory. Enabled Entry actually
installs the inert Hook set, and low-power/full-storage branches now exit before
snapshot creation. Its evidence list and
`docs/api/public-api-matrix.md:90` still use `205813` as the current proof
instead of corrected runs `223105/223236`.

The publish map still describes Mine as an experimental content-pack/probability
scheme, while the current official info correctly describes fixed
ProductNative production. Independent Mine artwork and removal of temporary 2x
well scaling remain an explicit `RebuildBlocked` release condition.

The two Mine Reviews also accumulated implementation completion narratives.
Future correction should leave only short resolution links there; the Mine
Update remains the sole lifecycle owner.

## 2. DebugConsole Split

### P1 — the old action executor did not move out of mandatory GameBridge

The prerequisite requires the old reflected UI **and old Diagnostic
executors** needed by the exact retained binary to move into the existing
Compatibility component, leaving only thin providers/proxies in mandatory
Runtime:

- `docs/reviews/api/2026/20260726-0002-debugconsole-admission-prerequisite-review.md:338-354`;
- the zero-leftover switch at `:359-374`.

The current tree moved the UI but not the executors:

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:232-257`
  unconditionally registers the seven Debug action providers;
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
  remains a 2,698-physical / 2,455-non-empty mandatory file containing
  inventory, weather, teleport, instant-save, time, movement and advanced
  action bodies, state and helpers, in addition to the separately classified
  mail API;
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs:113-165`
  still installs the old fifteen creative hooks on compatibility demand;
- `src/DTMAPI.GameBridge.DolocTown.Compatibility/DebugConsole/LegacyDebugActionAdapter.cs:17-62`
  contains no moved executor. It delegates every operation back to those
  mandatory APIs.

The new product has another 2,557 physical / 2,424 non-empty lines in its
private native-action files. Thus both action engines remain shipped and the
old one remains default-loaded.

The Catalog's existing live zero-leftover gate checks names such as
`DebugConsoleNativeActions`, `DebugConsoleUi` and
`LegacyDebugActionAdapter`, but does not detect the generically named
`DolocTownExperimentalBridgeApi.Diagnostics` action bodies. This is why
Catalog and Unit can pass while the planned E2/E3 physical boundary is still
false.

Required correction:

- retain the public ABI declarations and thin mandatory provider identity;
- move the seven old action implementations and their compatibility-only
  creative Hook owner into the existing optional Compatibility component;
- keep `IMailDeliveryApi` independently classified rather than moving it by
  association;
- extend the existing Catalog zero-leftover rule with the known old action
  fields/method bodies and creative-hook route. Do not add another receipt,
  host or checker family.

The current Update and Batch contract statements that the old action executor
already lives in Compatibility must be corrected until this is physically
true.

### P1 — title and transient-lease cleanup do not preserve their promised state

#### Returned-to-title UI graph

`DebugConsoleUi.ResetForTitleBoundary` at
`products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs:145-159` closes the
menu but does not release the owner graph. `Close` at `:231-259` hides the
Canvas, while full destruction exists only in `Shutdown` at `:161-165`.

The final implementation evidence proves the residue:

- before the product UI was opened, the first title transition reports zero;
- after opening it, `GAME-SMOKE/20260727-004836/DTMAPI-latest.log:698`
  reports `Canvas=1`, `Button=201`, `InputField=1`,
  `UnityEventListeners=201`, `DynamicBinders=201`, `rootAlive=1`;
- only later Loader deactivation reaches zero.

That contradicts the prerequisite's returned-to-title requirement at
`20260726-0002:411`.

#### Movement and time-scale originals

`DebugConsoleNativeActions.Helpers.cs:501-528` implements an apparent speed
multiplier by writing `SetMoveScaler(multiplier - 1)`, and `:543-568` restores
it to zero. It records only its own requested multiplier and current object,
not the native prior scaler.

The reviewed native owner shows this is shared mutable state:

- `MotionAbility.MoveScaler` is the current native modifier and
  `SetMoveScaler` overwrites it;
- `BuffManager.ComposeMoveScaler` and
  `BuffComponentBasic` add buffs through that same property.

Closing the console can therefore erase a pre-existing movement buff or a
later external change.

`DebugConsoleNativeActions.Advanced.cs:151-180` likewise stores only the
product's requested time multiplier. `ResetTimeScaleCore` at `:495-527` calls
native `RevertTimeScale`, whose reviewed native body writes the global time
scale to `1`, rather than restoring the exact prior value.

#### Failure reporting and creative state

`DebugConsoleNativeActions.Core.cs:58-63` discards the results of movement,
time and creative restoration. `ModEntry.Cleanup` can therefore report success
and unpatch the owner even if the global native value was not restored.

Creative enable at
`DebugConsoleNativeActions.Advanced.cs:530-550` writes three native flags
through a short-circuit `&&` chain. A middle write failure can leave a partial
native mutation. `RestoreCreative` at `:553-573` can return `false`, but the
caller ignores that result.

Required correction:

- title cleanup must release the complete product UI graph and recreate it
  lazily on a later save/open;
- each lease must capture the exact native original before its first mutation,
  define conflict behavior when another owner changes the same state, and
  restore or fail truthfully;
- creative enable must be all-or-nothing with immediate reverse rollback;
- aggregate restoration failures into owner cleanup and retain enough state
  for retry or a truthful restart-required condition;
- add fault-injection Unit cases and real non-default native-state cases. The
  existing `movement=1; timeScale=1; creative=False` smoke did not activate any
  lease and cannot prove restoration.

This remains ProductNative work. It does not justify a SharedNative API or
multi-owner dispatcher without a second real consumer/native owner.

### P1 — `Save here` is an unconfirmed permanent native commit

`DebugConsoleUi.cs:1103-1107` creates an ordinary single-click `Save here`
button. Its callback at `:1372-1380` immediately invokes the action, and
`DebugConsoleNativeActions.Core.cs:541-570` directly calls native
`DolocAPI.SaveGame`.

The prerequisite requires an explicit confirmation and one success/one failure
test on an AutoCloud-isolated disposable fixture. `004836` reports
`InstantSave=Skipped`; it is a `NoNativeSave` run and cannot accept this path.

Before admission, either:

1. add an unmistakable two-step confirmation and run the isolated
   `NativeSaveExpected` success/failure matrix; or
2. remove/disable this button from the 1.0 candidate and admit it later under a
   separate bounded save-action change.

The normal third save must not be used for this verification.

### P1 — exact old ABI/current Compatibility acceptance has not happened

The prerequisite Review explicitly authorizes only a later exact admission
Review at `:490-500`; it does not itself admit a twelfth product. No intervening
exact admission Review exists, while the implementation Update cites that
prerequisite as admission authority. This Review records the missing admission
decision as failed rather than creating a second policy or receipt system.

The current ABI gate is also incomplete:

- `tests/DTMAPI.AbiCompatibilityHarness/Program.cs:36-92` omits all eight
  DebugConsole/Diagnostic API types;
- `KnownCompatibilityConsumers` at `:93-232` contains nine retained products
  and no exact 0.3.1 Y-console consumer/MemberRef set;
- all eight declarations in
  `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:66-136,254-273`
  lack `DtmApiDisposition`, so the canonical rule treats them as `Open`,
  contrary to the public matrix's `Diagnostic` classification.

`GAME-SMOKE/20260715-153336` proves the exact old Workshop DLL against the
pre-split provider. It predates the new lazy Compatibility implementation.
`GAME-SMOKE/20260727-004836` loads only the new Advanced product, leaves the
Compatibility Host dormant and reports every native Debug action
(`Inventory`, `Weather`, `Teleport`, `Time`, `Movement`, `Advanced`,
`InstantSave`) as `Skipped`.

The installed Host in `004836` has SHA-256
`674126E2...F04D48A`; the current focused HEAD build has a different hash.
Regardless of the hash difference, a dormant Host cannot prove old bind,
MemberRef, UI, action or cleanup behavior.

Required correction and acceptance:

- extend the existing retained ABI harness with the exact 0.3.1 DLL hash,
  provider identities and complete consumed MemberRefs; do not add a new
  receipt family;
- explicitly classify the eight declarations with the canonical Diagnostic
  disposition while preserving signatures and warning compatibility;
- after the physical executor move, run one bounded third-slot
  `NoNativeSave` case using the exact old DLL and current Host: lazy load,
  provider/API bind, legacy Y/Escape/focus, representative read/write actions,
  one frozen warning, title and owner cleanup, then resident-dormant zero;
- separately prove missing/mismatched Host bytes fail closed with current
  Doctor/Manager repair text.

### P2 — the enabled product is not lightweight and the frozen UI is not frozen

`ModEntry.cs:67-68,163-173` keeps `UpdateTicked` subscribed for the product's
entire loaded lifetime. While in a save, every frame calls both action and UI
updates. `DebugConsoleUi.Update` at `:446-529` still calls visibility handling
when closed; `EnsureInitialized` at `:637-659` can create a hidden Canvas on
the first save frame before the player opens the console. The final smoke
ledger recorded more than four thousand DebugConsole event-handler calls.

This is product-only overhead, not default Runtime overhead, and is not proof
of the long-session Unity GC defect. Before the later UI rewrite/release,
subscribe only while a modal/drain/lease needs frame work, create the graph on
first open, and measure the closed and open steady states.

The Compatibility project also directly compiles five source files from the
new product UI:

`src/DTMAPI.GameBridge.DolocTown.Compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.csproj:9-13`.

A future 1.0 UI rewrite would silently change the supposedly frozen 0.3.1
Compatibility implementation. Freeze a Compatibility-owned implementation or
a deliberately versioned stable internal UI boundary before beginning that
rewrite.

## Size And Runtime-Burden Result

The comparison is `154b604a -> 2f11115e`, using compiled `.cs` items rather
than every file beneath a project directory:

| Surface | Before | Current | Delta |
| --- | ---: | ---: | ---: |
| Five mandatory Runtime projects, physical | 64,738 | 62,538 | -2,200 |
| Five mandatory Runtime projects, non-empty | 58,034 | 56,032 | -2,002 |
| Bootstrap, physical | 6,903 | 4,592 | -2,311 |
| Mandatory GameBridge, physical | 25,050 | 25,072 | +22 |
| DebugConsole product source, physical | 148 | 5,746 | +5,598 |
| Mandatory plus DebugConsole product, physical | 64,886 | 68,284 | +3,398 |

The current five mandatory DLLs total 2,022,400 bytes, down 67,584 from the
eleven-product baseline. The current DebugConsole DLL is 163,328 bytes.

Therefore the valid claim is:

> When DebugConsole is absent or not loaded, less source and fewer bytes are
> default-loaded because the old Bootstrap UI moved out.

The invalid claims are that the repository, download, total shipped package or
DebugConsole-enabled process is smaller. Mandatory GameBridge itself did not
shrink, and the old/new action engines coexist.

## Focused Validation Performed

- repository-local .NET 8 build of `DTMAPI.UnitTests`: passed with 0 errors and
  9 nullable warnings from the linked DebugConsole UI source;
- full `DTMAPI.UnitTests`: passed;
- `DTMAPI_UNIT_TEST_FOCUS=compatibility-host`: passed;
- `DTMAPI_UNIT_TEST_FOCUS=mine-product`: passed;
- Product Catalog check: passed
  (`27 products / 11 public / 21 Workshop items / 48 API rows`);
- synthetic retained ABI: passed, but its current contract does not contain
  DebugConsole and therefore is not acceptance evidence for this finding;
- `git diff --check`: passed.

Document governance currently fails three existing checks:

- `docs/debug/regressions/smoke-matrix.md` exceeds the active-router size cap;
- the DebugConsole Update uses an invalid compound `Related Issue State`;
- its monthly index row uses another invalid state spelling.

No game was launched. No complete Release, L0-L5, GC or long test was run.

## Smallest Safe Route

1. Reopen Mine to `implemented/open`; retain failed native restores and add one
   integrated retry/failure Unit. Do not replay the successful Mine game
   matrix unless normal behavior changes.
2. Keep DebugConsole `implemented/RebuildBlocked`. Move the old action
   executors into the existing Compatibility component and close the exact ABI
   gate.
3. Correct title graph release, exact movement/time/creative lease semantics,
   restoration failure propagation and `Save here` confirmation/decision.
4. Run focused source/Unit gates, then one new-product third-slot
   `NoNativeSave` matrix and one exact-old-DLL/current-Host third-slot
   `NoNativeSave` matrix.
5. Test `Save here` separately on an AutoCloud-isolated disposable
   `NativeSaveExpected` fixture, only if it remains in 1.0.
6. Run one independent acceptance review over the frozen final candidate.
   Do not run a complete Release, L0-L5, GC or long test for this closure.
7. Only after that acceptance may the thirteenth product or the separate Y
   console UI rewrite begin.

## 2026-07-27 Resolution Status

This Review's rejection remains the historical independent finding and is not
rewritten as a pass. Mine's failed-restore retention/retry correction is commit
`11f18098` with focused Unit coverage. DebugConsole's physical Host move,
title graph release, exact leases, cleanup failure propagation and confirmed
Save here corrections are owned by Update
[`20260726-0005`](../../../updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md).
That Update records current-product `083919`, exact old 0.3.1/current-Host
`085929` and isolated `NativeSaveExpected` `090702` self-acceptance evidence.
Both products remain `implemented/open` until a fresh independent acceptance
review passes.
