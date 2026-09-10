# 20260909-0004: PN-009 上下文、调度、资源与命令

## Metadata

- Update ID: `20260909-0004`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权与 [R1](../../reviews/code/2026/20260908-0016-platform-m1-r1.md)；实施 [PN-009](../../planning/platform-next/tasks.md#pn-009上下文作用域scheduler资源与命令) 和 RT-01–04，不新建全平台 Review。

## Summary

通过可选服务提供不可变 Runtime/context、owner/save/world 作用域、有界主线程调度、确定资源清理与命令。Core 复用唯一 Update；GameBridge 拥有原生开始、失效与 ready 事实。旧 helper/事件、保存语义与冻结 target 不变。

## Changed Files

- Abstractions `RuntimeServices` 候选契约，Core `PlatformRuntimeServices` context/scheduler/resource/commands 与 owner/Runtime 接入。
- GameBridge 精确新游戏/异常/场景/房间边界和 native-frame ready 检查；Harmony finalizer 只观察异常，不吞掉或替换。
- Bootstrap 现有标题 Status 页提供有命令注册时才出现的输入/运行和有界输出；SDK `SDK203` 通过 Roslyn 实际符号识别直接 async-void 平台回调。
- Author session 新增可选协商 `execute-command/1`、`prepare --commands true` 和 `session command --command-line`。复用现有认证/来源/hash/owner/replay，deferred completion 在现有 pending request 上等待同一 scheduler；关闭/期限取消尚未开始的工作，已运行不声称回滚。仍待完整作者说明、native/UI/后台调用实测，以及 PN-007.a 候选下的公开 CLI 新 API 路径。

## Validation

最终出口：[PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)与 R2 已接受本卡有界结果；下列内部阶段的 pending/not-run 是当时状态，已由末尾实测结论收口。

- PASS：`platform-runtime-core` 五入口，覆盖可控时钟/FIFO/轮转/容量/预算、相同 tick 不重复 drain、NextTick 与回调再入 cutoff、取消/开始竞争、异步 Completion、不保留异常图、跨 epoch 与原生 false 后拒绝 ready、错误线程、LIFO 资源清理/失败重试、命令解析/冲突/帮助/后台入队和 owner 隔离。已接入 Core 默认 suite。
- PASS：`platform-services-core`、`platform-data-core`、`phase1-core-cleanup` 集成回归；Core/GameBridge/Bootstrap 候选构建零警告零错误。初次把多个 focus 当数组传给单字符串参数被路由拒绝，改逐个执行后通过；未误跑默认全量。
- PASS：实际 AuthorSdk.Tests 的 `platform-sdk-targets`（Get-DotNetExe .NET 8）含新语义探针：四个真实 async-void 平台参数诊断 SDK203，同步/Task.Run/同名自有方法控制组不误报。普通 CLI/target/schema/frozen 组合回归同组通过；该探针是内部编译检查，不冒充新 SDK 的外部作者验收。Unit 同名 focus 只验证 Runtime reader，已分别执行并区分日志。
- PASS（有界原生）：候选五 DLL、PID 6108，真实 IO 失败清空作用域→同进程重试 save epoch 2→帐篷/农场/帐篷 world epoch 1/2/3→返回标题清空。全部 transition 在 thread 1；runner 的读档、owner/QA close、退出 gate 通过。30+5 玩家文件不变、五 DLL 精确还原、实际 SAVE 启用基线不变、QA 移除并释放锁。该日志没有 ShuttingDown context 通知，不能从 ProcessExited 推定公开回调已观测。
- not-run：本候选新游戏上下文、外部真实后台 Post/资源/命令/UI、跨档取消和公开 shutdown 通知；新公共双作者产品证据还归 PN-007.a/020。
- PASS：AuthorSdk.Tests `platform-session-handshake` 实际 SDK→Core pipe→主线程调度→SDK 回包，包含 commandLine 引号参数、相同 request ID、跨 owner 拒绝、deadline/会话关闭取消；旧认证/队列/replay/legacy schema 矩阵保持通过。握手 capability 是可选协商，不新增会话或去重系统。`artifacts/pn009-session-tests.log`；测试初次缺 using/断言重载编译失败，修正后完整 focus 通过。
- PASS：新候选对冻结 0.5.5 helper 实现者/consumer 的实际绑定调用，`artifacts/pn009-helper-abi.json`；shippingMono=false。文档治理 8770 检查通过于本卡最新 native 运行前，后续记录变更仍需同步。
- 输入复用：M1 新游戏绕过 LoadGame；native false 可能仍有 IsDataLoaded=true。先失效、无新成功边界保持 unavailable，不猜测原生恢复。

## Evidence

- [M1 native facts](../../hook-map/focused/RuntimeLifecycle.md)、[RT 契约](../../architecture/platform-runtime-contracts.md)、[PN-005](20260909-0002-platform-optional-services.md)。本卡 [原生上下文实测](../../debug/evidence/GAME-SMOKE/20260909-platform-pn009-runtime/README.md)，runner `20260909-091853` PASS；旧 Strict 0.1.2 仍是 0.5.5 consumer，不能替代新 API 外部产品验收。
- 日志：`artifacts/pn009-platform-runtime-core.log`、同前缀三个集成 focus 日志、`pn009-native-ui-build.log`、`pn009-author-sdk-callback-tests.log`。初次 SDK203 构建使用不存在的诊断 Line/Column 字段失败，已改用现有 Path:line 格式，后续真实构建/测试通过。

## Rollback Notes

撤回可选服务/对应 native 观测与 Runtime drain 调用时，清理队列、资源和平台强引用；不建立第二 driver、不改原生玩家档。

## Follow-Up

内部实现与有界 native 验证完成，作者语义已写入 `author-sdk/PLATFORM-SERVICES.md`。真实新服务产品/UI/shutdown 等未测 gate 保留到 PN-007.a/020；当前进入 PN-019 内部片，精确公共面经 R2/PN-007.b 才冻结。

PN-020 复现关闭入口缺失，按 [退出入口 Review](../../reviews/code/2026/20260909-0002-platform-shutdown-entry.md) 修订 Bootstrap：订阅 Unity Application.quitting，复用幂等关闭并隔离各阶段异常，保留 OnApplicationQuit。`artifacts/pn020-shutdown-build.log` 构建 PASS；`artifacts/pn007/runtime-e/runtime-patch.json` 保留修订 DLL 和源码，SDK D 与其他 Runtime 字节不变。E 正常退出及 G QA Application.Quit 均已观察主线程一次 ShuttingDown、两个 Mod Dispose、LIFO 与 remaining=0/failures=0；原 PN-009 旧记录不改写。

最终实测结论：双作者真实后台调度、SDK/标题命令与拒绝、两个完整读档循环、房间变化、新游戏、真实 IO false→同进程 retry、资源重试及公开 ShuttingDown 全部有证。失败 epoch 无 WorldReady；新游戏序章后才 WorldReady。 证据及失败沿 [PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)。公共面继续 Experimental；最终冻结归 PN-007.b，未发布。
