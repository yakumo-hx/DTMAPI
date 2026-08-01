# DTMAPI 项目简述

DTMAPI 是 Doloc Town ModdingAPI：面向 Windows 的、类似 SMAPI 的 Doloc Town 功能性 Mod API。当前定位是中期阶段：底层仍使用 BepInEx 做注入/启动，但通过 DTMAPI Installer、稳定 API、Mod 加载器、配置菜单、Hook 桥、日志诊断和创意工坊识别，把玩家和作者感知统一成 DTMAPI 生态。

本工作区已按“从零搭 DTMAPI”整理：旧 DLKsmapi 源码、旧发布包、旧本地 Mod 和临时目录没有复制；只带入 Doloc Town 官方说明/反编译参考、当前工作区已有第三方 Mod 样本、以及星露谷已安装 SMAPI 作为架构参考。

## 当前方向

- DTMAPI 不替代 Steam 创意工坊，而是让创意工坊获得功能性 Mod 能力。
- 首次安装可由 DTMAPI Installer 写入 BepInEx + DTMAPI Bootstrap；之后玩家仍应从 Steam 正常启动 Doloc Town。
- 功能性 Mod 应能从 Workshop Item 或本地 `Mods/` 目录被 DTMAPI manifest 识别。
- 官方/Steam 的 Mod 启用禁用状态应被尊重；DTMAPI 界面主要负责状态、配置、依赖错误、Hook 状态、日志和诊断。
- 公开 API 要稳定、Doloc 化且不暴露 raw Unity/Harmony/反编译游戏类型；原生实现按下述规范分类，不再无条件集中到 `DTMAPI.GameBridge.DolocTown`。

## Mod 身份与原生代码归属（规范）

本节是 DTMAPI Mod 身份术语和物理归属规则的唯一规范来源。其他当前文档应引用本节，不另行建立相互冲突的定义。该边界来自 [Batch 6 边界修正前置审查](docs/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md)；具体实现仍受后续准入门约束。

四种身份是：

- **Strict CodeMod**：DTMAPI 管理的默认 DLL Mod，只引用获准的稳定作者契约（当前为 `DTMAPI.Abstractions`），不直接引用 Unity、Harmony 或游戏程序集。
- **Advanced CodeMod**：显式声明的受管高级 DLL Mod；从 DTMAPI `Mods/` 发现并接受版本、依赖、排序、诊断、启停/重启和日志管理，同时可以为本产品引用 Unity、Harmony 或游戏程序集并拥有 `ProductNative` 实现。它不得安装到 `BepInEx/plugins`。精确准入产品集、策略和运行证据只由 [Batch 6 Managed Mod Identity Contract](docs/architecture/batch6-managed-mod-identity-contract.md) 及其链接收据维护；任何现有证明都不开放通用 Advanced 作者通道。
- **ContentPack**：没有代码 DLL 的声明式内容；规范上由其声明的可选 Content Host 发现、校验和管理，通用声明/加载绑定仍待 G7 实现。
- **External BepInEx Plugin**：第三方直接安装到 `BepInEx/plugins`、处于 DTMAPI Owner、依赖排序和启停承诺之外的外部插件。DTMAPI 最多提供只读诊断，不把它冒充受管 Mod。当前 `BepInEx/plugins/DTMAPI` 实际包含 one BepInEx plugin entry（Bootstrap）和 four co-located Runtime dependencies；后四个是 DTMAPI Runtime 依赖，不是 External，也不是受管产品 Mod。

另有一个仅供既有第三方包过渡的 Runtime 分类：**Third-party native compatibility CodeMod**。它不是可写入 `CodeModKind` 的第五种作者身份；仅当历史 `DtmMod` 省略 `CodeModKind` 时使用。此分类允许入口直接引用 Unity、Harmony、BepInEx、游戏程序集并携带私有辅助 DLL。DTMAPI 只承诺发现、依赖/顺序、冷启动调用、Entry 异常隔离、日志归属和重启提示；第三方作者自行承担 Hook、静态状态、存档副作用和清理，停用、退订或更新后必须重启，DTMAPI 不宣称已经热卸载未知 Hook。明确声明 `CodeModKind=Strict` 的包仍执行完整 Strict 引用/闭包限制；明确 Advanced 的第一方产品仍执行 receipt、owner 和生命周期门。未来形成完整第三方原生契约后可以收窄此过渡入口，但 0.5.5 不以尚未完成的统一规则禁用已可运行的 legacy Mod。

Batch 6 的精确实现、产品、策略、包、运行与兼容证据由上述契约及其链接的 Update/Review/收据拥有，不在本规范复制快速变化的数量或里程碑状态。Author SDK 的 `SDK160` 继续保护明确声明的 Strict；Advanced manifest、receipt 和 package 必须由 SDK 与受跟踪策略生成，不得手工伪造。任何新增产品、Content Host G7 与 0.5.5 发布仍须各自的有界授权；省略 `CodeModKind` 的 legacy 输入只表示第三方作者自行管理的兼容入口，不等于 SDK-verified Strict 或 receipt-verified Advanced。

代码的物理归属按以下顺序裁决：

- **Platform**：加载、版本、依赖、日志、配置、命令、数据目录、Owner 生命周期和管理 UI，归 Core、Abstractions、Bootstrap 或 ModConfigMenu。
- **SharedNative**：至少有两个彼此独立的真实消费者，并且共享同一 native owner、冲突点或全局生命周期不变量，才进入 `DTMAPI.GameBridge.DolocTown`。
- **ProductNative**：只有一个产品消费者，且表达该产品的状态机、补丁、缓存、动画或玩法规则，归该产品的 Advanced CodeMod。产品不得借其他产品或 synthetic PASS 扩张 mandatory GameBridge 或自行进入 Advanced 通道；精确产品归属仍查 Batch 6 契约。
- **ContentOwner**：稳定领域 schema、内容发现/校验和原生创建引擎，归可选 Content Host；具体内容仍由独立 ContentPack 拥有。

`public`、`internal`、friend assembly、provider、facade 或 demand route 只说明调用可见性，不证明物理归属。所有 SharedNative、ProductNative 和 ContentOwner 工作仍须先确认 native responsibility function 或状态持有者；稳定公共 API 只暴露 DTMAPI 自有 DTO、结果和适配契约。

## 游戏存档提交语义（规范）

本节是 DTMAPI 每存档玩法数据提交边界的唯一规范来源。玩家正常流程中，当天未保存的游戏变化会在返回标题、不保存退出、崩溃或强退后丢失；完整睡觉是普通玩家最常用的保存入口。反编译代码还存在睡眠界面的仅保存、按设置保存的小睡、部分时间跳过流程和诊断命令等官方 `DolocAPI.SaveGame` 调用，因此技术上的官方提交事实不是某一个 UI 按钮，而是 **native `SaveGame` 已成功**；DTMAPI 的正常进程内通知是只在原生成功后分发的 `SaveSaved`。

背包、装备、耐久、消耗、经济、任务和世界状态等 save-bound gameplay data，以及与它们对应的 sidecar，不得领先于同一存档的官方提交状态：

- `SaveLoaded` 建立最近一次官方提交的 `Committed` 状态；白天变化只形成进程内 `Working` 状态。
- `SaveSaving` 可以写入明确标为未提交、绑定存档身份且可回滚的候选或 journal，但不得把它当成新的正式玩法状态。
- 正常运行时，只有 native `SaveGame` 成功后分发的 `SaveSaved` 才能把同一事务的候选提升为新的 `Committed` 状态。
- native `SaveGame` 未成功或无法证明成功就返回标题、退出、崩溃、强退时，普通 `GameplayMutation` 必须丢弃或回滚到上一次 `Committed` 状态；journal 不能成为隐式自动保存或在未来保存时重放玩家已经放弃的操作。
- 原生保存已经成功、但进程在 `SaveSaved`/sidecar 提升前中断的窗口，可以用精确存档身份、原生提交指纹和 journal 恢复提升，但必须保持物品恰好一份。
- 禁用、卸载、退订和孤儿数据回收属于显式 `OwnerRecovery` / `OrphanRecovery` 管理事务，可以为防止物品永久不可访问而持久重试；它不得与普通玩法变更共用一个无类型的重放规则，也不得把同一会话尚未提交的 `GameplayMutation` 借停用转换为已提交状态。

配置、按键、日志、诊断、安装收据和非玩法的全局作者状态不受上述提交边界约束。任何产品若要采用不同于官方回档语义的玩法持久化，必须先有明确的产品决策和玩家可见说明，不能以“立即写盘更安全”为默认理由。调试用即时保存可以证明 Hook 和事务链路，但不能替代普通保存与未保存回滚验收。

## Codex 游戏测试规则

Codex 可以启动本地 Doloc Town、进入游戏并测试 Hook。除非任务另有说明，Hook 验证使用本地游戏第三个存档；已有领域专用 fixture 时遵循其权威 Review/Update，当前 AutoFishing 原生行为与 GC 验证使用第五个存档。

游戏测试必须先声明存档模式。普通功能、Hook、UI、标题循环、GC 和长测默认是 **NoNativeSave**：不得经过睡眠保存、仅保存/小睡保存、会保存的时间跳过、诊断 InstantSave、直接 `SaveGame` 或伪造 `SaveSaved`；测试前后默认只记录并比较目标 archive 的 current / prev / bak 文件与相关 committed sidecar 的 length/hash/mtime，并在任何测试框架或外部文件恢复前证明它们未变化。绿色 NoNativeSave 路径不得做例行字节备份或写回玩家 archive；只有显式说明高风险理由时才可准备应急快照，一旦需要恢复，该轮就是非验收失败，恢复结果不能冒充未保存回档证据。

只有明确验证 native 保存提交、存档复制/删除或启动迁移的测试才进入 **NativeSaveExpected / ArchiveMutation**。这类测试必须使用与玩家实时 Steam AutoCloud 隔离的可处置 fixture 或等效隔离；“游戏退出后再把旧文件复制回去”既不是验收证据，也可能与退出后开始的云同步竞争。Runtime lock、测试部署、profile、配置和非存档测试资产的恢复仍按各自协议执行；它们不因 NoNativeSave 而取消。

Hook 工作不能只靠编译通过判定完成，必须留下游戏证据：

- DTMAPI 启动日志。
- HookProbe/TestMod 日志证明 Hook 命中。
- 任务权威存档的 SaveLoaded 或等效证据：默认第三存档，当前 AutoFishing 原生行为/GC 使用第五存档。
- 退出后无残留 `DolocTown.exe`，且不复发 Steam“等待游戏退出”。
- 可用时收集日志或 report zip。

不要硬编码用户的 Steam/游戏路径；使用 local settings、环境变量或已有脚本。

## Debug 系统规则

DTMAPI 必须维护长期可读的 Mod Debug 系统：issue ledger、证据归档、hook map、smoke matrix、lessons learned。重复 bug 不能每次从零调查。

修改 runtime lifecycle、shutdown、BepInEx、Harmony patch、event dispatch、input、config menu、save/load、每存档 sidecar、玩法状态持久化、Workshop loading 或 mod loading 前：

1. 先读 debug/index 文档。
2. 先查已知问题，尤其是 Steam exit/stopping、卡顿、配置菜单、Hook 回归。
3. 修改前总结已知事实和已排除方向。
4. 每次修复后采集证据。
5. 失败方向不能删除，只能追加 dated notes。
6. 未经过 clean restart、游戏测试、日志和回归矩阵验证，不允许标 solved。

固定闭环：

```text
Review known issue -> minimal fix -> build/test -> enter authoritative save fixture -> verify hook/logs/exit -> record evidence -> update regression matrix
```

## 近期结构

- `AGENTS.md`: 给后续 Codex/开发者的强制上下文与边界。
- `docs/onboarding/current-state.md`: 给新 Codex 的当前事实路由页；不复制版本、分支、API、Issue 或 Smoke 状态，而是指向各自的权威来源。
- `docs/planning`: 原始需求与长文拆解。
- `docs/updates`: 可追溯更新记录；每次非平凡更新由一个 Update 承担实施生命周期，年度索引保存完整行，根索引只负责导航。
- `docs/reviews/api/native-owner-domains/INDEX.md`: 长期固定 native-owner 领域资料库；未来新增 API 或 GameBridge rebuild 前先查这里，把模糊需求落实到原生责任函数/状态 holder，再决定稳定 API 边界。
- `docs/reviews/api/smapi-ecosystem-map/INDEX.md`: clean-room SMAPI 生态语义/API 研究地图；未来设计事件、内容管线、UI/HUD、配置数据、跨 Mod API 等生态底座时先看这里，但它不提升任何 DTMAPI API 稳定级别。
- `references/doloc-town/official-workshop-docs`: 官方 Workshop/Modding 文档与更新说明。
- `references/doloc-town/reverse/builds`: Doloc Town 两个本地 build 的反编译研究数据。
- `references/stardew-smapi`: 星露谷已安装 SMAPI runtime 与 SMAPI 自带组件，仅作参考。
- `references/third-party-mods`: 当前工作区已有第三方 Doloc Town Mod 样本，仅作兼容性研究。
- `DTMAPI.BepInExBootstrap`: 当前唯一的 DTMAPI BepInEx plugin entry；它与四个 DTMAPI Runtime 依赖共址于 `BepInEx/plugins/DTMAPI`，受管产品 Mod 不在这里。
- `DTMAPI.Core`: manifest、依赖排序、Mod 加载、日志、配置、事件派发、错误隔离。
- `DTMAPI.Abstractions`: Mod 作者使用的稳定 API。
- `DTMAPI.GameBridge.DolocTown`: 已证明为 SharedNative 的游戏 Hook 与 Unity/Harmony/reflection 适配；不默认承载 Platform 或单产品实现。
- `DTMAPI.ModConfigMenu`: 内置声明式配置菜单 API。
- `tools/scripts`: build、install-to-game、run-game-smoke、run-hook-probe、collect-logs、status、package-report。
- `docs/debug`: issue ledger、evidence、protocols、smoke matrix、hook map。

中期成功标准：DTMAPI 能一次安装、识别已安装功能性 Mod、在界面展示状态和配置、尊重官方启停路径、允许 Codex 用默认第三存档或领域权威 fixture 进游戏测试真实 Hook，并保留足够 debug 证据，让后续 Codex 不再重复旧错误。
