# 正式版新建存档与七项功能异常责任边界审查

- Status: recorded — static responsibility analysis complete; Issue 5 player-save root cause
  confirmed; one concrete Issue 7 player instance confirmed as an incompatible external DolocPlus
  1.3.1 assembly; Issue 2 now has a complete player support bundle and six consecutive archives:
  its trigger-ready `wood 999 + 6` state is confirmed, but the saved transitions do not demonstrate
  an unexplained loss. Exact Runtime、第一方产品和可取得的已启用第三方代码均未找到可达的
  大型木箱木头删除路径；Issues 1、3、4、6 still await equivalent player evidence
- Time: 2026-08-08
- Source: 用户提供的七项玩家反馈截图
  C:\Users\ADMINI~1\AppData\Local\Temp\codex-clipboard-ab0558fa-58f0-46bc-abb8-f2a3fa640cb2.png
- Issue 5 source: `D:\下载\DolocTownSave绘空事.zip`，SHA-256
  `9026222023B609FE0115068FDE91C6AED084AE0D728856387AF135007A90E551`
- Issue 7 player-support source:
  `DTMAPI-player-support-20260809-104106-614-749d3b84.zip`，17,245 bytes，SHA-256
  `8393D919381574622FA8630EA16DFDAABFFDA352CBD4D99BAA1B6C5D1F0F5C14`
- Issue 2 player-evidence source: `D:\下载\DTMAPI-logs_ the\doloc-save-1.data`，
  1,752,075 bytes，SHA-256
  `264C6E3BF4BB4C59EE72253F6308C07D6BB0D2E53ACA1034828CE08EE5936CE6`；
  同目录 `latest.log`，199,845 bytes，SHA-256
  `2792A29B6ED06F52F8DD7750E2BD17C157D51412F57FABA10C49BFF7CE11B6DA`
- Scope: 按截图原顺序，对 current public build 24585411 的官方原生路径、
  DTMAPI Runtime 和第一方产品 Hook 做只读根因与责任边界审查
- User constraints: 先提交此前未提交工作，再开始排查；用户明确说明截图第 7 条是
  正式版“新建存档”问题，与此前恢复旧存档后的鸡窝/机器人站重叠黑屏不是同一问题；
  本轮已授权并行子智能体追查，但尚未授权实现修复或写玩家存档
- Related records:
  - 20260808-0001-save-list-null-scene-after-debug-save.md
  - ../../code/2026/20260806-0003-current-test-24585411-compatibility-audit.md
  - 20260806-0001-moreequipment-official-slot-growth-review.md
  - ../../code/2026/20260809-0001-subscription-third-party-dll-regression-audit.md
  - ../../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md
  - ../../../debug/issues/ISSUE-021-20260805-moreequipment-cold-recovery-transaction.md
- Not inspected yet: Issue 5 已取得并解析玩家 archive、随包 Player.log 并完成隔离加载；
  Issue 7 已取得一个玩家的故障 `Player-prev.log`、同会话 BepInEx `LogOutput.log` 和删除
  BepInEx 后的对照 `Player.log`。Issue 2 已取得完整回传包、当前 Unity/BepInEx/DTMAPI
  日志、current + `.prev0`–`.prev4` archive，并已完成 Runtime、第一方和可取得第三方包的
  静态代码反查；但回传发生在正式版更新后的新会话，仍缺 8 月 9 日声称丢失当刻的
  Unity/BepInEx 首因日志、精确输入方式和可核对的操作前后画面，另有 5 个 native 内容包
  未取得精确包体。
  Issues 1、3、4、6 仍缺玩家日志、精确启用 Mod 清单、受影响存档和操作录像。一例
  Issue 7 已闭合也不自动证明其余玩家的同名症状同根
- Evidence boundary: 截图是“玩家反馈较多”的汇总，没有证明七条来自同一玩家、同一
  存档或同一会话。症状可按代码结构分组，但不能据此宣称七条共享同一个首因。

## 共同基线

- 当前本机 Steam appmanifest 已实际挂载 public，build ID 为 24585411，且没有
  pending branch switch。
- 当前 public 的 Assembly-CSharp.dll、Addressables catalog、configs bundle、
  GU bundle、level87、sharedassets87 及其 resS 都与冻结的
  24585411_test_68AEA1 捕获逐文件 SHA-256 相同。此前只属于 test 的条件门因此在
  本轮只读检查中获得了关键 public payload 等价证据。
- 24567135 到 24585411 的 390 个 GenDatas 导出文件中，195 个实际 JSON 内容
  全部逐字不变；变化的是对应的 195 个 AssetRipper meta 文件。对话、邮件、任务、
  物品、背包、房间和全局参数表没有配置内容增量。
- 库存组直接涉及的 ExchangeInventory、QuickInventoryPanel、ContainerPanel、
  BackpackBottomPanel、BoxInventoryWidget、StorageShelfPanel 六个官方 UI prefab，
  在归一化 AssetRipper 导出 GUID 后也逐字相同。因而目前没有“24585411 改坏这些
  prefab 字段”的证据。
- 本轮逐文件复核的 Npc/Dialogue、LinearInventory/Container/DropItem/
  StorageShelf/InventoryPanel、DolocAPI NewGame、DataPersistenceManager 和
  GameDataUiState 反编译文件在 24567135 与 24585411 间均完全相同。已知唯一增量
  主方法仍是 DolocPagedLinearUI.RefreshView；它不是这些库存或新建存档直接 owner。
- raw level87 确有官方内容变化，且该场景仍映射旧城市废墟。它与第 5 条有调查相关性，
  但不能仅凭场景文件变化推导“信件触发已坏”。

## Issue Review

### Issue 1：与角色对话不出现对话选项

Original feedback:

- “正式版更新后，和角色对话没有出现对话选项”。

Screenshot transcription:

- 截图只给出上述行为描述；没有 NPC 名称、所在场景、日期、任务阶段、对话画面或日志。

Review record:

- User-confirmed facts: 玩家把异常时间点描述为正式版更新后；目前未确认是所有 NPC、
  单个 NPC，还是只缺候选选项而仍显示普通台词。
- Code/doc facts inspected: 官方直接路径是
  NpcRenderer.OnInteract -> Npc.OnInteract ->
  DialogueState.DialogueWithNpc。候选项由
  DialogueState.HandleCandidateDialogueNodes 调用
  DialogueManager.GetLocalizedCandidateLines，再交给 DialoguePlayer/
  DialogueOptionPanel。仓库 Runtime 与第一方产品没有 patch
  NpcRenderer、Npc、DialogueState、DialogueManager、DialogueRunner 或
  DialogueOptionPanel。
- Code/doc facts inspected: ActionSpeed 的 AgentStateInteract Hook 只对识别出的
  工具、植物、动物、机器、燃料/饲料等交互调整时长，并不拥有 NPC 的
  NpcRenderer.OnInteract 路径；其回调也有异常捕获。
- Code/doc facts inspected: 两个 build 的 133 个 Yarn、9 语言本地化、
  compiledYarnProgram 与 DialogueOptionPanel 资源语义一致；因此当前没有候选文本或
  选项面板资源被正式更新直接改坏的证据。
- Codex inference: 直接 owner 当前仍是官方对话状态/候选数据。若进入对话但候选数组
  为空，官方会跳过选项渲染；若更早的加载或 UI 回调异常中断，候选预载也可能没有完成。
  在拿到玩家栈前，这两类不能区分。
- Cross-issue boundary: Issue 1 与 Issue 7 可以共享 DialoguePlayer/DialogueOptionPanel
  “内容存在但 presentation 未显示”的边界；但 NPC candidate 与新档 opening Yarn 的
  数据入口不同。现有证据支持调查同一 UI 层，不支持宣称同一实际根因。
- Rejected/unproven hypotheses: 不能把“正式版更新后”直接写成 1.00.02 对话代码或
  GenDatas 回归；上述 owner 文件和实际 JSON 与上一 build 完全相同。也不能仅凭
  没有直接 Hook 就绝对排除某个 Mod 更早破坏共享状态。
- Ownership: 官方原生为直接 owner；DTMAPI/具体产品目前没有直接命中证据。
- Acceptance checks: 记录精确 NPC、存档阶段和操作；有 Mod 与纯官方隔离副本各冷启动
  一次，对比候选行数量与异常栈；不得用另一个 NPC 正常说话代替原问题验收。
- Blocker conditions: 没有受影响 NPC/存档和当次 Player.log，无法确定是合法空候选、
  对话状态卡住、内容状态缺失还是间接异常。

### Issue 2：箱内物品点击后消失且未进入背包

Original feedback:

- “箱子里的东西点击后消失，未出现在背包”。

Screenshot transcription:

- 没有物品类型、箱子类型、点击方式、背包容量、操作前后数量或日志。

Review record:

- Code/doc facts inspected: 官方 ContainerBaseUiState.PlaceToOtherSide 在从箱子取出时，
  先执行 containerInventory.Take(currentIndex)，再执行
  backpackInventory.PlaceItem。两步之间没有事务或回滚。
- Code/doc facts inspected: LinearInventory.Take 会先把源槽设为 null，再同步调用全部
  InventoryReceiver；任一 UI receiver 抛异常都会阻止后续 PlaceItem。该原生实现没有
  receiver 级异常隔离。
- Codex inference: “箱内立刻空、背包没有”的症状与 source Take 已提交、receiver
  抛错后 destination Place 未执行高度吻合。这是当前最强共同机制，但尚未知道具体
  receiver、物品或 Mod。
- Code/doc facts inspected: 玩家说“点击”时还必须区分鼠标路径。鼠标点击默认经
  ContainerBaseUiState.HandleSwapAllItems -> SwapItemFromOutside，把物品从箱格转入
  InventorySystem.buffer；SingleInventory.CurrentItem 先写 buffer，再同步调用
  ExchangeInventory.Render。若 cursor renderer 抛错，物品可能仍在 buffer，只是没有
  跟随鼠标显示，也尚未进入背包。这属于“不可见中间缓冲”而非已经证明的数据删除。
- Code/doc facts inspected: 当前最能同时覆盖多个症状的 receiver 候选是官方
  InventoryPanel.Render -> ItemNavSlot.Render/Clear，鼠标 buffer 路径则是
  ExchangeInventory.Render。两者都会读取 item sprite、耐久度和下标贴图，且原生
  receiver 分发没有逐项 try/catch；具体抛错成员仍必须由玩家栈确认。
- Ownership: 箱子转移与 LinearInventory transaction 是官方直接 owner。当前第一方
  产品没有 patch ContainerBaseUiState、LinearInventory 或 InventoryPanel。
- Rejected/unproven hypotheses: ChestLocatorEnhancer 只 postfix
  ArchiveDataHandle.GetAvailableInventories，用于扩展材料查询范围；它不接管直接箱子
  UI 的 Take/Place 路径。没有栈前不能据此完全排除其在其他操作中的间接异常。
- Acceptance checks: 异常后先不要保存，按 ESC 关闭容器，观察原生 Hide 是否把 buffer
  退回背包、原箱或地面；再重开容器并对比鼠标/手柄路径。在一次可丢弃副本中记录
  source、buffer、destination 槽对象和 receiver 清单；成功路径要求总量与唯一归属守恒。
- Blocker conditions: 必须取得第一次消失当刻的完整异常栈；后续重新启动日志可能已把
  第一现场轮换。

### Issue 3：世界物品可重复拾取，数量变化但地图贴图不消失

Original feedback:

- “拾取物品，物品栏有变化，但是地图未消失，物品栏数量也一直在变化，可以重复拾取”。

Screenshot transcription:

- 这里的“地图未消失”按玩家语义转写为世界场景中的物品贴图/实体没有被移除，而不是
  地图 UI 本身没有关闭。

Review record:

- Code/doc facts inspected: 官方 DropItem.OnTouch 先调用 DolocAPI.PlaceItem；只有该
  调用正常返回 null 表示全部放入后，才执行 Host.RemoveDropItem(this)。
- Code/doc facts inspected: InventorySystem.PlaceItem 最终由 LinearInventory.PlaceItem
  先写入背包，再同步 emit receiver。若 receiver 在写入后抛异常，DropItem.OnTouch
  不会继续到 RemoveDropItem，下一次碰触会再次写入。
- Code/doc facts inspected: 即使 inventory receiver 全部成功，原生仍在
  Host.RemoveDropItem 之前执行物品标题、获得提示与 achievement/broadcast 路径；
  其中任一步抛错也会形成“背包已增加、世界物仍在”的同一窗口。日志可以区分这两支。
- Codex inference: 该反馈几乎逐步复现了上述“目标库存已写入、世界源未删除”的异常
  中断窗口，是第 2/4/6 条共同半事务假设中证据形状最强的一条。
- Ownership: DropItem、InventorySystem 与 QuickInventory receiver 是官方直接 owner；
  当前第一方产品没有 patch DropItem.OnTouch、InventorySystem.PlaceItem 或
  LinearInventory.PlaceItem。
- Rejected/unproven hypotheses: 这不是单纯贴图刷新慢，因为玩家称数量可持续增加；
  也不能只凭数量增加认定作弊/创意模式，必须看调用栈是否在 receiver 抛错。
- Acceptance checks: 一次触碰只增加精确数量，世界实体在同一成功路径移除；背包满时
  不增加数量、世界实体保留且只提示一次。
- Blocker conditions: 缺少第一次触碰异常栈、物品 ID 和当时启用 Mod 清单。若物品是
  第一方 Oil 的 crude_oil，需继续审查该内容输入；若是普通官方物品，不能把 Oil 作为
  本条直接 owner。

### Issue 4：从箱子取出工具后工具凭空消失

Original feedback:

- “从箱子里拿工具出来，工具凭空消失”。

Screenshot transcription:

- 没有工具名称、箱子类型、鼠标/手柄操作方式或背包空位信息。

Review record:

- Code/doc facts inspected: 与 Issue 2 走同一个 ContainerBaseUiState/
  LinearInventory 先 Take 后 Place 路径；工具只是不能堆叠、可能触发 QuickSelect/
  Item title/render 的特定 Item 子类。
- Code/doc facts inspected: 若玩家用鼠标点击，本条也可能先进入 InventorySystem.buffer；
  ExchangeInventory.Render 失败会让工具“跟随鼠标图标”不可见，但对象仍可能留在
  buffer。只有检查关闭 UI 后的退回路径与存档对象，才能称为真实删除。
- Codex inference: 当前优先按 Issue 2 的半事务机制调查，而不是另造“工具删除”原因。
  若只有工具触发，则抛错者很可能读取工具特有的 sprite、title、QuickSelect 或状态字段。
- Ownership: 官方箱子/库存 UI 是直接 owner；OneActionComplete 和 ActionSpeed 的
  工具行为 Hook 不参与箱子转移，且其回调异常被产品自身捕获。
- Rejected/unproven hypotheses: 不能用普通可堆叠物品转移正常就关闭本条；工具子类路径
  必须单独复现。
- Acceptance checks: 至少用反馈中的同一工具和一个普通物品对照；关闭 UI、返回标题、
  无保存退出后都要证明工具仍在唯一目标槽，不能重复或丢失。
- Blocker conditions: 缺少工具 ID 与异常栈。

### Issue 5：地形恢复完成后未收到前往旧城市废墟的信

Original feedback:

- “地形改造已经完成了，但是没有去旧城市废墟的信”。

Screenshot transcription:

- 红箭头强调这一条。当前不清楚玩家说的是哪一项“地形改造/恢复”任务、是否已跨日、
  是否读过前置信件或是否已有对应任务链。

Review record:

- Code/doc facts inspected: 玩家描述混合了两条不同官方路径。正式任务链在完成
  wetland_main_6（COMPLETE_DIALOGUE: terraforming_terrawetland）后启动隐式节点
  wetland_main@1；必须再经过一次 DAY_PASSED，才并行发送 id=wetland_main、标题
  “环境改造器的进展”的邮件，并把 ruinedcity_main_entsk 候选对话加给澳柯玛。邮件
  只要求去找澳柯玛；完成该对话后，Yarn 才 start_mission_chain ruinedcity_main。
- Code/doc facts inspected: id=ruinedcity_continue、标题“关于旧城市废墟”、附件自动接受
  ruinedcity_main 的邮件属于旧档版本迁移：档案版本小于 0.99.00 且
  visitedArgs[ruinedcity_main_entsk] > 0 时由 version_patch0900 发送；它不是正式新流程
  在环境改造完成后必发的信。
- Code/doc facts inspected: Email/Mission/DialogueTask_SendEmail 相关原生代码和全部实际
  GenDatas JSON 在 24567135 与 24585411 间不变。仓库 Runtime 与第一方产品没有
  patch SendEmail、EmailManager、MissionManager、任务图或 level87。
- Code/doc facts inspected: 本次官方 raw 增量中 level87 确实改变；该文件是旧城市废墟
  scene，但“发信”发生在进入该 scene 之前，单凭 scene 字节变化不能证明邮件触发失败。
- Codex inference: 若玩家当天刚激活改造器，未立即来信是 DAY_PASSED 前置尚未满足，
  不是异常。若已经跨日，则依次检查 wetland_main_6 是否真正完成、wetland_main@1
  是否完成、wetland_main 邮件是否已存在，以及澳柯玛候选中是否已有
  ruinedcity_main_entsk；不能用 ruinedcity_continue 是否出现作为正式流程判据。
- Code/doc facts inspected: 官方还存在一个真实但尚未命中玩家的中断窗口：
  DialogueState.PlayDialogue 会在 Yarn 结尾 start_mission_chain 之前先 Visit 当前节点并
  移除非 resident 候选。若 ruinedcity_main_entsk 对话开始后崩溃/中断，可能留下
  visited>0、candidate 已移除、ruinedcity_main 尚未启动；普通同版本流程没有自动重试。
- Ownership: 官方任务链、邮件管理器和场景内容为直接 owner；当前无 DTMAPI 直接 Hook。
- Rejected/unproven hypotheses: 不能把 level87 变化自动等同于发信回归；也不能在不知道
  “地形改造”具体节点时假定玩家已满足 ruinedcity_continue 的全部前置。
- Acceptance checks: 从前置任务完成前的可丢弃副本执行一次官方完成路径，记录事件节点、
  DAY_PASSED、wetland_main 邮件入队、澳柯玛候选加入，并在完成该对话后确认
  ruinedcity_main 启动；跨日要求必须按原生设计单独确认。
- Blocker conditions: 缺少任务名称/ID、是否跨日，以及玩家 archive 中
  wetland_main_6、wetland_main@1、email id=wetland_main、澳柯玛候选
  ruinedcity_main_entsk、visitedArgs 和 ruinedcity_main handling/completed 状态。
  若 visited>0、candidate 缺失且 ruinedcity_main handle 也缺失，则高度符合上述官方
  对话中断窗口；若已有邮件和候选，则本条进度发放正常，缺选项应回到 Issue 1 调查。

#### 2026-08-09 玩家存档与隔离加载证据

- Evidence identity: 玩家包共 1,706,548 bytes，只含 `doloc-save-0.data` 和
  `Player.log`，没有 `.prev*` 或 `.bak`。存档仅在内存中按官方格式解密解析；原 ZIP 与
  原 archive 均未修改。明文 JSON 的 SHA-256 为
  `8D5FDBAF4BAC4C8E850290B821B23E025010D0831EB6C26B85A562E5A165BA50`。
- Player-log boundary: 随包 `Player.log` 只有启动/返回标题阶段，没有该次任务推进、换日或
  存档加载记录；可见 DTMAPI 0.6.0、EasyFishing、ModSettingManager、NpcMapMarkers、
  Configuration Manager 与 DolocPlus。唯一异常是退出阶段 ModSettingManager 的
  `OnApplicationQuit` NullReferenceException，与任务链 owner 无调用关系。它不能还原
  当年对话或迁移中断的首个异常。
- Archive facts: `baseData.version=1.00.02`，总天数为 254。`wetland_main` 已完成；
  `wetland_main_0` 至 `_6` 和 `wetland_main@1` 全部在 `finishMissions`。正常流程邮件
  `wetland_main` 已于总天数 80（第 1 年第 3 月第 24 日）发出，且未被回收。因而本例
  不是“还差一次 DAY_PASSED”，继续睡半年也不会改变卡点。
- Archive facts: `visitedArgs[ruinedcity_main_entsk]=1`，但澳柯玛的候选节点中已没有
  `ruinedcity_main_entsk`；`ruinedcity_main` 既不在 active handles，也不在 completed。
  这精确命中上述原生中断窗口：对话节点已经 Visit 并移除候选，但结尾的
  `start_mission_chain ruinedcity_main` 没有成功落地。存档不能证明当时究竟是崩溃、
  提前中断还是命令异常，但能证明故障发生在这两个原生提交点之间。
- Migration facts: archive 中没有 `ruinedcity_continue` 邮件，也没有
  `version_patch0900` 的访问/排队结果。作为第二个独立哨兵，`zenis_cup_1` 已完成但同一
  `version_patch0900` 应补发的 `zenis_cup_ex` 也不存在。因此 0.99.00 补救迁移没有在该
  存档上完成；档案现已标记为 1.00.02，`RunVersionAppendCommandScripts` 的严格
  `oldVersion < 0.99.00` 门不会再重试。
- Version-history boundary: 冻结的早期 public 23762374 尚无 0.99.00 组；已捕获的
  24256979 test 及之后版本都包含同一 SHA-256 的 `0.99.00.txt`。现有玩家包没有首次
  跨越 0.99 时的游戏 build 或日志，所以只能确认“迁移未完成且版本门已越过”，不能在
  缺少历史包的情况下断言是哪个具体官方 build、排队窗口或退出时点造成。
- Runtime reproduction: 原状态副本在 AutoCloud 隔离的第 12 槽成功加载，证据为
  `docs/debug/evidence/GAME-SMOKE/20260809-073118`。测试只启用当前 Runtime 与官方
  Workshop MoreSaves；`SaveLoaded slot/index=11 isNewGame=False`，没有
  `version_patch0900`、`ruinedcity_continue`、反序列化异常或 NullReferenceException。
- Migration replay: 第二份隔离副本只把 archive index 调整为 11，并把
  `baseData.version` 从 1.00.02 临时设为 0.98.99，以验证当前官方迁移，而不是修复玩家
  原件。`docs/debug/evidence/GAME-SMOKE/20260809-073241/Unity-Player.log:720-723`
  记录 `Start/Running node version_patch0900`，随后
  `<send_email ruinedcity_continue>` 返回 `Succeed`。这证明当前官方补救脚本会对该精确
  状态补发任务邮件；原档唯一缺少的是再次进入该一次性版本门的机会。
- Save-safety: 两次运行均为 `NoNativeSave`，`RunStatus`、SaveLoaded、fixture isolation、
  archive/committed-sidecar unchanged-before-cleanup、process/fatal/profile restore 和
  fixture cleanup 全部通过；`PlayerArchiveWritebackPerformed=false`。原状态副本的
  SHA-256 在运行前后均为
  `A4FA4D4A4726FEF01BB8CF6DE537F2797FA6AA1113B8824830B68C4808E841F9`；迁移重放副本
  前后均为 `F9EBF941F4CDCA1753F4BB8A476486ED7D20E7954EF6678E5D630D5FDF16AF9C`。本机真实
  Steam AutoCloud 和真实第 12 槽没有被测试读取或写入。
- Root-cause decision: Issue 5 的持久故障由官方对话进度的非原子提交窗口，加上只执行
  一次的官方 0.99 迁移补救没有落地共同形成。DTMAPI/第一方没有直接 Hook 这些 owner，
  隔离加载也没有出现 Runtime 异常；当前证据不支持把这个存档状态归因于 DTMAPI。
  历史中断的外部触发者仍不可从现存 archive 反推。
- Repair boundary: 最小恢复应只通过官方 native owner 一次性发送
  `ruinedcity_continue`，让其原生附件在首次阅读时接受 `ruinedcity_main`。不得把玩家
  原档版本全局降到 0.98.99：实测会同时重放 0900–0911 多组迁移，可能重复其他副作用；
  也不应盲目重新加入已经 visited 的候选对话节点。修复实现与玩家写档仍需单独授权和
  Update/回滚验证；截至该根因审查节点尚未执行。

#### 2026-08-09 定点修复结果

- 用户随后只授权生成修复后的存档文件。实现与验证由 Update
  [20260809-0001](../../../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md)
  持有；没有修改 Runtime、产品或玩家本机 AutoCloud。
- 派生 archive 只在邮件列表首部增加一个原生 `ruinedcity_continue` 对象，共 337 bytes；
  版本、时间、archive index、任务/对话状态和其余明文字节不变。邮件是 unread，唯一附件
  为 `ruinedcity_main`、`autoAccept=true`、`isAccept=false`，与官方迁移发送后的首次阅读
  语义一致。
- 输出 `D:\下载\DolocTownSave绘空事-旧城区任务修复.zip` 仅含修复后的
  `doloc-save-0.data`，ZIP SHA-256 为
  `3EFFE108F819C6CFBB2E7911A3D4294305E947A8110F611A567E387EB946B7F2`。
  AutoCloud 隔离第 12 槽加载 `GAME-SMOKE/20260809-074943` PASS，零反序列化/异常匹配，
  NoNativeSave archive/sidecar unchanged、process/fatal/profile/cleanup 全部通过且无 writeback。
- Remaining manual gate: 玩家替换后首次阅读“关于旧城市废墟”，确认任务自动接受并能正常
  与奥兰多推进；完成前不要把本修复扩大解释为其他六项反馈已修复。
- Highest-value historical evidence: 若要继续判断最初为何漏迁移，优先寻找同一玩家
  `baseData.version < 0.99.00`、尚未首次进入 0.99+ 的完整 current/prev/bak 存档家族，以及
  第一次用 0.99+ 打开并退出后立即保留的对应家族。只有升级后的 1.00.02 单档无法重建
  一次性版本门当时发生了什么。
- Highest-value trigger evidence: 若有人还能稳定复现正式流程卡住，保留两份配对存档：
  ① 已收到 `wetland_main`、尚未与澳柯玛进行 `ruinedcity_main_entsk` 对话；② 对话/闪退
  后立即退出、没有再睡觉或保存的故障档。每份都要带受影响 current 及 `.prev0...prevN`/
  `.bak`，并记录槽位、游戏 build、语言和精确启用 Mod/插件列表。
- Highest-value logs: 故障后不要再次启动游戏，立即收完整 `Player.log`、已有的
  `Player-prev.log`、BepInEx `LogOutput.log`、DTMAPI latest/history/diagnostics 和本次新产生
  的 crash dump；不要只截 tail。若只能找到一组证据，“最后一个 `<0.99` 存档 + 第一次
  升级运行的完整 Player/BepInEx 日志”价值最高。

### Issue 6：储物架交互后手持纸箱及内部物品一起消失

Original feedback:

- “储物架纸箱放满后，手上拿着一个纸箱，此时点击交互键，手上的纸箱和里面的物品都消失了”。

Screenshot transcription:

- “放满”目前有两种可能语义：储物架的纸箱槽全部占满，或架上纸箱的内部库存装满；
  两者会走不同分支，复现时必须按玩家原意确认。

Review record:

- Code/doc facts inspected: 官方 StorageShelf.OnInteract 在选中 ItemBox、该箱允许上架且
  shelf.isFull 为 false 时，先调用 InventorySystem.Take(SelectedItemIndex)，再调用
  PlaceBox。纸箱及其内部 inventory 是同一个对象；若 Take 的 backpack receiver 抛错，
  PlaceBox 不会执行，整个嵌套对象会一起失去可达槽位。
- Code/doc facts inspected: 若 shelf.isFull 确实为 true，原生不会执行上述 Take，而是进入
  StorageShelfUiState；因此玩家所说“满”的精确定义决定首个调查分支。
- Codex inference: 如果“货架满”精确表示 6/6 槽都占用，且操作确实发生在世界交互，
  原生代码不会 Take 手持纸箱，当前描述与该分支矛盾。此时优先怀疑视觉 isFull 状态已
  过期，或玩家实际已在货架 UI 内、buffer 持箱后再点格子；必须用视频/按键序列判别。
- Codex inference: 若是“箱内物品满、架上仍有空槽”，本条与共同半事务机制高度吻合；
  若是“架上槽位也全满”，则必须追 StorageShelfUiState 打开首帧的残留 Confirm 输入和
  buffer/shelf 交换路径。
- Ownership: StorageShelf/StorageShelfUiState/InventorySystem 是官方直接 owner。
  ChestLocatorEnhancer 虽会只读遍历 shared shelf 内的 ItemBox inventory，但它不接管
  StorageShelf.OnInteract 或 UI 交换方法。
- Acceptance checks: 分别覆盖架槽未满但箱内满、架槽全满两种矩阵；每次交互后纸箱对象、
  内部物品总数与唯一归属都必须守恒。
- Blocker conditions: 缺少“放满”语义、SelectedItemIndex、首个异常栈和操作设备。

### Issue 7：正式版新建存档后黑屏

Original feedback:

- “正式版创建新存档后黑屏。只能按ESC和右键，左上角有快捷图标”。

Screenshot transcription:

- 用户已明确：这是新建存档问题，不是此前恢复旧存档后进入世界黑屏；不得引用鸡窝与
  机器人站重叠作为本条根因。
- 当前没有该黑屏画面、所建槽位、是否跳过教学、按 ESC/右键后的实际反应或日志。

Review record:

- Code/doc facts inspected: 官方路径为 GameDataUiState.Load(new) ->
  DolocAPI.NewGame -> DataPersistenceManager.NewGame ->
  gameInitConfig.InitNewGame -> AfterLoadArchiveData -> StartGame。
  非跳过流程随后进入教学 dungeon；跳过或结束教学后会启动 opening dialogue，并经
  GameStateManager.TransitScene 的 callback 调用 Room.OnEnterRoom。
- Code/doc facts inspected: GameDataUiState 只在 DolocAPI.NewGame 正常返回后才隐藏
  LoadingPanel。上述任一点抛异常都可能留下黑屏/Loading 状态，而部分常驻 UI 或输入
  仍可响应。
- Code/doc facts inspected: 当前初始化配置走 skipTraining=true、skipInitAnim=false、
  skipStartDialogue=false，因此正常新档会在 FinishTrainingDungeonCallback 先启动
  openingAnimationNode=aside_anim，再 TransitScene。黑幕而 ESC/右键仍响应，首先落在
  官方 opening dialogue/cinema 与 scene transition 状态机，不是旧档设备卸载路径。
- Code/doc facts inspected: aside_anim 会先进入 PPM cinema、隐藏基本提示/快捷栏，再执行
  wait、旁白、姓名输入和选项；DialogueState.OnUpdate 本身处理 ESC、右键和确认键。
  因此“画面黑但 ESC/右键仍能推进”比“主线程或 LoadingPanel 完全死住”更符合 opening
  DialogueState 仍活着、但 line/option view 没有呈现。仍需视频确认按键是否真的推进节点。
- Code/doc facts inspected: 官方 AfterLoadArchiveData 在中途同步 Invoke
  OnAfterLoadArchiveData；DTMAPI 在这里派发 SaveLoaded。此时官方尚未 ResetBasicTip、
  Clear/BindQuickInventory、解除 global pause、预载 NPC 初始对话、新档命令/帽子/
  礼物对话/成就，也尚未进入 StartGame。事件处理异常有 feature/handler 隔离，但一个
  回调若成功改动半初始化原生对象，隔离机制不会自动回滚。
- Confirmed DTMAPI-owned defect: Bootstrap 的 titleSettingsUi.ResetForSaveBoundary 会
  SetActive(titleButtonRoot, true)，却不在该边界同步 SetActive(root, false)。按钮的固定
  位置正是左上角 (48,-38)，大小约 54x54。若 NewGame 后续在 InputContext 改变或下一
  DTMAPI Update 前中断，标题按钮会残留在黑屏上。该事实很可能解释玩家所说的左上快捷
  图标，责任属于 DTMAPI 标题 UI 生命周期。
- Scope limit: 这个对象只是一个小按钮；panelRoot 已被关闭，root 本身没有全屏黑色
  Image。它不能单独解释全屏黑屏，更可能是“原生流程已在别处中断”的可见伴随症状，
  不能把按钮泄漏与黑屏主因合并成一个已证实 DTMAPI 根因。
- Code/doc facts inspected: MoreSaves 当前没有 Harmony patch，只设置官方
  GameManager.archiveFileCount 并做旧文件名迁移；官方 GameDataUiState、NewGame 和
  scene/dialogue 仍是直接 owner。MoreEquipment 只 patch accessory manager/bar；
  其他第一方产品也没有直接 patch NewGame 或 opening dialogue。
- Codex inference: 该症状优先调查 NewGame/AfterLoadArchiveData/StartGame 的首个异常，
  以及它是否与 Issue 1 的 opening dialogue/候选 UI 状态相交；不能从 ESC/右键仍响应
  推导游戏主线程完全卡死。
- Preserved counter-evidence: 2026-08-06 的 AutoCloud 隔离真实矩阵
  GAME-SMOKE/20260806-062425 使用 566467f08193 Runtime 与 MoreSaves，经官方 UI 新建
  第 7 槽，完整经过 SaveLoaded isNewGame=True、结束教学、aside_anim 和
  DialogueState；RunStatus/SaveLoaded/MoreSaves lifecycle/
  process/fatal checks 全部 PASS。当前 HEAD 相比该 Runtime 的生产 lifecycle 差异只有
  PlayerDoctor 缺失时跳过启动诊断，不涉及 NewGame、SaveLoaded、标题 UI 或 GameBridge。
  但 QA 主动调用原生 QuitDialogue 退出首次对话，没有验收玩家正常观看时的 line/options
  UI；该运行也仍是 24567135 test build。故它只能反驳“Runtime/MoreSaves 必然让所有
  新档在 SaveLoaded/scene 前黑屏”，不能替代 24585411 public 与玩家完整 Mod 集合的
  对话呈现复现。
- Rejected/unproven hypotheses: 不能沿用旧存档 cold-load 设备 NRE；新建 ArchiveDataHandle
  不含那个玩家设备对象。也不能仅凭 MoreSaves 改槽位数就判定它导致 NewGame 黑屏。
  标题按钮 reset 与 SaveLoaded PlayerLoop refresh 在当前正式 game build 和本轮安装器
  三提交之前就已存在，也不能称为安装器重做新引入的回归。
- Ownership before player evidence: 官方 NewGame、LoadingPanel、教学/开场对话和 scene
  transition 是视觉直接 owner；DTMAPI 已确认拥有左上标题按钮泄漏。具体玩家实例仍应以
  同次首个异常栈归责，不能从相似截图外推。
- Acceptance checks: 使用与玩家 Steam AutoCloud 隔离的 disposable fixture，在相同
  official public 与相同 Mod 集合新建空槽；记录 NewGame 每个边界、opening dialogue、
  Room.OnEnterRoom 和 LoadingPanel 隐藏。之后需用禁用 Mod 对照，而不是改写玩家存档。
- Blocker conditions for other reports: 没有各自故障会话的 Player.log、BepInEx log、
  DTMAPI history 和精确启用 Mod 列表前，不能把下面这一个已闭合实例外推为所有新档黑屏
  的共同根因。

#### 2026-08-09 一个玩家故障会话与删除 BepInEx 后对照

- Collection integrity: 支持包状态为 `Complete`，10 个 ZIP entry 全部可读，报告零收集
  error。它保留了 10:24 左右的故障 `Player-prev.log` 与同会话 BepInEx
  `LogOutput.log`，以及 10:40 左右删除 BepInEx 后的新 `Player.log`。包内没有
  `doloc-save-*.data`，只有 `flags.json`、空 `mod_infos.json` 和 AutoCloud 元数据；因此
  本例没有可检查的新档 archive。这个缺失与“第一次存档前卡住”一致，但玩家后来已经做过
  删除/重试，不能仅凭缺文件证明 archive 从未生成。
- Exact loaded set: 故障会话由 BepInEx 5.4.23.3 明确报告 `2 plugins to load`，仅为
  Configuration Manager 18.4.1 和外部《多洛可小镇》增强功能 Mod 内置版 by Qiuzy
  1.3.1。没有 DTMAPI Bootstrap、DTMAPI Runtime 或任何第一方产品加载行，收集器也报告
  游戏目录下找不到 DTMAPI 日志。对这个具体实例，“DTMAPI 在 NewGame/SaveLoaded 中断”
  已由运行事实排除。
- Early incompatibility warning: DolocPlus 初始化时已经报告
  `SeedUnlockUiDataInjectorController` 补丁失败，因为它仍引用当前 Assembly-CSharp 中不再
  存在的 `DolocTown.Config.Localization.StaticTexts` 类型。插件随后继续初始化，没有因该
  兼容性错误停止加载。
- Exact failure point: 原生新档正常完成场景切换并进入 `farm_type1-平地`，随后开始
  `aside_anim`。`stop_bgm`、`set_cam_pos`、`wait 2` 与 `play_anim player faint` 均成功；
  第一条旁白交给 Febucci TextAnimator 时，`TAnimBuilder.InitializeGlobalDatabase` 调
  `Assembly.GetTypes()` 扫描已加载程序集，立即抛出 `ReflectionTypeLoadException`。
  LoaderExceptions 全部指向 DolocPlus 类型：`ParkingApronShopping` 仍引用旧
  `ItemSubType`/`ItemProto`，`InfoUiHelper` 引用旧 `SeedNodeProto`，`FishAnalyzer` 引用旧
  `FishProto`，`NPCsInfoUiController` 引用旧 `ItemProto`。异常沿
  `TextAnimator.ShowText -> AsideDialoguePanel.OnStartShow -> DialoguePlayer.RenderTextLine`
  传播，Yarn 异步任务没有继续；这精确解释了 DialogueState/黑幕仍在而开场文字不出现。
- Controlled comparison from the same machine: 删除 BepInEx 后的 `Player.log` 不含任何
  BepInEx/插件加载行，走过同一房间、同一 `aside_anim` 和同一四条前置命令；紧接着
  TextAnimator 只记录非致命的空 tag warning，没有 ReflectionTypeLoadException 或其他
  exception，随后由玩家正常关闭游戏。结合用户确认“删除 BepInEx 后解决”，这是同机、
  同 official payload、同原生节点的强 A/B 证据。
- Root cause and minimum remediation: 本例根因是与当前官方 Assembly-CSharp 不兼容的外部
  DolocPlus 1.3.1 被装入进程；官方 TextAnimator 对坏程序集做全域类型扫描且没有按程序集
  隔离，使该不兼容在第一次显示开场旁白时表现为黑屏。删除整个 BepInEx 当然会消除它，
  但从本包可证明的最小故障集合是 DolocPlus 1.3.1，而不是 DTMAPI，也没有证据指向
  Configuration Manager。若要保留 BepInEx，应移除这版 DolocPlus，或只使用其作者针对
  当前正式游戏明确验证的新版本；不能把旧整包重新覆盖回游戏目录。
- Scope limit: 该证据闭合的是这一个玩家实例。此前截图中“左上 DTMAPI 快捷图标残留”的
  玩家若确实加载过 DTMAPI，仍可能是另一套 Mod 集合与另一条首个异常；应继续按各自支持
  包判定，不能因症状同名而改写成 DolocPlus 的全局结论。

## Cross-Issue Summary

- 这七条至少分成三组，不能合并甩锅或合并修复：
  - Issue 2/3/4/6：原生库存 mutation、鼠标 buffer 与 UI receiver 之间缺少异常隔离/
    回滚的共同模式；“看不见”还不能自动等于对象已从 archive 删除；
  - Issue 1/7：只在官方 Dialogue UI/state 层相交。一个 Issue 7 玩家实例现已由首个异常
    栈确认是外部 DolocPlus 1.3.1 破坏 TextAnimator 程序集扫描；此前另一个带左上 DTMAPI
    图标的报告仍只确认标题按钮泄漏，不能假定二者同根；
  - Issue 5：正式流程的 wetland_main 跨日前置与澳柯玛对话，独立于库存问题。不能用
    ruinedcity_continue 判定普通新流程；但本次玩家档确为旧版升级档，正常邮件早已发出，
    且精确命中该迁移信原本要修复的 visited-without-chain 状态。
- 此前旧存档鸡窝/机器人站重叠只属于 20260808-0001 的 cold-load 现场，不进入本记录
  Issue 7 的候选根因。
- 当前静态证据足以反对“1.00.02 改了所有相关代码/配置，所以七项全是同一更新回归”：
  相关 owner 代码和 GenDatas 实际内容未变；仅 level87 与一个无直接消费者的分页方法
  具有当前增量相关性。
- 当前静态证据也不足以发布“DTMAPI 对全部七条、全部玩家一概无责”：直接 Hook 缺失只能
  排除直接接管，而且 Issue 7 的标题按钮泄漏仍是明确 DTMAPI UI 生命周期缺陷。不过
  2026-08-09 这一个新档黑屏会话明确没有加载 DTMAPI，首个异常及全部 LoaderExceptions
  均落在 DolocPlus 1.3.1；该实例不得再归责 DTMAPI。
- 库存共同“先 mutation、后无隔离 callback”的窗口为高置信代码事实；具体是
  InventoryPanel、ExchangeInventory、RecordCollection、获得提示还是 achievement
  首先抛错，目前只有候选置信度，不能写成已闭合根因。应从同次完整 Player.log 的最早
  异常开始看；若 SaveLoaded/AfterLoadData 已先失败，后续库存表现可能只是部分初始化的
  级联症状。
- 最小玩家证据应在复现后立即、退出且不再次启动时由一键收集器导出；每一类问题至少
  需要一份独立支持包，不能用另一个玩家、另一个存档或此前黑屏包代替。

## Evidence Requested Next

- Issue 1 与其余 Issue 7 报告：失败当次完整 Player.log、Player-prev.log、BepInEx
  LogOutput、存在时的 DTMAPI latest/history、清晰截图或视频、所建槽位/具体 NPC，以及
  全部启用 Mod。优先定位实际加载集合、AfterLoadArchiveData 后第一个异常、aside_anim、
  TextAnimator、DialogueState、TransitScene 和 LoadingPanel。已闭合的 DolocPlus 1.3.1
  实例无需让玩家再次破坏性复现；如要核验最小修复，只需保留同一 BepInEx，单独移除该
  外部插件后新建可丢弃存档。
- Issue 2/3/4/6：一次干净启动只复现一条，异常后不要保存，立即退出并收完整日志；附
  物品 ID、容器类型、鼠标/手柄和逐键操作。优先定位最早的 LinearInventory、
  SingleInventory、ExchangeInventory、InventoryPanel/ItemNavSlot、RecordCollection、
  DropItem/SpecialDropItem 或 StorageShelf 栈。
- Issue 5：本次玩家档的持久根因和当前官方恢复路径已闭合，不再需要玩家继续睡觉或补
  手测日志。若要追查多年前第一次中断的精确外部触发者，只能补首次跨越 0.99 时的完整
  Player/BepInEx 日志与准确 build；当前包无法逆推出该历史瞬间。任何实际修复前须再次
  核对玩家原 ZIP hash，并对一次性邮件修复做幂等、失败不写档和正常保存验证。

## Implementation Record Decision

- 初始轮次是 audit/root-cause 工作，只维护这一份 Review，不修改 Debug、Hook 或 API。
  2026-08-09 的两次隔离诊断进入 active smoke matrix；随后用户单独授权的 Issue 5 私有
  存档定点修复由 Update `20260809-0001` 持有。它不改项目 Runtime，也不写玩家存档。
- 只有在某一条首个异常及直接 owner 闭合、用户授权修复后，才为该职责创建一个
  task-specific Update；不得用一个“大修七项”提交掩盖不同 owner。
- 在任何实现前，库存组至少要把“谁抛异常”从假设提升为日志/可控复现事实；新建存档组
  必须使用与玩家 Steam AutoCloud 隔离的 disposable fixture。

## 2026-08-10 追加：十项截图中的新增对话卡死与图鉴异常

- Status: static common-cause candidate identified; exact player instance still awaiting the
  failing session log and loaded DLL set
- Source: 用户追加截图
  `D:\文档\Tencent Files\3033654263\nt_qq\nt_data\Pic\2026-08\Ori\a4d29db04730d67ec8dddd6f3acc85a5.png`，
  SHA-256 `D582272A309EB4D533924E1FB709223535FF83C4293139B245DDEB0CA002CEB8`
- Source request fact: 用户说明官方已把其中若干项描述为功能性 Mod 问题，并要求继续定位
  其余新出现症状的可能原因；截图本身没有附官方原文、玩家日志、启用 Mod 清单或证明十项
  来自同一玩家，因此下文保留“官方归类”与“本轮可独立验证的技术证据”的边界。
- Additional artifact inspected: `references/third-party-mods/ExpandedEncyclopedia.zip`，
  SHA-256 `5D75718497A5372D546474DB9C2F8306935F6CFB74D1BCAC934B2D8765C99FC7`；
  包内 `ExpandedEncyclopedia.dll` 的 SHA-256 为
  `7F69DAA619BD371E9751EB5922325F89DDF1839A0F158FE9FD0AEBED5ECE37E2`。
  该闭源样本只作兼容性静态审查，没有复制或合并其实现。
- Review boundary: 本追加按新截图的十项原顺序逐项记录。旧记录 Issue 4“从箱子取出工具
  后消失”没有出现在这张新截图中，但仍保留在上文，不因本次重排而删除或降级。

### Follow-up 1/10：和角色对话没有出现对话选项

Original feedback:

- “和角色对话没有出现对话选项”。

Review record:

- Mapping: 对应上文 Issue 1；截图没有增加 NPC、节点、画面或首个异常。
- New cross-issue inference: 若实际表现是普通台词也未显示、对话随后卡住，则它可以与下述
  Follow-up 2–4 共用 TextAnimator 失败机制；若普通台词完整显示、输入仍响应而只缺候选项，
  则该机制不充分，因为候选项走 `DialogueOptionPanel`，仍需检查候选节点条件和数组内容。
  不得仅凭一句“没有选项”把两种情况合并。
- Current classification: 可能属于旧功能性 DLL 的间接对话呈现故障，但没有本玩家日志，
  尚未闭合到具体插件。

### Follow-up 2/10：和角色对话会导致游戏卡死

Original feedback:

- “和角色对话会导致游戏卡死”。

Review record:

- Official path: `NpcRenderer.OnInteract -> Npc.OnInteract ->
  DialogueState.DialogueWithNpc -> DialoguePlayer.RenderTextLine`。普通角色气泡最终由
  `BubbleDialoguePanel.OnStartShow` 调 `TextAnimatorPlayer.ShowText`；旁白则由
  `AsideDialoguePanel` 进入同一个 Febucci TextAnimator 边界。
- Confirmed precedent: 上文 Issue 7 的 DolocPlus 1.3.1 玩家日志已经证明，一个仍引用已删除
  官方类型的已加载程序集会令 TextAnimator 的全局 `Assembly.GetTypes()` 扫描抛
  `ReflectionTypeLoadException`，异常不被对话层隔离，Yarn 异步流程因此停住。这一机制不只
  适用于新档开场；任意第一次渲染角色台词都可触发。
- New static candidate: ExpandedEncyclopedia 1.0.0 的插件类会加载并对程序集执行
  `Harmony.CreateAndPatchAll`，其补丁类型仍直接引用当前 24585411 已不存在的
  `DolocTown.Config.Item.ItemType` 与 `ItemSubType`。如果玩家实际加载了这份 DLL，
  它能够形成与已确认 DolocPlus 故障相同的“坏程序集留在进程中，随后被 TextAnimator
  全局反射扫描”条件。
- Confidence boundary: 对“卡在文字呈现层”是高置信静态定位；对具体玩家就是
  ExpandedEncyclopedia 仍是条件结论。DolocPlus 1.3.1、ExpandedEncyclopedia 1.0.0 或
  其他含缺失类型的旧 DLL 都可能产生同样外观，必须以同次日志中的第一个异常和
  `LoaderExceptions` 定名。

### Follow-up 3/10：使用睡袋、档案等道具会导致游戏卡死

Original feedback:

- “使用睡袋、档案等道具会导致游戏卡死”。

Review record:

- Official item path: 睡袋配置为 `ItemFunctionSleepingBag`，节点是 `use_sleeping_bag`；
  档案/笔记类物品配置为 `ItemFunctionAnimation`，节点包括 `documents_cloud_1` 等。
  两者都由 `ItemAnimation.UseItemInternal` 调 `DolocAPI.StartDialogueNode`，并非纯背包
  mutation。
- Exact failure intersection: `use_sleeping_bag` 在真正执行 `fall_asleep` 前先显示
  “就这样，睡一觉吧...”旁白；档案节点也需要显示 Yarn 文本。两者都会进入
  `DialoguePlayer -> AsideDialoguePanel -> TextAnimatorPlayer.ShowText`。因此旧 DLL
  引起的同一个全局类型扫描异常足以同时解释“NPC 对话、睡袋、档案都卡住”。
- Save boundary: 若日志显示卡在睡袋第一条旁白，原生 `fall_asleep` 尚未被 Yarn 执行，
  不能先把问题定性为睡眠保存损坏。不过成功对照会继续进入睡眠流程；后续复现不要拿玩家
  活档做睡袋 A/B，应优先用 NPC/档案/垃圾桶验证共同异常，睡袋单项只能用隔离 disposable
  fixture。
- Alternative branch: 如果卡死发生在旁白已正常显示之后，则需另查 `fall_asleep`、时间推进
  和保存边界；当前截图没有证明这一分支。

### Follow-up 4/10：与垃圾桶、电话亭等场景对象交互会导致游戏卡死

Original feedback:

- “与垃圾桶、电话亭等场景对象交互会导致游戏卡死”。

Review record:

- Official trash path: `trash_can.yarn` 中垃圾桶节点具有 `@aside`，无论显示空桶提示、
  随机搜索文本还是特殊掉落分支，都可能进入旁白文字呈现。
- Official telephone path: level14 与 level21 场景中的电话亭都序列化绑定
  `dialogueNode: object_telephone_booth`；`city.yarn` 的该节点先显示旁白并创建拨号选项，
  选择工坊号码后才执行 `open_store phone_booth_shop` 或
  `open_exchange_store phone_booth_exchange_shop`。电话亭不是绕过对话层的纯商店按钮。
- Root-cause candidate: 垃圾桶和电话亭都在进入各自掉落/商店功能前经过同一
  `AsideDialoguePanel -> TextAnimatorPlayer.ShowText` 边界，故与 Follow-up 2/3
  共享旧不兼容程序集触发 `ReflectionTypeLoadException` 的候选根因，当前不再需要假定
  四套场景对象分别坏掉。
- Scope limit: “等场景对象”仍需逐个核对；只有实际绑定 Yarn/旁白的对象才能直接纳入这个
  共同链。若日志首先落在商店表或物品原型查询，则是退出对话后的第二分支，不能沿用本结论。

### Follow-up 5/10：创建新存档后黑屏，只能按 ESC 和右键，左上角有快捷图标

Original feedback:

- “创建新存档后黑屏，只能按ESC和右键，左上角有快捷图标”。

Review record:

- Mapping: 对应上文 Issue 7。新截图没有增加新的会话证据。
- Preserved conclusion: 已闭合的一个玩家实例是 DolocPlus 1.3.1 的缺失类型令开场第一条
  旁白在 TextAnimator 全局扫描处失败；另一个带左上 DTMAPI 图标的报告仍只确认 DTMAPI
  标题按钮生命周期泄漏，不能把两个玩家自动合并。ExpandedEncyclopedia 现在也是能造成
  同类反射失败的强静态候选，但没有证据证明该黑屏玩家加载了它。

### Follow-up 6/10：图鉴内的图像没有正常解锁

Original feedback:

- “图鉴内的图像没有正常解锁”。

Review record:

- Official unlock owner: 官方 `CollectionManager.RecordCollectionInfo` 在获得物品时把对应
  `CollectionRecord.isUnlock` 设为 true；`BookItemData.obtained` 读取该记录，
  `BookItemSlot.Render` 再用白色或黑色渲染图标。这是存档内图鉴记录与 UI 的官方直接路径。
- Important expectation boundary: ExpandedEncyclopedia 的原始实现并不会修改
  `CollectionRecord`，也不会把所有图像直接设为已获得。它的设计只是在配置加载时增加物品
  分类，并把部分物品的 `viewable` 改为 true。因此“扩展后能看见黑色未获得图标”本来就不
  等于“插件会替玩家解锁图鉴”；若玩家期待全图点亮，首先是功能理解不符，不是官方解锁
  记录丢失。
- Confirmed current incompatibility: 该 1.0.0 DLL 只识别旧文件名
  `settings_globalparameters` 与 `item_tbitemprotos`，而 24585411 实际加载
  `settings_tbglobalparameter` 与 `item_tbitem`。它还把 `item_collection_labels` 当旧
  `ItemType` 数字数组、读取物品的旧 `type` 字段；当前数据是字符串分类 ID、只有
  `sub_type`，并通过 `ItemSubTypeInfo -> ItemMainTypeInfo` 解析主类。即使忽略缺失类型，
  其两个文件名分支在当前 build 也不会命中，扩展图鉴功能不会生效。
- Two possible symptom branches:
  - 新分类/原本隐藏的条目没有出现：最可能是 ExpandedEncyclopedia 1.0.0 已失配并且补丁
    根本没有处理当前表；
  - 已实际获得的官方条目仍为黑图：需要检查该 item ID 对应的
    `CollectionRecord.isUnlock` 以及获得当次是否在 `RecordCollectionInfo` 前后抛错，
    不能仅靠插件名称定因。
- Cross-issue implication: 同一个 ExpandedEncyclopedia DLL 一方面已经不能实现图鉴扩展，
  另一方面又含当前游戏无法解析的旧类型；若日志证明它被加载，它还是 Follow-up 2–4
  对话卡死的首要共同嫌疑。这里的“功能失效”和“污染 TextAnimator 扫描”是同一旧 DLL 的
  两个后果，但仍须玩家日志确认实际加载事实。
- Ownership/remediation boundary: 这是外部 BepInEx 功能性插件与当前官方 ABI/配置 schema
  不兼容，不应由 DTMAPI 复制或热修闭源 DLL。最小玩家处置是移除该 1.0.0 版本，或使用
  作者针对当前正式 build 明确验证的新版；DTMAPI 后续若实现兼容诊断，需要独立授权和
  Update。

### Follow-up 7/10：前置主线任务已完成但未收到旧城市废墟邮件

Original feedback:

- “前置主线任务已经完成了，但是没有收到去旧城市废墟的邮件”。

Review record:

- Mapping: 对应上文 Issue 5；这张截图没有增加新证据。本次已取得的那个玩家档属于旧版
  升级后的 visited-without-chain 状态，并已完成定点恢复；它与新增对话卡死/图鉴插件
  失配不是同一 owner。

### Follow-up 8/10：箱子里的东西点击后消失，未出现在背包

Original feedback:

- “箱子里的东西点击后消失，未出现在背包”。

Review record:

- Mapping: 对应上文 Issue 2。截图没有增加物品 ID、箱型、背包容量、输入方式或日志。
- Preserved candidate: 官方 `Take` 已清源槽、随后 receiver 或目标 `PlaceItem` 失败的半事务
  窗口仍是最强机制；它不经 Yarn/TextAnimator，不能因 Follow-up 2–4 找到共同对话候选
  就把本条一并归给 ExpandedEncyclopedia。

#### 2026-08-11 实例证据：大型木箱中的 `wood 999 + 6`

玩家补充的精确现象：箱内木头总数超过 999 时，把超过 999 的部分取入背包，取出的木头
会消失；已经遇到约三次。本节只读检查玩家提供的 archive 与 DTMAPI 日志，没有启动游戏、
写回 archive 或改动本机游戏部署。

Evidence facts:

- `doloc-save-1.data` 在内存中按当前原生 archive envelope 解密；解密 JSON 的 UTF-8
  SHA-256 为 `5F50B66460676E8BE9A0EA46DA55267A44E318E841451794315B2D41FCBA01E2`，
  `archiveIndex=1`、`baseData.version=1.00.02`。
- 农场 equipment ID 84 是 `large_wooden_case`，40 个槽中 slot 11 为 `wood x999`、
  slot 12 为 `wood x6`。玩家背包容量 40，仅占用 slot 0–21，slot 22–39 共 18 个空槽，
  且当前没有木头。因而该 archive 精确保存了“999 满栈旁有 6 个余量、背包足够接收”的
  触发前状态；不是背包已满，也不是单格被非法存成 1005。
- exact build 24585411 的 `wood.overlay` 是 999。`999` 本身只负责把第 1000 个木头拆成
  第二个合法栈；正常原生路径应把这 6 个木头放入任一空背包槽，不存在因上限而主动删除
  这 6 个木头的规则。
- `latest.log` 覆盖 2026-08-09 12:30:11 至 22:33:58，报告 game build 24585411、
  20 个 native subscriptions 和 6 个已加载 DTMAPI CodeMods；其中包含
  `纸箱容量翻倍 1.0.10`（Workshop 3743621104）。该日志没有 error/fatal，两个
  `Exception` 文本也都只是 `exceptionType=none`。但它是 DTMAPI 自身日志，不是 Unity
  `Player.log` 或 BepInEx `LogOutput.log`，不会枚举所有 External BepInEx plugins，也未
  捕获官方/外部 Harmony 首个异常。

Native transaction analysis:

- exact build 的 `ContainerBaseUiState.PlaceToOtherSide` 顺序是：先以 `Clone(1)` 检查
  背包可放，随后 `containerInventory.Take(currentIndex)` 清除源槽，再调用
  `backpackInventory.PlaceItem(item)`，最后才把未放完的 remainder 写回原槽。
- `LinearInventory.Take` 清空槽位后会同步通知 receivers；整个序列没有
  `try/finally`、事务提交或失败回滚。只要 `Take` 的 receiver/Harmony Postfix，或其后的
  目标放置阶段抛错，源槽已经为空而背包提交尚未发生，外观正是“点击后两边都没有”。
- 玩家 archive 中的 6 个木头和 18 个空槽证明这条失败窗口对本实例实际可达；它把直接
  责任 owner 收敛到官方 `ContainerBaseUiState + LinearInventory` 库存事务。它不能单凭
  一个触发前快照证明此前三次的前后总数，也不能指出是哪一个 receiver 首先中断。
- 当前 public reverse reference `24650773_public_76C24E` 中
  `ContainerBaseUiState.cs`、`LinearInventory.cs`、`InventorySystem.cs`、`Item.cs` 与
  `SingleInventory.cs` 和 24585411 对应源文件逐文件 SHA-256 相同；该非原子顺序在当前
  本机正式版参考中仍未改变。

Mod attribution:

- 对日志实际加载的 `纸箱容量翻倍 1.0.10` 本机 Workshop DLL 做只读 IL 审查后，其
  Harmony targets 是 ItemBox/StorageBox 容量、`BoxInventoryWidget` 和
  StorageShelf UI/layout；没有 patch `ContainerBaseUiState`、`LinearInventory`、
  `InventorySystem` 或这只世界 `large_wooden_case` 的直接转移路径。因此它适合作为后续
  隔离变量，但现有证据不支持把本实例直接归给它。
- 同会话其余五个已加载 DTMAPI CodeMods 也没有接管上述转移 owner，DTMAPI 日志中没有
  handler failure。当前没有证据把丢失归给 DTMAPI Runtime 或这六个已识别产品。
- 已有关联 DLL 审查发现，外部 `Mxx_DolocTownMod_Plugins.dll`
  (`com.mxx.doloc.itemlimiter`) 会给 `LinearInventory.Take` 等方法安装无异常隔离的
  Postfix；若其 `Take` Postfix 在官方已经清源槽后抛错，恰好会阻止后续背包
  `PlaceItem`。这是目前结构上最精确的条件性首因候选，但本包没有 BepInEx/Unity 日志或
  `BepInEx/plugins` 清单，不能证明该 helper 在本次玩家会话确实安装、加载或抛错。

Decision:

- 本次实例在“症状与直接责任 owner”层面符合官方已说明的库存丢失类 bug：官方先删源、
  后提交目标且无回滚，是物品能够永久消失的必要缺陷。
- 目前还不能把“首次中断是谁”写成已确认。最强条件性候选是上述 Mxx external helper；
  `纸箱容量翻倍 1.0.10` 不是这只大型木箱路径的直接 patch 方。该 Mxx 候选只属于首次
  两文件包的证据边界，已由下节完整回传包在这个玩家实例中降级，不再作为本玩家首要候选。
- 下一份最小证据应是同一次复现的 `Player.log`、`Player-prev.log`、BepInEx
  `LogOutput.log` 和实际 `BepInEx/plugins` 文件/hash 清单，优先定位
  `com.mxx.doloc.itemlimiter`、`MXX_BOOT` 及源槽清空后的第一个异常。隔离复现应使用存档
  副本且不触发原生保存，先只移除 Mxx helper；若仍复现，再单独移除
  `纸箱容量翻倍` 作对照。

Validation: 完成两个输入文件的 size/hash、archive 内存解密与库存对象定位、24585411
原生数据/转移源码复核、当前 public reverse source 对比、六个 DTMAPI CodeMods 的相关
Harmony target 审查，以及 `纸箱容量翻倍 1.0.10` 的只读 IL 审查；未启动游戏，未修改
玩家存档、游戏目录或 Workshop 订阅内容。

#### 2026-08-11 完整回传包：Mxx 从本玩家首要候选中排除，历史木头差额大多可核销

Source:

- `D:\下载\DTMAPI-logs_ the\DTMAPI-player-support-20260811-005826-139-d01b7b2d.zip`
- 8,435,581 bytes；SHA-256
  `882C856D1A1A981424B5DAF9169760932CBBD3F54018CA0C11C9B8D9B98F6A4A`。
- Collector 报告 `CollectionStatus=Complete`、19 files、12,051,845 copied bytes、
  0 errors；唯一 warning 是没有 Unity `Crashes` 目录。

Current-session facts:

- 当前 `Player.log` 是 2026-08-11 00:39–00:41 的新会话，game build 已由问题发生时的
  24585411 更新到 24650773。BepInEx 明确报告 `1 plugin to load`，唯一插件是
  `DTMAPI Bootstrap 0.6.1.0`；没有加载 `Mxx_DolocTownMod_Plugins.dll`、DolocPlus 或其他
  External BepInEx Plugin。
- `mod_infos.json` 保存的 20 个启用 Workshop IDs 中没有 Mxx installer 3759797170；
  `install-state.json` 还记录 DTMAPI 在 2026-08-08 安装前
  `BepInExDetectedBeforeInstall=false`、`BepInExInstalledByDTMAPI=true`、
  `LegacyDetections=[]`。这些证据足以把 Mxx 从这个玩家的首要候选中移除；但由于没有
  8 月 9 日长会话的 BepInEx `LogOutput.log`，不能把“那一进程绝无临时 DLL”提升为绝对
  运行证明。
- 当前会话正常 Load slot 1，进入并退出 `FarmCaseUiState` 各 9 次，随后正常关闭；严格
  扫描没有 exception stack、error/fatal 或 DTMAPI handler failure。日志中的两个
  `Exception` 文本仍是 `exceptionType=none`。本次没有 `SaveSaving`/`SaveSaved`，所以
  current archive 仍是 8 月 9 日 22:33 的同一文件；其 SHA-256 与首次提供的
  `264C6E3B...5936CE6` 完全相同。
- `Player-prev.log` 只有 315 bytes 的 Mono fallback-loader 信息，不是 8 月 9 日丢失会话；
  Windows Events 唯一记录是 8 月 8 日的 `RADAR_PRE_LEAK_64`，也没有库存异常栈。

Archive sequence:

| Archive | Game day/time | ID 84 large case | ID 33 case | Backpack | farm equipment + backpack wood |
|---|---:|---:|---:|---:|---:|
| `.prev4` | Y1 M2 D22 06:00 | `366 + 999` | 0 | 0 | 1365 |
| `.prev3` | Y1 M2 D23 06:00 | 0 | 172 | 994 | 1166 |
| `.prev2` | Y1 M2 D24 06:00 | `999 + 130` | 213 | 0 | 1342 |
| `.prev1` | Y1 M2 D25 06:00 | 993 | 335 | 0 | 1328 |
| `.prev0` | Y1 M2 D26 06:00 | `999 + 170 + 1` | 335 | 0 | 1505 |
| current | Y1 M2 D26 13:50 | `999 + 6` | 506 | 0 | 1511 |

The largest apparent drop is fully reconciled by native counters and exact 24585411 recipes:

- D22 → D23 inventory delta: `1365 -> 1166 = -199`。
- `OBTAIN_ITEM wood`: `+1`。
- `MAKE_ITEM`: 51 crafts of `pt_wood` × 1 wood、10 `plantbasin_shrub` × 6 wood、
  8 `plantbasin_tree` × 10 wood，合计 `-191` wood。
- `USE_EQUIPMENT wood_generator`: `+9` interactions；原生 `PowerGeneratorFuel.OnInteract`
  每次成功操作都会 `CostSelf` 一个当前燃料后才记录一次该事件。以木头作为这 9 次燃料时，
  `1365 + 1 - 191 - 9 = 1166`，与 archive 精确相等。
- 在这些 generator interactions 同样使用木头作为燃料的条件下，D23 → D24 的 `+176`
  等于 `+182 obtained - 6 generator uses`，D24 → D25 的 `-14` 等于 14 次 generator
  use；current 相对 `.prev0` 的 `+6` 则无条件等于新增获得 6 个木头。
- D25 → D26 尚有 10 个木头不能仅靠官方 recipe 表直接归类；同区间恰好新增制作
  `TFM_survival_burger x10` 并使用 `pot x10`，其输入定义来自玩家订阅的内容 Mod，未包含在
  support ZIP 或本机 reference 中。该 10 个差额与 999 余量转移没有时间/槽位证据关联，
  不能据此认定丢失，也不能在缺少内容配方时声称已经完全核销。

Revised decision:

- 六份 archive 没有给出一段可确认的“从箱子取出后无正常用途、库存总数仍下降”的保存
  转移。此前最像丢失的 199 已完整核销，另一个 10 也与同区间十次内容 Mod 烹饪相邻。
- 因而玩家所述现象仍应保留为待复现反馈，但当前证据不能称作已确认的永久丢物实例。
  `wood 999 + 6` 只证明触发条件存在；官方 `Take -> PlaceItem` 无回滚仍是结构风险，不是
  本玩家已经捕获的实际首因。
- `纸箱容量翻倍 1.0.10` 仍没有 patch 这只 world `large_wooden_case` 的 direct-transfer
  owner；完整包也没有产生支持归责它的新异常。Mxx 对本玩家不再是首要候选。
- 下一步不是继续猜 Mod，而是让玩家在 current build 上用 current archive 副本进行一次
  `NoNativeSave` 定点录屏：同时拍到 `999 + 6`、背包目标空槽、使用的鼠标键/辅助键，以及
  单次动作后的箱槽、背包槽和鼠标 buffer。若能稳定复现但仍无异常，才需要临时诊断 Hook
  记录 `ContainerBaseUiState`、两个 `LinearInventory` 与 `InventorySystem.buffer` 的逐步
  count/slot 变化。

Validation: 对外层 ZIP 和 collector inventory 做 size/hash/complete-status 复核；只读检查
当前 Unity/BepInEx/DTMAPI 日志、安装状态和启用 Workshop 清单；在内存中解密并比较六份
archive，按 24585411 官方 recipe 与 event counters 核销木头变化；未启动游戏、未解压
覆盖玩家目录、未写回任何 archive。

#### 2026-08-11 代码反查：未发现 DTMAPI、第一方或已取得第三方代码首因

本节按用户要求先审计 DTMAPI Runtime 与第一方 CodeMods，再审计玩家实际启用的第三方
CodeMods 和可取得的 native 内容包。目标是判断是否存在能在
`large_wooden_case -> FarmCaseUiState -> ContainerBaseUiState.PlaceToOtherSide` 路径上删除
`wood x6` 的可达代码；本节不把“注册了相关 API”或“名称涉及箱子”当作实际执行证据。

Evidence boundary:

- 完整回传包的 release manifest 记录 DTMAPI 0.6.1、build commit `db5e518a6d7f`；Bootstrap、
  Abstractions、Core、GameBridge、ModConfigMenu 和 Compatibility 的逐文件 SHA-256 均与本机
  可反编译副本一致，因此 Runtime 审计是精确二进制审计。
- 8 月 9 日长会话明确加载 6 个 CodeMods，严格扫描为 0 errors、0 warnings，日志中没有非
  `none` exception。第一方产品 DLL 没有收入 collector，因此第一方结论来自该会话记录的
  产品身份/Hook 清单、当前跟踪源和能取得的本地产品包；其中 ActionSpeed 会话为 11 hooks，
  不把本机另一个 9-hook 旧包冒充玩家精确字节。
- `纸箱与货架容量翻倍 1.0.10` 是 Catalog 精确 hash-bound 包；DLL SHA-256
  `45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366`。
  PriceHelper 只取得同来源、同版本 2.0.1 的本地包，没有玩家 DLL 的逐字节 receipt，故其
  结论保持“同版本静态审计”，不扩大为玩家包哈希证明。

DTMAPI 与第一方反查：

| Owner | 实际触点 | 对本实例的结论 |
|---|---|---|
| DTMAPI Runtime 0.6.1 | 没有 patch `ContainerBaseUiState`、`FarmCaseUiState`、`LinearInventory.Take/Place/Swap` 或本次箱到背包转移；DebugConsole `CostItem`、ActionCompletion 消耗和相关回调均按需求启用 | 会话中 DebugConsole、`CostItem`、`TryPlaceInBackpack`、`OneAction` 需求/调用均为 0；其余库存相邻功能没有取得本路径执行权，未找到删除木头入口 |
| 箱子定位器增强 | 唯一 postfix 是 `ArchiveDataHandle.GetAvailableInventories`，只为材料统计追加原生 inventory 引用，遍历有异常隔离 | 当前记录 `base=4, appended=0, sharedCases=0, sharedStorageBoxes=0`；不调用 `Take`/`Place`，排除 |
| 更多装备栏位 | Runtime compatibility 只 patch 装备参数、受击与 AccessoriesBar；空的 DTMAPI 扩展装备槽点击才会进入鼠标 buffer 取装 | `EquipExtraSlotFromNativeBuffer` 在 `buffer.Take` 后缺少完整事务回滚，是一个真实的通用风险，但入口先要求 count=1 且物品为被动装备/帽子。会话只记录 amoeba_hat、welder_helmet、partyhat 操作，没有 wood，不能解释本案的 `wood x6` |
| 动作加速 | 会话 11 hooks 只覆盖 tool/water/interact/eat 状态、持续输入、AnimalRenderer 和状态退出 | 不 patch 容器或 inventory；唯一物品调用是类型和水域交互共同限定的瓶子补水，排除木头转移 |
| 自动钓鱼 | 22 hooks 均属于钓鱼状态、滚动条、钓竿输入/渲染 | 只读取当前选中物确认鱼竿，不接管箱子、木头或 `Take`/`Place`，排除 |

Runtime 中另有鱼籽 tooltip 的 title/description/detail 只读适配；回调按需求启用、有异常隔离并在
失败时恢复原结果。它不改变 inventory。`Feature.ActionCompletion=ready` 也只是能力注册，不表示
本会话存在消耗策略消费者。

第三方反查：

| Product | Harmony targets / 行为 | 对本实例的结论 |
|---|---|---|
| 价格与种子信息 2.0.1 | 只 patch `ItemData(Item)` 构造、`ItemData.ShowAsGood` 和 `ItemHoverBox.RenderAndShow`，追加价格/种子文字与 tooltip 滚动 | 不改物品数量或 inventory。它创建的透明 Image 虽设 `raycastTarget=true`，但位于官方 `ItemHoverBox` 的 `CanvasGroup.blocksRaycasts=false` 下，不能截获箱格点击；各修改体也有异常隔离，排除 |
| 纸箱与货架容量翻倍 1.0.10 | 只 patch `ItemFunctionBox`、`ItemBox`、`StorageBox` 容量，`BoxInventoryWidget` 和 `StorageShelfUiState/Panel/Widget` | 世界大型木箱是独立的 `Case + FarmCaseUiState + ContainerWidget`；该 Mod 没有 patch 本次四个直接 owner。它仍与纸箱/储物架问题相关，但不是本次大型木箱转移首因 |

玩家 20 个启用 Workshop 项中，除 DTMAPI、上述 6 个 CodeMods 外还有 13 个 native 内容包。
本机取得其中 8 个精确包体：全部为 0 DLL，也没有 `wood` 物品定义或覆盖；只在“多洛可的
妖精面包房”和“枭のmod工坊”的 recipe 中把 wood 作为正常材料消耗。另 5 个包未能匿名取得
精确包体，不能对其 JSON 作绝对排除；但 BepInEx 当前只加载 Bootstrap，8 月 9 日 DTMAPI
也只发现上述 6 个 CodeMods，所以这 5 个包没有可执行库存 Hook。它们至多保留为未核验的
数据表边缘变量；当前干净会话 9 次进出 `FarmCaseUiState` 无异常，且原样读到了合法的
`wood x999 + x6`，不支持“wood 元数据已被内容包改坏”为首要解释。

Rejected native hypothesis:

- 官方 `SingleInventory.UnbindReceiver` 的实现确有明显笔误：它调用 `receivers.Add(receiver)`
  而非 Remove。但 exact build 全局没有任何调用者；buffer receiver 只在 archive 构造/加载时
  对同一个 `inventoryMouse.Render` 调用 `BindReceiver`，后者还会去重。反复开关大型木箱不会
  因此累加 receiver，所以这是原生潜在缺陷，但不是本实例可达首因。

Decision:

- 没有找到 DTMAPI Runtime 或第一方产品能在本实例条件下删除 `wood x6` 的可达代码；也没有
  找到两个已启用第三方可执行 Mod 的对应路径。现有静态证据不支持先归责或先禁用第一方。
- 更多装备栏位的单件装备非事务窗口应在其自己的 owner 下另行治理，不能因为同样“缺回滚”
  就映射到木头；`纸箱与货架容量翻倍` 也只保留给纸箱/储物架问题。
- 当前最接近现象的机制仍是官方 `PlaceToOtherSide` 的非原子顺序：先清源槽，再提交背包，
  中间没有异常回滚。代码反查排除了已知 Mod interrupter，却无法仅靠事后 archive 证明究竟是
  原生/UI receiver 在某次动作中断，还是玩家看到的是鼠标 buffer/画面状态而非已保存丢失。
- 因而下一步应保留当前 Mod 组合复现一次，不保存并在故障后不再重启，立即导出日志，同时
  录到动作前后的箱槽、背包槽、鼠标 buffer 和输入键。普通日志若仍无异常，只能证明没有
  抛错；要定位无异常的数据流，需另行授权临时诊断 Hook 记录源/目标/buffer 每一步数量。
  若随后做最小 A/B，先分别停用 PriceHelper 与纸箱容量 Mod 这两个第三方可执行变量即可，
  无需先拆第一方；该顺序是隔离策略，不表示静态审计已经怀疑它们。

Validation: 完成 exact Runtime 六程序集、4 个第一方产品、2 个第三方 CodeMods、8 个可取得
native 内容包和 24585411/24650773 相关原生路径的只读代码/IL/数据审计；未启动游戏、未部署
Runtime、未修改 Workshop 内容或玩家存档。另 5 个 native 内容包未取得精确包体，已按上述
证据边界保留，不把不可得材料写成已检查通过。

#### 2026-08-12 第二次完整回传：73 个木头全部核销，另捕获锅子配方过滤空引用

玩家本次描述：从箱子取出六十多个木材，随后给发电机加柴，最后在锅子前做饭时发现取出的
木头不见了。以下结论来自同一玩家的新完整支持包和其中一次正常保存；仍是只读分析，没有
启动游戏、写回 archive 或修改玩家部署。

Source:

- `D:\下载\DTMAPI-logs_ the\DTMAPI-player-support-20260811-215542-681-7f14ed46.zip`
  为 8,433,145 bytes，SHA-256
  `55750F58C2EEFA564ABD9CFEE523B3B78101DE9623C1D7172B619412858045CC`。
- Collector 报告 `CollectionStatus=Complete`、22 files、12,464,778 copied bytes、10 个
  save files、0 errors；唯一 warning 是没有 Unity `Crashes` 目录。会话使用 game build
  24650773、DTMAPI 0.6.1，21 个启用订阅中有 7 个 CodeMods。
- current archive 为 1,708,471 bytes、SHA-256
  `203B4DF8A0CB70F09BDFD84BC6D2C43E8A46585D550E2C8F20D97DCEB4F730A1`；其 `.prev0`
  为 1,699,619 bytes、SHA-256
  `E2797DF868D12820D6F25B5B4CC11E9A0513B311E8811FF8A6A2E998ED8AAA4E`。

Archive reconciliation:

| Fact | `.prev0` (Y1 M3 D1 06:55) | current (Y1 M3 D1 11:15) | Delta |
|---|---:|---:|---:|
| farm equipment + backpack 中的 `wood` | 1632 | 1559 | **-73** |
| backpack `wood` | 22 | 6 | -16 |
| wooden case ID 7 | 0 | 61 | +61 |
| wooden case ID 33 | 506 | 506 | 0 |
| large wooden case ID 84 | 1104 | 986 | -118 |
| `OBTAIN_ITEM wood` | 2914 | 2914 | 0 |
| `SUBMIT_ITEM wood` | 30 | 30 | 0 |
| `MAKE_ITEM barrel` | 0 | 2 | +2 |
| `MAKE_ITEM TFM_survival_burger` | 30 | 32 | +2 completed |
| `USE_EQUIPMENT wood_generator` | 33 | 41 | +8 |
| `USE_EQUIPMENT pot` | 104 | 106 | +2 completed |

The exact 73-item balance is closed:

1. 当前官方 `barrel` recipe 每个消耗 `wood x30` 和 `resin x2`；本段新增制作 2 个，故
   消耗 **60 wood**。`Player.log` 还把 `EquipmentPanelUiState -> CraftQuantitySubmitUiState`
   记录在第一次 `FarmCaseUiState` 之前，说明两只木桶是在本轮反复开箱前制作的，不是事后
   为对账臆测的用途。
2. 当前官方 `PowerGeneratorFuel.OnInteract` 只在 `item.CostSelf` 成功后才增加燃料并发送
   `USE_EQUIPMENT`。本段该计数增加 8，因而精确消费 **8 wood**。`wood` 的
   `electric_energy=50`、该发电机 `rate=2`，8 次共加入 800 fuel；current 保存为 668，
   与期间持续发电一致。21:51:23 的 ActionSpeed 行只说明交互/连续输入按 2 倍计时，不能把
   已由原生成功事件计数核销的 8 次消费改写成丢失。
3. current 的锅子保存 `latestRecipe=TFM_recipe_survival_burger`、
   `latestRecipeGroup=pot`，类型为固定 `Recipe/RecipeGroup`，且 `taskCounter=value 2,
   interval 5`：玩家一次提交了 5 份，保存时完成 2、剩余 3。官方
   `RecipePanelUiState.TryConfirmCraft` 会先以整个 `count` 调用
   `TryCostInputItemsInInventory`，再把同一 `count` 交给设备启动任务；不是每出锅一份才扣
   一份。前一完整包的相邻 archive 已用 `TFM_survival_burger x10 + pot x10` 对上唯一的
   `-10 wood`，故该内容 recipe 是每份 1 wood，本次在提交时消费 **5 wood**。

因此 `60 + 8 + 5 = 73`，与 archive 总量差精确相等；同区间没有取得或提交木头，event
recorder 的其他新增产物只有 `iron_ingot x1`。current 还明确在另一只普通 wooden case
ID 7 中保存了 `wood x61`。无法从事后 archive 证明它就是玩家记忆中的同一栈，但“六十多个
木头”并未在最终状态中形成无法解释的缺口。这次保存证据 **不是** 官方箱到背包半事务丢物
bug 的复现；此前确认的 `Take -> PlaceItem` 无回滚结构风险仍保留，但不能套到本次已守恒的
实例上。

Separate confirmed failure at the pot:

- `Player.log` 的真实异常是第一次打开锅子 `RecipePanelUiState` 时发生一次
  `NullReferenceException`，顶栈为
  `DolocTown.Synthesizer.BufferItemFilter(Item) [0x00148]`；UI 退出后第二次打开成功，并进入
  `CraftQuantitySubmitUiState`。
- 当前源码在缓存 miss 时先执行
  `craftFilterCache[proto.Id] = new HashSet<string>()`，随后遍历
  `DishInfo.InputClass -> IngredientGroupInfo.Items_Ref`，无空值检查地读取每个
  `ItemInfo.Id`。本次偏移和源码都指向其中一个 `ItemInfo` 为 null，即某个菜肴食材组引用了
  未能解析的 item ID。缓存条目先于遍历创建，所以异常留下半初始化空缓存；第二次打开命中
  该缓存并跳过坏引用扫描。这解释了“先报错、再能打开”，不是数据自行恢复。
- 直接健壮性 owner 是官方 `Synthesizer.BufferItemFilter`；触发数据最可能来自启用的功能性
  菜肴内容包或其跨包依赖。支持包没有收集这些 Workshop JSON/精确 hash，而本机也没有该
  玩家版本的 `战后美食模组`、`菜肴调味`、`豆制品工艺` 三个包体，故目前不能诚实地把坏
  item ID 归给其中某一个作者。

Mod attribution and next evidence:

- 这轮新增并冷启动加载了 `魔法存储 Magic Storage` (`Minato.MagicStorage`)；日志称其包含
  recipe-aware dispatch。但 current archive 中 Magic Storage 身份只存在于电话亭商店库存，
  农场、背包和设备列表没有放置/持有其 core、supplier 或 dispatcher。玩家精确版本的 DLL
  也不在支持包，本机可取得的旧 0.3.2 不能冒充该版本。因此它适合作为锅子空引用的第一个
  A/B 变量，却没有证据把本次木头变化归给它；木头守恒本身已经闭合。
- DTMAPI/第一方仍没有本次箱转移删除入口，owner event handlers 为 0 failures；箱子定位器
  本轮 `base=4, appended=0`，没有追加 inventory。ActionSpeed 只使发电机输入更快，实际
  消费仍被官方 +8 事件精确约束。
- 若只复核真实的新锅子故障，先停用 Magic Storage、重启并第一次打开锅子；仍抛错时再按
  菜肴内容包做二分，或收集其精确 JSON 后直接找 unresolved ingredient item ID。这个顺序是
  隔离策略，不是已经完成的归责。
- 若玩家仍声称存在箱子即时丢物，不应再把本次正常制作/加柴/做饭混在同一观测窗：应选定
  一只箱子和一栈木头，录下总数，单次取出后不制作、不操作设备、不保存，立即拍下源槽、
  背包槽和鼠标 buffer 并导出日志。只有那种前后总数不守恒的窗口才是库存 bug 新证据。

Decision: 本次“取出六十多个木头后消失”应从疑似官方库存丢失实例中撤回，归类为 **2 个
木桶 + 8 次发电机加柴 + 5 份料理预扣料**造成的观察误判；另立一个已确认的锅子首次打开
空引用，当前责任边界为“功能性菜肴数据提供无效 item 引用 + 官方过滤器无 null guard”。

Validation: 完成外层 ZIP/collector size、hash、complete-status 复核；在内存中解密并对比
current/`.prev0` archive；核对所有 wood 位置、相关 event recorder、设备任务与燃料状态；
复核 24650773 的 barrel recipe、发电机消费路径、RecipePanel 批量预扣路径和
`Synthesizer.BufferItemFilter` 缓存/空引用路径；扫描 Unity/BepInEx/DTMAPI 日志。未启动
游戏，未写出明文 archive，未修改玩家存档、游戏目录或 Workshop 订阅。

### Follow-up 9/10：拾取物品后栏位变化但贴图不消失，可重复拾取

Original feedback:

- “拾取物品，物品栏有变化，但是贴图未消失，物品栏数量也一直在变化，可以重复拾取”。

Review record:

- Mapping: 对应上文 Issue 3。截图没有增加物品/场景 ID 或异常栈。
- Preserved candidate: 背包添加已提交、后续世界对象销毁/禁用未完成的原生半事务窗口仍与
  现象精确吻合；它与图鉴记录可能在同一获得回调序列相邻，但没有证据说明旧图鉴插件就是
  首个抛错者。

### Follow-up 10/10：储物架放满后再次放纸箱会吞掉手持箱及内容

Original feedback:

- “储物架纸箱放满后，手上拿着一个纸箱，此时点击交互键，手上的纸箱和里面的物品都消失了”。

Review record:

- Mapping: 对应上文 Issue 6；本次文字把“放满”进一步指向储物架槽位全满，而不只是箱内
  inventory 满。后续复现应优先检查架槽全满时 `StorageShelfUiState` 首帧 Confirm 输入、
  手持 buffer 与 shelf 交换失败后的回滚，不再把“架上仍有空槽”作为唯一主分支。
- Ownership boundary: 该路径不需要 Yarn/TextAnimator；仍属于 StorageShelf、UI buffer
  与 InventorySystem 的官方直接 owner，不能由对话组的新证据代替独立日志和守恒矩阵。

### 2026-08-10 cross-issue decision

- 新出现的 Follow-up 2、3、4 不是三个独立卡死点：角色、睡袋/档案、垃圾桶和电话亭都在
  失败前收敛到官方 Yarn line view 与 Febucci TextAnimator。最强共同解释是进程中存在
  引用已删除官方类型的旧功能性 DLL，TextAnimator 无隔离扫描全部程序集时抛
  `ReflectionTypeLoadException`。
- ExpandedEncyclopedia 1.0.0 是本轮新找到的具体高风险样本：它同时精确解释图鉴扩展为何
  不工作，并具备污染上述全局反射扫描的条件。DolocPlus 1.3.1 已由另一玩家日志确认能产生
  同一机制。没有本截图玩家的加载日志前，结论必须保持“机制高置信、具体 DLL 未确认”。
- Follow-up 1 只有在普通台词也不显示/卡住时才并入对话组；只缺候选项仍是独立分支。
  Follow-up 7 的邮件链和 Follow-up 8–10 的库存/储物半事务仍是各自 owner，不随对话组
  合并。
- Minimum evidence: 在一次冷启动中优先用 NPC、档案、垃圾桶或电话亭复现，故障后不要再
  启动游戏，立即收集完整 Player.log、BepInEx LogOutput、全部加载 DLL 的版本/hash。
  首先检索 `ReflectionTypeLoadException`、`TAnimBuilder.InitializeGlobalDatabase` 和
  `LoaderExceptions`，看缺失类型是否来自 ExpandedEncyclopedia、DolocPlus 或其他 DLL。
- Minimum A/B: 保留相同 BepInEx 和其他 Mod，单独移除命中的旧 DLL 后冷启动对照；不要只
  用“删除整个 BepInEx 后恢复”代替最小归因。若同时加载多个坏程序集，移除一个后可能暴露
  下一个，必须以每次首个 LoaderException 继续收敛。
- Validation not run: 本追加只做工作区文档、当前 24585411 反编译/AssetRipper 参考和第三方
  样本的静态只读审查；未启动游戏、未部署 Runtime、未运行玩家存档，也未修改任何保存数据。

## 2026-08-10 更正：排除 ExpandedEncyclopedia，收敛到 DolocPlus 版本族

- Superseding user instruction: 用户明确要求从当前候选中排除 ExpandedEncyclopedia 1.0.0，
  原因是其实际用户量过少；本节取代上一节把它列为“首要共同嫌疑”的调查优先级。上一节保留
  为当时的静态假设记录，但后续归因、取证和玩家处置不再以该插件为目标。
- Scope: 只审查本机可找到的 DolocPlus 二进制版本、现有玩家支持包中的 DolocPlus 会话，
  以及它们与本截图十项症状的交集；不启动游戏，不把第三方 DLL 安装到共享 Runtime。

### 本机版本与来源

- Local 1.3.2:
  - 外层归档：
    `references/third-party-mods/小神增强包/DolocTownEA_BepInEx_DolocPlusMod.7z`，
    11,367,665 bytes，SHA-256
    `295EB1EA257E8F86521B858738C05E6C6448B8540F2770FABB92E8728058C1A6`；
  - `DolocPlus.dll`：126,976 bytes，插件元数据显示 1.3.2，SHA-256
    `99241AF3D4B8B7A8885C904F82F7399C55B7D5F239DBE24EAFAAF041C1B8A420`；
  - 工作区 `.tmp/inspect-xiaoshen-fishing` 副本与
    `C:\Users\Administrator\AppData\Local\Temp\dtmapi-dolocplus-inspect\BepInEx\plugins\DolocPlus.dll`
    是同一 SHA-256，不是第三个版本。
- Local 1.4.0:
  - 外层归档：
    `references/third-party-mods/小神增强包/20260801-v1.4.0-ct1.9/DolocTownEA_BepInEx_DolocPlusMod.7z`，
    11,369,633 bytes，SHA-256
    `AE09C98082E145D1FA560FFAA8FAD57F0ACEA53389EDF73ADC1E1A34051DCD14`；
  - `DolocPlus.dll`：129,536 bytes，插件元数据显示 1.4.0，SHA-256
    `BEAA313EFC99BC5ADDCC76C8220BFE19CDB479BF9F843005B3F592CB0E056EA1`。
- Logged 1.3.1: 本机没有找到可单独哈希的 1.3.1 DLL；但
  `D:\下载\余圻0809\DTMAPI-player-support-20260809-104106-614-749d3b84.zip`
  （SHA-256 `8393D919381574622FA8630EA16DFDAABFFDA352CBD4D99BAA1B6C5D1F0F5C14`）
  的 BepInEx 与 Player-prev 日志都明确记录加载 1.3.1。它是一个有运行证据但没有随包二进制
  的第三个历史版本，不能和本机两份 DLL 混称为同一版本。
- Current installation: 当前
  `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins` 没有 DolocPlus DLL；本轮没有
  更改该状态。

### 版本级兼容性结论

#### DolocPlus 1.3.1：对话类故障已由玩家日志确认

- 上文已有运行证据继续成立：1.3.1 在启动阶段先因已删除的
  `DolocTown.Config.Localization.StaticTexts` 令 SeedUnlock 补丁失败，但插件继续加载。
- 第一次显示 `aside_anim` 文字时，Febucci
  `TAnimBuilder.InitializeGlobalDatabase -> Assembly.GetTypes()` 抛
  `ReflectionTypeLoadException`。LoaderExceptions 指向 DolocPlus 内的旧
  `ItemSubType`、`ItemProto`、`SeedNodeProto` 与 `FishProto` 字段/闭包类型；异常随后沿
  `TextAnimator.ShowText -> AsideDialoguePanel.OnStartShow ->
  DialoguePlayer.RenderTextLine` 中断 Yarn。
- 同机移除 BepInEx 后对照通过同一开场节点。因此对那个玩家的新档黑屏，DolocPlus 1.3.1
  不是候选而是已确认根因。

#### DolocPlus 1.3.2：与 1.3.1 属于同一全局反射故障家族

- 对 1.3.2 DLL 与当前 public 24585411 `Assembly-CSharp.dll` 做 Mono.Cecil 只读解析：
  183 个直接游戏 TypeRef 中有 23 个当前不存在，包括 `ItemProto`、`ItemSubType`、
  `SeedNodeProto`、`FishProto`、`StaticTexts` 以及其他旧 `*Proto`/旧表类型。
- 更关键的是，其中 4 类缺失类型直接出现在 17 个字段或方法签名中：
  `ItemProto`、`ItemSubType`、`SeedNodeProto`、`FishProto`。owner 与 1.3.1 玩家日志中的
  LoaderExceptions 一致，包含 `ParkingApronShopping`、`InfoUiHelper`、`FishAnalyzer` 和
  `NPCsInfoUiController`。
- 因此 1.3.2 即使所有相关玩法开关关闭，只要程序集已被 BepInEx 装入 AppDomain，官方
  TextAnimator 的全程序集 `GetTypes()` 仍会扫描这些类型。它对 NPC/旁白对话造成同类
  `ReflectionTypeLoadException` 是高置信静态结论，不需要先启用停车坪购物、鱼情分析或
  NPC 信息功能。
- 1.3.2 的版本警告只比较 `Application.version.Minor`。在 1.00.x 上 minor 为 0，它反而
  不能可靠识别从 0.x 到 1.x 的大版本跨越，也不会形成安全停止加载。

#### DolocPlus 1.4.0：修复了全局类型污染，但并非完整兼容当前正式版

- 同一解析对 1.4.0 得到 186 个直接游戏 TypeRef、0 个缺失；原来的 `ItemProto`、
  `FishProto`、`SeedNodeProto`、`ItemSubType` 已迁移到当前 `ItemInfo`、`FishInfo`、
  `SeedNodeInfo` 和字符串/Info 表结构。字段与方法签名也没有当前游戏缺失类型。
- 因而现有证据不支持把 1.4.0 归入 1.3.1/1.3.2 的
  `Assembly.GetTypes -> ReflectionTypeLoadException -> 所有对话卡死` 机制。若一个只加载
  1.4.0 的玩家出现同样症状，必须看该次首个异常，不能用旧版结论代替日志。
- 1.4.0 仍声明面向 0.96.06；发现 1.00.x 后只弹兼容警告并继续初始化全部 26 个 controller。
  当前 24585411 仍有已知 feature-local drift：
  - Fish Analyzer 读取已删除的全局 `TimeArchiveData.weather`；
  - AFK Fishing 读取已删除的 `AgentPhysicalStatus.HorizontalMoveFactor`；
  - NPC Contacts 路径引用已删除的 `DolocPatch.IsNullOrEmpty(string)`；
  - Automatic Tank Collecting 仍声明已不存在的 `FishTankElectricEel.Update` 与
    `UpdateNoRender` Harmony targets；
  - Unlimited Bulk Production 仍声明已不存在的单参数
    `Synthesizer.GetMaxCraftCount(IRecipe)` target。
- 这些缺口只在相应功能初始化、启用或调用时产生局部失败；它们不能解释“DLL 只要存在，
  所有 Yarn 文字第一次显示就卡死”。1.4.0 应定性为部分不兼容，而不是本截图对话组的当前
  首要根因。

### 对新截图十项的 DolocPlus 归因

1. “和角色对话没有出现对话选项”：若普通台词也未显示或随后卡住，1.3.1/1.3.2 可在
   选项前中断 TextAnimator；若普通台词完整显示而仅缺候选项，则 DolocPlus 旧版全局扫描
   仍不足以解释。
2. “和角色对话会导致游戏卡死”：1.3.1 已有相同 native boundary 的运行实证；1.3.2
   保留同组签名级缺失类型，属于高置信同根；1.4.0 当前不能据此定因。
3. “使用睡袋、档案等道具会导致游戏卡死”：这些物品在实际效果前启动 Yarn 并显示
   Aside 文本，故 1.3.1/1.3.2 能用同一个 TextAnimator 失败完整解释；若已成功显示第一条
   文字后才卡住，则另查睡眠/时间/保存分支。
4. “垃圾桶、电话亭等场景对象交互会导致游戏卡死”：两者均先走 `@aside` Yarn 节点，
   1.3.1/1.3.2 同样成立；没有证据指向 1.4.0。
5. “创建新存档后黑屏”：1.3.1 的一个玩家实例已经闭合；1.3.2 具有同一失败条件；1.4.0
   的默认 `CustomTransitionMenu` 只替换开发者传送菜单 `GameInitConfig.EnterTransitionMenu`，
   不是正常 NewGame 开场路径，不能据此归责 1.4.0。
6. “图鉴内图像没有正常解锁”：两个 DolocPlus 版本都没有 patch `CollectionManager`、
   `CollectionBookUiState`、`BookItemData` 或 `BookItemSlot`。SeedUnlock 注入器属于种子解锁
   面板，Fish Analyzer/NPC Info 是独立只读信息 UI，不拥有官方图鉴 `isUnlock`。因此当前
   静态证据不支持把该项归给 DolocPlus；仍需具体 item ID 与 archive record。
7. “未收到旧城市废墟邮件”：DolocPlus 没有命中已审查的 ruinedcity 邮件/任务链 owner；
   保持上文 Issue 5 的独立结论。
8. “箱内物品点击后消失”：DolocPlus 的 `UseAllChests*` 只 patch
   `ArchiveDataHandle.GetAvailableInventories`，用途是远程材料枚举；没有 patch
   `ContainerBaseUiState.PlaceToOtherSide`、`LinearInventory.Take` 或容器槽点击路径。
   没有功能启用与首个异常证据时，不能用 DolocPlus 代替官方半事务分析。
9. “拾取后贴图不消失并可重复拾取”：DolocPlus 没有 patch 本条的 world pickup/drop
   完成路径。自动收集功能会在各自动化 owner 主动产出/收集，但不能从插件存在推导普通
   地面拾取重复。
10. “储物架满时吞掉手持纸箱”：DolocPlus 没有 patch `StorageShelf`、
    `StorageShelfUiState` 或其 buffer/shelf 交换；`UseAllChests*` 的库存发现 patch 也不接管
    该交互。保持官方储物架满槽回滚分支。

### 更正后的调查决策

- 当前最窄而有证据的结论是：DolocPlus 1.3.1 与 1.3.2 可统一解释截图中的对话卡死组
  （Follow-up 2–4），也可解释“普通台词尚未呈现”的 Follow-up 1 和新档开场黑屏；
  1.3.1 已运行确认，1.3.2 是同签名缺失族的高置信静态确认。
- DolocPlus 1.4.0 已消除造成该全局扫描失败的旧类型，不应因为产品名相同就继承旧版归责。
  它仍有五组当前 build 的 feature-local 兼容缺口，调查时必须记录具体功能开关与触发动作。
- ExpandedEncyclopedia 从当前玩家群体调查中排除。图鉴、邮件、箱取物、世界拾取和储物架
  五组继续按各自原生 owner 收证，不再尝试用一个功能性 Mod 包办十项。
- Validation: 完成两个本地 DLL 的版本/hash、当前 24585411 TypeRef/签名级缺失解析、
  Harmony target 静态交集和 1.3.1 支持包日志复核；未启动游戏，未修改安装目录或存档。
