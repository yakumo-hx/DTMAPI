# 20260721-0002: Full-Suite Retry Proportionality

## Metadata

- Update ID: `20260721-0002`
- Date: `2026-07-21`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `mitigated`
- Area: governance/assurance/testing/release/retry
- Source: User request to correct the repeated from-start complete Release retries observed during Batch 6 final integration.
- Related Update: [Assurance Governance Proportionality](20260721-0001-assurance-governance-proportionality.md)
- Observed implementation: [Batch 6 AutoFishing Advanced Pilot](20260720-0008-batch6-autofishing-advanced-pilot.md)

## Summary

This docs-only correction distinguishes diagnostic failure recovery from the single formal complete-suite acceptance run. A failed complete suite now leads to a focused failed-gate check and one pass through the unreached diagnostic tail before the frozen final candidate is tested once from the beginning.

The correction does not add a checkpoint ledger, cached-pass authority, receipt schema, test runner, Runtime change, package change, or new release gate.

## User-Visible Impact

- No player or Mod behavior changes.
- Future long integration work should expose downstream failures without repeatedly rebuilding and retesting every already-passing prefix after each local repair.

## Decisions

- A source repair invalidates any prior claim that the new HEAD has a complete-suite PASS, but it does not require an immediate complete-suite restart.
- First pass the failed gate through its existing focused entry point.
- Then run the still-unreached gates once as a non-acceptance diagnostic tail where they can be called safely, and batch adjacent corrections.
- Run one clean complete suite from the start only after the diagnostic tail is green and the final candidate is frozen.
- Re-run earlier gates sooner only when the repair can affect them, runner or provenance integrity is uncertain, or no safe focused/tail route exists.
- Partial and tail runs cannot be combined and reported as a formal complete-suite PASS.
- Use existing child scripts or a lightweight non-authoritative phase selector; do not create checkpoint receipts, cached-pass authorities, or a parallel assurance system.

## Changed Files

- `AGENTS.md`
- `docs/workflows/document-governance.md`
- `docs/workflows/codex-api-rebuild.md`
- this Update and `docs/updates/INDEX-2026-07.md`

## Validation

- `pwsh -NoProfile -File tools/scripts/check-doc-governance.ps1`: passed.
- Scoped `git diff --check` for the five owned documentation paths: passed, apart from existing Git line-ending notices where applicable.
- Complete Release suite and game smoke: intentionally not run because this change modifies workflow documentation only.

## Evidence

- The current complete-suite entry point is a linear fail-fast driver with no stage-resume parameter: [`tools/scripts/test.ps1`](../../../tools/scripts/test.ps1).
- The existing Batch 6 Update already requires one complete Release suite on the final integrated commit and independently permits unchanged-package runtime levels to remain reusable.

## Related Records

- Debug: none; no runtime symptom or runtime evidence changed.
- Hook map: none.
- Smoke matrix: none; no game run occurred.
- API matrix: none; no public contract changed.

## Rollback Notes

Revert only the five documentation paths listed above. Do not change Batch 6 source, packages, runtime evidence, user files, game state, or Workshop state.

## Follow-Up

Apply this recovery sequence after the next complete-suite failure. Add a lightweight diagnostic phase selector only if direct child-script invocation remains error-prone; it must not become another receipt or acceptance system.
