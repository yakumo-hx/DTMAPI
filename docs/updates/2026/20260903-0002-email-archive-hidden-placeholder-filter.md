# 20260903-0002：信件档案无触发占位过滤

## Metadata

- Update ID: `20260903-0002`
- Date: `2026-09-03`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Source: 用户指出沙盒仍把四个阵营的无触发重复原型显示成六封，要求生成器尽量自动判定并在 `EmailArchiveManual.lua` 写入 `hidden = true`，由 `EmailArchive.lua` 在联系人、分类和总数统计前跳过。

## Scope

- 仍以 `24966367_public_958EAF` 为唯一游戏事实基线，不读取旧 DLK 快照或 `Data:Email/conditions.json`。
- 自动识别可证明的无操作占位原型；不把“条件为空”直接解释为隐藏。
- 在生成的人工数据 Lua 中只给命中项追加 `hidden = true`。
- 业务 Lua 在创建联系人、分类、奖励和信件记录前跳过隐藏项。
- 本地预览构建器采用相同过滤语义，并把当前管理员增量目录收窄到两份 Lua。
- 用户随后把两份增量 Lua 保存到线上既有模块；不修改 `Project:沙盒`、公共加载器、前端资源或 Wiki 数据页。

## Related Review

- [20260903-0001 信件档案搜索刷新与条件再生成](20260903-0001-email-archive-search-and-condition-regeneration.md)

## Changed Files

- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/rebuild_email_conditions.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/build_email_archive_rebuild.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_rebuild_email_conditions.py`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/tools/test_email_archive_flat.js`
- generated `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchiveManual.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.lua`
- generated `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/data/generated/email_conditions.generated.json`
- generated `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/email_conditions_generated.md`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-09-03-admin-handoff.md`
- regenerated `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html` and delivery folders
- this Update and the 2026-09 Update ledger

## Validation

- Focused Python tests cover the automatic hidden set, current baseline counts, empty-condition non-equivalence, Doloc six-mail retention and generated Lua syntax shape.
- Frontend tests consume the generated hidden set and verify raw 205 versus visible 197, stable ID routing, four-item duplicate numbering, IME behavior and loader syntax.
- The normal builder regenerates the standalone HTML and both full delivery variants; the current administrator increment contains only the two changed Lua files.
- Chrome opens the local HTML over a loopback-only read server and verifies the visible total plus a direct faction-mail hash route.
- 用户保存两份 Lua 后，以 Chrome 只读核对线上修订差异和 `Project:沙盒` 的实际渲染结果。
- Documentation governance checks cover this Update and its single monthly-ledger row.

## Evidence

- The detector requires all of: a numbered sibling series; identical sender, title and body; explicit `CfgEmailAttachNone`; no NodeCanvas, Yarn or direct-code send reference; and a referenced sibling carrying a real attachment.
- The rule finds exactly eight records: `_4` and `_5` in the vulture, kontiki, skychild and cerrorico series. It does not hide any of the six Doloc reputation mails.
- Generated counts are 205 source rows, 197 visible rows, 8 hidden placeholders and 53 visible blank-condition mails; condition sources remain 122 graph, 18 seed and 4 manual.
- Local Chrome shows 197 total mails, 26 contacts, the Kontiki contact with 4 groups, and `#force_kontiki_6` selected as `群岛的问候（4/4）` with its attachment intact.
- `Module:Email/EmailArchiveManual` 已保存为修订 `26444`，标记为小修改，摘要为 `标记无触发占位信件`；差异确认八个 `_4/_5` 项均新增 `hidden = true`，上一版为 `26443`。
- `Module:Email/EmailArchive` 已保存为修订 `26445`，标记为小修改，摘要为 `过滤无触发占位信件`；差异确认过滤发生在联系人、分类、奖励和信件计数之前，上一版为 `26286`。
- 线上 `Project:沙盒` 随即显示 197 封、26 个联系人、102 封含附件信件；康提基、撑犁子和里科山各为 4 组，`#force_kontiki_6` 刷新后正确打开 `群岛的问候（4/4）`，附件 `岛屿徽章 × 1` 正常显示，既有 UI 未发生可见变化。

## Rollback Notes

- Revert the generator, builder, two tests and the two Lua source files, then rerun the builder to restore the preceding 205-visible-mail package.
- Wiki 回退时，把 `Module:Email/EmailArchiveManual` 恢复到修订 `26443`，把 `Module:Email/EmailArchive` 恢复到修订 `26286`；两页历史记录是线上回退权威。

## Follow-up

- 本轮两份 Lua 已上线并通过沙盒验收，无需继续上传。后续游戏基线更新时重新运行生成器；若占位结构发生变化，先查看生成报告再发布新人工表。
