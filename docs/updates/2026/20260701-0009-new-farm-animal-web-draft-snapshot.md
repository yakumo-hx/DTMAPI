# 20260701-0009 New Farm Animal Web Draft Snapshot

## Status

docs-snapshot-created

## Source Request

User asked to pause Feishu editing, first create a local folder draft, and use the current web document as-is.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/README.md`
- `author-docs/content-packs/new-farm-animal-draft/source-feishu-current.html`
- `author-docs/content-packs/new-farm-animal-draft/source-feishu-current.txt`
- `author-docs/content-packs/new-farm-animal-draft/source-visible.png`
- `author-docs/content-packs/new-farm-animal-draft/draft.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260701-0009-new-farm-animal-web-draft-snapshot.md`

## Summary

Captured the current Feishu web document titled `新增养殖动物` into a local draft folder without further editing the web page.

The folder keeps:

- a rich HTML snapshot for Feishu formatting and highlight reference;
- a plain text snapshot for search and rewrite work;
- an editable `draft.md` initially matching the copied plain text;
- a visible-page screenshot for quick visual reference;
- a README documenting source URL, snapshot date, and intended next draft direction.

The Feishu internal clipboard record was intentionally not retained because the HTML/text snapshots are sufficient for drafting and the internal record contains platform block metadata that is not needed for author documentation.

## Validation

- Confirmed the new draft folder exists.
- Confirmed all five snapshot/draft files exist.
- Confirmed `draft.md` text length matches the copied Feishu plain text length.

Runtime, build, game smoke, and JSON validation were not run because this update only creates an author-document draft snapshot.

## Evidence

- Local folder listing checked for `author-docs/content-packs/new-farm-animal-draft`.
- `README.md` was read back after creation.
- `draft.md` character length was read back as `13300`.

## Rollback

Delete `author-docs/content-packs/new-farm-animal-draft/` and remove this update record/index row.

## Follow-Up

Use `draft.md` as the local working copy for the planned rewrite:

1. make the main tutorial use official-style annotated examples;
2. keep highlights inside code examples only;
3. move Hatch real-package material into the reference section;
4. centralize animation frame and PNG requirements in section three.
