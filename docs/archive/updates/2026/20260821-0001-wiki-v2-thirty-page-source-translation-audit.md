# 20260821-0001：Wiki V2 三十页源码级英文翻译与逐页校对

## Metadata

- Update ID: `20260821-0001`
- Date: `2026-08-21`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: 用户要求使用其现有 Chrome 会话，只读读取灰机 Wiki 当前中文页面的原始 Wikitext，完成 V2 全部 30 个正式英文页面的源码级翻译、逐页第二遍校对、记录与完整本地验收；随后要求把面向使用者的交付包简化为四个中文目录，并为所有 `.wiki` 提供同内容 `.txt`。

## Scope

- 逐页读取 `wiki/v2/config/articles.json` 所列 30 个中文页面的当前 `action=raw` Wikitext，不把资料卡、模板、模块、配方表、商店表等动态渲染输出误当作硬编码翻译对象。
- 对照 30 个本地英文权威源、逐页 Translation Review、官方 English TextMapper 和已有 V2 语义边界，修正遗漏、误译、链接与英语表达问题。
- 为 30 页分别保存源码层读取证据、术语检查、结构检查、第二遍校对结论和仍需 Wiki 方确认的问题。
- 重建 54 目标交付并运行 `wiki/v2` 完整 `npm test`；线上 Wiki 始终只读，不保存、上传或修改页面。
- 面向管理员、Bot 维护者和编辑者的最终压缩包只保留 `组件/`、`bot工具/`、`页面示例/`、
  `说明/`；完整审计、manifest、报告和回执继续保存在开发仓库，不进入交付包。
- 不建立中文 revision 漂移、自动过期提醒、英文正文自动覆盖或通用多语言底座；不把 agent 校对冒充社区人工最终验收。

## Changed Files

- `wiki/v2/editorial-reviewed/*.wiki`（仅实际需要修正的正式英文正文）
- `wiki/v2/reviews/translation/*.md`（30 页逐页源码复核记录）
- `wiki/v2/records/V2-20260821-0005-thirty-page-source-translation-audit.md`
- `wiki/v2` 构建生成物、manifest、报告与台账（由受控构建重建）
- `wiki/v2/scripts/package-delivery.mjs`
- `wiki/v2/package.json`
- `wiki/v2/docs/README.md`
- `wiki/v2/docs/upload-runbook.md`
- `wiki/v2/docs/简化交付/*.md`
- `wiki/v2/upload/README.md`
- `docs/reviews/code/2026/20260821-0001-wiki-v2-final-delivery-audit.md`
- `docs/updates/2026/20260821-0001-wiki-v2-thirty-page-source-translation-audit.md`
- `docs/updates/INDEX-2026-08.md`

## Final Evidence

- Chrome 只读读取已完成 `30/30`；29 页当前 raw Wikitext SHA-256 与现有审校快照一致。
- `钓鱼概率计算器` 当前中文源码多出一段明显的 Google Translate 界面污染文字；该文字不是游戏或 Wiki 正文事实，不传播到英文页，并作为中文源待清理问题记录。
- 最终交付为 54 个公共目标、12 个受保护基础页、42 个编辑者页面（12 分类 + 30 正式英文正文）、0 redirects、0 aliases、无 `/en` 路由。
- 独立最终审查逐项检查 7 处本轮正文修正，未发现阻塞交付的语义、术语、结构或动态组件边界问题；其余 23 页由当前源码层复核记录与自动契约共同覆盖。
- 最终交付压缩包 `英文wiki尝试.zip` 只包含四个中文根目录；开发审计材料不再混入用户交付。
  2 个 Template、30 个英文正文和 12 个英文分类共 44 个 `.wiki`，每个都有逐字相同的 `.txt`
  副本。正式 Bot mapper、reverse/解包数据、历史 V1 和测试 fixture 均不入包。
- 未发生线上 Wiki 写入，也未触及游戏 Runtime、Workshop、存档或本地 Doloc Town 部署。

## Validation

- `npm run build`: passed；生成 30 articles / 61 routes / 132 rendered calls，以及 54 public targets / 12 protected / 42 editor targets。
- 完整 `npm test`: `26 passed, 0 failed, 5 server gates pending`。
- 真实 Data 组件 smoke: 132 calls 全部通过；entity golden: 20 cards / 317 official English keys。
- 直接解析最终 manifest：3 Data、7 Module、2 Template、12 Category、30 Article、0 Redirect，共 54；受保护 12、编辑者 42。
- 直接检查 active config、title Data、manifest、ordered targets 与 rollback manifest：alias、`Fish Generator`、`/en` 和 `#REDIRECT` 命中均为 0；redirect 目录为 0。
- 30 个 Translation Review 均有本次源码层复核段；translation ledger 共 30 项，`human-community-acceptance=true` 为 0、`false` 为 30。
- `npm run package:delivery`: passed；ZIP 解压后 103 个文件逐项与 staging 哈希复验通过，ZIP 路径使用标准 `/` 分隔符。
- 最终 ZIP：`wiki/v2/dist/英文wiki尝试.zip`，148,052 bytes，SHA-256
  `e121d4c8c3d709032ec3690f80fc103be64bb3db2e74e4f9cf941447e29f6c8d`；独立 `Get-FileHash` 一致。
- ZIP 根目录精确为 `组件/`、`bot工具/`、`页面示例/`、`说明/`；44 组 `.wiki/.txt` 哈希
  全部一致，0 个旧 audit/manifest/checksum 路径，0 redirect、alias、`/en`、reverse/fixture 或
  Bot mapper 数据文件。旧复杂交付目录、ZIP 和外置 checksum 已在新包验证成功后移除。
- `git diff --check`: passed。未运行游戏、Runtime 或 Workshop 测试；它们不属于本地 Wiki 内容交付边界。

## Evidence

- 30 页审校记录：`wiki/v2/reviews/translation/`。
- 总体执行记录：`wiki/v2/records/V2-20260821-0005-thirty-page-source-translation-audit.md`。
- 最终验证报告：`wiki/v2/reports/latest-validation.md` and `wiki/v2/reports/latest-validation.json`。
- 独立交付审查：`docs/reviews/code/2026/20260821-0001-wiki-v2-final-delivery-audit.md`。
- 精简包入口：`wiki/v2/dist/英文wiki尝试/说明/01-先读我.md`；54 个目标的文件映射见同目录
  `02-上传文件对照表.md`。完整 manifest、报告和翻译审查仍由 `wiki/v2` 源码树拥有。

## Related Records

- `wiki/v2/records/V2-20260821-0004-bilingual-pipeline-boundary-correction.md`
- `wiki/v2/docs/semantic-contracts.md`
- `wiki/v2/docs/server-acceptance.md`

## Rollback Notes

- 回滚仅需恢复本 Update 所列的英文正文、30 页审校附录、总体记录及本地构建生成物；不得恢复任何历史 alias、redirect 或中文标题 `/en` 路由。
- 若撤销交付打包能力，只删除精确的 `wiki/v2/dist/英文wiki尝试` 与
  `wiki/v2/dist/英文wiki尝试.zip`，并恢复 package script 和本 Update 所列交付文档；不得删除
  整个 `wiki/v2` 或 `wiki` 根目录。
- 本任务没有线上写入、Runtime、Workshop 或存档变更，因此不需要线上或玩家环境回滚。

## Follow-Up

- 保持 `human-community-acceptance=false`，直到真实社区/人工验收完成。
- 将钓鱼概率计算器的中文 Google Translate 界面污染、警用无人机/电力源页校对标记及逐页记录中的其他事实疑点交由 Wiki 方决定；本任务不修改中文页面。
- 上线前仍须完成既有五项服务器门禁：政策例外、授权 Bot mapper、未保存桌面/移动预览、Scribunto/搜索验收、保存 revision 回执。任何本地 PASS 都不授权线上写入。
