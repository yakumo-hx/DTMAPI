# 20260701-0014 Feishu Rich-Copy Table Widths

## Source

User reported that table widths looked correct in the local/browser page but failed after rich-text paste into Feishu. In the Feishu debug page, a manually widened table stayed wide, while the rich-copied table below it became narrow and tall.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-rich-copy.html`
- `author-docs/content-packs/new-farm-animal-draft/generated/feishu-blocks.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/validation-report.json`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-doc.preview.md`
- `docs/updates/2026/20260701-0014-feishu-rich-copy-table-widths.md`
- `docs/updates/INDEX.md`

## Clipboard Evidence

Using the Feishu table handle menu, copied both visible tables from the current Chrome Feishu debug page and inspected the Windows clipboard HTML:

- Manually widened table:
  - HTML wrapper: `byte-sheet-html-origin`
  - copied column widths: `<colgroup><col width="255"><col width="648"></colgroup>`
- Narrow rich-pasted table:
  - HTML wrapper: `byte-sheet-html-origin`
  - copied column widths: `<colgroup><col width="105"><col width="105"></colgroup>`

Conclusion: Feishu preserves table widths through its sheet-flavored clipboard HTML, not through a plain HTML table's CSS width. The previous generator emitted ordinary `<table class="doc-table">` markup, so Feishu imported it with default narrow columns.

Saved local debugging artifacts under:

- `author-docs/content-packs/new-farm-animal-draft/generated/table-clipboard-debug/`

## Summary

- Added optional `columnWidths` metadata to generated table model blocks.
- Assigned explicit widths to all five tutorial tables:
  - JSON relationship table: `255 / 648`, matching the manually widened Feishu table.
  - Animation frame table: compact wide action columns.
  - Image requirements table: wider multi-column layout to avoid tall wrapping.
  - File tree and key-ID reference tables: three-column 900px-style layouts.
- Changed rich-copy table rendering from normal HTML tables to Feishu-compatible sheet markup:
  - `byte-sheet-html-origin`
  - `<table style="border-collapse: collapse;">`
  - `<colgroup><col width="..."></colgroup>`
  - inline `td` wrapping styles.
- Updated rich-copy CSS so the local preview also respects fixed column widths and scrolls horizontally when a table is intentionally wide.

## Validation

Passed:

- `python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- Static generated HTML check:
  - `byte-sheet-html-origin` count: `5`
  - generated Feishu sheet table count: `5`
  - generated colgroups:
    - `<colgroup><col width="255"><col width="648"></colgroup>`
    - `<colgroup><col width="95"><col width="125"><col width="80"><col width="80"><col width="80"><col width="80"><col width="80"><col width="100"></colgroup>`
    - `<colgroup><col width="80"><col width="170"><col width="260"><col width="180"><col width="205"><col width="205"><col width="160"></colgroup>`
    - `<colgroup><col width="120"><col width="360"><col width="420"></colgroup>`
    - `<colgroup><col width="170"><col width="290"><col width="440"></colgroup>`
  - default narrow `<col width="105"><col width="105">`: absent.

Not run:

- Full regenerated-document paste into Feishu. This update uses clipboard evidence from existing Feishu tables plus static generated HTML verification; manual paste remains the next check before replacing the official page.
- Game/runtime smoke. This is author-document generation only.

## Rollback

- Revert the `table_block` `columnWidths` metadata and rich-copy `byte-sheet-html-origin` renderer.
- Regenerate `author-docs/content-packs/new-farm-animal-draft/generated/`.
- Remove this update record and its index row.

## Follow-Up

- Paste the regenerated rich-copy page into a Feishu debug page and verify that the first JSON relationship table keeps the `255 / 648` visual width without manual dragging.
