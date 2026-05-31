# Doloc Town Workshop Update 0.96.06 Notes

记录时间：2026-05-29

## 官方公告转写

来源：用户提供的更新公告截图。

```text
创意工坊版本更新公告
v0.96.06
- 新增建筑外部墙纸（可在杰拉德商店中获取）
- 草原衣柜增大容量、增加可放置道具类型限制
- 包含在烹饪相关配方中的原料道具会视为可烹饪
- 修复“明克的秘密行动”任务中摩托贴图丢失的问题
- 修复“共生滋养”和“希望”基因同时存在时未能正常产出种子的问题
- 修复试玩Demo存档继承后“河谷里的伊甸园”任务可能异常结束的问题
- 修复动画中讲话的npc行走结束后角色动画播放可能异常的问题
- 修复动画过程中加速播放可能导致npc移动轨迹异常的问题
- 修复个别剧情动画同时触发时会导致游戏无法操作的问题
- 修复部分主角动画贴图存在颜色偏差的问题
```

## 本地 Steam / 游戏状态

当前本地 Steam manifest：

- 文件：`D:\Steam\steamapps\appmanifest_2285550.acf`
- appid：`2285550`
- name：`Doloc Town`
- 分支：`workshop`
- build id：`23465763`
- TargetBuildID：`23465763`
- LastUpdated：`1780054500`

当前核心程序集：

- 文件：`D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed\Assembly-CSharp.dll`
- 修改时间：`2026-05-29 19:34:47 +08:00`
- 大小：`5991424`
- SHA256：`38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228`

新的反编译批次：

- 批次目录：`research/reverse/builds/23465763_workshop_38581E`
- 对比基线：`research/reverse/builds/23249387_workshop_247ACD`
- API map 入口：`research/reverse/builds/23465763_workshop_38581E/maps/00_Index.md`
- 配置抽取目录：`research/reverse/builds/23465763_workshop_38581E/content-configs`
- 差异目录：`research/reverse/builds/23465763_workshop_38581E/diffs`

结论：本地游戏已从 build `23249387` 更新到 `23465763`，反编译数据需要更新。

## 反编译统计变化

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

差异文件：

- `diffs/types-added.csv`
- `diffs/types-removed.csv`
- `diffs/methods-added.csv`
- `diffs/methods-removed.csv`
- `diffs/methods-body-size-changed.csv`
- `diffs/decompiled-files-changed.csv`
- `diffs/system-map-count-diff.csv`

新增核心类型：

- `DolocTown.Config.Equipment.ItemPlaceConditionInfo`
- `DolocTown.Config.Equipment.TbItemPlaceCondition`

删除类型只有 `Tables` 编译器 display class，属于 Luban/编译器生成类编号变化，不是业务类型移除。

## 官方内容更新对应的实证

### 建筑外部墙纸

抽取到相关配置：

- `building_tbbuildingexterior.json`
- `building_tbbuildingwallpaper.json`
- `item_tbitem.json`
- `store_tbstore.json`
- `store_tbstoreitemlist.json`

当前新增/暴露的外部墙纸道具：

- `exterior_default`：`外部墙纸（默认）`，买价 200，使用 `ItemFunctionBuildingExterior`。
- `exterior_white`：`外部墙纸（白色）`，买价 800，使用 `ItemFunctionBuildingExterior`。

`exterior_white` 只覆盖三类集装箱外观：

- `small_container`
- `medium_container`
- `large_container`

商店实证：

- `gerald_shop` 标题为 `杰拉德的小生意`。
- `gerald_shop` 出售 `wall_window`、`wall_default`、`exterior_white`、`exterior_default`。
- 这些墙纸/外部墙纸记录每季数量为 10。

反编译判断：外部墙纸机制相关类型在上一基线已经存在；本次更新主要是内容配置和商店售卖记录更新。

### 草原衣柜

`equipment_tbequipment.json` 中 `grassland_wardrobe` 现为：

- `total_capacity`: 20
- `line_capacity`: 10
- `place_condition`: `hat_only`
- `automate_types`: []

新增配置表 `equipment_tbitemplacecondition.json`：

```json
[
  {
    "id": "hat_only",
    "item_type_filter": [],
    "item_sub_type_filter": ["kit_hat"],
    "item_name_filter": []
  }
]
```

反编译对应变化：

- `EquipmentFuncCase` 新增 `PlaceCondition`、`PlaceCondition_Ref`、`AutomateTypes`、`AutomateTypes_Ref`。
- `Case.ContentFilter` 现在会拒绝不满足 `PlaceCondition_Ref.CheckCondition(item)` 的物品。
- `Case.ExtraInfoAsItem` 会把限制条件写入物品说明。

### 烹饪原料判定

`recipe_tbrecipesubtype.json` 新增/使用 `cookable` 标志：

- `cook`、`bake`、`drink` 为 `cookable=true`。
- 其他 recipe subtype 为 `cookable=false`。

反编译对应变化：

- `RecipeSubTypeInfo` 新增 `Cookable`。
- `TbRecipe` 新增 `CookableItems` 与 `PostResolve()`。
- `PostResolve()` 会把可烹饪配方的输入物品加入 `CookableItems`。
- `Item.CanCook()` 现在检查 `DolocConfig.Tables.TbRecipe.CookableItems.Contains(name)`。

这与官方“包含在烹饪相关配方中的原料道具会视为可烹饪”一致。

### “共生滋养” + “希望”基因

反编译对应变化：

- `CropGeneFunctionHope.AfterHarvest()` 现在调用 `base.AfterHarvest(shouldRender)`。
- `CropGeneFunctionHope.AfterClearWither()` 现在调用 `base.AfterClearWither(shouldRender)`。
- `CropGeneFunctionSymbioticSupply.AfterHarvest()` 现在调用 `base.AfterHarvest(shouldRender)`。
- 多个其他 `CropGeneFunction*` 子类也补了对应 base 回调。

判断：之前部分基因子类覆盖回调后没有继续传递 base 链，组合基因时可能截断其他基因效果；本次通过补 base 调用修复。

### Demo 存档与“河谷里的伊甸园”

`VersionPatchFunctions` 新增 `UpdateTo09606()`：

- 如果 `eden_main` 已完成但 `eden_main_2` 未完成，则把 `eden_main` 重启到 `eden_main_2`。
- 如果 `eden_main_valley_anim` 事件已完成，则完成 `eden_main_2`。

这对应试玩 Demo 存档继承后的任务状态修复。

### 摩托贴图与动画类修复

`Motor` API map 数量未变化：

- Types：25 -> 25
- Methods：437 -> 437
- Fields：216 -> 216
- Calls：1432 -> 1432

本次没有看到 `Motor*.cs` 代码文件变动。“明克的秘密行动”中的摩托贴图丢失更可能是资源、剧情配置或动画引用修复。

动画/剧情相关代码变动集中在：

- `AgentRenderer`
- `CharacterRenderer`
- `NpcRenderer`
- `RedSaw.ActionTask`
- `RedSaw.StateMachine`
- `LightPatch`
- `DolocUtils.setAlpha`

这组变化对应 NPC 行走后动画、动画加速轨迹、剧情动画并发导致无法操作、主角动画颜色偏差等公告项目。

## 对 SMAPI / 功能性 Mod 的影响

### 低风险区域

当前看起来不需要立刻改 SMAPI 核心加载链路：

- `ModManager_Workshop` map 未变化。
- `Fishing` map 未变化。
- `Input` map 未变化。
- `Motor` map 未变化。
- 官方仍没有 Workshop DLL 加载路径。

### 需要同步的反编译数据

需要纳入下一版研究/工具索引：

- `equipment_tbitemplacecondition`
- `ItemPlaceConditionInfo`
- `TbItemPlaceCondition`
- `EquipmentFuncCase.PlaceCondition`
- `EquipmentFuncCase.AutomateTypes`
- `RecipeSubTypeInfo.Cookable`
- `TbRecipe.CookableItems`
- `VersionPatchFunctions.UpdateTo09606`

### 需要复测的 mod 类型

以下类型 mod 有实际受影响风险：

- 直接 patch `Case.ContentFilter` 或容器放置逻辑的 mod。
- 修改/扩展设备容器配置的内容包或工具。
- 修改烹饪配方、食材、可烹饪判定的 mod。
- 修改作物基因、收获、枯萎清理回调的 mod。
- patch NPC 行走、剧情动画、`RedSaw` action/state machine 的 mod。

目前未看到对 Native Functional Patcher/Bridge 注入点的直接破坏信号，但由于 DLL hash 已变化，任何 hash 守卫型加载器都要显式加入新 hash 或进入未知版本流程。

## 本次已更新的本地研究资产

- 已创建 `research/reverse/builds/23465763_workshop_38581E`。
- 已复制官方 DLL 到 `input` 并标注不分发。
- 已导出完整 C# 反编译项目。
- 已导出 metadata。
- 已生成系统 API maps。
- 已抽取本次相关内容配置 JSON。
- 已生成与 `23249387_workshop_247ACD` 的差异 CSV。
- 已更新 `scripts/reverse/export_doloctown_reverse_metadata.ps1`，支持通过 `-CecilPath` 指定 Mono.Cecil 路径。

## 后续建议

优先级 1：

- 更新版本/hash 兼容记录，把 `38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228` 加入 0.96.06 识别。
- 内容包工具链把 `equipment_tbitemplacecondition` 加入可抽取/可索引表。
- 对容器/烹饪/作物基因相关 mod 做一次最小游戏内 smoke test。

优先级 2：

- 若要支持内容包修改外部墙纸，补充 `building_tbbuildingexterior` 的 schema 文档。
- 若要做稳定 helper，先以 experimental 方式暴露容器 `place_condition` 和 recipe subtype `cookable`。
