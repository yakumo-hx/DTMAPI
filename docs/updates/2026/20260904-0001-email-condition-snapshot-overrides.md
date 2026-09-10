# 20260904-0001：信件条件自动快照与在线单值覆盖

## Metadata

- Update ID: `20260904-0001`
- Date: `2026-09-04`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求把自动生成条件迁出 `EmailArchiveManual.lua`，由紧凑 Wiki JSON 快照承载；另设在线人工 JSON，以单值三态表达条件：缺失或空字符串回退自动条件、非空字符串覆盖、布尔 `false` 强制留空，并继续以独立布尔表覆盖自动隐藏结果。

## Scope

- 以 `24966367_public_958EAF` 解包基线生成自动条件与占位隐藏快照，不读取旧 `Data:Email/conditions.json`。
- `EmailArchiveManual.lua` 只保留联系人、头像、别名和分类数据，生成器不再重写它。
- `EmailArchive.lua` 从页面参数加载自动快照和人工覆盖 JSON，并在信件计数前合并条件与隐藏状态。
- 保持前端 UI、JS、CSS、头像读取和 ID 链接行为不变；只生成本地交付与验收文件，不写线上 Wiki。

## Related Records

- [20260903-0001 信件档案搜索刷新与条件再生成](20260903-0001-email-archive-search-and-condition-regeneration.md)
- [20260903-0002 信件档案无触发占位过滤](20260903-0002-email-archive-hidden-placeholder-filter.md)

## Changed Files

- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\wiki_source\EmailArchive.lua`
  - Optionally loads the automatic snapshot and online overrides through page arguments.
  - Applies the single-value condition rules and boolean hidden override before contact, category, and visible-mail counts are built.
  - Keeps only the primary email-data load as a page-blocking failure; every optional source degrades to an empty table.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\wiki_source\EmailArchiveManual.lua`
  - Removes all generated `condition` and `hidden` fields while retaining the 205 contact/category mappings, contact groups, portraits, and sender aliases.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\wiki_source\信件_新版.wikitext`
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\wiki_source\信件_Entrance.wikitext`
  - Pass the new `conditions` and `overrides` Data pages to the module.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\data\manual\email_condition_overrides.json`
  - Becomes the generator-independent online-authority seed with four dynamic-template conditions and an empty hidden override table.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\data\generated\email_condition_snapshot.json`
  - Adds the compact generated runtime snapshot for build `24966367_public_958EAF`.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\tools\rebuild_email_conditions.py`
  - Generates only the compact snapshot, local audit JSON, and report; it no longer reads or rewrites the manual Lua or override JSON.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\tools\build_email_archive_rebuild.py`
  - Merges the automatic snapshot and overrides for the local preview and includes both Data files in delivery output.
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\tools\test_rebuild_email_conditions.py`
- `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\tools\test_email_archive_flat.js`
  - Cover snapshot scope, single-value condition semantics, hidden booleans, optional-source degradation, generator ownership, stable links, search, and IME behavior.
- Generated local artifacts under `E:\Python_project\DLK\src\wiki\EmailArchiveAssistant\reports`, `data\generated`, and `dist` were rebuilt from the same sources.
- `docs/updates/2026/20260904-0001-email-condition-snapshot-overrides.md`
- `docs/updates/INDEX-2026-09.md`

## Validation

- `python tools\test_rebuild_email_conditions.py`: passed all 14 focused tests.
- `python tools\rebuild_email_conditions.py --check`: passed with no stale generated files; reported 205 source emails, 197 visible emails, 140 non-empty automatic conditions, and 8 hidden placeholders.
- `node tools\test_email_archive_flat.js`: passed routing, duplicate-title rejection, Wiki-link, automatic source-link, search/category, partial rendering, IME composition/debounce, loader, and data-merge checks.
- `python -m py_compile tools\rebuild_email_conditions.py tools\build_email_archive_rebuild.py tools\test_rebuild_email_conditions.py`: passed.
- `node --check wiki_source\EmailArchive.js`: passed; the JS source itself was not modified by this Update.
- `python tools\build_email_archive_rebuild.py`: rebuilt the preview and all delivery variants; the administrator-update directory contains exactly the five intended files.
- Local Chrome acceptance passed at desktop and mobile layouts. It confirmed 197 visible emails, 26 contacts, intact ID routing, automatic and manually overridden condition display, 20 loaded portrait images with no broken image, and no console errors.
- Source-to-delivery SHA-256 parity passed for both Data files, both Lua modules, and the page invocation.

## Evidence

- The automatic runtime snapshot contains only `game_build`, `conditions`, and `hidden`: 140 non-empty condition strings and exactly 8 `true` placeholder entries.
- The first online override seed contains four non-empty dynamic-template conditions and no hidden overrides. Missing IDs, empty strings, and whitespace fall back to automatic conditions; a non-empty string replaces the automatic condition; JSON boolean `false` clears it.
- Automatic hidden values are used by default. A boolean in the online `hidden` map directly overrides them, so `false` can restore an automatically hidden item.
- `EmailArchiveManual.lua` still has 205 metadata entries and contains no condition, hidden, confidence, review, source, or trace fields.
- The full isolated-loader and Entrance-registration deliveries each contain exactly eight files. The administrator-update directory contains only the two Data JSON files, two Lua modules, and `信件.wikitext`.
- `EmailArchive.js`, `EmailArchive.css`, avatar configuration, and `#ID` link behavior were not changed.

## Rollback Notes

- Restore the source Lua, generator, builder, tests and page invocation from the preceding local copies, then rebuild the delivery folders.
- This Update does not modify the online Wiki; existing Wiki revisions remain the rollback authority.

## Follow-up

- An administrator should create `Data:Email/condition_snapshot.json` and `Data:Email/condition_overrides.json`, update the two Lua modules, and update the `Project:沙盒` invocation from the five-file administrator directory.
- After the Wiki sandbox confirms the three condition states and both hidden overrides, reuse the same invocation on the live mail page. No online Wiki page was changed by this Update.
