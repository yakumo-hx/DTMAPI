# 原生内容能力与负面案例

本文提炼已读历史研究的能力边界。当前公有 API 状态查 [矩阵](../../api/public-api-matrix.md)，原生领域证据查 [domain index](../../reviews/api/native-owner-domains/INDEX.md)，身份和物理 owner 分类查 [PROJECT](../../../PROJECT.md)。历史结论必须带原始 build 语境，不能当作当前实现。

## 车辆抽象的负面案例

[SecondMotor 退役回顾](../../archive/reviews/api/2026/20260706-0002-second-motor-retirement-retrospective.md) 记录 2026-06-15 玩家发现灯光、农场房间与跨地图纹理污染，随后退役原 IMotorVehicleApi/样例。回顾没有给出确切 native build，故此处仅作为历史失败案例：一个 singleton-oriented 的 native 路径不能凭一次 spawn 或 smoke PASS 推广为通用多车辆 API。

有价值的后续拆分是 riding、collider、harvest/attack、key-item routing、store、asset scope 与 cleanup；重新实施时由当前 vehicle owner 和任务计划决定，不恢复旧包或全局纹理覆盖。

## 23465763 原生领域资料的适用范围

下面九份领域报告全文均声明 `23465763_workshop_38581E`，创建于 2026-06-13。它们仍可作为既有报告/工具引用的原生定位资料；Found/Partial/Blocked 及历史 confidence 数值是当时研究判断，不是今日游戏实测或新的 API 稳定性评级。文中的统一 GameBridge/sidecar 方案属于旧规划；实施时重新应用 PROJECT 的物理 owner 与保存分类。

| 需求 | 可定位原生责任 | 不能据此跨越的边界 | 原报告 |
| --- | --- | --- | --- |
| 世界时间/天气 | TimeArchiveData 持久数据；ArchiveDataHandle 时间推进、渲染/无渲染刷新；Room 有效天气 | Query、当前强制、forecast/history patch 是不同能力；时间跳跃须核对机器/作物/NPC/动物/任务等刷新次序 | [01](../../reviews/api/native-owner-domains/01-world-time-weather-refresh.md) |
| NPC | Npc/NpcManager；NpcController/NodeCanvas schedule；Dialogue/Yarn；StoreManager | NPC 文档 ID/构造器不证明完整新 NPC；schedule、mark point、对话节点、保存重建与商店关系各有 owner | [02](../../reviews/api/native-owner-domains/02-npc-body-behavior.md) |
| 家畜 | AnimalManager、Room/IAnimalHost、AnimalSystem；Animal/AI care；工具/机器 collection | 既有种类实例 release/catch 不等于新物种；produce eligibility 与实际 collection 事务不同；装饰动物另属 scene manager | [03](../../reviews/api/native-owner-domains/03-animal-husbandry-behavior.md) |
| 野鸟 | EnvObjectBird 的 touch/flee/drop；Room env spawn；SeasonInfo.BirdDropSpawnEntry | 野鸟是环境事件；season content drop 数据不等于逐只鸟 runtime 自定义行为 | [04](../../reviews/api/native-owner-domains/04-wild-birds-events-drops.md) |
| 摩托 | MotorController/MotorDataManager 与 DolocAPI.Motor；ItemMotorKey/骑乘/gate | 未发现多 vehicle registry、独立 save slot、key-to-vehicle 映射；skin 不是新车型行为证明 | [06](../../reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md) |
| 地图/地牢 | ArchiveDataHandle/Room/SceneManager；Dungeon、resource/vegetation hosts | mark-point、直接 room、city/farm、dungeon enter/quit 各自证明；ReGenRoomDatas/Bundle loader 不等于 runtime authoring API | [07](../../reviews/api/native-owner-domains/07-maps-dungeons-scenes-resources.md) |
| 堆叠/库存 | ItemInfo.Overlay；Item combine/cost；LinearInventory/InventorySystem | wrapper 返回 false 仍可能已部分放置或邮寄溢出；cursor/buffer 不应假装公共事务状态；未发现 over-cap 自动迁移 owner | [10](../../reviews/api/native-owner-domains/10-item-stack-quantity-limits.md) |
| 地面跟宠 | decorative wander/eat、livestock room/feeding 仅供参考 | AnimalWatchDog、drone follower、motor anchor 都不是已证明的地面玩家跟随生命周期；旧 sidecar 提案未经当前保存契约准入 | [11](../../reviews/api/native-owner-domains/11-original-follow-pet.md) |
| 手持远程武器 | selected item/tool render、missile item archetypes、Battle/Bullet manager 与 damage | drone gun/UI、farming gun、monster bullet 都不证明玩家枪械 owner；animation/cadence/attachments/custom proto 仍需独立设计 | [12](../../reviews/api/native-owner-domains/12-held-ranged-weapons-projectiles.md) |

新研究沿用 [领域报告模板](../../reviews/api/native-owner-domains/REPORT-TEMPLATE.md) 的语义目标、原生责任、精确基线和缺口结构，避免复制原生方法体。全新 mutation 的证据要求由任务改变的风险决定，旧模板不能自动触发所有场景重测。

## June 专项审查的可复用边界

[0008 专项索引](../../archive/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md) 与 [0009 专项索引](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md) 属于 June 历史状态；map 候选多取 `23465763_workshop_38581E` 并比较 `23249387_workshop_247ACD`，部分只搜到候选，没有 method-body 证明。它们不是当前能力表。

- [Machine](../../archive/reviews/api/2026/20260607-0008-native-owner-special-audits/03-machine-production.md)：原生 recipe/tech、电力 Launch、Case 库存与 DTMAPI due-time/fuel/output 调度分别记账。查询到 configured 不能证明产出成功，time skip 也不能证明所有房间完成 catch-up。当前 Mine 的 session-derived 决定查其 focused map，不把旧“必须保存 scheduler”提案复活。
- [CustomEntity](../../archive/reviews/api/2026/20260607-0008-native-owner-special-audits/04-custom-entity-runtime-verbs.md)：当时注册成功仅是 Core dictionary，运行请求明确 blocked；SpawnRequested 在原生对象不存在时也可触发。字段名 Handle/Snapshot/RuntimeInstanceCount 与 HookStatus 不能单独作为对象已经存在的证明。后续官方 CustomAnimals 内容路径与此旧 registry API 分开。
- [Save](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/01-save-apis.md)：当时 review 明示没有读 LocalSave/GameDataPanel 方法正文。槽数量改一个全局 archiveFileCount，不构成独立 Mod 存档命名空间；save-only 与 safe reload 是不同功能，事件发出顺序与 sidecar flush 顺序需要明确。当前保存语义只查 PROJECT 和对应产品 owner。
- [Time](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/02-time-api.md)：旧天气时段 debug skip 调用 PassTimeNoControl/OnWakeUp，不能声称是任意任务调度器或所有子系统的原子结算。按名字选择任意参数数目的 overload 与缺失 wake 后静默继续都是历史失败风险。
- [Teleport](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/03-teleport-api.md)：旧 whitelist DoTransport 调用的成功是 request accepted；立即取得 AfterRequest snapshot 不证明转场完成。mark-point、room-position、dungeon 与完成通知必须按真实 owner 分开验证。

[旧 Input 专项](../../archive/reviews/api/2026/20260607-0008-native-owner-special-audits/01-input-helper-suppress.md) 当时发现 Suppress 仅写无人消费的 HashSet；该缺陷不是对当前输入契约的描述。它说明应追踪“谁实际读状态”而非只看 API 名。旧提案把普通 Suppress 接入 native action 的方向也没有成为当前输入规范的承诺。

[Camera 专项](../../archive/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md) 与 [method-body review](../../archive/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md) 最终读取 `23465763_workshop_38581E` 的 CameraController、背景、fog、Room/Dungeon；当时补偿方案解决当时症状，但大视野与跨房间 panorama 的区别仍可复用。具体写权限已经后续收窄，必须使用当前 Camera map，不按该旧重建清单恢复 controller/背景字段写入。

## 后续纠偏与收窄

[June 12 Camera review](../../archive/reviews/api/2026/20260612-camera-background-native-owner-review.md) 已在 `23465763_workshop_38581E` 将 playable zoom 收窄回 orthographic-size-only，并明确旧 ZOOM-042 compensation 截图不能当新实现证据。背景 preset/layer offset/fog 另有 native owner，没有找到一个可顺手调用的全同步方法。不能把后续重复收窄误记成首次修正。

[June crop review](../../archive/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md) 基于 `23465763_workshop_38581E` 将执行限制在 basin-level CouldHarvest/IsCropMature 与 PlantBasin.Harvest(bool,bool)。Crop.isMature 只是观察；没有可执行成熟目标是成功空操作；scan 返回的 opaque target 执行前仍要重新验证。TreeBasinCrop 的 cocoa 与 PlantBasinGrass/forage 不能借普通盆栽证据宣称可执行。显式请求、batch lock、MaxHarvests 与原生输出比常驻自动扫描更贴合这个 API 的边界。

[June SaveSlots review](../../archive/reviews/api/2026/20260610-saveslots-native-responsibility.md) 的 July 13 补记确认用户长期证据：7–12 创建/保存/重载/重启、复制/删除和无损停启已观察；12/16 可用、18 单页溢出。原正文里的 Unverified 只代表其所列自动证据未覆盖，不能抹去用户证据，也不因此扩展为任意 60 槽支持。native 基线仍为该 review 的 `23465763_workshop_38581E`，当前产品限制归 MoreSaves owner。

[June multi-motor review](../../archive/reviews/api/2026/20260614-0001-multi-custom-motor-native-owner-review.md) 的第八/九槽 PASS 曾证明一次 clone 路线可用和原车锁不被改变，但后续退役证明这不够闭合通用多车与外观。可复用的内容边界是 phone_booth_shop 官方扩展负责销售，而全局 sprite_vehicle_motor 替换不能充当独立实例的私有皮肤；不能据此恢复已退役实现。

[Fishing owner review](../../archive/reviews/api/2026/20260610-fishing-native-responsibility.md) 与 [options review](../../archive/reviews/api/2026/20260610-fishing-options-contract-review.md) 保留 `23465763_workshop_38581E` 的阶段观察/干预区别：Ready/Start/Stop 观察不等于 RollFish、energy、state overwrite 或 delayed success 干预；smoke force-fish 只是让场景可到达。AutoRecast/RequireSelectedFishingRod 当时接受但归一化为 true，SkipMiniGame 是 instant-bite 交接，FastAnimations 可 no-op。产品 UI 文案必须对齐实际 policy；旧 API DTO 字段存在不证明调用者值已被遵守。现在 frozen compatibility 与 ProductNative 分别查 owner。

## Mine 的前提为何不能扩成平台门

[G1 remainder review](../../archive/reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md) 和 [Mine prerequisite](../../archive/reviews/api/2026/20260726-0001-mine-admission-prerequisite-review.md) 将静态 official JSON 与单产品 scheduler/RNG/config/recipe override 分开。后者依据 `24256979_test_7A1907` 查明 native appliance threshold=10，UI 的0–100选项没有 coherent rated-load/charging/deduction 实现；Launch(float) 单点扣电也不满足它。因此先收窄真实产品声明，而非为假选项设计一整套电力 API。IncludeRuntimeModMinerals 同样没有读取者，不需为一个遗留字段新建发现引擎。

其首版选择明确是 session-derived：next due 从当前 native clock 加一个周期重建，保存成果只是 native Case inventory/电量。跨槽残留是清理缺陷，不是“缺少 sidecar”；不创建额外journal、fingerprint或调度保存协议。逐步故障注入属于source/unit，不应逐点变成大型游戏矩阵。review 的旧第三槽hash/isolated fixture前提已由现行 product validation workflow 取代，不能在结构迁移时重新强制全存档隔离。其后是否完成准入/恢复失败重试，查当前 Mine focused map/Update。

## Y 控制台的实体事务与固定语义目录

[2026-08-12 语义目录 Review](../../archive/reviews/manual-qa/2026/20260812-0001-y-console-roadmap-native-owner-review.md) 将实测基线写为 `24650773`，并确认相关 Monster/Animal host、实体、proto、husbandry 源文件与已准入的 `24456188` 字节相同。这是按受影响 owner 复用证据的实例，版本数字变化本身不要求重做整份 Advanced policy。

Monster 使用 `IMonsterHost.GenerateMonster(MonsterProto, Vector2, bool)`；Animal 由 `IAnimalHost` 负责容量、可行走格、`DM_animal`、全局 `AnimalSystem` 和创建/移除。批量操作验证返回实体和 host/global 计数，部分失败通过原生 host 撤销；若撤销失败，该存档会话停止继续批量变更并记录诊断。没有立即存档或产品持久化副本。成年和畜牧进度仍走原生状态，阈值读取 `TbHusbandry`，不复制成产品常量。

目录使用一次维护的精确语义 ID：物品十主类与 Monster/Animal 分开；传送的 UI、执行、CSV 共享精确 `MarkPointId` 注册；七种天气固定位置，仅改高亮。`鹿神池塘` 对应 `林地深处-右端`，不等于 `后山悬崖-奥兰多`。原存档可见的 `dtmapi_creative_generator` 身份保留，名称改成“无限燃料”是表现层变化。闭窗无 Canvas/常驻 UI 订阅、单一游戏 locale、对象池和局部刷新属于此 Review 的设计验收项；是否完成由其 owning Update 记录，不能仅凭 Review 标成已实装。