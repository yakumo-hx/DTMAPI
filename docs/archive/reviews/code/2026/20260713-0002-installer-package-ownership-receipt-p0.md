# 20260713-0002 Installer Package Ownership Receipt P0

Status: recorded / product decision closed / P0 implementation open
Date: 2026-07-13
Scope: destructive ownership authority for DTMAPI-installed official-local packages
Related decision docket: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0001-major-update-second-decision-docket.md`
Refines: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

## Source Request

The user asked to continue the full boundary audit discussion, including the finding that public author guidance tells third-party authors to create `dtmapi-package.json` while the uninstaller treats that file as DTMAPI ownership.

This review is a source/docs root-cause record. It does not change the installer, uninstaller, author packages, local `MODS`, official enablement state, game files, or Workshop subscriptions.

## Root Cause

`Content/DTMAPI/dtmapi-package.json` currently carries five unrelated meanings:

1. author-supplied source/build metadata;
2. DTMAPI content recognition;
3. release/upload package marker;
4. installer permission to overwrite an existing official-local directory;
5. uninstaller proof that DTMAPI owns and may move the whole directory.

The first three meanings are non-destructive metadata. The last two grant destructive authority. An author-controlled file inside the candidate directory cannot prove who installed or owns that directory.

Concrete current paths:

- `author-docs/content-packs/custom-animal-json-png-wav.md` tells a third-party author to create the file;
- the retained Feishu draft includes a copyable example whose `owner` is `DTMAPI`, so checking only that text is not sufficient;
- `ContentQueryService.HasDtmApiMarker` uses the file as one way to classify DTMAPI content;
- `build-release-workshop-packages.ps1` writes it as build/package metadata;
- `install-to-game.ps1` accepts its existence as permission to update an existing official-local destination and may remove other content beneath that destination;
- `Get-DtmApiOwnedOfficialLocalPackages` recognizes every directory containing the file, even when the file is empty, malformed, or unrelated to the current installation transaction;
- `uninstall-dtmapi.ps1 -RemoveOfficialLocalPackages` removes corresponding `mod_infos` entries and then moves every existence-recognized directory.

The uninstaller defaults to keeping official-local packages and creates backups when removal is explicitly requested. Those are useful damage mitigations, but they do not create ownership authority. The current operation also changes `mod_infos.json` before every package move has succeeded, so a later move failure can leave source enablement and files out of sync.

## Current Local Evidence

Read-only inspection on 2026-07-13 found:

```text
official-local directories recognized from marker existence: 22
packages in the installed release-manifest BundledMods:        14
marker-only directories outside that current receipt set:       8
```

The eight marker-only folders are:

- `DTMAPI_DreckoAssets`;
- `DTMAPI_ExtraVehicle`;
- `DTMAPI_HatchAssets`;
- `DTMAPI_LightningChicken`;
- `DTMAPI_MoleAssets`;
- `DTMAPI_OilfloaterAssets`;
- `DTMAPI_ShellCrab`;
- `Yuuka_DTMAPI_ChickenPetBag`.

This is not proof that any of the eight are third-party or unwanted. It proves only that marker existence produces a broader destructive candidate set than the current external installation record.

## Options

### A - Runtime-only uninstaller

Remove official-local package deletion from the DTMAPI uninstaller entirely.

Benefits:

- simplest fail-safe boundary;
- never interprets author content as installer ownership.

Costs:

- verified first-party local packages installed by DTMAPI remain behind;
- cleanup requires the official UI or a documented manual process.

### B - Validate the existing marker more strictly

Keep using `dtmapi-package.json`, but require fields, product allowlists, and matching ids.

Benefits:

- smaller implementation change.

Costs:

- the marker remains controlled by the directory being deleted;
- public examples already demonstrate copyable DTMAPI-looking values;
- build metadata and installation authority remain conflated.

This is not sufficient as the final boundary.

### C - Separate build identity from installation ownership

Recommended when DTMAPI should retain a complete uninstall option.

Use three distinct artifacts:

```text
Content/DTMAPI/manifest.json
  author/runtime identity; author-controlled

release/build manifest
  generated package provenance; non-destructive tooling input

dtmapi-install-receipt.json + external DTMAPI/install-state.json
  installer-created matching transaction; destructive authority
```

Destructive candidates must be enumerated from the external install state, not by scanning directories for a marker. Receipt and state must agree on at least:

- schema and receipt kind;
- install transaction id;
- canonical `MODS` root and relative path;
- official folder and `UniqueID`;
- product/catalog identity;
- installer-managed file list and hashes;
- detected unknown additions or identity drift.

A package-local receipt without matching external state, external state without a receipt, a path/id/hash mismatch, or a modified/unknown directory must fail closed: do not overwrite, do not move, and do not modify `mod_infos.json`.

## Recommended Migration

- From 0.5.5 onward, permanently downgrade legacy `dtmapi-package.json` to non-destructive legacy/build metadata.
- Remove it from author requirements. `manifest.json` is enough to recognize DTMAPI CodeMods/ContentPacks.
- On a later verified install, migrate only packages whose exact path/id is present in the external install/release state. Back them up before writing a new receipt.
- Treat the eight current marker-only folders as `legacy/unverified`; keep them and report them. Do not silently adopt them.
- Change the uninstall transaction to back up/move verified packages first, then atomically update `mod_infos.json`; restore or leave the enablement file unchanged when any move fails.
- A non-destructive upload/build UI may read legacy build metadata for one transition release, but it must not convert that metadata into delete/overwrite permission.

## Acceptance Matrix

Temporary-root tests must cover:

- a third-party author marker;
- a copied marker containing `owner=DTMAPI`;
- empty and invalid JSON;
- a forged receipt without external state;
- wrong transaction, path, folder, `UniqueID`, or hash;
- a legacy marker-only package;
- a correct receipt plus matching state;
- a verified package with unknown added files;
- a directory move/backup failure.

Every non-matching case must leave both the package directory and `mod_infos.json` byte-identical and report `unverified/refused`, not `owned`. A valid case must pass dry-run, backup/move, atomic enablement update, failure recovery, PowerShell 5.1 parsing, and source tests.

## Decision Boundary

The user closed the product boundary on 2026-07-13:

- the **player DTMAPI uninstaller uses A** and removes Runtime only;
- transaction-verified cleanup **C** (called A2 in the second docket) moves to the Author SDK and internal development/QA/deployment toolchain;
- if a player-facing tool later offers cleanup for an Author-SDK-deployed package, it may only invoke that same verified receipt transaction;
- `dtmapi-package.json`, a stricter version of it, and every marker-only legacy package permanently remain non-destructive.

The implementation P0 remains open until the player uninstaller loses marker scanning/official-local mutation, destructive operations use the fail-closed receipt matrix only inside the owning deployment toolchain, public author guidance is corrected, and the temporary-root acceptance matrix passes.

## Resolution — 2026-07-13

The player/current-installer marker-ownership P0 is verified by [Update 20260713-0011](../../../updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md): player A1 is Runtime-only, marker authority is permanently non-destructive, and the current developer installer refuses existing or racing destinations. Author SDK A2 receipt cleanup remains deferred; this resolution does not claim that future transaction lane is implemented.
