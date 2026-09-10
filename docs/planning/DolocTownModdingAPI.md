# DTMAPI 原始规划交接页

- Lifecycle Status: `superseded`
- Document Role: 保留旧路径的兼容交接页；不是当前需求、架构或实施权威
- Superseded on: `2026-08-31`
- Frozen source: [2026-06-01 原始规划对话](archive/20260601-dtmapi-original-planning-transcript.md)
- Retirement review: [20260831-0001](../archive/reviews/code/2026/20260831-0001-original-planning-document-retirement-review.md)
- Owning Update: [20260831-0003](../archive/updates/2026/20260831-0003-original-planning-document-retirement.md)

这一路径仍被历史 Goal、Review、Update、Debug 记录以及冻结的 G2
治理路径引用，因此保留为稳定交接入口。原始 903 行对话已移入冻结历史区，
不再进入新任务的默认上下文，也不再接收当前实现进度。

## 当前权威路由

- 项目范围、稳定身份、物理所有权和原生存档提交语义：[`PROJECT.md`](../../PROJECT.md)
- 当前事实如何定位：[`docs/onboarding/current-state.md`](../onboarding/current-state.md)
- 文档生命周期与事实所有权：[`docs/workflows/document-governance.md`](../workflows/document-governance.md)
- Runtime Workshop 安装器与包边界：[`runtime-workshop-installer-boundary.md`](../architecture/runtime-workshop-installer-boundary.md)
- 精确受管 Advanced/ProductNative 准入：[`managed-product-admission-registry.md`](../architecture/managed-product-admission-registry.md)
- 当前公开/订阅集合与发布事实：[`Product Catalog`](../../tools/release/dtmapi-product-catalog.json)、[`current subscription manifest`](../../tools/release/current-subscription-manifest.json) 及其指向的最新发布 Update
- 公共 API 状态：[`public-api-matrix.md`](../api/public-api-matrix.md)
- Hook、Debug 与运行证据：[`Hook Map`](../hook-map/README.md)、[`Debug Index`](../debug/INDEX.md) 和 active smoke matrix

## 历史材料的使用边界

冻结原文只用于追溯最初产品愿景、当时的候选架构、早期 SMAPI/BepInEx
类比以及后续边界为何发生变化。它包含已经失效的 Windows-only、`Mods/`
发现路径、旧目录名、旧版本路线、旧 GameBridge 集中化表述和当时的 Codex
工作流建议；任何一句都不能绕过上述当前权威重新开启实施。

后续若要恢复其中某个主题，先从相应当前事实所有者建立新的有界 Review，
实施时再建立一个新的 Update。不要修改冻结原文来表达今天的状态。
