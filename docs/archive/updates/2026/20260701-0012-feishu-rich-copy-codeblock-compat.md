# 20260701-0012 Feishu Rich Copy Codeblock Compatibility

## Source

User reported that the token-free rich-copy HTML pasted into Feishu with broken code-block behavior:

- Code content collapsed into a single line.
- Replacement highlights were not preserved.
- Code blocks appeared as `Plain Text` instead of JSON, requiring manual language adjustment.

## Changed Files

- `author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `author-docs/content-packs/new-farm-animal-draft/README.md`
- `author-docs/content-packs/new-farm-animal-draft/generated/hatch-feishu-rich-copy.html`
- `docs/updates/2026/20260701-0012-feishu-rich-copy-codeblock-compat.md`
- `docs/updates/INDEX.md`

## Summary

- Reworked generated code blocks away from generic webpage markup:
  - Removed per-line `<span class="code-line">` wrappers that Feishu collapsed during paste.
  - Removed `<mark>` replacement highlights that Feishu stripped in code blocks.
  - Generated code blocks now use Feishu-style copied HTML:
    - `<pre style="white-space:pre;">`
    - `<code class="language-JSON" data-lark-language="JSON" data-wrap="false">`
    - one inner `<div>` containing real newline-separated code text
    - inline highlight spans using `background-color:rgba(255,246,122,0.8)`
- Wrapped the clipboard HTML fragment with `data-lark-html-role="root"` and `data-docx-has-block-data="false"` to better match Feishu copied HTML.
- Updated README troubleshooting notes for stale pages and old broken copy behavior.
- User manually pasted the regenerated rich-copy HTML into Feishu and confirmed the revised route works.

## Validation

Passed:

- `python -m py_compile author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- `python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py`
- Generated rich-copy HTML structure check:
  - `class="language-JSON"`: `11`
  - `data-lark-language="JSON"`: `11`
  - Feishu-style code blocks: `11`
  - inline yellow highlight spans: `110`
  - old `class="code-line"` wrappers: `0`
  - old `<mark>` tags: `0`
  - `data-lark-html-role="root"`: present
  - embedded PNG data images: `5`
  - Hatch validation errors: `0`
- User manual Feishu paste confirmation from screenshot feedback:
  - `Content/DTMAPI/custom-animals.json` pasted as a Feishu code block with language set to `JSON`.
  - Code line breaks and indentation were preserved.
  - Inline yellow replacement highlights were preserved inside the code block.

Not run:

- Game/runtime smoke; this is author-document tooling only and does not touch runtime, hooks, game files, or local `MODS`.

## Rollback

Revert `render_code_html`, `highlighted_lark_code_html`, the clipboard wrapper change, the README compatibility note, this update record, and this row in `docs/updates/INDEX.md`.

## Follow-Up

- Continue using the regenerated `generated/hatch-feishu-rich-copy.html` route for token-free Feishu author-document paste work.
