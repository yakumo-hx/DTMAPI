# 缺氧动物包（DTMAPI.AnimalPack）

这是四种本地自定义畜牧动物的统一 ContentPack 真源，作者为 `Yuuka`。游戏内名称为“缺氧动物包”，最低需要 DTMAPI `0.6.0`。它本身是纯内容包，但动物与资源的加载能力由 DTMAPI 提供。它保留 `hatch`、`drecko`、`mole`、`oilfloater` 及其既有动画、声音和动物袋 ID，并新增普通蛋、隐藏产物与“小动物照料站”的官方 JSON 经济路线。

## 当前冻结经济

| 动物 | 理论最短普通周期 | 普通产物 | 原版产出设备 | 照料站结果 | 隐藏结果 | 阈值 |
| --- | ---: | --- | --- | --- | --- | ---: |
| 哈奇 | 1 天 | 哈奇蛋 | 鸡窝 | 铁矿石 5 + 铜矿石 5 | 石化哈奇蛋；处理为金矿石 10 + 钛矿石 5 | 60 |
| 壁虎 | 3 天 | 壁虎蛋 | 粘毛滚 | 羊毛 3 | 直接产出羊毛脂 3 | 60 |
| 田鼠 | 1 天 | 田鼠蛋 | 挤奶器 | 生肉 10 | 肥沃田鼠蛋；处理为生肉 5 + 有机肥 30 | 40 |
| 浮游生物 | 1 天 | 浮游生物蛋 | 蜂箱 | 煤 20 + 树脂 5 | 聚合浮游生物蛋；处理为橡胶 30 + 塑料 30 | 120 |

普通蛋与需要处理的隐藏蛋可以直接出售，价格约为处理结果原版总售价的三分之二；使用照料站可提升约 50%。壁虎隐藏奖励没有隐藏蛋，达到阈值后由官方 `animal_tbhusbandry.json` 路径直接产出 `wool_grease x3`。

## 购买、图纸与设备制作

- 凯涅尼木的 `animal_shop` 固定出售四种幼体袋：哈奇 `800G`、壁虎 `3000G`、田鼠 `5000G`、浮游生物 `1200G`。成年动物的基础出售价格分别为 `2400G / 9000G / 15000G / 3600G`，均为对应幼体购买价的三倍。
- 同一商店每季固定出售一张“小动物照料站图纸”，价格 `1000G`。图纸是原版 `ItemFunctionRecipe` 消耗品；购买并使用后才会解锁 `animal_care_station` 配方，设备不再默认解锁。
- 在设备工作台使用 `copper_ingot x5 + wood x100`，即铜锭 5 个、木头 100 个，制作一台小动物照料站。
- 将普通蛋或哈奇、田鼠、浮游生物的稀有蛋放入照料站即可按上表加工。七条加工配方的 `cost_time` 均为 `12 TU`，在 1.00.06 的 `TU2Min = 5` 下正好是游戏内 `1h`。壁虎的隐藏产物羊毛脂直接产出，不经过设备。
- 照料站的 `electronic_component` 是 `EComProtoAppliance`，`threshold = 1`，因此原生界面显示额定耗电 `1`，工作推进时按原生器械机制耗电。物品表里的 `electric_energy = 0` 是正确配置：该字段描述手持物品自身携带的电量，并不是放置后设备的耗电。
- 照料站可出售。物品表使用原版设备通用的 `selling_price = -1`，由 `ItemEquipment.GetSellingPrice()` 按制作材料动态计算；1.00.06 的铜锭售价 `50G`、木头售价 `4G`，所以当前基础售价为 `5×50 + 100×4 = 650G`。`buying_price = 0` 也属正常，因为商店出售的是图纸，不直接出售成品设备。
- 照料站保留官方农场器械的天气处理器；酸雨期间会暂停工作，酸雨结束后恢复。普通实时等待和 Y 控制台原生跳时均已人工验证可正常完成加工。

## 示例 Mod 定位

本mod为示例mod，基于DTMAPI的动物拓展能力，将在日后更新教程文档。作者只需要配置PNG、WAV、JSON即可实现添加养殖动物。欢迎大家体验。有bug请反馈，喜欢请点好评♥️

## 封面与 Steam 三语介绍

- 根目录 `preview.png` 来自 `D:/图片/封面图/缺氧动物包.jpg`，保持原构图转换为真正的 `1549x925` PNG，用作 Steam 大预览图。
- 根目录 `icon.png` 来自 `D:/图片/封面图/缺氧动物包 - 副本.jpg`，保持原构图转换为真正的 `744x482` PNG，用作游戏内 Mod 封面和本项目约定的 Steam 小预览图。
- `official-info.json.localized_description` 已准备 `schinese / tchinese / english` 三份完整的 19 行介绍；三种语言均逐项列出普通产出、原版产出设备、隐藏产出、阈值、动物袋价格、成年售价、照料站图纸和设备加工方式。
- public 1.00.06 的正式设备名称是：简中 `鸡窝 / 粘毛滚 / 挤奶器 / 蜂箱`，繁中 `雞窩 / 粘毛滾 / 擠奶器 / 蜂箱`，英文 `Chicken Coop / Lint Roller / Milking Machine / Hive Box`。壁虎设备不是“粘毛器”，发布介绍统一使用游戏内正式名称“粘毛滚”。
- `asset-sources.json` 冻结两张源 JPG 的路径、尺寸与 SHA-256，以及两张最终 PNG 的尺寸与 SHA-256；本地组装器拒绝缺图、格式伪装、尺寸漂移或哈希漂移。

## 当前 Workshop 交付记录

- 2026-08-28，压缩后的 `preview.png` 通过游戏内上传器提交成功。当前本地 `workshop.json` 记录 Workshop item `3791474574`；`Player.log` 依次记录基础内容提交成功、繁中/英文文本提交成功和最终 `Workshop upload success!`。
- Steam 已把该 item 下载到 `D:/Steam/steamapps/workshop/content/2285550/3791474574`，`appworkshop_2285550.acf` 记录 manifest `8336118746041049876`。
- 仓库组装包、本地 `MODS/DTMAPI_AnimalPack`（排除成功回调后才生成的 `workshop.json`）和 Steam 订阅目录均为 `211` 个文件、`1,898,757` 字节。按相对路径、长度和 SHA-256 比较为 `missing=0 / extra=0 / mismatch=0`；订阅包也分别通过 PowerShell 7 与 Windows PowerShell 5.1 的 `test-static.ps1`。
- `preview.png` 保持 `1549x925` 和逐像素内容不变，压缩后为 `849,062` 字节，SHA-256 为 `743206EE3A57402CA20611F6160A738E82E61292EE31400911D866A099ADC96F`。`asset-sources.json` 与静态检查将上限保守锁为小于 `1,000,000` 字节。
- 压缩前的失败提交曾创建多条未完成 item，它们不是本包后续更新目标；后续游戏内更新只应使用 `workshop.json` 中的 `3791474574`。
- 以上是本机实际上传与订阅字节的观察记录，不把 item 可见性、公开发布授权或第三方素材再分发许可判为已通过。Product Catalog 仍把该产品保留为 `LocalDeveloper / PrototypeBlocked`，也不把这次绕过发布流程的上传写入当前公开订阅 authority。

## 当前蛋图标原型

七种蛋均使用 `icon_item_<物品ID>` 独立资源键，不再复用成年动物帧：

| AnimalPack 物品 | ONI 图集变体 |
| --- | --- |
| 哈奇蛋 | Hatch Egg `ui` |
| 石化哈奇蛋 | Metal Hatch Egg `mtl_ui` |
| 壁虎蛋 | Drecko Egg `ui` |
| 田鼠蛋 | Shove Vole Egg `ui` |
| 肥沃田鼠蛋 | Delecta Vole Egg `del_ui` |
| 浮游生物蛋 | Slickster Egg `ui` |
| 聚合浮游生物蛋 | Molten Slickster Egg `hot_ui` |

这些图标是 `E:/DolocTownUnity/ONIExtracted` 中渲染好的 UI 帧；`asset-sources.json` 冻结精确相对路径、源 SHA-256 和规范化输出 SHA-256。组装器先裁掉透明边，再用高质量双三次采样把主体等比缩进 `24x24`，最后居中写入原版物品图标规格的 `28x28` 透明画布。蛋图没有单独元数据，沿用官方 Mod 加载器默认的 `8 PPU`，因此背包图标和照料站上方的世界提示使用同一套原版尺度。它们仍只用于本地原型，不构成公开再分发授权。

## 小动物照料站几何

- `equipment_tbequipment.json` 的占地固定为 `2x4`。
- 组装器把照料站源图裁到实际透明边界，再等比缩入 `32x46`，底部对齐到 `32x48` 画布并只保留 1 像素底边空白。
- 同名 sprite 元数据固定为 bottom-center pivot `(16,1)` 和原生 `8 PPU`。这既让可见底边贴地，也把原生按 PNG 原始高度计算的设备中心/掉落点从旧 249 像素画布恢复到 48 像素设备范围。
- 这些尺寸与 1.00.06 的 `2x4` 原版设备相容；本轮没有给 `EquipmentFuncGarbageShredder` 增加坐标 Hook。

## 本地素材边界

仓库真源现在包含用户指定的 `icon.png` 与 `preview.png` 发布封面，以及作者自有的 JSON、组装脚本和验证脚本。现有四包的动物 PNG/WAV、`ONIExtracted/rancherstation` 图片与七张 ONI 蛋图仍是本地来源不闭合的研究素材。它们虽已随 2026-08-28 的本机原型上传进入 item `3791474574`，但该物理事实不授予公开再分发权，也不解除 AssetProvenance 阻塞；在来源替换或许可闭合前，项目发布流程不得把该 item 认定为正式公开候选。`asset-sources.json` 的路径与哈希记录同样不构成授权。

本地组装：

```powershell
products/first-party/AnimalPack/build-local-prototype.ps1
```

可通过 `DTMAPI_ANIMAL_PACK_INPUT_ROOT` 覆盖四个旧包的共同父目录，通过 `DTMAPI_ANIMAL_PACK_ICON_INPUT_ROOT` 覆盖 ONI 蛋图标根目录，通过 `DTMAPI_ANIMAL_PACK_STATION_SPRITE` 覆盖照料站 PNG。默认输出位于 `.local-build/DTMAPI_AnimalPack`，该目录已被 Git 忽略。

旧四包与统一包包含相同的 species/item/LUT ID，绝不能同时启用。本地部署和游戏验收必须在共享 Runtime lock 下先隔离旧四包，再启用统一包。

## 验证

```powershell
products/first-party/AnimalPack/test-static.ps1
products/first-party/AnimalPack/build-local-prototype.ps1
products/first-party/AnimalPack/test-static.ps1 -PackageRoot products/first-party/AnimalPack/.local-build/DTMAPI_AnimalPack
```

静态 PASS 证明结构、引用、数量、阈值、经济算术、四种动物的官方产出设备映射、凯涅尼木五项固定货架（四种幼体袋与一张图纸）、图纸解锁关系、`铜锭 x5 + 木头 x100` 制作成本、设备动态售价 `650G`、加工 `1h`、额定耗电 `1`、七张 `28x28` 蛋图的居中透明边界、照料站 `32x48` 画布/底边/pivot、三语发布介绍哈希、两张封面的真实 PNG 格式/尺寸/哈希以及所有规范化输出哈希；它不代替游戏内加载、动画、声音、隐藏贡献、设备交互或存档验收。
