# 20260713-0011 Player Runtime-Only Uninstall Ownership P0

## Metadata

- Update ID: `20260713-0011`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Area: installer/uninstaller/ownership/receipts/official-local/author-docs/workshop-package
- Source: user requested the next Full Boundary Audit construction step after verified Batch 0

## Source Request And Ordering

The user requested the next step from the Full Boundary Audit construction plan after Batch 0. The later decision closure in `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` refines the original audit labels and explicitly orders the installer ownership P0 before the independent Oil/OneAction ownership P0.

This Update implements the closed A1 player boundary from `docs/reviews/code/2026/20260713-0002-installer-package-ownership-receipt-p0.md`:

- the player DTMAPI uninstaller removes Runtime-owned files only;
- it never scans `MODS` for `dtmapi-package.json`, never treats author/build metadata as ownership, never moves an official-local package, and never edits `SAVE/mod_infos.json`;
- any retained legacy cleanup switch must fail closed as a compatibility no-op;
- the current local developer installer must not overwrite an existing official-local destination merely because that destination contains `dtmapi-package.json`;
- author-facing guidance must stop requiring or recommending `dtmapi-package.json`;
- temporary player-like tests must prove third-party, copied-DTMAPI, empty, malformed, forged, legacy, and receipt-looking package candidates remain byte-identical in dry-run and real uninstall modes.

## Scope Boundary

This Update does not implement the future Author SDK, its transaction receipt schema, verified SDK cleanup, 0.5.5 version authority, Oil/OneAction changes, public uploads, or game behavior. Package-local receipts plus matching external state remain the only acceptable future destructive cleanup model, but that model belongs to the Author SDK/internal deployment toolchain and is not exposed by the player Runtime uninstaller.

`Content/DTMAPI/dtmapi-package.json` may remain readable as legacy/non-destructive build or content-classification metadata during compatibility transition. It grants no permission to overwrite, move, delete, adopt, or edit official enablement state.

## Changed Files

- `tools/scripts/uninstall-dtmapi.ps1`: removes official-local marker scanning, package backup/move, and `mod_infos.json` mutation from the player uninstaller; the retired cleanup switch now warns, records the request, performs no package scan, and continues Runtime-only uninstall;
- `tools/scripts/release-common.ps1` and `tools/scripts/check-dtmapi-status.ps1`: retain marker reads only as `LegacyMetadataOnly` diagnostics with `Authority=None` and `DestructiveActionAllowed=false`, and stop advertising player cleanup;
- `tools/scripts/install-to-game.ps1`: refuses every existing official-local destination regardless of marker contents, removes the old destination-cleanup path, prepares new packages on the same persistent volume but outside the native `MODS` scan root, and publishes them with collision-failing `Directory.Move` so a destination created during preparation is not merged or overwritten and an interrupted stage cannot be enumerated as a package;
- `tools/scripts/test-player-runtime-only-uninstall.ps1` and `tools/scripts/test.ps1`: add the PowerShell 5.1-compatible temporary-root ownership matrix, syntax gates, pre-existing destination refusals, successful fresh atomic publication, and tracked Release execution;
- `tools/release/dtmapi-product-catalog.json` and `tools/scripts/check-product-catalog.ps1`: replace the stale external marker-ownership gate with the verified player Runtime-only state while keeping Author SDK cleanup deferred;
- `docs/guides/uninstall-dtmapi.md`, `docs/guides/migrate-from-old-doloc-smapi.md`, and `docs/guides/install-dev-preview.md`: document Runtime-only player uninstall, permanent marker non-authority, and fail-closed developer local installation;
- `author-docs/content-packs/custom-animal-json-png-wav.md`, `author-docs/content-packs/new-farm-animal-draft/README.md`, and the editable `draft.md`: remove marker creation from current author instructions while retaining source snapshots as historical inputs;
- `docs/workflows/workshop-package-subscription-test-matrix.md`: adds the Runtime-only byte-preservation matrix row;
- `docs/releases/0.5.0-alpha-developer-preview-checklist.md` and `docs/releases/0.5.0-alpha-release-hygiene-report.md`: add narrow superseded notes without rewriting their historical facts;
- this Update, `docs/updates/INDEX-2026-07.md`, and the focused Review resolution link.

## Implemented Boundary

- The player uninstaller owns Runtime files only. It does not enumerate official-local packages, read package-local metadata, or modify `SAVE/mod_infos.json` in dry-run, normal, or retired-switch paths.
- `dtmapi-package.json` is permanently non-destructive legacy/build metadata. Status output may report its presence but exports no enablement-removal id and grants no overwrite, adoption, move, or deletion authority.
- The current developer installer can create a previously absent official-local package, but cannot update an existing one. New content is assembled outside both the final destination and the native `MODS` scan root, then published by an atomic same-volume directory move; a pre-existing or racing destination is preserved and reported, while an interrupted stage remains outside game package enumeration.
- The future Author SDK/internal deployment lane may later implement receipt plus matching external-state cleanup. No receipt schema, player cleanup surface, silent legacy adoption, or destructive SDK transaction is claimed by this Update.

## Validation

- `tools/scripts/test-player-runtime-only-uninstall.ps1 -Configuration Release` passed under PowerShell 7 and Windows PowerShell 5.1.26100.8655 in roots containing spaces and Chinese characters. Both hosts preserved nine package candidates and `mod_infos.json` byte-for-byte across dry-run, real uninstall, and the retired-switch path; classified seven legacy markers as non-destructive; refused eight existing destinations; atomically published eight fresh destinations; and left no staging directory.
- The dedicated matrix also proved that the retired switch still removes the Runtime plugin while preserving third-party BepInEx content, and that post-uninstall status reports the Runtime-only/non-destructive boundary with a nonzero incomplete-runtime result.
- `tools/scripts/check-product-catalog.ps1` passed in both PowerShell hosts: 26 products, 11 first-party public products, 21 Workshop snapshot items, and 45 API rows.
- A unique temporary Runtime package was produced with `build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly`. The optional staged-artifact Catalog check passed in both hosts. The player-package audit parsed all ten packaged PowerShell scripts under Windows PowerShell 5.1, passed all eight missing/empty/valid install, status, collect, uninstall, and post-uninstall cases with zero blockers, observed expected exits `07=0` and `08=1`, and left the explicitly isolated persistent root empty.
- The final `tools/scripts/test.ps1 -Configuration Release` run passed in 106.3 seconds. All tracked Runtime and Mod projects built for their existing targets with zero warnings/errors; `DTMAPI.UnitTests`, evidence-retention checks, the ownership matrix, the Catalog checker, and tracked source gates passed.
- `tools/scripts/check-doc-governance.ps1` and `git diff --check` passed after the final documentation update.

No real game directory, local official `MODS`, Steam subscription, Workshop item, save, or running Doloc Town process was touched. All installer/uninstaller checks used verified temporary roots, so the shared Runtime lock was not required and Runtime Validation remains `not-required`.

## Evidence

- Uninstall state now records `PlayerUninstallPolicy=RuntimeOnly`, `OfficialLocalPackageScanPerformed=false`, and `OfficialLocalPackageRemovalPerformed=false`; compatibility arrays for detected/removed packages and enablement entries remain empty.
- The ownership fixture fingerprints directories plus file lengths and SHA-256 values, so the byte-preservation result covers marker-only, forged-receipt, receipt-looking, unknown-added-file, no-marker, third-party, copied-owner, empty, and malformed candidates as well as `mod_infos.json`.
- The staged package audit used an explicit `DTMAPI_DOLOC_PERSISTENT_ROOT` under the system temp directory. Its isolated persistent tree remained at zero items; no fallback to the real player path occurred.
- Temporary package/audit roots were treated as ephemeral validation material and removed after the result was recorded; no generated package was promoted to a release or upload lane.

## Rollback

Restore the previous scripts and guidance, remove the new source matrix from the tracked test path, and remove this Update/monthly row. No game, subscription, Workshop, save, or runtime rollback should be required because validation is isolated.

## Follow-Up

This player/current-installer ownership P0 is verified. Proceed next to the independent Oil/OneAction ownership P0: explicit Oil content/product demand, no OneAction-to-Oil callback, generic reviewed GameBridge adapter only, missing GameBridge dependencies, and third-save Oil-off/Oil-on coexistence evidence. The Author SDK receipt transaction remains a later separately reviewed construction step.
