# ISSUE-028: MoreEquipment save-slot reuse retains the deleted owner's Product-v3 sidecar

- State: `mitigated`
- Current boundary: MoreEquipmentSlots 1.0.1 deletes the exact stale Product directory only at a proven NewGame boundary and passed first-save/cold-load acceptance; absence across both deletion and NewGame remains explicit.

## Status

- Opened: `2026-08-24`
- Severity: high; potential cross-save item attachment
- Area: MoreEquipmentSlots / NewGame reset / protected sidecar / first save / initial naming
- Related review: `docs/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md`
- Related issues: `ISSUE-024`, `ISSUE-026`
- Current published lifecycle: `docs/updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md`

## Player Symptom

A player used MoreEquipmentSlots successfully in native archive index `0`,
deleted that save through the native game UI, then created a new save in the
same archive position. The three Product slots were absent. After the new save
was committed and reloaded, current MoreEquipmentSlots `1.0.0` repeatedly
failed its `SaveLoaded` handler with:

```text
product=scope-revision-regressed; format=ProductV3
```

The error repeated in fresh processes at new-save clocks `1420`, `2441` and
`3717`. Product Entry and all five Product Hooks had succeeded.

## Confirmed Timeline

- `09:08:46`: old archive `0` loaded without a MoreEquipment storage error.
- `09:20:30`: old save returned to title.
- `09:20:37`: `SaveLoaded slot=unknown isNewGame=True`.
- `09:23:08`: returned to title with no intervening native save.
- `09:24:53`: a second `SaveLoaded slot=unknown isNewGame=True`.
- `09:48:33`: the new save completed `SaveSaving/SaveSaved slot=0`.
- `10:05:25`: the first subsequent slot-0 load failed against the retained v3
  sidecar at new-save clock `1420`.
- Subsequent saves, reloads and process restarts retained the same failure.

The player's native delete action is user-confirmed. DTMAPI currently has no
DeleteGame diagnostic. The old-save load, both NewGame boundaries, first native
save and all subsequent slot-0 failures are independently present in the
support logs.

## Root Cause

The Product sidecar is keyed by numeric archive index only. MoreEquipmentSlots
does not observe a successful native `DeleteGame(index)`, so deletion of native
archive `0` leaves `DTMAPI/config/protected-items/equipment-slots/slot-0`
active.

ISSUE-024 then prevents the new save from establishing Product state:
`ModEntry.OnSaveLoaded` discards `IsNewGame`; `OnSaveLoaded(null)` clears the
document and sidecar path; and `OnSaveSaving(slot)` returns unless a matching
non-null document already exists. The new save's first successful native save
therefore cannot bind a new Product document or classify the retained one as an
orphan.

On cold reload, the retained file is classified as Product v3. Raw validation
checks archive index and both player-name fields before its clock. Because the
terminal failure is `scope-revision-regressed` rather than `scope-mismatch`,
the old and new saves have the exact same current Product identity fields; only
the old document clock is more than the 300-second rollback tolerance ahead.

This is a ProductNative lifecycle and owner-incarnation defect. Native save
deletion and same-position recreation are normal player operations.

## Data-Integrity Risk

The current clock guard is temporary:

```text
currentClock + 300 >= storedClock
```

Once a same-name new save catches up to the old document clock, the current
validator can accept the deleted owner's sidecar. If the old three slots are
occupied, this can attach old-owner Product items to the new character. The
support bundle omitted the sidecar, so occupancy, journal/candidate state and
the exact stored clock are unknown. The code path that can later admit it is
confirmed.

## Rejected Hypotheses

- Product disabled/not loaded: Workshop `1.0.0`, Entry and five Hooks passed.
- Hook drift: the installed-game Drift activation passed; failure is a
  deterministic v3 storage validation.
- Accessory-bag unlock requirement: a bag changes native passive-slot count and
  redraws UI; it does not create or bind a Product document.
- Two independent MoreEquipment failures: the Settings UI projects one thrown
  exception once as a Product Monitor error and again as an event-handler
  diagnostic.
- Refactor lifecycle warnings caused storage failure: they occur later during
  repeated title hot refresh and are a separate diagnostic issue.
- Runtime installer wrote the sidecar: config/protected Product state is
  outside installer ownership, and the causal transition is the observed
  delete/NewGame/first-save chain.

## Diagnostic Debt

- Current support collection copies SAVE, logs, reports and selected Runtime
  state, but not registered `DTMAPI/config/protected-items` artifacts. This
  prevented exact item/journal/candidate and stored-clock classification.
- Existing unit coverage proves that a Product revision 301 seconds ahead
  fails closed, but not that a same-name new archive remains permanently
  isolated after its clock catches up.
- Existing disposable save acceptance deletes its whole fixture after testing;
  it does not exercise native delete-and-recreate of the same slot while a
  Product sidecar remains.

## Safety Rules

- The user confirmed that the native save was intentionally deleted and that
  its Product state does not need recovery or preservation. For this incident,
  the complete Product-owned slot-0 directory may be removed with the game
  fully exited; deleting only the live JSON is insufficient because `.previous`
  or transition residue can restore stale authority.
- Do not edit names or clocks to force the old document through validation.
- The preferred bounded correction removes stale Product authority when a new
  owner is proven by `NewGame`, not when the native delete is requested.
- The normal new-game name prompt runs after `SaveLoaded`; the pending in-memory
  scope must therefore be rebound before first save. General existing-save
  rename is not authorized by this correction.

## Acceptance Boundary

1. `SaveLoaded(null, true)` reads the exact archive index from the initialized
   native holder; a non-null event slot must agree. It requires the native
   current file to be absent, idempotently deletes the complete old Product
   slot directory and creates only a non-durable empty `newGamePending`
   document. It renders three Product slots before first save; `null,false`
   remains distinctly fail-closed.
2. The Product does not add a DeleteGame Hook, Core/GameBridge NewGame Hook,
   pending-delete record, incarnation field or schema v4 in this correction.
   DeleteGame-side cleanup remains optional disk hygiene; platform-wide exact
   NewGame slot reporting remains a separate lifecycle improvement.
3. Before first `SaveSaving`, the Product re-reads native scope because the
   ordinary new-game dialogue sets the player's name after `SaveLoaded`. The
   uncommitted in-memory document may adopt that final name; general persisted
   name mismatches remain fail-closed.
4. Every fingerprint path used while `newGamePending` supports exact
   `current=missing` while still hashing actual `prev0..N` and `.bak`. First
   `SaveSaving` always reuses the existing gameplay-candidate protocol, even
   for three empty slots. Successful `SaveSaved`, or cold recovery with exact
   native-commit proof, promotes that candidate into a new Product-v3
   authority. A title return without any save attempt leaves no new sidecar; a
   failed attempt may leave only a prepared candidate that the same preimage or
   the next NewGame can discard.
5. Empty, occupied, journal-bearing, gameplay-candidate and fallback Product
   state under that exact slot is discarded at the proven NewGame boundary; no
   old item enters either a same-name or different-name new character.
6. A disposable AutoCloud-isolated `ArchiveMutation` matrix covers old save ->
   native delete -> same-slot same/different-name NewGame -> first-session UI ->
   initial name prompt -> unsaved title return and normal first save -> cold
   reload, including an initial native save failure/retry.
7. Real-game acceptance retains the existing five Product Hooks and proves
   three visible/usable Product slots, no storage/event error, no cross-save
   item attachment, clean title return and clean process exit.
8. Product-absent-through-delete-and-NewGame, debug/future existing-save rename,
   and native `DuplicateGame` are outside this bounded fix. A later mismatch
   stays fail-closed and requires manual slot-directory cleanup, but a same-name
   save that already caught up to the old v3 clock cannot be distinguished
   reliably; this residual risk is explicit, not accepted proof.

## Current Disposition

Root cause is confirmed and remains open. This audit did not change Product,
Runtime, player saves, configuration or sidecars and ran no game process. Any
implementation requires a new bounded Update and must close ISSUE-024 together
with this issue. After reopening the alternatives, Product-local NewGame reset
is the preferred minimal candidate rather than a binding user-selected design;
no source change has started.

## 2026-08-25 Native-Archive Embedded Storage Reassessment

The Product-local NewGame reset above remains the smallest correction **only
if Product-v3 sidecars remain the steady-state authority**. The user has since
accepted a simpler disabled-owner contract: occupied Product slots may remain
parked and inaccessible while the Product is absent, then reappear when it is
enabled again. Automatic backpack/mail orphan recovery is no longer a required
steady-state feature. Under that contract, an embedded native-save value is
the preferred design candidate and the NewGame reset is now a fallback.

Current build `24788406_public_F06183` confirms that native save, backup,
delete and duplicate can own the Product data lifecycle. Directly adding an
unknown top-level JSON property is not viable because `ArchiveDataHandle` is
opt-in and has no extension-data member; an unmodified native load/save drops
unknown properties. Directly extending serialized `passiveItems` is also
rejected: its length is official accessory-bag progression, native UI/effects
remain active without the Product, and the native slot type does not preserve
the Product's hat/shield semantics.

The narrow candidate is one namespaced string in the already serialized
`cityData.dialogueManager.variableStorage`, containing only schema and the
three Product slot DTOs. The physical archive becomes identity, so a new
`ArchiveDataHandle` has empty Product state, native duplicate copies it,
DeleteGame moves it with the deleted archive, player rename is irrelevant, and
native SaveGame commits backpack and Product mutations together. Product
absence leaves the string inert instead of moving items.

This container is a native dialogue-variable dictionary, not an official
generic ModData contract. Before implementation it must pass a disposable,
AutoCloud-isolated spike proving that a namespaced unknown string survives a
native load/save with the Product absent and is not cleared by Yarn program
initialization. Failure of that spike returns the issue to the bounded NewGame
reset; it does not authorize a broad serializer patch.

Existing Product-v3 data remains a migration input. When no embedded key
exists, the current strict journal/candidate reconciliation may seed the native
in-memory value; only successful native save makes it authoritative. Once a
valid embedded value exists, it wins over stale sidecar residue so a crash
between native commit and sidecar archival cannot import the same state twice.
The detailed evidence, transition and acceptance matrix are recorded in Review
`20260824-0001`. No source or player data changed in this reassessment.

## 2026-08-26 Interim Product-v3 Mitigation

The user chose to retain disabled-Product item fallback while the embedded
authority trade-off goes to a later player vote. Update `20260826-0001`
therefore implements the bounded Product-local NewGame reset as
MoreEquipmentSlots `1.0.1`, without adding a DeleteGame Hook, schema v4,
incarnation identifier or public save API.

At a proven `isNewGame=true` boundary, ProductNative now reads the exact native
archive index, rejects event/holder disagreement and an already-present native
current, deletes only the exact complete Product slot directory, and creates an
empty in-memory three-slot document. The first save refreshes the post-dialogue
name, allows only native current to be `missing` in the preimage, and commits
through the existing gameplay-candidate transaction. Existing-save identity
and clock checks remain strict.

Focused Product and Harmony-owner tests passed whole-directory deletion,
sibling preservation, mismatch rejection, same-name/different-name stale-owner
reuse, empty first save and cold load. On real game code, isolated archive index
`11` passed NewGame cleanup plus first native save in
`GAME-SMOKE/20260826-022728`; the new native current and Product-v3 authority
then passed an independent `NoNativeSave` cold load in
`GAME-SMOKE/20260826-023044`, including unchanged native/committed-sidecar
proof, no storage/event error, clean cleanup and process exit.

This closes the reported ordinary path as a `mitigated` issue. It is not marked
`verified` because the Product cannot detect a new owner if it was absent for
both native deletion and NewGame, first-session UI screenshot acceptance was
not captured, and DuplicateGame/general rename remain separate debts. The
embedded-native design remains a future voted migration rather than part of
this patch.
