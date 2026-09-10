# 20260903-0001：信件档案搜索刷新与条件再生成

## Metadata

- Update ID: `20260903-0001`
- Date: `2026-09-03`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: 用户明确提出两个任务：修正搜索输入反复渲染；完全排除旧 `Data:Email/conditions.json`，以 `24966367_public_958EAF` 及其后明确选择的最新 DTMAPI 解包基线为游戏触发事实来源，自动转换可证明的条件并允许少量人工补充。

## Scope

- 保持现有信件 UI、DOM 类名、数据加载边界和 ID 路由不变。
- 将搜索更新改为组合输入安全的异步防抖，并只刷新筛选结果相关区域。
- 新建离线条件提取器，读取最新解包基线中的 NodeCanvas 图、种子解锁表和必要的本地化表，输出结构化证据及玩家可读条件。
- 新建小型人工覆盖输入；自动结果优先承载明确游戏条件，人工覆盖只处理无法自然表达或另有可靠证据的例外。
- 生成现有 `EmailArchiveManual.lua`，不新增 Wiki 运行时模块，不读取旧 `conditions.json` 或旧 DLK 游戏导出。
- 重建本地视觉验收文件与扁平交付目录；不写入线上 Wiki。

## Related Review

- [Email Archive 中文输入法组合输入被取消](../../archive/reviews/manual-qa/2026/20260831-0001-email-archive-ime-composition-cancel.md)
- [Email Archive online-pattern and redundancy review](../../archive/reviews/code/2026/20260829-0001-email-archive-online-pattern-redundancy-review.md)

## Changed Files

- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchiveManual.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/rebuild_email_conditions.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_archive_rebuild.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_email_archive_flat.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_rebuild_email_conditions.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/data/manual/email_condition_source.json`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/data/manual/email_condition_overrides.json`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/data/generated/email_conditions.generated.json`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/email_conditions_generated.md`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-09-03-admin-handoff.md`
- regenerated `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html` and delivery folders
- this Update, the related Manual QA append-only observation, and the 2026-09 Update ledger

## Validation

- Python syntax and focused condition-extraction tests ran against the pinned authoritative build.
- Tests assert that no active condition-generation input references `Data:Email/conditions.json`, old Wiki condition pages, DLK game exports, or an obsolete reverse build.
- JS syntax and event tests cover composition, debounce coalescing, ordinary input, category changes, routing and copy-link behavior.
- The standalone HTML was rebuilt and structurally checked; Chrome automation inspected the current online sandbox baseline without modifying Wiki. Automated navigation to the new local file was rejected by Chrome's local-file safety policy, so the generated HTML remains the user-opened visual acceptance artifact.
- Generated Lua passed the existing builder, all 205 emails remain present, unresolved conditions remain empty, and the administrator directory contains only its six declared files.

## Evidence

- Search input now ignores `input` while `compositionstart`/`event.isComposing` is active, coalesces `compositionend` and its following `input`, debounces accepted values for 160 ms, and runs a result-only renderer that leaves the search, statistics and category DOM untouched.
- `python tools/test_rebuild_email_conditions.py`: 9 focused tests passed against `24966367_public_958EAF`, including exact 205-ID coverage, faction thresholds, seed automation, narrow overrides, blank unresolved items, Feilian/Sacco separation and generated-Lua boundaries.
- `node tools/test_email_archive_flat.js`: all route, duplicate-title, stable-link, composition/debounce, partial-render, search/category, loader syntax and automatic-ID-link checks passed; direct import from the pinned reverse baseline confirmed 205 emails, 10 duplicate-title groups and 66 involved emails.
- `python tools/rebuild_email_conditions.py --check`: no stale generated outputs. Final split is 122 NodeCanvas, 18 seed table, 4 manual dynamic-template exceptions and 61 blank; the player-visible module therefore has 144 non-empty conditions and no generic unresolved placeholder.
- `python -m py_compile ...` and `node --check wiki_source/EmailArchive.js`: passed.
- `python tools/build_email_archive_rebuild.py`: rebuilt the acceptance HTML directly from the pinned reverse email table and produced delivery outputs with 205 emails and 26 contact sources. `dist/wiki_admin_updates` contains exactly the six declared files.
- Standalone HTML structural checks confirmed the new condition samples, composition handlers, 160 ms debounce, `first_wharf` ID and absence of `conditions.json`; every administrator file has exact SHA-256 parity with its `wiki_source` owner.
- Chrome read-only audit confirmed that all six upload targets already exist and recorded rollback revisions `26246`, `26247`, `26286`, `26285`, `26056` and `23893`; `Project:沙盒` revision `26294` already invokes the correct data/manual pair and needs no edit.
- Chrome sandbox audit confirmed 205 emails, 26 contact sources, and 20/20 portrait images loaded after lazy-load scrolling with no broken image. This verifies the current online baseline; the newly generated local file could not be opened through browser automation because Chrome rejected local-file navigation, so visual comparison remains available through the standalone HTML for user opening.
- `tools/scripts/check-doc-governance.ps1`: passed 7,620 checks after registering the September Update and closing the August monthly ledger summary.

## Rollback Notes

- Restore the listed `wiki_source`, tools and tests, then rerun the existing rebuild tool.
- Wiki remains untouched in this Update; current online revisions remain independent rollback points.

## Follow-up

- Administrator replaces the six existing Wiki pages from `dist/wiki_admin_updates`; exact targets, current revision IDs, edit summaries and exclusions are in `reports/2026-09-03-admin-handoff.md`.
- Final closure of the IME symptom still requires one user test with the real Windows Chinese input method after `零件:Debug.js` is updated.
