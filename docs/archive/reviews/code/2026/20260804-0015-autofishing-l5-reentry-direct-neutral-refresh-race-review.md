# AutoFishing L5 重入直接中性刷新时序审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / root cause confirmed / bounded native-parity repair authorized`
- 性质：AutoFishing 正式 L0--L5 GC 梯度在 L0--L4 通过后，由 L5 第二次第五档加载的首次直接中性预飞停止的根因审查
- Audited HEAD：`7482fdd85dd94cc50b536ef713ccde5dd1af75b4`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 前置预飞审查：[AutoFishing 直接中性预飞加载接触时序审查](20260804-0014-autofishing-direct-neutral-load-contact-race-review.md)
- 失败外层 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260804-175508-27232cc8`
- 失败 L5 game evidence：`docs/debug/evidence/GAME-SMOKE/20260804-185030`

本 Review 只冻结失败事实、证据缺口和下一次诊断边界。实施文件、验证、正式重跑与发布状态只由 owning Update 维护。本次调查与有界修复继续属于第四轮后的 AutoFishing 功能切片，不新增独立切片计数。

## 1. 已确认事实

1. 干净 HEAD `7482fdd85dd9` 已通过同源 Runtime 刷新：五个已安装 Runtime DLL 与 Release 输出逐字节相同，manifest/install-state 都绑定该 HEAD，Player Doctor 退出 `0`，AutoFishing 初始部署为 `AbsentNoJournal`。正式 runner 重建的 SDK ZIP 为 `B965C27CFC914D3268FBC95CE207E3800F9A62A53E6808559C251F5E9B92D05C`，AutoFishing package 为 `7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`。
2. 同一正式 outer 中 L0、L1、L2、L3、L4 均形成 `Passed` stage：各有 `21` 个样本并分别覆盖约 `600.226 / 600.107 / 600.201 / 600.097 / 600.107 s`；六类 DTMAPI root metrics 稳定，Runtime record range/delta 均为 `0/0`，GC counters 单调，最后 `90 s` native progress 通过。L4 另完成一个 `QaProductNativeRecovery` unit，recovery owner/session/input/animation/scheduler/native transient 全部为零。
3. L5 的初次第五档加载、无输入同位置直接中性 `1002.122 ms`、物理 F6 启用、`5` 条预热鱼、`600.175 s` 正式计量、`462` 条 measured fish、末尾 F6 停用、ReturnHome 与第二次第五档 `SaveLoaded` 均已发生。Save/load coordinator 报告 `requests=2 / nativeEnter=2 / nativeReturn=2 / saveLoaded=2 / timeouts=0`。
4. 第二次 `SaveLoaded` 在 `19:01:23.563` 被观察；`19:01:24.060` 的 post-reload native fishing context 已验证同一 `carbon_fishrod`、一个 fishing pool 与 `DolocTown.CityRoom`。约 `24 ms` 后 `BeginDirectNeutralPreflight(reentry:true)` 失败，异常为 `Direct-neutral preflight requires exact ProductNative inputMultiplier=0 and VelocityX=0 without a preflight input.`。
5. runner 只发送了前两次 receipt-bound F6。第三次 `awaiting-reentry-enable-toggle` handshake 未发布，因此第三个 F6 `Sent=false`；没有用输入掩盖失败。失败后的 product updater/session inactive，22 个产品 patch 保持安装，QA cleanup、NoNativeSave current/prev/bak 与 committed sidecar 比对、官方 profile/source、进程、fatal-window、SDK deployment `AbsentNoJournal` 和 Runtime lock release 均安全关闭。

## 2. 当前根因与证据缺口

`BeginDirectNeutralPreflight` 已显式调用 `observer.Observe(..., refreshInactiveNativeMovement:true)`。门能越过 `PreflightNativeMovementRefreshed`、`NativeMovementAvailable` 和 finite 检查后命中 exact-zero 异常，证明该失败观察至少有一个 ProductNative `inputMultiplier` / `VelocityX` 为有限非零值；它不是“refresh 尚未执行”或 owner unavailable。

但现有首次预飞路径只在 `gate.Observe` 成功后记录 surface receipt，并且异常文本不带两个 ProductNative 实际值。随后 `FailFromException` 写入的 final observation 是一次未要求 refresh 的普通快照，因此其中 `preflightNativeMovementRefreshed=false` 与两个默认零不能代表失败观察。当前 evidence 也没有 `reentry-direct-neutral-failed` surface。由此尚不能区分：

- 第二次加载的短暂非零 `MoveModifier.inputMultiplier`；
- 第二次加载的短暂或 tiny nonzero Rigidbody `VelocityX`；
- 两者同时非零；
- ProductNative cache 与同帧直接 surface 读取之间的时序差异。

在保留实际数值和同帧 surface 前，不能授权把非零运动改判为可等待，也不能把现象归因给玩家输入、conveyor、墙体、平台或产品 bug。

## 3. 被拒绝方案

- **直接重跑正式 L5 直到碰巧通过。** 拒绝；会丢失已发生的重入时序事实。
- **复用初次加载的中性 receipt。** 拒绝；L5 必须直接证明第二次加载的 owner、position、surface 与 movement。
- **给 `VelocityX` 或 inputMultiplier 加 epsilon。** 拒绝；产品对任意有限非零 movement 的精确取消边界没有被本次 evidence 推翻。
- **对任意非零 movement 固定 sleep 或无限等待。** 拒绝；当前尚不知道值、owner 与收敛轨迹，且等待不能容忍真实玩家输入或位置漂移。
- **发送 D/Escape/Jump、写 native 字段或跳过第二次预飞。** 拒绝；会改变被测状态并破坏零预飞输入证据。
- **因 L5 失败重跑已通过且 package/Runtime 未变的 L0--L4。** 拒绝；本次故障只在 L5 重入路径，项目规则允许对受影响 gate 做 focused 修复与重跑。

## 4. 授权的下一步

当前只授权 optional QA 的诊断增强，不改产品 DLL、22 Hook、Runtime 五 DLL、manifest/policy/package、公共 API或玩家存档：

1. 首次 `BeginDirectNeutralPreflight` 的 `gate.Observe` 也必须像后续观察一样在异常时记录 `initial/reentry-direct-neutral-failed` surface receipt。
2. exact-zero 异常必须以 invariant round-trip 形式写出 ProductNative `inputMultiplier` 和 `VelocityX`；surface receipt 同时保留直接读取的 input、conveyor offset、Rigidbody XY、position/cell、ground/platform/wall 与 conveyor enumeration。
3. QA Unit / source contract 锁住实际值进入异常、失败 phase 被记录且不改变现有成功序列。
4. 先运行一个 focused、短时、明确 `non-authoritative` 的 L5 诊断，只用于取得第二次加载的精确值与收敛轨迹；它不能关闭正式 GC gate。
5. 只有新 evidence 证明一个有界、无输入、同位置且全 surface 安全的加载瞬态后，才能在本 Review 增补新的有界修复授权。任何产品语义或 epsilon 仍需另行审查。

## 5. 正式验收边界

- 诊断后只重跑受影响的正式 L5；本 outer 已通过的 L0--L4 可与新 L5 组成 exact unchanged-package evidence set。
- 正式 L5 仍须覆盖 `600/30/5/10`、两次第五档 native context、第二次直接中性、三次启用/停用交互所需的全部 handshakes/input provenance、重入一条真实鱼、最终 cleanup、NoNativeSave 与 lock/deployment 恢复。
- 若诊断或修复改变产品/Runtime/package 任一 hash，则上述复用失效，必须重跑受影响的完整集合。

## 6. 非正式诊断结果与最终根因

提交 `c2c731341ab0` 的短时、非正式 focused L5 使用既有 Runtime `7482fdd85dd9` 与未变产品 package `7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`，证据为：

- outer：`docs/debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260804-191406-a2ba87b2-l5-reentry-diagnostic`
- game：`docs/debug/evidence/GAME-SMOKE/20260804-191409`

该 run 完成初次直接中性、F6 启用、`5` 条预热与 `16` 条总完成鱼、F6 停用、ReturnHome、第二次第五档 `SaveLoaded` 和同一 fishing context；第三次 F6 仍未发送。第二次加载的失败异常精确保留 `actualInputMultiplier=0` 与 `actualVelocityX=-1.6153926480910741E-06`。同帧 `reentry-direct-neutral-failed` surface 证明：

- `DolocTown.AgentStateIdle`、朝右、位置仍为 `(21.59466552734375, 8.990165710449219, -0.5)`、cell `(14,6)`；
- ordinary ground 两项都为 true，ground platform、wall 和全部 conveyor instance/touched/stay 都为零；
- direct `inputMultiplier=0`、`conveyorOffset=(0,0)`、Rigidbody Y 为 `0`，只有 Rigidbody X 为同一个 `-1.6153926480910741E-06`；
- ProductNative refresh 与 direct Rigidbody 逐值一致，排除 stale cache；该精确数值又与 Review `0012` 中经过约 `300 s / 17,960` 次 refresh 仍不收敛的 D 后残余相同，排除“再等几帧即可归零”。

current build `24456188_test_E861E0` 的原生责任函数给出三条不同条件，不能被压成一个 exact-nonzero Velocity 判断：

1. `AgentStateFishingWait.NextState()` 以 `MoveModifier.inputMultiplier != 0f` 识别设备无关的直接玩家移动。
2. `AgentStateFishingWait.OnPlay()` 在调用 base 前只以 `Mathf.Abs(status.VelocityX) > 0.001f` 拒绝既有 Rigidbody 速度。
3. `AgentStateBase.OnPlay()` 随后把 `status.VelocityX` 覆写为 `status.MoveModifier.OffsetX`；Wait 再以覆写后的 `VelocityX != 0f` 精确拒绝 conveyor offset。

当前产品只读取 input 与旧 VelocityX，并把任意旧 VelocityX 非零同时当成第 2、3 条，因此在 `input=0 / offset=0 / tiny residual VelocityX` 上比原生 Wait 更严格，且永远停在 neutral arming，原生 cast/Wait 没有机会执行其自身的覆写。这才是 L5 re-entry 阻断；不是允许任意浮点 epsilon，也不是加载时序或 QA cache 问题。

## 7. 有界修复授权

只授权把产品和 optional QA 的 movement 判定机械对齐上述三条原生分支：

1. ProductNative cache/availability/snapshot 增加当前 `MoveModifier.OffsetX` 的只读 getter；任一 input、VelocityX 或 OffsetX 缺失/非有限继续 fail-closed。
2. movement policy 的顺序固定为：`inputMultiplier != 0`；否则 `abs(existing VelocityX) > (double)0.001f`；否则 `OffsetX != 0`。前两条分别保留 manual/native reason，第三条使用独立 conveyor/native-offset reason。阈值必须直接来自原生 `0.001f` 分支，不能改成可配置 epsilon；OffsetX 仍按精确非零判断。
3. 初次与 re-entry direct-neutral gate 使用同一 native-parity movement policy，并继续额外要求 direct input/offset、Rigidbody Y、位置/cell、ground/platform/wall/conveyor 全 surface 在 `1000 ms` 内不变。`input=0 / offset=0 / abs(VelocityX)<=0.001f` 只表示原生 Wait 对旧速度的可接受前置，不得跳过位置稳定性。
4. Unit/QA Unit/source trace 必须覆盖阈值两侧、tiny residual + zero offset、tiny residual + nonzero offset、真实 A 输入、unavailable/非有限与原有 phase cleanup；不得恢复键盘 fallback、blind window 或 QA native write。
5. 该修复会改变 AutoFishing 产品 DLL/package，故先前 L0--L4 不能与新产品拼接。修复后必须从头重跑八项正式 behavior matrix、完整正式 L0--L5 GC ladder 和正式 Manager lifecycle，并重新绑定 SDK/package/entry/manifest/policy hashes；Runtime 五 DLL若不变可继续保持其独立同源证明。

除上述 native-parity 修正外，不授权改 22 Hook、InstantBite、动画/charge、公共 API、Runtime/GameBridge、policy/receipt schema 或玩家存档。
