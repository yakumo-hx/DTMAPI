# DTMAPI 项目简述

DTMAPI 是 Doloc Town ModdingAPI：面向 Windows 的、类似 SMAPI 的 Doloc Town 功能性 Mod API。当前定位是中期阶段：底层仍使用 BepInEx 做注入/启动，但通过 DTMAPI Installer、稳定 API、Mod 加载器、配置菜单、Hook 桥、日志诊断和创意工坊识别，把玩家和作者感知统一成 DTMAPI 生态。

本工作区已按“从零搭 DTMAPI”整理：旧 DLKsmapi 源码、旧发布包、旧本地 Mod 和临时目录没有复制；只带入 Doloc Town 官方说明/反编译参考、当前工作区已有第三方 Mod 样本、以及星露谷已安装 SMAPI 作为架构参考。

## 当前方向

- DTMAPI 不替代 Steam 创意工坊，而是让创意工坊获得功能性 Mod 能力。
- 首次安装可由 DTMAPI Installer 写入 BepInEx + DTMAPI Bootstrap；之后玩家仍应从 Steam 正常启动 Doloc Town。
- 功能性 Mod 应能从 Workshop Item 或本地 `Mods/` 目录被 DTMAPI manifest 识别。
- 官方/Steam 的 Mod 启用禁用状态应被尊重；DTMAPI 界面主要负责状态、配置、依赖错误、Hook 状态、日志和诊断。
- 公开 API 要稳定、Doloc 化；脆弱的 Unity/Harmony/reflection Hook 只能集中在 `DTMAPI.GameBridge.DolocTown`。

## Codex 游戏测试规则

Codex 可以启动本地 Doloc Town、进入游戏并测试 Hook。除非任务另有说明，Hook 验证使用本地游戏第三个存档。

Hook 工作不能只靠编译通过判定完成，必须留下游戏证据：

- DTMAPI 启动日志。
- HookProbe/TestMod 日志证明 Hook 命中。
- 第三个本地存档的 SaveLoaded 或等效证据。
- 退出后无残留 `DolocTown.exe`，且不复发 Steam“等待游戏退出”。
- 可用时收集日志或 report zip。

不要硬编码用户的 Steam/游戏路径；使用 local settings、环境变量或已有脚本。

## Debug 系统规则

DTMAPI 必须维护长期可读的 Mod Debug 系统：issue ledger、证据归档、hook map、smoke matrix、lessons learned。重复 bug 不能每次从零调查。

修改 runtime lifecycle、shutdown、BepInEx、Harmony patch、event dispatch、input、config menu、Workshop loading 或 mod loading 前：

1. 先读 debug/index 文档。
2. 先查已知问题，尤其是 Steam exit/stopping、卡顿、配置菜单、Hook 回归。
3. 修改前总结已知事实和已排除方向。
4. 每次修复后采集证据。
5. 失败方向不能删除，只能追加 dated notes。
6. 未经过 clean restart、游戏测试、日志和回归矩阵验证，不允许标 solved。

固定闭环：

```text
Review known issue -> minimal fix -> build/test -> enter Doloc Town third save -> verify hook/logs/exit -> record evidence -> update regression matrix
```

## 近期结构

- `AGENTS.md`: 给后续 Codex/开发者的强制上下文与边界。
- `docs/onboarding/current-state.md`: 给新 Codex 的当前状态入口，标出活跃事实、历史证据、实验 API 和分支纪律。
- `docs/planning`: 原始需求与长文拆解。
- `docs/updates`: 可追溯更新记录；每次非平凡更新记录目标、改动文件、验证、证据、关联 debug/hook/smoke/API 项和回滚说明。
- `docs/reviews/api/native-owner-domains/INDEX.md`: 长期固定 native-owner 领域资料库；未来新增 API 或 GameBridge rebuild 前先查这里，把模糊需求落实到原生责任函数/状态 holder，再决定稳定 API 边界。
- `docs/reviews/api/smapi-ecosystem-map/INDEX.md`: clean-room SMAPI 生态语义/API 研究地图；未来设计事件、内容管线、UI/HUD、配置数据、跨 Mod API 等生态底座时先看这里，但它不提升任何 DTMAPI API 稳定级别。
- `references/doloc-town/official-workshop-docs`: 官方 Workshop/Modding 文档与更新说明。
- `references/doloc-town/reverse/builds`: Doloc Town 两个本地 build 的反编译研究数据。
- `references/stardew-smapi`: 星露谷已安装 SMAPI runtime 与 SMAPI 自带组件，仅作参考。
- `references/third-party-mods`: 当前工作区已有第三方 Doloc Town Mod 样本，仅作兼容性研究。
- `DTMAPI.BepInExBootstrap`: 唯一放进 `BepInEx/plugins` 的 DTMAPI 插件。
- `DTMAPI.Core`: manifest、依赖排序、Mod 加载、日志、配置、事件派发、错误隔离。
- `DTMAPI.Abstractions`: Mod 作者使用的稳定 API。
- `DTMAPI.GameBridge.DolocTown`: 所有游戏 Hook 与 Unity/Harmony/reflection 适配。
- `DTMAPI.ModConfigMenu`: 内置声明式配置菜单 API。
- `tools/scripts`: build、install-to-game、run-game-smoke、run-hook-probe、collect-logs、status、package-report。
- `docs/debug`: issue ledger、evidence、protocols、smoke matrix、hook map。

中期成功标准：DTMAPI 能一次安装、识别已安装功能性 Mod、在界面展示状态和配置、尊重官方启停路径、允许 Codex 用第三个存档进游戏测试真实 Hook，并保留足够 debug 证据，让后续 Codex 不再重复旧错误。
