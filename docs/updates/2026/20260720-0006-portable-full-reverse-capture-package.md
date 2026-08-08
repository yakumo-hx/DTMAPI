# 20260720-0006: Portable Full Reverse Capture Package

## Metadata

- Update ID: `20260720-0006`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: User request to package the full reverse-capture script for preserving a separate pre-release Doloc Town build on another computer.

## Summary

- Reused the tracked clean-room `capture-doloctown-reverse-baseline.ps1` as the package engine instead of creating a divergent reverse pipeline.
- Added a self-contained Steam/game locator and package-local path-safety layer, with explicit rejection of output paths that overlap the game directory or escape the package's dedicated reverse root.
- Added a one-click Windows launcher and a full wrapper that freezes official player bytes, runs the pinned AssetRipper export, decompiles `Assembly-CSharp.dll` and the optional `Assembly-CSharp-firstpass.dll` through pinned ILSpy, and emits hash/integrity receipts.
- Added pinned download identities for AssetRipper `1.3.14`, ILSpy `9.1.0.7988`, and the fallback Microsoft .NET Runtime `8.0.27`; no third-party executable is bundled.
- Corrected Steam branch identity so `public`, `workshop`, and the new baseline's `test` branch are derived from matching `UserConfig.BetaKey` and `MountedConfig.BetaKey` values instead of defaulting every capture to `public`.
- Freezes the appmanifest beside the game bytes and requires its build, branch, and SHA-256 to match the live start identity, the frozen copy, and the live end identity, including `-ReuseSnapshot` and `-InventoryOnly`.
- Made package provenance fail closed unless every payload input and the builder itself are tracked and exactly match the recorded Git commit; package manifest v2 records those scoped inputs.
- Rechecks scoped Git state and HEAD after staging, then compares every staged payload hash with its source before writing the manifest, closing the package-copy provenance window.
- Generated and verified a portable ZIP containing only scripts, documentation, and its own package manifest.

## User-Visible Impact

- Produces a portable ZIP that can preserve and fully unpack a different Doloc Town Steam build on another Windows computer.
- The default path requires only extracting the ZIP, closing the game, and double-clicking `1_RUN_FULL_UNPACK.bat`; `GAME_PATH.txt` and explicit PowerShell parameters cover nonstandard Steam layouts.
- No DTMAPI Runtime, Mod, Workshop, or game behavior changes.

## Changed Files

- `tools/portable-reverse-capture/common.ps1`
- `tools/portable-reverse-capture/reverse-baseline-path-safety.ps1`
- `tools/portable-reverse-capture/run-doloctown-full-capture.ps1`
- `tools/portable-reverse-capture/1_RUN_FULL_UNPACK.bat`
- `tools/portable-reverse-capture/README.zh-CN.md`
- `tools/portable-reverse-capture/THIRD-PARTY-TOOLS.txt`
- `tools/scripts/capture-doloctown-reverse-baseline.ps1`
- `tools/scripts/build-portable-reverse-capture-package.ps1`
- `tools/scripts/reverse-baseline-path-safety.ps1`
- `tools/scripts/steam-appmanifest-identity.ps1`
- `tools/scripts/test-portable-reverse-capture-path-safety.ps1`
- `tools/scripts/test-reverse-baseline-path-safety.ps1`
- `tools/scripts/test-steam-appmanifest-identity.ps1`
- `tools/scripts/test.ps1`
- `docs/updates/2026/20260720-0006-portable-full-reverse-capture-package.md`
- `docs/updates/INDEX-2026-07.md`
- Generated, ignored deliverables: `dist/DTMAPI-DolocTown-Full-Reverse-Capture-20260720-r2.zip` and its `.sha256.txt` sidecar.

## Validation

- Windows PowerShell `5.1` executed the packaged wrapper from the generated staging directory with `-InventoryOnly` against the real frozen `23762374_public_C416D4` baseline and returned exit code `0` after `495.2` seconds.
- The portable Steam resolver independently found `D:\steam\steamapps\common\Doloc Town` without repository settings or `-GameDir`.
- The full baseline recheck reported 440 frozen files / 1,687,547,481 bytes with exact source parity; 45,761 AssetRipper files / 4,045,416,767 bytes; 85 exported scenes and 85 ordered native build-scene paths; 187 config tables; 8 bundles; and 181 managed assemblies.
- The existing main ILSpy project was inventoried as 3,460 files / 8,829,275 bytes. The older baseline did not contain the newly added optional firstpass decompile, so the pinned ILSpy command was also run independently against its frozen `Assembly-CSharp-firstpass.dll`; it produced 28 C# files and one project file with exit code `0`.
- The local pinned AssetRipper and ILSpy archives matched the package's expected SHA-256 values. The fallback .NET Runtime `8.0.27` URL and SHA-512 are pinned from Microsoft's official .NET 8 release metadata.
- The original `r1` ZIP was extracted into an isolated verification directory. All seven manifest-listed payloads matched length and SHA-256, all four PowerShell files parsed with zero errors, the archive contained zero DLL/EXE/PDB/asset/bundle payloads, and no `DLK`/predecessor-workspace reference was found.
- Output-path tests accepted the dedicated package reverse root and rejected the real game root and an unrelated package-local child.
- The corrected portable path test also rejects the package root's parent/ancestors and the game root's parent/children; external dedicated roots and the package's dedicated ignored reverse child remain accepted.
- Windows PowerShell 5.1 parses the corrected source scripts with zero errors. The focused identity test resolves the two real frozen manifests as `public` and `test`, accepts case-insensitive matching values, and rejects missing, conflicting, unsafe, unknown-without-opt-in, and explicitly mismatched branches.
- The focused identity test also requires exact build ID and full appmanifest SHA-256 parity; changed manifest bytes are rejected even when build and branch strings remain unchanged.
- The package builder rejects unsafe `PackageTag` traversal and rejects generation while any scoped payload/builder input is untracked or differs from HEAD.
- From committed source `ca78ec2420df73a4d5063176b256cbcd8cae154e`, the v2 builder generated the package successfully. Its receipt names eight payload inputs plus the builder, records `state=exact-tracked-inputs`, and every input exists in that commit.
- The final ZIP was extracted under the managed test root. All eight manifest-listed payloads matched length and SHA-256 before and after extraction; all five packaged PowerShell files parsed under Windows PowerShell 5.1; forbidden DLL/EXE/PDB/asset/bundle count was zero; private-predecessor reference count was zero.
- A second disposable build under an output directory containing spaces succeeded. A temporary committed-input modification was then rejected before any output directory was created. Both disposable test roots were removed after verification.
- No game launch or runtime mutation was required.

## Evidence

- Source baseline workflow: `docs/updates/2026/20260719-0002-current-game-full-reverse-baseline.md`.
- Portable receipt: ignored local `references/doloc-town/reverse/builds/23762374_public_C416D4/full-baseline-inventory/portable-full-capture-summary.json`.
- Superseded package identity: former local `r1`, 19,033 bytes, SHA-256 `D2B930488613B098487B15A4C7269228DFD64F7566BD3DDC41A32B19EDDF30CE`. Its payload hashes were valid, but its `sourceCommit` did not contain the then-untracked payload inputs and its default branch could mislabel a Steam `test` build as `public`. The untagged and `r1` staging directories, ZIPs, and checksum sidecars were removed after `r2` verification.
- Final package: ignored local `dist/DTMAPI-DolocTown-Full-Reverse-Capture-20260720-r2.zip`, 21,933 bytes, SHA-256 `570400554807F26302BE766512DD9F5CE39ABC0219026686CEEF6760C146F5D6`.
- Checksum sidecar: ignored local `dist/DTMAPI-DolocTown-Full-Reverse-Capture-20260720-r2.zip.sha256.txt`.

## Related Records

- Debug: none.
- Hook map: none.
- Smoke matrix: not applicable.
- API matrix: no public API change.

## Rollback Notes

- Remove the portable support directory, package builder, Update row/record, and ignored generated ZIP; retain the existing local build `23762374` baseline and the tracked clean-room capture engine.

## Follow-Up

- Run the ZIP on the second computer before Steam updates that install. After it produces the pre-release baseline, move the private baseline directory back into the ignored reverse tree for structural comparison; do not commit or publish official content.
