# 正式版新建存档与七项功能异常责任边界审查

- Status: recorded — static responsibility analysis complete; Issue 5 player-save root cause
  confirmed; Issues 1–4 and 6–7 player evidence pending
- Time: 2026-08-08
- Source: 用户提供的七项玩家反馈截图
  C:\Users\ADMINI~1\AppData\Local\Temp\codex-clipboard-ab0558fa-58f0-46bc-abb8-f2a3fa640cb2.png
- Issue 5 source: `D:\下载\DolocTownSave绘空事.zip`，SHA-256
  `9026222023B609FE0115068FDE91C6AED084AE0D728856387AF135007A90E551`
- Scope: 按截图原顺序，对 current public build 24585411 的官方原生路径、
  DTMAPI Runtime 和第一方产品 Hook 做只读根因与责任边界审查
- User constraints: 先提交此前未提交工作，再开始排查；用户明确说明截图第 7 条是
  正式版“新建存档”问题，与此前恢复旧存档后的鸡窝/机器人站重叠黑屏不是同一问题；
  本轮已授权并行子智能体追查，但尚未授权实现修复或写玩家存档
- Related records:
  - 20260808-0001-save-list-null-scene-after-debug-save.md
  - ../../code/2026/20260806-0003-current-test-24585411-compatibility-audit.md
  - 20260806-0001-moreequipment-official-slot-growth-review.md
  - ../../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md
  - ../../../debug/issues/ISSUE-021-20260805-moreequipment-cold-recovery-transaction.md
- Not inspected yet: 除 Issue 5 已取得并解析玩家 archive、随包 Player.log 并完成两次
  AutoCloud 隔离加载外，其余六项仍缺玩家 Player.log、BepInEx LogOutput、DTMAPI
  history、精确启用 Mod 清单、受影响存档和操作录像；因此本记录不会把“安装了
  DTMAPI”当作因果证据
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
  [20260809-0001](../../../updates/2026/20260809-0001-ruined-city-player-save-repair.md)
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
- Ownership: 官方 NewGame、LoadingPanel、教学/开场对话和 scene transition 是黑屏
  直接 owner；DTMAPI 已确认拥有左上标题按钮泄漏。黑屏主因及具体 Mod 间接责任仍等待
  首个异常栈。
- Acceptance checks: 使用与玩家 Steam AutoCloud 隔离的 disposable fixture，在相同
  official public 与相同 Mod 集合新建空槽；记录 NewGame 每个边界、opening dialogue、
  Room.OnEnterRoom 和 LoadingPanel 隐藏。之后需用禁用 Mod 对照，而不是改写玩家存档。
- Blocker conditions: 没有该次新建存档的 Player.log、BepInEx log、DTMAPI history 和
  精确启用 Mod 列表前，无法区分官方初始化、DTMAPI lifecycle、产品 callback 或第三方
  native Hook。

## Cross-Issue Summary

- 这七条至少分成三组，不能合并甩锅或合并修复：
  - Issue 2/3/4/6：原生库存 mutation、鼠标 buffer 与 UI receiver 之间缺少异常隔离/
    回滚的共同模式；“看不见”还不能自动等于对象已从 archive 删除；
  - Issue 1/7：只在官方 Dialogue UI/state 层相交。Issue 7 还包含一个已证实的 DTMAPI
    标题按钮泄漏，但黑屏主因等待首个异常栈；
  - Issue 5：正式流程的 wetland_main 跨日前置与澳柯玛对话，独立于库存问题。不能用
    ruinedcity_continue 判定普通新流程；但本次玩家档确为旧版升级档，正常邮件早已发出，
    且精确命中该迁移信原本要修复的 visited-without-chain 状态。
- 此前旧存档鸡窝/机器人站重叠只属于 20260808-0001 的 cold-load 现场，不进入本记录
  Issue 7 的候选根因。
- 当前静态证据足以反对“1.00.02 改了所有相关代码/配置，所以七项全是同一更新回归”：
  相关 owner 代码和 GenDatas 实际内容未变；仅 level87 与一个无直接消费者的分页方法
  具有当前增量相关性。
- 当前静态证据也不足以发布“DTMAPI 全部无责”：直接 Hook 缺失只能排除直接接管，
  不能排除某个产品或第三方 callback 更早留下非法共享状态，而且 Issue 7 的标题按钮
  泄漏已经是明确 DTMAPI UI 生命周期缺陷。首个异常栈与实际 Harmony owner 清单才是
  其余归责证据。
- 库存共同“先 mutation、后无隔离 callback”的窗口为高置信代码事实；具体是
  InventoryPanel、ExchangeInventory、RecordCollection、获得提示还是 achievement
  首先抛错，目前只有候选置信度，不能写成已闭合根因。应从同次完整 Player.log 的最早
  异常开始看；若 SaveLoaded/AfterLoadData 已先失败，后续库存表现可能只是部分初始化的
  级联症状。
- 最小玩家证据应在复现后立即、退出且不再次启动时由一键收集器导出；每一类问题至少
  需要一份独立支持包，不能用另一个玩家、另一个存档或此前黑屏包代替。

## Evidence Requested Next

- Issue 1/7：失败当次完整 Player.log、Player-prev.log、BepInEx LogOutput、DTMAPI
  latest/history、清晰截图或视频、所建槽位/具体 NPC，以及全部启用 Mod。优先定位
  SaveLoaded.Step 的最后一步、AfterLoadArchiveData 后第一个异常、aside_anim、
  DialogueState、TransitScene 和 LoadingPanel。
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
