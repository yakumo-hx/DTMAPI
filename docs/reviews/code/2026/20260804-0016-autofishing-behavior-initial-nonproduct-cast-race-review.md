# AutoFishing behavior 首个非产品施法循环竞态审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / root cause confirmed / bounded QA rebaseline authorized`
- 性质：AutoFishing exact candidate 正式八 profile behavior matrix 在第四项 `FastAnimations` 完成一个真实 native 循环、却没有产品施法计数后的重复验收失败根因审查
- Audited HEAD：`9075d54c48a6e4e0754ae9c6a034d0f0ffee11ac`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 失败外层 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-194749-2c4b332c`
- 已通过 profile game evidence：`docs/debug/evidence/GAME-SMOKE/20260804-194752`、`docs/debug/evidence/GAME-SMOKE/20260804-194857`、`docs/debug/evidence/GAME-SMOKE/20260804-194958`
- 失败 FastAnimations game evidence：`docs/debug/evidence/GAME-SMOKE/20260804-195058`

本 Review 只冻结失败事实、根因边界与下一次修复授权。实施文件、验证、正式重跑和发布状态只由 owning Update 维护。本次调查与有界修复继续属于第四轮后的同一个 AutoFishing 功能切片，不新增独立切片计数。

## 1. 已确认事实

1. 干净产品 HEAD `9075d54c48a6` 的正式 AutoFishing package SHA-256 为 `23AEA43A2B367775578E4457B2F6C9992DF240C6E83F17D2A615ACFA1D93BF36`，entry / manifest / policy SHA-256 分别为 `A98B510F38B3959C2E39A9A7DC08F00D616B4CA3DB7517F0190CB975EFD61C27`、`146B54D3AD110BAE3484E29855224DB47219AAF1AC2C17B9148010F0F84C0259` 与 `0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628`。同源 Runtime 五 DLL、manifest/install-state 和 Player Doctor 在启动矩阵前均已通过精确核对。
2. 同一个正式 outer 中 `DefaultLoop`、`InstantBite` 与 `SkipMiniGame` 依次通过完整产品行为、NoNativeSave、进程、profile/source 与部署恢复门。第四项 `FastAnimations` 的第五档初次 direct-neutral 预飞也在原位置证明 input、offset、Rigidbody X/Y 精确为零，surface 安全且连续稳定超过 `1000 ms`，随后才由既有 receipt-bound、前台进程匹配的物理 F6 启用产品。
3. `FastAnimations` 在启用后确实完成一个可见原生钓鱼循环：`FishCompleted/PullEntered/PullExited=1/1/1`，`NativeVisibleReel=1`，visible reel `queued/consumed/nativeAccepted=1/1/1`，`AnimationApplication=2`，retry、timeout 和 native accessor failure 均为零。
4. 该循环从 behavior baseline 到终点的 `CastApplied` 增量却为 `0`。启用后的首个采样已经是 `lastReason=not-normal-state`；稍后采样进入 Wait、reel 和 Pull/exit。现有 gate 在第一条鱼完成时立即终止计量，随后因 `fullLoop=false` 正确拒绝该 profile。
5. 本次 runner 在上述时间窗内唯一授权输入是 receipt-bound F6。当前原生默认 `UseTool` 映射为鼠标左键，本地 `key_rebinds.json` 为空；没有证据证明 F6 是原生 UseTool，也没有证据能唯一识别是谁在基线与启用边界之间启动该原生循环。因此只能确认“计量窗口捕获了一个没有产品-owned `TryCast` 证明的初始循环”，不能把触发原因写成 F6 映射、玩家操作、产品施法或 native bug。
6. 失败后的 `NoNativeSave` 在任何 cleanup/restore 前证明第五档 current / prev / bak 与相关 committed sidecar 的 length/hash/mtime 不变；没有 routine byte backup 或 player archive writeback。进程、fatal-window、QA、官方 profile、Author source、最终 `AbsentNoJournal` 和 Runtime lock 均安全关闭。该 run 是有效失败安全证据，不是 `FastAnimations` 或完整矩阵 PASS。

## 2. 根因

正式 behavior gate 在产品 disabled 的 direct-neutral 时捕获一次 baseline；F6 使产品 active 后立即从该 baseline 开始计量。它把“第一条完成鱼”同时当作“第一条由产品发起且完整完成的鱼”，但真实 evidence 证明这两个条件并不等价：计量窗口可以首先接触一个已经离开 normal/idle、且没有任何产品 `CastApplied` 计数的原生循环。

产品计数没有失真：它拒绝把没有通过产品-owned `TryCast` 的循环记为产品施法。失真的是 optional QA 的窗口归属假设。直接把该循环与下一次产品施法的 Pull/result 计数拼接，会制造跨循环的伪 full-loop；直接接受 `CastApplied=0` 又会删除当前最关键的产品-owned cast 证明。

## 3. 被拒绝方案

- **直接重跑直到首个循环恰好来自产品。** 拒绝；会把可重复出现的基线/启用边界竞态隐藏为运气。
- **放宽 `fullLoop` 或删除 `CastApplied >= 1`。** 拒绝；这会把纯原生或外部施法错误归给 AutoFishing。
- **保留旧 baseline，等待第二次产品施法后只补一个 cast 增量。** 拒绝；会把不同循环的 cast、reel、Pull、动画和结果拼成一条伪链。
- **根据当前 evidence 修改产品、22 Hook、F6 或原生键位。** 拒绝；触发 owner 未被证明，产品已经正确没有冒领该 cast。
- **发送额外原生 UseTool、写 native state 或进入保存路径清场。** 拒绝；会改变被测语义并破坏 direct-neutral / NoNativeSave 边界。
- **允许任意数量的无产品施法循环。** 拒绝；会把持续的 owner/计数故障无限隐藏。

## 4. 有界修复授权

只授权修改 optional QA 的正式 `Behavior` 计量窗口，不改产品或 Runtime：

1. `PilotState.Measuring` 观察到目标鱼已完成、但相对当前 behavior baseline 的 `CastApplied` 增量仍小于 `1` 时，可把这条**已经终止的首个循环**记为 `discardedNonProductInitialLoops=1`，并在该终点观察上重新建立全部 behavior/measurement baseline。
2. 重基线必须同时重置 measurement start time/fish、last native progress/value/time 和 next sample time；下一条循环的 cast、reel/skip、Pull、动画/charge、retry/timeout 与 fish result 必须全部从新 baseline 独立计算，禁止跨循环拼接。
3. 每个 profile 最多允许一次这种重基线。第二个已完成循环若仍没有产品 `CastApplied` 增量，必须立即 fail-closed，不能继续等待第三次。
4. 最终 behavior receipt 的 details 必须写出 `discardedNonProductInitialLoops=<0|1>`；stage evidence 必须明确记录一次性丢弃原因。已有 `fullLoop`、scenario-specific delta、cleanup、NoNativeSave 和 input provenance 条件不变。
5. QA Unit / source contract 必须锁住“一次允许、第二次拒绝”、完整计量字段重置、最终 provenance 与不得改产品/Hook/input 的边界。

该修复不得改变 AutoFishing 产品源码/DLL、22 Hook、Runtime 五 DLL、GameBridge production path、public API、manifest、policy、receipt schema、产品版本或 package hash。

## 5. 正式验收边界

- QA 修复后，必须用同一个 exact package `23AEA43A2B367775578E4457B2F6C9992DF240C6E83F17D2A615ACFA1D93BF36` 从头运行全部八个正式 behavior profile；本次前三个 profile PASS 不能与新 QA runner 拼接成矩阵 PASS。
- 随后仍须从头通过正式 L0--L5 GC ladder 与 Manager lifecycle。每一门都继续要求 fifth-save/native context、产品 package provenance、NoNativeSave、进程、profile/source/deployment 和 Runtime lock 收尾。
- 只有新 behavior evidence 的最终 clean deltas 包含产品-owned cast，才可关闭本 Review 对 behavior gate 的阻断；一次性 rebaseline 本身不是产品行为 PASS。
