# DTMAPI More Equipment Slots

This admitted Advanced CodeMod owns exactly three additional attribute
equipment slots. Official hat visuals and the official shield remain native
game state. The frozen `IEquipmentSlotsApi` compatibility surface is not this
product's configuration model and retains its historical `0..24` range in the
optional Compatibility Host.

Release status: `1.0.1` is the current observed Steam artifact after the
downloaded subscription tree matched the prepared eight-file package,
manifest, single entry DLL and Advanced receipt exactly. Current
public/subscription identity is owned by the
[Product Catalog](../../../tools/release/dtmapi-product-catalog.json),
[subscription manifest](../../../tools/release/current-subscription-manifest.json),
and [release Update](../../../docs/archive/updates/2026/20260830-0002-moreequipment-101-workshop-release-closeout.md);
there is no remaining upload authorization. The corrected U1 route binds the
exact retained 0.3.1 Workshop tree and proves each protected item exists
exactly once; U3/U4, C0 and the migrated-save no-save/save chain pass their
targeted boundaries, while final cold `GAME-SMOKE/20260730-161355` proves each
target item exists exactly once across backpack, unaccepted mail and committed
sidecar. The separately proposed player claim crash/resume gate remains
withdrawn rather than counted as a PASS: deterministic child-process fault
tests own the filesystem transaction windows, while U2 and migrated-save
evidence own the Unity/save integration.

The current Product uses one exact Harmony owner for five all-or-nothing
native hooks: native parameter reload, typed shield provision, passive-slot
render completion, the AccessoriesBar selectable getter and panel callback
clear. After native passive-slot rendering completes, one `ignoreLayout`
Product row follows the last active official slot and exposes three reusable
full-size slots. Official growth from one through five passive slots moves that
same row by one native step per added slot; the Product never joins or resizes the official
passive-item array or pool. Layout work is event-driven by the native render
callback, not polled per frame. Unexpected native counts, reviewed slot
geometry, or intersection with an official equipment slot or the read-only
drone boundary hides the whole Product row. Stable viewport, backpack,
close-button and full native-control intersection checks run after the panel
settles in the bounded release QA route; transient native panel animation is
not treated as a Product layout failure. The bounded 1.0 promise stops at five
official passive slots (base slot plus four official bags); a sixth official
passive slot remains unsupported and fails closed.
Protected item recovery is coordinated with the native save through an
embedded schema-v3 prepared/committed journal.

Version `1.0.1` also treats a proven native NewGame load as the ownership
boundary for a reused archive index. Before exposing the three empty in-memory
slots, it deletes only that index's complete Product sidecar directory; normal
existing-save loads keep the strict archive, player-name and save-clock checks.
This prevents a deleted save's local Product data from being adopted by a new
save created in the same position without weakening mismatch rejection.

On the first load after an upgrade, the product recognizes the exact public
pre-schema global writer shape plus flat schema 1-3 used by the old mod and the
0.5.5 Compatibility Host. Flat documents must match owner, storage scope,
archive, effective player and native save clock. The schema-less historical
global exception is accepted only when no scoped authority exists. Before
publishing Product v3, the product atomically writes a durable claim at
`DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/<SHA256>.json`;
the claim binds the exact source hash and target save scope. A separate
source-independent `winner.json` is the game-root singleton; a cross-process
operation lock serializes cooperating new processes, while create-if-absent
still prevents a mixed old process from being overwritten. An exact completed
winner is classified before per-hash evidence: stale loser claim/transition
bytes remain discoverable but cannot make the winner unreadable. Per-hash
claims remain optional evidence. Pending-to-completed transitions atomically
capture the
current claim bytes to `.transition-<GUID>`, validate the immutable owner, and
publish completed only into an absent canonical path. A late different owner
and the captured original both remain discoverable fail-closed. Older
schema-1 claims without `state` remain `pending`.

An exact historical Product/archive/global-absent terminal state backfills the
same game-root winner before returning Product authority. If no winner exists,
the pre-winner census includes canonical eligible live and `.previous`
Product authorities across source hashes; multiple historical authorities
remain unchanged and require explicit resolution instead of load-order
selection. An interrupted `.migration-capture-<GUID>` resumes
only when it is the sole capture, matches the Product source and scoped byte
backup, and no replacement global exists. Every global migration kind proves
the active global is absent and the deterministic archive is exact before
success.
Completed validation accepts only the Product store's already-eligible live
or stamped `.previous` authority; invalid, wrong-scope or future previous data
remains fail-closed.
Current-scope pending/incomplete claim state, global capture residue, an
unbound deterministic archive and current-scope canonical backup block empty
authority creation. A validated completed Product/winner/archive for one save,
and identity-bearing Product/backup data for another save, remain unchanged
without preventing a new save from returning an in-memory empty state.
Existing pending evidence or a pending winner must pass the operation lock and
full Product census before any Product/backup/archive write and again before
completion. Completed-winner terminal proof compares immutable save identity,
requires Product revision not older than the winner's migration clock, and
therefore remains valid when later successful saves advance Product revision;
the original save's later cold load uses the same immutable terminal proof
without rewriting winner T0. Exact completed winner authority is classified
before retained loser claim/transition evidence in both completion and
other-save empty handling; loser bytes remain diagnostic only. A collision
while optional evidence is being backfilled reuses the immutable winner plus
terminal Product rule, so Product revision T1 is not forced back through the
obsolete strict T0 validator. The normal current-save anti-ahead rule remains
unchanged. A deterministic `GlobalFlat` archive is terminal for cross-save
empty-state decisions only when its hash is exact and exactly one eligible
canonical Product authority carries the matching `GlobalFlat` migration
stamp; missing, ambiguous or wrong-stamp Product bindings remain fail-closed.
The mandatory proxy derives
the Product owner for capture,
archive, scoped backup, winner/evidence and claim-transition artifacts:
absent-owner artifacts wake the dormant Host, while a loaded healthy Product
suppresses its terminal evidence. A resident Host likewise does not report
that healthy terminal evidence as orphan failure. It never auto-adopts
unresolved residue without the exact Product scope/stamp. These source
corrections and their bounded transition acceptance are closed. The product
also publishes an
exact legacy-byte backup via
temporary write, disk flush, hash verification and atomic move,
validates a save-bound Product-v3 document, and only then replaces or archives
the legacy source. The backup is the short, Mono-safe, hash-addressed
`.legacy-migrations/<SHA256>.flat.json`; same-directory temporary files never
become authority. Product v3 preserves the native save clock, rejects a stored
document or current native scope that omits it, and rejects a committed
document that is ahead of the current native save beyond the frozen tolerance.
Cold Compatibility discovery is recomputed at each relevant lifecycle probe,
so an earlier negative result cannot hide orphan data after an in-process
owner change.
Unknown, future, wrong-save, ambiguous, over-capacity or same-owner
scoped/global conflicts fail closed and are left unchanged. If the product is
disabled or missing, the Compatibility Host routes proven flat documents to
the legacy backpack/mail recovery path instead of treating them as Product v3;
its shared code is only a lightweight format probe, not the migration engine.
