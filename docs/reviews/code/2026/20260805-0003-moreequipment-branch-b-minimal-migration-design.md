# MoreEquipmentSlots Branch B 最简迁移顶层设计

Date: 2026-08-05
Status: `recorded / pre-implementation design frozen`
Audited HEAD: `09ebe5ebf163b2f12ba42d048370b7dfce9a853e`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

Prior authorities:

- [`20260804-0007`](20260804-0007-moreequipment-100-overlap-decision-gate.md)
- [`20260730-0016`](20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)
- [`20260729-0004`](20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md)
- [旧侧车迁移 Manual QA](../../manual-qa/2026/20260729-0001-moreequipment-legacy-sidecar-migration.md)
- [MoreEquipmentSlots Hook map](../../../hook-map/focused/MoreEquipmentSlots.md)

## Scope And User Decision

The user explicitly authorized the full Branch B lifecycle: retain the exact
old `0.3.1-dtmapi` ABI and recovery exit, bring the deferred `1.0.0`
ProductNative path forward, close Review `0016`'s five transaction findings,
and cover current-game attack and native-save changes. Before implementation,
the user asked for the simplest top-level design that preserves items stored
by an old MoreEquipmentSlots save.

This Review owns that pre-implementation design. It does not claim that code,
package, player migration, game behavior, publication or Steam replacement is
complete. Implementation and evidence remain in the owning 0.6 Update.

## Read-Only Data Inventory

The current player machine was inspected without changing the game, official
Mod state, saves or sidecars:

- the current per-save Product-v3 family exists only for `slot-5`, with a live
  document and `.previous` generation;
- all three committed Product slots in the live document are empty;
- the previous generation also has empty committed slots and records a
  completed candidate whose `welder_helmet` was returned to the native
  backpack;
- the old global
  `DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json` does not
  currently exist;
- both retained Local and Workshop MoreEquipmentSlots candidates are disabled.

This proves that the player's currently observed fifth-slot Product document
has no item waiting to be rescued. It does **not** remove the release
requirement to migrate other players' or other saves' legacy state. Historical
fixtures bind the concrete compatibility case: the old flat document may hold
`grandmas_button` in legacy index 0 and `box_hat` in index 1 with shield
maximum 100, remaining value 80 and defend value 3.

## Minimal Top-Level Design

The smallest safe path is a representation migration from one committed
sidecar generation to another. It is not an item transfer through backpack or
mail and is not an ordinary gameplay mutation.

```text
SaveLoaded for the selected native archive
  resolve exact Product sidecar path and expected save scope
  classify bytes before choosing an owner

  valid Product v3 for this scope
    -> load the existing committed document

  flat schema 1..3 for this scope
    -> validate owner, scope, save clock, transaction shape and slot range
    -> map legacy index 0..2 to Product index 0..2
    -> preserve exact item/effect/shield values
    -> preserve exact old bytes in the existing migration backup
    -> atomically publish and round-trip validate Product v3

  exact pre-schema global candidate
    -> use the existing single-winner/current-save claim protocol
    -> perform the same same-index Product-v3 conversion

  future, malformed, ambiguous, wrong-scope, conflicting or occupied >2
    -> fail closed; do not consume, overwrite or relocate source bytes

Product absent or disabled
  -> retained Compatibility Host continues OwnerRecovery to backpack/mail
```

The exact slot mapping is:

| Legacy field | Product-v3 field | Rule |
| --- | --- | --- |
| `index` | `index` | preserve 0, 1 or 2; reject an occupied later index |
| `itemId` | `itemId` | trim and preserve; an empty ID means an empty slot |
| `displayName` | `displayName` | trim and preserve |
| `skillId` | `skillId` | trim and preserve |
| `defenseBonus` | `defenseBonus` | preserve a validated non-negative value |
| `isShieldHat` | `isShield` | direct semantic rename |
| `shieldValue` | `shieldValue` | preserve only for a valid shield |
| `shieldMaxValue` | `shieldMaxValue` | preserve only for a valid shield |
| `shieldDefend` | `shieldDefend` | preserve only for a valid shield |

The existing converter already expresses this mapping and the existing store
already owns exact-byte backup, temporary write, flush, hash/round-trip
validation, atomic publication and the pre-schema global claim protocol. The
Branch B implementation must repair and regress these paths where required;
it must not replace them with a new journal family or move items through a
native inventory as part of format migration.

## Save-Commit Classification

The migrated flat document represents state that the old implementation had
already committed for that save. Publishing the equivalent Product-v3
document therefore changes representation, not gameplay state:

- it may occur during `SaveLoaded` after exact save-scope classification;
- it must preserve the source bytes and never advance a Working equipment
  change;
- it does not require an immediate native `SaveGame` merely to legitimize the
  format conversion;
- any legacy gameplay candidate or transaction journal must be translated
  only when its complete invariant is valid; incompatible mixed shapes fail
  closed;
- subsequent equip, replace, shield depletion or unequip changes remain
  Working until normal native-save success promotes them;
- title or exit without native save restores the prior Committed projection.

Persistent Compatibility Host recovery is a separate
`OwnerRecovery`/`OrphanRecovery` operation. It may use backpack then mail under
its existing durable transaction, but that is the fallback for a missing or
disabled Product owner, not the normal Product-v3 import path.

## Branch B Implementation Boundaries

The implementation has two independently testable exits in one lifecycle:

1. **Retained `0.3.1-dtmapi`.** Frozen `IEquipmentSlotsApi` signatures remain
   unchanged. The Compatibility Host targets the current five-parameter
   `BodyController.OnAttacked`, observes the current native archive family and
   preserves old data/recovery behavior. It never becomes the owner of the
   fixed three-slot 1.0 product.
2. **ProductNative `1.0.0`.** The product keeps exactly three slots, receives
   the same current attack responsibility, preserves Product-v3 migration and
   closes Review `0016`'s five P1 findings: strict mail collection shape,
   exact withdrawal observation, non-missing native fingerprints, exact cold
   journal recovery and removal of delayed count-only placement authority.

No generic protected-storage API, SharedNative storage engine, new receipt
schema, fourth source root, arbitrary slot count or hand-authored Advanced
package is authorized. Publication remains a separate gate after source,
policy, package and player evidence.

## Resolution

Implementation began after this Review froze the design. The owning
[`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
Update owns changed files, implementation deviations, validation and release
state. The eighth five-slice parallel audit is recorded separately in
[`20260805-0004`](20260805-0004-dtmapi-060-eighth-five-slice-parallel-review.md).

## Acceptance Matrix

Source/unit acceptance must prove at least:

- flat schemas 1, 2 and 3 preserve all three slot values and Product scope;
- the historical `grandmas_button` / `box_hat` fixture survives exact
  same-index migration, including shield `80 / 100 / 3`;
- pre-schema global migration remains single-winner and retryable across
  interruption;
- malformed/future/wrong-scope/duplicate or occupied out-of-range input keeps
  original bytes and publishes no Product authority;
- existing Product v3 wins over stale legacy evidence only under the recorded
  authority rules;
- a missing native current archive cannot equal another missing image or count
  as exact native-commit proof;
- mail enumeration rejects strings and unreadable reward entries;
- backpack withdrawal accepts only an exact observed `-1`, irrespective of an
  unreliable native Boolean, and rejects every other delta;
- cold recovery binds exact destination, item count and post-save fingerprint;
- no delayed count-only reconciliation can consume an unrelated same-item
  change;
- current attack behavior covers attackability, Thunder, death/drone escape,
  invincibility, fishing interruption, hit state, hitback and native-shield
  priority for both Product and retained Host.

Game acceptance must use a disposable archive isolated from live Steam
AutoCloud state. It must cover old flat data -> Product v3 -> visible/effective
equipment, normal native save and restart, no-save rollback, an interrupted
save window, retained Product-absent recovery, title/reload and exact owner
cleanup. A read-only public-build comparison or public package identity check
is necessary evidence but is not the endpoint of this Branch B lifecycle.

## Rejected Designs

- **Move every legacy item to backpack/mail, then let the Product re-equip it.**
  Rejected because it changes native inventory state, can overflow, needs a
  second transaction and loses same-slot intent when a direct lossless format
  conversion already exists.
- **Treat the empty live fifth-slot sidecar as proof migration can be removed.**
  Rejected because it is one current observation and historical occupied
  formats are already bound by fixtures and prior player evidence.
- **Patch only the fifth `OnAttacked` argument.** Rejected by the current body
  and native save-family overlap recorded in Review `20260804-0007`.
- **Let a missing/all-missing archive fingerprint compare equal.** Rejected
  because absence is not exact proof that native commit succeeded.
- **Publish 1.0 immediately after source tests.** Rejected; exact current-build
  policy, SDK-generated package, disposable game acceptance and explicit
  publication authority remain required.
