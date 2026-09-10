# AutoFishing 朝向预飞稳定中性握手根因审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / direct native owner closed / exact-neutral no-input preflight authorized`
- 性质：AutoFishing 第五档正式 behavior matrix 五次真实游戏失败的连续根因审查
- Audited HEAD：`704813a7ead2`（首个失败）、`30e6d3f5eb1b`（stable-neutral 实施后的失败）、`57eb815a9581`（native-menu-reset 实施后的失败）、`592c0c4df9fd`（bounded surface-exit 实施后的失败）与 `eaeca60ebd83`（direct native-owner diagnostics 实施后的失败）
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 首次外层 / 游戏 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-125535-5e02fea6` / `docs/debug/evidence/GAME-SMOKE/20260804-125558`
- 第二次外层 / 游戏 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-132752-3ece549c` / `docs/debug/evidence/GAME-SMOKE/20260804-132815`
- 第三次外层 / 游戏 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-140417-97255f77` / `docs/debug/evidence/GAME-SMOKE/20260804-140439`
- 第四次外层 / 游戏 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-143231-db9bc829` / `docs/debug/evidence/GAME-SMOKE/20260804-143258`
- 第五次外层 / 游戏 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-151549-6fdf0608` / `docs/debug/evidence/GAME-SMOKE/20260804-151611`

本 Review 只冻结五次失败的事实、被证伪的假设、当前根因边界、拒绝方案和有界诊断/修复边界。实施文件、验证、重跑结果与 0.6 发布状态继续只由 owning Update 维护。本修复是第四轮五切片审查后的第一个新独立功能切片；第二至五次失败、诊断及其有界修复仍属于同一切片，不重复累计。

## 1. 运行事实

干净候选 `704813a7ead2` 已安装到当前玩家 Runtime；安装 manifest/state 为 `0.6.0` / `0.6.0.0`，五个 Runtime DLL 与 Release 输出逐字节一致，Player Doctor 为零 error、零 warning。正式 behavior runner 通过当前 Author SDK 生成并部署 AutoFishing `1.0.0` 包：

- SDK ZIP SHA-256：`B965C27CFC914D3268FBC95CE207E3800F9A62A53E6808559C251F5E9B92D05C`
- Product package SHA-256：`7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`
- 第五档 `slot=4` 已经由真实 native `LoadGame` 返回并发布 `SaveLoaded`；产品完整 `22` Hook、Core-resident instance、receipt/package identity 均已通过启动门。
- runner 在 `awaiting-initial-enable-toggle` 后向 exact foreground DolocTown 进程发送物理 `D`：hold `180 ms`、`SendInputSucceeded=true`、`PostMessageFallbackUsed=false`，key-up 为 `04:56:39.3434948Z`。
- runner 随后于 `04:56:40.2000541Z` 发出物理 `F6`；产品成功启用、创建 session 并开始真实 cast。
- 产品随后按已冻结的移动规则关闭，精确 reason 为 `native-move VelocityX=-1.6153926480910741E-06`；最终 `enabled=false`、`updateSubscribed=false`、`sessionPresent=false`，所以 `DefaultLoop` 正确判为失败，后七个 profile 未运行。

失败清理仍是安全证据，而不是行为 PASS：`PlayerSaveUnchangedBeforeCleanup=Passed`、`CommittedSidecarsUnchangedBeforeCleanup=Passed`、`SaveLoaded=Passed`、`ProcessExited=Passed`、`ForcedClose=Passed`、`NoFatalInstanceWindow=Passed`。临时产品、deployment journal/recovery、官方 profile/source 都已恢复，最终部署状态为 `AbsentNoJournal`，Runtime lock 已释放且无遗留 `DolocTown.exe`。

### 1.1 第三次运行：菜单开关成立，但返回 Gameplay 后 offset 被重新写入

干净候选 `57eb815a9581` 已把第二次失败授权的原生菜单 reset 握手完整实现并安装；五个 Runtime DLL 与 build/manifest 哈希逐字节一致，Player Doctor 为零 error、零 warning。SDK ZIP 与产品 package SHA-256 仍分别为 `B965C27C...D05C` 与 `7A9C1A66...A753`，产品源码与包字节没有变化。

第三次正式运行只进入 `DefaultLoop`，并证明菜单握手本身不是失败点：

- 物理 `D` 于 `06:05:22.285Z` key-down、`06:05:22.486Z` key-up，exact foreground、`SendInputSucceeded=true`、`PostMessageFallbackUsed=false`；QA 随后观察 `inputMultiplier=1` 的正向 movement 并发布 open-menu handshake。
- 第一次物理 `Escape` 于 `06:05:23.343Z` key-down、`06:05:23.436Z` key-up；QA 随后实际观察 `inputContext=MainMenuUiState`，不是由按键成功推定菜单已开。
- 第二次物理 `Escape` 于 `06:05:24.291Z` key-down、`06:05:24.383Z` key-up；它距离第一次 key-up 约 `855 ms`。QA 随后实际观察返回 `Gameplay` 并调用显式 native-reset boundary，因此“第二次 Escape 与第一次释放挤在同一帧而未被识别”的初步推断被日志反驳。
- 返回 Gameplay 后 `inputMultiplier` 已为精确 `0`，证明 `AgentControllerState.EnterUICheck()` 的清理路径确实生效；但 `VelocityX` 又稳定为同一个 `-1.6153926480910741E-06`。QA 在约 `300 s` / `17,960` 次 native refresh、零 accessor failure 后仍没有一个精确双零观察，故没有发布 enable handshake，runner 没有发送 `F6`，产品从未启用。

第三次失败继续安全：`NoNativeSave` 在 cleanup 前证明第五档 current / `.bak` / `.prev0` 的 length、SHA-256、mtime 全部不变，committed sidecar 仍不存在；无 routine byte backup、无 player archive writeback。进程在状态恢复前稳定退出，fatal-window 无新鲜实例；QA host 精确清理、官方 profile/source 恢复、`AbsentNoJournal` 恢复和 Runtime lock 释放均通过。`qa-host-lifecycle-gate` 因产品场景终态为 Failed 而整体失败，其 cleanup 子门为通过，不能误写成 behavior PASS。

### 1.2 第四次运行：一次 `1000 ms` 物理 D 仍未消除同一非零值

干净候选 `592c0c4df9fd` 已把第三次失败授权的唯一 bounded surface-exit 动作完整实现并安装；提交后完整 Release build 通过，五个 Runtime DLL 与 build/manifest 哈希逐字节一致，Player Doctor 为零 error、零 warning。SDK ZIP 与产品 package SHA-256 仍分别为 `B965C27C...D05C` 与 `7A9C1A66...A753`，产品源码、DLL、22 Hook、movement rule 和 package 均未变化。

第四次正式运行仍只进入 `DefaultLoop`：

- runner 在真实第五档 native fishing context 后发送唯一物理 `D`，key-down `06:33:56.278Z`、key-up `06:33:57.318Z`，实际 hold 约 `1040 ms`；PID `20880` 精确前台匹配，`SendInputSucceeded=true`、`PostMessageFallbackUsed=false`。QA 实际观察 `inputMultiplier=1` 的正向原生 movement，故动作没有丢失。
- 两次物理 `Escape` 分别于 `06:33:58.194Z` 和 `06:33:59.141Z` key-down；QA 依次观察 `MainMenuUiState` 与返回 `Gameplay`，原生 reset boundary 成立。
- enable handshake 从 `06:33:59.232Z` 等到 `06:38:55.085Z` 仍未出现；F6 receipt 为 `Sent=false`，产品始终 `enabled=false`、`updateSubscribed=false`、`sessionPresent=false`、`transitionCount=0`。
- 约 `300 s` 后 QA 的最后预飞值仍精确为 `inputMultiplier=0`、`VelocityX=-1.6153926480910741E-06`，`stableNeutralMilliseconds=0`、`neutralObservationCount=0`；`nativeFrameRefreshCount=17,964`、accessor failure 为 `0`。因此“把短按改为 runner 上限内的一次一秒 D 即可离开责任面”已被反证。

第四次失败同样安全闭合：`NoNativeSave` 在任何 cleanup 前证明 current / `.bak` / `.prev0` 的 length、SHA-256、mtime 全部不变，committed sidecar 仍不存在；未创建 routine byte backup，未执行 player archive writeback。进程先稳定退出，QA host 精确清理、官方 profile、Author source 与 deployment 均恢复，最终状态为 `AbsentNoJournal`，Runtime lock 已释放且无遗留 `DolocTown.exe`。这仍不是 behavior PASS。

### 1.3 第五次运行：直接 owner 证明强制 D 本身制造了新落点残余

干净候选 `eaeca60ebd83` 已把上一节授权的阶段化只读 native surface diagnostics 完整实现并安装；提交后完整 Release build 通过，五个 Runtime DLL 与 Release 输出、install-state、manifest 精确一致，Player Doctor 为零 error、零 warning。SDK ZIP 与产品 package SHA-256 仍分别为 `B965C27C...D05C` 与 `7A9C1A66...A753`，产品源码、DLL、22 Hook 和精确非零 movement rule 均未变化。

第五次正式运行仍只进入 `DefaultLoop`，但五个直接快照已经关闭 native owner：

- `initial-before-input` 在未发送任何预飞输入时即为 `AgentStateIdle`、`agentFaceRight=true`、position `(21.59466552734375, 8.9901657104492188)`、cell `(14,6)`；ground 与 ground-excluding-platform 都为 true、platform count `0`、wall false，`inputMultiplier=0`、`conveyorOffset=(0,0)`、Rigidbody velocity `(0,0)`，活动 / touched / stay conveyor 均为 `0`。读取完整且 `verified=true`。
- runner 随后按既有合同发送唯一物理 D：`07:16:54.656Z` key-down、`07:16:55.672Z` key-up，exact foreground PID `5456`、`SendInputSucceeded=true`、`PostMessageFallbackUsed=false`。即时 `initial-after-right-movement` 已读到 `inputMultiplier=1`；物理积分后 `initial-menu-open` 的 position 变为 `(33.834667205810547, 8.9901657104492188)`、cell `(23,6)`，证明 D 并非只设朝向，而是把已中性的玩家平移到新落点。
- 菜单返回 Gameplay 时 position/cell 保持新值，`inputMultiplier=0`、`conveyorOffset=(0,0)`、活动 / touched / stay conveyor 均为 `0`、wall false；只有 Rigidbody X 成为 `-1.6153926480910741E-06`。约 `300 s` 后 `initial-timeout` 仍是同一位置、同一 X，地面状态已稳定恢复，native refresh `17,959`、accessor failure `0`。
- 两次 Escape 的 exact-foreground `SendInput` 与双向 InputContext provenance 均成立；F6 因 enable handshake 超时而安全未发送，产品始终 disabled、零 session/updater/transition/fish。

这组对照排除了输入未清、`conveyorOffset`、活动或接触中的 `ConveyorPlatform`、墙面阻挡以及“原第五档加载点本来就有 tiny X”。根因是：正式第五档加载点已经朝右且精确中性，强制 D 不必要地改变了落点；新落点的原生落地/碰撞求解随后留下持久的 Rigidbody X 残余。第五次失败仍安全闭合：任何 cleanup 前 current / `.bak` / `.prev0` 的 length、SHA-256、mtime 和 committed sidecar 均不变，无 routine byte backup / archive writeback；进程、fatal-window、QA、官方 profile/source、deployment `AbsentNoJournal` 与 Runtime lock 全部恢复。这是 root-cause 和失败安全证据，不是 behavior PASS。

## 2. 根因

第三轮审查已经有意冻结 `FishingNativeMovementPolicy`：一旦 session 在精确中性帧完成 arming，任意有限非零 `MoveModifier.inputMultiplier` 或 `VelocityX` 都是原生移动，必须取消 automation；不得使用 epsilon，也不得恢复一秒盲窗。

首次运行时的 QA/runner 只证明以下顺序：

```text
真实第五档 native fishing context
  -> awaiting-initial-enable-toggle
  -> D key-down / key-up
  -> F6 key-down
```

它没有证明 D 抬键后的原生移动已经稳定回到精确中性。第一次修复据此增加了“先观察正向移动，再连续精确双零 `1000 ms`”的读取门；该修复仍保持 fail-closed，但第二次正式运行已证明“只需等待余量自然衰减”并不成立：

- 物理 `D` 的 exact foreground、`SendInput`、`180 ms` hold 和 key-up receipt 均成立；QA 也实际观察到正向原生移动，故不是短按落在采样缝隙。
- 产品始终未启用，runner 没有发送 `F6`；optional QA 对同一 ProductNative cache 完成 `17,980` 次原生 frame refresh，accessor failure 为 `0`。
- enable handshake 等待约 `300 s` 后超时，最终仍为 `status=WaitingForStableNeutral`、`stableNeutralMilliseconds=0`、`neutralObservationCount=0`、`inputMultiplier=0`、`VelocityX=-1.6153926480910741E-06`。延长等待因而已被实证否定。

当前 `24456188` native body 解释了为什么该值具有真实语义，而不是可忽略的浮点噪声；第三次运行还证明菜单清理不是充分条件：

1. `AgentStateBase.OnPlay()` 每帧把 `status.VelocityX` 写成 `status.MoveModifier.OffsetX`；`AgentStateFishingWait.OnPlay()` 在 `base.OnPlay()` 后以精确 `VelocityX != 0f` 判失败。因此产品继续以任意有限非零值取消，与原生 fishing owner 一致。
2. `MoveModifier.OffsetX` 就是 `conveyorOffset.x`，而 `MoveModifier.Clear()` / `AgentPhysicalStatus.ClearHorizontalMoveFactor()` 会同时清掉 input 与 conveyor offset。
3. `ConveyorPlatform.FixedUpdate()` 在持续接触且未暂停时写入 `group.SpeedX/group.SpeedY`，暂停时写零；`OnDisTouch()` 为空。`ConveyorPlatformGroup.SpeedX` 又是移动方向归一化后的 X 分量，接近垂直的移动平台可以产生有限但极小的非零 X。第三、四次 evidence 一度使“当前仍接触 moving platform”成为合理候选，但当时尚未读取 `conveyorOffset`、具体 ConveyorPlatform 或碰撞接触，因而从未把候选写成事实。第五次直接 evidence 已观测 `conveyorOffset=(0,0)`、活动 / touched / stay conveyor 全为 `0`，故该候选在本 fixture 上被排除。
4. 正常游戏的 `AgentControllerState.EnterUICheck()` 在 `GlobalToggleMenu` 后调用 `ClearHorizontalMoveFactor()`，再进入 `MainMenuUiState`。第三次运行已实际观察菜单双向切换、Gameplay return 和 `inputMultiplier=0`，证明这条玩家可触发清理路径已经工作；同一 `VelocityX` 随后重现，反证“只清一次即可保持中性”。

第四次运行进一步证明：即使把 D 扩展到单次 runner 上限并实际观察正向移动，也没有建立“玩家坐标离开原责任方”的证据。第五次直接快照补齐了这一缺口：D 前原地已经朝右、精确双零且无 platform/wall；D 后从 cell `(14,6)` 平移到 `(23,6)`，菜单清理后 input 与 conveyor offset 保持零，只有新落点的 Rigidbody X 持续非零。因此不应继续寻找离台动作，而应避免从已中性的加载点移动。产品 movement policy、22-Hook transaction 和 inactive lifecycle 都没有被五次失败反驳。

## 3. 已实施但被证伪的有界修复

只修 optional QA 与 runner，产品 DLL、movement policy、22-Hook inventory、manifest/policy/package 版本轴和公共 API 都不改：

1. 保留已实现的初次 / L5 re-entry 独立朝向 handshake，以及 optional QA 对同一 ProductNative `FishingNativeStateCache` 的 inactive-only refresh。QA assembly 继续不产生静态 Product AssemblyRef，也不复制原生 accessor。
2. 每次预飞先由 runner 在对应 facing handshake 后发送一次有界物理 `D`，hold 从仅够朝向的 `180 ms` 调整为 `1000 ms`，目的明确为“朝右并走离当前原生 moving-surface contact”。`1000 ms` 是 runner 已有单次严格物理输入上限；它只授权一个玩家可执行的位移动作，不构成通过依据。QA 仍必须实际观察到正向原生 movement，才发布 `awaiting-*-neutral-reset-open-menu`。
3. runner 在该 handshake 后向 exact foreground DolocTown 发送一次真实 `Escape`。QA 只有观察 `InputContext` 已进入 `MainMenuUiState`，才发布 `awaiting-*-neutral-reset-close-menu`；runner 随后发送第二次真实 `Escape`。
4. QA 只有观察 `InputContext` 精确返回 `Gameplay`，才允许 movement gate 开始连续精确中性计时。此后每个 QA update 都必须保持 `NativeMovementAvailable=true`、两个值均有限且精确等于 `0` 达至少 `1000 ms`；任一非零重置，unavailable / 非有限 / 时间倒退均 fail-closed。
5. 只有稳定窗口成立，QA 才发布既有 `awaiting-initial-enable-toggle` / `awaiting-reentry-enable-toggle`；runner 此后才可发送配置的 F6/F7。evidence 必须保留 D、open-menu handshake、第一次 Escape、close-menu handshake、第二次 Escape、Gameplay return、stable-neutral completion 与 toggle 的严格顺序和物理输入 provenance。
6. 额外 native refresh 只存在于 inactive preflight；正式 enable、measurement、phase movement 与 cleanup 继续使用既有观察路径，不能用 QA 轮询改变产品激活后的行为计数或结果。若 `1000 ms` 真实位移后玩家仍处于会继续写 offset 的原生环境，菜单关闭后的精确中性门必须继续失败并记录最后值，不能循环盲按、直接改位置或直接改字段。

两次 `Escape` 是原生菜单开/关动作，不是 QA 直接调用 `Clear()`；以 `InputContext` 双向确认，而不是固定 sleep 推断菜单状态。物理 D 的 `1000 ms` 与返回 Gameplay 后的稳定中性 `1000 ms` 是两个不同边界：前者是单次有界原生位移，后者是只读、连续、精确双零的后置证明。

第四次运行已完整执行以上顺序但仍失败，所以本节不再授权继续延长 D、重复 D 或直接改产品门。它作为已被反证的实现历史保留。

### 3.1 直接诊断结论与下一步有界修复

阶段化只读 diagnostics 已按上节完成，且在任何写入前证明正式第五档加载点本身就是所需起点。下一次修复仍只允许 optional QA 与 runner 变化；不得修改产品 DLL、精确非零 movement rule、22-Hook transaction、包字节或公共 API，也不得写原生字段、调用 `Clear()`、传送或修改坐标。

初次与 L5 re-entry 预飞必须先在未发送 D/Escape 的原始加载位置取得完整直接快照，并同时满足：

- `verified=true`、InputContext 为 `Gameplay`、state 为 `DolocTown.AgentStateIdle`、`agentFaceRight=true`；
- ground 与 ground-excluding-platform 为 true、ground platform count `0`、wall false；
- `inputMultiplier`、`conveyorOffset.x/y` 与 Rigidbody `velocity.x/y` 全部有限且精确等于 `0`；
- 活动 / touched / stay conveyor 全为 `0`，直接成员无缺失或读取失败。

只有上述条件成立，QA 才可在同一位置开始连续只读的精确中性 `1000 ms` 窗口；期间任一条件失效、时间倒退、位置/cell 改变或任何 movement 值非零都 fail-closed。窗口结束时保存 `initial-stable-neutral-ready` / `reentry-stable-neutral-ready` 快照，必须仍与起始 position/cell 完全相同，然后才发布既有 enable handshake。runner 在握手前不得发送 D、Escape 或任何替代动作；若直接前置条件不成立，本正式 fixture 直接失败，不猜测其他动作或回退到旧 D/menu 路径。

## 4. 拒绝方案

- **在 D 后固定 sleep 更久。** 拒绝；它没有读取原生 owner，机器时序变化后仍可能在余量尚存时发 toggle。
- **继续把 stable-neutral timeout 拉长。** 拒绝；第二次运行已经刷新 `17,980` 帧并等待约 `300 s`，最后仍是完全相同的精确非零值。
- **发送 A+D chord 抵消。** 拒绝；输入乘数相消不能可靠清理独立的 `MoveModifier.OffsetX` / conveyor offset，且不能证明走过原生 owner 的清理路径。
- **给 `VelocityX` 增加 epsilon。** 拒绝；这会放宽已审查的设备无关产品语义，并重新引入玩家真实微移动不取消的窗口。
- **恢复启用后一秒 arming 盲窗。** 拒绝；第三轮审查已证明该路径可吞掉真实移动，且本次失败不反驳该结论。
- **无证据地全局删掉 D 朝向前置。** 仍拒绝；未知 fixture 不能假定朝向或中性。但第五次直接证据已证明本正式第五档原始加载点 `agentFaceRight=true` 且精确中性，因此在完整只读前置条件约束下跳过 D 已被明确授权；这不是放宽产品门或对其他 fixture 的普遍结论。
- **把本次安全失败改判为 PASS。** 拒绝；它只证明 save/process/deployment cleanup，未完成一个真实 native fishing loop。
- **只重复菜单开关或增加菜单等待。** 拒绝；第三次运行已经证明菜单双向切换、约 `855 ms` 独立释放间隔和 Gameplay return 全部成立，同一值仍被重新写入。
- **QA 反射调用 `Clear()`、设置 `VelocityX`、`conveyorOffset` 或玩家坐标。** 拒绝；这会把 fixture 直接变成 native 状态写入者，不能证明普通玩家输入下产品成立。
- **无限或自适应循环发送 D。** 拒绝；它会离开已证明中性的固定夹具落点并掩盖问题。旧实现只允许一次受 runner 上限约束的物理离台动作；第五次直接 evidence 已证明该唯一动作本身就是错误前置，当前正式 fixture 在 enable handshake 前不再允许 D。
- **继续延长或重复一次 D。** 拒绝；第四次运行已经以 runner 的单次上限实际执行约 `1040 ms` 且观察到正向 movement，同一非零值仍持续约 `300 s`；第五次又直接证明 D 把玩家从中性 cell `(14,6)` 移到产生残余的 `(23,6)`。更多 D 不是修复。
- **直接猜测 Jump、JumpDown、Dash 或组合键。** 暂不授权；这些可能是后续合理的玩家动作，但必须先由只读 surface/ground/wall/position 证据证明其针对的真实责任方与几何条件。

## 5. 实施与验收边界

- 修复必须先通过 QA Unit、AutoFishing behavior/GC/Manager focused static gates、双 PowerShell 语法/合同检查和相关 Release build。然后在干净提交上重建 QA Runtime、刷新玩家安装来源，并只从 `DefaultLoop` 做最小正式 behavior 重跑；失败运行不能续接为通过。产品 package SHA-256 应继续保持 `7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`，除非另有独立产品源码变更授权。

下一次正式 behavior evidence 必须证明：

- `initial-before-input` / `reentry-before-input` 直接快照满足第 3.1 节完整前置条件；
- F6/F7 enable handshake 之前没有 D、Escape、PostMessage 或其他预飞输入；
- 同一原始 position/cell 上的直接 native movement/offset 连续精确中性至少 `1000 ms`；
- `*-stable-neutral-ready` 快照仍满足完整直接条件并与 before snapshot 的 position/cell 精确相同；
- 稳定中性完成严格早于 F6/F7 key-down；
- 产品不再因该预飞余量自行关闭，并完成相应 profile 的真实 native loop；
- 原有 NoNativeSave、process exit、fatal-window、deployment/profile/source rollback 继续通过。

如果直接前置或稳定中性在状态 timeout 内从未成立，矩阵必须保持失败并记录最后的原生值；不得自动改用阈值、模拟预飞输入、PostMessage、旧 D/menu fallback 或直接改 native field。

## 6. 当前验证与副作用

本 Review 当前包含五次正式游戏失败。第二至五次分别使用干净提交 `30e6d3f5eb1b`、`57eb815a9581`、`592c0c4df9fd` 与 `eaeca60ebd83`；四次完整 Release post-commit build 均通过，安装后五个 Runtime DLL 与对应 build/manifest 哈希一致，Player Doctor 均为零 error、零 warning。Author SDK ZIP 仍为 `B965C27CFC914D3268FBC95CE207E3800F9A62A53E6808559C251F5E9B92D05C`，产品包仍为 `7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`。

第二至五次运行都是 `NoNativeSave`，第五档均已加载；各自的 `player-save-unchanged-before-cleanup.json` 在任何 runner/external restore 前证明 current / `.bak` / `.prev0` 的 length、SHA-256、mtime 均未变，committed sidecar 同样未变。进程退出、fatal-window、QA cleanup、官方 profile/source 与 deployment 恢复均通过，未创建 routine byte backup 或 player archive writeback，Runtime lock 已释放且无遗留 `DolocTown.exe`。这些是失败安全证据，不是 player behavior PASS。

本 Review 冻结的 direct native-owner diagnostics 已由 owning Update 实现、通过提交前与 post-commit Release/focused 门，并在干净提交 `eaeca60ebd83` 上完成第五次真实运行。诊断关闭了根因但 behavior 结论仍是失败，不能从 root-cause evidence 推导 PASS；下一次实现与验证叙事继续只由 owning Update 持有。

前四次 Review 修订与 owning Update 链接完成后，PowerShell 7 与 Windows PowerShell 5.1 的 `check-doc-governance.ps1` 均通过 `6288` 项检查；第五次修订后的文档门将在 no-input 修复实施前重跑并由 owning Update 记录。

## 7. Rollback

QA/runner 修复可作为一个切片整体回滚；回滚会恢复已被后续正式运行证伪的预飞合同，并使 AutoFishing behavior gate 回到未通过。不得只回滚 no-input gate 而保留无法消费的新 expected-state / diagnostic receipt 断言，也不得通过回滚产品精确非零规则让旧 runner 变绿。
