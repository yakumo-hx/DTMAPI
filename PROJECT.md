# DTMAPI 项目简述

DTMAPI 是 Doloc Town ModdingAPI。BepInEx 负责注入/启动，DTMAPI 提供受管 Mod 加载、稳定作者 API、配置界面、Hook、日志诊断和官方来源识别。多个物理 Runtime 分发投影同一个 `DTMAPI.Runtime` 身份；产品、订阅和发布事实查 [Product Catalog](tools/release/dtmapi-product-catalog.json)、[subscription manifest](tools/release/current-subscription-manifest.json)及其最新发布 Update。

## 当前方向

- 从零重建，不使用旧 DLKsmapi 实现。官方反编译、Workshop 资料、第三方 Mod 和 SMAPI 只作参考；不分发官方程序集、反编译源码、提取资产或未经许可的第三方代码。
- 玩家从 Steam 正常启动游戏。受管产品只从官方 `Local.*` MODS 和当前原生订阅快照证明的 `Workshop.*` 目录发现，并尊重官方启停状态。历史 `<game>/Mods` 不参与发现；旧 SDK 部署 journal 仅保留 status/recover/withdraw 和选择清理兼容，不能作为可加载/发布来源。
- Runtime 安装器边界由 [installer architecture](docs/architecture/runtime-workshop-installer-boundary.md)维护；普通产品 Mod 修改不触发整套 Runtime 安装器矩阵。
- 平台演进沿 [platform-next](docs/planning/platform-next/README.md)的任务队列和指定复盘点推进，内部可逆实施及必要测试已获授权；计划不是当前能力，也不授权 Workshop 上传。

## Mod 身份与原生代码归属（规范）

本节是身份和物理归属的唯一规范来源。

| 身份 | 契约 |
| --- | --- |
| Strict CodeMod | 默认受管 DLL Mod，只引用获准稳定作者契约（当前为 `DTMAPI.Abstractions`），不直接引用 Unity、Harmony、BepInEx 或游戏程序集。 |
| Advanced CodeMod | 显式受管高级 DLL Mod，可为自身产品引用原生类型并拥有 ProductNative；接受来源、版本、依赖、排序、诊断、Owner 生命周期和重启管理。旧 receipt 通道的精确准入由 [生成的 registry](docs/architecture/managed-product-admission-registry.md)拥有。当前 Runtime 0.7.0 的 [NativeContractVersion=2 通道（保留 V1）](author-sdk/NATIVE-REFERENCES.md)允许任意合法作者 ID，由 SDK 从本机宿主元数据生成引用、必需签名及包绑定，Core/Doctor 加载前校验；无需第一方 Catalog 行。两个通道均禁止手写准入凭证或把原生/平台 DLL 打包，不绕过 Strict 的 SDK160。 |
| ContentPack | 不含代码 DLL 的声明式内容，由可选 Content Host 发现、校验和管理；通用声明/加载绑定仍待相应任务实现。 |
| External BepInEx Plugin | 第三方直接置于 `BepInEx/plugins`，不受 DTMAPI Owner、依赖排序和启停承诺管理，只作只读诊断。 |

受管 Mod 不放进 `BepInEx/plugins`。其中 `DTMAPI` 子目录只有一个 Bootstrap 插件入口及四个共址 Runtime 依赖；这些依赖不是 External 或受管产品 Mod。

历史 DtmMod 省略 `CodeModKind` 时可进入 **Third-party native compatibility CodeMod** 分类；它不是第五种可声明身份。入口可以引用 Unity/Harmony/BepInEx/游戏并携带私有 DLL。DTMAPI 只承诺发现、依赖/顺序、冷启动 Entry、异常隔离、日志归属和重启提示；第三方自行承担 Hook、静态状态、存档副作用和清理。停用、退订或更新后必须重启，不承诺热卸载未知 Hook。明确 Strict 仍执行 Strict 引用闭包；明确 Advanced 根据 NativeContractVersion=1/2 或缺省字段分别执行开放 provenance 或旧 receipt 门，并共同接受 owner/lifecycle 管理。未知或错误的新格式拒绝加载，不能退回 legacy。

准入、公开/订阅、安装/启用、加载/行为、再次上传授权是不同事实：registry 只投影 Catalog 准入；subscription manifest 只证明捕获时成员关系和 installed manifest；最新已发布 Update 来自其 `authority.latestReleaseUpdate`；未来上传授权由 Catalog `releaseStop` 单独管理。目录存在、PublishedProduct 标记或旧 smoke 不能推导其他事实。新增产品/Host/发布须有各自范围，冻结历史 annex 不拥有当前集合。

原生工作先找到责任函数或状态 holder，再判定物理归属：

- **Platform**：加载、依赖、日志、配置、命令、数据目录、Owner 生命周期、管理 UI，归 Core/Abstractions/Bootstrap/ModConfigMenu。
- **SharedNative**：至少两个独立真实消费者共享同一 native owner、冲突点或全局生命周期不变量，才进入 GameBridge。
- **ProductNative**：单产品状态机、补丁、缓存、动画和玩法，归其已准入 Advanced CodeMod；未来复用、synthetic PASS、provider/facade/friend/public/internal 可见性都不能创造 SharedNative 或准入。
- **ContentOwner**：稳定 schema、内容发现/校验和原生创建引擎归可选 Host；具体内容归独立 Pack。

公共 API 只暴露 DTMAPI 自有 DTO、结果及适配契约，不暴露 raw Unity/Harmony/BepInEx/游戏类型。

## 游戏存档提交语义（规范）

**游戏进展只有在原生 SaveGame 成功后才会保存。** 普通未保存变化会在返回标题、不保存退出、崩溃或强退后丢失；睡觉是常用入口，但仅保存、按设置保存的小睡、部分时间跳过和诊断命令也可能调用保存。跨日、昏倒、UI 关闭或时间经过本身不能证明已保存；确认原生调用结果。

背包、装备、耐久、消耗、经济、任务、世界状态及对应 sidecar 属于 save-bound gameplay data，不能领先于同档官方提交：

1. `SaveLoaded` 建立最近的 `Committed`；会话变化只形成 `Working`。
2. `SaveSaving` 可写绑定存档身份、可回滚且明确未提交的 candidate/journal。
3. 正常进程内只有原生保存成功后的 `SaveSaved` 可提升同一事务为 `Committed`。
4. 未证明原生保存成功时，普通 `GameplayMutation` 回滚/丢弃到上次提交；不能借 journal 自动保存或在未来重放已放弃操作。
5. 原生已提交但 sidecar 未提升的中断窗口，可凭精确身份、原生提交指纹和 journal 恢复，保持物品恰好一份。
6. 禁用、卸载、退订、孤儿回收是显式 `OwnerRecovery` / `OrphanRecovery` 管理事务，可持久重试；不得把未提交玩法变更借此提交，或与玩法操作共用无类型重放。

配置、按键、日志、诊断、安装收据和非玩法全局作者状态不受玩法提交约束。偏离官方回档语义须有明确产品决策和玩家说明，不能默认“立即写盘更安全”。

## Codex 游戏测试规则

普通功能、Hook、UI、标题循环和性能测试默认 **NoNativeSave：在现有环境进游戏、验证、退出**。不例行复制存档、不为测试重新搭建隔离环境、不备份后写回。禁止所有原生保存入口；前后仅比较目标 archive 的 current/prev/bak 和相关 committed sidecar 的 length/hash/mtime，在任何清理/恢复前证明未变化。应急快照必须有具体高风险理由；一旦需要恢复，该轮不是未保存验收 PASS。

需要保存或删建档才使用 **NativeSaveExpected / ArchiveMutation**：
- 用户已明确指定可处置测试槽，允许对应保存/删建操作并接受最终状态时，可直接原地测试，不再要求为同一槽复制隔离副本或恢复旧档。授权只覆盖该槽及其产品数据，不扩展到其他档或修改云同步设置。
- 故障注入、强杀保存窗口、跨档/启动迁移、不能证明写入只限该槽、或需保护原有数据时，使用与实时 Steam AutoCloud 隔离的可处置 fixture。一次准备后沿必要阶段复用，不每轮重建。
- 专项自动 runner 的 fixture 断言只约束其场景；普通产品验收可用直接游戏操作和日志。不得为迎合旧 runner 而扩大测试范围。

默认第三存档；Y 控制台第十 UI 槽（`-SaveSlot 10`，原生 index 9）；AutoFishing 原生行为/GC 第五槽。任务明确指定的专用槽优先。

构建不能证明玩家行为。实测留下实际产品来源、目标存档、对应行为/Hook、日志或必要截图，并确认干净退出、无残留 DolocTown.exe/Steam 等待退出。产品日志可证明 Hook，不强制安装 HookProbe。先取共享 Runtime 锁，结束释放；部署/profile/配置等有意改变的非存档资产仍需恢复。临时产物沿现有托管会话协议，dump 仅在需要时启用。

具体测试前提、触发条件、停止规则及保存入口证据区分，见 [产品修改验证](docs/workflows/product-change-validation.md)。

## 项目记录与导航

[当前事实路由](docs/onboarding/current-state.md)定位源码、任务、API、Hook、Issue、测试和发布权威。一次非平凡实施只维护一个 Update；其他记录按事实变化链接它。重复问题复用已知原因和被排除方向，保留失败证据；只有相应真实行为、重启和退出验收齐备才能关闭问题。
