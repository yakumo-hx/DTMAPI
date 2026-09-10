# 20260831-0004：新任务上下文路由与根 Agent 规则收口

## Metadata

- Update ID: `20260831-0004`
- Date: `2026-08-31`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求先实施上下文/Skill 审计建议的第 1、2 项：让历史 Debug 规划退出通用必读，并精简根 `AGENTS.md`
- Review: [20260831-0005 新任务工作空间必读上下文与 Skill 路由审计](../../reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md)

## Scope

- 默认设计/代码上下文只保留 `PROJECT.md` 与 `docs/onboarding/current-state.md`。
- `references/README.md` 只在 reverse、官方资料、第三方 Mod 或 SMAPI 研究时读取；`docs/debug/INDEX.md` 只在 Runtime/Debug/重复缺陷等匹配任务读取。
- 把 `docs/planning/Debug.md` 缩成 superseded 稳定路径交接页，完整保留 2026-06-13 原始规划/对话于冻结 archive。
- 把根 `AGENTS.md` 从重复的领域正文收束为硬安全边界和 task-to-owner 路由，不改变 `PROJECT.md` 拥有的身份、物理 owner 与 native-save 语义。
- 扩展文档治理检查，固定两份默认必读、条件 references/debug 路由、Debug 交接页大小和冻结原文哈希。
- 不修改 Runtime、API、产品、Skill、Workshop 包、游戏目录、Official MODS 或玩家存档。

## Changed Files

- `AGENTS.md`
- `docs/planning/Debug.md`
- `docs/planning/archive/20260613-dtmapi-debug-system-planning-transcript.md`
- `docs/planning/README.md`
- `tools/scripts/check-doc-governance.ps1`
- `docs/reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md`
- `docs/updates/2026/20260831-0004-new-task-context-routing-and-agent-rules-slimming.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `tools/scripts/check-doc-governance.ps1`: PASS, `7,484` checks. The checker now requires exactly `PROJECT.md` and `docs/onboarding/current-state.md` in default context, plus conditional reference and Debug routes.
- PowerShell parser check for `tools/scripts/check-doc-governance.ps1`: `0` parse errors.
- Frozen archive body comparison against `HEAD:docs/planning/Debug.md`: `280/280` logical lines and `0` differences.
- Frozen archive normalized SHA-256: `7c4be54cd41718e6fff160dff09dd623c400f597fc065b267c53819d9d5432a4`, equal to the checker manifest.
- Focused Markdown-link resolution: `0` missing links.
- Focused trailing-whitespace/conflict-marker scan: `0` issues outside the byte-preserved frozen transcript; focused tracked `git diff --check`: PASS with only expected line-ending notices.
- `tools/scripts/check-test-artifact-governance.ps1`: PASS.
- Runtime/game validation is not required because this change does not touch Runtime, package, Hook, API or gameplay behavior.

## Evidence

- Pre-change root `AGENTS.md`: 16,720 bytes and 160 lines.
- Current compact root `AGENTS.md`: 9,089 bytes and 89 lines, a 7,631-byte (45.6%) reduction while retaining hard source, ownership, toolchain, save and shared-runtime boundaries.
- Pre-change `docs/planning/Debug.md`: 16,051 bytes; the compatibility handoff is 2,291 bytes and 32 lines.
- Frozen Debug archive normalized SHA-256 target: `7c4be54cd41718e6fff160dff09dd623c400f597fc065b267c53819d9d5432a4`.
- Historical backlinks continue to resolve through the unchanged `docs/planning/Debug.md` path.
- Official Codex discovery reference: [Custom instructions with AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

## Rollback Notes

- Restore the original Debug body from the frozen archive to `docs/planning/Debug.md`, revert the planning router/checker entries, and restore the prior root `AGENTS.md` if the route change must be rolled back.
- Do not rewrite historical Goal/Review/Update backlinks; the stable Debug path is deliberately preserved.
- If only one conditional route is defective, correct `AGENTS.md` and its focused checker without unfreezing the transcript.

## Follow-Up

- The audit's Skill cleanup, `PROJECT.md` volatile release-state cleanup, and API/installer document progressive-disclosure work remain separate, unimplemented scopes.
- After future cold-start use, revise a conditional signal only if a real task misses or falsely matches its canonical owner; do not re-expand the root file with copied domain policy.
