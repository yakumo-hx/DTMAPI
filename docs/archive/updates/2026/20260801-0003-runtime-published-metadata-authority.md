# Runtime 0.5.5 实际发布元数据权威

- Update ID: `20260801-0003`
- Date: `2026-08-01`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: `release/workshop/runtime/0.5.5/published-metadata/native-normalization`
- Source Request: re-record the exact Runtime bytes actually delivered by Steam after investigating the added `localized_name` field
- Pre-upload authorization Update: [20260801-0002 0.5.5 与功能 Mod 上传目录发布收口](20260801-0002-workshop-upload-release-closeout.md)
- Manual QA chain: [20260801-0002 旧 Runtime、新 Mod 与旧更多装备栏升级兼容手测](../../reviews/manual-qa/2026/20260801-0002-old-runtime-newmods-upgrade-compatibility.md)

## Result

The pre-upload authorization and the post-publication artifact are now recorded
as two distinct authorities. The existing
`releaseStop.publicMutationEntrypoints.runtime` value remains the exact
player-tested tree authorized before upload; it is not silently rewritten as
though the later native metadata normalization had been part of that manual
test candidate.

The Product Catalog adds `runtime.currentPublishedArtifact` for the actual Steam
result:

- Workshop item: `3743016467`;
- Steam manifest: `1475234683223104244`;
- version: `0.5.5`; binary file version: `0.5.5.0`; assembly compatibility
  identity: `0.5.3.0`;
- Steam-delivered upload tree: 31 files, 71,593,719 bytes, SHA-256
  `d7275bccc06207929de4ea62c8976bf20723ed33dd81666149a35289d29d5ebf`;
- player payload after excluding the uploader-control `workshop.json`: 30 files,
  71,593,686 bytes, SHA-256
  `571793091b47d5d2db909059b17c478bdebd60cc53a262bf88ff00e9e4becd63`;
- normalized `info.json`: 9,192 bytes, SHA-256
  `4ab3d471747a7e2ec37bda01884a604dcc63b0b74b764c6d34412b4dae905144`;
- retained `workshop.json`: 33 bytes, SHA-256
  `d6d9206a4a58b88cc985ee72832d57f226ff58767ef6e606a2731b0d604ef98d`.

The tree algorithm is defined directly beside these facts. It excludes the
subscription-generated `Content/.tools/bepinex/extract/**` cache, sorts
forward-slash relative paths with `StringComparer.Ordinal`, writes each row as
lower-case file SHA-256, two spaces and path, joins rows with LF without a final
LF, then hashes the UTF-8/no-BOM text with SHA-256. The 22 extracted cache files
are therefore not mistaken for Steam upload bytes.

### 2026-08-06 normalization correction

A current-subscription recheck found that the two tree hashes originally
recorded here were generated with `StringComparer.OrdinalIgnoreCase`, although
this authority and the nine current ProductNative publication trees define and
use strict `StringComparer.Ordinal`. The retained upload candidate and current
Steam tree were compared file by file: all 31 paths are identical except for
the already owned 90-byte `info.json` native normalization, and the published
`info.json` and `workshop.json` identities still match this authority exactly.
Strict ordinal recomputation on PowerShell 7 and Windows PowerShell 5.1 yields
the corrected full/payload hashes `d7275bcc...d5ebf` and
`57179309...ecd63`; the former mistaken case-insensitive aggregates were
`894026ce...d182` and `b31b09cc...11dd`. The Steam bytes, counts, manifest and
per-file identities remain unchanged. The Catalog and checker correct only the
two derived Runtime aggregates, preserving one normalization contract instead
of creating a Runtime-specific variant of the existing algorithm.

## Root cause and source reconciliation

The 90-byte difference is deterministic official-game normalization, not a
Steam mutation and not a Runtime behavior change:

- native `DolocTown.Config.ModManager` calls `ReloadMods()` from its constructor;
- a local source is passed through `TryMigrateData()`;
- `EnsureLocalizedManifestField(..., "localized_name", string.Empty)` appends
  `schinese`, `tchinese` and `english` with empty values, then writes the
  indented JSON back;
- `ModManifest.Title` falls back to the top-level `name` when the localized value
  is empty, so the displayed Runtime title is unchanged.

The source package builder previously emitted `localized_description` but not
`localized_name`. It now emits the exact empty three-language object itself,
after `localized_description`, so a newly generated `info.json` is already in
the official stable form and does not acquire another 90-byte difference when
the native local-source scan runs.

## Changed files

- `tools/scripts/build-release-workshop-packages.ps1` — emits the stable native
  `localized_name` object for Runtime packages;
- `tools/release/dtmapi-product-catalog.json` — records the exact current Steam
  artifact separately from the pre-upload authorization;
- `tools/scripts/check-product-catalog.ps1` — freezes the manifest, counts,
  byte totals, hashes, normalization owner and owning Update;
- the 2026-08-06 correction also updates `run-game-smoke.ps1`, its Unit source
  contract and the 0.6 authority roadmap to keep strict ordinal normalization;
- this Update, the August ledger and the existing Manual QA Review resolution
  chain — preserve fact ownership without rewriting the earlier observation.

## Validation

- Focused `build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly` package
  generation: PASS. The generated package remains 30 files and 71,593,686
  bytes.
- Generated/published `info.json` exact comparison: PASS. Both are 9,192 bytes
  and SHA-256
  `4ab3d471747a7e2ec37bda01884a604dcc63b0b74b764c6d34412b4dae905144`;
  `localized_name` contains exactly `schinese`, `tchinese`, `english`, all
  empty.
- Actual local upload and filtered Steam subscription tree comparison: PASS at
  both the 31-file Steam-delivered boundary and 30-file player-payload boundary.
- 2026-08-06 correction: retained/current 31-path comparison differs only at
  the known normalized `info.json`; strict ordinal full/payload recomputation
  and all nine current ProductNative trees match the single Catalog algorithm
  on PowerShell 7 and Windows PowerShell 5.1.
- Product Catalog gate under PowerShell 7: PASS.
- Product Catalog gate under Windows PowerShell 5.1: PASS.
- Document governance: PASS.
- The focused package build also passed the packaged Player Doctor release gate.

No game run, Runtime install, Steam upload or full Release run was performed or
required for this metadata-only reconciliation. The complete release-contract
checker requires the paired Author SDK artifact roots and is not a standalone
entry point for this bounded Runtime metadata change.

## External-state boundary

The Steam subscription, official local upload directory, installed Runtime and
game files are read only in this Update. No shared Runtime lock is required and
no player save or sidecar is in scope.

## Rollback

Revert this Update's source and Catalog changes. That restores the old builder
behavior and removes the post-publication authority; it does not alter the
already published Steam item, the retained manual candidate or the historical
pre-upload authorization.

## Follow-up

No Runtime re-upload is required for `0.5.5`: the current Steam bytes already
contain the stable native form. Future Runtime package builds should retain the
explicit `localized_name` projection and use the post-publication authority only
for exact observation, not as retroactive manual-test evidence.
