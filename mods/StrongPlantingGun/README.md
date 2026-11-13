# DTMAPI 强力农用喷枪

This internal 0.7.5 slice provides four general material slots whenever the
official Mod is enabled. Existing wider native inventories are retained. Product
and item identities are unchanged. Old enable/category/verbose settings are no
longer read. The configuration page has one Range setting: an integer side length
from 7 to 15 tiles (default 7). Saving a changed range cancels the pending batch
and rebuilds the current gun preview. Reset returns to 7; cancel keeps the saved
value. Non-finite/fractional inputs fall back to 7 and out-of-range integers clamp
to 7–15. Malformed JSON uses the existing configuration service's fallback.

The real farming-gun UI delegates its nine specialized operation/tip methods to
the corresponding existing native container methods. Mouse pickup, buffer
placement, one/half splitting, swapping, quick transfer, put/take all and matching
stack shortcuts use native filters, receivers, leftovers and close behavior.
These operations use the current backpack/container, with no remote materials.

The native preview uses the selected square size and the game's original offset
and facing rules. On build 25625185, the native table stays Area(7,7), Offset(-3,0):
for side length N, the right anchor is agent+(-3,0), the left is agent+(3-N,0).
The exact native InitCellTip area read supplies (N,N) without changing shared
configuration. Its validity checks every current material slot through the
same native CheckCanInteract qualification used by the delayed tool batch.
Only the actually selected gun in a ready world may query or update the shared
native tip; retained callbacks from prior guns or loading saves are ignored.
For each target, execution tries seed, fertilizer, film and bottled water in that
order, choosing the lowest currently qualified slot of each category.

The product retains native animation, flight delay and complete native actions.
Each callback checks owner/save/world/room/gun and target identity; each step
rechecks material and consumes one unit after normal success. Rejection, missing
material and invalid delayed targets cost nothing. Unknown native exceptions stop
remaining actions and preserve actual state, without refunds or replay. Water
retains the native empty-bottle result.

Title/Owner cleanup removes all 15 exact product-owned targets, clears pending
work and restores the observed shared function capacity without shrinking held
inventories. It clears the owned preview on leaving the world; Owner exit in a
ready world restores the selected gun's original native size. The V2 owner is DTMAPI.StrongPlantingGunMod.Native; overlapping old
product/Compatibility owners are rejected. The source manifest and SDK declaration
retain minimum Runtime 0.7.0 and product version 1.0.0; the 0.7.5 delivery uses
the selected common candidate without changing these declarations.

The gun's complete inventory uses native save serialization, with no product
gameplay sidecar. The tested cold-load and unsaved rollback preserved full item
data. A cold process without Runtime, product or observer completed native
first-slot pickup/return and a sleeping-bag save; re-enabling preserved the first
slot, backpack and named unrelated values, apart from the expected sleep changes.
Extra slots and an existing six-slot gun naturally survived that native re-save
on build 25625185. This observation is not an uninstall preservation guarantee.

The accepted UI
and E11 action-core evidence are reused for unchanged execution inputs.
Actual cloud transport remains untested. The
accepted S06 product-development/normal-behavior scope follows the revised
R10 boundary, reusing the UI, range, save and qualification evidence. The final
common candidate, exact publication source and release authorization remain
separate gates.
