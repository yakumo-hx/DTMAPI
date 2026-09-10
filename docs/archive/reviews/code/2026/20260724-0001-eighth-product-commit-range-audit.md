# Eighth-Product Commit-Range Audit

**Review ID:** `20260724-0001`

**Date:** 2026-07-24

**Status:** `recorded/resolved — retain implementation; all audit findings closed by focused correction, independent rereview and final integration`

## Scope

This independent audit reviews the admitted and implemented
MoreEquipmentSlots range:

```text
858bd607 docs: admit MoreEquipmentSlots as eighth product
71304214 feat: split MoreEquipmentSlots eighth product
5eda96b6 chore: refresh eighth-product evidence allowlist
bfaf4d69 test: align advanced gates with catalog authority
46e2b996 fix: close eighth-product release tail drift
c58e7dcd fix: preserve frozen ActionSpeed status parameter
7d137ac0 fix: make Phase 0 git reads encoding-stable
732c08c3 fix: tolerate heterogeneous catalog rows in release suite
d38af17a test: close eighth-product integration gates
```

The unrelated untracked portable reverse-capture Update, tool directory and
builder were excluded. This is an audit-only Review: it creates no Update,
receipt, schema, Host, gate or assurance family. Update
[`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
remains the implementation lifecycle owner.

## Verdict

Keep the eighth-product split. It is a real ProductNative extraction:
mandatory GameBridge no longer compiles the EquipmentSlots executor, sidecar
and UI policy, or the four Harmony callback bodies; the frozen ABI executor is
dormant in the existing Compatibility Host; the product owns exactly four
Harmony patches; its player-facing contract is fixed at three additional
slots; and the product adds no SharedNative capability or new public API.

At audit time, the reviewed range had to remain **implemented/open**. The enabled
game behavior is credible, the final complete Release and current
PlayerDoctor/policy evidence pass, and the package/SDK/Catalog route is
coherent. Those successes do not close four correctness findings and one
physical-proof gap:

1. a real product replacement can lose the incoming item when outgoing native
   placement fails;
2. the disabled/uninstalled cold-recovery path cannot read the current product
   sidecar/journal schema, while the accepted cold smoke used a legacy ABI
   fixture instead;
3. the product shield attack tail omits official native behavior and supplies
   the wrong hurt reason;
4. cleanup removes product functions without recomputing native aggregated
   equipment state;
5. the advertised dual-order and product failure-rollback matrix does not
   physically exercise the real ProductNative installer in the
   compatibility-first and partial-install cases.

These are bounded corrections. They do not justify rollback, a second Host,
a new public API, another receipt family or a new assurance ladder. The ninth
product must not be implemented while this eighth-product baseline is still
open.

## Resolution

The verdict and findings below describe the audited pre-correction range.
Commits `b0ef85a9` and `034ea5e6` closed the five findings without changing the
admitted identity or adding a Host, API, receipt or assurance family:

- replacement and same-item transactions now retain grouped durable evidence
  and preserve exactly one logical copy across native, committed sidecar and
  journal;
- the existing Host validates and reconciles the exact ProductNative-v3
  document/journal, including prepared and committed cold states;
- the shield path preserves the faint guard, official shield priority, exact
  damage tail, fishing cleanup and hit-state transition;
- function cleanup recomputes native aggregate equipment state while
  exact-owner unpatch remains exception-safe;
- physical fixtures invoke the real product installer in both owner orders,
  partial rollback, residue and true owner-deactivation paths.

The final package is SHA-256
`40370DAF406E4C6A45B6A86DD0AAA38B8E3B8BAFDB08817A7BB41F8363E8326A`.
`GAME-SMOKE/20260724-053248` passes the protected enabled transaction and real
Loader cleanup; `GAME-SMOKE/20260724-053342` passes exact ProductNative-v3
cold recovery. Both runs pass current Player Doctor, save/profile/source
restoration and process exit.

One final-candidate from-start Release run reached the evidence-retention
allowlist gate after passing build, complete Unit/QA, Doctor, Catalog and
package gates. The existing allowlist was repaired and its focused tests
passed, then the exact previously unreached Release tail passed in original
order. Per explicit user instruction, no second full from-start Release was
run. An independent rereview reported no remaining P0/P1/P2. Update
`20260723-0008` is therefore `verified/closed`; this Review remains the single
historical audit authority.

## Confirmed Results

### Physical and contract boundary

- `DTMAPI.MoreEquipmentSlotsMod` is one SDK-generated `netstandard2.0`
  Advanced ProductNative product at version `1.0.0`, minimum Runtime `0.5.5`.
- Its exact owner is `dtmapi.mod.dtmapi.moreequipmentslotsmod`, and its
  installer targets four product-owned Harmony callbacks as one set.
- Mandatory `DTMAPI.GameBridge.DolocTown` excludes the heavy EquipmentSlots
  executor and hook bodies. The frozen executor is linked only into the
  already existing dormant-shipped Compatibility Host.
- Product behavior is fixed at three additional slots. The frozen
  `IEquipmentSlotsApi` retains the historical `0..24` range independently.
- `IEquipmentSlotsApi` and all six public DTO types are marked
  Experimental/Frozen/Obsolete without deleting them. The ABI harness locks
  their retained type and MemberRef surface against the frozen baseline.
- No SharedNative capability, second Host, public DTO expansion or new content
  ownership was introduced.

### Doctor, build, package and enabled runtime

- `temp/eighth-final-release-20260724-022948.out.log` records one complete
  Release PASS at candidate `d38af17a`, including the exact eight-product
  Catalog, InstallDoctor, PlayerDoctor, Manager/SDK/package, install/uninstall
  and zero-leftover checks.
- The current PlayerDoctor recognizes the Chest policy and the complete
  Release includes
  `DoctorEmbeddedAdvancedPolicySetExactlyMatchesCatalog` PASS. The earlier
  seventh-product `findings / Exit=2` Doctor receipt is not current
  eighth-product evidence.
- MoreEquipmentSlots rebuilds through the existing generic Advanced SDK and
  Catalog route. The recorded deterministic package SHA-256 is
  `AE9D3D181C...ED4853`.
- [`GAME-SMOKE/20260724-025433`](../../../../debug/evidence/GAME-SMOKE/20260724-025433/result.json)
  credibly proves startup, HookProbe, third-save load, four product hooks,
  fixed-three-slot behavior, one protected transaction, real Loader owner
  deactivation, title recovery, player/save/local-author restoration and a
  clean process exit.
- That enabled smoke ends its protected transaction with
  `native=1, committedSidecar=0, journal=0, logicalItems=1`.

These results support retaining the implementation. They do not cover the
failure branches described below.

## Findings At Audit Time

### P1 — Failed replacement can discard the incoming item

`EquipmentSlotTransactionCoordinator.PrepareReplacement` moves the outgoing
slot item into escrow and records the incoming item as a replacement. During
save, `MoreEquipmentSlotsNativeRuntime.OnSaveSaving` withdraws the incoming
native item first and only then tries to place the outgoing escrow item.

If the incoming withdrawal succeeds but outgoing placement returns the real
`Failure` state:

- `EquipmentSlotTransactionJournal.ApplyReplacements` skips the incoming
  replacement because its paired outgoing escrow did not succeed;
- `RestoreFailedEscrow` restores the old item to the sidecar;
- finalization clears the journal;
- the already withdrawn incoming item is not returned to native storage,
  retained in committed sidecar storage or retained in the journal.

The result is one lost logical item. Existing
`ReplacementCommitsIncomingAndOutgoingTogether` coverage exercises only the
all-success branch, so the enabled smoke cannot exclude this failure.

Minimum correction:

1. make an outgoing placement failure roll back the earlier incoming
   withdrawal, or persist both sides in a retryable journal state;
2. prove incoming success plus outgoing failure, incoming failure, and full
   success;
3. at every interruption/final state assert that native storage, committed
   sidecar and durable journal together contain exactly one copy of each
   logical item.

Use the existing transaction/journal test authority. Do not add a new receipt
or transaction framework.

### P1 — Current ProductNative cold storage is unreadable by the dormant Host

The product writes
`equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json` with the current product
schema: nested `scope`, `generation`, fixed `slots`, and a journal containing
escrow and replacement collections. Mandatory Runtime can detect that an
EquipmentSlots file exists and demand the existing Compatibility Host.

The Host orphan scanner then deserializes only the older private ABI sidecar:
it expects top-level `ownerId`, `archiveIndex`, `storageScope`, flat slots and
the old single-item journal. A real product file therefore yields an empty
owner and is skipped; its nested scope and current journal are not interpreted.

The accepted cold receipt
[`GAME-SMOKE/20260724-031003`](../../../../debug/evidence/GAME-SMOKE/20260724-031003/result.json)
does not close this gap. `run-game-smoke.ps1` constructs
`equipment-slots-DTMAPI.Smoke.MoreEquipmentSlotsCold.json` in the old ABI
format with top-level owner and archive fields. It proves legacy ABI orphan
recovery, not disabled/uninstalled recovery of the eighth product's actual
sidecar or prepared journal.

Minimum correction:

1. teach the existing Host orphan-recovery boundary to recognize the exact
   ProductNative document and journal schema without taking over live product
   behavior;
2. add focused cold tests that use files emitted by the product serializer,
   including prepared and committed crash states;
3. verify disabled/uninstalled cold recovery from an actual product document
   and removal only after native placement or mail delivery is known to have
   succeeded.

This stays inside the existing Host. It does not authorize a second Host,
public schema, shared product executor or new API.

### P1 — Shield attack handling does not preserve the official native tail

`MoreEquipmentSlotsNativeRuntime.HandleAttackPrefix` and
`ApplyNativeAttackTail` diverge from the official
`BodyController.Attack(int)` body:

- the product path does not reject an already faint body before consuming a
  shield or applying the attack;
- it invokes `DolocAPI.CostHealth` with default values for trailing
  parameters instead of the official monster-attack hurt reason;
- on non-fatal damage it omits the official fishing cleanup
  (`UnsetUiControl` and rod renderer hide) and
  `StateManager.Overwrite<AgentStateHit>()` transition.

The retained compatibility executor already contains equivalent checks and
helpers, showing that the missing behavior is not an intentional contract
change. Current pure effect-policy Units and the protected `grandmas_button`
smoke do not execute the real shield/defense attack tail.

Minimum correction:

1. preserve the official faint guard, exact `CostHealth` argument semantics,
   fishing cleanup and hit-state transition;
2. keep official-shield priority, passive/defense hats, shield hats and native
   hat visuals unchanged;
3. add focused executable tests for shield consumed, shield not applicable,
   non-fatal native tail and fatal/faint branches.

Use reverse reference material only to match behavior; do not copy or
distribute decompiled source.

### P1 — Function cleanup leaves native aggregated equipment state stale

`MoreEquipmentSlotsNativeRuntime.ApplyStoredFunctions` updates the native
function dictionary and then invokes `AgentEquipmentManager.ReloadParams`.
`ClearFunctions`, config disable and real owner deactivation remove/dispose the
same leases without an equivalent native parameter refresh.

Consequently `functions=0` and zero callback/listener/root counts can coexist
with stale passive or defense aggregates until some unrelated native equipment
reload occurs. The current owner-deactivation smoke asserts resource counts,
not the effective native parameters.

Minimum correction:

1. remove the exact product entries and recompute native equipment parameters
   in an order that cannot let the still-active Postfix immediately re-add
   them;
2. preserve exact-owner unpatch even if state restoration or recomputation
   throws;
3. prove config disable, title reset and real Loader deactivation restore both
   zero resources and native passive/defense values.

This is ProductNative cleanup and must not be promoted into SharedNative.

### P1 — Dual-order and failure rollback evidence does not use the real product installer

The Harmony-owner fixture physically exercises product-first order by
installing a product owner on all four targets and then invoking the real Host.
The compatibility-first branch installs the Host, but it does not invoke
`MoreEquipmentSlotsHookInstaller`; it computes whether the product *may*
acquire based on owner count and asserts the computed result.

Likewise the physical partial-install rollback fixture injects a Host failure
gate. Product tests cover `DecideInstall` arrays and source tokens, not a real
ProductNative partial-patch failure and exact-owner rollback.

The product installer appears to implement real owner observation and
exact-owner rollback, so this is an acceptance-proof gap rather than evidence
of a duplicate current hook. It still blocks the claim that both physical
orders, residual owner handling and product-side failure rollback are
fail-closed.

Minimum correction:

1. invoke the real product installer after a real compatibility owner is
   present and inspect all four Harmony targets;
2. inject a product-side failure after a proper subset of hooks and prove all
   product-owned patches are rolled back while unrelated owners remain;
3. exercise residual product and compatibility owners separately, then prove
   real deactivation returns all four targets to the expected exact-owner set.

Extend the current fixture and focused tests; do not create another ownership
gate or receipt family.

## Evidence and Documentation Corrections Required At Audit Time

- Keep Update `20260723-0008` at `implemented/open` until the findings above
  are corrected and independently rereviewed.
- The enabled smoke may remain cited for the behaviors it actually observed.
- Do not cite the `031003` cold smoke as ProductNative sidecar/journal recovery;
  label it as legacy ABI orphan-recovery evidence until a real product document
  is exercised.
- The complete Release PASS is valid for candidate `d38af17a`, but any
  corrective runtime/product change produces a new final candidate and must be
  validated proportionately before `verified/closed`.
- Claims remain limited to reduced **default-loaded Runtime**. The dormant
  frozen executor and ProductNative DLL still ship, so neither total repository
  source nor download/package size is proven smaller.

## Validation Performed

- required project, architecture, API, review and document-governance context:
  read;
- commit range `858bd607..d38af17a` and changed-source boundaries: inspected;
- `git diff --check 858bd607^..d38af17a`: PASS;
- existing complete Release log
  `temp/eighth-final-release-20260724-022948.out.log`: reviewed, PASS;
- enabled and cold game receipts plus their referenced logs and smoke setup:
  inspected;
- product sidecar/journal transaction, Host orphan recovery, Harmony installer,
  effect application/cleanup, frozen ABI metadata and reverse native attack
  behavior: source-inspected.

This independent audit did not launch the game, rerun complete Release, run
L0-L5, run a GC ladder or long test, install/uninstall the player Runtime, or
modify Workshop state.

## Closeout Route

1. Correct replacement rollback and actual product-schema cold recovery.
2. Restore the official shield attack tail and refresh native aggregates during
   every cleanup path.
3. Convert the real-product owner order and product-side rollback cases into
   executable focused tests.
4. Run the affected focused build/Unit/API/Doctor/Catalog/package/transaction
   entries. Reuse the existing Release and smoke authorities.
5. Run only the smallest game evidence still required for real product cold
   recovery and native shield/cleanup behavior, then one final complete
   Release against the frozen corrected candidate if that candidate differs
   from `d38af17a`.
6. Mark Update `20260723-0008` `verified/closed` only after those results are
   green; only then proceed to ninth-product implementation.
