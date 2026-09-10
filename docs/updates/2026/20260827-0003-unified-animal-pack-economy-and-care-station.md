# 20260827-0003：统一 AnimalPack 经济与小动物照料站

## Metadata

- Update ID: `20260827-0003`
- Date: `2026-08-27`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 用户在四动物经济与教程预实施审查后，冻结普通产物、隐藏产物、阈值和设备名称，并要求开始更新 Mod；2026-08-28 又明确动物应通过通用动物袋在凯涅尼木商店购买/出售，商店出售幼体，成年基础售价为对应幼体购买价的三倍；随后提供 `E:/DolocTownUnity/ONIExtracted/creature_research_summary.csv` 并冻结七种蛋的本地原型选图，特别指定石化哈奇蛋使用 Metal Hatch Egg。最新人工复测确认普通等待与 Y 跳时加工均正常，先前停工由跳时跨入酸雨触发；用户随后把游戏内名称改为“缺氧动物包”，要求补完整产品说明、在同一动物商店出售照料站图纸，并把设备成本冻结为铜锭 5、木头 100；最终提供新的完整简体中文介绍，要求最低依赖改为 DTMAPI 0.6.0，并复核设备售价、耗电、加工时间及相关 JSON 是否配全。其后用户指定两张本地 JPG 分别作为 Steam 大预览、游戏内封面与 Steam 小预览，并要求四条普通产出补原版设备名称、结尾补反馈/好评提示，以及准备简中、繁中、英文三份完整 Steam 介绍；最后明确产品作者应为 `Yuuka`。

## Scope

- 为 Catalog 已保留的 `DTMAPI.AnimalPack` 建立仓库内规范真源，合并 Hatch、Drecko、Mole、Oilfloater 四个独立本地 ContentPack，同时保留原 species、动物袋、产物 LUT、Animator、Sprite 前缀和声音身份。
- 把四种普通产出改为自定义蛋，并通过独立“小动物照料站”按固定数量拆出原版物品。
- 配置四条隐藏畜牧路线：Hatch 阈值 60、Drecko 60、Mole 40、Oilfloater 120；Drecko 直接产出 `wool_grease x3`，其余隐藏蛋进入照料站。
- 复用官方 `EquipmentFuncGarbageShredder`、独立拆解配方组与官方 JSON/PNG 入口，不新增 Runtime Hook 或公开 API。
- 四个购买载体统一使用官方 `icon_item_sack_full`，但各自保留唯一物品标题、`preset_animal`、幼年/成年袋内标签和默认动物名；凯涅尼木的 `animal_shop` 以固定货架出售四种幼体袋。
- 采用四个原生模板的官方幼体购买档位，幼体回售价保持购买价的 80%，成年基础售价提高为购买价的三倍。
- 把“小动物照料站”改为图纸解锁：图纸在凯涅尼木同一 `animal_shop` 固定出售，购买并使用后才解锁设备工作台配方；设备成本为 `copper_ingot x5 + wood x100`。
- 七种蛋各自使用 `icon_item_<物品ID>` 与一张哈希锁定的 ONI UI 帧；普通/隐藏产物不再指向成年动物 Sprite。
- 只将现有 ONI 提取物作为本地开发原型输入；公开发布继续被资产来源与跨项目再分发许可阻塞。

## Frozen Economy Input

| Animal | Primary cadence/output | Care-station result | Hidden result | Threshold |
| --- | --- | --- | --- | ---: |
| Hatch | 1 day / Hatch egg | `iron_ore x5 + copper_ore x5` | Petrified Hatch egg -> `gold_ore x10 + titanium_ore x5` | 60 |
| Drecko | 3 days / Drecko egg | `wool x3` | direct `wool_grease x3` | 60 |
| Mole | 1 day / Mole egg | `meat x10` | rare Mole egg -> `meat x5 + organic_fertilizer x30` | 40 |
| Oilfloater | 1 day / Oilfloater egg | `coal x20 + resin x5` | rare Oilfloater egg -> `rubber x30 + plastic x30` | 120 |

The package is intentionally stronger than vanilla. The review records exact output value, per-space value and value-per-threshold comparisons; this Update implements the user's final numbers without silently rebalancing them.

## Frozen Animal Purchase And Resale Input

| Animal | Template price tier | Juvenile bag purchase | Juvenile resale | Adult base resale | Adult / juvenile purchase |
| --- | --- | ---: | ---: | ---: | ---: |
| Hatch | Chicken | 800 | 640 | 2400 | 3x |
| Drecko | Goat | 3000 | 2400 | 9000 | 3x |
| Mole | Marsh pangolin | 5000 | 4000 | 15000 | 3x |
| Oilfloater | Slime | 1200 | 960 | 3600 | 3x |

`ItemAnimalPackage` creates the configured `preset_animal` as a juvenile when the store generates the item. Store purchase reads the bag item's `buying_price`; resale of a full animal bag reads the captured animal's child/adult `levels[].price`. The base item `selling_price` remains half of purchase price, matching the four official preset-animal bags, but it is not the full-animal resale authority. The table freezes base values before the native store's seasonal subtype multiplier; public 1.00.06 uses `1.0` for `husbandry_animal` in the first three seasons and `1.25` in the fourth.

## Changed Files

- `products/first-party/AnimalPack/**`
- `tools/release/dtmapi-product-catalog.json`
- `tools/scripts/check-product-catalog.ps1`
- `docs/reviews/code/2026/20260827-0001-animalpack-economy-tutorial-unification-preimplementation-review.md`
- `docs/reviews/code/2026/20260828-0001-animalpack-workshop-subscription-parity-audit.md`
- `docs/reviews/manual-qa/2026/20260827-0002-animalpack-native-voice-and-egg-icon-review.md`
- `docs/updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md`
- `docs/updates/INDEX-2026-08.md`
- local developer package under the official LocalLow `MODS/DTMAPI_AnimalPack` path, only while the shared Runtime lock is held

## Authority And Boundaries

- Product identity is the already reserved Catalog row `animal-pack / DTMAPI.AnimalPack / DTMAPI_AnimalPack`. The later game-UI action physically created observed item `3791474574`, but it did not originate from a Catalog release authorization; Catalog therefore remains `LocalDeveloper / PrototypeBlocked` with no authoritative public Workshop projection.
- Native save compatibility requires preserving `hatch`, `drecko`, `mole`, `oilfloater` and their existing package/LUT identities. The new package and any old input package must never be enabled together.
- Native JSON owns animals, items, shops, production, hidden husbandry, equipment and dismantle recipes. DTMAPI JSON owns only the reviewed template Animator/AI/PNG and scoped WAV adaptation.
- Public 1.00.06 identifies 凯涅尼木 as `kenenimuu`, opens the canonical `animal_shop`, and retains `kenenimuu_shop` only as a compatibility alias in the Mod-store merge. AnimalPack targets canonical `animal_shop`.
- `StoreItemSeasonData.IsFixedInSeason` treats `spawn_weight <= 0` as a fixed item. The public `animal_shop` has zero random slots, so positive-weight extra rows are not a reliable purchase route; all four custom bags therefore use weight `0`, official stock `20/20/20/1--3`, and `default_unlock=true`.
- `GarbageShredder.ContentFilter` also accepts salable equipment. Pure official JSON cannot narrow the station to animal products only; the first implementation retains and documents that native behavior.
- External PNG/WAV、`E:/DolocTownUnity/ONIExtracted/rancherstation` 与本轮七张 ONI 蛋 UI 帧都是本地原型素材。它们不是 public-release-authorized assets；`asset-sources.json` 的路径与哈希记录不授予再分发权。

## Validation Plan

- Parse every JSON and verify exact unique IDs, cross-table references, no duplicate official rows, four species, seven custom egg items, seven dismantle recipes, fixed output quantities, final thresholds and Drecko three-day cadence.
- Verify every referenced PNG/WAV exists, frame manifests cover their declared files, and the care-station sprite has explicit pivot/PPU metadata.
- Verify all seven egg items use dedicated `icon_item_*` keys, the chosen ordinary/hidden variants match the frozen source paths, and assembled PNG SHA-256 values match `asset-sources.json`; reject every `anim_animal_*_adult_idle_0` fallback.
- Verify all native output IDs and current sale values against local public `1.00.06 / 24966367_public_958EAF` GenDatas.
- Verify each bag's unique title/key, generic bag sprite, exact `preset_animal`, child/adult label keys, template-tier purchase/base resale, 80% juvenile resale, 3x adult resale, and four fixed `animal_shop` season rows.
- Verify `recipe_animal_care_station` uses native `ItemFunctionRecipe`, costs `1000G`, is fixed at one copy per season in the same `animal_shop`, unlocks the otherwise locked `animal_care_station` recipe, and that recipe consumes exactly `copper_ingot x5 + wood x100`.
- Run focused product/static checks and document governance. No complete Release suite is planned because this is an unpublished content-only prototype.
- Under the shared Runtime lock, deploy the unified package while isolating the four old input packages, then use the authoritative seventh-save `NoNativeSave` fixture to validate package discovery, four animals, bridge health, station/config load, clean exit and unchanged save archives/committed sidecars.

## Validation

- `products/first-party/AnimalPack/test-static.ps1` 对仓库真源、本地组装包和本地安装包均通过：严格解析全部 JSON，验证四个 species、七个自定义蛋类物品、七条独立蛋图标键/选定变体/源与规范化输出哈希、七条拆解配方、全部跨表引用、最终周期/阈值/数量、PNG/WAV 引用、四种动物袋身份/价格/标签以及 public `1.00.06 / 24966367_public_958EAF` 的原版物品售价。
- `products/first-party/AnimalPack/build-local-prototype.ps1` 当前组装出 `209` 个文件、`704587` 字节的开发包，其中包含 `181` 张 PNG 和 `8` 个 WAV；部署树与组装树按相对路径、长度和 SHA-256 完全一致。
- `tools/scripts/check-product-catalog.ps1`：PASS，Catalog 识别 `animal-pack` 为 `DTMAPI.AnimalPack / OfficialJsonContentPack / CanonicalLocalPrototypeImplemented`；公开发布仍保持阻塞。
- `GAME-SMOKE/20260827-231124` 在 public `1.00.06`、UI 第七档 / native index `6`“大型畜棚”运行。隔离配置只启用统一 `Local.DTMAPI_AnimalPack` 与第七档依赖 `Local.DTMAPI_MoreSaves`，四个旧输入包均未加载；Runtime 注册四个 species、八个 Animator key、四组 PNG 映射和八个就绪 WAV。用户在畜棚内目视确认 Hatch、Drecko、Mole、Oilfloater 均按预期显示并完成无保存退出。
- 本轮真实交互还额外触发了四种动物的受击/抚摸声音路径。用户怀疑最后两次可能误替换原版沼泽兽声音；日志否定了这个假设：`23:17:33.849`、`23:17:35.032` 和 `23:17:36.134` 的触发实例均为 `title=田鼠 species=mole`，随后才把共享的 `PLAY_ANIMAL_PET_PANGOLIN` 替换为 `mole_pet_young.wav` 并抑制原声。原版沼泽兽 ID 是 `marsh_pangolin`；音频 Hook 从当前 `Animal.PlayAnimalSound` 实例读取 `protoName`，并要求 replacement `speciesId == context.SpeciesId`，因此只共享 Wwise 事件名不足以命中替换。本次日志没有任何 `species=marsh_pangolin` 被替换的记录。
- Smoke runner 最终状态为 PASS，进程正常退出，模组 profile 已恢复为测试前精确 `14473` 字节 / SHA-256 `8F376A93F3DD868FE65EBF445CDC07185BCCE571D77E04D179B320412874C1E0`，共享 Runtime lock 已释放。按用户最新要求，不把存档一致性检查列为本次人工验收要求。
- 当前 Runtime 证据只证明统一包发现、四动物渲染/模板行为和声音作用域；没有实际建造或操作“小动物照料站”，也没有等待普通生产、提交隐藏贡献或领取隐藏产物。因此 Update 保持 `implemented`，不提升为完整 `verified`。

### 2026-08-28：动物袋、凯涅尼木商店与动物售价修正

- 对 public `1.00.06 / 24966367_public_958EAF` 的原生责任路径复核确认：普通 `sack`/`sturdy_sack` 是捕获载体；四个官方预置动物袋均为 `ItemFunctionAnimalPackage`，用 `preset_animal` 创建幼体，用 `ui_sprite_catch` 显示通用装袋图，并从动物阶段数据决定满袋回售价。
- 四个自定义袋的目录图改为官方 `icon_item_sack_full`，标题改为 `麻袋（物种）`；唯一 `preset_animal`、动物标题、默认名字及幼年/成年 `description_in_sack` key 均由静态检查锁定。此修正完成时七种蛋图标仍待独立美术；后续同日已按下一节补齐，没有用袋图代替蛋图。
- 购买/回售价按上表落地。源码与组装包静态检查均 PASS，明确断言每种 `AdultSell == BagBuy * 3`；原有产物直售、照料站增值、周期、阈值和分解数量未改变。
- `mod_tbmodstoreextension.json` 继续使用 canonical `animal_shop`，但把四个条目从无法占用该商店零随机槽的正权重改为固定权重 `0`，并采用官方动物袋季节库存 `20/20/20/1--3`。`default_unlock=true` 让四种 Mod 幼体不依赖原版剧情解锁。
- 在共享 Runtime lock 下，只同步了 `animal_tbanimal.json`、`item_tbitem.json`、`mod_tbmodstoreextension.json` 到本地官方 `MODS/DTMAPI_AnimalPack`。部署前后均为 `202` 个文件，最终安装树与本地组装树逐文件 SHA-256 完全一致；未启动游戏、未改 Mod 启用 profile，锁已释放。
- 这轮仅完成 source/package 验证。凯涅尼木商店可见性、实际购买后幼体阶段、成年捕获回售金额及第四季倍率仍缺新实机证据，因此 Runtime Validation 保持 `partial`。

### 2026-08-28：七种蛋专用图标修正

| AnimalPack item | Dedicated key | Local ONI variant |
| --- | --- | --- |
| `hatch_egg` | `icon_item_hatch_egg` | Hatch Egg `ui` |
| `petrified_hatch_egg` | `icon_item_petrified_hatch_egg` | Metal Hatch Egg `mtl_ui` |
| `drecko_egg` | `icon_item_drecko_egg` | Drecko Egg `ui` |
| `mole_egg` | `icon_item_mole_egg` | Shove Vole Egg `ui` |
| `fertile_mole_egg` | `icon_item_fertile_mole_egg` | Delecta Vole Egg `del_ui` |
| `oilfloater_egg` | `icon_item_oilfloater_egg` | Slickster Egg `ui` |
| `polymer_oilfloater_egg` | `icon_item_polymer_oilfloater_egg` | Molten Slickster Egg `hot_ui` |

- `item_tbitem.json` 的七条 `ui_sprite_asset` 已全部脱离成年动物占位帧。官方 Content 文件夹会按 PNG 文件名注册资源，组装器把七个选定 UI 帧裁掉透明边、等比缩进 `24x24` 主体并居中写为对应的 `28x28 Content/Sprites/icon_item_*.png`。
- `asset-sources.json` 保存每个 item、ONI 变体、相对源路径、目标键、源 SHA-256、规范化参数和输出 SHA-256；组装器拒绝根目录逃逸、目标重复、文件缺失、源哈希漂移或规范化输出漂移。`DTMAPI_ANIMAL_PACK_ICON_INPUT_ROOT` 可覆盖默认的 `E:/DolocTownUnity/ONIExtracted` 根目录。
- 七张最终组装 PNG 已逐张检查：均为透明背景、完整蛋形轮廓并在 28 像素画布内清晰可辨；哈奇普通/金属、田鼠普通/美味、浮游生物普通/高温在小图颜色和外形上可区分。壁虎只有一个普通产物蛋，不需要隐藏变体。
- 在共享 Runtime lock 下，本地安装包只替换 `Content/item_tbitem.json` 并新增七张 PNG；安装树和组装树最终均为 `209` 个文件且逐文件 SHA-256 一致。未启动游戏、未改启用 profile 或存档，锁已释放。
- 本轮只闭合 source/package/deployment 与本地图像检查。Y 控制台、背包详情和小动物照料站中的实际缩放/裁切仍缺新游戏证据，所以 Runtime Validation 保持 `partial`。

### 2026-08-28：照料站视觉几何与蛋提示尺度修正

- Manual QA 截图证明旧七张蛋源图以 `220--264` 像素画布直接进入默认 `8 PPU`，照料站则以 `178x249 / 32 PPU / cover 6x4` 混用两套尺度。用户把子项 1 与 3 明确归并为缩小蛋图，并要求子项 2 同时裁透明边、让设备底部贴地及改为 `2x4`；子项 4 留给新包复测，不在本节修改 worker 状态逻辑。
- 组装器新增确定性 PNG 规范化：逐像素取 alpha 边界，以高质量双三次采样缩小后写入透明目标画布。七张蛋固定 `28x28`、主体不超过 `24x24`、居中、沿用默认 `8 PPU`；照料站固定 `32x48`、主体不超过 `32x46`、bottom-center、底部只留 1 像素。
- `equipment_tbequipment.json` 已改为 `cover_size=2x4`；照料站元数据改为 pivot `(16,1)`、`8 PPU`。public 1.00.06 四个原版 `2x4` 设备的 Sprite rect 为 `25--32` 像素宽、`50--57` 像素高，因此该画布处在原生设备尺度范围；不需要 DTMAPI 坐标 Hook。
- 旧 249 像素画布使原生 `PositionCenter` 位于 base 上方约 `15.3125`；48 像素画布把同一路径恢复为约 `2.75`。这是设备图缩小的伴随修正，配方与掉落 LUT 未增加私有坐标字段。
- `asset-sources.json` 升为 schema 2，并锁定站点源/输出哈希、蛋源/输出哈希、画布、主体边界、对齐、采样、PPU、pivot 与 cover。静态检查读取 PNG alpha，逐张断言画布、主体上限、居中/贴底和输出 SHA-256。
- source 与组装包静态检查均 PASS；最终包保持 `209` 个文件、`181` 张 PNG、`8` 个 WAV，总计 `704587` 字节。七蛋缩小图与照料站缩小图已制作放大审查图并目视确认轮廓可辨，后续仍需用户在游戏内复测真实世界提示、设备落地和第二种单件蛋重启。
- 组装与 source/package 静态检查同时在 PowerShell 7 和 Windows PowerShell 5.1 通过。为避免 5.1 把无 BOM 中文 JSON 当作本地代码页，两个脚本均显式按 UTF-8 读取 JSON，静态读取器也显式展开顶层数组；两种宿主生成的规范化 PNG 哈希一致。
- 在共享 Runtime lock 下，部署前把安装包中七张蛋 PNG、照料站 PNG、照料站元数据和设备表备份到 `.local-build/before-20260828-station-geometry/`，随后同步完整组装包。本地安装包静态检查 PASS，安装树与组装树均为 `209` 个文件、`704587` 字节，按相对路径与 SHA-256 比对为 `missing=0 / extra=0 / mismatch=0`。未启动游戏、未改 Mod profile 或存档，锁已释放。

### 2026-08-28：酸雨结论、缺氧动物包说明与照料站图纸

- 用户完成新的普通等待与 Y 跳时 A/B，二者都能正常完成连续加工。先前的“第二枚到时无产物”发生在跳时跨入酸雨后；照料站配置的原版 `WDP_AcidRain_Worker` 会在酸雨期间接管 `Update/UpdateNoRender` 并暂停原 worker，天气结束后恢复。这是预期机制，不建立 Debug issue，不修改设备 worker、配方完成或掉落代码。
- `manifest.json`、`official-info.json` 与 README 的玩家可见名称改为“缺氧动物包”（繁中“缺氧動物包”、英文 `Oxygen Not Included Animal Pack`），`UniqueID=DTMAPI.AnimalPack`、官方文件夹和内部 species/item ID 均保持不变，因此不引入存档或依赖身份迁移。
- `official-info.json` 现在逐项列出四种动物、普通周期与加工数量、四条隐藏产物与阈值、四种幼体购买价、成年 3 倍售价、凯涅尼木购买位置、照料站图纸/制作/加工方式及酸雨暂停规则，并保留用户指定的示例 Mod、未来教程与 `PNG/WAV/JSON` 作者入口说明。
- 新增 `recipe_animal_care_station`：采用原版 `ItemFunctionRecipe`，购买价 `1000G`，使用后解锁 `animal_care_station`；设备配方从默认解锁改为 `default_unlock=false`。`animal_shop` 与四种幼体袋同架，每季固定一张图纸，使用官方图纸货架形态 `storage=1 / spawn_weight=0 / count=1`。
- 设备制作材料改为 `copper_ingot x5 + wood x100`。这里使用的是 public 1.00.06 物品 ID `wood`（“木头”），不再沿用旧配置的 `wood_stone`（“岩木”）或 `iron_ingot`。
- `test-static.ps1` 新增产品名/说明关键事实、图纸物品、图纸到配方引用、商店固定库存、配方锁定状态和精确材料集断言；仓库真源检查 PASS，并继续验证七条加工经济为约 `49%--52%` 增值。
- PowerShell 7 下的仓库真源、组装包与本地安装包静态检查全部 PASS；Windows PowerShell 5.1 下的仓库真源和组装包检查同样 PASS。新增说明断言仍使用 Unicode code point 构造，避免 5.1 对无 BOM 脚本采用本地代码页造成中文解析失败。
- `build-local-prototype.ps1` 组装结果仍为 `209` 个文件、`181` 张 PNG、`8` 个 WAV；完整包为 `709380` 字节。名称和完整多段说明进入顶层 `info.json`，ContentPack manifest 中的 `Name` 同步为“缺氧动物包”。
- 在共享 Runtime lock 下，部署前把安装包的 `info.json`、manifest、物品表、普通配方表和商店扩展表备份到 `.local-build/before-20260828-name-blueprint/`，随后只同步这五个已变更文件。最终安装树与组装树逐文件长度/SHA-256 比对为 `missing=0 / extra=0 / mismatch=0`，安装包静态检查 PASS。未启动游戏、未改 Mod profile 或存档，锁已释放。

### 2026-08-28：0.6.0 发布介绍与照料站字段审计

- `manifest.json`、顶层 `official-info.json.description` 和简体中文 localized description 已同步为用户给定的 19 行完整介绍；其中明显笔误“田献”按既有 species 与物品名校正为“田鼠”。三处文本完全相等，UTF-8 文本 SHA-256 冻结为 `58582E89D45C8CEBBFDF88DD8832EBED10C4403ED212530CC4FFCCBCC12B8243`。
- 实际最低依赖不只改了展示文字：ContentPack manifest 的 `MinimumDTMApiVersion`、Catalog 的 `sourceMinimumDtmApiVersion`/`targetMinimumDtmApiVersion` 以及两处静态契约均统一为 `0.6.0`。产品版本和稳定身份仍为 `1.0.0 / DTMAPI.AnimalPack`。
- `item_tbitem.json` 的照料站成品已有有效售价配置：`salable=true / selling_price=-1`。public 1.00.06 的 `ItemEquipment.GetSellingPrice()` 把负值解释为按制作配方动态求和；`copper_ingot=50G`、`wood=4G`，所以当前成品基础售价是 `5×50 + 100×4 = 650G`。`buying_price=0` 正确，因为商店出售图纸而不是成品设备。
- 照料站物品行的 `electric_energy=0` 也保持不变。该字段是手持物品携带/产生的电量，不是放置设备功耗；真正的设备配置位于 `equipment_tbequipment.json`，其 `EComProtoAppliance.threshold=1` 对应原生界面的额定耗电 `1`，与用户文案一致。
- 七条 `recipe_tbdismantlerecipe.json` 都是 `cost_time=12`，配方组 `time_ratio=1`。public 1.00.06 的 `TU2Min=5 / Hour2Min=60`，因此 `12 TU = 60 分钟 = 1h`。设备函数 `interval=12`、图纸 `buying_price=1000`、锁定配方、`copper_ingot x5 + wood x100`、动物商店固定图纸及七条普通/稀有蛋输入引用也全部齐备，本节无需修改任何设备/配方/商店玩法数值。
- `test-static.ps1` 新增完整介绍哈希、三处文案一致性、0.6.0 最低依赖、设备物品售价哨兵、650G 动态售价算术、手持物品电量字段、设备电气元件/阈值、七条 1h 配方及 1.00.06 时间换算断言。仓库真源和组装包在 PowerShell 7、Windows PowerShell 5.1 下均 PASS；`check-product-catalog.ps1` 也 PASS（`products=27 / public=11 / workshop-items=22 / api-rows=48`）。
- 新组装包为 `209` 个文件、`710705` 字节。持有共享 Runtime lock 且确认游戏未运行后，只把 `info.json` 和 `Content/DTMAPI/manifest.json` 同步至本地安装包；安装包静态检查 PASS，安装树与组装树逐文件比对为 `missing=0 / extra=0 / mismatch=0`。本节未启动游戏、未改启用 profile 或存档，锁已释放。

### 2026-08-28：发布封面、产出设备名称与三语 Steam 介绍

- 用户指定 `D:/图片/封面图/缺氧动物包.jpg` 为 Steam 大预览图，指定 `D:/图片/封面图/缺氧动物包 - 副本.jpg` 为游戏内 Mod 封面与 Steam 小预览图。两张 JPG 分别以原构图、原尺寸转换为真正的 `preview.png (1549x925)` 与 `icon.png (744x482)`；没有只改扩展名，也没有额外裁切或拉伸。
- `asset-sources.json` 升为 schema 3，记录两张源 JPG 的角色、路径、格式、尺寸与 SHA-256，并同时冻结最终 PNG 的格式、尺寸与 SHA-256。组装器会验证源图、产品根 PNG 和组装输出，拒绝源图漂移、伪 PNG、尺寸变化、哈希变化或非根目录目标。
- public 1.00.06 本地化表核准的简中设备名为 `鸡窝 / 粘毛滚 / 挤奶器 / 蜂箱`，繁中为 `雞窩 / 粘毛滾 / 擠奶器 / 蜂箱`，英文为 `Chicken Coop / Lint Roller / Milking Machine / Hive Box`。其中用户口述的“粘毛器”不是当前正式名称，说明统一采用“粘毛滚”。四个自定义动物的 `schedule_id` 分别复用官方 `chicken / goat / marsh_pangolin / slime`，与上述四套原版产出设备路径一致。
- `manifest.json` 默认说明、`official-info.json.description` 和简中 localized description 同步增加四条“产出设备为……”文本，并在“欢迎大家体验。”后追加“有bug请反馈，喜欢请点好评♥️”。繁中与英文不再使用短摘要，而是各自完整翻译全部 19 行普通产出、设备、隐藏产出、阈值、购买价格、成年售价、图纸、制作与加工说明。
- 三语说明的 UTF-8 SHA-256 分别冻结为简中 `125ACEECE2667F184A8957BF757EAFD5EEC999153D211A6E7DFF5B068A82A026`、繁中 `B2934BD613C59D1F383638CC2C0948AE59C12D734479B0F47735D626B17D1EA7`、英文 `A139839A9BE782DF1F004EF85DCC93009620D953B1431ACAB0BC4557AC43383D`。静态检查还逐种断言 schedule 模板及三种语言中的正式设备名。
- PowerShell 7 与 Windows PowerShell 5.1 下，仓库真源、组装器和组装包静态检查均 PASS。最终本地包为 `211` 个文件、`2299595` 字节、`183` 张 PNG、`8` 个 WAV；`icon.png` 与 `preview.png` 已目视确认构图与指定源图一致。
- 持有共享 Runtime lock 且确认游戏未运行后，把组装包的 `info.json`、`Content/DTMAPI/manifest.json`、`icon.png`、`preview.png` 同步到本地 `MODS/DTMAPI_AnimalPack`。前两项旧文件已备份，后两项部署前不存在。安装包静态检查 PASS；组装树与安装树均为 `211` 个文件、`2299595` 字节，逐文件对比为 `missing=0 / extra=0 / mismatch=0`。未启动游戏、未改启用 profile 或存档，Runtime lock 已释放。
- 用户随后把作者身份校正为 `Yuuka`。`manifest.json.Author` 与 `official-info.json.author` 已同步修改并由静态检查锁定；重新组装后的包为 `211` 个文件、`2299593` 字节。持有 Runtime lock 时只同步这两个元数据文件，本地安装包再次 PASS，且与组装树逐文件对比仍为 `missing=0 / extra=0 / mismatch=0`；未启动游戏、未改 profile 或存档，锁已释放。
- 本节准备并部署的是本地真源和候选包，不代表已获 Workshop 上传授权。Catalog 当前没有该产品的 Workshop ID 或上传授权，且 ONI 动物/蛋/设备素材仍受 AssetProvenance 发布阻塞；因此没有执行 Steam 上传，也没有把“准备好封面”误记为产品可公开发布。

### 2026-08-28：Steam 预览图限额修正

- 用户从游戏内上传界面多次尝试；先后捕获的 `Player-prev.log` 证明至少六次 `CreateItem` 成功后由 `SubmitItemUpdate` 返回 `k_EResultLimitExceeded`：`3791468161`、`3791468267`、`3791468303`、`3791469784`、`3791469844`、`3791469910`。游戏 UI 只显示通用“上传失败”，没有呈现该 Steam 返回码；这些未完成 item 都不是后续更新目标。
- public 1.00.06 上传器会把产品根目录的 `preview.png` 交给 `SteamUGC.SetItemPreview`。原文件为 `1,249,898` 字节，超过 Steam 图片预览必须小于 1 MB 的限制；本包只有约 2.3 MB，且条目创建已经成功，因此预览图是本次失败的直接、可复现限额违反。除非压缩后仍返回同一错误，才需要继续排查 Steam Cloud 配额。
- `preview.png` 在保持 `1549x925`、RGB 像素和构图完全不变的前提下重新压缩为 `849,062` 字节；逐像素比较为相同，SHA-256 更新为 `743206EE3A57402CA20611F6160A738E82E61292EE31400911D866A099ADC96F`。没有缩放、调色板量化或可见画质损失。
- `asset-sources.json` 新增精确 `outputBytes=849062` 与保守的 `maximumBytesExclusive=1000000`，并更新输出哈希；`test-static.ps1` 同时断言声明值、实际字节数和小于上限，防止以后替换封面时重新引入同类上传失败。
- PowerShell 7 与 Windows PowerShell 5.1 下，仓库真源和重新组装包的静态检查全部 PASS。最终组装包与本地安装包均为 `211` 个文件、`1,898,757` 字节，逐文件比对为 `missing=0 / extra=0 / mismatch=0`。
- 完成压缩与部署时尚未再次提交 Steam，也没有删除已创建但提交失败的 Workshop 条目；下一节记录随后发生的单次成功重试与订阅校对。压缩步骤本身未启动游戏、未改 Mod profile 或存档。

### 2026-08-28：Workshop 成功提交与订阅字节闭环

- 新一轮 `Player.log` 明确记录：item `3791474574` 创建成功，基础内容提交成功，随后繁中与英文 localized text 提交成功，最终出现 `Workshop upload success!`；成功回调把 `workshop_id=3791474574` 写入本地 `MODS/DTMAPI_AnimalPack/workshop.json`。
- Steam 客户端已把该 item 下载到 `D:/Steam/steamapps/workshop/content/2285550/3791474574`。`appworkshop_2285550.acf` 的 installed/details 两处均记录 manifest `8336118746041049876`，目录时间为 `2026-08-28 20:40:11 +08:00`。
- 以 Steam 订阅目录作为玩家实际交付物，与仓库 `.local-build/DTMAPI_AnimalPack` 逐相对路径、长度和 SHA-256 比较：两边都是 `211` 个文件、`1,898,757` 字节，结果为 `missing=0 / extra=0 / mismatch=0`。本地 `MODS` 树排除上传成功后生成的 33 字节 `workshop.json` 后也得到完全相同结果；该身份文件未进入首个上传包是正确时序，不是订阅缺失。
- 订阅 `preview.png` 为 `849,062` 字节，SHA-256 `743206EE3A57402CA20611F6160A738E82E61292EE31400911D866A099ADC96F`，与仓库候选相同。订阅目录在 PowerShell 7 和 Windows PowerShell 5.1 下分别通过 AnimalPack `test-static.ps1`。
- 审计结论保存在 `docs/reviews/code/2026/20260828-0001-animalpack-workshop-subscription-parity-audit.md`。当前证据闭合“本机上传成功且订阅字节与候选一致”，但没有审查 item 的公开可见性，也不把绕过 Catalog 发布授权的物理上传解释为 AssetProvenance、重复旧包预检或剩余玩法验收已经通过；Catalog/current-subscription authority 保持不变。

## Evidence

- Pre-implementation review: `docs/reviews/code/2026/20260827-0001-animalpack-economy-tutorial-unification-preimplementation-review.md`.
- Workshop subscription parity audit: `docs/reviews/code/2026/20260828-0001-animalpack-workshop-subscription-parity-audit.md`.
- Latest public reverse baseline: `references/doloc-town/reverse/builds/24966367_public_958EAF/`.
- Current runtime baseline for the four separate inputs: `docs/updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md`.
- Unified-package Runtime evidence: `docs/debug/evidence/GAME-SMOKE/20260827-231124/`; the final Mole/Pangolin-sound observations are retained in `support-context.txt` lines 141--155.
- Runtime smoke matrix: `docs/debug/regressions/smoke-matrix.md`, row `ANIMALPACK-UNIFIED-10006-SLOT7-LOAD-20260827`.

## Rollback Notes

- Before local deployment, retain exact inventories/hashes of the four input packages, the destination path and relevant Mod profile files. Rollback removes only the new `DTMAPI_AnimalPack` local developer package and restores the exact prior profile; it does not rewrite player save archives.
- Source rollback removes the new AnimalPack source and restores the Catalog row to `FrozenReservedNoArtifact` with its original blockers.
- The test profile has already been restored byte-for-byte. The assembled local developer package remains at `LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_AnimalPack` but is not added to the restored enabled-profile map; this preserves a ready local test artifact without leaving the old and new animal packages simultaneously enabled.
- 2026-08-28 三份被替换的安装包 JSON 保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-bag-store-price/`；可在持有 Runtime lock 且游戏未运行时逐文件恢复，不需要改存档或 profile。
- 2026-08-28 蛋图标部署前的 `item_tbitem.json` 保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-egg-icons/`；七张 PNG 在部署前均不存在。回滚时须持有 Runtime lock、确认游戏未运行，恢复该 JSON 并只移除本节列出的七个 `icon_item_*.png`。
- 2026-08-28 视觉几何修正部署前的 `equipment_tbequipment.json`、照料站 sprite 元数据、照料站 PNG 与七张蛋 PNG 保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-station-geometry/`。回滚只应在持有 Runtime lock 且确认游戏未运行时覆盖这些精确文件；不要改 profile 或存档。
- 2026-08-28 改名、完整说明与图纸部署前的五个安装文件保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-name-blueprint/`。回滚时须持有 Runtime lock，并只恢复其中的 `info.json`、`Content/DTMAPI/manifest.json`、`Content/item_tbitem.json`、`Content/recipe_tbrecipe.json` 和 `Content/mod_tbmodstoreextension.json`；不修改其他资产、profile 或存档。
- 2026-08-28 新介绍与 0.6.0 最低依赖部署前的两个安装文件保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-description-060/`。回滚时须持有 Runtime lock，只恢复 `info.json` 和 `Content/DTMAPI/manifest.json`；设备、商店、profile 与存档均不属于本次回滚范围。
- 2026-08-28 封面和三语说明部署前的 `info.json`、`Content/DTMAPI/manifest.json` 保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-branding-l10n/`；`icon.png` 与 `preview.png` 在该次部署前不存在。回滚时须持有 Runtime lock、确认游戏未运行，恢复这两个 JSON，并只移除安装包根目录的这两张新增 PNG；不要改 profile 或存档。
- 2026-08-28 作者改为 `Yuuka` 前的 `info.json` 与 `Content/DTMAPI/manifest.json` 保存在忽略目录 `products/first-party/AnimalPack/.local-build/before-20260828-author-yuuka/`。如需单独回滚作者字段，须持有 Runtime lock、确认游戏未运行，并只恢复这两个文件。

## Follow-Up

- Run a separate interaction acceptance for station placement/filtering, all seven exact dismantle outputs, batch count/full-inventory behavior, four ordinary production cadences, contribution accumulation and hidden output timing.
- 在下一次第七档 NoNativeSave 交互验收中，通过 Y 控制台、普通背包/详情和小动物照料站逐一查看七种蛋，确认无粉色缺图、成年动物占位、裁切溢出或普通/隐藏版本混淆。
- 连续两种单件蛋、普通等待和 Y 跳时已经由用户复测通过；先前停工由酸雨天气处理器解释，状态机 Debug 候选关闭。后续设备验收只需覆盖尚未实测的七条精确产物与酸雨结束后的恢复提示，不再重复无天气差异的第二轮假设。
- 在下一次第七档 NoNativeSave 交互验收中打开凯涅尼木的 `animal_shop`，确认四种通用袋图和唯一名称均可见；至少购买一种后确认生成幼体。成年售价需要用测试生成/已有成年动物装入普通或结实麻袋后查看商店报价，不能用目录中的袋子底价代替。
- Add the old-input/new-pack duplicate preflight before any player-facing distribution; do not rely on authors remembering to disable four predecessor packages manually.
- Replace or relicense every ONI-derived PNG/WAV/station image before public distribution; `AssetProvenance` remains a hard Catalog blocker.
- Use this verified one-package/four-species layout as the advanced example when the separate author-tutorial task begins; tutorial work is not part of this Update.
