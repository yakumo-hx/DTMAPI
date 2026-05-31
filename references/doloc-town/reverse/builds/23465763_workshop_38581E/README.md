# Doloc Town Reverse Batch 23465763_workshop_38581E

记录时间：2026-05-29

## 输入

- 游戏：Doloc Town
- Steam appid：`2285550`
- 分支：`workshop`
- Steam build：`23465763`
- 官方 DLL：`D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed\Assembly-CSharp.dll`
- 官方 DLL 修改时间：`2026-05-29 19:34:47 +08:00`
- `Assembly-CSharp.dll` SHA256：`38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228`
- 上一反编译基线：`research/reverse/builds/23249387_workshop_247ACD`

`input/Assembly-CSharp.dll` 是本地研究输入副本，不应分发。

## 产物

- `decompiled/Assembly-CSharp`：ilspycmd 导出的完整 C# 项目。
- `metadata`：Mono.Cecil 导出的程序集、类型、方法、字段、调用、字符串清单。
- `maps`：按系统聚合的 API map。
- `content-configs`：从当前 Addressables config bundle 抽取的本次更新相关配置表。
- `diffs`：与 `23249387_workshop_247ACD` 的结构差异清单。

## 反编译命令

```powershell
dotnet .tools\ilspycmd\8.2.0.7535\tools\net6.0\any\ilspycmd.dll `
  --disable-updatecheck `
  --nested-directories `
  -p `
  -r "D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed" `
  -o research\reverse\builds\23465763_workshop_38581E\decompiled\Assembly-CSharp `
  research\reverse\builds\23465763_workshop_38581E\input\Assembly-CSharp.dll
```

当前游戏 `Managed` 目录没有随带 `Mono.Cecil.dll`，所以 metadata 导出使用旧测试目录中的 Cecil：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts\reverse\export_doloctown_reverse_metadata.ps1 `
  -ManagedDir "D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed" `
  -AssemblyPath "research\reverse\builds\23465763_workshop_38581E\input\Assembly-CSharp.dll" `
  -BuildRoot "research\reverse\builds\23465763_workshop_38581E" `
  -SteamBuild "23465763" `
  -Branch "workshop" `
  -CecilPath "research\reverse\patch_test_20260517_09605\DolocTown_Data\Managed\Mono.Cecil.dll"
```

## 程序集统计

| 指标 | 23249387 | 23465763 | 差异 |
| --- | ---: | ---: | ---: |
| Types | 4792 | 4794 | +2 |
| Methods | 42873 | 42925 | +52 |
| Fields | 19519 | 19541 | +22 |
| Properties | 12293 | 12315 | +22 |
| Events | 19 | 19 | 0 |
| Calls | 173100 | 173424 | +324 |
| String occurrences | 20358 | 20404 | +46 |
| Unique strings | 7952 | 7963 | +11 |

差异明细：

- 新增类型：5 个，见 `diffs/types-added.csv`。
- 删除类型：3 个，均为 `Tables` 的编译器 display class，见 `diffs/types-removed.csv`。
- 新增方法：81 个，见 `diffs/methods-added.csv`。
- 删除方法：29 个，见 `diffs/methods-removed.csv`。
- 方法体大小变化：49 个，见 `diffs/methods-body-size-changed.csv`。
- 反编译文件变动：33 个，见 `diffs/decompiled-files-changed.csv`。

## 关键新增/变化

### 容器放置限制

新增配置类型：

- `DolocTown.Config.Equipment.ItemPlaceConditionInfo`
- `DolocTown.Config.Equipment.TbItemPlaceCondition`

`EquipmentFuncCase` 新增：

- `PlaceCondition`
- `PlaceCondition_Ref`
- `AutomateTypes`
- `AutomateTypes_Ref`

`Case.ContentFilter` 现在会调用 `PlaceCondition_Ref.CheckCondition(item)`，不符合限制的物品不能放入对应容器。`Case.ExtraInfoAsItem` 也会用 `ItemContainerConditionFormat` 展示限制说明。

当前配置表中只有一个放置条件：

```json
{
  "id": "hat_only",
  "item_type_filter": [],
  "item_sub_type_filter": ["kit_hat"],
  "item_name_filter": []
}
```

`grassland_wardrobe` 现在是：

- `total_capacity`: 20
- `line_capacity`: 10
- `place_condition`: `hat_only`
- `automate_types`: []

### 建筑外部墙纸

本批次抽取到 `building_tbbuildingexterior.json`、`building_tbbuildingwallpaper.json`、`item_tbitem.json`、`store_tbstoreitemlist.json` 的相关记录。

当前外部墙纸道具：

- `exterior_default`：`外部墙纸（默认）`，买价 200，`ItemFunctionBuildingExterior(exterior_default)`。
- `exterior_white`：`外部墙纸（白色）`，买价 800，`ItemFunctionBuildingExterior(exterior_white)`。

`exterior_white` 覆盖 `small_container`、`medium_container`、`large_container` 的外部贴图。`gerald_shop`（`杰拉德的小生意`）出售 `wall_window`、`wall_default`、`exterior_white`、`exterior_default`，每季数量 10。

注意：`ItemFunctionBuildingExterior`、`TbBuildingExterior`、`ItemBuildingExterior` 在上一基线已经存在；本次主要是配置和商店内容暴露/更新，不是机制首次加入。

### 烹饪可用原料

`RecipeSubTypeInfo` 新增 `Cookable`。当前 `recipe_tbrecipesubtype.json` 标记：

- `cook`、`bake`、`drink`：`cookable=true`
- 其他 recipe subtype：`cookable=false`

`TbRecipe.PostResolve` 会把可烹饪配方的输入物品加入 `CookableItems`。`Item.CanCook` 现在除了旧逻辑外，也检查 `DolocConfig.Tables.TbRecipe.CookableItems.Contains(name)`。

### 基因连锁修复

`CropGeneFunctionHope.AfterHarvest`、`CropGeneFunctionHope.AfterClearWither`、`CropGeneFunctionSymbioticSupply.AfterHarvest` 等方法现在调用对应 `base.*`。这解释了官方公告里“共生滋养”和“希望”同时存在时种子产出异常的修复：基因回调链不再被子类覆盖后截断。

### Demo 存档任务补丁

`VersionPatchFunctions` 新增 `UpdateTo09606()`：

- 如果任务链 `eden_main` 已完成但 `eden_main_2` 未完成，则把 `eden_main` 重启到 `eden_main_2`。
- 如果事件 `eden_main_valley_anim` 已完成，则直接完成 `eden_main_2`。

这对应试玩 Demo 存档继承后“河谷里的伊甸园”任务可能异常结束的问题。

### 动画/剧情相关修复

变动集中在：

- `AgentRenderer`
- `CharacterRenderer`
- `NpcRenderer`
- `RedSaw.ActionTask`
- `RedSaw.StateMachine`
- `LightPatch`
- `DolocUtils.setAlpha`

这些变化对应公告中的 NPC 行走后动画、动画加速轨迹、个别剧情动画同时触发导致无法操作、主角动画贴图颜色偏差等修复。

### 摩托相关

`Motor` 系统 map 数量未变化：

- Types：25 -> 25
- Methods：437 -> 437
- Fields：216 -> 216
- Calls：1432 -> 1432

反编译文件差异中也没有 `Motor*.cs` 变动。因此“明克的秘密行动”摩托贴图丢失更像是资源、剧情或动画配置修复，而不是公开 Motor 代码/API 变化。

## 对 SMAPI 的影响

需要更新反编译数据：是。

原因：

- 官方 DLL hash、Steam build 都已变化。
- 新增了容器放置限制配置表和对应代码类型。
- 物品、配方、容器、存档迁移、动画状态机都有代码或配置变动。

当前看起来不需要立即改 SMAPI 1.0 核心：

- `ModManager_Workshop` map 数量未变化。
- 官方仍未暴露 Workshop DLL 加载。
- `Motor`、`Fishing`、`Input` 等现有核心系统 map 未出现结构性扩张。

建议后续跟进：

- 内容包/配置 patcher 要把 `equipment_tbitemplacecondition` 纳入可读配置表。
- 容器相关 helper 如果存在，应暴露或至少记录 `place_condition` 语义。
- 食谱/烹饪 helper 应同步 `RecipeSubTypeInfo.Cookable` 和 `TbRecipe.CookableItems`。
- 任何直接 patch `Case.ContentFilter`、`EquipmentFuncCase`、`Item.CanCook`、作物基因回调、剧情动作状态机的 mod 都需要复测。

## 分发说明

本目录包含本地反编译源码和官方 DLL 副本，只能作为本地研究资料。公开发布时只分发：

- 自己编写的 runtime/SDK/mod 代码。
- 文档。
- 统计、API map、差异摘要。
- 不包含官方 DLL，不发布大段官方源码。
