# Official Tutorial Real-Environment Audit

Status: recorded

Date: 2026-07-15

## Source request

Put the official Doloc Town Workshop authoring tutorials into a real game environment and check/proofread them once, using the earlier `E:\Python_project\DLK` attempt as research history rather than as implementation authority.

## Boundary

- The live Feishu Wiki remains the documentation authority.
- Workshop item `3705665433` (`【官方】示例模组`, version `0.96.06`) is a read-only compatibility fixture. Its files may be inspected or copied into a temporary local runtime fixture, but must not be committed or redistributed.
- The current game target is Steam public build `23762374`.
- The clean-room restriction remains in force: do not copy the old DLK runtime or ModDoctor implementation into DTMAPI.
- Real-game runs use save slot 3 and the shared runtime lock. Every temporary enablement or local package mutation must be backed up and restored.

## Prior DLK evidence

DLK completed two useful but incomplete layers on 2026-05-19:

1. `official_docs_audit_report.md` statically inspected 55 documents, 83 extracted JSON blocks, 107 embedded sheets and 246 images. It reported four JSON/JSONC parse failures plus path, identifier and comment/value mismatches.
2. `mod_doctor_official_example/mod_doctor_report.md` scanned the official example package as 96 JSON files, 611 PNGs and no DLLs. It reported one missing fish-document reference and 40 duplicate-ID warnings caused largely by combining many tutorial examples into one package.

Those reports predate the current Feishu edits and current public build. They are hypotheses to reproduce, not current proof. No retained DLK record supplies a complete per-tutorial current-build game matrix, native runtime-table proof, third-save evidence and clean state restoration.

## Current facts before implementation

- The fresh Feishu crawl contains 55 page tokens and 107/107 real embedded-sheet exports.
- Before stable-fragment merging, the rendered DOM scan exposes 88 code-block records across 24 pages, five more than the old DLK extraction. This is a pre-correction observation: virtualized fragments must be merged before determining the canonical block count.
- A new archive defect was found while preparing this audit: long Feishu code blocks are virtualized. The crawler walked the whole page but retained only the last DOM fragment for each code-block record, so some `content.md` code fences start or end in the middle of a JSON file. The source page is not necessarily wrong; the normalized export is incomplete.
- The official example subscription is installed at the Steam Workshop path, contains 1,349 files, and is currently disabled in `SAVE/mod_infos.json`.
- Other ordinary local and Workshop mods are enabled in the user's current state, so an isolated official-example run must not reuse the current enablement set.

## Audit layers and acceptance gates

### 1. Archive fidelity

- Merge virtualized code lines by stable block ID and `data-line-num` while scrolling.
- Export every code block separately and record line coverage.
- Do not call the page complete when a code block has a missing line range.
- Rebuild the dated snapshot manifest and pass its SHA-256 inventory audit.

### 2. Tutorial text and sample parity

- Parse every complete JSON/JSONC code block after safely removing comments outside strings.
- Map each tutorial's stated file list and example path to the current official example package.
- Distinguish an actual tutorial defect from an intentionally partial excerpt, crawler loss, translated folder suffix, and whole-package duplicate IDs.
- Compare documented IDs, field names and referenced assets with the current sample and current native configuration where a stable mapping exists.

### 3. Isolated loader smoke

- Temporarily disable all ordinary official-local and Workshop entries except `Workshop.3705665433`.
- Launch through the tracked smoke harness, enter save slot 3, and capture DTMAPI/BepInEx/Player logs.
- Require startup, save load, clean exit, no new fatal window/native crash, and byte-for-byte restoration of `mod_infos.json`.
- Treat a clean load only as package-level evidence; it does not prove every recipe, shop, crop, fish, resource, visual replacement or vehicle interaction.

### 4. Native/runtime presence

- Build a machine-readable expectation matrix from tutorial/sample IDs.
- Check that expected records reach the actual native runtime tables after official loading, rather than relying only on file presence or DTMAPI's source index.
- Record missing, overwritten and ambiguous IDs separately.

### 5. Player-visible behavior

- Exercise representative behaviors for basic item, hat, equipment, recipe/store, crop, fish, dish, resource/vegetation, wallpaper/platform and replacement-content groups.
- Record features that need season, map, shop refresh, unlock state or a fresh save as blocked/conditional instead of inferring success.
- Full tutorial verification requires this layer; static and startup success alone are insufficient.

## Known risks and rejected shortcuts

- Do not enable the official example on top of the user's current mod set; conflicts would make tutorial attribution unreliable.
- Do not infer correctness from the official sample package merely being published by the developer.
- Do not treat whole-package duplicate IDs as tutorial errors until the corresponding tutorial is isolated; the official example intentionally aggregates overlapping standalone examples.
- Do not copy official PNG/JSON files into tracked DTMAPI sources or evidence.
- Do not edit DTMAPI author documentation from a single static mismatch. Corrections require current source/sample/runtime evidence and must identify whether they correct official guidance or only add a DTMAPI compatibility note.

## Owning update

- [20260715-0003 Official Tutorial Real-Environment Audit](../../../../updates/2026/20260715-0003-official-tutorial-real-environment-audit.md)
