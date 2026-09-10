# 1.00.03 正式版 public build 24650773 基线差异审查

## Metadata

- Review ID: `20260811-0001`
- Date: `2026-08-11`
- Status: `recorded`
- Scope: 最新正式版 `public` payload 的完整捕获、三层保留基线比较、Unity 导出噪声归一化、真实消费者与兼容策略审查。
- Source: 用户要求“解包最新版正式版 public payload，审查实际与当前基线实际差异”。
- Owning Update: [20260810-0001：正式版 public 1.00.03 全量捕获与基线差异审查](../../../updates/2026/20260810-0001-public-10003-full-reverse-capture-and-diff.md)

## 一、裁决

`24650773_public_76C24E / 1.00.03` 是当前工作空间第一份身份一致、完整冻结并独立复核的 1.0 正式版 `public` 捕获。它可以作为后续原生责任、资源和兼容研究的**最新 public reverse 观察头**，但本次不把它自动改写成 Author SDK exact policy、产品重签依据或玩家行为验收。

相对最近完整保留的 `24585411_test_68AEA1 / 1.00.02`：

- 没有新增或删除场景身份、托管程序集、配置表或 Addressables 资源地址；
- `Assembly-CSharp.dll` 只在 6 个同路径反编译文件中出现方法体或私有序列化字段变化，没有类型文件增删，也没有公开方法签名变化；
- 195 张 GenDatas 中 5 张改变，实际内容是 3 个变异鱼烘干配方、英语文本修正和制作人员名单；
- 既有剧情脚本增加一条防重复对话移除命令，日/葡/俄三种剧情翻译做批量修订；
- 既有河谷地牢、镇长办公室、通用场景背景 prefab、名称输入框和启动画面有小型修订；
- 12 个音频/Wwise raw 路径逐字不变，没有新增音乐或音频 payload；
- 没有新增 Steam 成就定义；`1.00.03` 只增加对既有春节剧情和亚种鱼成就事件的存档补发。

生产 Runtime 和第一方产品没有变化成员的直接消费者。`MoreSaves`、MoreEquipment 的存档证明和 QA 虽然认识 `LocalSave` 类型，但所用的路径、备份、云目录或计数成员均未改变；QA 对 `IEquipmentHost` 只调用未改变的创建成员。当前结论是不修改 DTMAPI 或产品源码、不新增 `24650773` policy、不批量重签。既有 `24456188` exact 产品继续通过已审查的 `Drift -> 真实类型/Entry/Hook/产品事务` 路径运行。

本审查关闭“正式版 public 字节尚未取得、与三份保留 test 快照的代码差异未知”的静态阻断；它不声称十一项产品已在 public 上完成玩家行为回归。

## 二、捕获身份与完整性

| 边界 | 结果 |
| --- | --- |
| Steam | app `2285550`；build `24650773`；`UserConfig=public`；`MountedConfig=public`；无 pending switch |
| Manifest | SHA-256 `CB84BFBCDC9AF3555FA98B2F68C59B9B5C78995C95019B36F358F7578D1EB6E3` |
| 游戏版本 | PlayerSettings/Application version 槽为 `1.00.03` |
| 主程序集 | 6,384,640 bytes；SHA-256 `76C24EE0E4BBBF27C7A68C7DFAA36A9B0B7BD0903FA6617BA05177E4AA45F0DD` |
| raw snapshot | 542 files / 1,804,556,553 bytes；source parity exact |
| AssetRipper | 51,988 files / 4,381,949,450 bytes；90 scenes；195 configs；181 managed assemblies |
| ILSpy main / firstpass | 3,666 / 29 files |
| AssetRipper errors | 93：90 个已知 Cubemap size mismatch + 3 个 `globalgamemanagers` custom-version setting-object read error；其他错误 0 |
| 污染检查 | raw 中 `BepInEx`、`Mods`、`Saves`、`Player.log`、`LogOutput.log` 命中 0 |

捕获后独立重算 raw、AssetRipper、main、firstpass 四份实际结果树。四组均为 inventory missing `0`、extra `0`、length mismatch `0`、SHA-256 mismatch `0`；捕获结束时 live Steam identity 未漂移，游戏进程为 0，共享 Runtime lock 已释放。

官方 bytes、导出资源和反编译源码只保存在 Git 忽略的 `references/doloc-town/reverse/builds/24650773_public_76C24E/`，不得提交、发布或进入 DTMAPI 包。

## 三、最近捕获差异：24585411 test -> 24650773 public

| 层 | 旧 -> 新 | 精确差异 | 稳定解释 |
| --- | ---: | --- | --- |
| raw official files | `542 -> 542` | 新增 3、删除 3、同路径改变 185、不变 354；净 `-9,547` bytes | 3 个 content-addressed bundle 换名；185 项精确为 90 个 `levelN`、90 个 `sharedassetsN.assets`、两个 global manager、`resources.assets`、catalog 和主程序集 |
| managed assemblies | `181 -> 181` | 只变 `Assembly-CSharp.dll` | 其余 180 个程序集逐字不变 |
| ILSpy main | `3,666 -> 3,666` | 6 changed、3,660 unchanged；`+35/-13` | 零文件增删；见第四节 |
| ILSpy firstpass | `29 -> 29` | 0 changed | 逐字不变 |
| Build Settings scenes | `90 -> 90` | 0 identity add/remove/index/path move | 没有新增地图或场景身份 |
| GenDatas | `195 -> 195` | 5 changed、190 unchanged | 3 个配方 + 本地化/名单修订 |
| AssetRipper export | `51,988 -> 51,988` | 新增 3、删除 3、同路径改变 45,862、不变 6,123；净 `+17,767` bytes | 新增/删除只来自 3 个 bundle 换名；不能把导出 hash 数当功能数 |

### 3.1 Unity 导出噪声归一化

45,862 个同路径导出变化中有 1 个是主程序集，余下 45,861 个文本/YAML 项逐文件做归一化：

- 45,707 项在消除 AssetRipper 重建的 GUID 和 `timeCreated` 后完全相同；
- 154 项仍有差异：100 `.asset`、33 `.unity`、3 prefab、8 JSON、6 C#、3 CSV、1 Yarn；
- 154 不是 154 项功能变化，其中包含编译后的剧情副本、重复 MonoBehaviour 投影、同一组河谷 room asset 的后缀重排、scene 对象顺序/fileID 变化和 sprite 缓存字段。

进一步按内容集合和场景行值多重集比较：

- 90 个场景中只有 `level0`、`level1`、`level12`、`level37` 有实际值变化；
- `level0/sys_global` 与 `level1/sys_splash` 只同步启动画面私有序列化字段改名；
- `level12/dungeon_多洛可河谷` 增加一个既有基底层 tile，并同步修正 `stage2_哨站` 的障碍、地面和植被约束；
- `level37/city_镇长办公室` 只移动两个“中房间墙面装饰”和“待办事项”对象；
- 其余 86 个场景没有值集合变化；其中部分仅因对象顺序/fileID 重排而非逐字相同。

Addressables `m_InternalIds` 仍为 3,345 项。路径集合只把 `configs`、`gu`、`uu` 三个旧内容哈希 bundle 名替换为新名，没有新增地址。12 个名称含 Wwise/audio/sound/bank 的 raw 路径全部逐字不变。

## 四、实际代码变化

| 文件 / 成员 | 1.00.03 实际变化 | DTMAPI 消费结论 |
| --- | --- | --- |
| `DolocTown.Config.Fishing.TbFarmFish.IsSubspeciesFish` | 修正亚种判断：只有存在于养殖鱼表、但不在普通鱼表中的鱼才算亚种 | FishBreedingAssistant 的既有脚本只检查 `TbFarmFish.DataMap`，没有调用该方法 |
| `DolocTown.GameData.LocalSave.GetArchiveDataAsString` | JSON 序列化异常时不再向外抛出，改为返回官方失败文本 | MoreSaves/QA/MoreEquipment 读取的是其他 LocalSave 路径、目录、计数成员；这些成员逐字不变 |
| `DolocTown.UI.SplashController` | 私有序列化字段由“editor 显示”语义改名为“editor 禁用”语义；两版运行时 `disabled` 仍固定为 false | 无仓库消费者；`SplashCanvas` 同步改名 |
| `DolocTown.Crop.GenCropOutput` | 调用 CropDecorator 时增加空引用保护 | 无仓库直接消费者 |
| `DolocTown.IEquipmentHost.RemoveEquipment / CheckLightChanged` | 回收物品异常被官方隔离；检查灯光前增加 `CurrentRoom` 空值保护 | 仓库 QA 只反射未改变的 `CreateEquipment` / `CreateEquipmentNoRender`；产品无这些变化成员调用 |
| `DolocTown.VersionPatchFunctions` | 从旧成就重建中移出错误的亚种鱼补发；新增 `1.00.03` patch，对已访问春节入口剧情和已解锁亚种鱼补发对应事件 | 新方法为官方私有版本迁移；生产 Runtime/产品无消费者 |

主反编译树没有新增/删除类型文件；上述方法的既有参数和返回签名均未改变。唯一成员名变化是 `SplashController` 的私有序列化字段，且官方 prefab/两个启动场景已同步。

## 五、实际内容、剧情与界面变化

### 5.1 配方与配置

`recipe_tbrecipe.json` 从 481 行记录增至 484，新增：

- `dried_fish_hermit_octopus_variant`：1 个变异寄居章鱼 -> 3 个 `dried_fish_small_plus`；
- `dried_fish_dumbo_octopus_variant`：1 个变异小飞象章鱼 -> 16 个 `dried_fish_small_plus`；
- `dried_fish_arapaima_variant`：1 个变异巨骨舌鱼 -> 2 个 `dried_fish_small_plus`。

三项均为 `cost_time=288`、`recipe_sub_type=food`、`tech_point=5`，并加入 `air_drying_box` 与 `air_drying_box_big` 两个既有配方组。

其余配置变化：

- 英文 `Construction Items` 两处拼写修正；
- 英文删除存档确认句一处语法修正；
- `developer_marketing_content` 增加“苏保瑞”。

`item_tbitem.json`、成就配置和其他 190 张 GenDatas 逐字不变。因此本次没有新增“CD”物品或新成就定义。

### 5.2 剧情与翻译

- `ruinedcity_main.yarn` 在启动 `doloc_bus_upgrade`、登记后续对话并完成事件后，新增移除 `ruinedcity_main_actsk1`，语义是防止入口对话继续残留/重复。
- 日语、葡萄牙语（巴西）、俄语剧情 CSV 均保持 9,800 个相同 ID，无增删；文本分别修订 2,149、42、933 条。
- 编译后的 Yarn project 和三种语言 asset 随源文本变化；没有新增剧情文件或节点 ID 集合。
- `npc_favorability.asset` 只从 `@hult_gift_3` 节点移除一个编辑器 `_UID`，任务条件、动作、连接和节点数量均不变。

### 5.3 既有资源与场景

- 通用 `game_entity_covariant_controller` 的 `Sky`、`Background`、`Front_01`、`Front_02` 四层 scale 从 `45 x 25.3125` 扩至 `60 x 30`。
- `InputNameBox` 的输入区、文本区和按钮宽高增加，属于名称输入框布局放宽。
- 河谷 `stage2_哨站`：基底层坐标 `(252,63)` 增加一个 tile；局部坐标 `(12,6)` 从 ground 改入 obstacle，`(12,7)` 加入 ground 和 vegetation constraint。
- 镇长办公室：“中房间墙面装饰3”横坐标 `3.91 -> 6.75`；“中房间墙面装饰7”横坐标 `9.53 -> 11.5`；“待办事项”由 `(-10.88, 5.2500153)` 移至 `(0.575, 4.75)`。
- 默认 TMP style sheet 保持视觉 opening/closing definition 文本不变，但移除空的 `Normal` 项、更新 `Quote/Link/Title` lookup hash，并清空导出的 unicode 缓存数组。
- 20 个河谷背景/通用 sprite 只改变 `_typelessdata` 缓存尾部；底层纹理 payload 和资源路径没有变化，静态导出不足以把该缓存差异解释为可见画面变化。
- 启动画面九种语言从 Early Access 文案切换为 1.0 正式版文案。

## 六、与最后明确准入基线的累计差异

最后明确准入的 final-test/Author 参考仍是 `24456188_test_E861E0 / 1.00.00`。`24567135` 与 `24585411` 是保留的兼容快照；本次 public 捕获不回写既有 exact policy。

| 比较 | raw | ILSpy main | GenDatas | 场景身份 |
| --- | --- | --- | --- | --- |
| `24585411 -> 24650773` | `+3/-3/185 changed/354 unchanged` | 6 changed | 5 changed | `90 -> 90`，零增删/移动 |
| `24567135 -> 24650773` | `+3/-3/185 changed/354 unchanged` | 7 changed，额外包含 `DolocPagedLinearUI` | 5 changed | 同上 |
| `24456188 -> 24650773` | `+93/-3/187 changed/262 unchanged` | 9 changed | 15 changed | 同上 |

累计 9 个主反编译文件可按版本层精确拆开：

1. `24456188 -> 24567135 / 1.00.01`：`SteamWorkshopUploader.UploadUpdate` 对已有 Workshop item 不再重写 tags；`VersionPatcher.LoadAllVersionPatchesBeyond` 增加版本排序。另有 11 张配置小修、90 个 `level*.resS` 打包布局回归和 old-photo shader/material 名称替换。
2. `24567135 -> 24585411 / 1.00.02`：只改变 `DolocPagedLinearUI.RefreshView` 的分页 stale-slot 清理顺序；配置表全同，场景身份全同。
3. `24585411 -> 24650773 / public 1.00.03`：本审查第四、第五节的 6 个代码文件和小型数据/资源修订。

因此 `24456188 -> public` 的累计变化仍是少量、可逐项分类的方法体和内容修订，不是一次原生 API/类型布局重构。

## 七、真实消费者与处置边界

对 `src`、`products`、`first-party-mods`、`tools`、`tests`、`author-sdk` 做了排除 `bin/obj` 的直接消费者扫描：

- `IsSubspeciesFish`、`GetArchiveDataAsString`、`GenCropOutput`、`RetrieveItemOnRemoval`、`CheckLightChanged` 和变化的 `RemoveEquipment` 无仓库调用；
- `LocalSave` 有 MoreSaves、MoreEquipment 和 QA 消费，但命中成员是 `GetDataFullPath`、backup/prev/temp 路径、`cloudDirPath`、`backupCount` 等未变化成员；
- `IEquipmentHost` 只在可选内容 QA 中用于未变化的创建接口；
- 新增三项配方 ID、三项新 bundle hash 和启动画面字段没有生产消费者；
- 既有 `SteamWorkshopUploader.ResolveUploadPlan` Hook 仍位于逐字未变的方法中；
- `VersionPatcher` 和 `DolocPagedLinearUI.RefreshView` 仍只有既有可选 QA 消费，签名不变。

处置：

- 不修改 Core、GameBridge、Manager、公开 API 或第一方产品；
- 不为纯 drift 生成 `24650773` Author policy，不把 live public Assembly 冒充 `24456188` exact reference；
- 将 `24650773_public_76C24E` 作为未来静态原生/资源研究的最新 public 观察头；
- 只有某个产品在 public 上发生真实 Entry、Hook 或事务失败时，才对命中的产品做定向修复和最小行为重放；
- 若未来要把产品正式重编译到 `24650773`，必须另开有界 policy/SDK/receipt 生命周期，不能由本审查自动授权。

## 八、验证与仍未声称的内容

已完成：

- full capture start/frozen/end Steam identity 和 source parity；
- 四组 inventory 独立路径/长度/hash 复核；
- raw、managed、scene、GenDatas、ILSpy main/firstpass、AssetRipper 全层比较；
- 6 个代码文件逐行方法体比较；
- 5 张 GenDatas 按稳定键逐记录比较；
- 三种剧情 CSV 按 9,800 个稳定 ID 比较；
- AssetRipper GUID/时间戳归一化、河谷 room asset 内容集合比较、90 场景 fileID/顺序归一化和值多重集比较；
- Addressables internal ID 和 12 个音频路径比较；
- DTMAPI/第一方产品真实消费者扫描。

没有运行游戏、完整 Release、Workshop publication 或十一产品玩家行为回归。静态捕获和兼容审查不能替代这些门；本次没有产品源码变化，也没有证据要求为无消费者增量启动广泛回归。

本地详细证据：

- `references/doloc-town/reverse/builds/24650773_public_76C24E/full-baseline-inventory/comparison-to-24585411.json`
- `references/doloc-town/reverse/builds/24650773_public_76C24E/full-baseline-inventory/comparison-to-24567135.json`
- `references/doloc-town/reverse/builds/24650773_public_76C24E/full-baseline-inventory/comparison-to-24456188.json`

这些 JSON 和所有官方输入均为 ignored local evidence，不得提交或发布。
