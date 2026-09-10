# 20260831-0003：DTMAPI 原始规划文档退役与默认上下文收口

## Metadata

- Update ID: `20260831-0003`
- Date: `2026-08-31`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户确认采用审查建议，将过时的 `docs/planning/DolocTownModdingAPI.md` 从当前必读体系退役，同时保留原始历史证据、旧引用落点和机器契约连续性
- Review: [20260831-0001 原始规划文档退役审查](../../reviews/code/2026/20260831-0001-original-planning-document-retirement-review.md)

## Scope

- 完整保存 2026-06-01 原始规划正文，但把它移入冻结历史目录并增加只读前页。
- 把旧路径缩成 current-owner 交接页，以兼容 33 个跟踪引用和冻结 G2 治理路径。
- 从默认设计/代码上下文移除旧规划，以 compact current-state router 替代。
- 修正 planning router 的隐式活跃规则。
- 扩展文档治理规范与检查器，阻止退休 planning 文档重新进入默认必读上下文，并对交接页大小和冻结正文哈希做回归保护。
- 不修改任何历史 Goal、Review、Update、Debug 内容，不改变 Runtime、API、产品、发布或游戏行为。

## Changed Files

- `AGENTS.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/archive/20260601-dtmapi-original-planning-transcript.md`
- `docs/planning/README.md`
- `docs/workflows/document-governance.md`
- `tools/scripts/check-doc-governance.ps1`
- `docs/reviews/code/2026/20260831-0001-original-planning-document-retirement-review.md`
- `docs/updates/2026/20260831-0003-original-planning-document-retirement.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- `tools/scripts/check-doc-governance.ps1`: PASS, `7,429` checks.
- PowerShell parser check for `check-doc-governance.ps1`: `0` parse errors.
- Frozen-body comparison against `HEAD:docs/planning/DolocTownModdingAPI.md`: original `903` lines, archived body `903` lines, `0` differences after reversing only the two required relative-link depth changes.
- Frozen transcript normalized SHA-256: `651aa95b4534b33a092525302c9aab1edcb963c2b388ba2e615118d69262a55b`, equal to the checker manifest.
- Superseded handoff size: `2,469` bytes, below the enforced `8,192`-byte ceiling.
- Default-context parser resolves exactly `PROJECT.md`, `docs/onboarding/current-state.md`, `docs/planning/Debug.md`, `references/README.md`, and `docs/debug/INDEX.md`; the retired planning path is absent.
- `git status --short -- docs/goals/2026`: empty; historical Goal files were not rewritten.
- Focused `git diff --check` for the tracked governance files: PASS; output contains only expected Git line-ending notices.
- Runtime/game validation is not required because no runtime, package, Hook, API or gameplay file changes.

## Evidence

- 退役审查记录原文件 42,248 字节、903 行、891 行初始基线以及 33 个跟踪引用的分类。
- 冻结全文规范化 SHA-256 目标：`651aa95b4534b33a092525302c9aab1edcb963c2b388ba2e615118d69262a55b`。
- 原路径保留 `superseded` 交接页，因此历史链接和 G2 路径无需重写。
- 治理检查项由退役前的 7,260 增至 7,429，新增检查覆盖冻结正文、交接页大小、默认上下文定位和退休 planning 拒绝规则。

## Rollback Notes

- 若退役方案需要回滚，将冻结正文移回原路径，恢复 `AGENTS.md` 与 planning router 的旧入口，并移除新增哈希/默认上下文检查；不要改写历史 Goal、Review、Update 或 G2 契约来掩盖回滚。
- 若仅路由文字有问题，修正交接页和当前治理规则即可；冻结原文需要变更时必须同时显式更新其治理哈希。

## Follow-Up

- 单独审计 `docs/planning/Debug.md`；本次不因相似外观自动退役它。
- 逐份审核其余 planning/research/roadmap 文档的 lifecycle、当前 owner 和路由必要性，不以本次单文件结论批量推断。
- 若后续形成统一 planning metadata schema，应沿用本次 current-owner / frozen-archive / stable-stub 边界，不建立第二套事实状态系统。
