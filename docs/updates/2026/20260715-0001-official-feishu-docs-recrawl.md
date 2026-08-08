# 20260715-0001 Official Feishu Workshop Documentation Recrawl

## Metadata

- Update ID: `20260715-0001`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: official Workshop documentation references and crawl tooling
- Source: user request to locate and independently refresh the official Feishu authoring guide

## Source request

Locate the official Doloc Town Mod authoring guide, the prior crawl output, and its legacy scripts; open the supplied Feishu Wiki through Chrome; then independently rebuild the export so the official reference snapshot is complete and reproducible.

## Review

- [Official Feishu Workshop Documentation Crawl Boundary Review](../../reviews/code/2026/20260715-0001-official-feishu-crawl-boundary.md)

## Planned scope

- Add a current-workspace browser-backed crawler under `tools/scripts`.
- Preserve the existing 2026-05-17 snapshot.
- Write the new crawl into its own dated folder under `references/doloc-town/official-workshop-docs`.
- Export rendered page text/Markdown/HTML, document images, embedded-sheet TSV/screenshots, raw diagnostic responses, indexes, hashes, and prior-snapshot comparison.
- Add a short reference-root README explaining authority and refresh commands.

## Changed files

- `.gitattributes`（保留哈希清单覆盖的抓取原文空白，不让 Git 空白检查改写官方快照）
- `tools/scripts/crawl-official-workshop-docs.py`
- `references/README.md`
- `references/doloc-town/official-workshop-docs/README.md`
- `references/doloc-town/official-workshop-docs/feishu-crawl-20260715/**`
- `docs/reviews/code/2026/20260715-0001-official-feishu-crawl-boundary.md`
- this Update and `docs/updates/INDEX-2026-07.md`

## Validation

- `python -m py_compile tools/scripts/crawl-official-workshop-docs.py`: passed.
- Root-page smoke: expected title, 33 structured blocks, ten first-level official children, and June 12 source-modified label captured.
- Representative `01 新增道具` smoke: JSON code block, two distinct TSV/screenshot sheets, one rendered image, and original image responses captured.
- Representative large `04 ID对照表（道具）` smoke: four distinct sheets exported at 34x5, 55x2, 11x2, and 11x7 instead of four copies of the first sheet.
- Full crawl plus bounded resume: `complete=true`, 55 pages, zero page failures, empty remaining queue, 107/107 sheets, and 138/138 rendered document images.
- Offline artifact audit: 979 manifest inventory files, zero missing/size/hash mismatches, zero failed/empty TSVs, zero failed rendered images, zero same-page duplicate TSV hash groups, and no legacy `渲染片段分隔` marker.
- `git diff --check` applies normally to authored files; the hash-inventoried crawl snapshot is path-exempt from whitespace rewriting so captured HTML/TSV bytes remain faithful.
- Additional retained evidence: 238 original public image responses and 109 bounded public sheet API responses.
- Previous-snapshot comparison: 55 retained tokens, zero added, zero removed, 50 spacing-only title changes, and one substantive title change (`获取创意工坊权限&查看示例模组` to `查看示例模组`).
- Visible source-modified labels put 19 pages after the old 2026-05-17 snapshot; the latest observed child label is June 21 (`03 新增料理`).
- `tools/scripts/check-doc-governance.ps1`: passed (4,672 checks).
- No game/runtime validation was required or run.

## Evidence

- Chrome window title confirmed the supplied live root.
- Independent Chrome render returned HTTP 200 and exposed the current June 12 root text and official child links.
- Representative embedded-sheet clipboard extraction returned ordered TSV; details are frozen in the linked Review.
- [Current snapshot README](../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260715/README.md)
- [Crawl report](../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260715/REPORT.md)
- [Machine-readable manifest](../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260715/manifest.json)
- The Chrome plugin opened the supplied page, but its page-state read requested an application approval that the current session could not display. The actual extraction therefore used a separate local Chrome/Playwright session against the same public URL; no login bypass or private source was used.

## Rollback

- Remove the new dated snapshot, crawler, reference README changes, and this Update/ledger entry. The preserved 2026-05-17 snapshot remains unchanged.

## Follow-up

- Treat the live Feishu Wiki as canonical and rerun into a new dated folder when its source-modified labels or linked token set changes.
- Use `--resume` only for an existing dated snapshot whose verified pages should be retained; a new source date should use a new output directory.
- Any DTMAPI author-document change derived from these official tables remains a separate reviewed update; this crawl does not itself change runtime/API behavior.

## Post-validation correction (2026-07-15)

The real-environment tutorial audit in [20260715-0003](20260715-0003-official-tutorial-real-environment-audit.md) found that crawler v1.0.0 could retain only the last rendered fragment of a long virtualized Feishu code block and could repeat descendant-list text in its parent block. The dated snapshot was rebuilt in place with crawler v1.1.0 because this was an export-fidelity correction on the same capture date, not a new source snapshot.

- Final corrected snapshot: 55 pages, 86/86 complete code-line ranges, 107/107 sheets, 138/138 rendered images, zero failures and an empty remaining queue.
- Final inventory: 1,081 files, zero missing files, size mismatches or SHA-256 mismatches.
- The earlier 979-file count above describes the pre-correction v1.0.0 artifact and must not be used as the current snapshot acceptance count.
