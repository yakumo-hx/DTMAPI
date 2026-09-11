# 20260911-0007: 0.7.0 公开源码边界与持续导出设计

## Metadata

- Update ID: `20260911-0007`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求在当前共享目录细化其他开发者支持计划的事项 2；本次设计兼作专项 Review，归[公开源码交付设计](../../reviews/code/2026/20260911-0003-public-source-delivery-design.md)。不新建工作树，不实施导出工具、Linux、Runtime/API 或远端操作。

## Summary

核对准确提交、公开路径和实际构建/测试输入，设计固定规则、Git 对象导出、E/P/N 三方更新、贡献回流以及公开 Windows 验证边界。本记录完成只代表设计交付；工具和首次公开导出仍待实施。源码、生产脚本、platform-next 主线和原验收产物均未因本任务改动。

## Changed Files

- [专项设计](../../reviews/code/2026/20260911-0003-public-source-delivery-design.md)及[规则草案](../../reviews/code/2026/20260911-public-source-delivery/export-policy.draft.json)：路径决策、许可缺口、流程接口、失败边界、实施与验收。
- `docs/planning/README.md` 和 `docs/planning/contributor-support.md`：事项 2 的详细设计入口。
- 本 Update 与月表投影行。

## Validation

- PASS：规则 JSON、唯一规则 ID/精确路径/映射目标、当前 HEAD 全部 4,432 个跟踪路径分类，无未分类项或缺失的现有必需路径。第一次检查发现 `game-smoke/README.md` 尚未分类，已补明确内部说明排除后通过；没有修改被检查的源码。
- PASS：有界独立 Git fixture 的显式基线、双方新增、同行冲突、无争议删除、modify/delete、rename/edit、两轮纯导出基线保留公开文件、错误基线的负控制和失败 CAS 行为。共 8 个例子，证明范围仅限 Git 原语，不是生产 exporter 验收。
- PASS：本次新增正文的本地链接；修正编号冲突后 `check-doc-governance.ps1 -Quiet` 通过，月表由同步脚本生成。最终差异检查仅覆盖本任务文件及共享文件中的本次新增行。
- 已知未实施项：4 个根映射源尚不存在；2 个 binary record 为 hold，Doorstop 对应源码摘要为 null。草案必须拒绝正式 Export，不能将这些预期缺口报作公开交付通过。
- 首次月表/治理检查发现并行地图实验也创建了编号 0006；本设计改用空闲编号 0007，保留对方 Update 及月表行。该轮失败属于编号冲突，不涉及产品测试。
- 不执行：生产构建/完整测试、真实游戏、SDK 重包、远端写入。

## Evidence

- 源码观察基线：`cc7044a79ffd432ab2428d3d7e8f0d45b0a19d37`，分支 `codex/workspace-construction-20260908`；工作树包含未提交的 SDK/MSBuild 及其他任务变更。
- 既有 D7/r6/r2 的验收仍归 [20260910-0012](20260910-0012-sdk-msbuild-first-release.md)，本次不重开或复制验收。
- 临时 Git 例子结果：`C:/Users/ADMINI~1/AppData/Local/Temp/dtmapi-public-source-design-tqpn8znx/result.json`；Git `2.54.0.windows.1`。只创建了独立合成对象库，没有 worktree、分支切换或真实远端操作；临时结果不参与长期同步状态。
- 许可判断使用准确 BepInEx tag 及其引用的上游许可原文，链接和未完成身份核对均在专项设计。未下载或重新封装第三方载荷。

## Rollback Notes

逐项撤回本次新增设计、规则及新增导航行；保留其他任务对同一规划索引、支持计划和月表的变更。不重置目录或恢复整份共享文件。

## Follow-Up

按设计的实施分解完成源码归集、许可补件、公开入口调整、导出和三方同步工具；本次不执行这些后续工作。
