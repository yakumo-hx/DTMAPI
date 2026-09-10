# 20260831-0005：PROJECT 与 Runtime Workshop Skill 权威路由收口

## Metadata

- Update ID: `20260831-0005`
- Date: `2026-08-31`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求继续优化 `PROJECT.md` 与审计建议第三批中的 Workshop audit Skill
- Review: [20260831-0005 新任务工作空间必读上下文与 Skill 路由审计](../../reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md)

## Scope

- 从 `PROJECT.md` 顶部移除具体 Workshop item、首次上传/订阅字节一致性和待玩家验收等易变快照，只保留“多个物理分发投影同一 Runtime 身份”的稳定规则。
- 将当前分发、订阅和发布/验收事实路由到 Product Catalog、current subscription manifest 及其指向的 Review/Update，并增加文档治理回归门。
- 缩窄 `dtmapi-workshop-release-audit` 的触发描述，明确只覆盖 DTMAPI Runtime 的 Windows/多平台包、订阅一致性和玩家安装失败，不覆盖普通产品 Mod 上传目录同步。
- 把 Skill 正文改为 Windows、多平台、订阅一致性和玩家失败四类模式的薄路由；现行包级/生命周期脚本继续拥有确定性检查。
- 删除 Skill 内无现行调用者、BAT-only 且默认创建独立系统临时根的平行测试器。
- 保留隐式 Skill 调用；不处理 `guidang`、Skill Git 持久化选择、Runtime/package bytes、Steam/upload、游戏目录、Official MODS 或玩家存档。

## Changed Files

- `PROJECT.md`
- `.agents/skills/dtmapi-workshop-release-audit/SKILL.md`
- `.agents/skills/dtmapi-workshop-release-audit/scripts/test_subscription_package.ps1`（删除）
- `tools/scripts/check-doc-governance.ps1`
- `docs/reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md`
- `docs/updates/2026/20260831-0005-project-workshop-skill-authority-routing.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `skill-creator/scripts/quick_validate.py .agents/skills/dtmapi-workshop-release-audit`: PASS (`Skill is valid!`).
- `tools/scripts/check-doc-governance.ps1`: PASS, `7,523` checks. The checker now rejects concrete ten-digit Workshop item IDs and publication/acceptance snapshot language in the stable `PROJECT.md` introduction, and requires both live authority routes.
- PowerShell parser check for `tools/scripts/check-doc-governance.ps1`: `0` parse errors.
- `tools/scripts/check-test-artifact-governance.ps1`: PASS.
- Skill route target check: `7/7` referenced canonical documents, authorities and scripts exist; active references to the retired tester: `0`.
- Trigger-boundary static contract: Runtime Workshop, published Windows, multi-platform and player-installer intents remain positive; ordinary product Mod upload-folder syncing is explicitly excluded. This is a description/body contract check, not a live model invocation.
- Focused Markdown-link resolution: `10` local targets checked, `0` missing.
- Focused trailing-whitespace/conflict-marker scan and tracked `git diff --check`: PASS; only expected line-ending notices were emitted.
- No package matrix, game, Runtime, Steam or upload validation was run because no package bytes, installer implementation or shared runtime state changed.

## Evidence

- 删除前的平行脚本为 `12,727` bytes，SHA-256 `3005cd0a7272b0a992225e85f216a7ec8dc880a118e60d480935c1173c52777e`；仓库活动代码/脚本中没有调用者。
- Skill 从 `85` 行收束为 `39` 行，启动描述为 `291` 个字符；确定性测试完全路由到仓库现有三条 canonical 脚本。
- `PROJECT.md` 稳定介绍中具体十位 item ID 和首次上传/字节一致性/待验收快照均为零，并由治理脚本持续检查。
- 官方 Skill 渐进披露与仓库级位置依据：[Build skills](https://learn.chatgpt.com/docs/build-skills)。

## Rollback Notes

- `PROJECT.md` 可恢复旧首段，但这会重新引入易变发布快照，不应作为长期方案。
- Skill 的旧测试器在删除前未被 Git 跟踪，不能通过 `git restore` 恢复；若必须调查历史行为，使用上面的哈希识别可信旧副本，并保持其为历史证据，不重新设为执行入口。
- 若新路由漏掉真实 Runtime 审计意图，优先修正 Skill 描述或模式表，不复制 canonical 脚本实现。

## Follow-Up

- `guidang` 的触发边界与两个 Skill 的持久化策略仍由用户另行决定。
- 大型 API/installer 文档是否需要进一步分层，应在单独范围内按实际冷启动成本处理。
