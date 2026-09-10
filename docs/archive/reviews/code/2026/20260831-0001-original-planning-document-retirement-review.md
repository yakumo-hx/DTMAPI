# DTMAPI 原始规划文档退役审查

- Date: `2026-08-31`
- Status: `recorded`
- Area: `governance/docs/planning/default-context/history-retention`
- Source request: 用户要求先审核 2026-06-01 手写/对话式 `docs/planning/DolocTownModdingAPI.md` 是否仍有留存必要；审核结论获用户确认后，按“移出当前上下文、保留历史证据与旧路径”的方案实施
- Current authorities: [`PROJECT.md`](../../../../../PROJECT.md), [current-state router](../../../../onboarding/current-state.md), [document governance](../../../../workflows/document-governance.md)
- Resolution: [Update 20260831-0003](../../../updates/2026/20260831-0003-original-planning-document-retirement.md)

本记录只保存退役前的事实、风险、选项与验收门。文件变更、最终验证和回滚
属于 owning Update。

## 1. 结论

原文有原始产品愿景、clean-room 设计演变和早期决策来源价值，但没有继续
充当当前需求、架构或默认必读上下文的必要。采用“冻结全文 + 原路径兼容
交接页”，不采用继续活跃维护或直接删除。

## 2. 退役前事实

- 原文件为 42,248 字节、903 行；一次完整读取约产生 10,580 个工具输出 token。
- `git blame` 显示 891/903 行来自 2026-06-01 初始基线；其余仅为 2026-06-13、2026-07-20 和 2026-07-21 的 12 行局部修正。
- 正文混合用户最初提问、当时的长篇建议、阶段任务示例、旧 `/goal` 建议、外部网页引用和后补纠偏，不是持续维护的规范文档。
- `AGENTS.md` 把它列在所有设计/代码工作的默认必读列表；`docs/planning/README.md` 又把未冻结条目统一视为需求来源，因此新任务会同时读入旧表述和当前权威。
- 当前 `check-doc-governance.ps1` 在退役前通过 7,260 项检查，说明问题是生命周期/路由语义缺口，不是现有链接或索引格式故障。

## 3. 已失效或高风险表述

- 原始请求和功能矩阵仍写“只支持 Windows”，与当前两个物理 Runtime Workshop 分发及实验多平台宿主边界不一致。
- 多处把玩家 Mod 发现写成 `<game>/Mods`，而当前玩家 Runtime 来源只允许官方 `Local.*` 与原生订阅快照证明的 `Workshop.*`；历史目录只剩受限恢复兼容。
- 旧正文以普遍 GameBridge 集中化组织原生代码；一段后补说明不足以替代 `PROJECT.md` 的 Platform / SharedNative / ProductNative / ContentOwner 唯一规范。
- `docs/hook-map.md`、`scripts/`、固定 `0.0.x -> 0.4` 路线、旧 Manager 启停设想和旧 Codex 工作流均已被当前目录、产品边界、Update、Hook、API 与验证体系取代。
- 原文缺少后来建立的存档提交语义、Runtime lock、Advanced 准入与 Catalog/订阅正交边界、文档事实所有权和 assurance proportionality 规则。

## 4. 引用拓扑

退役前 `git grep` 找到 33 个跟踪文件引用旧路径：

- 2 个当前入口：`AGENTS.md` 与 `docs/planning/README.md`；
- 12 个历史 Goal 文件；
- 11 个 Review；
- 5 个 Update；
- 1 个 Debug 历史记录；
- 2 个 G2 机器契约/校验脚本。

因此直接删除旧路径会破坏审计导航，并让冻结 G2 治理路径不再对应实际文件。
保留一个小型交接页可以维持所有旧引用，同时把全文移出默认上下文。

## 5. 当前事实接管

| 旧文档主题 | 当前所有者 |
| --- | --- |
| 项目方向、身份、物理归属、存档提交语义 | `PROJECT.md` |
| 当前事实路由 | `docs/onboarding/current-state.md` |
| 安装器与 Workshop 包边界 | `docs/architecture/runtime-workshop-installer-boundary.md` 及其测试工作流 |
| Advanced/ProductNative 准入 | Catalog 生成的 managed-product admission registry |
| 当前公开/订阅与发布事实 | Product Catalog、current subscription manifest 及其指向的发布 Update |
| 公共 API 状态 | `docs/api/public-api-matrix.md` |
| Hook、Issue 与运行证据 | focused Hook map、Debug issue、active smoke matrix |
| 文档生命周期与事实所有权 | `docs/workflows/document-governance.md` |

## 6. 选定方案

1. 把完整正文移到 `docs/planning/archive/`，增加冻结前页并保护其规范化文本哈希。
2. 在原路径保留小于 8 KiB 的 `superseded` 交接页，解释旧引用为何保留并只路由当前所有者。
3. 从 `AGENTS.md` 默认必读移除旧规划，以 compact current-state router 替代。
4. 修正 planning router，禁止由目录位置或“没有 frozen 标签”反推当前权威。
5. 在文档治理规范与检查器中加入通用门：默认设计/代码上下文不得包含前页声明 frozen、superseded 或 historical 的 planning 文档。
6. 不重写任何历史 Goal、Review、Update、Debug 或冻结 G2 契约。

## 7. 未采用方案

- **继续原地活跃维护**：每次边界变化都要在 903 行对话里继续打补丁，会形成第二套事实所有权。
- **只在正文顶部加警告**：虽然能减少误读，但全文仍被默认加载和全文搜索命中，不能解决上下文污染。
- **直接删除或只依赖 Git 历史**：会破坏 33 个跟踪引用的稳定落点，并削弱没有旧 commit 上下文时的审计可读性。

## 8. 验收门

- 旧路径存在、标记 `superseded`、体积小于 8 KiB，并链接冻结全文和当前所有者。
- 冻结全文完整保留原 903 行正文，移动后相对链接有效，规范化 SHA-256 受治理检查器保护。
- 默认必读区不再包含旧路径，并由通用检查防止退休 planning 文档重新进入。
- planning router 不再把“非 frozen”作为自动需求准入。
- 历史引用和 G2 路径不被批量改写。
- 聚焦文档治理检查通过；不要求 Runtime 或游戏验证。
