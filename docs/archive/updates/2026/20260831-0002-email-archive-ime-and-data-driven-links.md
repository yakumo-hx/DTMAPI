# 20260831-0002：信件档案中文输入法与数据驱动链接修复

## Metadata

- Update ID: `20260831-0002`
- Date: `2026-08-31`
- Lifecycle Status: `implemented`
- Validation Level: `source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: 用户要求把沙盒搜索框的拼音输入法闪退与重名信件链接维护一并处理；链接方案需先遵循 Wiki 既有惯例，主要覆盖飞廉的完全同名标题，核心由导入 Wiki 的信件数据自动解析，不得把人物、标题或 ID 写成人工维护表，只有无法继续消歧时才允许自动编号。

## Scope

- 修正 `EmailArchive.js` 的中文输入法组合输入事件：组合期间不筛选、不回写输入值，在 `compositionend` 后应用最终查询。
- 线上 `模板:信件检索` 的“标题 → 发件人 → 序号 → ID”层级继续只负责检索卡片，不新增会为永久链接二次加载主数据的包装模板。
- 信件 ID 始终是唯一锚点；`EmailArchive.link` 保持轻量的 `id=` 入口且不加载主数据。
- 前端从 Lua 已输出的导入信件数组自动统计同名标题；优先使用同组唯一的正文高亮词生成 `标题（数据提示）`，只在提示缺失或仍重复时退回 `标题（序号/总数）`。人物、标题、次数和 ID 均不写入人工表。
- 折叠的“条件 / 标签”区域新增“复制 Wiki 链接”，普通编辑先按标题、正文或奖励搜索并打开目标信件，再复制 `[[信件#<ID>|<自动显示标题>]]`，无需浏览 JSON。
- 重名标题哈希继续拒绝猜测；唯一旧标题仍可兼容解析，自动序号不参与锚点身份。
- `EmailArchive.lua` 与 `EmailArchiveManual.lua` 经线上逐字复核后保持不变；本轮没有增加 Lua 数据加载、校验或维护层。
- 既有 `EmailUtils.item_source_info` 与 `RecipeUtils.recipe_source_info` 各保留原逻辑，只把自动来源链接的锚点参数从 `email_proto.title.text` 改为同一导入记录的 `email_proto.id`；它们仍不是新核心依赖。
- 重建本地视觉验收页和两套扁平交付目录；通过本地和 Chrome 沙盒注入验收后再报告管理员需要更新的 Wiki 资源，本轮不擅自保存线上修订。

## Changed Files

- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.css`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailUtils.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/RecipeUtils.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_archive_rebuild.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_email_archive_flat.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html`（重建）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery/*`（重建）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/*`（重建）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_admin_updates/*`（新增 4 文件增量包）
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-31-ime-link-candidate/TARGETS.md`
- `docs/reviews/manual-qa/2026/20260831-0001-email-archive-ime-composition-cancel.md`
- `docs/updates/2026/20260831-0002-email-archive-ime-and-data-driven-links.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `node --check wiki_source/EmailArchive.js`：通过。
- `node tools/test_email_archive_flat.js`：通过；覆盖组合期间零提交、`compositionend` 提交、普通输入、非搜索目标、直接 ID、唯一旧标题、重名标题拒绝、数据提示优先、编号降级、Clipboard 写入与稳定 Wiki 链接。
- 同一测试读取真实 `data/wiki/tbemail.json`：205/205 个非重复 ID；10 组、66 封重名；`seed_agave` 自动为 `种子上新！（龙舌兰种子）`，`seed_wheat` 自动为 `种子上新！（小麦种子）`，两者仍分别路由到自己的 ID；内容也无法区分的 `force_vulture_4/5` 才使用 `/6` 序号且不会串信。
- `python -m py_compile tools/build_email_archive_rebuild.py` 与主构建：通过；输出 205 封、26 个联系人、88 封有人工条件，重建两套各 6 文件的完整交付和一个恰好 4 文件的管理员增量包。
- 两套交付的 `EmailArchive.lua`、`EmailArchiveManual.lua`、`EmailArchive.js` 与 `EmailArchive.css` 均与 `wiki_source` SHA-256 一致；两份交付 JS 再次通过 `node --check`。
- Chrome 只读检查公共 `Project:沙盒`：现行界面加载 26 个联系人、20 个真实头像；线上 Lua r26286 与 Manual r26285 在统一换行后和本地逐字一致，因此本轮无需更新 Lua。
- Chrome 用现行沙盒直接检索导入正文“龙舌兰”，结果只剩飞廉并精确展开 `seed_agave`；逐一滚动离屏联系人后头像为 20/20 自然尺寸非零，证明普通编辑可以从玩家可见内容定位目标信件，不必查 JSON ID。
- Chrome 只读检查现行沙盒资源：`零件:Debug.js` r26246、`零件:Debug.css` r26247 尚未包含本轮候选；站内源码检索未找到可复用的 Clipboard/`execCommand` 包装。
- Chrome 只读差异核对：本地 `EmailUtils.lua` 只比线上 r26056 改第 278 行的锚点参数，本地 `RecipeUtils.lua` 只比线上 r23893 改第 217 行的锚点参数；自动测试确认两处均使用 `email_proto.id`。`EmailUtils` 当前至少被 500 个 Wiki 页面转入，因此作为独立管理员合并项保留。
- 候选未保存到 Wiki，真实 Windows 中文输入法候选窗口及线上视觉仍待管理员更新 JS/CSS 后由用户验收；本地自动事件检查不能替代该手工门槛。

## Evidence

- 根因与玩家验收门槛：[Email Archive 中文输入法组合输入被取消](../../reviews/manual-qa/2026/20260831-0001-email-archive-ime-composition-cancel.md)。
- 线上 `模板:信件检索/doc` `r24714` 明确把标题和发件人作为常规查询参数，无法唯一时使用序号，ID 正常可不使用。
- 线上 `模块:Email/EmailUtils` `r26056` 已承认信件标题和发件人可能重名，并从 `Data:Email/tbemail.json` 查询；旧生成链接仍使用标题锚点，是本轮需要替换的维护缺口。
- 当前本地交付与沙盒仍以 205 封导入信件数据为运行基础。
- 导入快照中 205 个 ID 全部唯一；10 个重名标题组覆盖 66 封，其中飞廉的 `种子上新！` 22 封、`种子包上新！` 2 封。标题加联系人仍不能消歧，且四对信件连标题、寄件人、正文和附件都相同，进一步证明永久身份必须使用源 ID。
- 当前 Wiki 资源回退基线与本地候选文件记录在 `reports/2026-08-31-ime-link-candidate/TARGETS.md`。

## Rollback Notes

- 本轮线上写入保持为零；本地回滚只需恢复列出的 `wiki_source`、构建器与测试文件并重新运行主构建器。
- 后续若候选发布失败，恢复 `零件:Debug.js` r26246、`零件:Debug.css` r26247、`模块:Email/EmailUtils` r26056 与 `模块:Recipe/RecipeUtils` r23893；核心业务 Lua 与人工表没有本轮变更。
- 不删除或覆盖整个 `EmailArchiveAssistant`、`dist`、`零件:Debug.js`、现有 `模板:信件检索` 或正式 `信件` 页面。

## Follow-Up

- 实现完成后用真实中文输入法做最终玩家验收；自动事件测试与浏览器合成事件不能替代系统候选窗口确认。
- 管理员增量目录只含四个目标：`EmailArchive.js` → `零件:Debug.js`、`EmailArchive.css` → `零件:Debug.css`、`EmailUtils.lua` → `模块:Email/EmailUtils`、`RecipeUtils.lua` → `模块:Recipe/RecipeUtils`。更新后用真实中文输入法和飞廉重名信件完成最终验收。
- 验收通过后再决定是否把相同资源登记到正式信件页；本轮不修改 Common.js、Entrance.js、正式 `信件` 页面或任何 Lua。
