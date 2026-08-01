# 20260712-0007 — Oldest Smoke Evidence Cleanup

## Metadata

- Update ID: `20260712-0007`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: user authorized cleaning approximately the oldest 50 GiB before the next large Update.

## Scope

- preserve one hashed canonical copy of every runtime-evidence identity referenced by project Markdown;
- delete only the oldest duplicate `DTMAPI-evidence` subdirectories inside GAME-SMOKE runs;
- preserve outer run directories, run-level logs/results, explicit allowlisted artifacts, ZIP files, Process-Dumps, and live game evidence;
- record the exact selected runs, logical sizes, hashes, exclusions, and post-delete checks in a tracked manifest.

## Result

- Materialized 62 canonical runtime identities containing 194 files and about 78.7 MiB under ignored local path `docs/debug/evidence/RETAINED-RUNTIME`.
- Deleted 418 duplicate runtime-evidence subtrees, from the oldest eligible 2026-05-31 run through the last selected 2026-06-10 run.
- Deleted logical size: 50.252 GiB across 173,096 files and 74,956 directories.
- E: free space changed from 47.939 GiB immediately before deletion to 98.463 GiB afterward: actual filesystem gain 50.524 GiB.
- Compared with the 48.01 GiB observed before canonical retention was materialized, net free-space gain was about 50.453 GiB.
- Exact audit manifest: `docs/debug/evidence-retention-cleanups/20260712-oldest-50g.json`.

## Safety Boundary

- All targets resolved exactly as `docs/debug/evidence/GAME-SMOKE/<run>/DTMAPI-evidence`.
- No target was a reparse point or contained a reparse point.
- Twenty-two runs containing explicit allowlisted nested artifacts were excluded.
- Every canonical identity was hashed before deletion and re-hashed successfully afterward.
- Every selected outer run directory remained present; no selected nested target remained.
- The pre-existing allowlist availability baseline remained unchanged: 676 referenced runs and 215 referenced concrete artifacts existed both before and after deletion.
- Latest accepted Y-console evidence, GAME-SMOKE ZIP files, Process-Dumps, and the live game evidence tree were not deleted.

## Changed Files

- `docs/debug/evidence-retention-cleanups/20260712-oldest-50g.json`
- `docs/debug/protocols/evidence-retention.md`
- `docs/updates/2026/20260712-0007-oldest-smoke-evidence-cleanup.md`
- `docs/updates/INDEX-2026-07.md`

Local ignored evidence changes:

- added `docs/debug/evidence/RETAINED-RUNTIME` canonical copies;
- removed only the 418 nested duplicate paths listed in the audit manifest.

## Validation

- Post-delete allowlist check passed: 370 source files, 689 GAME-SMOKE runs, and 62 runtime identities.
- Post-delete document governance passed before recording (`4142` checks) and after the manifest/Update/index links were added: `Document governance: OK (4165 checks)`.
- Runtime lock remained free and no `DolocTown.exe` process was running during planning, canonical retention, deletion, or verification.
- No game install, launch, Smoke run, ZIP deletion, process-dump deletion, or live runtime-evidence deletion occurred.

## Rollback Notes

- Git cannot restore ignored duplicate evidence copies. The cleanup intentionally preserves all run-level records plus hashed canonical referenced identities; recreating every removed duplicate would require recopying the live/canonical data into each historical run and is neither necessary nor recommended.
- Revert the tracked manifest/Update only if the audit record itself is incorrect; do not treat a documentation revert as data restoration.

## Follow-Up

- After the next large Update and real Smoke, compare its evidence size and selection summary with the 25 MiB Y-console baseline.
- Consider a separate bounded change that avoids copying stale Unity crash directories and ten historical logs into every successful Smoke.
- Any later cleanup must generate a new dry-run manifest and obtain explicit deletion authorization.
