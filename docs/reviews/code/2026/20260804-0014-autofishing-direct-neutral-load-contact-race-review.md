# AutoFishing 直接中性预飞加载接触时序审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / root cause closed / bounded same-position load-contact settling authorized`
- 性质：AutoFishing 正式 behavior matrix 在七个 profile 通过后，由最后一个 profile 的首帧地面接触假阴性停止的连续根因审查
- Audited HEAD：`c72afe9bbdcb6b787b2f65f79faf1e09a3574848`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 前置根因：[AutoFishing 朝向预飞稳定中性握手根因审查](20260804-0012-autofishing-facing-preflight-neutral-handshake-review.md)
- 外层 evidence：`docs/debug/evidence/BATCH6-AUTOFISHING-BEHAVIOR-MATRIX/20260804-170252-b9909791`
- 七个通过 profile：`docs/debug/evidence/GAME-SMOKE/20260804-170312`、`docs/debug/evidence/GAME-SMOKE/20260804-170413`、`docs/debug/evidence/GAME-SMOKE/20260804-170507`、`docs/debug/evidence/GAME-SMOKE/20260804-170610`、`docs/debug/evidence/GAME-SMOKE/20260804-170709`、`docs/debug/evidence/GAME-SMOKE/20260804-170759`、`docs/debug/evidence/GAME-SMOKE/20260804-170854`
- 失败 profile：`docs/debug/evidence/GAME-SMOKE/20260804-170954`

本 Review 只冻结失败事实、根因、被拒绝方案和下一次有界 QA/runner 修复。实施文件、验证、重跑和发布状态只由 owning Update 维护。它继续属于第四轮后的 AutoFishing 同一功能切片，不新增独立切片计数。

## 1. 已确认事实

1. 干净 HEAD `c72afe9bbdcb` 的 canonical PowerShell 7 Release 已完整退出 `0`。随后事务安装的五个 Runtime DLL 与同一 HEAD Release 输出逐字节相同，manifest/state 为 `0.6.0` / `0.6.0.0` / `c72afe9bbdcb`，Player Doctor 报告 `5 artifacts / 0 errors / 0 warnings`。
2. 正式第五档 `NoNativeSave` behavior matrix 使用同一 SDK ZIP `B965C27CFC914D3268FBC95CE207E3800F9A62A53E6808559C251F5E9B92D05C` 与 AutoFishing 包 `7A9C1A66DE1DB3D0D37154161D3C3C8D17FEC76B3DBDED1F884CB8B919ACA753`。`DefaultLoop`、`InstantBite`、`SkipMiniGame`、`FastAnimations`、`InstantSkip`、`CombinedInstantComplete` 与 `NonZeroCastCharge` 依次形成正式 `Passed` 回执；每项完成一个真实 native loop，且各自 `PlayerSaveUnchangedBeforeCleanup`、committed sidecar、进程、fatal-window、source/profile/deployment 清理门通过。
3. 七个通过 profile 的 `initial-before-input` 都在完全相同的 position `(21.59466552734375, 8.990165710449219, -0.5)` / cell `(14,6)`，并同时为 Gameplay、`DolocTown.AgentStateIdle`、朝右、ground `true/true`、platform count `0`、wall false、input/conveyor offset/Rigidbody XY 精确零、空 conveyor enumeration；连续同位置精确中性窗口分别为约 `1002.7–1006.9 ms`。
4. 最后的 `ManualMovementCancel` 在同一 position/cell、room/scene、Gameplay、Idle、朝右、platform count `0`、wall false、input/conveyor offset/Rigidbody XY 精确零和空 conveyor enumeration下，唯一差异是首次快照的 `groundTouched=false`、`groundTouchedExcludePlatforms=false`。该快照发生在 native fishing context 成立后的首轮预飞；当前 gate 立即抛出 `Direct-neutral preflight requires ordinary ground contact...`，所以 enable handshake 未发布，F7/A 均为 `Sent=false`，产品始终 disabled，尚未进入任何 movement phase。
5. 失败清理是有效安全证据而不是 behavior PASS：第五档 current/prev/bak 与 committed sidecar 在任何 cleanup 前保持 length/hash/mtime 不变，无 routine byte backup 或 archive writeback；进程、fatal-window、QA cleanup、官方 profile/source 与 SDK deployment 全部恢复，最终为 `AbsentNoJournal`，共享锁已释放且无 `DolocTown.exe`。

## 2. 根因

`Batch6AutoFishingDirectNeutralPreflightStatus` 已定义 `WaitingForDirectNeutral`，但当前 `Observe` 在任何 ordinary-ground 缺失时都直接抛错，因此该状态从不承担“加载接触尚未可见”的职责。`BeginDirectNeutralPreflight` 又要求第一次观察必须立刻进入 `WaitingForStableNeutral`。这把一次仍满足同位置、无输入、完整读取和精确中性的首帧 `ground=false/false` 当成永久不合格表面。

证据不能证明下一帧必然接地，因此不能把它直接改判为通过；但七个相邻独立进程在同一固定位置得到 ordinary ground，最后一项只有这两个 ground 布尔量不同，足以证明“首个 ungrounded receipt 必须立即终止”会产生启动时序假阴性。正确边界是先在无输入、同一位置且所有其他原生条件严格成立时做有界 load-contact settling；只有实际观察 ordinary ground 后才开始原有连续精确中性 `1000 ms` 窗口。

这不反驳产品对任意有限非零 movement 的精确取消规则，也不反驳 Review `0012` 对 D/Escape、epsilon、字段写入和坐标移动的禁止。

## 3. 被拒绝方案

- **原样重跑直到八项碰巧全绿。** 拒绝；会把已观测的进程间时序假阴性留在正式验收工具中。
- **删除 ground 前置或把 ground=false 当成合格。** 拒绝；正式 enable 仍必须由真实 ordinary-ground receipt 授权。
- **固定 sleep 后不读取 owner。** 拒绝；时间经过不能替代 ground、position、movement 和 contact 的直接证据。
- **允许等待期间位置/cell 漂移。** 拒绝；这会恢复 Review `0012` 已排除的“换落点再启用”路径。
- **对 wall/platform/conveyor、非零 movement、错误朝向、非 Gameplay/Idle 或读取失败也等待。** 拒绝；这些不是本次唯一差异，必须继续立即 fail-close。
- **发送 D/Escape/Jump、直接写 ground/velocity/position，或放宽产品 epsilon。** 拒绝；本次失败发生在任何控制输入前，不能用状态突变制造验收。

## 4. 授权的有界修复

只允许修改 optional QA、behavior runner 静态合同和 QA Unit；产品 DLL、22 Hook、manifest/policy/package、Runtime 五 DLL、公共 API 与玩家存档均不得改变。

1. 复用现有 `WaitingForDirectNeutral`：仅当 receipt 完整 verified，且 Gameplay、Idle、朝右、room/scene 非空、position/cell 未变、platform count `0`、wall false、两项 ground 都为 false、input/conveyor offset/Rigidbody XY 全部有限且精确零、conveyor enumeration 完整且为空时，允许保持等待。
2. 第一次无输入 receipt 即冻结 save-load ordinal、room/scene、position 与 cell；等待期间任一变化立即失败。只允许 `false/false -> true/true` 的 ordinary-ground 获得过程；不一致 ground 标志、platform/wall/contact 或 ground 在稳定窗口开始后再次丢失都立即失败。
3. 首次等待 receipt 以 `initial-load-contact-pending` / `reentry-load-contact-pending` 最多记录一次。实际接地时才记录既有 `*-before-input`，并从该时刻开始连续 `1000 ms` exact-neutral；`*-stable-neutral-ready` 仍必须与最初冻结位置完全一致。
4. 直接预飞状态使用明确 `30 s` 上限；超时保留最后完整 surface receipt 并失败，不回退到输入或阈值。正常首帧已接地路径仍只产生原有两个 primary diagnostics。
5. behavior runner 必须接受且严格验证两种唯一序列：`before-input -> stable-neutral-ready`，或 `load-contact-pending -> before-input -> stable-neutral-ready`；任何额外/重复/乱序 phase 均失败。

## 5. 验收边界

- QA Unit 覆盖：首帧 `false/false` 保持 `WaitingForDirectNeutral`；同位置接地后从零开始计 `1000 ms`；等待期位置漂移、混合 ground 标志、platform/wall/非零 movement 及接地后再次失联均抛错。
- PowerShell 7 与 Windows PowerShell 5.1 的 behavior/GC/Manager/source contract、脚本 AST 和 test-artifact governance 通过；Release QA build / QA Unit 为零 error。
- 修复须在干净提交上刷新 exact Runtime 后重跑正式第五档矩阵。前七项历史 PASS 不可拼接成新矩阵 PASS；八项必须在一个新 outer run 中从头全部通过。
- 新运行继续证明配置 toggle 前零预飞输入、同位置 exact-neutral、真实 profile 行为、NoNativeSave、进程/fatal-window、source/profile/deployment 恢复与锁释放。
