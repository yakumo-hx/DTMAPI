# 20260712-0005 — Smoke Evidence Retention Bounds

## Metadata

- Update ID: `20260712-0005`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: user approved the first two remediation steps after the E-drive audit identified repeated full runtime-evidence snapshots.

## Scope

- stop routine GAME-SMOKE from copying the complete historical runtime evidence tree;
- select only runtime `case/timestamp` directories created or updated since the current Smoke began;
- reject over-limit batches before copying any selected file;
- generate and validate a machine-readable evidence retention allowlist from project Markdown;
- document the later cleanup boundary without deleting current evidence.

## Changed Files

- `tools/scripts/common.ps1`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/test-runtime-evidence-retention.ps1`
- `tools/scripts/build-evidence-retention-allowlist.ps1`
- `tools/scripts/test.ps1`
- `tools/scripts/README.md`
- `docs/debug/evidence-retention-allowlist.json`
- `docs/debug/protocols/evidence-retention.md`
- `docs/debug/protocols/README.md`
- `docs/debug/INDEX.md`
- `docs/reviews/code/2026/20260712-0002-smoke-evidence-retention-boundary.md`
- `docs/updates/2026/20260712-0005-smoke-evidence-retention-bounds.md`
- `docs/updates/INDEX-2026-07.md`

## Validation

- Synthetic current-run selection and fail-closed limit tests passed, including repeated-copy nesting protection and source assertions that routine GAME-SMOKE no longer requests the full tree.
- Generated allowlist check passed: 368 source Markdown files, 687 GAME-SMOKE run identities, 217 specific Smoke artifacts, 62 canonical runtime-evidence identities, and 6 process-dump identities.
- PowerShell parser validation passed for every changed/new script. Windows PowerShell 5.1 also executed both the runtime-evidence tests and allowlist check successfully; the generator uses ASCII source, explicit Git UTF-8 output, ordinal sorting, and host-independent compressed JSON.
- Standalone governance passed: `Document governance: OK (4118 checks)`.
- Final full `tools/scripts/test.ps1 -Configuration Release` passed in 101.7 seconds: all builds completed with 0 warnings and 0 errors, `DTMAPI.UnitTests: OK`, runtime-evidence retention tests passed, allowlist validation passed, and integrated governance returned success. An earlier wrapper attempt was terminated only by its 124-second tool timeout and produced no failed assertion; the completed rerun is the final result.
- `git diff --check` passed with line-ending normalization warnings only.
- No game install, game launch, runtime lock, or historical evidence deletion occurred.

## Rollback Notes

- Revert this Update to restore routine full-tree Smoke collection. Existing evidence is not modified by rollback because this change does not delete it.

## Follow-Up

- Run one future locked game Smoke to validate the real `DTMAPI-evidence-selection.txt` payload and confirm the new outer evidence directory remains bounded.
- Design a dry-run cleanup that consumes the allowlist, hashes canonical runtime identities, reports reclaimable bytes, and requires explicit deletion approval.
