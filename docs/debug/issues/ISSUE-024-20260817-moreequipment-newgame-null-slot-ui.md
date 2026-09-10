# ISSUE-024: MoreEquipment new-game null slot suppresses Product UI

- State: `mitigated`
- Current boundary: MoreEquipmentSlots 1.0.1 resolves the initialized native index and creates an empty pending document at NewGame; first-save/cold-load runtime acceptance passed, while first-session UI screenshot and Product-absent coverage remain.

## Status

- Opened: `2026-08-17`
- Severity: high
- Area: MoreEquipmentSlots / ProductNative / NewGame / UI / save lifecycle
- Related review: `docs/reviews/manual-qa/2026/20260817-0001-moreequipment-newgame-null-slot-review.md`
- Current published lifecycle: `docs/updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md`

## Symptom

In the first gameplay session of a newly created save, opening the equipment UI
does not show the three MoreEquipmentSlots Product slots. The same session loads
the current `1.0.0` Product successfully and installs all five Product Hooks.

## Reproduction And Evidence

- The penultimate DTMAPI startup log is
  `D:\Steam\steamapps\common\Doloc Town\DTMAPI\logs\latest-20260816-162048769.log`;
  its Unity companion is
  `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player-prev.log`.
- DTMAPI selects the enabled Local source, accepts the Advanced receipt, installs
  five Hooks, reports `slotCount=3`, commits the load transaction and completes
  Entry.
- The lifecycle then dispatches `slot/index=unknown isNewGame=True`. Product
  SaveLoaded cleanup reports `clones=0`, `roots=0`, `hooks=5`.
- Unity enters and exits `EquipmentBarUiState` twice after SaveLoaded. There is
  no later Product-render or layout-block record.
- The Local and subscribed Product trees are byte-identical current `1.0.0`
  artifacts. Their Product DLL SHA-256 is
  `699E95BC05E79F67EE45D83C89D8119EB2BE723FF342CF2DA0CD0A2C7DBC8E31`;
  config is enabled with three Product slots.
- No game process was launched for this review. The evidence is the user's
  physical reproduction plus read-only log/source/artifact inspection.

## Root Cause

`SaveLoadedEventArgs` contains both `SaveSlot` and `IsNewGame`, but Product
`ModEntry.OnSaveLoaded` passes only `e.SaveSlot` into
`MoreEquipmentSlotsNativeRuntime.OnSaveLoaded(int?)`.

Native NewGame legitimately dispatches a null slot. The Product treats every
null/negative slot as no archive: it clears archive index, scope, sidecar path
and Product document, then returns. `RenderAccessoriesBar` silently returns
when the document is null, so the installed `AccessoriesBar.RenderPassiveItems`
Postfix cannot create the three Product clones.

An existing save follows a different path: the `LoadGame(index)` Hook supplies
the exact slot, Product loads/migrates its scoped sidecar or creates an in-memory
empty three-slot document, and the next native `RenderPassiveItems` call can
create the three Product clones. A pre-existing sidecar is not required.

This is not ISSUE-021: no placement, save quarantine or cold-recovery
transaction has begun. It is a distinct pre-scope NewGame/UI lifecycle gap.

## Rejected Hypotheses

- Disabled Product or config: the enabled Local source loaded and config is true.
- Stale `0.3.1` or mismatched Local/Workshop package: both inspected trees are
  identical current `1.0.0` bytes.
- Hook failure: all five Product Hooks and Entry completed.
- Unsupported native passive count `6+`: a new save begins at one native passive
  slot and no layout rejection was logged.
- Equipment UI was not opened: Unity records two complete open/close cycles.
- Broad `Feature.EquipmentSlots=ready` proves Product UI: that status belongs to
  GameBridge feature fanout and does not observe Product clone creation.
- One accessory bag is required to unlock the Product slots: the native
  `add_accessory_slot` command only calls
  `SetPassiveSlotCount(passiveItems.Length + 1)`. It adds one official passive
  slot and may cause a native UI redraw, but does not load/save an archive or
  initialize the Product document. In the affected null-document session that
  redraw still returns early. A report that the slots appeared after the bag
  most likely includes a later save/title/reload or confuses the new official
  slot with the three Product slots; an immediate same-session appearance would
  be a separate reproduction requiring its own exact logs.

## Ownership And Safety Boundary

- Owner is the MoreEquipmentSlots ProductNative lifecycle/UI/sidecar state
  machine, not SharedNative GameBridge and not the frozen public compatibility
  API.
- `isNewGame=true, slot=null` needs a distinct pending-scope state. An empty
  in-memory Working document may render the UI, but ordinary gameplay state
  must not be durably committed before a successful native `SaveGame`.
- First-save scope binding must use exact native archive holder/index/identity;
  it must not guess a future/free slot. `SaveLoaded(null, false)` remains
  fail-closed.
- New-save runtime acceptance is an `ArchiveMutation` test and must use a
  disposable Steam AutoCloud-isolated fixture.

## Acceptance Criteria

1. Focused tests cover `SaveLoaded(null, true)` and prove one-native-plus-three-
   Product rendering/navigation before first reload; `SaveLoaded(null, false)`
   stays fail-closed with a distinct diagnostic.
2. A disposable fresh-save run opens the equipment UI before cold reload and
   proves three visible Product slots, the existing five installed Product
   Hooks and no layout block.
3. After establishing the new-save baseline, a `NoNativeSave` mutation rolls
   back exactly; current/prev/bak and committed sidecars are unchanged before
   cleanup.
4. A normal native save binds the exact archive identity and promotes Product
   state only after `SaveSaved`; cold reload retains the item exactly once.
5. Failures around `SaveSaving`, native save and `SaveSaved` retain the previous
   committed state or reconcile an exactly proven native commit without loss or
   duplication.
6. Existing-save `1–5` native plus three Product slots, config disable/cleanup,
   equip/unequip and ISSUE-021 quarantine behavior do not regress; native `6+`
   remains explicit fail-closed.

## Current Disposition

Root cause is confirmed and the issue remains `open`. This review made no
implementation, package, player-environment or save changes and ran no new game
smoke. A future implementation requires a new bounded Update because the
published `20260811-0001` lifecycle is already verified and closed.

## 2026-08-24 Independent Player Recurrence

Player support logs in Review `20260824-0001` contain two independent
`SaveLoaded slot/index=unknown isNewGame=True` boundaries at `09:20:37` and
`09:24:53`. The same Product had loaded and installed five Hooks, yet no Product
document could exist before first save. The later first `SaveSaving/SaveSaved`
carried slot `0`, but the current `document != null` precondition made both
callbacks ineffective. This independently confirms the first-session gap and
shows how it composes with retained same-slot Product-v3 state; that second
boundary is tracked in ISSUE-028.

## 2026-08-25 Preferred Minimal NewGame Route

The earlier candidate is no longer treated as a binding user-selected design.
After comparing DeleteGame cleanup, a shared NewGame slot Hook and Product-local
resolution, the preferred minimal route remains Product-local: pass
`IsNewGame`, read the exact initialized native archive index when the event slot
is null, require its current file to be absent, idempotently remove the complete
old Product slot directory, and create an in-memory empty `newGamePending`
document. No new Product or platform Hook is required.

The native first-game dialogue sets the player's name after `SaveLoaded`, so
first `SaveSaving` must rebind the still-uncommitted scope to the final native
name. All pending-session fingerprint paths support `current=missing`, and the
first save always uses the existing gameplay-candidate protocol even when all
three slots are empty. `SaveSaved` or exact cold commit proof promotes it; a
title return with no save attempt writes no new sidecar. The detailed boundary
and explicit absent-Product limitation are owned by ISSUE-028 and Review
`20260824-0001`.

## 2026-08-25 Embedded-Authority Supersession

The Product-local route above is retained as the fallback for a sidecar-based
fix, not the current long-term preference. With the user-approved
park-while-disabled behavior, ISSUE-028 now prefers a Product-owned namespaced
string inside the native archive. A fresh `ArchiveDataHandle` naturally lacks
that value, so NewGame can initialize an empty in-memory three-slot projection
without resolving a numeric save slot, deleting a directory, binding a player
name or preparing a sidecar candidate. Ordinary native SaveGame then commits
that projection.

This supersession is conditional on the disposable persistence spike specified
in Review `20260824-0001`. Until it passes, ISSUE-024 remains open and no native
storage capability is claimed as verified.

## 2026-08-26 Bounded Product-v3 Mitigation

Update `20260826-0001` implements the smaller sidecar-preserving correction as
MoreEquipmentSlots `1.0.1`; the embedded-authority proposal remains deferred
for player voting. `ModEntry` now forwards `IsNewGame`, and ProductNative
resolves the initialized archive holder, requires native current to be absent,
removes the complete stale Product slot directory, and establishes an empty
three-slot `newGamePending` document before the first UI render can consume it.
The first `SaveSaving` rebinds the final name and uses the existing candidate
transaction even when all three slots are empty.

Focused tests passed the null-NewGame, event/native index conflict, present
native-current rejection, complete directory deletion, same-name and
different-name reuse, first save and cold-load cases. Runtime evidence
`GAME-SMOKE/20260826-022728` then passed real NewGame plus the first native save
on isolated archive index `11`; `GAME-SMOKE/20260826-023044` independently
cold-loaded that archive in `NoNativeSave` mode with unchanged native and
committed Product files, no storage/event error, clean fixture cleanup and
clean process exit.

The state is `mitigated`, not `verified`: the corrected runtime document path
was exercised, but this bounded run did not capture a first-session equipment
UI screenshot. Product absence throughout both deletion and NewGame also
remains an explicit ISSUE-028 limitation. The older conditional paragraph
above records the design history; it no longer blocks this selected interim
Product-v3 mitigation.
