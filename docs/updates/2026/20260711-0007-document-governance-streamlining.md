# 20260711-0007 — Document governance streamlining

## Metadata

- Update ID: `20260711-0007`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: user requested a clean third-level branch and implementation of six documentation-governance optimizations.

## Scope

- reduce documentation write amplification;
- separate Review, historical Goal, Update, Debug, and specialized matrix lifecycles;
- replace the stale current-state snapshot with a stable current-truth router;
- compact oversized root indexes while preserving their full history;
- normalize status dimensions for new records;
- add an automated consistency checker.

## Current Decisions

- Preserve large historical ledgers as cutoff snapshots instead of deleting their evidence.
- Keep root index files short and navigational.
- Treat one Update record as the implementation lifecycle owner.
- Update Review, Debug, API, Hook, and Smoke records only when facts owned by those systems change.
- Apply the normalized status schema prospectively; do not rewrite hundreds of historical records.

## Changed Files

- `AGENTS.md`, `PROJECT.md`
- `docs/workflows/document-governance.md`
- `docs/onboarding/current-state.md`
- `docs/reviews/README.md`
- `docs/updates/README.md`, `docs/updates/INDEX.md`, `docs/updates/INDEX-2026.md`
- `docs/debug/INDEX.md`, `docs/debug/INDEX-history-through-20260711.md`
- `docs/debug/issues/README.md`
- `docs/debug/regressions/smoke-matrix.md`, `docs/debug/regressions/smoke-matrix-history-through-20260711.md`
- `docs/hook-map/README.md`, `docs/hook-map/README-history-through-20260711.md`
- `tools/scripts/check-doc-governance.ps1`, `tools/scripts/test.ps1`, `tools/scripts/README.md`

## Validation

- PowerShell parser checks passed for `check-doc-governance.ps1` and `test.ps1`.
- `tools/scripts/check-doc-governance.ps1`: `Document governance: OK (1747 checks)`.
- The checker found 16 pre-existing duplicate rows in the 2026 Update index; the duplicate rows were removed and the rerun passed.
- Full `tools/scripts/test.ps1 -Configuration Release`: all builds completed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`; the integrated governance check passed.
- Checked 448 Markdown links across the 17 changed/new Markdown files; 0 broken links.
- `git diff --check`: passed with line-ending normalization warnings only.
- Root-size reductions while preserving full cutoff snapshots:
  - Update Index: 180,532 bytes to 613 bytes;
  - Debug Index: 105,300 bytes to 1,527 bytes;
  - Hook Map root: 277,264 bytes to 1,060 bytes;
  - Smoke Matrix root: 494,671 bytes to 1,252 bytes.
- No game install, runtime lock, game launch, or smoke run occurred; runtime validation was not required.

## Evidence

- Branch parent: `codex/phase822-input-root-lifetime-20260708` at `4cdd49b`.
- Working branch: `codex/phase822-doc-governance-20260711`.

## Rollback Notes

- Revert the final documentation-governance commit. Historical cutoff snapshots will make the compacted indexes independently recoverable.

## Follow-Up

- Add new Hook domains as focused maps instead of expanding the root router.
- Keep historical records unchanged; use normalized Update metadata prospectively.
- If annual indexes later become too large, split them by year/month without reintroducing duplicate root rows.
