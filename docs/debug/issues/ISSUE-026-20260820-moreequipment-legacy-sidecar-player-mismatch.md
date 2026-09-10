# ISSUE-026: MoreEquipment legacy sidecar belongs to a different player identity

- State: `open`
- Current boundary: Archive 0 found a scoped flat legacy MoreEquipment sidecar whose embedded player identity differs from the current save; migration safely stops before cross-save attachment, while player artifact classification/recovery remains pending.

## Status

- Opened: `2026-08-20`
- Severity: high
- Area: MoreEquipmentSlots / legacy migration / save identity / orphan recovery
- Related review: `docs/reviews/manual-qa/2026/20260820-0001-moreequipment-legacy-sidecar-player-mismatch.md`
- Current Product update: `docs/updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md`

## Player Symptom

After updating only the DTMAPI Runtime installer on 2026-08-20, one player
loaded native archive index `0` and current MoreEquipmentSlots `1.0.0` rejected
the scoped flat legacy file at:

```text
<game>\DTMAPI\config\protected-items\equipment-slots\slot-0\equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json
```

The exact terminal reason was
`legacy-scope-mismatch:player-name-mismatch`. Current native identity was
present and its save clock was `11433`.

## Meaning And Root-Cause Boundary

The legacy source passes format, owner, `storageScope=slot-0`, archive-index
and generation checks. Migration then compares the legacy preferred player
name (`customPlayerName`, falling back to `playerName`) with the current native
scope using the same preference. They do not match.

The Product fails before it publishes an exact legacy backup or Product-v3
document. That is the correct no-cross-save safety disposition: silently
accepting the file could attach three old Product slots and items to the wrong
character. The failed session has no loaded MoreEquipment document.

The stack MVID `4268920ba1014f08b2622dc0909301dc` exactly matches the current
Steam MoreEquipmentSlots `1.0.0.0` DLL, SHA-256
`699E95BC05E79F67EE45D83C89D8119EB2BE723FF342CF2DA0CD0A2C7DBC8E31`.

## Installer Causality

The 2026-08-20 Runtime release changed seven installer/dispatcher scripts only.
Runtime and Product assemblies were not rebuilt. The Runtime transaction does
not own `DTMAPI\config`; install and uninstall preserve configs, logs, reports,
Mods and saves.

A fresh audit of the published Runtime subscription passed with zero blockers
under Windows PowerShell 5.1 and temporary player-like directories. Evidence:
`tmp/test-runs/runtime-installer-20260820-sidecar-report-audit/DTMAPI Workshop Audit 20260820-173201/Results/stress-summary.md`.

Therefore the installer did not create or rewrite the mismatched sidecar.
It may have exposed a dormant conflict by restoring Runtime operation, causing
the first post-update archive-0 load, or selecting the current Workshop
library's game copy. Exact pre/post logs and the install summary are needed to
distinguish those indirect triggers.

## Candidate Origins

- Archive slot 0 was deleted/recreated while its DTMAPI sidecar remained.
- The same native character was renamed after the legacy document was written.
- Native saves and local DTMAPI config came from different cloud/copy/machine
  generations.
- A different game installation/config tree became active after install.
- The legacy file was manually copied into the scoped path.

None is proven by the single error line.

## Safety Rules

- Do not delete, rename or edit the legacy JSON before preserving and
  classifying it. It may contain owned items, a journal or a gameplay candidate.
- Do not force a player-name match. Same slot index is not sufficient ownership
  proof.
- Preserve the source file, any `.previous`, `.legacy-migrations`, global legacy
  candidate, adjacent logs and installer `GameDir` summary.
- Avoid further debug/instant saves while ownership is unresolved.
- Treat legitimate rename recovery separately from deleted-owner/orphan
  recovery and from an actually empty stale file.

## Acceptance Boundary For Any Future Correction

1. Exact same-character rename can rebind only with independent native identity
   proof and no loss/duplication across normal save and cold reload.
2. Deleted/recreated slot reuse never attaches the prior player's Product
   items to the new player.
3. Owner/orphan recovery places every occupied item exactly once through the
   existing quarantine/commit contract and preserves outcome-unknown evidence.
4. Empty stale documents are proven empty, including journal and gameplay
   candidate, before any cleanup authority exists.
5. Copy/cloud/multi-install fixtures remain fail-closed when identity is
   ambiguous.
6. Runtime installer install, repair and uninstall continue to preserve config
   byte-for-byte and never become Product-data cleanup authorities.

## Current Disposition

The protection is working as designed, but player-safe classification and
recovery for this observed stale/mismatched scoped legacy file remain open.
No player artifact has been inspected and no Product, installer, save or
sidecar was changed in this review.

## 2026-08-24 Related Confirmed Slot-Reuse Case

Review `20260824-0001` independently confirms that one player deleted native
slot `0` and recreated it while the scoped MoreEquipment sidecar survived.
That player's retained file is already Product v3 and fails by
`scope-revision-regressed`, so it does not prove the origin of the distinct
2026-08-20 flat-legacy/player-name-mismatch artifact. It does confirm that the
previously listed delete/recreate mechanism is real and that current numeric
slot scoping lacks a complete deletion/orphan lifecycle. The Product-v3,
same-name and clock-catch-up risk is tracked separately in ISSUE-028.

## 2026-08-25 Rename Reassessment

The earlier `pendingNameChange` proposal is no longer selected for the bounded
slot-reuse correction. Current-build source search finds the production
`archiveHandle.SetPlayerName` call in the new-game `first_anim` dialogue, after
`SaveLoaded` and before the first ordinary save. ISSUE-028's `newGamePending`
scope rebind covers that normal initial naming without weakening persisted
identity checks.

An existing-save rename invoked by debug command or future content remains a
separate case. This single legacy mismatch report does not prove that origin,
so no generic scope rewrite or global name-mismatch relaxation is authorized.
The 2026-08-20 artifact remains fail-closed and unclassified.

## 2026-08-25 Embedded-Authority Relationship

ISSUE-028's later design reassessment prefers a Product-namespaced string
inside the native archive if a disposable persistence spike passes. Under that
steady-state model, player name, archive index and save clock are no longer
Product ownership fields: copying and renaming the native archive carries the
Product state as part of the same physical save. This would eliminate the
class of future steady-state name mismatch represented here.

It does not retroactively authorize this flat legacy artifact. A first embedded
migration may read it only through the existing strict legacy classification
and journal/candidate rules; an unresolved player-name mismatch remains
fail-closed. No data was migrated or changed by this design note.
