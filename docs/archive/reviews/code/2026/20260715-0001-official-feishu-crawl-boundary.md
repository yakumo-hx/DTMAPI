# Official Feishu Workshop Documentation Crawl Boundary Review

Status: recorded

Date: 2026-07-15

Source request: locate the official Doloc Town Mod authoring guide, the existing crawl output, and its scripts; then reuse only the useful crawling lessons and produce a fresh browser-backed export because the earlier export may be incomplete.

## Scope and source boundaries

- Live canonical entry: <https://ka7deoo0opr.feishu.cn/wiki/ElmCwXsRCi7OPvkOh8JcvXK9nfc>
- The Chrome plugin opened the expected page and exposed the title `《多洛可小镇》创意工坊模组制作说明 - 飞书云文档`.
- A separate Playwright Chrome session loaded the same public page with HTTP 200 and rendered the root document. The live root currently says it was modified on June 12.
- Existing DTMAPI snapshot: `references/doloc-town/official-workshop-docs/feishu-crawl-20260517`.
- The user-remembered sibling `DLKsmapi` directory is named `E:\Python_project\DLK` on this machine.
- Legacy crawl scripts are reference-only. DTMAPI must not copy or imitate the old private runtime implementation. A new documentation crawler may retain generic crawling lessons, but it must be independently implemented in this repository.

## Existing artifact inventory

- DTMAPI official-doc reference root: 273 files, 57,187,320 bytes.
- Existing Feishu snapshot: 55 HTML files, 55 Markdown files, 55 text files, 98 PNG files, one manifest, two TSV indexes, and one crawl log.
- Legacy private crawl root: `E:\Python_project\DLK\research\创意工坊说明\飞书抓取`, with 233 files and 50,167,312 bytes.
- Legacy scripts inspected:
  - `E:\Python_project\DLK\scripts\tools\download_feishu_workshop_docs.py`
  - `E:\Python_project\DLK\scripts\tools\recrawl_feishu_workshop_docs_rendered.py`
  - `E:\Python_project\DLK\scripts\tools\audit_official_workshop_docs.py`

## Confirmed export weaknesses

### Static downloader

- It reads server-rendered HTML with `requests`, so browser-only/lazy content is not authoritative.
- It flattens visible text and loses headings, lists, code-block semantics, link placement, sheet placement, and image placement.
- Its sheet regex records a sheet token on every existing page, but it does not export sheet cells. The old `embedded_sheets.tsv` is therefore an embed/token inventory, not the missing ID/configuration tables.
- Page filenames depend on traversal order, which makes refresh diffs noisy.
- The manifest has no crawl timestamp, source snapshot comparison, content hashes, or explicit completeness gates.

### Rendered recrawler

- It repeatedly captures the full rendered document while scrolling and concatenates those captures with `--- 渲染片段分隔 ---`; overlapping chunks can duplicate most of a page.
- It treats strings recovered heuristically from compressed/protobuf-like sheet payloads as sheet text. That does not preserve cell order, rows, columns, formulas, or formatting and cannot be the canonical table export.
- It saves only a bounded number of viewport screenshots, not a complete structured document.
- Some stored paths are process-local absolute paths, and Chrome/dependency discovery is hard-coded.
- It has no deterministic per-file digest or previous-snapshot token/content comparison.

## Browser-backed proof for the replacement path

- The live root rendered its main body and exposed ten first-level official Wiki links plus the English Google document and feedback link.
- A representative `01 新增道具` page rendered headings, bullets, a JSON code block, two embedded spreadsheet canvases, one image, and official child links.
- Browser selection of each embedded spreadsheet followed by `Ctrl+A`, `Ctrl+C`, and clipboard read produced ordered TSV:
  - `类型 / 图片大小（像素） / 作图规范`
  - `名称 / 命名格式`
- This proves the browser can export the real used cell range without guessing strings from the wire payload. A screenshot is still required because cell colors and other visual authoring cues are meaningful.

## Replacement design

1. Crawl only canonical `ka7deoo0opr.feishu.cn/wiki/<token>` pages reachable from the official root.
2. Use the Wiki token as the stable page identity and filename prefix; keep display titles in metadata.
3. Scroll the actual Feishu content container to settle lazy blocks, then capture the complete rendered DOM once.
4. Export per page:
   - initial response HTML;
   - settled rendered document HTML;
   - normalized plain text;
   - structured Markdown;
   - external and internal link metadata;
   - downloaded document images;
   - each embedded sheet as TSV plus element screenshot;
   - selected raw JSON API responses needed to reproduce or diagnose missing embeds.
5. Generate a manifest with crawl time, tool version, HTTP/final URL, title, source-modified label, links, assets, sheets, byte counts, SHA-256 digests, failures, and remaining queue.
6. Generate a comparison with the 2026-05-17 snapshot: added, removed, retained, and failed tokens. Do not overwrite the old snapshot.
7. Keep live Feishu as canonical authority. The repository snapshot is dated reference evidence, not a replacement publication.

## Acceptance gates

- The root succeeds with the expected title and no login-only redirect.
- The crawl queue drains below the configured maximum with zero failed official pages, or every failure is recorded and the run remains incomplete.
- Every discovered embedded sheet has a non-empty TSV or an explicit extraction failure, and every sheet has a visual screenshot when renderable.
- Markdown contains no old scroll-fragment separator and does not duplicate the whole document by scroll position.
- All manifest paths are snapshot-relative and all material files have SHA-256 digests.
- The generated audit reports page/image/sheet/link counts and comparison against the prior 55-token snapshot.
- The new snapshot is stored in its own dated directory.
- Source validation and documentation-governance checks pass; no game/runtime validation is required.

## Implementation owner

Implementation and final validation belong to `docs/updates/2026/20260715-0001-official-feishu-docs-recrawl.md`.
