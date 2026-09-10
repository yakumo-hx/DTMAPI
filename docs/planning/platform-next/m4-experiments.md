# M4 首轮实验输入：保存提交与展示内容

- Lifecycle: accepted-design
- Scope: PN-024 / PN-013 的有界实验说明，承接 [方法验证路线](method-validation.md)；本次仅完成源码/元数据调查和实验设计，下面所有方法对照、故障注入、保存和内容编辑实验均未执行。
- Authority: [D02–D05](../../architecture/platform-data-content.md)、[RT-07](../../architecture/platform-runtime-contracts.md)；状态只在 [status](status.md)，准备工作随 [PN-031.a](../../updates/2026/20260909-0019-platform-release-preparation.md)收尾。不给 Abstractions 增加 SaveData 或 GameContent API。

## 原生输入与证据边界

调查日期 2026-09-10。当前 public build **25163613**，Assembly-CSharp SHA-256 `60489873c645886c5a523fd0d17c4d451a7d68df133f552c6667501245110ac6`。只读输入根为 `references/doloc-town/reverse/builds/25163613_public_604898/`；`full-baseline-inventory/snapshot-source-parity.json` 证明采集时 543 项一致，最终候选游戏证据再次绑定实际 build。下表路径均相对于其 `asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/`，行号用于本次定位，不是未来版本兼容判据。

遵守 [references](../../../references/README.md)：只记录签名、责任、顺序及自己的实验设计，不复制反编译实现到产品。实验启动前重新核对实际游戏 hash 与对应 IL；不匹配则停止该 adapter，按这些入口重做有限差异调查，不照旧行号下注入。

现有 [RuntimeLifecycle](../../hook-map/focused/RuntimeLifecycle.md)只证明加载/世界/退出边界；[API matrix](../../api/public-api-matrix.md)的 SaveSaving/SaveSaved 仍 Experimental。现行 `DolocTownGameBridge.Hooks.cs` 先选择 DolocAPI.SaveGame，缺失才回退 DataPersistenceManager.SaveGame；`DolocTownHookCallbacks.SaveGamePostfix` 依所选外层 bool 发事件。它不是下面的 NativeWriteOutcome，不能直接升级为事务证明。本准备不改这些 Hook/API 行；实验实现时把实际观测补入同一 focused 记录。

<a id="r4a保存身份与提交窗口"></a>

## R4a：先选择保存后端，再验证身份与提交窗口

### 已查实的原生责任

| 精确入口/字段 | 当前源码事实与实验含义 |
| --- | --- |
| `DolocTown/GameData/DataPersistenceManager.cs:91`，`bool SaveGame(int index)` | 顺序为四个数据参与者 BeforeSaveData(ref gameData) → LocalSave.SaveGame → 成功后四个 AfterSaveData(ref gameData) → true。AfterSaveData 循环未捕获异常；低层已落盘后仍可能向外抛异常。dataPersistences 为 city/dungeon/farm/extra，不能想当然把 timeData 当第五个回调。 |
| `DolocTown/GameData/LocalSave.cs:413`，`bool SaveGame(ArchiveDataHandle data, int index)` | try 前 SetArchiveIndex；序列化/可选加密，独占写 temp，关闭 writer/stream；已有目标先 ScrollBackups 再 `File.Replace(temp, full, prev0)`（431），新槽 `File.Move(temp, full)`（436）；随后日志、true。catch 清 temp 后 false。即使低层 false 也要分辨异常是否发生在切换之后。 |
| `LocalSave.cs:391`，`void ScrollBackups(int dataIndex, int backupCount)` | 最终替换前删除/移动备份链；失败可改变备份而未提交主文件。记录 full/temp/prev/bak 各自状态，不能用“主档未变”推导整个目录无变化。 |
| `LocalSave.cs:250`，`string GetDataToStore(object obj)` | 实际序列化文本可能加密。证据应取本次已经关闭的 temp 的准确字节摘要；不要二次序列化对象猜同一候选，baseData 的实时时间会改变。 |
| `DolocAPI.cs:820`，`static bool SaveGame(int index)` | 开始 UI → DataPersistenceManager.SaveGame → 成功/失败 UI → bool。成功 UI 或下层 AfterSaveData 异常不能把已写入数据改判为未提交。 |
| `ArchiveDataHandle.cs:19,164,312,326,368` | 序列化 archiveIndex、各数据块、dataTracker、动态 baseData；新建/反序列化构造及 SetArchiveIndex 未生成独立持久身份。baseDataOnLoad/timeOnLoad 不提供不可变 GUID。 |
| `DolocTown/BaseArchiveData.cs`、`GameData/ExtraArchiveData.cs`、`GameData/AgentArchiveData.cs` | 所查基线字段为版本、槽号、姓名、游戏/现实时间及游戏数据；Extra 的 JsonObject OptIn 当前仅 achievementSystem 被标注序列化。未找到官方通用 ModData 契约；这不足以排除其他已序列化容器，也不能据此直接选择 sidecar。 |
| `GameData/CityArchiveData.cs:17–18`、`DolocTown/DialogueManager.cs:22–23,89,96`、`DolocTown/DialogueVariableStorage.cs:12–16,53–66` | cityData 序列化 dialogueManager，后者序列化 variableStorage；值/类型字典支持 `$` 开头名称的 string。当前 build 保留了既有同档容器候选；它是原生对话变量存储，不是官方通用 ModData 承诺。 |
| `DolocTown/DialogueRunner.cs:83–99` | 已加载变量交给 Yarn 并调用 SetProgram。未知命名空间值能否经过实际初始化、对话和缺 Runtime 的原版重存仍未验证；源码中字段存在不算保留证据。 |
| `LocalSave.cs:461/500`，`bool DuplicateGame(int index, out int targetIndex)` / `bool FixArchiveIndex(int,string,out string)` | 选首个 GetArchiveInfo 为 null 的槽；读源，改根与 baseData 的 archiveIndex，再直接 WriteAllText 目标，不走 SaveGame 的 temp/replace。目标失败可能有部分文件，不能只看 targetIndex。未观察到独立副本 GUID。 |
| `LocalSave.cs:540`，`bool DeleteGame(int index)` | 删已有 bak 后把 full 移到 bak；不是销毁所有历史。上层 DataPersistenceManager/DolocAPI DeleteGame 都是 void，丢弃低层 bool。槽号以后可复用。 |

### 方法对照与最早证伪门

按 [V-Save](method-validation.md#v-save)先比较下面四种方法，不先实现完整 journal/coordinator。既有 [ISSUE-028 内嵌评估](../../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md)和[原审查](../../archive/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md)已提出同档容器；当时因 MoreEquipment 停用后的退物政策暂缓，不能据此认定技术失败，也不能把该产品政策默认扩展为所有作者 SaveData 的要求。

| 方法 | 先验证什么 | 否决或进入下一步的条件 |
| --- | --- | --- |
| A 官方通用扩展 | 限定当前 build 的正式字段/官方资料，确认是否已有无需借用其他领域的存储扩展 | 目前未发现。若找到，先证明有/无 Runtime 的加载保存与复制；不为等待官方新增能力无限阻塞其他实验 |
| B 已序列化的同档容器 | 优先测试上述对话变量中的命名空间字符串，以原生 archive 同时承载游戏与 Mod 值 | **首先证伪缺 Runtime 重存。** 值被初始化/对话清理、类型变化或污染剧情时否决该容器；往返成功后才做完整原生保存和故障矩阵 |
| C 有界序列化适配 | 仅当 A/B 不满足真实需求时，比较增加受控同档 envelope 的接缝、缺 Runtime 后果和维护成本 | 直接插入未知顶层属性会在 typed load/save 后丢失，不能作为方案。需要全局 serializer 替换、缺 Runtime 静默丢数据或无法隔离时不得称为透明默认；失败不自动授权扩大 Hook |
| D 外部 sidecar＋journal | 比较可靠身份、准确提交 witness、复制/回滚/映射丢失及恢复负担；下方完整 sidecar 矩阵保留 | 不允许根据槽号/姓名/mtime 猜继承。若透明恢复需要无法证明的永久身份或提交标记，记录明确限制并回到方法复盘，不先发布接口 |

首个有游戏写入的实验使用一次准备、可复用的 AutoCloud 隔离 disposable fixture，遵守 PROJECT 的允许路径、Runtime 锁、原有资产保全和结束检查；不操作真实玩家档。先以私有 QA 探针将两个 owner 的纯数据字符串写入当前 native holder，不创建玩法物品、不迁移旧产品侧车。走原生保存后彻底移除探针与 Runtime，原版冷加载、触发正常对话并再次保存，再装回探针读取：核对原始载荷、类型、剧情控制组和保存日志。这个最小实验失败就停止 B，不先建设通用服务。

B 通过最小实验后，复用 fixture 验证同进程两个档、owner 停用/缺失、原生复制、删档后同槽新建、current/prev/bak 回退、未知 schema、坏载荷、命名空间碰撞和有界容量；睡眠及另一个原生入口分别验证正常保存与不保存回滚。注入写入前失败、文件切换后通知/外层 UI 抛错及切换前后进程中断，证明 native 和 envelope 的实际提交关系，不依赖 SaveSaved 送达。没有外部提升阶段时不人为创建 sidecar-failure 场景；原生备份变化和未知磁盘结果仍需记录，进程故障不等同断电承诺。

R4a 输出同一作者/玩家旅程的比较：所需原生接缝、独立磁盘 authority、缺 Runtime 后果、备份/复制行为、人工恢复次数、作者需要理解的概念和未满足的承诺。先选后端，再展开 PN-012。作者 API 的 owner/key/schema、会话失效与随原生保存语义可独立设计；永久 SaveIdentity、外部谱系映射和每 owner journal 不先冻结为公共必需概念。同档嵌入 ID 会被原生复制带走，不能因此声称副本已有独立永久身份。

### 结果观察与 sidecar 分支

以下是实验日志字段/状态草案，不是公开枚举或冻结协议。每轮有独立 experimentRun、attempt、fixture 身份和候选 revision；冷恢复不复用进程内对象 ID。NativeWriteOutcome 与 OuterCallOutcome 在所有方法中都分开；独立 durable prepare、participant 提升和下面的 journal/映射矩阵仅在 D 或另一个确实含独立提交域的方案被选中时适用。

| 结果 | 观察点与可报告范围 |
| --- | --- |
| PrepareResult | 旧 SaveSaving 更新 Working 后冻结本次 revision，记录校验/加入原生候选的结果。D 的 required participants 还须写独立 prepared journal；任一 required 失败不得进入 native write。冻结后包括 SaveSaved 在内的新写入属于下一 revision。 |
| NativeWriteOutcome | 使用准确 LocalSave 方法内、temp 关闭之后及 File.Replace/Move 前后的有界观察点，记录 temp digest、old/full digest、切换是否返回与重新打开 full 后 digest。区分未进入、未切换、已切换且匹配、证据不足/歧义。不能用 slot/mtime、文件存在、外层 bool 或 native callback 单独赋值。 |
| OuterCallOutcome | DataPersistenceManager 与 DolocAPI 各自记录返回 true/false/异常/未返回及 AfterSaveData/UI 故障点。保留原异常传播，不吞掉异常来制造成功；与 NativeWriteOutcome 并列。 |
| ParticipantCommitOutcome | D 中只有本次 native 文件强证据匹配才推进 sidecar committed revision，记录部分/待恢复和 journal 保全；禁止自动重放玩法。B 的载荷随同一个原生文件提交，不存在额外磁盘提升时记录不适用，不能补造第二份权威。 |

**以下为 D 的完整实验，不是所有方案的实现前置。** 候选 journal 应包含前一 committed revision、本次 prepared 数据摘要、准确 native temp/full witness、声明的存档谱系与复制来源。先证明 journal 自身失败保全，再做 native 窗口实验；temp witness 必须在切换前持久化失败可阻止切换的边界取得，不能只放在崩溃即丢失的内存日志里。若本次 temp 与旧 full 字节完全相同，冷启动后仅看 full 匹配无法辨别是否执行过切换，必须报歧义/待处理，不能自动推进变了的 sidecar。记录原生对象/epoch、目标路径与 attempt，拒绝跨档/并发混用 witness。正常 StreamWriter 关闭和 File.Replace 成功只证明观察到的文件切换，不等于断电 durable/fsync 承诺。进程强退恢复与断电是不同证据。

**身份判据待证明。** 路径/槽号是位置；完整文件 digest 是该修订内容证据，不是永久身份。两个字节相同的离线副本不能由内容推导独立身份。实验先采用自有外部映射和明确复制事件记录作为候选：受观察的复制必须有独立目标谱系、源 committed revision；删除保留可诊断 tombstone，复用槽不继承旧 sidecar。手动导入/回滚/云覆盖或丢失映射时只可报告未知/冲突并隔离，不能自动套用同槽数据。若不能提供确定的恢复/认领流程，R4a 不通过“任意存档的透明 SaveData”承诺；不得把问题藏在启发式匹配里。

### D 被选中后的可执行实验步骤

1. 创建新的有标记可处置实验根，明确列出允许写入的 SAVE 和 DTMAPI 路径，检查 AutoCloud/profile 隔离。使用原生新建的可处置档或已标记 fixture 的专用副本；不得复制/写回真实玩家存档作例行保护。按产品验证工作流为本次真正保存选择 NativeSaveExpected/对应 mutation 路线，准确参数以 tracked runner 帮助为准。操作共享游戏前持锁；结束核验真实 30 档及既有 sidecar 指纹，恢复 Runtime/非存档资产。
2. 先用独立 host/filesystem fixture 实现两个 required participant A/B 的 prepared/committed journal 与四结果记录。跑成功、A/B prepare 失败、冻结后 Working 改变、重入保存拒绝和 commit 失败；检查候选 revision 一致且旧 committed 不损坏。
3. 加一个私有 QA 原生探针：只匹配本 build 的 LocalSave.SaveGame 方法及两处准确 file-switch 调用，在关闭 temp 后记录字节 witness；before/after 边界若不能可靠匹配就拒绝安装。不得全局替换 System.IO 或改玩家原生 DLL，也不能只观察外层 postfix。先无注入完成一次真实保存与冷加载，核对日志顺序/线程/摘要和原生显示。
4. 正向分别走原生睡眠保存和另一正式保存入口，再做同一进程连续进入两个 fixture 档、不保存返回标题/重进、owner 停用/卸载及未知新 schema 回滚保护；旧句柄失效且 Working 不偷变 Committed。每个故障从独立 fixture 初态启动，按下表一次只注入一个窗口；每轮都冷重启读取真实 full、prepared、committed 与备份，不仅检查内存结果。旧 SaveSaving/SaveSaved 监听者与独立 Control 同时记录，验证次序、失败隔离及 next revision。睡眠/UI 的 fail-closed 只能在相应原生入口实测后成立，不能靠直接调用 SaveGame 的成功推导。
5. 独立执行原生 DuplicateGame、DeleteGame→同槽 NewGame、外部同字节复制、旧 full 导入/备份回滚、映射丢失；核查复制源未被写入、目标不误继承、删除失败/部分目标可诊断、未知谱系阻止写入而不删原数据。
6. 输出四结果全矩阵、准确 native/package hash、允许路径、故障点命中计数、冷恢复结论与保全报告。R4a 评审确认身份/强 witness/失败窗口后才能领取 PN-012；本次准备不算这一步通过。

| 注入窗口 | 预期检查/恢复出口 |
| --- | --- |
| prepare / BeforeSaveData / 序列化或 temp 创建写入失败 | native 未切换，旧 committed 仍可用；新 Working 保留或明确放弃，不伪造 SaveSaved。temp 残留与备份变化单列。 |
| temp 关闭后、备份滚动中、File.Replace/Move 前 | 已有 prepared 但 full 仍旧；恢复应舍弃/隔离未提交候选，不自动重放。backup 链可部分改变。 |
| File.Replace/Move 已返回后，低层日志 / AfterSaveData / 成功 UI 抛错 | full 等于准确候选时 NativeWriteOutcome 已提交，即使 outer false/exception；sidecar 可按 journal 冷完成，不回滚 native 到旧副本。 |
| native 成功、A commit 成功/B commit 失败或 journal 完结失败 | 报部分/待恢复；重启最多推进缺失 participant 一次，不重做 native 保存或 A 的游戏副作用。 |
| 切换之前/之后/sidecar 提交之间强退探针进程 | 冷查强 witness；证据完整则完成/放弃对应候选，证据不足一律隔离。只有专用 fixture 路线可强退；不得把进程故障冒充断电测试。 |
| full 与 old/temp 均不匹配、备份导入或 journal 被截断 | Unknown/Conflict，保留原始 payload 与诊断；不根据最近 mtime/同槽“修复”。必要时该档 SaveData 写能力不可用，要求明确恢复决定。 |
| old 与 temp 同字节但 Mod candidate 不同，切换前后中断 | 冷 full 摘要不能区分两个窗口；缺独立可验证提交标记则 Unknown，不能因为内容匹配便提升 sidecar。 |

## R4b：一项展示表字段与一种静态图标

### 当前原生路径与选定对象

| 入口 | 已查实事实 |
| --- | --- |
| `DolocTown/Config/ModManager.cs:639/250/304` | 官方启用选择按 priority 排序；UpdateCache 按实际顺序形成 CachedConfigs/CachedSprites。`JSONNode LoadWithMods(string file, JSONNode baseJson)` 合并官方选中的 JSON，按 id/key 分组并逐源捕获错误。DTMAPI 不另选一份官方目录或绕过其过滤。 |
| `Config/DolocConfig.cs:44`，`static JSONNode Loader(string file)` | 读取 Addressables Configs/file.json，释放 handle，然后调用官方 LoadWithMods，返回最终 JSON。需要额外表编辑时可验证其返回后、Tables/TbItem 解析前的接缝；方法 A 不为此新增 Hook。官方完成不代表后续本地化已完成。 |
| `Config/Tables.cs:658/1756` 与 `Config/Item/TbItem.cs` | `item_tbitem` 转成 ItemInfo 并建立 map/list，后续 Resolve 和 TranslateText；切语言重新载入文本 provider 并翻译。全局 DolocConfig.Reload 只重建 Tables，不自动重绑所有活 Item。 |
| `Config/Item/ItemInfo.cs:83/167/176/184/233` | JSON `title.key/text`、`description_basic.key/text`，`ui_sprite_asset.url` 转 SpriteAsset。Title/DescriptionBasic 在 TranslateText 改写；Description 还有前缀，不能把渲染字符串与原 JSON 混为一个资产。 |
| `DolocAssetCache.cs:93/111`，`bool CheckAsset<TAsset>(string address)` / `TAsset GetAsset<TAsset>(string address,bool useLog=true)` | TAsset 约束 UnityEngine.Object；Sprite 先询问官方 LoadSpriteFromFile，再 native cache。Get/Check 必须保持一致，且不能仅 Hook 这里便宣称覆盖下行 SpriteAsset 的官方提前返回。 |
| `DolocTown/SpriteAsset.cs:8` 与 `AssetBase.cs:13` | SpriteAsset.Asset getter 自己先走官方覆盖，成功直接返回；否则 base.Asset 缓存首次成功的引用。`TryLoadAsset(string,out Sprite)` 调 DolocAPI.LoadSprite；清其他缓存不必然清这里的引用。 |
| `Config/ModInfo.cs:166/177/212` | UpdateCache 清旧 Sprite 再加载 PNG；Sprite 和 Texture2D 原生创建，ClearCache 显式 Destroy(Sprite)。没有查到这里对对应 Texture2D 的完整释放证明；平台不得销毁借来的原生资源，也不能据此复制释放策略。 |
| `DolocTown/Item.cs:82/92/104/106/272` | 活 Item 持有 private-set proto；title/description/uiSprite 读其 proto。换 Tables 不会自动把这个引用换为新行。 |
| `UI/ItemData.cs:77` → `UI/ItemHoverBox.cs:64` | ItemData 构造时拷贝 item.title/description；RenderAndShow 将 DTO 写入文本控件。旧 DTO、当前 HoverBox、新 DTO 三者必须分开验证。 |
| `UI/ItemNavSlot.cs:48/76` | Render(Item,bool) 取得 item.uiSprite，再 RenderItem 写 base.iconSprite；已挂 Image 持有旧 Sprite，需显式 rerender/recreate 才能证明传播。 |

选定原生数据 `Assets/Configs/GenDatas/item_tbitem.json` 中 **stone**：展示字段 `description_basic.text`（翻译 key `item_stone_desc`），静态图标 URL **icon_item_stone**。只改展示，不触碰 id、数量、价格、功能或存档。展示实验必须记录当前语言及 key→最终文本，另以 `title.text`/source 数组作嵌套隔离负例；不能只更改 fallback 文本就声称所有语言都有效。图像正例用自己制作的可辨认小 PNG，不复制或修改原生美术文件。

### 先比较官方能力、薄适配与受限编辑

按 [V-Content](method-validation.md#v-content)先证明同一展示需求由哪个最小方案满足；[V-Host](method-validation.md#v-host)另负责 Host/Pack 的接受与激活边界。下面 C 的深拷贝候选和事务框架不是 A/B 的默认前置。

| 方法 | 实验范围 | 进入下一种方法的条件 |
| --- | --- | --- |
| A 官方 JSON/PNG 与原有启停语义 | 用合法官方 Mod 的 JSON/自制 PNG 完成上述文本/图标需求，记录真实优先级、语言、坏输入、开关与重开/重启生效条件 | 只有明确作者需求无法由官方原语义满足才扩展；已能完成的静态内容不要求作者另学平台编辑 DSL、申请 Host 或经过第二套排序 |
| B 官方能力上的薄适配 | 复用 A 的加载/选择规则，只补必要的作者校验、诊断、统一打包、命名和已证明的刷新提示；确需运行时接缝时逐项说明其新增价值 | 若只缺错误定位或部署体验，在 B 闭合。只有真实需求要求动态组合、条件或其他官方无法表达的行为，才做 C |
| C 受限候选编辑 | 选定字段/资产与明确消费者，建立候选隔离、失败保全和可证明的传播；不扩展为全表/全 Addressables 框架 | 下方正反矩阵证明需求、隔离和成本后，R4b 才决定保留该 adapter；无增益或传播边界不可靠时回到 A/B 或明确重启要求 |

A/B 共用官方控制组：无覆盖、一个合法覆盖、两个覆盖按真实 priority 组合；输入损坏、禁用/撤回、切语言、重开 UI、回标题和冷重启分别记录源、最终字段、画面和资源身份。先查清官方行为再承诺平台行为，官方逐源容错不自动等于 C 的 last-good/原子回滚；也不为保留测试名称而复制官方引擎。

**Host 不能被官方扫描旁路。** 声明依赖某 Host 的 Pack 即使带有原生可识别 JSON/PNG，也必须在 Host 缺失、禁用、拒绝 schema/版本或激活失败时不产生它声明的受管游戏效果；停止 Host 后重新加载/重启的残留也需检查。对比 Host 在场的正常激活与直接官方扫描结果，确认没有因 `Content` 目录自动发现、旧生成文件或缓存而绕过 Host。若直接复用官方布局无法保持这个边界，先在 V-Host 比较绑定/交付适配，不能先发布格式再补隐藏规则。**原本独立合法的官方内容 Mod 是单独控制组，继续无需 Host。** 发现一个包、标为 loaded 或生成文件均不能代替 Host 接受/实际激活证据。

### C 被选中后的编辑与传播实验

1. 独立 NoNativeSave fixture 上，先无平台编辑，记录官方选中 source 顺序、Loader 最终 JSON 摘要、stone ItemInfo/翻译后字段、老 Item.proto、老 ItemData/可见 HoverBox、老 SpriteAsset/Image 引用身份。准备无官方覆盖、一个官方覆盖、两个官方覆盖/优先级对照，不上传。
2. 制作两份自有作者包 A/B（按稳定顺序 A→B）；平台只消费官方最终结果。每个编辑者取得上一接受结果的**深层独立候选**，包括嵌套 JSON、数组、派生 DTO；不得向编辑者暴露活 ItemInfo 或借来的 Texture2D。A 成功后 B 抛异常，应保留 A，且 official base 和 A 接受快照摘要不变。每轮从原始官方基线重建，禁止重复 invalidate 后叠加旧编辑。
3. 表 positive：对选定展示字段追加各自可辨认标记，验证 A→B；B 改嵌套 title/source 后抛错，验证基线/A/已发布对象均未变。覆盖返回非法类型、删除 id/必需字段、重复 ID、重复/重入 invalidation、编辑中取消 owner，以及作者保留候选引用并在返回后修改。使用解析/字段白名单/发布前再次隔离；不支持的异步编辑准确拒绝。
4. 图标 positive：在 SpriteAsset.Asset **官方最终结果后**构造平台自己拥有的 Texture2D/Sprite 候选，另核查 DolocAssetCache.GetAsset/CheckAsset 的查询一致性，防递归和双编辑。A/B 修改像素、B 抛错、解码失败、非可读/atlas 引用、尺寸越界、取消/撤回分别验证。失败只释放该候选拥有的资源，借来的 native Sprite/Texture 必须存活；成功后仍需 lease/generation 跟踪。
5. 按“新查询、旧 Item、旧 DTO、已挂 HoverBox/Image、重新打开 UI、切语言、官方重载/开关 Mod、回标题、撤回 A/B”逐项查实际显示与引用身份。先只允许已证明的重建路径；如只证明下次打开有效，结果报告为 pending until reopen；需要重新加载/重启就报告准确条件。不得以 invalidated=true 宣称所有活对象即时更新，不全局扫描/强改未知 Item.proto 凑成功。
6. 撤回 B 应从 official base 重放 A；撤回 A/B 回官方结果；关闭 owner 不留下回调、候选、纹理或本平台 lease。失效中的现有消费者未释放前旧 generation 不可销毁；实验统计创建/释放和活引用，不以总数量净零代替逐对象所有权。不能传播的借用原生对象只报告不支持，等待 R4b 决定收窄 adapter。
7. 提交两个作者正反矩阵、官方选择证据、深层引用/摘要对照、语言与可见画面、资源释放、cold restart/reopen 结果，并对照方法 A/B 说明新增能力及成本。仅在此资产/消费者边界通过 R4b 后进入 PN-013 首个受限 adapter；不外推所有表、动画、材质或 Addressables。

### 尚未观测的事实与下一步出口

本次没有实际方法对照、表编辑、Sprite 替换、保存/故障注入或冷恢复实验。仍待证明：同档容器缺 Runtime 重存、官方配置切换的准确重建时点、当前语言翻译覆盖行为、现存 Item/DTO/UI 的有效传播策略、Host 不被官方自动扫描绕过，以及所选方法需要的 SpriteAsset/GetAsset/CheckAsset 一致性、资源寿命或 native 提交/外部映射。源码存在某个 getter/回调不算这些实验 PASS。

PN-024 与 PN-013 先按各自的方法对照开始有界实验；两支互不等待，完整 MSBuild、较低 TFM 依赖和实体控制器都不是前置。先用反例筛掉不合适方法，再展开所选方法的行为/故障矩阵；没有选中 sidecar 或 C 时不实施其通用框架。实验出现既定产品语义不可实现时带准确反例进入 R4a/R4b，调整内部后端或明确能力范围，不能悄悄改写 D02–D05 的长期承诺。新能力尚未公开，以上实验不默认成为 0.7.0 首发阻塞；现有 Mod、保存行为和接入方式的兼容门照常执行，改到它们时才追加相应验收。

