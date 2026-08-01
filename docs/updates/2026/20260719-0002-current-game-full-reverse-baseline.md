# 20260719-0002: Current Game Full Reverse Baseline

## Metadata

- Update ID: `20260719-0002`
- Date: `2026-07-19`
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: User request to preserve and fully unpack the current Doloc Town build before a future official update may replace the last useful before/after baseline.

## Summary

- Froze the current Steam build's official player bytes before performing the slower extraction. The snapshot contains 440 files / 1,687,547,481 bytes and matched the still-installed game byte-for-byte across every preserved file.
- Added a clean-room, repeatable DTMAPI-owned capture/extraction workflow rather than copying predecessor-workspace scripts. It pins AssetRipper 1.3.14 by archive SHA-256, refuses implicit overwrite, and runs on PowerShell 7 and Windows PowerShell 5.1.
- Exported a recovered Unity project containing 45,761 files / 4,045,416,767 bytes. Indexed 85 Unity scenes, 187 parseable official config tables, eight StreamingAssets bundles, 181 managed assemblies, and 2,882 Addressables catalog internal IDs.
- Recovered 85 ordered original UTF-8 build-scene paths directly from the frozen `globalgamemanagers` bytes and mapped them exactly to exported `level0` through `level84`. This preserves the current map-bearing baseline even though AssetRipper could not deserialize the `BuildSettings` object itself.
- Kept official binaries, decompiled source, and extracted official content local-only and excluded from Git/public packages.

## User-Visible Impact

- No runtime or player-facing behavior changes.
- Future Doloc Town builds can be compared against this frozen baseline to determine how official maps and other content were registered and loaded.

## Changed Files

- `tools/scripts/capture-doloctown-reverse-baseline.ps1`
- `tools/scripts/reverse-baseline-path-safety.ps1`
- `tools/scripts/test-reverse-baseline-path-safety.ps1`
- `tools/scripts/test.ps1`
- `references/README.md`
- `docs/updates/2026/20260719-0002-current-game-full-reverse-baseline.md`
- `docs/updates/INDEX-2026-07.md`
- Local-only `references/doloc-town/reverse/builds/23762374_public_C416D4/raw-snapshot/`
- Local-only `references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper-unity-project/`
- Local-only `references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper/`
- Local-only `references/doloc-town/reverse/builds/23762374_public_C416D4/full-baseline-inventory/`
- Local-only `references/doloc-town/reverse/builds/23762374_public_C416D4/README.md`

## Validation

- Steam identity remained build `23762374`, branch `public`, with `Assembly-CSharp.dll` SHA-256 `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`.
- All 440 frozen player files matched the still-installed source by length and SHA-256.
- Actual snapshot/export file counts and byte totals matched the generated inventories.
- All 187 recovered config JSON files parsed; the scene-name recovery produced 85 names and an exact 85/85 exported-level mapping.
- AssetRipper completed `Finished post-export`. Its retained log contains three known `globalgamemanagers` setting-object read errors: asset type 129, `BuildSettings`, and `UnityConnectSettings`. Raw bytes remain preserved; no claim is made that those three settings objects were reconstructed.
- Wwise `MUSIC`/`SFX` payloads and compiled shaders are preserved as recovered build artifacts, not claimed as individually decoded audio files or author-original shader source. This does not reduce the scene/config/Addressables map-diff baseline.
- Snapshot exclusion check found no `BepInEx`, `DTMAPI`, `Mods`, or local `steamapps` root.
- The inventory workflow completed under both PowerShell 7 and Windows PowerShell 5.1.
- PowerShell parser checks passed under both hosts.
- `git diff --check` passed for the tracked task files.
- Private-reference ignore/source-boundary check passed; the new script contains no predecessor-workspace path or script dependency.
- The repository-local `-BuildRoot` boundary now fails closed before creating a directory: it accepts only a descendant of the ignored `references/doloc-town/reverse/` tree, verifies `git check-ignore`, and rejects traversal, sibling-prefix, tracked-tree, file, and existing reparse-point targets. Explicit external targets remain supported.
- The path-safety matrix passed under PowerShell 7 and Windows PowerShell 5.1, including the ignored default, explicit external, unignored temporary repository, traversal, sibling-prefix, and junction cases.
- `tools/scripts/check-doc-governance.ps1` passed with 5,280 checks.
- No game launch or runtime smoke was required or run.

## Evidence

- Current reverse reference: `references/doloc-town/reverse/builds/23762374_public_C416D4/README.md` (local-only).
- Baseline audit: `docs/reviews/api/2026/20260617-0003-reverse-baseline-23762374-audit.md`.
- Snapshot/export summary: `references/doloc-town/reverse/builds/23762374_public_C416D4/full-baseline-inventory/summary.json` (local-only).
- Exact scene mapping: `references/doloc-town/reverse/builds/23762374_public_C416D4/full-baseline-inventory/built-scenes.json` (local-only).
- Snapshot/source receipt: `references/doloc-town/reverse/builds/23762374_public_C416D4/full-baseline-inventory/snapshot-source-parity.json` (local-only).
- AssetRipper log/tool identity: `references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper/` (local-only).

## Related Records

- Debug: none.
- Hook map: none.
- Smoke matrix: not applicable; the game/runtime is not launched.
- API matrix: no public API change.

## Rollback Notes

- Remove only the new local-only snapshot/extraction/inventory folders and the new clean-room capture script if this capability must be reverted.
- Do not remove the pre-existing decompile, metadata, config extraction, or audit records for build `23762374`.

## Follow-Up

- Preserve this baseline unchanged after Steam updates the installed game; do not rerun source-parity validation against a later build.
- Run the same workflow into a new build directory after the official map-bearing build arrives, then compare build-scene mappings, Unity YAML/prefabs, room/map/portal/mark-point tables, Addressables IDs/bundles, and managed-code metadata before proposing any DTMAPI map-content capability.
