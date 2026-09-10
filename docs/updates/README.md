# DTMAPI Update Records

Each non-trivial implementation owns one `YYYY/YYYYMMDD-NNNN-short-slug.md` and one monthly row, maintained through corrections and acceptance. Pure discussion creates no file; typo-only edits may skip a record. Detailed lifecycle exceptions are in [governance](../workflows/document-governance.md).

## Start and finish

Copy the template below, assign an unused daily ID, and record scope plus acceptance before implementation. Keep required headings exactly as shown; write `not-run` for missing tests. Logs/hashes live in their evidence owner and are linked, not pasted repeatedly.

Create the monthly row with the sync script (area/summary required only for a new row), then omit them when updating status:

```powershell
tools/scripts/sync-update-ledger.ps1 -UpdatePath docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md -Area "product/domain" -Summary "Concrete change."
tools/scripts/sync-update-ledger.ps1 -UpdatePath docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md
tools/scripts/check-doc-governance.ps1
```

The script derives ID/date and four status columns from the Update, preserves the existing human summary/area and other rows, and never changes root/year indexes. `-Check` verifies the projection without writing. Do not create parallel status lists.

## Template

```md
# YYYYMMDD-NNNN: Short title

## Metadata

- Update ID: `YYYYMMDD-NNNN`
- Date: `YYYY-MM-DD`
- Lifecycle Status: `in-progress`
- Validation Level: `not-run`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: Request and relevant Review link, or why no new Review is needed.

## Summary

Expected user-visible change and scope.

## Changed Files

Affected files, grouped by purpose.

## Validation

Required checks; actual passed/failed/not-run results and remaining gaps.

## Evidence

Relevant logs/reports/package or smoke links. No game run means no smoke row.

## Rollback Notes

How to reverse this change and any data limitations.

## Follow-Up

Remaining work, or none.
```

Allowed field values and historical/monthly policy are in [governance](../workflows/document-governance.md#metadata-and-history). A verified Update does not automatically close an Issue or prove publication/user acceptance.
