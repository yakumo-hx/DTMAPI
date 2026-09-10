# 诊断投影与 UI 可见行为

当前 UI 决策归 [Manager 设计](../../design/dtmapi-manager-ui-mvp.md)、对应产品与 Issue；Core 配置合同归 API owner。本页合并 2026 年 5–8 月的失败机制，不维护版本或完成清单。

## 一份结构化事实，多种呈现

早期 Manager 从 view model 到 provider、Status、Logs，沿既有 diagnostics/config/registry 取快照。Refresh 不应顺便导出报告；导出后再读快照；失败保留 last-good 并显示错误。severity 应使用结构化 code/exact status，不能因为 Reason 含 `error/blocked` 就改判。missing Hook、failed Hook、degraded feature 也不是同一状态。旧“所有 disabled 都 warning”只是当时模型，不能成为今天 UX 规则。

报告路径曾先 BuildSummary 后赋值，导致新 ZIP 指向上一报告；数轮 Oil 测试只有新日志，原记录正确拒绝借用旧 latest-report。这个赋值和目录问题可用导出 fixture 验证，不需要再跑 Camera/钓鱼。一个可读 DTO 或全局 Copy Summary 也不证明选中行复制、真实剪贴板或页面按钮可用。

6 月重复故障节流已经区分累计 FailureCount、当前 episode 和日志输出：外层节流后仍逐帧重写 HookStatus，照样制造重复日志。状态更新不能依赖日志；aggregate 总量和 retained 近期窗口各有用途，不应从自己导出的文本反推状态。故障注入覆盖重复异常、容量及恢复，正常游戏零错误样本没有再次覆盖 catch/cap 分支。行数有界还须限制外来字符串及对象图字节，具体预算由实现拥有。

来源：6 月 9–11 日 Manager/diagnostics Updates，定位见[全文迁移清单](../../archive/migrations/20260908-workspace.json)；[owner 生命周期](owner-lifetime.md)、[Manager 设计](../../design/dtmapi-manager-ui-mvp.md)。

## UI 状态和真实交互不同

HomePageUiState 可以在其它标题面板下仍存活，缓存存在不能证明当前可交互；blocking panel、modal 和输入 owner 才决定可见/可用。返回标题后 Unity 对象可能已销毁而托管引用未空，挂载需要判断实际对象并保持幂等。

[ISSUE-003](../../debug/issues/ISSUE-003-hotkey-openconfig-no-overlay.md) 中，F10 收到、API 请求成功仍没有 IMGUI。输入 fallback 修正不等于渲染修复；6 月 15 日已删除不再构造的备用 IMGUI。只在真的需要档内配置 UI 时重新沿现有 Canvas/title/debug host 设计，不能为满足旧 F8/F10 gate 复活它。

6 月 hover 标 dirty、重建销毁 button 的推断需要真实点击确认。晚期 Manager 已通过选择、分页、中文 detail，但其设计尾部还泛称 Mods details 为 Future Work；应核对当前实现后修原 owner 的旧投影。selected-row copy、Errors/Warnings、Hooks、Features 进一步详情仍各有独立范围，不能用全局 Copy Summary 或归档动作宣称完成。

截图只证明对应页面和时刻。早期 17 个 Mod 只显示 16 个是 first-N 限制，不能推断未发现；最终动物截图正确不能证明首次切换不闪。异步截图请求过近、overlay 被正常切换、旧 session receipt 或长健康行截断都曾让已通过行为被 aggregate 拒绝。修正所缺证据或工具路径，再补对应交互，不能用未发生的旧截图补 PASS。

来源：[6 月 A–F 人工审查](../../archive/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md)、[7 月 Manager 缺口审查](../../archive/reviews/code/2026/20260726-0001-five-commit-c1-manager-and-prerequisite-audit.md)、[后继真实交互](../../archive/updates/2026/20260726-0003-manager-gmcm-player-information-architecture.md)。

## 配置事务不承诺任意作者副作用可回滚

6 月 setter/save/reset 失败保留真实值与 pending 快照。早期 cancel 吞错后又被纠正为恢复失败保持 editing、阻止切页/关闭，让错误和 pending 可见；getter 失败不得半刷新。Unit 注入这些分支，成功标题 smoke 只补集成。作者 callback 的外部副作用必须另有合同，字段恢复不能推导万能事务。单纯拆 registry/page/items/transactions 曾复用既有 UI 证据。

公开 ConfigMenu API 保留注册、选项和冲突查询，编辑/保存/cancel/preview 属内部 runtime；读 DTO 不必再建立重复生命周期 facade。Hotkeys 聚合也只投影已有注册与配置事务，不生成第二份输入 owner。来源：6 月 8 日 ConfigMenu 拆分与 6 月 15 日配置事务 Updates；[Manager 信息边界](../../archive/reviews/code/2026/20260713-0006-manager-player-information-boundary-review.md)。

## Config 翻页的确定性根因与有限尾项

8 月 Next 已将 pageIndex 设为 1，RenderConfig 又无条件跟随 page 0 的 selected Mod，立即回弹；低分辨率放大 hit target/重叠问题，不是状态根因。新会话/显式请求/实际选中变化才跟随，普通 dirty render 保留浏览页；不能强行切 Mod 丢弃 pending 配置。Manager 的另一套分页不覆盖此处。

后继在 2560×1440 用六个临时 QA 页补足 16，真实 Next→选末页→Previous，六 owner 归零且未进档。旧 session receipt 造成的单次失败单独保留。逻辑与重叠已验，低分辨率物理手感仍查 [ISSUE-027](../../debug/issues/ISSUE-027-20260823-config-mod-list-pager-snapback.md)；不把这个尾项扩成存档/全部产品门。来源：[根因](../../archive/reviews/manual-qa/2026/20260823-0004-config-mod-list-pager-snapback.md)、[实施](../../updates/2026/20260823-0005-config-mod-list-pager-state-layout.md)。
