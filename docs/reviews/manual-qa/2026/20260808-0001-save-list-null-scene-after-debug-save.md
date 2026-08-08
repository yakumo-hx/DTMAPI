# 存档列表在调试原生保存后因空场景摘要无法打开

- Status: `recorded`
- Time: `2026-08-08`
- Source: 用户反馈“闪退过后存档界面打不开”及日志包 `D:\下载\DTMAPI-logs\20260808-152557-954-4fe75b90`
- Scope: 只读日志、当前源码与 build `24585411` 反编译责任路径审查；未启动游戏、未读取或修改玩家存档、未改 Runtime/Product 源码
- User constraint: 本轮只要求分析最新日志，没有授权修复或玩家存档写入
- Related records: [长期 Mono GC 问题](../../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)、[DebugConsole ProductNative Update](../../../updates/2026/20260726-0005-debugconsole-twelfth-advanced-product.md)、[MoreSaves ProductNative Update](../../../updates/2026/20260723-0004-moresaves-sixth-advanced-product.md)、[public API matrix](../../../api/public-api-matrix.md)

## 问题 1：闪退后的存档列表无法打开

原始反馈：

- “分析D盘下载文件夹下最新的日志，这个是闪退过后存档界面打不开”。
- 本次没有截图、原始存档文件或闪退时即时导出的 Unity crash dump。

日志转写：

- 最新日志包由 `2026-08-08 15:25:59` 左右的收集操作生成。当前 Runtime 为 `DTMAPI 0.6.1`、build commit `db5e518a6d7f`，五个 Runtime DLL 与 release manifest 收据一致，不是低版本 Runtime 或安装残缺。
- 长会话历史 `latest-20260808-070936921.log` 在 `15:09:36.920` 的 DebugConsole 关闭日志后终止，没有 DTMAPI 正常关机尾部。该会话最后一次原生保存是 `14:56:58` 的 slot/index `0` `SaveSaving -> SaveSaved`。日志包没有保留这次终止对应的 Unity Player 日志或 dump，因此只能确认终止不完整，不能从本包判定原始闪退的异常类型。
- 闪退后的下一会话 `latest-20260808-071421891.log` 从 `15:10:22` 开始。它在 `15:11:08` 成功完成 slot/index `0` 的 `SaveLoaded`，在返回标题后又于 `15:13:31` 第二次成功完成同一存档的 `SaveLoaded`。这两次加载都必须先经过原生 `GameDataUiState` 存档列表，证明闪退后磁盘上的列表并非立即不可用。
- 同一会话在 `15:13:52` 和 `15:14:00` 两次只进入 DebugConsole 保存确认状态；`15:14:05.398` 第三次操作实际触发 slot/index `0` 的 `SaveSaving`，`15:14:05.623` 收到 `SaveSaved`，随后 DebugConsole 报告 `confirmed native save completed success=True slot=0`。
- `15:14:19` 返回标题，`15:14:21.891` 标题页因进入另一个 UI 而隐藏，随后该 DTMAPI 历史日志终止。之后两次可见玩家尝试的 `Unity-Player-prev.log` 与 `Unity-Player.log` 都复现完全相同的错误：`GameDataUiState.Show -> GameDataPanel.Render -> GameDataSlot.Render -> BaseArchiveData.GetPositionTitle -> TbScene.GetOrDefault`，最终为 `ArgumentNullException: Value cannot be null. Parameter name: key`。
- 两份 Unity 日志都走到 `游戏关闭..` 和 Unity 退出内存统计；包内没有 Unity crash 目录或 dump。日志中的 allocator `Failed Allocations` 是退出统计字段，不是本次 OOM 证据；没有 `Fatal error in GC`、`OutOfMemoryException` 或 GC 崩溃栈。

审查记录：

- 用户确认事实：玩家经历过一次闪退；后续点击存档入口时存档界面无法打开。
- 当前原生代码事实：build `24585411` 的 `GameDataUiState.Show` 读取全部 `BaseArchiveData` 后逐槽调用 `GameDataSlot.Render`。`BaseArchiveData.GetPositionTitle` 直接以 `currentScene` 调用场景表 `TbScene.GetOrDefault`，没有空值保护；因此任一非空存档摘要的 `currentScene == null` 都会中断整个列表。
- 当前原生保存事实：`ArchiveDataHandle.baseData.currentScene` 每次序列化时来自 `farmData.currentSceneName`，而后者是 `farmData.currentRoom?.SceneRawName`。若保存时 `currentRoom` 为空，原生保存仍能成功写入一个 `currentScene: null` 的摘要。`LocalSave.SaveGame` 的成功只代表序列化与临时文件替换成功，不验证摘要能否被存档列表渲染。
- 当前 DebugConsole 代码事实：保存前置只检查 `DolocAPI`、archive handle、`SaveGame(int)` 方法和非负 archive index；不检查 native `currentRoom`、`SceneRawName` 或场景表命中。调用返回非 `false` 后即报告成功，也不重新读取并验证刚写入的 `BaseArchiveData`。
- 高置信 Codex 推断：slot/index `0` 在 `15:14:05` 的 DebugConsole 原生保存时写入了空 `baseData.currentScene`。这是本包唯一位于“15:13 仍可两次打开列表”和“此后列表持续失败”之间的存档写入，并且写入路径与后续异常字段形成完整因果链。`SaveSaved` 在这里证明 native save 返回成功，不证明存档摘要语义有效。
- 尚未闭合：日志没有记录 `15:14:05` 保存前的 `farmData.currentRoom`/`SceneRawName`，也没有包含当前 slot `0` 与 `.prev0` 的只读摘要，所以不能确定 native `currentRoom` 为什么为空。原始闪退遗留的内存状态、native load 后状态异常、DebugConsole 所在 UI/过渡状态或第三方 native mod 的先前副作用都仍是来源假设，不能选定其中一个。
- 时间线反证：原始闪退本身没有直接把磁盘存档列表破坏，因为其后的进程在 DebugConsole 保存之前两次正常打开并加载了 slot `0`。不能把“发生在闪退后”直接写成“由闪退写坏”。
- 已排除为当前直接原因：旧 Runtime/安装器字节、GC/OOM、MoreSaves 数量/分页 UI，以及 FishBreedingAssistant tooltip patch。异常栈没有 DTMAPI/Harmony 帧；失败前后 GameBridge 报告 `saveUiStates=0, saveUiPagers=0, saveUiBinders=0`，当前 MoreSaves 边界也只写 native slot count，不拥有 archive 内容或逐槽渲染。
- 仍未排除：`Codex.DolocPriceHelper` 是 `legacy-native-compatibility-unverified` 的第三方 native CodeMod，日志不能证明其没有更早改变 native 状态；但本次栈、产品日志和当前可见行为没有把它连接到 archive summary，因此不能据此归责。
- 归属：第一责任点是 DebugConsole ProductNative 的显式原生保存资格与写后校验；第二层是官方 `BaseArchiveData.GetPositionTitle` 对畸形摘要不 fail-soft。不能为了一个 Product 的保存前置缺失，先把修复错误地下沉为通用 GameBridge save owner。
- 数据分类与提交边界：这是 native save-bound gameplay/archive metadata。`15:14:05` 已完成一次真实 native commit；后续修复不能把普通文件覆盖当作验收，必须先保全 current/`.prev0...N`/`.bak` archive family，并在隔离副本或 disposable fixture 上验证。

后续修复门（本轮未实施）：

- 玩家数据诊断先只读确认 slot `0` current 与 `.prev0` 的 `baseData.currentScene`。按 build `24585411` 的 `LocalSave`，`15:14:05` 的成功替换应把此前能打开的 current 放入 `.prev0`；但未检查实际文件前不得指导玩家直接替换或删除。
- DebugConsole 保存前至少要求 active archive、有效 current room、非空 `SceneRawName` 与可解析场景摘要；不满足时拒绝调用 native `SaveGame`，并记录失败原因而不是“保存成功”。
- DebugConsole 保存返回后应只读重开当前 archive summary，确认 index、scene 与位置标题可解析；写后校验失败必须报告 semantic failure，并保留原生 backup family 供明确恢复流程使用。
- 官方列表 fail-soft（跳过/降级显示畸形位置，而不是整页失败）只能作为独立 native-owner/兼容性审查项，不能代替生产者的保存前置修复。

验收点：

- 合成/隔离测试：`currentRoom == null` 或空/未知 scene 时 DebugConsole 不调用一次 native `SaveGame`，current/prev/bak 长度、hash、mtime 均不变。
- 正常隔离存档：有效场景下确认保存一次，写后摘要 scene 可解析，返回标题并连续两次打开官方存档列表，再冷启动打开一次；slot 内容与 native backup rotation 符合预期。
- 玩家恢复：在保存全量 archive family 的只读快照后，验证选定备份摘要非空且可渲染，再用可回滚、隔离 Steam AutoCloud 的流程恢复；恢复后能打开列表并加载 slot `0`，且不以删除存档作为通过条件。
- 原始闪退若需继续归因，必须取得闪退后立即导出的 `Player.log`/`Player-prev.log`、fresh Unity crash 目录或 dump；当前包中后续启动已轮换掉对应 Unity 日志，不能补推为 GC 或任何具体 Mod 崩溃。

blocker 判定：

- 没有 slot `0` current/`.prev0` 的只读摘要前，精确玩家恢复方案仍 blocked。
- 没有原始闪退对应的 Unity 日志/dump 前，原始闪退根因仍 blocked；这不阻碍单独修复已闭合的 DebugConsole 保存资格问题。

## 实施记录决定

- 本轮仅诊断，创建这一份 Review；不创建 Update，不修改 Debug issue、Hook map、API matrix 或 smoke matrix。
- 若用户授权修复，应创建一个 DebugConsole ProductNative Update；若运行真实 native-save 验收，必须使用与玩家 Steam AutoCloud 隔离的 disposable fixture，并按保存事务规则记录证据。

## 2026-08-08 玩家支持包补充证据与恢复授权

新增来源：

- 玩家完整支持包：`D:\下载\DTMAPI-player-support-20260808-211555-289-4ca2895f.zip`。
- ZIP 长度 `14015187`，SHA-256
  `3131656F65C890414C279A337F92F42F4553D2E878913DB3F09EB968A18033D6`；
  20 个文件条目全部可解压并通过逐文件哈希读取，没有路径穿越或采集失败。
- 本次用户明确授权：先分析 `currentRoom` 为空的原因，再把本地已有的已验证备份做成玩家一键恢复；玩家电脑不再额外创建备份。
- 实施生命周期由
  [20260808-0003](../../../updates/2026/20260808-0003-player-slot0-verified-recovery.md)
  负责；本 Review 只保存根因与恢复选择依据。

存档事实：

- slot `0` current `doloc-save-0.data` 长度 `3126071`，最后写入
  `2026-08-08 15:14:05 +08:00`，SHA-256
  `B79E6E709C4B1A3F455845AF03839C77A10E0E5D91E01C2A0CFF7036D645FEE6`。
- `.prev0` 长度 `3137187`，最后写入
  `2026-08-08 14:56:58 +08:00`，SHA-256
  `328718D4705186E271C34D8A737C10A4E99AAE169218293AC522A753360799A6`。
- 仅在内存中按 build `24585411` 的原生 AES/JSON 格式解密并解析 current、
  `.prev0` 至 `.prev4`；没有把明文写入磁盘。六份文件都能完成解密和 JSON
  解析，因此 current 不是截断或随机字节损坏。
- current 的 `baseData.currentScene` 明确为 `null`，且 `farmData` 相比
  `.prev0` 唯一缺少的属性就是 `currentRoomId`。`.prev0` 至 `.prev4` 的
  `currentScene` 均为 `farm_大型集装箱`，`currentRoomId` 均为
  `farm_大型集装箱.e0f19c24-7840-4c05-bd27-550eb8d4053e`。
- 这与原生序列化路径完全吻合：`currentSceneName` 使用
  `currentRoom?.SceneRawName`，所以空 room 写出 `currentScene: null`；
  `currentRoomId` getter 直接访问 `currentRoom.RoomId`，其异常被 JSON
  serializer 的错误处理吞掉后，该属性被省略。至此，`15:14:05` 保存时
  `farmData.currentRoom == null` 以及它直接生成坏摘要均已由存档字节证实，
  不再只是日志推断。

`currentRoom` 为空的分层根因：

- 已证实的直接状态：第二次加载的 archive 数据已经可用，但原生进入房间流程没有在
  保存前完成 `Room.OnEnterRoom -> archiveHandle.currentRoom = this` 这一赋值。
- 已证实的生命周期缺口：JSON 构造器和 `FarmArchiveData.AfterLoadData` 都先把
  `currentRoom` 置空；随后 `EnterRoom` 通过 `GameStateSceneTransition` 异步加载
  scene。`DolocAPI.AfterLoadArchiveData` 在异步回调之前就发布
  `OnAfterLoadArchiveData`，而 `DolocAPI.LoadGame` 也不等待 scene callback。
  因而 `SaveLoaded`、`LoadGame == true`、archive index 有效和 DTMAPI
  `Gameplay` 输入上下文都不构成“native room 已就绪”的证明。
- 与现场最吻合的原生触发路径：日志在 `15:13:26.294` 返回标题，
  `15:13:29.607` 又进入同一 slot 的第二次加载。原生 `ReturnHome` 只调度
  `UnloadAll` 异步卸载；`SceneManager._LoadSceneAsync` 若仍在
  `loadedScenes` 看见目标 scene，会记录“已经加载”并直接返回 `true`，却不调用
  传入 callback。`GameStateSceneTransition` 因此既不失败退出，也不会执行
  `room.OnEnterRoom`，能稳定留下本案的空 `currentRoom`。
- 仍不能把上一条写成唯一已证实触发：精确覆盖 `15:13` 会话的 Unity
  `Player.log` 已被后续两次启动轮换，无法查看“场景已经加载”或进入房间回调中的
  原生异常。另一个代码上可达的同结果路径是 `Room.OnEnterRoom` 在赋值前的
  scene-handle、天气、相机或 agent 操作抛错。因此结论等级是：异步 room-ready
  缺口与 DebugConsole 误放行已证实；“旧 scene 尚未卸载导致 callback 丢失”是
  当前最强、高置信触发解释，但缺少当时 Unity 行日志作最后区分。
- 已排除“原始闪退把空 room 内存直接带进下一进程”：进程退出后该对象状态不可能
  跨进程保留，而且新进程在坏保存前已经两次读取原有可用 current。原始闪退仍可与
  玩家后续快速返回/重载操作相关，但不是 `15:14:05` 坏字节的直接写入者。
- 产品放大条件已证实：DebugConsole 的 `GetSaveState` 只检查 API、archive、
  `SaveGame(int)` 和非负 slot；它虽然采集 `CurrentLocation`，却不把
  `Native.CurrentRoom`、RoomId 或 SceneRawName 纳入 `CanSave`。原生 SaveGame
  自身同样不做摘要语义校验，所以最终把非法状态当成成功提交。

恢复选择：

- `.prev0` 是坏保存前最近一次 native commit，时间与旧 DTMAPI 日志中
  `14:56:58` 的 `SaveSaving -> SaveSaved` 对齐，且摘要与其余四份备份一致有效；
  选择它而不是更老备份可把玩家进度回退限制到约 17 分钟。
- 玩家恢复阻塞项已经关闭。恢复只准在游戏与 Steam 完全退出后，把已验证
  `.prev0` 的密文字节原子替换为 current；不得删除、滚动或覆盖原有
  `.prev0...prev4`，也不得把恢复脚本测试指向玩家实时 Steam AutoCloud 目录。
- 原始闪退类型仍因缺少当时 Unity log/dump 保持未闭合；该缺口不阻碍恢复已证实的
  坏摘要。

## 2026-08-08 玩家恢复后首次加载反馈

原始反馈：

- “显示OK了，但是就按退出键才出来，不然就是纯黑屏”。
- 图片来源：
  `D:\文档\Tencent Files\3033654263\nt_qq\nt_data\Pic\2026-08\Ori\dceb9ed3a0452fea1bd1bafba240cc08.jpg`。

图片转写：

- 游戏已进入 `v1.00.02` Gameplay 画面；左上角色头像与状态条、右上时间/日期/货币、
  底部快捷栏和物品数量均已渲染。
- 世界视口为均匀的深灰/黑色，没有角色、地形、房间物件或背景，符合“全局 HUD
  已加载但 room scene/world renderer 未建立”的可见状态。
- 用户称未按“退出键”前是纯黑；按键后才出现当前画面。该按键究竟是 `Escape`、
  手柄返回键还是退出菜单操作尚未由日志确认，不能把它先写成确定 hook。

审查记录：

- 用户确认事实：恢复脚本已经显示 `[OK]`；恢复后官方存档列表至少能选中并加载
  slot `0`，原先“列表入口立即抛空 key”的阻塞已解除。
- 截图事实：archive、时间、玩家状态、背包/快捷栏等数据已加载到 HUD，但 native
  room/scene 内容没有出现。恢复不是“脚本未替换”或“仍卡在坏摘要列表”的失败。
- 新的高置信判断：恢复到坏保存之前、摘要完整且此前曾被游戏读取的 `.prev0` 后，
  新进程仍可出现 room world 缺失。这说明 `currentScene: null` 是先前
  DebugConsole 保存造成的下游持久化损坏，却不是黑屏/room-entry 故障的唯一上游
  原因；原生异步场景进入未完成的问题仍可在有效 archive 上重现。
- 该截图进一步支持前述 lifecycle 路径：`LoadGame` 和 Gameplay UI 可以先完成，
  真正的 `GameStateSceneTransition -> callback -> Room.OnEnterRoom -> currentRoom`
  链仍可能没有完成。持续的 fade/scene-transition 状态、
  `_LoadSceneAsync` 已加载分支漏 callback，或 `Room.OnEnterRoom` 赋值前异常都能产生
  “HUD 有、世界无”的结果。
- 仍不能从截图区分上述分支，也不能判定 DTMAPI、官方原生场景生命周期或第三方
  native mod 谁触发了现场；必须取得这一次黑屏会话尚未被轮换的 `Player.log`、
  DTMAPI history 与 BepInEx 日志。
- 数据边界：本次后续诊断必须是 `NoNativeSave`。玩家不得睡觉保存、使用
  DebugConsole 保存或进入任何原生保存入口；应直接退出并先采集日志。若没有发生
  native save，恢复后的 current 应继续保持
  `328718D4705186E271C34D8A737C10A4E99AAE169218293AC522A753360799A6`。
- 最小下一步：退出游戏后不要再次启动，立即运行已有
  `1_collect_save_and_crash_logs.bat`，把新生成的 `DTMAPI-player-support-*.zip`
  发回。再次启动会把本次关键 `Player.log` 轮换成 `Player-prev.log`，更多启动还会
  继续覆盖证据。
- 验收点：冷启动选择 slot `0` 后无需按任何退出/返回键，角色、地形和房间物件在
  合理加载时间内出现；随后无保存退出，采集前先证明 current 与 backup family 的
  hash/mtime 未变化。
- blocker：没有本次黑屏会话的新支持包前，不能安全决定是修
  SceneManager callback、Room.OnEnterRoom 前异常、DTMAPI transition interaction
  还是第三方干扰；不应继续盲改或让玩家反复启动试错。

## 2026-08-08 黑屏支持包与确定性设备冲突根因

新增来源与完整性：

- 玩家在黑屏会话退出后提供
  `D:\下载\DTMAPI-player-support-20260808-223245-408-aee326d1.zip`。
- ZIP 长度 `14087881`，SHA-256
  `4BE74035D4E0E3FC2BF5358C27F61E02759E07F91F8B1FD9017CF5579B43EBAD`；
  25 个条目全部可读取并完成逐项哈希，`CollectionStatus=Complete`、
  `SaveCopyFailures=0`、`Errors=0`。没有 Unity crash 目录或 dump。
- 收集到的 current 长度 `3137187`，SHA-256
  `328718D4705186E271C34D8A737C10A4E99AAE169218293AC522A753360799A6`；
  `.prev0` 与它长度、哈希完全相同。这证明第一版恢复包已经精确生效，且玩家没有在
  黑屏状态再做 native save。

两次冷启动的相同失败：

- `Unity-Player-prev.log` 在约 `22:07:43`、`Unity-Player.log` 在约
  `22:15:21` 都完成 slot `0` 密文读取，并在 `FarmArchiveData.AfterLoadData`
  中复现同一个异常。异常链为
  `AutomateBot.OnRemove -> AutomateBotStation.OnRemove -> Equipment.DecoratedRemove
  -> IEquipmentHost.RemoveEquipment -> IEquipmentHost.__HandleCrowedOutEquipments
  -> Room.AfterLoadData -> Building.AfterLoadData -> FarmArchiveData.AfterLoadData`，
  根异常为 `NullReferenceException`。
- 每次异常前都记录 7 条未找到旧地窖 room
  `farm_地窖.044e7ae6-5cc7-4578-bb11-9df5b50ca802`。这些引用是真实的陈旧数据，
  但日志继续执行到设备拥挤处理后才抛错，不能把它们写成本次中断点。
- 原生 `DataPersistenceManager.LoadGame` 对单个 data block 捕获异常后仍设置
  `IsDataLoaded=true` 并返回 `true`，所以 DTMAPI 仍发布 `SaveLoaded`/`LoadGame Exit`。
  这解释了“HUD、日期、货币、背包可见，但世界全黑且 Escape 仍有响应”：全局数据和
  UI 已进入 Gameplay，负责建立 farm/room 的 data block 却在进入房间前中断。
- 首次黑屏日志随后共有 31 个 NRE：1 个 `AutomateBot.OnRemove`、6 个
  `Room.CalcAgentPositionCell`、24 个 `PlantBasinTree.UpdateNoRender`；第二次共有
  138 个：1、25、112。后两组是 room 未完成建立后的持续症状，不是最初中断点。
- 两次会话都走到原生正常退出尾部，没有 `Fatal error in GC`、OOM 或 crash dump；
  本问题不是此前长期 Mono GC issue 的一次复现。

存档对象级复核：

- 仅在内存中用原生格式解密并解析 current 与 `.prev0...prev4`，未把明文写入磁盘。
  六份 archive 都包含同一个设备重叠，因此继续回滚更老 rolling backup 不能修复黑屏。
- 唯一会进入 `AutomateBotStation` 拥挤移除路径的对象位于
  `farmData.farm.DM_building.buildings[8]`：建筑 `small_barn`、房间 `小型畜棚`，
  room GUID `b007603e-9d8b-4b90-98a6-01f4e4649974`。
- equipment index `23` 是空的 `ChickenNest`（id `19`），anchor `(11,5)`、尺寸
  `2x1`，覆盖 `(11,5)` 与 `(12,5)`；index `24` 是小型
  `AutomateBotStation`（id `8`），anchor `(12,5)`、尺寸 `3x2`，内含 1 个 bot。
  两者精确重叠于 `(12,5)`。
- 原生反序列化构造出的 bot 尚未设置 `decisionMaker`。设备宿主先按序列化顺序注册
  ChickenNest，再把后注册的 station 判为 crowded out 并调用移除；只有完成拥挤处理后
  才会调用 `AfterLoadEquipment(station)` 初始化 bot。于是 station 的
  `OnRemove` 提前调用 `AutomateBot.OnRemove -> decisionMaker.OnUnload()`，对必然为空的
  `decisionMaker` 解引用并中断整个 `FarmArchiveData.AfterLoadData`。
- current build 与已保留 builds `24256979`、`24456188`、`24567135`、`24585411`
  对这两个设备的尺寸配置一致，排除“游戏更新改变 footprint 后才重叠”。现有证据不能
  确定当初是哪次玩家放置/移动或哪条官方逻辑允许了重叠；这是上游创建条件，不能在没有
  当时日志的情况下归责 DTMAPI 或某个第三方 Mod。

对先前假设的修正：

- 前一节基于截图提出的 scene callback race 不再是本次黑屏的主因。新日志给出了更早、
  可重复且与 archive 字节一一对应的异常；`farm.__AfterLoadData()` 在执行 room entry
  之前已经抛出。
- 同一重叠存在于 `14:56:58` 的 `.prev0` 及更老备份，因此它也为
  `15:13` 第二次加载后 `currentRoom == null` 提供了更强的直接解释：farm data block
  在设备恢复时中断，DebugConsole 随后把“LoadGame 返回 true”误当成可保存状态，才把
  `currentScene:null` 与缺失 `currentRoomId` 提交到 current。此前的异步
  unload/load callback 假设保留为代码上可达的独立风险，但已被本案确定性证据降级，
  不能再作为本案首要根因。

最小恢复设计：

- 保留 station、bot 和所有其他 archive 数据，只把空 ChickenNest 向左移动一格：
  anchor `x:11 -> 10`，序列化 position `x:33.0 -> 31.5`，`y` 不变。修复后 nest
  覆盖 `(10,5)-(11,5)`，station 仍覆盖 `(12,5)-(14,6)`，离线按当前 build footprint
  重放得到 `invalid_count=0`、`invalid_bot_station_count=0`。
- 修复后的 archive 仍为原生 `DOLOC-TOWN:` 加密格式，长度仍为 `3137187`，SHA-256
  `CF27A15E26620A7A4AC4ED4389AC2BAAB13CE90D123A80325A25B6D3FE53C980`。
  回读解密后 JSON 可解析，明文长度仍为 `2330611`；全文件只有 3 个 UTF-8 字节变化，
  分别对应上述两个数值，没有更改 nest 类型/id/items、station anchor、bot 或其他字段。
- 玩家一键包、脚本矩阵与离线 exact-byte 验证由 Update
  [20260808-0003](../../../updates/2026/20260808-0003-player-slot0-verified-recovery.md)
  继续负责。本 Review 仍不把“离线可加载结构”冒充玩家游戏验收；必须等玩家冷启动后
  确认无需 Escape 即能看到角色、地形和物件，才可关闭现场结果。

关于最初闪退与重叠创建时点：

- 五代 rolling backup 的源文件时间依次为北京时间 `13:46:58`、`14:06:16`、
  `14:23:36`、`14:42:30`、`14:56:58`，且六份解析结果都有同一处重叠；玩家描述的
  最初闪退约在 `15:09`。所以重叠最迟在闪退前约 82 分钟已经存在，并连续跨过至少
  五次 native save，反驳“重叠恰在 `15:09` 闪退瞬间产生”的首要解释。
- 本案的确定性 NRE 还依赖反序列化时序：bot 已从 JSON 构造、但尚未执行
  `AfterLoadEquipment`，拥挤处理就先调用了 `OnRemove`。在设备刚于运行中放置且对象已
  初始化的现场，并不具备这个精确前置条件。因此现有栈更像 cold-load-only 缺陷，不能
  直接证明放置重叠本身造成进程闪退。
- 当前最符合证据的事件链是：官方放置/占格逻辑更早允许并保存了潜伏重叠；`15:09`
  的原始闪退原因未知，但它迫使下一进程进行 cold load；cold load 才命中官方设备恢复
  顺序缺陷并留下空 `currentRoom`；DebugConsole 随后误放行保存，把可定位的加载失败
  放大成 `currentScene:null` 的列表阻断。第一版恢复再次冷加载同一潜伏重叠，所以只修好
  列表、未修好世界。
- 因为原始闪退对应的 Unity log/dump 已轮换，仍不能排除它与该重叠的其他下游逻辑有关，
  也不能排除 GC/native/第三方等独立原因；只能明确否定“现有
  `AutomateBot.OnRemove` NRE 已经证明最初硬闪退”的说法。黑屏会话本身则有正常退出尾部，
  与原始未分类闪退必须分开记账。
