# 1.00.00 随身听、载具、动物与模板怪物 Native-Owner 复查

## 记录状态

- 日期：2026-08-02
- 状态：`recorded`
- 性质：只读 API/native-owner、官方内容表、反编译差异与资源结构复查；不是 Runtime、Hook、Content Host 或 Mod 实现
- 当前反向基线：`references/doloc-town/reverse/builds/24456188_test_E861E0`
- 直接对比基线：`references/doloc-town/reverse/builds/24256979_test_7A1907`
- 补充历史基线：`references/doloc-town/reverse/builds/23762374_public_C416D4`
- 官方教学快照：`references/doloc-town/official-workshop-docs/feishu-crawl-20260715`
- Source：用户要求复查随身听是否降低 BGM/音频替换门槛、载具是否仍是单人单载具系统、动物路径变化是否要求重做畜牧接口，以及能否以官方怪物 AI/Unity 资源为模板建立类似畜牧内容路线的怪物接口

相关现行边界：

- `docs/api/public-api-matrix.md`
- `docs/reviews/api/2026/20260713-0004-bgm-native-lifecycle-project-boundary-review.md`
- `docs/reviews/api/2026/20260713-0001-customentity-public-promise-review.md`
- `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
- `docs/reviews/api/native-owner-domains/06-flying-motor-vehicle-types.md`
- `docs/reviews/api/2026/20260717-0006-official-json-monster-capture-production-native-owner-review.md`
- `docs/reviews/code/2026/20260801-0002-current-game-vs-24256979-reverse-baseline-audit.md`

本轮没有安装或启动 DTMAPI/游戏，没有写 Official MODS、Workshop 或存档，也没有取得共享 Runtime lock。官方 player、导出资源和反编译源码仍只位于 Git 忽略的本地 reverse 区；本 Review 只记录 DTMAPI 自己得出的类型名、职责链、差异和边界结论，不分发官方 DLL、反编译方法体或官方资源。

## 先决安全条款

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

本轮已找到四个领域的当前 native owner，但“找到 owner”不等于“可直接稳定公开”。每项仍按 `Platform / SharedNative / ProductNative / ContentOwner` 分类，并要求真实消费者、保存/场景/禁用生命周期和多 Mod 冲突证据。

## 总体裁决

| 问题 | 当前结论 | DTMAPI 后果 |
| --- | --- | --- |
| 1. 随身听与 BGM/音频 | 只降低了选择、编排和切换**已有官方 BGM event** 的内部实现门槛；没有形成自定义 WAV/MP3/OGG/Wwise bank 的官方作者入口，也没有降低短 SFX 与 BGM 的生命周期差异 | 不扩张 `IAudioReplacementApi`，不新增 `BgmMusic` 类别；若有真实产品需求，先做只播放官方曲目的窄 `ProductNative` 试验 |
| 2. 多载具 | 仍是一个玩家、一辆原生摩托、一个保存状态和一个全局运动参数组；多涂装不是多载具 | `IMotorVehicleApi` 继续 Retired；优先验证官方 JSON 新涂装，不建立多载具 GameBridge |
| 3. 动物路径 | 有一处逃跑目标必须留在 `CurrentEnv` 的窄修正，另有 Wwise 注册从 render/unrender 移到 pooled renderer 创建/销毁生命周期；饲养、繁殖、产物、保存、模板 AI/Animator 没有结构漂移 | JSON + PNG + WAV 畜牧路线不重做，只做定向新基线回归；旧 C# CustomEntity/AnimalViewer 接口不扩张 |
| 4. 模板怪物 | 生成原版怪物很简单；新增一个“继承原版模板的新怪物 ID”是可限定的中等复杂度项目；任意 Unity 资源、自定义 AI/攻击仍是高复杂度 | 不复活/扩张 `ICustomMonsterApi`；先做单产品内部模板原型，证明后才讨论可选 ContentOwner |

---

## 问题 1：新增随身听是否放宽了 BGM 或音频替换门槛

### 原始反馈与已确认事实

用户确认新增随身听是可切换播放音乐的物品，并询问官方是否因此降低了 BGM 替换或普通音频替换门槛。本项没有截图或运行日志；判断依据为当前/前一反向基线、官方教学快照和现有 DTMAPI 音频边界。

### 当前公开状态

- `IAudioReplacementApi` 当前为 `Experimental`，公开 C# 路线只准入经审查的短 SFX 事件；当前纸箱事件是唯一公开窄切片。
- `AnimalVoice` 是内部 ContentPack/GameBridge 路线，不是公开 C# API 扩张。
- BGM、循环音频和音乐播放状态仍在公开矩阵中明确超出当前短 SFX 路线。

建议状态不变。随身听不是 `IAudioReplacementApi` 稳定化或扩大 allowlist 的证据。

### 当前构建事实

`24456188_test_E861E0` 相对 `24256979_test_7A1907`：

- `CDManager`、`CDUiState`、`ItemCDPlayer`、`ItemCD`、`BgmPlaylist` 和 `SoundEvents` 逐文件相同；随身听的主要状态机在上一测试基线中已经存在。
- `WwiseSoundManager` 只把 `PLAY_BGM_50`、`PLAY_BGM_51`、`PLAY_BGM_52` 补入 MUSIC LUT；当前 LUT 为 172 行，其中 41 MUSIC、131 SFX，当前 BGM STOP 名称为 `STOP_BGM_FADEOUT` 和 `STOP_BGM_IMMEDIATE`。
- `Stereo` 只增加播放时的音乐音符粒子；没有增加媒体加载或作者注册入口。
- `CDInfo.title` 从本地化对象改为普通字符串；`sound_tbcd` 仍为同样的 32 个 CD ID 和 32 个 BGM event。
- 24256979 的 CD 表已经引用 `PLAY_BGM_50/51/52`，24456188 仍需在代码 LUT 中补齐这些 event。由此可直接排除“只在 JSON 中写一个新 event 名就能创建或加载新音频”的假设。
- 两版的 AK/Wwise managed DLL、`AkSoundEngine.dll`、Addressable SoundBank 和 InitBank 精确相同。Wwise data bundle 增长约 77.3 MB 只证明官方封装媒体变化，不证明新增第三方加载入口。

相对更早的 `23762374_public_C416D4`，真正降低内部调用门槛的是正式版前的音乐状态机重构：

- `WwiseSoundManager.SetCustomPlaylist`
- `WwiseSoundManager.SetLoopBgm`
- `WwiseSoundManager.ClearCustomPlaylist`
- `WwiseSoundManager.PauseBgm`
- `WwiseSoundManager.TryRefreshPlayState`
- `WwiseSoundManager.OnBgmEventPost`
- `BgmPlaylist`
- `CDManager`

`CDManager` 持有启用状态、解锁/已查看 CD、播放列表、播放模式和最近曲目，并响应背包、Stereo 和房间变化。`WwiseSoundManager` 仍持有实际 BGM 播放状态、EndOfEvent callback、STOP、下一首和环境恢复。

### 官方作者入口与事实边界

通用官方 JSON loader 会按 `Tables` 的文件映射命中 `sound_tbcd`、`sound_tbbgmgroup` 等表；有 `id` 的表在技术上可追加或覆盖。因此存在两条**未文档化**的内部数据路径：

1. 添加一张指向已有官方 BGM event 的 CD；
2. 重排或覆盖使用已有官方 event 的播放组。

但官方教学快照中没有 CD、随身听、BGM、Audio、Wwise 或 SoundBank 作者章节，也没有 `ItemFunctionCD`、`ItemFunctionCDPlayer`、`EquipmentFuncStereo` 或 `sound_tbcd` 示例。料理教学中的 `sound_event` 反而标注为“音效事件 ID，不动”。官方 Mod 资源缓存当前只发现 JSON/PNG 路线，没有 WAV、MP3、OGG、BNK、WEM 或任意音频 AssetBundle 作者入口。

所以必须区分：

- 通用 loader **技术上能命中内部表**；
- 官方**没有承诺这是稳定作者契约**。

### Native owner 与物理归属

| 责任 | Native owner / 状态持有者 | 物理归属判断 |
| --- | --- | --- |
| 已有官方曲目的播放列表、单曲循环、环境恢复 | `CDManager`、`BgmPlaylist`、`WwiseSoundManager.SetCustomPlaylist/SetLoopBgm/ClearCustomPlaylist` | 单一首发功能先为 `ProductNative`；出现两个真实独立消费者争用全局音乐状态后，才审 `SharedNative` lease/arbitration |
| CD 定义、道具、曲目标题、指向已有 event | `TbCD`、`CDInfo`、`ItemFunctionCD`、官方 JSON loader | 候选 `ContentOwner`，但当前未文档化且有可移除内容保存风险 |
| 自定义 BGM 媒体、bank、event、callback 和 unload | `WwiseSoundManager`、Ak/Wwise bank/media/runtime state | 尚无安全作者 owner；独立 ProductNative QA 研究，不能提前进入 mandatory GameBridge |
| 短 SFX WAV 替换 | 当前 `AudioReplacementService` 与经审查 event owner | 维持现状；不因随身听改变 |

### 为什么“自定义歌曲”仍然不简单

当前 CD/BGM 数据只保存 Wwise event 字符串，没有媒体文件路径。`PlayBGMInternal` 使用非空 EndOfEvent callback，而当前短 SFX 桥遇到 emitter/callback 语义会 fail open。随身听没有解决：

- playing ID、自然结束 callback 恰好一次；
- fade STOP / immediate STOP、loop、pause/resume；
- `VOLUME_MUSIC`、RTPC、state、switch、bus；
- bank/media 版本、加载、卸载和 owner 清理；
- 标题、读档、房间、天气、Stereo 与环境 BGM 恢复；
- 两个 Mod 对同一全局音乐状态的仲裁。

`WwiseSoundManager.LoadSoundBank(string)` 也只解析游戏自己的固定 Addressables bank 路径，不是任意 Mod 文件加载器。

另有保存风险：`CDManager` 属于 `CityArchiveData`。反序列化时会过滤当前 `TbCD` 不存在的 unlocked/viewed/playlist ID。第三方 CD 内容临时禁用后再保存，可能永久丢失该 CD 的解锁、查看和播放列表状态；原生实现显然假设 CD 表是内置且不可移除的。

### 裁决、被排除的假设与下一步

最终裁决：**随身听让“复用官方音乐播放器播放已有官方曲目”从困难降到中等偏易；没有让“接入自己的音乐文件”变简单，也没有降低普通短音效替换的生命周期门槛。**

被排除或尚未证明：

- 排除：JSON 中自造 event 等于新增音频。
- 排除：随身听状态机可以直接复用为本地 WAV/MP3 播放器。
- 未证明：新增 CD、覆盖 BGM group 是跨版本稳定官方契约。
- 未证明：内容包禁用/恢复能保留 CD 孤儿状态。

若存在真实第一方需求，可建立不进入 public API 的 `VanillaMusicSession` 试验：只接受当前 `TbCD`/MUSIC LUT 中已有曲目，提供 owner lease、sequence/random/single，不接收文件路径、任意 Wwise event、bank 或 raw Unity/Wwise 类型，初期也不写 `CDManager` 持久状态。

最低验收包括：自然结束 callback、两种 STOP、官方随身听开关、背包增删、Stereo 创建/移除、房间和环境恢复、音量/静音/暂停、标题/读档、owner disable/re-enable、两个 owner 仲裁、无效 event fail-open，以及 CD 内容禁用/恢复的孤儿状态保留。若实现发生，应新建独立 Update，并只在真实契约变化后更新公开矩阵、音频 Debug/Hook 和 smoke 记录。

---

## 问题 2：载具是否仍只服务一个玩家，是否仍不能简单扩展为多载具

### 原始反馈与已确认事实

用户询问官方载具系统是否仍然是“只服务于个人的系统”，以及是否仍不存在简单扩展为多载具系统的可能。本项没有截图或运行日志；判断依据为两版代码、配置与导出资源的定向比较。

### 当前公开状态

`IMotorVehicleApi` 当前为 Retired。建议状态不变，不复活旧接口，也不把原生摩托状态查询包装成“多载具 API”。

### 当前构建事实

`24456188_test_E861E0` 相对 `24256979_test_7A1907`：

- `MotorController`、`MotorDataManager`、`MotorInteractable`、`MotorRenderer`、`MotorLight`、`MotorDriverRenderer`、`ManagerGate`、`ItemVehicleSkin`、`TbVehicleSkin`、`VehicleSkinInfo`、`MotorBar` 和相关地图/UI 路径均无载具语义变化。
- `ItemMotorKey` 只删除一条“摩托未解锁”调试日志；召唤和解锁逻辑未变。
- `AgentControllerState` 的变化属于无人机脱战，不改变骑乘方法和单个 controller 字段。
- `game_entity_motor.prefab`、`motor_light.prefab` 与摩托驾驶 Animator 在消除 AssetRipper GUID 漂移后内容一致。
- `player_tbvehicleskin`、全局摩托参数、钥匙和两个原生涂装物品均精确相同。
- 官方 `ModManager` 的新兼容/合并逻辑没有新增车辆表、车辆 ID、车辆工厂或车辆扩展入口。

### Native owner 与单例证据

| 责任 | 当前 owner / 状态持有者 | 单数约束 |
| --- | --- | --- |
| 全局玩家摩托 | `DolocAPI.Motor` | 只有一个静态 `MotorController` |
| 上下车、摄像机、无人机跟随、怪物目标切换 | `AgentControllerState.motorController` | 只有一个 controller 字段 |
| 解锁、房间/地牢、位置、当前皮肤 | `MotorDataManager` | 保存一份状态，没有 vehicle ID 或集合 |
| 钥匙 | `ItemFunctionMotorKey` / `ItemMotorKey` | 钥匙定义没有 `vehicle_id` |
| 地图与装备 UI | 单个 `motorPointer`、单个摩托栏和预览 | 无复数 UI 语义 |
| 速度、重力、耐力、碰撞反弹 | 全局参数 | 不属于某个载具定义 |
| 实体资源 | 一个玩家 `game_entity_motor.prefab` | 公交、NPC 汽车、飞机怪物不属于通用玩家车辆系统 |

原生系统的真实模型仍是：

```text
一个玩家
  -> 一个 DolocAPI.Motor / MotorController
  -> 一份 MotorDataManager 保存状态
  -> 一组全局运动参数
  -> 当前选择的一个 VehicleSkin
```

### 能力分层与物理归属

| 目标 | 难度/结论 | 归属 |
| --- | --- | --- |
| 给同一辆原生摩托增加涂装 | 较简单，已有 `TbVehicleSkin`、皮肤物品、当前皮肤保存和渲染路径 | `ContentOwner`，优先官方 JSON |
| 多个涂装定义、任一时刻选择一个 | 可行，但仍是“一辆摩托换皮” | `ContentOwner` |
| 独立的已拥有皮肤收藏/车库 UI | 中等，原生无完整收藏集合 | 具体产品的 `ProductNative` + 内容定义 |
| 一次只激活一辆，但不同配置改变速度/碰撞 | 中高，全局参数需要独占租约、恢复和冲突处理 | 单一产品 `ProductNative` |
| 多辆分别保存位置、同场存在 | 高风险，无 registry、实例工厂、保存集合或复数 UI | 新 `ProductNative` 状态机，不是现有系统扩展 |
| 不同运动形式、攻击和碰撞箱的多种载具 | 高风险独立项目 | `ProductNative`；当前无 SharedNative 证据 |

历史 SecondMotor 的灯光、农场贴图和跨地图污染与这些单例耦合一致，不能把曾经“出现第二个对象”当成多载具 owner 已存在。

### 裁决、被排除的假设与下一步

最终裁决：**是，当前官方载具仍是服务一个玩家的一辆摩托系统；仍不存在把它简单扩展为多个独立载具的原生入口。** 多涂装只是最低风险外观层，不能作为多载具系统的证据。

被排除或尚未证明：

- 排除：`TbVehicleSkin` 是车辆 registry。
- 排除：公交/NPC 汽车/飞机怪物证明存在通用玩家载具基类。
- 未证明：多个配置可以安全共享全局速度、碰撞、摄像机和保存 owner。
- 未证明：多实例跨房间、返回标题、禁用/卸载可无孤儿恢复。

最值得先做的不是载具 API，而是一次官方内容实验：新增唯一 `VehicleSkinInfo`、对应皮肤物品、独立 PNG/灯光遮罩/锚点，并通过商店发放，再验证保存重载、来源禁用后的默认皮肤回退、ID 冲突、灯光/驾驶员偏移和标题清理。实验若成功，应进入官方内容作者文档；它仍不会重新开放 `IMotorVehicleApi`。

未来只有两个独立真实产品共同争用同一全局参数或生命周期 owner 时，才重新审查窄 `SharedNative` 仲裁。多实例项目另需钥匙映射、每车保存、正常保存/no-save 回滚、召唤停车、上车、跨房间、摄像机、无人机、怪物目标、地图指针、灯光材质、禁用/卸载孤儿恢复和多 Mod 仲裁的完整设计。

---

## 问题 3：动物路径是否更新，DTMAPI 畜牧接口是否需要重做

### 原始反馈与已确认事实

用户询问最终测试基线是否修改动物路径，以及 DTMAPI 当前 JSON + PNG + WAV 畜牧能力是否需要重新设计。本项没有截图或新手测日志；既有用户确认的稳定事实仍是自定义畜牧动物内容路线可正常添加、成长、繁殖和产出。

### 当前公开状态

需要分开三条身份：

- 实际工作的 JSON + PNG + WAV 自定义畜牧路线是内部内容宿主能力，长期目标是可选 `ContentOwner`，不是旧 C# CustomEntity 接口。
- `IAnimalViewerApi` 是 `Experimental / Deprecated / Frozen` 兼容壳，产品实现已经归 AnimalHusbandryProgress 的 `ProductNative`。
- `ICustomAnimalApi` 是 `Experimental / Frozen` registry compatibility，native runtime creation 继续返回 `runtime-creation-blocked`。

三者状态均不因本次官方变化而提升或扩张。

### 当前构建差异

24256979 到 24456188 只有四个动物代码文件发生变化：

| 文件 | 变化 | 影响 |
| --- | --- | --- |
| `Animal` | 将 Wwise GameObject 注册/注销移出 `OnRender` / `OnUnRender` | 声音对象生命周期迁移，不是畜牧状态或路径重构 |
| `AnimalRenderer` | 新增 `OnCreated()` 与 private `OnDestroy()` 承接 Wwise 注册/注销 | 注册跟随 pooled renderer 对象寿命，而不是每次显隐 |
| `AnimalRoomEnv` | 构造函数不再立即拒绝空 `Room` | 更像加载/切房暂态容错，没有新保存模型证据 |
| `AnimalUtils.TryGetFleePosition` | 候选逃跑点除房间几何和水平可达外，还必须属于 `animal.CurrentEnv` | 防止受击逃跑越过围栏/分区，是唯一实质路径变化 |

现有方法没有移除、参数改变或既有 Hook 目标签名改变。CustomAnimals 当前依赖的 `Animal.OnRender`、`DEBUG_SetAdult`、`Sleep`、`WakeUp`、`CallToRoom`，`AnimalRenderer.OnRecycle`、`OnFell`、`PlayAnimation`、`FixedUpdate`，以及 `AnimalAI`、`AnimalController`、`AnimatorAsset`、`SpriteOverrideHandler` 目标仍可静态解析。

额外核对结果：

- 31 个 AnimalAI/Controller/Path/Task/Work/Move/Jump 文件逐文件相同。
- 9 个动物 Animator controller 在消除 AssetRipper GUID 漂移后相同，55 个 `.anim` 相同；相关资源路径 365 -> 365，无新增、删除或改名。
- 六张 `animal_tb*` 表的内容、行数和 SHA-256 全部相同，行数分别为 4、4、4、35、4、32。
- `AnimalData`、`AnimalInfo`、`AnimalDocumentInfo`、`AnimalHusbandryData`、`AnimalManager`、`AnimalSystem`、`IAnimalHost`、`ItemAnimalPackage`、Feeder、Toilet、LivestockNursery 及四类产物设备均相同。
- 通用 `ModManager` 虽有变化，但版本兜底只覆盖帽子、配方、鱼塘鱼和种子；表合并从硬编码 `id` 改为表自身 `keyName`，而六张动物表仍以 `id` 为键，没有动物 schema 迁移迹象。
- AnimalHusbandryProgress 的 `AnimalFullInfoData`、`AnimalViewer`、`AnimalPanelUiState` 三个 ProductNative 目标文件也相同。

### Native owner 与影响判断

| 责任 | Native owner / 状态持有者 | 本次影响 |
| --- | --- | --- |
| 受击逃跑目标 | `AnimalUtils.TryGetFleePosition`、`Animal.CurrentEnv`、房间几何 | 新增 CurrentEnv 限制；只需兼容性验证 |
| 普通寻路、任务和 AI | `AnimalAI`、`AnimalController`、Path/Task/Work | 无静态漂移 |
| renderer 与 Wwise 对象注册 | `Animal.OnRender/OnUnRender`、`AnimalRenderer.OnCreated/OnDestroy` | owner 从显隐周期移到池对象寿命；需要核对复用和清理 |
| 喂食、排泄、繁殖、产物 | Animal/Husbandry 数据、Feeder/Toilet/Nursery/产物设备 | 无代码或配置漂移 |
| 动物包创建、房间、保存 | `AnimalManager`、`AnimalSystem`、`IAnimalHost`、`ItemAnimalPackage`、`AnimalData` | 无保存结构漂移 |
| 自定义物种数据/图像/声音 | 内容包 + 当前内部 CustomAnimals/AnimalVoice 桥 + 原生模拟 | 内容归 `ContentOwner`；无需因本次更新重写 schema |

### 裁决、被排除的假设与下一步

最终裁决：**动物路径有更新，但只是逃跑目标的环境边界修正；DTMAPI 畜牧内容路线不需要全量重做。** 需要的是小范围适配/复核，而不是重新发明动物运行时。

被排除或尚未证明：

- 排除：官方重写了动物寻路、AI、繁殖、产物或保存系统。
- 排除：Animator/state/clip 漂移要求重写 PNG 命名桥。
- 排除：本次变化让旧 `ICustomAnimalApi` runtime creation 可以解封。
- 未证明：新 pooled renderer/Wwise 生命周期下，跨物种复用和长时间注册计数完全无泄漏。

新基线最低定向回归应覆盖：

```text
official JSON reload
-> 动物包释放
-> child/adult PNG
-> 进食、睡眠/醒来
-> 围栏/畜棚分区/房间边缘受击逃跑且不越出 CurrentEnv
-> 普通和隐藏产物
-> 正常保存/重载
-> 返回标题
-> renderer 被另一物种复用
-> AnimalVoice 与原版动物隔离
-> DTMAPI 资源/声音上下文清零
```

长时间检查应确认 Wwise 注册对象数量稳定在 renderer 池容量附近，而不是随 render/unrender 持续增长。未来将 CustomAnimals 从过重 mandatory GameBridge 迁到可选 `DTMAPI.CustomAnimals` ContentOwner 仍是合理结构工作，但原因是 DTMAPI 自身所有权和重量，不是本次动物 native 漂移。

---

## 问题 4：基于官方怪物 AI 与 Unity 资源模板，类似畜牧路线的怪物接口是否简单

### 原始反馈与已确认事实

用户设想复用某些官方怪物的 AI 和 Unity 资源作为模板，建立类似现有畜牧内容路线的怪物接口，并询问实现是否简单。本项没有截图或运行日志；目标按“仅生成原版怪物”“新 ID 精确继承模板”“自定义视觉”“自定义 AI/攻击”四档分析，避免把复用 prefab 误写成完整怪物 API。

### 当前公开状态

- `ICustomMonsterApi` 当前为 `Experimental / Frozen` registry compatibility。
- native `RequestSpawn` 继续为 `runtime-creation-blocked`。
- 旧接口同时承诺定义、状态、移动、目标、攻击、provider callback、持久化和 runtime snapshot，范围远大于本次“模板怪物”目标。

建议状态不变。不得在旧 `ICustomMonsterApi` 上继续堆字段或把一个模板原型写成其 runtime implementation；未来模板路线应使用新的窄声明模型。

### 当前构建与官方内容事实

24456188 没有新增怪物身份：

- `monster_tbmonster` 仍为 26 行；只修改 6 个现有怪物的血量/体积数值。
- `monster_tbmonsterspawn` 从 51 增至 52，新增 `ruined_city_industrial_zone_1`，但该生成组只复用已有 `bastion` 和 `bird`。
- `MonsterController`、`MonsterManager`、`MonsterEnv`、`IMonsterHost`、`MonsterSO`、`MonsterProto`、`MonsterDatabase`、`DolocBundleManager.LoadMonsters`、`GameEntitySystem` 和 `GameEntityManager` 等创建/运行/回收主链相对上一基线精确相同。

本次确有怪物 AI/攻击生命周期变化：

- `IMonsterSight` 新增 fixed-update 责任，`MonsterAI.OnFixedUpdate` 开始调用它；`MonsterSight`/`MonsterSightEx` 的视野 tick 因此迁入 AI fixed update。
- 辐射史莱姆与红绿灯 AI 调整状态/目标规则。
- 冲撞、暗影冲击等攻击行为补充效果回收或伤害框启停。
- 个别 decorator/mover 增加重置。

这些变化不阻断“使用当前模板”的研究，但证明 raw `MonsterAI` 子类、attack behaviour、sight 和 pooled reset 都是版本敏感 native contract，不应直接暴露给普通 Mod。

官方教学目前只公开怪物击败掉落库/保底掉落等内容扩展，没有公开新 `MonsterInfo`、`MonsterSO`、prefab、AI 或 spawn-table 怪物创作流程。`MonsterInfo` 中虽然存在 `prefab` 字段，但当前 managed live-spawn 主链并不使用它作为任意 prefab 工厂；只添加 JSON 行不足以创建新运行时怪物。

### Native owner 主链

```text
MonsterSO (mover + attack behaviour assets)
  -> MonsterProto
  -> DolocBundleManager.LoadMonsters / MonsterDatabase
  -> IMonsterHost.GenerateMonster(s)
  -> GameEntitySystem.Next<MonsterController>(MonsterProto.Name)
  -> MonsterController.Run
     -> MonsterAI.Create(MonsterProto.Name)
     -> mover / attack behaviours / decorator / sound / battle registration
  -> IMonsterHost.Remove/Hide/Clear
  -> GameEntitySystem.Recycle(controller, MonsterProto.Name)
  -> MonsterController.OnRecycle cleanup
```

关键约束：

1. `MonsterSO` 是 Odin `SerializedScriptableObject`，mover 与攻击原型不是普通作者 JSON schema。
2. `MonsterController` 通过 26 个固定 `GameEntityManager(... Alias = monster_id)` 绑定官方 prefab 池；新 ID 没有对应池 alias。
3. 生成和回收都使用 `MonsterProto.Name` 选择同一 alias；只在生成时重定向会造成错误回收或泄漏。
4. `MonsterAI.Create(name)` 通过 `MonsterAIAttribute` 的静态 name -> Type 映射选择 AI；未知新 ID 会落到 `MonsterAI_Default`，不会自动继承模板 AI。
5. `MonsterProto` 用同一个 `Name` 动态查 `TbMonster` 统计/掉落，又用于池和 AI 路由。要保留“新内容 ID + 模板池/模板 AI”，桥必须明确拆开这三个身份。
6. controller 的运行/回收还拥有 mover、attack behaviour、decorator、Wwise、情绪、BattleSystem 和环境注册/清理；复用 prefab 不等于这些生命周期自动安全。

因此，一个真正的新模板怪物至少需要：

- 新 `TbMonster` 内容行；
- 复用/克隆模板 `MonsterProto` 的 mover 和 attack prototypes；
- `custom monster id -> template prefab/pool alias` 的生成与回收双向路由；
- `custom monster id -> template AI` 映射；
- spawn table/房间生成整合；
- owner 禁用、刷新、房间退出和池清理规则。

### 难度分层

| 档位 | 目标 | 判断 |
| --- | --- | --- |
| M0 | 查询或临时生成一个已有官方怪物 | 相对简单，native owner 已找到；适合 DebugConsole/QA，不足以证明自定义怪物接口 |
| M1 | 新 ID 精确继承一个官方 prefab、AI、mover、attack，只覆盖统计、掉落和生成位置 | **中等复杂度、可做限定原型，但不是纯 JSON，也不是简单稳定 API**；主要工作是 proto、pool alias、AI 路由和 cleanup |
| M2 | M1 加自定义 PNG/动画/声音 | 中高；官方怪物 prefab 的 Animator、Renderer、Collider、Decorator、Sight、AttackBehaviourRenderer 相互耦合，需要单独资产/动画/声音层 |
| M3 | 自定义 AI、攻击、弹幕、decorator、任意 Unity prefab 或持久怪物状态 | 高复杂度 Advanced ProductNative 项目，不能称为“类畜牧简单接口” |

怪物相对畜牧动物有一个较简单之处：普通怪物通常跟随房间/地牢生成和对象池生命周期，没有动物的喂食、排泄、繁殖、产物、home room、睡眠和个体饲养保存链。但它同时有更紧的 prefab pool、AI name、攻击、碰撞、战斗、死亡/掉落、任务/收集和房间再生成耦合，所以总判断仍不是“简单”。

### 物理归属与推荐最小原型

- 只有一个首发怪物产品时，模板适配器和专用规则属于该产品的 `ProductNative`；不能因“未来也许复用”放进 mandatory GameBridge。
- 出现至少两个独立真实内容包，并证明它们共享同一稳定模板 schema、native owner 和冲突/清理规则后，才评估可选 `ContentOwner` / Content Host。
- 当前 G7 Content Host 仍未准入，所以本 Review 只允许后续独立 admission/原型设计，不授权实现或发布。
- 只有两个真实 CodeMod 消费者共同需要相同运行时查询/命令时，才另审窄 `SharedNative`；内容引擎本身不因内部 provider/facade 自动成为 SharedNative。

推荐 M1 声明只包含：

```text
monster_id
template_monster_id
有限的 health / defense / hurt / exp / drop 覆盖
有限的 spawn placement
```

第一版明确不包含任意 C# behavior provider、自定义 AI/攻击/弹幕、任意 prefab、持久实例、raw Unity/Harmony/Wwise 类型。应先在单一内部 QA/ProductNative 原型中证明“新 ID、模板 AI、模板池、双向回收、掉落和房间重生”，再决定是否存在值得稳定化的内容 schema。

### 裁决、被排除的假设与最低验收

最终裁决：**以一个官方怪物为完整模板、只改数值/掉落/生成的窄怪物内容路线是可行研究方向，难度低于任意自定义怪物，但仍是中等复杂度，不是简单复制 JSON/Unity 资源即可完成。自定义 AI 或任意资源则仍是大型项目。**

被排除或尚未证明：

- 排除：`monster_tbmonster.prefab` 字段本身就是运行时 prefab registry。
- 排除：新 ID 会自动使用模板 AI；未知 ID 当前会回落默认 AI。
- 排除：只修生成 alias、不修回收 alias 可以安全运行。
- 排除：官方新增一条 spawn row 等于开放新怪物作者 API；本次只复用了已有怪物。
- 未证明：自定义动画、声音、攻击和 decorator 可形成跨模板统一 schema。
- 未证明：禁用/刷新、掉落、任务/收集、房间再生成和长时间对象池均无孤儿/重复。

M1 最低验收应包括：唯一 ID 和冲突拒绝、模板 proto/AI/prefab 精确选择、地面/空中/表面生成限制、同场多只、受击/追踪/攻击/死亡、普通与保底掉落、经验/任务/收集边界、房间退出/重入与地牢再生成、对象池重复 reuse/recycle、返回标题、owner disable/re-enable、内容移除 fail-closed、两个包 ID 冲突、无残留 controller/Wwise/BattleSystem 注册和稳定退出。若实现发生，应新建独立 Review/Update，并在真实 runtime 证据形成后才更新 CustomEntity 文档、API matrix、Hook/Debug 和 smoke 记录。

---

## 推荐任务顺序

1. **动物只做兼容复核，不重构。** 用现有权威自定义动物 fixture 跑一轮 `CurrentEnv` 逃跑 + pooled renderer/AnimalVoice + 正常保存/重载的最小新基线回归。
2. **载具先做官方 JSON 涂装实验。** 若通过，把能力写入作者文档；继续冻结多载具项目。
3. **有真实听歌产品需求时，再做官方曲目 session 原型。** 只复用 `SetCustomPlaylist/SetLoopBgm`，不碰自定义媒体，也不扩 `IAudioReplacementApi`。
4. **怪物先做 M1 单模板内部原型设计。** 在 ProductNative/QA 范围证明新 ID、模板池、模板 AI、回收和掉落；没有 runtime 证据前不设计公开接口。
5. **自定义 BGM bank 和 M2/M3 怪物分别作为未来独立项目。** 二者都不能借本轮较窄成功提前进入 mandatory Runtime 或通用 public API。

这个顺序把最低风险、最接近官方内容的验证放在前面，也避免为尚无真实消费者的多载具、自定义音频和任意怪物系统增加 DTMAPI 本体重量。

## 证据与验证

本轮静态检查包括：

- 比较 24256979 与 24456188 的相关反编译文件 SHA-256、方法声明和定向文本差异；
- 比较 CD/BGM、车辆皮肤、六张动物表和怪物/怪物生成 JSON 的行数、ID 与内容；
- 对动物 Animator controller、`.anim` 和相关资源路径做 GUID 噪声归一化后比较；
- 检查当前 `CDManager` / `WwiseSoundManager`、Motor owner、Animal/AnimalRenderer/AnimalUtils、MonsterSO/MonsterProto/MonsterDatabase/IMonsterHost/MonsterController/MonsterAI/GameEntitySystem 主链；
- 检查本地官方教学快照是否存在 CD/BGM/audio、车辆、动物或新怪物作者入口；
- 对照现行 public API matrix、CustomEntity Frozen 边界、BGM Review、动物/载具 native-owner 域报告和当前基线总审计。

没有运行构建或游戏 smoke，因为本次只新增 Review，没有修改 Runtime、Hook、配置、API、Mod 包、官方内容或测试脚本。没有更新 public API matrix：四项状态均维持现行结论；没有更新 Debug、Hook 或 smoke 记录：本轮没有产生运行时证据。后续任何原型/实现必须拥有自己的 Update；发生新的根因问题时再建立任务专属 Review。
