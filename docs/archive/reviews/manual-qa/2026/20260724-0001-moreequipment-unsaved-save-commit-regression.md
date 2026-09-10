# Manual QA Review: MoreEquipmentSlots 未保存回档与 Sidecar 提交边界

- 时间：2026-07-24 12:00 +08:00
- 状态：`recorded`
- 来源：用户对 Doloc Town 保存语义的明确说明，以及对 MoreEquipmentSlots 盾牌每次受击同步写盘审计结论的纠正。
- 范围：保存语义、当前 ProductNative/Compatibility 事务路径、历史行为基线和最小回归门；本记录不实现修复。
- 用户约束：多洛可崩溃、强退或不保存退出会丢失未保存数据；正常玩家流程在睡觉时保存。Mod 的每存档玩法数据不能比官方存档领先。
- 相关 Update：[MoreEquipmentSlots Eighth Advanced Product](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
- 相关历史 Review：[0.2.8 follow-up issue 4](20260606-0002-028-manual-qa-followup-review.md)
- 相关 Hook：[MoreEquipmentSlots focused map](../../../../hook-map/focused/MoreEquipmentSlots.md)

## 问题 1：盾牌与装备事务提前越过官方存档提交边界

原始反馈：

- “多洛可崩溃或强退、不保存退出会丢失未保存数据，游戏内只有睡觉才会保存数据。”
- 每次盾牌实际受击同步写 sidecar 不能只作为“即时耐久性与战斗流畅度”的 P2 取舍；如果官方存档回到早晨而 sidecar 保留白天状态，就会造成耐久不回档或永久丢失已经破碎的盾牌。
- 图片转写：无截图。

审查记录：

- 用户确认事实：
  - 正常玩家流程中，未保存的一天会在返回标题、不保存退出、崩溃或强退后丢失。
  - 因此装备、盾值和盾牌破碎等 Mod 玩法状态也应回到同一个最近保存检查点。
- 截图/日志观察：本轮没有新的游戏日志或截图；问题由用户确认的产品语义和当前源码路径共同成立，不把它冒充已经发生的玩家丢物报告。
- 代码/文档事实：
  - 官方完整睡眠在 `SleepUiState.FallAsleep(..., saveData:true)` 后进入 `DolocAPI.OnWakeUp`，再调用 `DolocAPI.SaveGame`。当前构建也存在睡眠界面的仅保存、可配置的小睡保存、部分 `KillTimeUiState` 路径和诊断命令等其他 `SaveGame` 入口。因此实现层的准确提交点是成功的 native `SaveGame`，不是某一个 UI 按钮：`references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/SleepUiState.cs:83,94,106,131`，`DolocAPI.cs:765,4248,4267`。
  - DTMAPI 的 `DolocTownHookCallbacks.SaveGamePostfix` 只有在原生返回成功时才分发 `SaveSaved`：`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:159-165,1336-1344`。
  - ProductNative 盾牌路径在真实抵挡后直接调用 `PersistDocument("shield state changed")`；盾牌破碎前已经清空槽记录，而 `ReturnedToTitle` 只丢弃内存引用，不能撤回磁盘中的新状态：`products/first-party/MoreEquipmentSlots/src/Native/MoreEquipmentSlotsNativeRuntime.cs:542-633,437-458`。
  - 普通 `EquipFromBackpack` 和 `RequestUnequip` 在玩家操作时就持久化 prepared journal：同文件 `:352-396`。当前恢复规则把未开始 native attempt 的记录判为 `RetryPlacement`，使一次未保存会话中的装备/卸下意图可能在未来保存时重放：`products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotTransactionJournal.cs:328-340`。
  - Compatibility Host 的旧 ABI 手动装备/替换/卸下路径也持久化同类 prepared intent；这条路径只在真实旧 ABI demand 下启用，但仍属于 0.5.5 的兼容承诺：`src/DTMAPI.GameBridge.DolocTown/Compatibility/EquipmentSlots/EquipmentSlotsCompatibilityTransactions.cs:105-159,181-236`。
  - 拆分前已经有相反且正确的基线：玩法变化只标记 dirty，`SaveSaved` 后才提交，`SaveLoaded` / `ReturnedToTitle` 丢弃未保存状态。历史 Update 和 smoke 明确记录 sidecar 时间戳在未保存退出时不变：`docs/updates/2026/20260606-0004-029-readme-implementation.md:21,60-61`；旧盾牌更新也明确“等待下一次 native SaveGame”：`docs/updates/2026/20260614-0006-equipment-slots-shield-hat-protection.md:31`。
  - 历史手测问题 4 已把“放入后不保存退出重进，原背包状态一致”列为验收条件。当前拆分没有保住这条受保护行为，属于回归，不是新的产品偏好。
- Codex 推断：
  - 盾牌受击后立即提交会让 sidecar 比官方存档领先；部分受击会导致耐久不回档，破碎会让已清除的盾牌在官方世界回档后仍永久消失。
  - 普通装备/卸下 prepared intent 跨未保存退出重放通常不立即丢物，但同样违反玩家放弃当天变化的语义，并可能在下一次睡觉时突然完成旧操作。
  - 每击 JSON/`Flush(true)` 的主线程 I/O 和分配仍是性能后果，但主问题已经是 P1 存档一致性，不能再用测量后接受来关闭。
- 反证/未证实：
  - 没有证据表明这一问题就是少数玩家长期 Unity/Mono GC 的来源；该路径不是每帧运行，产品未启用或没有额外盾牌时不会执行。
  - 不能把所有持久 journal 都删除。禁用、卸载、退订或 orphan recovery 需要在产品不再正常加载时保护物品，允许持久重试，但必须与普通 gameplay intent 明确分类。
  - 当前 `053248` 实机验证的是 `grandmas_button` 保存事务，`053342` 验证的是产品禁用后的冷恢复；它们没有验证盾牌未保存回档或普通 equip/unequip intent 丢弃。
- 归属：
  - `MoreEquipmentSlots` 的 Working/Committed 状态、盾牌耐久、普通装备事务与四个 Hook 属于该产品的 `ProductNative`。
  - 冻结旧 ABI 的执行器仍在单一 Compatibility Host，但必须采用相同的官方提交语义。
  - native `SaveGame` 成功这一提交事实及 DTMAPI `SaveSaved` 正常通知的公共不变量属于 `PROJECT.md`，不是新 SharedNative API。
- 数据分类与官方提交边界：
  - `GameplayMutation`：装备、替换、卸下、盾值变化和盾牌破碎。只更新当日 `Working`；正常运行时在 native save 成功并分发 `SaveSaved` 后提升为 `Committed`，或在中断恢复时凭精确指纹证明同一 native save 已提交。
  - `OwnerRecovery` / `OrphanRecovery`：禁用、卸载、退订和孤儿物品回收。允许显式持久重试，但保持物品恰好一份。
  - `Config/Diagnostic`：设置、按键、日志、报告和安装状态。可以即时写盘，不随游戏存档回档。
- 需要更新：
  - 由现有 Update `20260723-0008` 继续拥有修复生命周期；不新增 sidecar 收据、checkpoint 或第二套保证体系。
  - `PROJECT.md` 保存唯一规范，`AGENTS.md` 和现有 feedback/API workflow 投影执行门。
  - 当前 Batch 6 契约、路线图、API Matrix、MoreEquipmentSlots Hook map 及八/九产品收口审计撤回“第八产品数据安全 verified/closed”表述。
- 验收点：
  1. 盾牌部分受击后不保存返回标题：耐久恢复到最近一次官方提交。
  2. 盾牌破碎后不保存返回标题及冷重启：盾牌恢复且恰好一份。
  3. 普通装备、替换、卸下后不保存退出：槽、背包、邮件恢复，不保留未来待执行命令。
  4. 上述变化后完成正常 native 保存：新状态正确提交。
  5. `SaveSaving` 前、native 保存失败或无法证明 native 成功：普通 gameplay candidate 回滚，旧 committed 不损坏。
  6. native 保存成功、`SaveSaved` 通知或 sidecar promote 前中断：用精确存档身份/指纹提升候选，物品恰好一份。
  7. sidecar promote 后、journal cleanup 前中断：重启收口且物品恰好一份。
  8. 连续盾击在保存前只更新内存/界面，committed sidecar bytes/generation 不变；一个保存周期最多完成一次有界提交。
  9. Compatibility Host 的旧 ABI 普通 equip/replace/unequip 重复 3–7；owner/orphan recovery 另测持久重试和恰好一份。
- blocker 判定：
  - `P1`。第八产品的物理拆分、ProductNative 归属、默认加载 Runtime 减重及已有 owner/冷恢复证据仍成立，但行为/数据安全门必须重开。
  - StrongPlantingGun 第九产品自身的 `verified/closed` 不受此根因影响；“精确九产品全部 verified/closed”的聚合结论撤回。
  - 在本问题修复并通过上述最小矩阵前，不准入第十产品，也不把 0.5.5 作为可发布候选。

## 建议状态机

```text
SaveLoaded
  -> Committed C
  -> 当日 Working W
  -> SaveSaving: 可写未提交 Candidate/Journal P
  -> native SaveGame 成功（官方提交事实）
  -> 正常路径 SaveSaved: P 提升为 Committed C+1

native SaveGame 未成功或无法证明成功
  -> GameplayMutation 丢弃 W/P，恢复 C

native 已成功但 SaveSaved/sidecar 提升前中断
  -> 以精确存档身份/指纹收口 P，保持恰好一份

OwnerRecovery / OrphanRecovery
  -> 作为显式管理事务单独允许持久重试
```

该模型保留现有两阶段事务在真实跨存储崩溃窗口中的价值，同时禁止它把普通白天操作变成隐式自动保存。

## 问题 2：无保存测试不应靠备份写回制造“回档”

原始反馈：

- “存档测试不需要备份、写回存档。”
- 图片转写：无截图。

审查记录：

- 用户判断成立的范围：
  - 普通功能、Hook、UI、标题循环、GC 和长测只要不触发 native `SaveGame`，就应由官方返回标题/退出语义自然回到最近一次提交；这种 `NoNativeSave` 测试的绿色路径不需要也不应写回玩家 archive。
  - 绿色路径不做例行存档字节备份；只有显式说明高风险理由时才可保留异常安全快照，而且它只用于测试失控后的人工恢复。测试必须在测试框架或外部文件恢复前证明 current / prev / bak archive、相关 committed sidecar 和玩家可见状态均未变化。需要恢复即为非验收失败，`PlayerSaveRestored=Passed` 不能证明回档正确。
  - 该判断不覆盖明确调用 native `SaveGame`、官方存档复制/删除或启动加密/索引修复的测试；这些路径确实会修改 archive，但应使用与玩家实时 Steam AutoCloud 隔离的可处置 fixture。
- 官方/运行事实：
  - 系统菜单返回标题只执行 motor/room 清理与 `ReturnHome`，最终卸载游戏状态；普通退出只调用 `Application.Quit`，`OnApplicationQuit` 也不保存：`references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/SystemMenuUiState.cs:39-53`，`DolocAPI.cs:782-817`，`GameManager.cs:298-309`。
  - 正常保存写盘发生在 `DolocAPI.SaveGame` 调用链，并通过临时文件替换正式 archive；成功返回是正常提交信号，文件已经替换、随后在回调或返回前中断的窗口仍须用精确原生提交指纹判断。启动校验、复制和删除属于单独的 archive mutation：`DolocAPI.cs:765-774`，`DataPersistenceManager.cs:91-107`，`LocalSave.cs:90-163,230-274`。
  - 已带 `UnchangedBeforeRestore` 的现有 smoke 也支持分型：抽查的普通生命周期/无保存路线所选存档三件套保持 `3/3` 未变；真实执行 native 保存的 MoreEquipmentSlots `053248` 和 StrongPlantingGun `101757` 则有 current/prev 两项变化。Strong 保存前失败的 `095510` / `095853` 仍保持 `3/3` 未变。更早只记录恢复后 hash 的 receipt 不能补充证明“恢复前未变”。
- 当前测试基础设施问题：
  - `tools/scripts/run-game-smoke.ps1` 的 `RequirePlayerSaveRestore` 路线会先复制玩家存档，并在游戏退出后无条件复制回去。脚本虽然计算 `UnchangedBeforeRestore`，普通存档变化并不普遍使测试失败，因此恢复会掩盖意外 native 保存。
  - G5 路线还会恢复整个 DTMAPI config 目录，其中包含 MoreEquipmentSlots sidecar；这会同时掩盖 sidecar 提前提交。玩家 archive 与 committed sidecar 都必须在任何恢复前验收。
  - Batch 5 no-demand/GC 与三个 AutoFishing 第五存档 wrapper 将 `RequirePlayerSaveRestore` 固化到本应无保存的路线；MoreSaves UI、外部输入、发布产品组合和 no-QA UI 等路线也被过度纳入自动写回。相反，真正的 native-save/InstantSave 路线没有形成统一隔离门，当前分类方向倒置。
  - 截至 2026-07-24 本轮审查，当前 `docs/debug/evidence/GAME-SMOKE/**/player-save-before` 已累计 908 个文件、约 1.38 GiB。无保存路线改为只记录 length/hash/mtime，不仅更符合语义，也会减少重复 I/O 和证据膨胀。
  - Steam `cloud_log.txt`、`remotecache.vdf` 与 `steam_autocloud.vdf` 证明第三存档受 AutoCloud 管理；`101553` 与 `101757` 两轮中，Steam 都在游戏进程退出附近先扫描，runner 随后才写回备份。同一近期日志另有 SHA mismatch/conflict 记录，但不能据此断言由 runner 造成。已能确定的是，“退出后 Copy-Item 恢复实时存档”存在竞争窗口，不是可靠隔离。该审查没有修改 Steam、云端或玩家存档状态。
- 功能代码影响：
  - 本轮对当前九个第一方产品的源码扫描中，发现会在普通玩法变更时提前写 gameplay sidecar 的产品只有 MoreEquipmentSlots：盾牌变化立即写 committed 文档，普通 equip/replace/unequip 立即写 prepared journal；冻结旧 ABI Host 具有同类普通事务风险。
  - StrongPlantingGun 的 QA fixture 与 MoreEquipmentSlots 的已保存事务 fixture 是有意调用 native `SaveGame` 的 commit-path 测试；DebugConsole/Y 键“存这里”也直接调用 InstantSave。它们不是 NoNativeSave 证据，未来必须使用隔离 fixture。玩家可见的“存这里”还应明确提示它会立即永久提交当前游戏状态。
  - 其他第一方产品本轮找到的即时写入是 Mod 配置，不随官方存档回档；CustomEntity persistence DTO 尚没有当前活跃 Host 写入者。未来 Content Host 仍必须遵守 `PROJECT.md` 的提交边界。
- 工作模式调整：
  - `NoNativeSave`：禁止保存入口；回标题/冷重启后验证玩法状态，并在任何测试框架或外部文件恢复前比较 archive 与 committed sidecar；绿色路径默认只记录 length/hash/mtime，不创建或写回完整备份。
  - `NativeSaveExpected`：验证真实提交、`SaveSaving`/`SaveSaved` 和重载后的新状态；使用云隔离的可处置 fixture，再清理。
  - `ArchiveMutation`：MoreSaves 复制/删除、启动修复/迁移等独立隔离测试。
  - `OwnerRecovery` / `OrphanRecovery`：继续允许显式持久重试，但与普通玩法回档分开验证。
  - runtime lock、安装/部署、官方 profile、QA 配置和 Author source 恢复仍保留；“不写回存档”不等于取消共享运行环境保护。
- 证据影响：
  - 已有 Hook 命中、UI 行为、owner cleanup、冷恢复和真实 native-save 证据按其原范围继续有效。
  - 只以 `PlayerSaveRestored=Passed` 收口的历史运行不再能证明 NoNativeSave；不倒改历史 receipt，但未来重验必须采用恢复前 unchanged 门。
  - MoreEquipmentSlots 的 P1 修复必须先修正上述 runner 模式，再做一轮无保存回档与一轮隔离 native-save 提交验收；无需完整 Release、L0-L5、GC 梯度或长测。

blocker 判定：

- `P1` 测试基础设施：当前自动写回既可能掩盖无保存回归，又与 Steam AutoCloud 竞争。在 runner 分型完成前，不应再用实时第三/第五存档执行会保存的 commit-path 验收。
- `P1` 功能代码仍是问题 1 的 MoreEquipmentSlots 普通玩法提前提交；本轮没有发现第二个现役第一方 gameplay-sidecar 产品需要同步修复。
- 本轮只修订约束与证据口径，不修改 runner、产品源码、Steam 或存档，也不启动游戏。

## 2026-07-24 修复与复核结论

本 P1 已关闭：

- ProductNative 与冻结 ABI Host 均实现显式 `Working` / `Committed`；
  受击、破碎、装备、替换和卸下不再提前提交普通玩法状态；
- `GameplayMutation` 只能在成功 native save 后提升，
  `OwnerRecovery` / `OrphanRecovery` 仍作为独立管理事务持久重试；
- focused Product/Host/故障注入覆盖未保存回档、成功保存、中断提升、
  tombstone cleanup、三态放置与恰好一份逻辑物品；
- `155216` / `155344` 通过真实第三存档 `NoNativeSave` 标准，
  archive 与 committed sidecar 在清理前不变，绿色路径无备份/写回；
- `161422` / `161536` 通过 Steam AutoCloud 隔离 fixture 的真实保存、
  提升、冷重载与恢复标准；
- `154906`、`155811`、`161030` 保持 non-acceptance，历史
  `PlayerSaveRestored=Passed` 不被倒推为 no-save 证据。

独立复核未发现剩余 P0/P1/P2。Update `20260723-0008` 恢复
`verified/passed/closed`；完整 Release、L0-L5、GC 梯度和长测均未运行。
