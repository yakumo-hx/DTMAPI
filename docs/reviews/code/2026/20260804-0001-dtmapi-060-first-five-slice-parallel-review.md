# DTMAPI 0.6.0 前五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 前五个独立实现切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖版本三轴、API 稳定性元数据、Doctor 冻结 API 指引、Loader 游戏漂移上下文和默认真实 retained-consumer Release 门
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查基线：`08f13114`（`test: require real retained consumers in release`）

本 Review 只保存并行审查发现、根因和验收边界。修复生命周期、changed files、验证结果与发布状态仍由 owning Update 维护；审核发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. 0.6 三版本投影、公开 API maturity 和冻结兼容文案；
2. Advanced 分类、Doctor 对等性以及 `Exact` / `Drift` / `Unknown` 激活路径；
3. 默认 Release retained-artifact 解析、11 个第一方和 4 个外部消费者的真实 ABI 绑定。

审查只读取仓库和既有 sibling retained-artifact authority，未安装 Runtime、未启动游戏、未写玩家订阅目录或存档，也未获取 Runtime lock。

## 2. 发布阻断发现

### R1：0.6 Runtime 会在兼容判断前误拒合法 SDK 0.1 Advanced 包（P0）

`ManagedModClassification.VerifyAdvancedPackageMarker` 把 package marker 的 `TargetDtmApiVersion` 与当前 Runtime `ApiVersion` 做精确相等比较。0.6 源码候选因此要求 `0.6.0`，但当前唯一受准入的 Author SDK `0.1.0` 契约生成并由 receipt/hash 绑定的目标是 `0.5.5`。现有九个公开 ProductNative 包会在类型解析、`Entry` 和真实产品事务之前全部被误拒；Doctor 仍接受同一 `0.1.0` / `0.5.5` 对，形成 Core/Doctor 不一致。

有界修复必须保持 manifest、receipt、marker、payload、entry hash、reference policy 与 owner 严格，只把 marker 校验改为“匹配 tracked SDK contract 及 receipt-bound manifest”，再由已有 `MinimumDTMApiVersion <= current Runtime` 规则决定运行时版本兼容。不得把 marker target 改写成当前 Runtime，也不得放宽到任意 SDK/target 对。

验收至少覆盖真实 SDK `0.1.0` / target `0.5.5` 包在 0.6 Core 中经过完整 `Classify -> load -> Entry` 的 `Exact`、`Drift`、`Unknown` 三路成功；成功路只有 Info、没有 Warning；失败只清理当前 owner 且 sibling 继续；篡改 marker 仍在程序集加载前失败；Core 与 Doctor 对同一包结论一致。

### R2：11 个第一方 retained consumers 未全部执行真实成员绑定（P1）

ABI harness 对 10 个 frozen Compatibility 家族只核对预设 compatibility reference，对 Manbo AudioReplacement 没有执行其实际 `DTMAPI.Abstractions` `MemberRef` 解析。现有 `RetainedPublicProductExactHashCount=11` 只能证明输入 hash，不能证明每个旧 DLL 的全部公开成员引用仍能绑定到 0.6 candidate。

有界修复应把现有 metadata resolver 泛化到全部 11 个 Catalog-owned retained first-party DLL，逐产品报告 Abstractions `AssemblyRef`、`MemberRef` 总数与成功解析数；任一引用缺失或绑定失败均使默认 Release 失败。4 个外部消费者的现有真实绑定门继续保留。

### R3：retained 订阅快照选择可能被较新的非权威目录遮蔽（P2）

resolver 当前按目录名选择“含全部 15 个 identity 的最新快照”，再由下游做 hash 校验。一个更新但字节不匹配的完整目录会遮蔽较旧的 exact retained authority，造成结果依赖无关目录顺序。

有界修复应以 Catalog/retained binary audit 的期望 identity 与 SHA-256 扫描候选，要求唯一 exact 完整快照；零个或多个 exact 候选都明确失败。不得把“最新”升级成新的发布权威。

### R4：Catalog ABI 删除授权检查是宽松子串（P2）

Catalog checker 只查找 `authorizes ABI removal` 子串，否定该授权的句子也可能误通过。检查必须锁定路线图的明确否定语义，确保 0.6 没有获得 public ABI breaking-removal 授权。

## 3. 一致性发现

### R5：Frozen Fishing 运行时警告仍承诺 0.6 最早移除（P2）

GameBridge Fishing compatibility warning 仍把 `0.6.0` 描述为最早 breaking cleanup，API matrix 若干说明仍只承诺 `0.5.5` 窗口。这与路线图“保持 Frozen、禁止新增消费者、既有二进制保留至另行批准的 breaking change；不承诺删除版本”冲突。

修复须同时覆盖运行时文案、matrix 和非 Doctor 负向断言，禁止重新出现固定删除版本或迁移到另一 Frozen API 的暗示。

### R6：玩家状态检查仍写死 0.5.5 文案（P2）

`check-dtmapi-status.ps1` 的实际版本比较已读取当前 authority，但两处玩家输出和开发预览指南仍写 `0.5.5`。修复应投影动态 `0.6.0` release authority，并以 fixture 锁住输出；不能改写 Catalog 中真实已发布的 `currentPublishedArtifact=0.5.5` 历史事实。

## 4. 非发现与边界

- 版本三轴 `0.6.0` / `0.6.0.0` / `0.5.3.0` 和三项 API maturity 的机械投影未发现签名破坏。
- Doctor 的 package/receipt/hash fail-closed 与 Drift/Unknown Info 投影本身未发现放宽；R1 是 Core marker 把 SDK target 错当当前 Runtime 版本。
- 本 Review 不授权删除任何 frozen Compatibility Host、proxy、公开 API/DTO 或 retained fixture，不新增 receipt/schema/能力注册体系。
- 本轮不运行游戏；产品 native 修复和最终 game acceptance 仍按路线图后续切片执行。

## 5. 关闭条件

R1–R6 全部完成有界修复并通过各自 focused gates 后，owning Update 记录 commit、验证和残余风险。本 Review 保持 `recorded`，不复制实现完成叙事；若修复暴露新的独立根因，再建立对应 Review，而不是扩写本审查范围。
