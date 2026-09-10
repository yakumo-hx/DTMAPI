# 20260909-0012: PN-011 依赖格式与共享程序集

## Metadata

- Update ID: `20260909-0012`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 用户连续实施授权、[P01–P04](../../architecture/platform-package-contracts.md)及 [PN-011](../../planning/platform-next/execution-next.md#pn-011依赖格式加载计划和共享-clr-类型)；[R3.shared GO](../../reviews/code/2026/20260909-0005-platform-r3-shared.md)。

## Summary

新格式包声明 Mod 版本区间和实际程序集清单。预检完整依赖与同名冲突后确定性加载，共享相同 CLR 契约；失败只阻断受影响的必需闭包。旧 reader 保留原解释。

## Changed Files

Shared/Authoring.Contracts、SDK 校验/构建/打包、Core manifest/加载/owner/registry 和 Doctor；不新增强制 Runtime DLL。新内部 Runtime/API/SDK 候选为 0.6.3，旧 0.5.5/0.6.2 payload 和原内部快照保持不变。

补全依赖/marker JSON schema、公开库编译 recipe 和 BCL 身份清单；修复已部署收据白名单、旧 BOM manifest reader 分流，以及完整 Core 回归发现的 scheduler 空闲分配。当前 Catalog 投影 321 个生产源文件，API 表仍 53 行，assembly compatibility 仍 0.5.3.0。

## Validation

已通过固定 SemVer、严格 reader、PE/inventory 闭包、依赖/冲突 planner、原 metadata 回归、SDK target matrix 和 0.6.3 source/payload compatibility。实际 .NET Runtime 集成测试证明共享准确 CLR Type、返回值、必需 consumer 先关闭及清理失败重试后 provider 才关闭，Control 不受影响。

仓库外 `PN011 共享契约` 的 Contract A/B/V2、Provider/Consumer/Control/Optional 及失败变体已用 SDK 0.6.3 公开 new/pack 构建，重复包摘要一致。首次 Mono 发现安装器生成的 `.dtmapi-author-receipt.json` 被新 inventory 校验误拒绝，未进入样例 Entry；修复为仅允许该既有部署元数据，并验证伪装 MZ 仍拒绝。部署后校验与嵌套重复/错大小写字段回归通过。该轮保留失败，不算正常组合 PASS。

最终 Mono 正常组合、同 identity 异 bytes、同名异版本三轮全部 Passed；准确 Type/Assembly、返回值 63、公开会话命令、静态初始化正常各 1/冲突均 0、逆序关闭与 Control 独立运行均断言通过。Provider 故意 Entry 失败与 optional provider 缺失的行为证据通过；故意失败轮总健康门 Failed 保留。最终原 retained 两 DLL 未重编译，返回 73、writes=1。

Core 154 入口完整通过；SDK package-dependencies、pack-build、official-local、session handshake、target matrix、旧 Advanced target compatibility、SDK release/0.6.3 source parity、Catalog 和文档检查通过。实际 Runtime 进程中磁盘换包触发 resident-conflict/restart-required；静态 PE 故障覆盖缺少传递库、Strict 间接 native 和宿主 DLL 改名。空 Runtime 与已激活但空闲 scheduler 各 10,000 帧零分配。先运行 official-local 测试时游戏尚未退出而被正确拒绝，退出后同检查通过。

桌面截图两次报 `foreground window did not report a process id`，重新定位后仍失败；实际 Mono 使用既有 QA 自动退出，不构成手动输入/实体设备验收。两轮均恢复五 DLL、fixture 启用文件，30 archive/5 sidecar length/hash/mtime/count 不变，原生启用文件未变，所有样例已 withdraw，session/QA 清理且锁释放。一份本卡失败重复安装的 8 文件 staging 因自动审批拒绝清理而保留，位置见证据 README，不参与加载。

## Evidence

[最终 Mono 与全部原失败路由](../../debug/evidence/GAME-SMOKE/20260909-platform-pn011-final-runtime/README.md)。`artifacts/pn011-core-final.log`、`pn011-package-faults-final.log`、`pn011-pack-build-final.log`、`pn011-official-local-stopped.log`、`pn011-session-final.log`、`pn011-runtime-targets-final.log`、`pn011-catalog-final.log`。

最终 SDK：`artifacts/pn011/sdk-final/DTMAPI-Author-SDK-0.6.3-win-x64.zip`，SHA `3165eba89b3a8d4914a0490641468e085743e9bb5f3596bd131dd8620ce3dd1a`。新 API contract SHA `fb15a29c592751b56828b48150a411096b94158be568159ddcc0d511f8153908`；Abstractions SHA `ea1507a37a0a63da203fdbaa762e0ff28009e0701aaa7ecb57754619ce02f963`。源码 metadata `artifacts/pn011/source-metadata/info.json` 为 9192 字节、SHA `35db4549402b8e6bbb58909c1a96a88a252dc28a9dfd43b5ac0d8aa5686b15a8`；它不是正式 Runtime 发布包。

## Rollback Notes

撤回新格式入口和绑定计划；既有包与存档格式不迁移。

## Follow-Up

R3.shared 接收内部格式，继续 PN-010 及本批后续任务；0.7.0 仍 planned，没有上传。
