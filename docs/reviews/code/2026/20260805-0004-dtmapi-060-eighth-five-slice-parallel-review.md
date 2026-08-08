# DTMAPI 0.6 第八次五切片并行审查

Date: 2026-08-05
Status: `recorded`
Audited commits: `3c848ddb`, `09e7bf00`, `a8163647`, `62d11900`, `4d250f0c`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

Input design:

- [`20260805-0003`](20260805-0003-moreequipment-branch-b-minimal-migration-design.md)
- [`20260730-0016`](20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)

## Scope

按用户规则，在 MoreEquipment Branch B 顶层设计、事务加固、当前攻击/
policy、旧格式矩阵和官方 Local 冷恢复 source contract 五个切片后，同时
启动三路只读审查：

1. Product/Compatibility 事务与 save-commit；
2. SDK policy、Catalog、官方来源与游戏验收 runner；
3. 文档治理、状态投影、回滚和切片计数。

审核期间未安装 Runtime、未启动游戏、未修改官方 `MODS`、启用状态、玩家
存档或侧车。发现及其有界修复仍属于本审核周期，不重新累计功能切片。

## Findings

### R1 — P1：当前 MoreEquipment policy 的 Runtime floor 失真

`doloctown-24456188-moreequipmentslots-v1` 是 0.6 新增的 current policy，
但 active registry、产品 manifest 和 Catalog 仍写 `0.5.5`。已发布 0.5.5
Core 只嵌入历史 `23762374` policy；即便 manifest 版本门放行，当前 receipt
仍会被旧 Runtime 拒绝。当前 policy 必须要求 `0.6.0`，历史 policy 继续以
`0.5.5` 只供 Runtime/Doctor 接受既发收据；Author SDK 公共 API compile target
仍保持冻结 `0.5.5`。

### R2 — P1：原生已改变但即时证据失败时可能失去 save veto

Product cold recovery 原先在所有 placement 完成后才登记 session，并直接
调用 placement。若背包或邮件已经发生原生变化，但 native call 抛出或即时
readback 不可读，durable journal 已记录 attempt，内存中却可能没有 session
阻止 `SaveSaving`。必须在 prepared journal 落盘后立即登记 session，以
`PlaceNativeItemWithImmediateEvidence` 取得同一尝试的精确结果；outcome unknown
保留 quarantine，未完成/Failure escrow 一律 veto `SaveSaving`，且
`SaveSaved` 不得提升它。

### R3 — P1：单进程 cold runner 可形成假通过

`-AssertMoreEquipmentSlotsColdRecovery` 默认运行 `NoNativeSave`，只等待 transient
placement 日志后删除 synthetic sidecar；没有证明 native `SaveGame`、
`SaveSaved`、journal 清除或第二个冷进程不重放。`AutoSaveAfterLoad` 也没有接入
这条产品恢复路径。正确验收必须是同一 disposable fixture 上的两阶段：先以
`NativeSaveExpected` 正常提交并保留 fixture，再以独立 `NoNativeSave` 冷进程
验证 exact native count、无 journal、无重放后清理。在专用 route 完成前，
现有参数必须在任何 staging/launch 前 fail closed，不能继续产生 PASS。

### R4 — P1：官方 Local package preflight 只证明自洽，未绑定 tracked authority

七文件检查与 marker 自带哈希只能证明包内部自洽。产品被禁用时 Loader 不会
替 runner 完成 Advanced 校验，因此 preflight 还必须逐项绑定：manifest 的
`0.6.0` floor 和精确字段、Catalog identity/Harmony owner、active registry
policy hash/floor、policy version/game build/game assembly、receipt schema/kind/
policy/hash/manifest/DLL length+hash/Harmony owner，以及 exact ordered reference
rows；SDK marker 也必须有精确字段集和 authority/hash/path 值。未知字段或任一
漂移都应在启动前拒绝。

### R5 — P2：synthetic sidecar cleanup 校验过宽

旧 cleanup 只检查 schema、archive index 和正则提取的 item ID，可能删除结构
已漂移但仍碰巧只含 `grandmas_button` 的 JSON。若未来恢复此 route，应先同时
验证 live/`.previous` 的 exact Product-v3 顶层、scope、journal、三槽顺序/
字段和值域，再执行任何删除；任一文件不匹配则两者都保留，manual recovery
必须始终列出两个可能路径。R3 的暂停门在当前候选中先消除了该删除路径。

### R6 — P2：AutoFishing Manager smoke 仍持有 `<game>/Mods` 启停语义

历史 Manager 生命周期仍会在
`<game>/Mods/Yuuka.DTMAPI.AutoFishing/dtmapi.disabled` 写标记，模拟同进程
disable/reload。这与 0.6 的官方 Local/Workshop 双来源及功能 Mod
restart-only 规则冲突。当前 runner/wrapper 应在任何路径解析、部署、锁、标记
写入或游戏启动前退役；历史 QA 类型/收据可作为审计材料，但不能继续作为
默认 Release 的功能性当前契约。

### R7 — P2：Review/Debug/Batch 6 状态归属漂移

- pre-implementation Review `20260805-0003` 追加了 changed files 与 PASS 叙事，
  应恢复为冻结设计，只留指向 owning Update 的短 resolution；
- MoreEquipment 的重复 save/sidecar transaction 风险需要独立 Debug issue，
  状态只能是 source-mitigated、disposable player acceptance pending；没有本轮
  game run，因此不得添加 smoke row；
- Batch 6 仍写 Review 0016 五项 open，并把 Product Hook 写成四目标/
  `BodyController` Prefix。应投影当前 typed
  `TryGetShieldItem(IAgentEquipmentShieldItem&)` Postfix、事务 source closure、
  current `24456188` policy / 0.6 floor 和保留 ABI；
- 路线图必须把 MoreEquipment 加入第四个 current 244/0.6 product，并明确
  cold route 不清空 Author state；空 selection helper 只属于历史 AutoFishing
  no-demand 隔离。

### R8 — P3：五切片计数口径过宽

本轮五项中，顶层设计和纯测试矩阵不都是独立玩家功能。此次并行审查可作为
用户要求的提前审计保留，但后续累计应只计算独立功能/发布行为切片；本轮审核
修复不重置、不重复累计。

## Global Blocker Preserved

本机 live game build 已为 `24567135`，而 reverse/current tracked Advanced
authority 仍为 `24456188`。在完成 public capture、差异审查和有界 policy 决定
前，`SDK202` 必须继续拒绝 ambient live assembly；不得把旧 exact fixture 的
包重建或一次未知-build Runtime 激活解释为 245 已兼容，也不得把候选标为可发布。

## Resolution Boundary

本 Review 只拥有审查发现。源码、测试、Debug/Batch 6 投影、验证、提交和发布
状态由 owning Update `20260802-0001` 记录。修复 R1--R7 不形成新的五切片周期，
R8 从下一项独立功能切片开始按收窄口径执行。
