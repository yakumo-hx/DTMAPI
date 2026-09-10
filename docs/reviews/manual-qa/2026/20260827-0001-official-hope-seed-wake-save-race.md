# Manual QA Review: 官方“希望”返种在睡醒存档前尚未落地

## Review Header

- Time: 2026-08-27
- Status: recorded
- Source: 用户关于“最近自动化种植越种种子越少”的反馈、两张聊天/官方已知问题截图，以及用户后续排除项。
- Scope: Doloc Town 官方采集无人机收获带“希望”基因作物时，睡眠快进、返种创建与睡醒自动存档之间的时序；当前本机 public build `24966367` / `1.00.06` 的静态复核。
- User constraints: 排除“种子只是留在无人机背包中”和“边缘盆掉落超出采集范围”两条解释；边缘盆也不符合当前问题的速度和讨论范围。
- Related review/update/debug records: 无 DTMAPI 实现记录；这是官方内容根因 Review，不是修复或运行时通过证明。
- Files/docs inspected:
  - `PROJECT.md`
  - `docs/planning/DolocTownModdingAPI.md`
  - `docs/planning/Debug.md`
  - `references/README.md`
  - `docs/debug/INDEX.md`
  - `docs/workflows/codex-feedback-to-goal.md`
  - `docs/reviews/README.md`
  - `docs/workflows/document-governance.md`
  - `docs/debug/protocols/test-artifact-retention.md`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/CropGeneFunctionHope.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/plant_tbcropgene.json`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/PlantBasin.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Crop.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/CropDecorator.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/EquipmentPatch.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/IDropItemHost.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/DropItemManager.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/SpecialDropItem.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/GameData/ArchiveDataHandle.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Room.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/AutomateSystem.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/AutomateBotDecisionMakerGathering.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/AutomateTaskPickupItem.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/SleepUiState.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocAPI.cs`
  - `references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/AutomateBot.cs`
  - available `CropGeneFunctionHope.cs` history from build `23249387` through `24966367`
  - `references/doloc-town/official-workshop-docs/update-notes/README-DolocTown-Workshop-Update-0.96.06.md`
  - current build `UniTask.dll` decompilation for `UniTask.Delay` / `DelayPromise`
  - installed Steam manifest `D:/Steam/steamapps/appmanifest_2285550.acf`
- Not inspected: 尚未发布的“下个更新补丁”字节；没有改动或启动游戏，也没有拿玩家实时 Steam AutoCloud 存档做 native-save 实验。

## Issue Review

### Issue 1: 采集无人机睡眠收获后，“希望”返种错过睡醒自动存档

Original feedback, in order:

1. “关于自动化种植导致种子消失的问题，这个大家有什么头绪吗？这个到底是普通的种子丢失了，还是说希望基因没有正常产出。”
2. 用户判断第一种候选可能只是种子藏在无人机背包里；背包容量无限，且背包内种子不会再次种植，因此该候选不能解释长期越种越少。
3. 用户排除边缘盆：边缘盆掉落范围问题不应引起当前讨论，而且减少速度不会太快。
4. 用户提供官方已知问题公示，要求沿官方“睡醒后等待一秒再手动存档”的解决方法继续追查。

Screenshot/log transcription:

- 聊天截图：有人将问题概括为“自动化种植导致种子消失”，并询问究竟是普通种子丢失还是“希望”基因没有正常产出。
- 官方已知问题截图：
  - 标题为“已知问题公示”。
  - 公告说明这些问题将在下个更新补丁中修复。
  - 与本问题对应的原文为：“采集无人机收获带‘希望’基因作物后返还的种子在主角睡醒时没有及时保存（可以通过在睡醒后等待一秒再手动存档临时避免该问题）”。

Review record:

- User-confirmed facts:
  - 观察目标是最近自动化种植过程中种子库存逐步减少。
  - 本次不再把无人机无限背包内的隐藏库存或边缘盆掉落范围当作主因。
  - 官方已把问题收窄到“采集无人机 + 希望基因 + 睡眠收获 + 睡醒保存不及时”。
- Screenshot/log observations:
  - 官方没有说普通播种扣种事务会吞种，也没有说“希望”判定不触发；其表述是返还种子已经进入生成流程，但没有及时进入睡醒时提交的存档。
  - 官方临时规避法包含两个必要动作：醒来后等待约一秒，以及随后手动存档。只等待而不产生新的成功存档，不会改写已经完成的睡醒自动存档。
- Code/doc facts inspected:
  - public `1.00.06` 的 `CropGeneFunctionHope.DropSeed(bool)` 先克隆当前种子，再通过 `UniTask.Delay(200).ContinueWith(...)` 延迟调用 `PlantBasin.CreateDropItem(...)`。返种的权威物品状态不是在收获回调中同步落地。
  - 同一构建的 `SleepUiState.FallAsleep(...)` 在睡眠快进完成回调中直接调用 `DolocAPI.OnWakeUp(saveData, ...)`；完整睡眠传入 `saveData: true`。
  - `DolocAPI.OnWakeUp(...)` 在 `saveData` 为真时立即调用 `SaveGame(archiveHandle.archiveIndex)`，没有等待或冲刷尚未执行的“希望”返种 continuation。
  - 当前 `UniTask.Delay(200)` 默认使用 `PlayerLoopTiming.Update` 和 `Time.deltaTime`；其 `DelayPromise` 只有被 Unity PlayerLoop 再次驱动时才累加 elapsed。`PassTimeNoControl` 则在同一调用栈中用 `for` 循环跑完整段无渲染时间，随后同步执行 `_AfterTimePass` 和睡醒回调。
  - `Room.AfterTimePass()` 在睡醒回调前根据当时已有的 `DM_dropitem` 重建渲染；当前延迟返种在这次重建和自动保存之后才加入，因此还存在“数据随后出现但没有 renderer，直到下次房间重渲染”的隐藏窗口。
  - `CropGeneFunctionHope.cs` 与上一 public build `24788406` 的对应文件 SHA-256 完全相同；`SleepUiState.cs` 也完全相同。当前 `1.00.06` 尚未包含公告所说的下个补丁修复。
  - 可用历史中的 build `23249387`、`23465763`、`23762374`、`24256979`、`24456188`、`24567135`、`24585411`、`24650773`、`24788406`、`24966367` 均保留同一个 `UniTask.Delay(200)`。历史只能证明该节拍长期存在，不能单凭字节证明开发者主观意图。
  - `0.96.06` / build `23465763` 只给 Hope 的 `AfterHarvest` / `AfterClearWither` 各增加一次 `base.*`，没有改动 `DropSeed` 的延迟；build `24256979` 又出现第二次重复 `base.AfterHarvest`。当前 `CropGeneFunction` 的这两个基类方法均为空，`CropDecorator` 也会独立遍历并调用全部基因，因此现有执行代码不支持旧说明中“base 链继续传递其他基因回调”的因果推断。能确认的是公告与字节差分，不能确认空 base 调用为何或是否直接修复了当时的“共生滋养 + 希望”组合问题。
  - 当前 `AutomateBot` JSON 构造路径继续使用 `this.inventory = inventory ?? new LinearInventory(proto.inventorySize)`，已经保存的无人机库存会被恢复；这不支持“每次读档都清空无人机背包”的当前主因。
- Codex inference:
  - 睡眠的无渲染时间推进会让无人机在快进期间收获，`Hope.DropSeed` 只向 Unity PlayerLoop 登记延迟任务。同步快进没有中途帧，因此危险范围不是“临近醒来的最后 `200 ms`”：**整段睡眠快进内登记的 Hope 返种都会排到 `_AfterTimePass` 和睡醒 native `SaveGame` 之后。**
  - 醒来等待一秒给延迟 continuation 足够的实际帧时间，让返种成为地面掉落或随后进入可序列化库存；再使用睡眠菜单的“仅保存”路径，新的 native save 才会包含它。
  - 这能形成“长期逐步减少”的玩家观感：每次游戏会话最后一次睡眠快进产生的整批返种都错过该次自动提交；若返种落地后没有第二次成功保存便退出、崩溃或冷读档，这一批会丢。持续游玩并在返种落地后再成功保存，旧批次本身不会无条件继续减少。
- Ownership:
  - 官方 ProductNative 内容：`CropGeneFunctionHope` 拥有返种状态创建；`SleepUiState` / `DolocAPI.SaveGame` 拥有睡醒提交边界。
  - 这不是 DTMAPI Strict/Advanced mod、GameBridge 或普通播种扣种路径的已证实问题。
- Root-cause hypotheses:
  1. **Confirmed by official notice plus matching current code:** “希望”返种的延迟物品创建与睡醒即时存档发生竞态，存档先于返种权威状态落地。
  2. Possible implementation detail to verify after the official patch: 官方应把物品状态创建提前为同步操作，只延迟粒子/动画；或者在睡醒保存前可靠地完成相应待处理状态。单纯把 `200 ms` 改成另一个魔法延迟不能建立提交保证。
- Repair assessment (2026-08-27 follow-up):
  - `EquipmentPatch.CreateDropItem(Item, bool, bool)` 最终同步调用 `DropItemManager.AddData`；一旦该调用返回，返种已经进入 `IDropItemHost.DM_dropitem` 的可序列化对象图。问题不是保存器忽略了现有掉落物，而是保存发生时这一步尚未执行。
  - **Most likely intent, not proven author intent:** `PlantBasin.Harvest` 先同步生成普通作物产出，再调用基因回调，最后再生或清除作物；Hope 在捕获种子与盆中心后延迟星光和返种，最像是为了把“希望”星光/种子弹出与普通收获树叶、声音、产物错开约 `200 ms`。配置语义仅为“被收获或枯萎后被清理时会掉落种子”，没有“等待”或“睡醒后才结算”的玩法承诺。
  - `0.96.06` 的组合基因历史不能用来反推 `200 ms` 是组合链或生命周期屏障：该版本没有修改延迟，新增的 base 调用在可见基类中为空，组合回调本身又由 `CropDecorator` fan-out。真正起效的变化可能在配置、资源或当前差分未解释的其他因素中。
  - 当前代码中的更强惯例是 `state now, presentation later`：`ForageGrass.TakeFeeds` 先同步降低等级、`PlantBasin.TakeFeeds` 先同步回退作物，延迟闭包只更新 renderer/粒子；气生根与蓄电基因的延迟也只承载特效。Hope 把星光和权威物品创建包进同一闭包，更像表现层延迟误包了状态写入。
  - **Recommended official repair:** 在 `CropGeneFunctionHope.DropSeed` 中同步创建返种，把 `200 ms` 仅保留给星光粒子等非权威表现。可概括为 `create returned-seed state now -> optionally schedule cosmetic effect`。
  - **Acceptable minimal repair:** 完全删除 `UniTask.Delay(200).ContinueWith(...)`，立即执行粒子和 `CreateDropItem`。当前其他收获/设备路径已经广泛同步创建掉落物；静态检查没有发现“希望”必须延迟物品状态才能维持的生命周期前置条件。代价是返种掉落及星光比当前早约 `200 ms`，会与普通收获效果更重叠。
  - **Insufficient repair:** 把 `200` 改成 `0`、`1` 或一个更短值但仍保留异步 continuation。只要 `CreateDropItem` 仍可能在 `SaveGame` 之后执行，提交竞态就仍然存在。
  - **Not recommended:** 在 `DolocAPI.OnWakeUp` 或全局 `SaveGame` 前统一等待 `200 ms`。这会把一个单一基因的 ProductNative 问题扩散到所有保存入口，仍不能覆盖手动保存、命令保存或未来其他异步状态，也没有可证明的 pending-work 完成条件。若等待方式是主线程 `Sleep`，PlayerLoop 同样不能推进，等待本身甚至不会让当前 continuation 完成。
  - Removing the state delay also removes a stale-callback window: 当前 continuation 捕获 `PlantBasin`，即使这 `200 ms` 内盆被清除、移除或场景状态变化，仍会尝试通过旧对象生成掉落物。同步创建不会留下该窗口。
  - `ForageGrass.TakeFeeds` 与 `PlantBasin.TakeFeeds` 提供了当前官方代码内的正确对照：它们先同步降低生长/作物状态，再只延迟 renderer/粒子更新。`Hope` 应采用同一“状态先行、表现可延迟”原则。
  - `AfterClearWither` 复用同一个 `DropSeed`，因此修正共享方法可同时消除枯萎清除返种的同类保存窗口；不要只在采集无人机任务外层打补丁。
  - **Observable automation change requiring acceptance:** 同步生成后，返种会在睡眠剩余的模拟秒中进入 `DM_dropitem`，后续采集无人机决策可以拾取、入箱，农业无人机也可能在条件满足后再种下；当前延迟实现则把所有返种挡在整段快进之外。普通作物产出本来就同步参与这套自动化，因此静态上更像恢复一致语义，但仍须用总量和吞吐量 A/B 验证，不能只看醒来时地面位置。
  - 同步 `CreateDropItem` 不会在当前调用栈中递归触发拾取或播种；后续行为必须等无人机决策更新。多无人机通过 drop-item locker、`IsRemoved` 和 `Host.RemoveDropItem` 的成功条件避免同时取走同一对象。同步化本身不复制种子；真正的重复风险来自错误地用 Postfix 额外生成一颗而没有跳过原始延迟路径。
  - 收获回调内同步加入的是独立 `DM_dropitem`，之后 `ClearCrop` 只回收作物 renderer 并把 `crop` 置空，不会删除返种，也不会修改正在枚举的 equipment 集合。枯萎清理时盆、Host 和 `currentSeed` 也仍有效。
  - 细小但真实的兼容差异还包括：掉落位置和 Unity 随机数在收获当刻而非 `200 ms` 后决定；种子更早可碰触；若保留延迟星光，应检查盆已移除/房间已切换，避免旧位置幽灵特效。当前静态路径没有发现这些差异会造成数量丢失或无限递归。
  - 若官方必须保持“种子本体也晚 `200 ms` 才可见/可被自动化使用”，则需要可序列化、可幂等、可取消并能在保存边界冲刷的 pending-return 状态；简单 fire-and-forget continuation 或给现有掉落物事后补 renderer 都没有这样的保证，复杂度明显高于直接同步结算。
  - 当前 `AfterHarvest` 还连续调用两次空实现的 `base.AfterHarvest`；在当前基类中没有行为，与本次丢种无关，不应混入同一修复结论。若未来基类获得状态行为，应单独复核这一重复调用。
- Rejected/unproven hypotheses:
  - Rejected for this investigation: 种子只是藏在容量无限的无人机背包里；该路径不能解释公告明确命名的睡醒保存窗口，当前构造器也会恢复已序列化 inventory。
  - Rejected by user constraint: 边缘盆生成位置超出采集范围。
  - Not supported by inspected path: 普通自动播种事务每轮固定吞掉一颗种子。
  - Not yet runtime-proven locally: 在完全隔离的冷启动 A/B 中，立即依赖睡醒自动存档必丢、等待一秒并“仅保存”必保。官方公告与静态路径强烈支持，但本 Review 不把静态分析冒充本机 runtime acceptance。
- Required downstream updates:
  - 若只等待官方补丁：不需要 DTMAPI Update。
  - 补丁发布后，应比较 `CropGeneFunctionHope.DropSeed`、睡醒提交边界及相关序列化字节，并把结果追加到本 Manual QA Review；只有实际运行游戏才可新增 smoke 证据。
  - 只有用户明确要求 DTMAPI 临时兼容措施、且官方补丁后仍可复现时，才建立实现 Update 和对应 Debug/Hook 记录。
- Acceptance checks:
  1. 使用 AutoCloud 隔离、可处置、可冷重启的相同起始 fixture；盆位放在采集范围中央，排除边缘盆。
  2. fixture 至少含一颗带 `hope` 的可收获作物、一台启用采集作业的无人机，并在测试前统计盆中种子、地面掉落、无人机 inventory、输入/输出容器等所有权威位置的精确总数。
  3. A 路径：使收获发生在完整睡眠的时间快进内；只依赖醒来自动保存，不触发第二次 native save；确认内存中的延迟返种事件发生后退出，再冷启动读取同一 fixture，记录精确总数。
  4. B 路径：从与 A 字节相同的起点重建；同样睡眠收获，醒来等待至少一秒，再使用“仅保存”；退出并冷启动，要求返种总数恰好多一颗且无重复。
  5. 补丁验收：A 路径也必须在冷读档后保留返种；B 路径继续无丢失、无重复。记录 startup、选槽、SleepUiState、native SaveGame/SaveSaved、延迟返种次序和无残留进程证据。
  6. 追加当前画面中的手动收获后立即“仅保存”控制，证明补丁不只遮住睡眠入口；再覆盖无渲染睡眠收获、枯萎清除、同夜多盆和“共生滋养 + 希望”组合，逐项要求冷读档总数守恒且无重复。
  7. 若保留延迟特效，验证返种在同步调用返回后已经进入 `DM_dropitem`，而延迟任务只包含粒子/renderer 行为；不得仅以屏幕上看见种子作为提交证明。
  8. 对补丁前后使用相同长睡眠 fixture 做自动化闭环 A/B：逐秒或在关键节点统计地面、采集无人机 inventory、容器、农业无人机 inventory、盆中已种种子的总量与位置，确认同步返种只改变后续可见时点/吞吐，不产生复制、吞失或失控的同夜循环。
  9. 至少覆盖一台与多台采集无人机、有/无匹配输出箱、农业无人机启停、一次性作物最后一轮与可再生作物非最后一轮、枯萎清理，以及收获后立即拆盆/切房的控制；`lifespan == 1` 条件必须保留。
- Blocker conditions:
  - 本机现有测试槽没有可直接复用的“希望 + 已放置自动化站”场景；不能把造场景过程混入玩家实时云存档。
  - 初查时共享 Runtime lock 正由另一个工作树用于 public `1.00.06` reverse capture；归档完成、锁释放后仍没有满足前置条件的 disposable 场景，因此本次未启动游戏。锁占用不是后续测试的长期 blocker。
  - native-save A/B 只能使用带受管 marker 且 `steamAutoCloudIsolated=true` 的 disposable fixture；退出后覆盖回玩家云存档不是可接受隔离。

## Cross-Issue Summary

- Confirmed user facts: 隐藏库存和边缘盆两条解释不再考虑。
- Screenshot/log facts: 官方已明确命名“希望”返种错过睡醒保存，并给出“等一秒后手动存档”的临时规避法。
- Code-path findings: public `1.00.06` 仍是返种延迟 `200 ms`、整段睡眠同步快进、睡醒立即 `SaveGame`；问题尚未被当前安装版本修复。最强意图推断是视觉错峰，不是延迟玩法结算。
- Risks: 玩家看见返种已经出现，不代表最近一次自动存档包含它；立即退出/崩溃/冷读档会暴露差异。同步化的主要新可观察差异是返种可在剩余睡眠模拟中继续参与自动化，而不是数量层面的已知不守恒。
- Suggested implementation scope: 首选等待官方补丁；稳健修复应把权威物品状态提交与表现延迟分开，而不是由 DTMAPI 全局延迟所有睡眠保存。
- Items that should not be carried forward: 无人机背包容量、边缘盆半径、泛化为所有普通自动播种吞种。

## Implementation Record Decision

- Create/update an implementation update record: no。当前是官方问题的 audit/root-cause Review，没有 DTMAPI 实现变更。
- Additional debug/API/hook/smoke records required: 当前 no。只有补丁字节复核或隔离 game acceptance 产生新事实时再追加；静态检查不写 smoke PASS。
- Suggested task titles: “Doloc Town 希望返种睡醒提交竞态补丁差分复核”；“希望返种 disposable cold-save A/B”。
- Completion standard: 官方补丁的源/IL 差分解释其提交语义；隔离 A/B 证明睡醒自动保存本身保留且不重复返种，并包含一次冷读档验证。

## 2026-09-07 Official 1.00.07 Static Follow-Up

- Owning capture Update: [正式版 public 1.00.07 全量逆向捕获与公告核对](../../../updates/2026/20260907-0006-public-10007-full-reverse-capture.md)。
- 新 public build `25163613` / `1.00.07` 已包含官方公告所述修复。`CropGeneFunctionHope.DropSeed(bool)` 删除 `UniTask.Delay(200).ContinueWith(...)`，在当前收获调用栈中先克隆种子并同步调用 `PlantBasin.CreateDropItem`，随后才在房间可渲染时播放星光。
- 这精确落地了本 Review 的首选边界：权威返种状态先进入可序列化 `DM_dropitem`，不再等待下一次 Unity PlayerLoop；官方没有把全局睡醒保存延迟，也没有保留更短的 fire-and-forget 状态任务。`SleepUiState` 与保存入口本次无需改变。
- 同一修订还删除 `AfterHarvest` 中重复的第二次 `base.AfterHarvest`。共享 `DropSeed` 仍同时覆盖一次性作物收获和枯萎清理路径，`lifespan == 1` 门保持不变。
- Static outcome: 先前确认的“返种状态晚于睡醒 native save”根因已由新字节直接消除；这是官方静态修复证据，不是本机玩家行为验收。
- Remaining acceptance gap: 本次没有创建或写入 disposable save fixture，也没有运行睡眠收获、立即退出、冷读档、同夜多盆、多无人机或“共生滋养 + 希望”总量矩阵。原 Acceptance checks 仍是把该结论提升为 runtime/user verified 的必要条件；不新增 smoke 行。
