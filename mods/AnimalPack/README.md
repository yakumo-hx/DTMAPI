# 缺氧动物包（DTMAPI.AnimalPack）

畜牧定义、声音和绑定放在 `PackData/content.json`，资源路径相对包根。官方 `animal_tbanimal.json` 的成年/幼体动画地址直接使用原生模板，行为使用动物实例自己的 `schedule_id`。PNG 定义只写物种和帧前缀，不再重复填写 AI 模板或动画机键。原生动物、物品身份保持不变。

内容在启动时读取；修改后重启游戏。定义与声音可以分别释放，无声音内容不启动 Audio。作者入口见[畜牧指南](../../docs/authors/ANIMAL-CONTENT.md)。AnimalHusbandryProgress 保持独立产品。

这是四种本地自定义畜牧动物的统一内容包源码，作者为 `Yuuka`。游戏内名称为“缺氧动物包”。当前包最低需要 DTMAPI `0.7.5`，在同一个安装项内以 `PackData/content.json` 的 audio 分区和八段 WAV 接入可选 Audio `0.2.x`；缺少 Audio 时仅声音回到原生，动物内容仍可加载。它本身没有 DLL，保留 `hatch`、`drecko`、`mole`、`oilfloater`、既有动物袋和非音频内容身份。

本目录公开 JSON、配置、组装和验证脚本。图片、声音、封面及素材来源清单由打包者通过参数或环境变量提供，不随公开源码分发。

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

本 Mod 为畜牧内容示例。作者配置 PNG、WAV、JSON 即可添加养殖动物；[新增养殖动物 HTML 教程](../../docs/authors/content-packs/custom-animal-tutorial.html)以当前哈奇配置为原型，提供可按文件复制的完整示例。

## 封面与 Steam 三语介绍

- 素材目录中的 `preview.png` 是 `1549x925` PNG，用作 Steam 大预览图。
- 素材目录中的 `icon.png` 是 `744x482` PNG，用作游戏内 Mod 封面和 Steam 小预览图。
- `info.json.localized_description` 已准备 `schinese / tchinese / english` 三份完整的 19 行介绍；三种语言均逐项列出普通产出、原版产出设备、隐藏产出、阈值、动物袋价格、成年售价、照料站图纸和设备加工方式。
- public 1.00.06 的正式设备名称是：简中 `鸡窝 / 粘毛滚 / 挤奶器 / 蜂箱`，繁中 `雞窩 / 粘毛滾 / 擠奶器 / 蜂箱`，英文 `Chicken Coop / Lint Roller / Milking Machine / Hive Box`。壁虎设备不是“粘毛器”，发布介绍统一使用游戏内正式名称“粘毛滚”。
- 素材目录中的 `asset-sources.json` 记录源图信息及最终 PNG 的尺寸、SHA-256；本地组装器继续检查缺图、图片格式、尺寸和哈希。

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

这些图标来自打包者提供的 UI 帧；`asset-sources.json` 记录相对路径、源 SHA-256 和规范化输出 SHA-256。组装器先裁掉透明边，再用高质量双三次采样把主体等比缩进 `24x24`，最后居中写入原版物品图标规格的 `28x28` 透明画布。蛋图没有单独元数据，沿用官方 Mod 加载器默认的 `8 PPU`，因此背包图标和照料站上方的世界提示使用同一套原版尺度。图片不随公开源码分发。

## 小动物照料站几何

- `equipment_tbequipment.json` 的占地固定为 `2x4`。
- 组装器把照料站源图裁到实际透明边界，再等比缩入 `32x46`，底部对齐到 `32x48` 画布并只保留 1 像素底边空白。
- 同名 sprite 元数据固定为 bottom-center pivot `(16,1)` 和原生 `8 PPU`。这既让可见底边贴地，也把原生按 PNG 原始高度计算的设备中心/掉落点从旧 249 像素画布恢复到 48 像素设备范围。
- 这些尺寸与 1.00.06 的 `2x4` 原版设备相容；本轮没有给 `EquipmentFuncGarbageShredder` 增加坐标 Hook。

## 本地素材边界

素材目录须包含 `asset-sources.json`、`icon.png` 和 `preview.png`。用 `-AssetRoot` 指定该目录，或设置 `DTMAPI_ANIMAL_PACK_ASSET_ROOT`；脚本不假定素材位于工作区内。四个动物素材包、蛋图标和照料站 PNG 的来源仍由该清单及现有参数决定。

以下命令在公开源码根目录运行。本地素材组装后，输出可以直接加载：

```powershell
./mods/AnimalPack/build-local-prototype.ps1 -AssetRoot '<素材目录>'
```

可通过 `-PrototypeInputRoot` 或 `DTMAPI_ANIMAL_PACK_INPUT_ROOT` 指定四个旧素材包的共同父目录，通过 `-ItemIconRoot` 或 `DTMAPI_ANIMAL_PACK_ICON_INPUT_ROOT` 指定蛋图标根目录，通过 `-StationSprite` 或 `DTMAPI_ANIMAL_PACK_STATION_SPRITE` 指定照料站 PNG。未提供这些参数时沿用素材清单中的本机配置。默认输出位于本 Mod 的 `.local-build/DTMAPI_AnimalPack`，该目录已被 Git 忽略。

纯内容目录无需 SDK 封包或校验文件。可以直接编辑 manifest、JSON 和素材；`build-package.ps1` 是可选的目录整理/ZIP 工具，用现有素材叠加当前声明，保留官方 `Content/`、受控 `PackData/`、封面与三语介绍，不调用 SDK、不生成校验凭证。当前更新后的 Runtime 0.7.5 直接读取这些内容。

成年 Hatch 的原生鸡模板需要 `jump_0/1/2`。仓库维护修正后的 `Content/Sprites/hatch_frame_manifest.json`；组装器按 `asset-sources.json.hatchAdultJumpCompletion` 的固定规则，将既有 `jump_0` 原字节复制为 `jump_2`。原生 `jump_ready` 使用低身的 `0`，`jump` 循环使用抬身的 `1` 与低身的 `2`，落地后回到 idle。三帧保持 `32x32` 和原有朝向，左右翻转继续由原生 renderer 执行。原型四包只读，补帧不改变上述素材来源边界。

旧四包与统一包包含相同的 species/item/LUT ID，绝不能同时启用。本地部署和游戏验收必须在共享 Runtime lock 下先隔离旧四包，再启用统一包。

## 验证

```powershell
$animalAssets = '<素材目录>'
./mods/AnimalPack/test-static.ps1 -AssetRoot $animalAssets
./mods/AnimalPack/build-local-prototype.ps1 -AssetRoot $animalAssets
./mods/AnimalPack/build-package.ps1 -AssetRoot $animalAssets -AssembledRoot ./mods/AnimalPack/.local-build/DTMAPI_AnimalPack -OutputRoot '<新的交付目录>'
```

可选整理入口最后检查实际内容目录；`test-static.ps1 -PackageRoot <package目录> -AssetRoot <素材目录>` 不要求 SDK 文件。静态 PASS 证明结构、引用、数量、阈值、经济算术、四种动物的官方产出设备映射、凯涅尼木五项固定货架（四种幼体袋与一张图纸）、图纸解锁关系、`铜锭 x5 + 木头 x100` 制作成本、设备动态售价 `650G`、加工 `1h`、额定耗电 `1`、七张 `28x28` 蛋图的居中透明边界、照料站 `32x48` 画布/底边/pivot、三语发布介绍哈希、两张封面的真实 PNG 格式/尺寸/哈希以及所有规范化输出哈希；它不代替游戏内加载、动画、声音、隐藏贡献、设备交互或存档验收。

Hatch 成年跳跃检查独立要求 `jump_0/1/2` 的文件及 manifest 顺序、数量和 `32x32` 画布，验证 `jump_2` 沿固定复制规则产生。因此文件和清单同时漏掉 `jump_2` 也会失败；这项要求只覆盖本片成年 Hatch 跳跃。

如需同时核对本机原生数据表，给 `test-static.ps1` 追加 `-NativeDataRoot <原生数据表目录>`。它按需读取该目录中的 `item_tbitem.json` 和 `settings_tbglobalparameter.json`；未提供时仍使用已有的 1.00.06 期望值，不搜索工作区外的研究目录。
