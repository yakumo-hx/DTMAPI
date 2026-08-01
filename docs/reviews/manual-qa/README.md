# Manual QA Reviews

Store durable manual-test review records here when user feedback needs root-cause or code-path analysis before implementation.

Phase summary: [2026-07-06 Manual QA Phase Summary](2026/20260706-0001-manual-qa-phase-summary.md).

Naming:

```text
YYYY/YYYYMMDD-NNNN-short-slug.md
```

The `NNNN` counter is local to the date when practical. Keep slugs short and focused on the reviewed scope, for example:

```text
2026/20260605-0001-animal-ui-flicker-review.md
```

Manual QA review records preserve pre-implementation reasoning. When implementation begins, create or update the corresponding `docs/updates/YYYY/...` record with `proposed` or `in-progress` status; completion evidence belongs in that update and the relevant debug records.
