# DTMAPI Update Records

This folder is the durable implementation lifecycle ledger for DTMAPI. Use one Update record to trace a change from scope through files, validation, evidence, rollback, and final state.

## Rule

Every non-trivial update must add one record under this folder and link it from the matching monthly ledger `INDEX-YYYY-MM.md`. The root `INDEX.md` and year `INDEX-YYYY.md` files are navigation only.

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

## Required Fields For New Records

Each record should include:

- Update ID
- Date
- Lifecycle Status: proposed / in-progress / implemented / verified / blocked / reverted / superseded
- Validation Level: not-run / docs / source / unit / runtime / player; comma-separated when needed
- Runtime Validation: not-required / not-run / passed / failed / blocked / partial
- Related Issue State: none / open / monitoring / mitigated / verified / closed / deferred
- Source request and relevant review
- Summary
- User-visible impact
- Changed files
- Validation
- Evidence links
- Related debug issues, hook-map rows, smoke-matrix rows, or API matrix rows
- Rollback notes
- Follow-up

Historical records and their slash-separated status strings remain unchanged. New records follow `docs/workflows/document-governance.md`.

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
- Lifecycle Status:
- Validation Level:
- Runtime Validation:
- Related Issue State:
- Source:

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
