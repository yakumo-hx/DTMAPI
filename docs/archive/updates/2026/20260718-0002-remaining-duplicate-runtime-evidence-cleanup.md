# Update 20260718-0002: Remaining Duplicate Runtime Evidence Cleanup

- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Date: 2026-07-18
- Area: evidence/retention/storage/cleanup/smoke
- Source Request: user authorized deleting previously uncleaned duplicate and useless E-drive records, primarily repeated `DTMAPI-evidence` snapshots
- Related Review: [Smoke Evidence Retention Boundary](../../reviews/code/2026/20260712-0002-smoke-evidence-retention-boundary.md)

## Scope

Delete only remaining pre-retention-fix `docs/debug/evidence/GAME-SMOKE/<run>/DTMAPI-evidence` duplicates. Preserve outer Smoke runs and run-level results, every explicit nested artifact reference, all post-fix bounded evidence, the 62-identity `RETAINED-RUNTIME` canonical set, report ZIPs, Process-Dumps, live Runtime evidence, package-audit candidates, and the source worktree.

The dry-run inventory found 152 eligible historical nested trees totaling 248.531 GiB, 500,804 files, and 248,622 descendant directories. Twenty-six runs containing explicit nested artifact references were excluded, preserving another 6.453 GiB. No candidate or descendant is a reparse point.

## Result

- Deleted all 152 planned nested `DTMAPI-evidence` trees: 266,858,358,691 logical bytes (`248.531 GiB`), 500,804 files, and 248,622 descendant directories.
- E-drive free space increased by 267,793,129,472 bytes (`249.402 GiB`), reaching about 452 GiB free immediately after cleanup.
- All 152 outer GAME-SMOKE runs remain. The only 26 pre-cutoff nested trees still present are the explicitly referenced exclusions recorded in the manifest.
- The 62 canonical identities remained present under `RETAINED-RUNTIME`; their 182 unique files, 78,839,410 bytes, identity fingerprint, and file-manifest SHA-256 were unchanged.
- Existing referenced-run and literal-artifact sets remained `745/758` and `223/224`; the same 13 run references and one literal artifact were already missing before cleanup and did not change.
- The root report ZIP class remained unchanged at 28 files and 6,685,760,566 total bytes. Process-Dumps, post-cutoff evidence, live Runtime evidence, and ignored package-audit candidates were outside the deletion scope.

## Changed Files

- `tools/scripts/cleanup-duplicate-runtime-evidence.ps1`: preview-first, cutoff-bound cleanup with explicit-artifact exclusions, canonical hashing, reference fingerprints, path containment, reparse checks, post-delete validation, and a machine-readable manifest.
- `docs/debug/evidence-retention-cleanups/20260718-remaining-duplicate-runtime-evidence.json`: generated preview/final cleanup inventory and validation receipt.
- `docs/debug/protocols/evidence-retention.md`: executed-cleanup route after successful deletion.
- this Update and `docs/updates/INDEX-2026-07.md`.

## Validation

- Preview and apply independently found the same 152 targets and 26 explicit-artifact exclusions.
- The cleanup refused active Doloc Town or `run-game-smoke.ps1` processes, exact-path checked every target, rejected reparse points, and never selected an outer run.
- Pre/post canonical file-manifest SHA-256 stayed `b2f7f54ada22192bf80bbfd65fa6b69ac780a2913a3388a3d829665f93009cb3`.
- The final applied cleanup manifest SHA-256 is `C53AAB45EE1850090B3D68DF6083F0BA5FB7F7AAC68C123EB87F30F16D6B53A9`.
- The tracked allowlist SHA-256, referenced-run fingerprint, literal-artifact fingerprint, root ZIP count/bytes, and canonical identity fingerprint were identical before and after deletion.
- Post-delete inspection confirmed all selected leaves absent, all outer runs present, and exactly the 26 excluded pre-cutoff leaves remaining.
- `tools/scripts/build-evidence-retention-allowlist.ps1 -Check`, Windows PowerShell 5.1 syntax, document governance, and `git diff --check` are final tracked gates.
- Runtime/game validation was not required or run: no installation, launch, live Runtime mutation, ZIP deletion, Dump deletion, or runtime source change occurred.

## Rollback

Git cannot reconstruct ignored duplicate placement. Canonical referenced Runtime identities and outer run records remain available; recreating deleted duplicates would require intentionally copying canonical/live evidence back into every historical run and is not a normal rollback.

## Follow-Up

Keep post-cutoff delta collection intact. Treat root ZIPs, Process-Dumps, historical full logs, and ignored package-audit candidates as separate retention classes requiring their own inventory and authorization.
