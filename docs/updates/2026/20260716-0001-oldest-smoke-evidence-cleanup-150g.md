# 20260716-0001 Oldest Smoke Evidence Cleanup — 150 GiB

- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: evidence/retention/storage/cleanup/smoke
- Source: user authorized cleaning approximately 150 GiB of past duplicate data from E: while other threads might still be testing.

## Scope

- Delete only planned historical `DTMAPI-evidence` subdirectories nested inside old GAME-SMOKE run directories.
- Preserve outer run directories, run-level logs and results, explicit referenced artifacts, report ZIPs, Process-Dumps, live runtime evidence, and the current Batch 4 worktree.
- Reuse and re-hash the 62 canonical runtime-evidence identities retained by the first cleanup.
- Keep every run newer than the selected historical boundary outside the deletion plan.

## Result

- Deleted 284 planned duplicate runtime-evidence subtrees from run `20260610-163928` through run `20260705-001613`.
- Deleted logical size: 150.743 GiB across 490,390 files and 204,854 directories.
- E: free space changed from 94.962 GiB immediately before deletion to 246.605 GiB afterward, an actual filesystem gain of 151.643 GiB.
- Exact selected runs, counts, hashes, exclusions, preservation sizes, and before/after checks are in `docs/debug/evidence-retention-cleanups/20260716-oldest-150g.json`.

## Safety Boundary

- Every target resolved exactly as one historical run directory followed by the leaf `DTMAPI-evidence`; no outer run directory was a deletion target.
- All targets and descendants had zero reparse points before recursive deletion.
- Twenty-six runs containing explicit nested artifact references were excluded.
- All 62 canonical identities containing 194 files and about 78.7 MiB were hashed successfully before and after deletion.
- All 284 selected nested targets were absent afterward and all 284 outer run directories remained present.
- The allowlist SHA-256 and its existing-run/existing-artifact sets were identical before and after deletion.
- GAME-SMOKE ZIP files remained 6.227 GiB and Process-Dumps remained 27.423 GiB; neither class was targeted.
- The other active thread's Batch 4 G3 source and test changes were not read as deletion inputs, modified, staged, or committed.

## Changed Files

- `docs/debug/evidence-retention-cleanups/20260716-oldest-150g.json`
- `docs/debug/protocols/evidence-retention.md`
- `docs/updates/2026/20260716-0001-oldest-smoke-evidence-cleanup-150g.md`
- `docs/updates/INDEX-2026-07.md`

Ignored local evidence changes removed only the 284 nested paths listed in the audit manifest.

## Validation

- Pre-delete plan measured 399.274 GiB of eligible historical duplicate snapshots and selected the oldest 150.743 GiB.
- Pre-delete and post-delete canonical hashes matched the retained manifest.
- Referenced existence baseline remained 706 of 719 listed run paths and 223 of 224 literal artifact paths; all pre-existing missing paths remained the same set.
- Runtime validation was not required: no game install, launch, Smoke, live runtime mutation, ZIP deletion, process-dump deletion, or live evidence deletion was performed.
- Document governance and evidence-retention allowlist checks are the final tracked-record gates.

## Rollback Notes

Git cannot reconstruct ignored duplicate snapshot placement. The canonical referenced identities and all run-level audit records remain available, but recreating every removed duplicate would require copying canonical/live evidence back into each historical run and is intentionally not part of normal rollback.

## Follow-Up

- Keep the corrected per-Smoke retention baseline and do not resume full-tree runtime-evidence collection.
- Bound stale Unity crash and historical-log copying before another large evidence cleanup.
- Treat future Process-Dump or root-ZIP cleanup as separate, explicitly authorized retention decisions.
