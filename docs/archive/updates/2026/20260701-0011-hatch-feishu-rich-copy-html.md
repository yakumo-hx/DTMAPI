# 20260701-0011 Hatch Feishu Rich Copy HTML

## Source

User switched the `新增养殖动物` Feishu author-document workflow from the Feishu OpenAPI/token route to a token-free browser rich-text copy/paste route because the token application path is heavier than manual formatting.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `author-docs/content-packs/new-farm-animal-draft/README.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-rich-copy.html`
- `docs/updates/2026/20260701-0011-hatch-feishu-rich-copy-html.md`
- `docs/updates/INDEX.md`

## Summary

- Extended the Hatch-specific document generator with a Plan A rich-text HTML copy page:
  - Output path: `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-rich-copy.html`.
  - The page embeds the five generated PNG action-frame atlases as data URIs, so it can be copied as one rich document fragment.
  - The page renders Feishu-like headings, bullets, tables, JSON code cards, line numbers via CSS, and shallow-yellow replacement highlights.
  - The page includes a `复制富文本` button using `ClipboardItem` with `text/html` and `text/plain`, plus a fallback that selects the document body for manual `Ctrl+C`.
- Updated the local draft README to make this token-free rich-copy path the first documented Feishu workflow.
- Kept the existing OpenAPI block payload and upload code available for later use, but the local rich-copy HTML now supports the immediate browser paste path.

## Validation

Passed:

- `python -m py_compile author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- Generated rich-copy HTML structure check:
  - `ClipboardItem`: present
  - embedded PNG data images: `5`
  - code cards: `11`
  - tables: `6`
  - replacement highlight marks: `110`
  - Hatch validation errors: `0`
  - JSON count: `11`
  - PNG count: `44`

Not run:

- Actual Feishu paste through browser automation. The Chrome automation surface blocked direct `file://` navigation to the generated local HTML by policy, so this pass stops at the verified local rich-copy artifact.
- Game/runtime smoke; this is author-document tooling only and does not touch runtime, hooks, game files, or local `MODS`.

## Rollback

Remove the rich-copy HTML rendering functions and generated HTML output from the generator, remove the README rich-copy workflow, delete this update record, and remove this row from `docs/updates/INDEX.md`.

## Follow-Up

- Open `generated/hatch-feishu-rich-copy.html` manually in Chrome, click `复制富文本`, then paste into the Feishu debug page.
- If Chrome's direct local-file clipboard behavior is inconsistent, serve the generated folder over a temporary local HTTP server and open the same HTML through `http://127.0.0.1`.
