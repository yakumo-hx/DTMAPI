# 20260829-0001：信件档案扁平化与双页面加载交付

## Metadata

- Update ID: `20260829-0001`
- Date: `2026-08-29`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户确认将 205 封信从 `groups[].entries[0]` 扁平化，删除首版 `MediaWiki:Common.js` 加载片段，并同时交付页面级 `Html:EmailArchiveLoader` 与合并到既有 `零件:Entrance.js` 的二选一方案；只做本地实现、视觉验收与精简交付，不上传 Wiki。

## Scope

- 将信件运行时快照改为顶层 `emails[]`，移除一封信一层 group/entry 的重复包装。
- 保持现有暗色邮务终端 DOM/CSS、搜索、分类、联系人展开、阅读器、移动端和 `#<信件ID>` 路由行为。
- 新增小型 `Html:EmailArchiveLoader`，只在显式调用它的页面加载信件 JS/CSS。
- 另提供标明 `MERGE-ONLY` 的 `零件:Entrance.js` 登记片段，页面白名单严格为 `信件` 和 `Project:沙盒`；两个方案不得同时部署。
- 从首版交付移除 `MediaWiki_Common_loader.js`、`EmailUtils.lua` 和 `RecipeUtils.lua`；不修改后二者的线上兼容逻辑。
- 同步本地主构建器、视觉验收页与扁平六文件交付目录。
- 不写入灰机 Wiki，不修改游戏 Runtime、Workshop、存档或本地 Doloc Town 部署。

## Changed Files

- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/Html_EmailArchiveLoader.html`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/Entrance_EmailArchive_registration.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/信件_新版.wikitext`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/信件_Entrance.wikitext`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/MediaWiki_Common_loader.js`（删除）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_archive_rebuild.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_wiki_delivery.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_email_archive_flat.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery/*`（受控重建）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/*`（受控重建）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html`（受控重建）
- `docs/updates/2026/20260829-0001-email-archive-flat-sandbox-delivery.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `python tools/build_email_archive_rebuild.py` 通过：输出 205 封信、205 个唯一 ID、32 个联系人、88 条非空人工条件；扁平快照仅含顶层 `emails[]`，不再含 `groups/entries`。
- `node --check wiki_source/EmailArchive.js`、`node --check wiki_source/Entrance_EmailArchive_registration.js` 与两个 Python 构建器的 `py_compile` 均通过。
- `node tools/test_email_archive_flat.js` 通过直接 ID、`email-` 前缀、唯一旧标题、重复标题拒绝、角色、搜索、分类以及两种加载器语法检查。
- 降级注入通过：人工表缺失、未知寄件人、未分类、未知附件类型和 `$type/＄type` 均不阻断页面；Lua 仅保留主 JSON 与人工表两个 `pcall` 边界，主数据失败只输出短错误区。
- Chrome 1440×900 验证默认页、搜索、分类、清空筛选、头像、附件、空条件、`#first_wharf`、重复标题不同 ID 及前进/后退；Chrome 内 390×844 独立视口验证默认联系人页、深链接阅读器、返回列表与返回角色。20 个活跃头像均加载，控制台 0 warning/error。
- `dist/wiki_delivery` 与 `dist/wiki_delivery_entrance` 均为恰好 6 个文件、0 个子目录；四个公共业务资源与各自 `wiki_source` 哈希一致，两个目录只含各自加载方案。
- 当前线上 `信件` 页面仅只读检查，未上传或修改 Wiki。游戏、Runtime 和 Workshop 测试不属于本地 Wiki 交付边界。

## Evidence

- 本地视觉验收页：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html`。
- 独立 Html 交付：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery/`。
- Entrance 登记交付：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/`。
- 可复现构建入口：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_archive_rebuild.py`。
- 扁平结构与路由检查：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_email_archive_flat.js`。
- 运行时数据来源仍为 Wiki 既有 `Data:Email/tbemail.json`；本任务未复制或交付该数据页。

## Rollback Notes

- 恢复上述 `wiki_source` 与构建器文件后重新运行主构建器，即可恢复旧本地交付。
- 本任务不写线上 Wiki，因此不需要线上回滚；不得为了回滚删除整个 `EmailArchiveAssistant` 或 `dist` 根目录。

## Follow-Up

- 管理员上线时只选择一个交付目录：独立 `Html:` 方案，或将登记片段合并进既有 `零件:Entrance.js`；不得整页覆盖 Entrance，也不得同时部署两种加载器。
- 首轮只在 `Project:沙盒` 演示；线上 `信件` 页面继续保持原状，直到管理员和维护者另行确认切换。
