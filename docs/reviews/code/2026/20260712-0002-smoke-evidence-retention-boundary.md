# 20260712-0002 Smoke Evidence Retention Boundary

Status: recorded
Date: 2026-07-12
Scope: root-cause and retention-boundary review for E-drive exhaustion caused by GAME-SMOKE evidence collection
Related Update: `docs/updates/2026/20260712-0005-smoke-evidence-retention-bounds.md`

## Source Request

The user reported that E: had only about 48 GiB free, approved fixing future Smoke collection, and requested a whitelist of evidence referenced by project documentation. Historical evidence deletion was not authorized.

## Observed Facts

- E: used about 780.37 GiB of 828.41 GiB.
- `E:/Python_project/DTMAPI` occupied about 497 GiB even though Git objects were only about 43 MiB and `references/` about 638 MiB.
- `docs/debug/evidence/GAME-SMOKE` held about 495 GiB across 1,093 run directories.
- Excluding nested `DTMAPI-evidence` copies reduced that tree to about 38.5 GiB, so repeated runtime-evidence snapshots accounted for about 456.5 GiB.
- `Process-Dumps` contributed about 27.42 GiB and root report ZIP files about 6.23 GiB; those are separate retention decisions.
- Current live runtime evidence under `D:/steam/steamapps/common/Doloc Town/DTMAPI/evidence` was about 1.66 GiB.
- `run-game-smoke.ps1` passed `-IncludeRuntimeEvidence` for every collection, and `collect-logs.ps1` recursively copied the entire live evidence tree into each new run. `GAME-SMOKE/20260707-153322` was one representative multi-GiB snapshot.
- `docs/debug/evidence/` is Git-ignored, so ordinary Git status did not expose this growth.

## Decision

1. Keep the explicit full-tree collector switch for exceptional manual support packages, but remove it from routine GAME-SMOKE.
2. Routine GAME-SMOKE passes its start time and collects only immediate `case/timestamp` directories created or updated since that boundary.
3. Preflight the whole selected window before copying. Reject the batch without partial files when any directory, file-count, total-byte, or per-file cap is exceeded.
4. Generate a machine-readable whitelist from versionable Markdown references. Record runtime evidence by canonical `DTMAPI-evidence/<case>/<timestamp>/...` identity rather than treating every duplicated outer snapshot as independently valuable.
5. Do not delete historical evidence in this Update. A later cleanup must preserve referenced run roots/artifacts and materialize one canonical copy of every referenced runtime identity first.

## Acceptance Gates

- synthetic evidence created before the boundary is not copied;
- synthetic evidence created after the boundary is copied with its `case/timestamp` layout;
- over-limit evidence produces a summary and no partial destination payload;
- routine GAME-SMOKE no longer passes `-IncludeRuntimeEvidence`;
- the allowlist is deterministic and `-Check` detects documentation drift;
- all PowerShell scripts parse in Windows PowerShell-compatible syntax;
- repository Release/unit/governance paths pass;
- no evidence deletion or game launch occurs in this task.

## Documentation Boundary

Implementation, changed files, validation, and final status belong to Update `20260712-0005`. This Review retains the storage diagnosis and safety decision only.
