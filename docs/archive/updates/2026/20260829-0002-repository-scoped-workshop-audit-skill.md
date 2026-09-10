# 20260829-0002：Workshop 审计 Skill 限定为 DTMAPI 仓库级

## Metadata

- Update ID: `20260829-0002`
- Date: `2026-08-29`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求将 `dtmapi-workshop-release-audit` 从用户级 Skill 目录迁入 DTMAPI 仓库，只限制可见作用域，并明确保留基于描述匹配的隐式调用。

## Scope

- 将完整 Skill 目录从 `D:/OpenAI/CodexHome/skills/dtmapi-workshop-release-audit` 移至仓库根目录 `.agents/skills/dtmapi-workshop-release-audit`。
- 保留原有 `SKILL.md` 和订阅包测试脚本；脚本文件字节不变。
- 将 `SKILL.md` 的示例命令从用户全局路径改为通过 Git 仓库根目录解析仓库级脚本。
- 不添加 `policy.allow_implicit_invocation: false`，也不新增其他显式调用限制；隐式调用保持默认开启。
- 不修改 DTMAPI Runtime、游戏目录、Workshop 包、发布授权或玩家订阅目录。

## Changed Files

- `.agents/skills/dtmapi-workshop-release-audit/SKILL.md`
- `.agents/skills/dtmapi-workshop-release-audit/scripts/test_subscription_package.ps1`
- `D:/OpenAI/CodexHome/skills/dtmapi-workshop-release-audit/`（用户级原目录已迁出）
- `docs/updates/2026/20260829-0002-repository-scoped-workshop-audit-skill.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- 使用 Skill Creator 的 `quick_validate.py` 验证仓库级目录，结果为 `Skill is valid!`。
- 校验用户级源目录已不存在，仓库级目标目录存在且位于 `E:/Python_project/DTMAPI` 内。
- 校验迁移后的 `scripts/test_subscription_package.ps1` SHA-256 与迁移前一致。
- 搜索确认仓库级 Skill 不再引用旧的用户全局目录，且没有关闭隐式调用的策略。
- 运行项目文档治理检查与 Git 空白错误检查。
- 未运行游戏、Runtime 或 Workshop 安装测试；本次变更仅改变 Codex Skill 的发现作用域和一条示例命令，Runtime 验证不需要。

## Evidence

- 仓库级 Skill：`.agents/skills/dtmapi-workshop-release-audit/SKILL.md`。
- 测试脚本：`.agents/skills/dtmapi-workshop-release-audit/scripts/test_subscription_package.ps1`。
- OpenAI Skill 文档：`https://learn.chatgpt.com/docs/build-skills`，其中仓库根目录 `.agents/skills` 属于 `REPO` 作用域，隐式调用默认开启。

## Rollback Notes

- 如需回退，先确认仓库级目录和用户级目标目录，再将该 Skill 目录完整迁回 `D:/OpenAI/CodexHome/skills/`，并把示例命令恢复为用户级路径。
- 回退会重新让无工作空间任务发现该 DTMAPI 专用 Skill；不要通过复制保留两份同名 Skill，否则选择器可能同时出现两项。

## Follow-Up

- Codex 会自动检测 Skill 变更；若已有任务仍显示启动时缓存的旧 Skill 列表，重启 Codex 或新建任务后再验证作用域。
