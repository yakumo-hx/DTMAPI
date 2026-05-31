# DTMAPI Update Records

This folder is the durable update ledger for DTMAPI. Use it for changes that future developers or Codex sessions must be able to trace back to a goal, files, validation, and evidence.

## Rule

Every non-trivial update must add one record under this folder and link it from `INDEX.md`.

Create a record when a change touches:

- runtime, bootstrap, GameBridge, config menu, input, mod loading, Workshop loading, installer, packaging, public API, migrated mods, docs that change project direction, or project assets used by runtime/package output;
- any bug fix or regression investigation;
- any user-visible behavior, mod naming convention, or install layout.

Small typo-only documentation edits may skip a record if they do not change project state.

## File Naming

Use:

```text
docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md
```

Example:

```text
docs/updates/2026/20260530-0001-main-menu-config-entry.md
```

`NNNN` is per-day sequence order. Keep slugs short and factual.

## Required Fields

Each record should include:

- Update ID
- Date
- Status: proposed / implemented / verified / blocked / reverted
- Source request or goal
- Summary
- User-visible impact
- Changed files
- Validation
- Evidence links
- Related debug issues, hook-map rows, smoke-matrix rows, or API matrix rows
- Rollback notes
- Follow-up

## Evidence Policy

Do not paste large logs into update records. Link to:

- `docs/debug/evidence/...`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- local package/report paths when relevant

If validation was not run, say so explicitly.

## Template

```md
# YYYYMMDD-NNNN: Short Title

## Metadata

- Update ID:
- Date:
- Status:
- Source:
- Owner:

## Summary

- 

## User-Visible Impact

- 

## Changed Files

- 

## Validation

- 

## Evidence

- 

## Related Records

- Debug:
- Hook map:
- Smoke matrix:
- API matrix:

## Rollback Notes

- 

## Follow-Up

- 
```
