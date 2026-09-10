# 四动物经济、作者教程与单包合并预实施审查

## 记录状态

- Status: recorded
- Date: 2026-08-27
- Scope: AnimalPack 经济、单动物作者教程、四动物单包官方 JSON 合并的只读预实施审查
- Source: 用户先提出三个后续事项并要求简单审查，随后按 `1 / 1.5 / 2 / 3` 补充四动物角色、经济基线、独立设备与教程参数问题；再要求把新 public `1.00.06` 与 `1.00.05` 做相关代码/配置复核，并列出矿石与肥料精确 ID；最后给出四条普通/稀有产物树、周期和阈值，命名额外设备为“小动物照料站”，并指定本地 ONI 提取素材位置
- Runtime action: 未安装、启用、禁用或修改本地 Mod；未启动游戏；未读取或修改存档
- Implementation action: 未修改经济数据、作者教程、四个本地动物包或 Runtime；本审查只维护这一份 Review
- Existing current-version evidence: [1.00.06 全量逆向捕获](../../../../reviews/updates/2026/20260827-0002-public-10006-full-reverse-capture.md)、[1.00.05 畜牧接口审查](../../../../reviews/code/api/2026/20260827-0001-livestock-interface-10005-official-content-audit.md) 与 [第七档四动物运行验收](../../../../reviews/updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md)
- Earlier product boundary: [First-Party Animal Pack Product Boundary Review](20260713-0009-first-party-animal-pack-product-boundary-review.md) 与 [AnimalPack Deferred Product Design Draft](20260713-0011-major-update-sixth-decision-docket.md)

## 审查结论摘要

三个方向都可继续，但现在还不应直接冻结物品 ID、数量和售价：

1. “动物按各自原版模板周期产一个自定义产物，再由分解机固定拆成多种、多数量原版物品”在当前原生数据结构中可行；隐藏产物也有独立的原生 JSON 路线。用户本轮已选择新增独立处理设备，因此最终方案应使用独立的分解配方组，不再覆盖现成 `garbage_shredder` 配方组。
2. 现有作者指南技术内容完整，但主教程过长；本地飞书稿已有五张动作帧合成图和实包数据，仍存在过长、读取可变 LocalLow 实包以及解释四个 Runtime 不读取字段的问题。应拆成短教程与字段参考，而不是只继续润色同一篇长文。
3. 四个动物合入一个 ContentPack 在官方表和 DTMAPI 数组模型上都成立。必须保留四个独立 species ID，并把旧四包与新包设为互斥；当前四包同时运行的证据不能自动当成“一个包内四条定义”的验收。

审查按用户的 1 / 2 / 3 顺序展开。实施依赖更适合采用 `3（建立仓库内单包基线） -> 1（在该基线上做经济） -> 2（用已验证实包写教程）`。

## 1. 四动物经济与隐藏产物

### 原始需求

- 四种动物分别生产一个自定义初级产物。
- 初级产物经分解机产出多种、且各有不同数量的物品。
- 当前方向为毛、肉、矿以及一个尚未确定的第四类型。
- 最后为四种动物分别配置高价值隐藏产物。

### 当前本地原型事实

四包沿用各自模板的成年代谢节奏；Hatch、Oilfloater、Mole 的理论最短周期约为 1 天，Drecko 沿用角羊驼的 3 天周期。当前产物价值和动物袋价格只是测试数据：

| 动物 / 模板 | 占用空间 | 周期 | 当前普通产物 | 当前每周期 / 每日直接价值 | 原版模板普通产物 | 模板每周期 / 每日直接价值 | 当前 / 模板动物袋买价 |
| --- | ---: | ---: | --- | ---: | --- | ---: | ---: |
| Hatch / `chicken` | 2 | 1 天 | `meat x1` | 60 / 60 | `egg x1` | 75 / 75 | 800 / 800 |
| Drecko / `goat` | 3 | 3 天 | `wool x1 + meat x1` | 560 / 186.67 | `wool x1` | 500 / 166.67 | 800 / 3000 |
| Mole / `marsh_pangolin` | 4 | 1 天 | `meat x1 + soil x1` | 66 / 66 | `milk x1` | 250 / 250 | 800 / 5000 |
| Oilfloater / `slime` | 2 | 1 天 | `coal x1` | 10 / 10 | `honey x1` | 100 / 100 | 800 / 1200 |

这组数据不能直接当经济基准。Oilfloater 和 Mole 明显低于对应模板，Drecko 略高于模板，而四个动物袋统一为 800，未反映成长时间、占用空间、饲料、生产设备和繁殖价值。

### 分解机路线核实

当前原生分解链路是：

```text
recipe_tbdismantlerecipegroup.json
-> recipe_tbdismantlerecipe.json
-> output_item_spawn_entry
-> item_tbitemspawn.json
```

`recipe_tbdismantlerecipe` 的输出不是普通配方的单个 `output_item`，而是一个物品掉落库。掉落库会先分配各行的最低数量，再处理剩余抽取次数，因此可以得到多个不同物品和各自固定数量。当前订阅实包 `Workshop.3749143385` 已采用这条纯 JSON 路线，例如一条配方固定拆出金矿、铁矿和破烂电线。

需要避免两个误区：

- 官方飞书《04 新增配方》公开的 `recipe_tbrecipe.json + mod_tbmodrecipegroupextension.json` 只扩展普通 `TbRecipeGroup`，不能扩展分解机的 `TbDismantleRecipeGroup`。
- 官方作者文档没有公开分解表教程；这条路线是当前版本源码、原生配置和真实 Workshop 包共同证明的“未文档化官方内容能力”，不是官方承诺的稳定作者接口。

如果直接使用农场现成 `garbage_shredder`，包必须在 `recipe_tbdismantlerecipegroup.json` 中重写完整 `garbage_shredder` 行，保留五条原版 recipe ID 后再加入四条自定义 recipe ID。官方 `ModManager.MergeArray` 对普通表按 `id` 整行替换，所以另一个 Mod 若也声明 `garbage_shredder`，最终只有后加载的完整行有效。这是公开发布前必须处理或明确提示的兼容风险。

第一版不要把动物产物加入 `garbage_shredder_city` / `garbage_shredder_city_upgrade`。城市设备的 `time_ratio=0.0835`，第三方包曾因 `cost_time=1` 得到零工作间隔，造成投入物留在设备库存并可随存档持久化。农场 `garbage_shredder` 的 `time_ratio=1` 不具有这个特定零间隔问题，但仍需实际测试批量、掉落和重载。

用户本轮已经选择给 AnimalPack 定义独立的“动物产物处理设备”，复用原生 `EquipmentFuncGarbageShredder` 和独立配方组。它能避免覆盖共享 `garbage_shredder` 行，但会增加设备物品、设备配置、建造配方、贴图、供电和放置验证；现有源码只证明链路可达，尚无 AnimalPack 实包运行验收。第一轮探针也应直接走独立设备，避免先为共享组写一套随后丢弃的数据。

### 四种产出定位

用户已明确冻结四个普通产出角色：Hatch 产矿、Drecko 产毛、Mole 产肉、Oilfloater 产燃料。它取代 2026-07-13 草稿中的 Hatch 食物与 Mole 土壤方向；旧草稿继续保留为历史决策证据，但不再是当前产品语义：

| 动物 | 建议普通产出角色 | 与当前原型的关系 | 分解结果方向（尚不冻结数量） |
| --- | --- | --- | --- |
| Hatch | 矿物 | 从当前测试用 `meat` 改为地下矿物身份 | 铜、铁为稳定项；金矿只进入隐藏产物 |
| Drecko | 毛 / 纺织 | 保留 `wool` 主体并删除普通肉类重心 | 羊毛为主，允许低价值有机副产物 |
| Mole | 肉类 | 保留当前 `meat`、移除 `soil` 重心 | 肉为主，允许少量肥料类副产物 |
| Oilfloater | 燃料 / 化工 | 延续当前 `coal` 方向 | 煤、树脂、橡胶或其他已审查燃料/工业材料 |

这里只冻结“角色”，不冻结公开物品名、英文 ID、数量或价格。Hatch 的普通池以铜/铁等稳定资源为主，把金矿或特殊矿核留给隐藏产物。

### 隐藏产物路线

四种隐藏产物可以直接在一个 `animal_tbhusbandry.json` 中各写一条 species 行。每条行引用一个自定义高价值物品，并拥有独立数量、阈值和贡献来源限制。

第一版若沿用原版可贡献食物，**不需要**新增 `animal_tbhusbandryenergy.json`：`limited_contributions=[]` 会读取原版整张贡献表。若只允许某几种现有食物，则在 `limited_contributions` 中列出这些现有食物 ID。只有需要新增一种全局可贡献食物时，才应向 `animal_tbhusbandryenergy.json` 添加新的唯一 ID；不要覆盖原版食物行，因为那会改变所有使用该贡献表的动物。

默认饲料槽只补普通饲料能量，不增长隐藏进度。隐藏产物验收必须走会把具体物品 ID 传给动物的吃食路径，并等到下一次普通生产时确认隐藏产物被追加；只看 JSON 成功加载不算有效验证。

### 经济模型边界

经济表至少同时计算：

```text
每日普通产物数
× 单个产物处理后固定总价值
÷ 动物占用空间
- 饲料 / 能量 / 设备时间成本
```

并单独列出：动物袋成本、幼体到成年时间、繁殖出售、直接出售底价、处理后价值、隐藏进度阈值、每种食物贡献、隐藏产物长期日均价值和回本天数。

早期 AnimalPack 决策记录曾选定“稳定回报约为原版比较对象的 2--3 倍，含隐藏产物的长期回报约为 3--5 倍”作为待模拟目标。它可以继续作为测试带，但不能直接变成售价：四种模板空间不同，隐藏贡献又取决于玩家喂食选择。隐藏物应比较 `隐藏物价值 / 阈值`，再乘实际日均贡献，而不是只看单件售价是否够高。

推荐保留“低价直接出售出口 + 处理后溢价”的原则：初级产物不应同时拥有接近完整处理价值的售价，否则玩家可以跳过设备；也不应完全无出口而让未解锁设备的玩家卡死。具体折扣和溢价在模拟后冻结。

### 当前判断、阻塞条件与验收

- 判断：技术路线可行，四种角色与独立处理设备方向已经明确；经济数值可进入第一版模拟，但尚未具备公开冻结条件。
- 未证明：一个真实 AnimalPack 包中的四条分解配方、四条隐藏产物、固定多输出、特殊喂食、批量处理和存档重载。
- 阻塞条件：独立处理设备尚无实包验收；尚无仓库内 AnimalPack 真源；新产物图标/名称和现有 PNG/WAV 的公开来源仍未闭合。
- 后续自动检查：严格 JSON 解析、所有跨表 ID、掉落最小数之和与 `count_range`、直接/处理价值、配方组保留的原版 ID、`round(time_ratio * cost_time) >= 1`。
- 后续游戏验收：普通产物一次只占模板设备一个物品单位；四种产物分别进入处理设备并得到精确结果；批量和设备满载不丢物；四种特殊喂食分别增长正确进度；隐藏产物仅在阈值达成后的生产追加；退出重进和禁用/恢复包不产生复制、丢失或卡机。

## 2. 完善单一动物作者教程

### 原始需求

- 当前单动物教程不够直观、好看。
- 希望接近本地官方飞书文档的表达和视觉结构。

### 当前文档事实

当前规范说明 [custom-animal-json-png-wav.md](../../../../../author-docs/content-packs/custom-animal-json-png-wav.md) 共有 1159 个物理行，覆盖最小树、四模板帧数、九步 JSON、隐藏产物、设备容量、模板切换、FAQ、手测和发布检查。内容准确且适合作为参考手册，但新作者需要在同一长页中穿过基础、进阶、边界说明和排错，主线不够突出。

本地官方飞书《03 小动物》导出页只有 53 个物理行，主要采用：

- `一、格式要求 / 二、命名对照 / 三、参考示例` 的短编号结构；
- 小型表格及其渲染截图；
- 图片紧跟说明；
- 示例路径与独立 ID 对照页链接。

本地 [新增养殖动物底稿](../../../authoring/2026/new-farm-animal-draft/README.md) 已经具备可复用资产：哈奇实包数据、五张动作帧合成图、ID 对照表、复制顺序和飞书富文本 HTML。但生成预览仍有 683 个物理行和十段完整 JSON，仍然更接近“视觉化参考手册”而不是短教程。

### 当前飞书稿中的准确性缺口

生成器和预览目前把以下字段解释为会连接或改变运行行为：

```text
movementMultiplier
metabolismMultiplier
packageItemId
shopItemListId
```

当前 Runtime 实际不读取这四个字段。规范指南已经正确写明：移动/代谢来自 `animal_tbanimal.json`，动物袋来自 `item_tbitem.json`，商店来自 `mod_tbmodstoreextension.json`。因此生成器、预览、富文本 HTML 和关键 ID 表必须同步删除这些字段，或明确标成历史原型字段且不建议作者填写。

生成器默认从 `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets` 读取内容。LocalLow 是可变安装/测试状态，不是文档真源；以后本地测试包一变，教程就可能无审查地漂移。生成器应改读仓库内、经过验证的单动物示例包或冻结 fixture。

官方文档为了讲解会在 JSON 中保留 `//` 注释，但也明确要求复制时删除。DTMAPI 教程应同时提供：

- 带中文注释、不可直接保存的“讲解版”；
- 无注释、可被严格解析和直接复制的“干净示例文件”。

不能只提供带注释代码块并让初学者自己清理十个文件。

### 推荐信息结构

不要把现有 1159 行参考手册全部塞回一篇更漂亮的飞书页。推荐拆成三层：

1. **新增一只动物：10 分钟快速教程**
   - 用仓库内 Hatch 示例；
   - 一张文件树、一张 ID 关系表、五张动作帧图；
   - 正文只展示最关键的短片段，完整干净 JSON 通过示例路径打开；
   - 末尾只保留启动前 8--12 项检查和最短游戏手测。
2. **字段与模板参考**
   - 保留四模板帧数、`sprite_size`、左右翻转、设备容量、全部字段和 FAQ；
   - 由当前长指南演化，不要求新作者从头读完。
3. **进阶案例**
   - 普通产物、隐藏产物、分解加工；
   - 一个包增加多种动物；
   - 同模板混养与产物设备容量。

### 当前判断、阻塞条件与验收

- 判断：不缺内容，缺的是分层、可靠示例源和生成器纠错；现有五张动作帧图可以直接成为新短教程的视觉骨架。
- 阻塞条件：仓库内尚无经过本轮合并/经济验证的单包真源；在真源确定前继续从 LocalLow 自动生成会重复漂移。
- 后续验收：所有干净 JSON 逐文件严格解析；所有教程 ID 通过跨表检查；生成预览和 HTML 不再宣称四个无效字段生效；五张图与帧清单一致；链接和示例路径可用；一名只读快速教程的作者可以完成最小包，而不必先读完整参考手册。

## 3. 将本地四个动物改为一个 Mod，并作为多动物案例

### 原始需求

- 尽量按照官方 JSON 写法，把本地四种动物合并成一个 Mod。
- 形成“一个 Mod 增加多种动物”的真实案例，并加入作者教程。

### 当前包与合并能力事实

四个本地包的物种、动物袋、产物 LUT、animator key、PNG 前缀、WAV 文件名和 audio replacement ID 都互不相同。它们共同使用的 `mod_tbmodstoreextension.id=animal_shop` 不是普通表冲突：文件名包含 `mod_tbmod`，官方加载器会追加这些扩展行，随后逐项写入商店。合并后应把它们整理成一条 `animal_shop` 行、四个 `extra_items`，更清楚也更容易验证。

官方普通配置表本身都是数组，一个文件可以包含多行；DTMAPI `custom-animals.json` 由 `CustomAnimalDefinitionModel[]` 读取，同一 owner generation 会依次建立多条 animator 与 AI 注册。因此单包结构在源码层成立：

```text
AnimalPack/
|- info.json
`- Content/
   |- animal_tbanimal.json                 # 4 行
   |- animal_tbanimaldocument.json         # 4 行
   |- item_tbitem.json                     # 4 动物袋 + 8 个产物 + 1 个处理设备
   |- item_tbitemspawn.json                # 4 普通产物池 + 8 个处理结果池
   |- mod_tbmodstoreextension.json         # 1 个 animal_shop 扩展，含 4 项
   |- animal_tbhusbandry.json              # 4 行
   |- equipment_tbequipment.json            # 1 个独立处理设备
   |- recipe_tbrecipe.json                  # 处理设备建造配方
   |- mod_tbmodrecipegroupextension.json    # 追加设备工作台配方
   |- recipe_tbdismantlerecipe.json         # 8 行普通/隐藏产物处理配方
   |- recipe_tbdismantlerecipegroup.json    # 1 个独立处理配方组
   |- Sprites/                              # 4 个命名空间化帧清单与 PNG
   |- Audio/                                # 8 个命名空间化 WAV
   `- DTMAPI/
      |- manifest.json                     # 1 个新 UniqueID
      |- custom-animals.json               # 4 行
      `- audio-replacements.json            # 8 行
```

用户现已选择独立动物产物处理设备，所以上述文件属于目标结构；但它们仍只是结构草图，不能提前伪装成已经过游戏验收的最小包。

### ID 与存档迁移边界

新包应有一个新的 ContentPack `UniqueID`，但为保持本地第七档中现有动物可解析，应保留至少这些内容身份：

- `hatch`、`mole`、`drecko`、`oilfloater` species ID；
- 四个 `sack_*`；
- 四个当前 animator key、PNG 前缀和音频 scope；
- 当前 document ID，以及没有必要迁移时的 produce LUT ID。

新经济可以让原 produce LUT 指向新的自定义普通产物；不要为了“合成一个包”同时重命名物种。原生存档以 animal proto ID 解析动物，缺少旧 ID 的中间启动或保存窗口会让迁移不可接受。

旧四包与新合包不能同时启用。它们会声明相同 species、动物袋、产物 LUT、animator key 和音频 scope；普通官方表后加载行会整行替换，DTMAPI owner generation 也会拒绝重复 animator/species。迁移必须在游戏退出时原子切换：先验证新包完整，再启用新包并同时禁用旧四包。Runtime 不应自动删除玩家旧包。

“四个独立包在 1.00.05 同时正常显示”只证明多 owner 能共存；“Hatch 与 Mole 分别复用 chicken 模板”只证明独立 species 可共享模板。两者都没有证明一个 owner 文件内四行定义、四行官方表合并、单一商店扩展和一次 owner generation 的最终行为，必须补一次专门验收。

### “尽量官方 JSON”应如何理解

应由官方 JSON 拥有：动物数值、动物袋、商店、图鉴、普通/隐藏产物、分解配方、掉落数量、价格和文本。DTMAPI 文件只保留当前官方内容系统表达不了的三处桥接：

- `manifest.json`：ContentPack 身份；
- `custom-animals.json`：新 species 到原版 Animator / AI 模板与自有 PNG 前缀的映射；
- `audio-replacements.json`：按 species/stage 隔离的 WAV 替换。

这是一条“官方 JSON 为主体、DTMAPI 补新物种动画/AI/声音缺口”的路线，不应在教程中宣传成无需 DTMAPI 的纯官方新动物包。

### 当前判断、阻塞条件与验收

- 判断：单包合并是可实施的正确产品方向，也是多动物作者案例的合适真源。
- 阻塞条件：仓库内还没有统一包源；新 ContentPack 身份未冻结；旧/新包互斥预检未实现；PNG/WAV/角色设计的公开再分发来源仍是发布阻塞，不影响本地技术验收。
- 静态验收：一个包内所有 JSON 严格解析；四 species、四 bag、四普通产物、四隐藏产物和八个声音 scope 均唯一；四份帧清单完整；商店扩展含四项；没有 DLL；旧四包同时启用时明确阻止安全游戏或给出可执行迁移指引。
- 游戏验收：先对第七档做 `NoNativeSave` 加载/显示/动画/声音/四动物身份检查，并在外部恢复前证明存档归档未变；涉及普通产物、隐藏进度、分解批量和正常保存/重载时，使用与 Steam AutoCloud 隔离的 disposable fixture，不能把实时存档事后拷回当隔离。
- 教程准入：只有单包完成静态检查和当前版本运行验收后，才能成为“一个 Mod 增加多种动物”的正式案例；此前只能标为草稿。

## 2026-08-27 继续研究：经济基线、独立设备、像素与可配置参数

本节按用户最新的 `1 / 1.5 / 2 / 3` 顺序记录。本节只更新审查事实与第一版数值草案，不授权修改本地四包、教程、Runtime 或存档，也不把源码可达性冒充成游戏验收。

### 1. Hatch 产矿、Drecko 产毛、Mole 产肉、Oilfloater 产燃料，并设计隐藏产物

用户要求：四只动物的普通产出角色固定为 Hatch 矿物、Drecko 毛料、Mole 肉类、Oilfloater 燃料；四只动物还要各有一种高价值隐藏产物。

审查分析：角色已经足够清楚，可以先给出一套**数值模拟用 V0**。V0 保留当前模板周期，不同时改生产频率；普通自定义产物直接售价约为处理后固定价值的三分之二，使用独立设备处理后获得约 44%--50% 溢价。这样有未解锁设备时的出售出口，也不会让直接出售等同完整加工。

当前 1.00.05 原版物品出售价值用于下表计算：`copper_ore=12`、`iron_ore=40`、`gold_ore=300`、`glass=50`、`wool=500`、`woolen_cloth=700`、`wool_grease=1000`、`meat=60`、`jerky=200`、`fertilizer=25`、`coal=10`、`resin=30`、`rubber=50`、`engine_core=500`。

| 动物 | 普通自定义产物暂名 | 周期 | 直接售价 | V0 固定处理结果 | 处理总价值 | 每日处理价值 | 相对模板普通产出的倍率 | 加工溢价 |
| --- | --- | ---: | ---: | --- | ---: | ---: | ---: | ---: |
| Hatch | 矿质结核 | 1 天 | 120 | `iron_ore x3 + copper_ore x5` | 180 | 180 | `180 / 75 = 2.40x` | +50.0% |
| Drecko | 壁虎绒簇 | 3 天 | 700 | `wool x2 + fertilizer x2` | 1050 | 350 | `1050 / 500 = 2.10x` | +50.0% |
| Mole | 田鼠肉囊 | 1 天 | 420 | `meat x9 + fertilizer x3` | 615 | 615 | `615 / 250 = 2.46x` | +46.4% |
| Oilfloater | 浮油燃囊 | 1 天 | 180 | `coal x10 + resin x2 + rubber x2` | 260 | 260 | `260 / 100 = 2.60x` | +44.4% |

这组普通结果有三个刻意限制：Hatch 普通池不放金矿；Drecko 不再把肉当主收益；Oilfloater 不依赖另一 Oil 产品拥有的 `crude_oil` 身份。每次处理的总掉落单位分别为 8、4、12、14，仍需游戏内验证掉落节奏，但没有把几十个低价物品塞进一次普通处理。

隐藏产物 V0 采用“单个高价值自定义物品 + 同一设备可加工”的一致规则：

| 动物 | 隐藏产物暂名 | V0 阈值 | 直接售价 | V0 固定处理结果 | 处理价值 | 单次掉落单位 | 处理价值 / 阈值 |
| --- | --- | ---: | ---: | --- | ---: | ---: | ---: |
| Hatch | 熔金晶核 | 100 | 750 | `gold_ore x3 + iron_ore x3 + glass x2` | 1120 | 8 | 11.20 |
| Drecko | 虹彩绒膜 | 240 | 2250 | `wool x2 + woolen_cloth x2 + wool_grease x1` | 3400 | 5 | 14.17 |
| Mole | 珍馐脂腺 | 120 | 1200 | `jerky x7 + meat x5 + fertilizer x4` | 1800 | 16 | 15.00 |
| Oilfloater | 高能油核 | 100 | 800 | `engine_core x2 + coal x10 + resin x3` | 1190 | 15 | 11.90 |

这些名称、ID、阈值和数量仍是模拟输入，不是公开冻结值。尤其 `engine_core` 可能提前绕过工业进度；Oilfloater 隐藏结果必须再查实际解锁与配方消耗，必要时换成等价的电池、橡胶、树脂组合。Drecko 和 Mole 当前是 `manual_metabolism=true`：原生代码在特殊产物生成后把对应隐藏进度直接重置为 0；Hatch 和 Oilfloater 则减去阈值并保留溢出。Drecko 的 240、Mole 的 120 暂时选择为常见贡献 10 与 24 的公倍数，目的就是减少手动动物在牧草/龙舌兰路线上的溢出浪费。

V0 不把“高价值”只理解为单件售价。最终经济表仍应同时列出 `处理价值 / 阈值`、每次进食贡献、实际进食频率、下一次生产门槛、占用空间、设备时间与动物袋回本期。没有真实喂食频率前，不应宣称隐藏产物的固定日收益。

### 1.5. 当前四种生物每天稳定产出多少直接价值，加工后提升多少

用户要求：用 Wiki 和本地 1.00.05 数据核对四个原版模板的稳定产出、每日直接价值及加工增值，再据此设计数值。

审查分析：Chrome 中查阅的 [立尾雉](https://doloctown.huijiwiki.com/wiki/立尾雉)、[变形蜜虫](https://doloctown.huijiwiki.com/wiki/变形蜜虫)、[角羊驼](https://doloctown.huijiwiki.com/wiki/角羊驼)、[沼泽兽](https://doloctown.huijiwiki.com/wiki/沼泽兽) 页面与本地 1.00.05 表一致：立尾雉、变形蜜虫、沼泽兽理论周期为 1 天，角羊驼为 3 天；占用空间分别为 2、2、3、4。Wiki 还明确说明，巢穴满、缺生产条件或手动产物未采集时，生产周期会停滞。因此下表是成年、能量与心情满足、生产设备可用且及时采集时的**理论稳定上限**。

| 原版动物 | 基础产物 | 周期 | 每周期直接价值 | 每日直接价值 | 每日每空间直接价值 | 可干净归因的单原料加工 | 加工后每日价值 | 售价增幅 |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| 立尾雉 | `egg x1` | 1 天 | 75 | 75 | 37.50 | `egg -> century_egg` | 200 | +166.7% |
| 变形蜜虫 | `honey x1` | 1 天 | 100 | 100 | 50.00 | 无单原料出售加工；蛋糕/爆米花还要其他原料 | 不单独归因 | 总原料售价约 +12% |
| 角羊驼 | `wool x1` | 3 天 | 500 | 166.67 | 55.56 | `wool -> woolen_cloth` | 233.33 | +40.0% |
| 沼泽兽 | `milk x1` | 1 天 | 250 | 250 | 62.50 | `milk -> yogurt` | 550 | +120.0% |

“最高单原料售价增幅”不是净利润：上表没有扣配方解锁、加工时间、设备占用或机会成本；它只回答原料直接卖与成品直接卖之间的价差。立尾雉还有更低的煎蛋路线，沼泽兽还有奶酪（物品 ID `chess`）的 +50% 路线，不能只拿最高值当所有玩家的常态。

当前四个本地测试包若继续使用原产物，其可归因加工上限如下：

| 本地动物 | 当前周期 | 当前产物直接价值 / 周期 | 直接价值 / 天 | 可归因加工 | 加工价值 / 周期 | 加工价值 / 天 | 增幅 |
| --- | ---: | ---: | ---: | --- | ---: | ---: | ---: |
| Hatch | 1 天 | 60 | 60 | `meat -> jerky` | 200 | 200 | +233.3% |
| Drecko | 3 天 | 560 | 186.67 | `wool -> woolen_cloth` 且 `meat -> jerky` | 900 | 300 | +60.7% |
| Mole | 1 天 | 66 | 66 | `meat -> jerky`，土块仍按直接售价计 | 206 | 206 | +212.1% |
| Oilfloater | 1 天 | 10 | 10 | 煤主要是燃料和多原料配方投入，无法单独归因 | - | - | - |

这也纠正了旧审查中的“一天一产”概括：Drecko 的 560 是三天价值，不是每日价值。当前 Hatch、Mole、Oilfloater 的测试产物又会被肉干的异常高单原料售价放大，不能用这些加工上限直接拟合新经济。V0 改用统一的约 45%--50% 设备溢价，并把最终普通回报控制在对应模板基础产出的约 2.1--2.6 倍，更容易解释和测试。

### 2. 新设计一个设备处理畜产品，并复核怪物素材分解 Mod 与旧故障

用户要求：不继续修改现成垃圾分解机，设计一个新设备；复核本地怪物素材分解 Mod 和此前克朗/朗克城市分解机故障审查。

审查分析：本机当前订阅包 `D:\Steam\steamapps\workshop\content\2285550\3749143385` 的 2026-08-06 文件与 7 月审查时的内容已经不同。当前 `recipe_tbdismantlerecipegroup.json` 只整行声明农场 `garbage_shredder`，保留 5 条原版配方并加入 13 条怪物素材配方；不再声明两个城市组。此前 [城市分解机故障审查](../../manual-qa/2026/20260713-0002-garbage-shredder-last-run-log-review.md) 对应的是当时会把 13 条 `cost_time=1` 配方同时放入城市组的字节，`RoundToInt(0.0835 * 1)=0` 才造成设备库存卡住。旧根因仍有效，但不能再描述成当前订阅包状态。

独立设备的纯官方内容链路已经由当前表结构与设备工厂证明：

```text
item_tbitem.json                         设备道具
equipment_tbequipment.json               EquipmentFuncGarbageShredder
recipe_tbrecipe.json                     建造配方
mod_tbmodrecipegroupextension.json       追加到 equipment_workbench
recipe_tbdismantlerecipegroup.json       独立分解组
recipe_tbdismantlerecipe.json            8 条普通/隐藏产物配方
item_tbitemspawn.json                    多种、多数量固定结果
PNG                                      UI 图标、场景贴图，可选工作帧
```

本地官方飞书 [《08 综合案例三（新增设备）》](../../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/047_08%E7%BB%BC%E5%90%88%E6%A1%88%E4%BE%8B%E4%B8%89%EF%BC%88%E6%96%B0%E5%A2%9E%E8%AE%BE%E5%A4%87%EF%BC%89_P57twsINsitUu6kWCDEc4vz6nNc.md) 公开了前四项中的设备/建造主链；独立 `TbDismantleRecipeGroup` 三表则仍是源码、原版配置和真实 Workshop 包共同证明的未文档化内容能力。

设备 V0 暂定：

- 暂名“畜产分离机”，暂定 ID `animal_product_separator`；名称和 ID 不冻结。
- `function.$type=EquipmentFuncGarbageShredder`、`in_farm=true`、独立 `recipe_group_name=animal_product_separator`。
- 配方组 `time_ratio=1`；所有普通/隐藏配方先用 `cost_time=12`，保证工作间隔为 12 TU，不进入零间隔路径。
- 第一轮 `max_craft_count=5`，而不是照搬当前 Workshop 包的 20。原生掉落实现在每个单位之间等待 200 ms，单次多产物与大批量会叠加出很长的掉落序列。
- 第一轮沿用原版农场垃圾分解机的 `cover_size=6x4`、供电阈值 1 和静态渲染结构；贴图通过后再按用户找到的素材调整占地与画布。
- 建造配方通过 `mod_tbmodrecipegroupextension.json` 追加到 `equipment_workbench`，不整行覆盖原版普通配方组。

一个必须公开写明的继承行为：原生 `GarbageShredder.ContentFilter` 除了接受独立组中的配方物品，还会接受所有可出售的 `ItemEquipment`，并按建造材料与 `recycle_factor` 回收。纯 JSON 没有“只允许八种畜产品”的过滤字段。若设备仍复用这个原生类型，它就是“以畜产品为主、仍能回收设备”的处理机；若必须严格拒绝其他设备，那将不再是纯官方 JSON 能力，需要另立 ProductNative 方案，不能偷偷塞进 GameBridge。

新设备本身可以只靠官方 JSON 与 PNG 构造，不需要 DTMAPI。AnimalPack 整体仍需要 DTMAPI，因为新增 species 的模板动画 / AI 映射和按 species 隔离的 WAV 不是当前官方纯内容路线所能完整表达的。

### 3. 教程补齐像素、判定范围、隐藏贡献、周期产物与 DTMAPI 文件识别

用户要求：教程要说明图片像素与识别范围；确认范围是否由 DTMAPI 注册或硬编码；说明四倍面积沼泽兽是否正常；说明隐藏贡献表、鸡 60 / 沼泽兽 120、普通周期/产物能否自定义；列出所有非官方 DTMAPI 文件格式及识别方式。

审查分析：这里实际上有三套互相独立的“大小”，教程必须画成一张对照图，而不能继续只说“图片尺寸”。

#### PNG 像素、世界显示大小与碰撞判定

当前官方 Mod 图片加载器会用 Point 过滤读取 PNG，默认 `pixels_per_unit=8`。以 `sprite_` 或 `anim_` 开头、且没有同名元数据的 PNG，默认使用接近底部中心的锚点。作者也可以为单张 PNG 放一个**同文件名、不同扩展名**的 JSON 元数据：

```json
{
  "pivot": { "x": 16, "y": 2 },
  "pixels_per_unit": 8
}
```

这个同名 JSON 是官方 `ModSpriteMeta`，不是 DTMAPI 的 `*_frame_manifest.json`。前者实际改变锚点/世界尺寸；后者目前只供作者、工具和生命周期诊断阅读，不会枚举或补齐帧。

| 量 | 所有者 | 实际作用 |
| --- | --- | --- |
| PNG 宽高 / `pixels_per_unit` | 官方图片加载器 | 决定画面在世界里显示多大；默认 8 像素 = 1 世界单位 |
| `levels[*].sprite_size` | 官方 `animal_tbanimal.json` | 原生计算 `BoxCollider2D.size = sprite_size * 0.125`，并计算表情气泡偏移 |
| `size` | 官方 `animal_tbanimal.json` | 地面占位宽度、寻路与可站平台宽度相关 |
| `space` | 官方 `animal_tbanimal.json` | 畜棚容量消耗 |

因此答案不是“DTMAPI 硬编码了识别范围”。DTMAPI 的 PNG 桥只把模板帧名前缀映射到自定义 PNG，并调用官方 `LoadSpriteFromFile`；它不读取或钳制 `sprite_size`。固定的 `0.125` 换算系数在游戏原生代码中，具体 `sprite_size`、`size`、`space` 都由作者的官方 JSON 定义。

“沼泽兽四倍面积”还要先区分含义：面积四倍通常是宽、高各两倍；宽、高各四倍则是面积十六倍。

- 若把 PNG 宽高各放大两倍、同时把 `pixels_per_unit` 从 8 改成 16，世界显示大小不变，只是像素分辨率提高；`sprite_size` 可以保持原值。
- 若把 PNG 宽高各放大两倍、仍用 8 PPU，画面面积会变为四倍；若 `sprite_size` 不变，会出现“大画面、小交互框”。
- 若同时把 `sprite_size` 的 x/y 各放大两倍，碰撞/选中核心框面积也会变为四倍，源码没有 DTMAPI 上限阻止它加载。

“能加载”仍不等于“按预期正常”。沼泽兽模板原版成年 `sprite_size=38x29`、`size=3`、`space=4`。做成 76x58 的交互框后，还需决定地面宽度是否从 3 增长、畜棚容量是否增长，并测试选中、抚摸、受击、吃饭、睡觉、手动采集、转向、平台边缘、动物互相遮挡和屏幕边缘。教程应把四倍体型列为高风险进阶案例，不应写成“只要加载上就正常”。

#### 鸡 60、沼泽兽 120 与隐藏贡献能否自定义

用户的记忆是准确的，但对应早期版本，不是当前 1.00.05：

| 本地反编译构建 | 立尾雉 | 变形蜜虫 | 角羊驼 | 沼泽兽 |
| --- | ---: | ---: | ---: | ---: |
| 最早保存的 `23762374_public_C416D4` | 60 | 45 | 100 | 120 |
| 当前 1.00.05 `24788406_public_F06183` | 50 | 35 | 80 | 100 |

这些值来自官方 `animal_tbhusbandry.json.husbandry_datas[*].threshold`。DTMAPI 的 AnimalHusbandryProgress 产品会枚举当前原生 `HusbandryDatas` 并调用原生 `TryGetThreshold`，没有把 60、120 或当前 50、100写死。因此自定义 species 可以自行定义：

- `output`：隐藏产物物品 ID；
- `output_range`：一次数量范围；
- `threshold`：达到多少贡献后可随下一次普通生产一起产出；
- `limited_contributions`：空数组表示读取整张全局贡献表，非空数组表示只接受列出的食物 ID。

`animal_tbhusbandryenergy.json` 也是官方可合并表；每行用食物物品 ID 定义 `energy` 与 `contribution`。可以新增一个唯一食物 ID，也可以覆盖原版 ID，但覆盖原版行会全局改变所有使用它的动物，教程应明确标为兼容风险。仅写一行贡献数据不会让动物凭空学会吃新物品；仍需该物品走到会调用 `Animal.Eat(..., itemId)` 的原生放牧/作物路径。默认饲料槽传入空物品名，只补能量，不增加隐藏进度。

贡献值是绝对点数，不是固定百分比。当前 `alfalfa.contribution=10`：对阈值 50 的鸡是 20%，对阈值 100 的沼泽兽是 10%。教程里的百分比必须按 `contribution / threshold` 计算，不能在贡献表里硬写“鸡 20%”。

#### 普通产出周期和普通产物能否自定义

可以，且都属于官方 JSON，不属于 DTMAPI 参数：

- `metabolism_interval`：每隔多少 TU 尝试增长一次生产进度；
- `metabolism_increase`：每次增加多少；
- `metabolism_cost`：每次增长消耗多少能量；
- `produce_require_mood`：生产心情门槛；
- `produce_spawn_entry.spawn_lut` 与 `count_range`：普通产物掉落库与总抽取数量；
- `item_tbitemspawn.json.spawn_datas`：候选物品、权重、最小和最大数量；
- `manual_metabolism`、`schedule_id` 与模板 AI：决定手动采集还是寻找原版巢穴/设备的行为路线。

当前模板的 `metabolism_increase=1.04167`，约 96 次增长达到 100；游戏每天 288 TU，所以 `interval=3` 约为 1 天，`interval=9` 约为 3 天。作者可以改，但周期到达后仍会受能量、心情、手动采集和设备容量约束；教程应称“理论最短周期”，不要承诺自然日必产。

#### 官方文件与 DTMAPI 文件如何被识别

| 文件 / 资源 | 解析者 | DTMAPI 是否读取 | 识别规则与当前作用 |
| --- | --- | ---: | --- |
| 包根 `info.json` | 游戏官方 ModManager | 否 | 游戏发现并加载内容 Mod 的入口 |
| `Content/animal_*.json`、`item_*.json`、`recipe_*.json`、`equipment_*.json`、`mod_tbmod*.json` | 游戏官方表加载器 | 否 | 按官方固定文件名选表，数组中的 `id` 行被追加或整行替换 |
| `Content/DTMAPI/manifest.json` | DTMAPI Core | 是 | 建立 ContentPack owner；`Type=ContentPack`、`EntryDll=""`、唯一 `UniqueID`、最低 DTMAPI 版本 |
| `Content/DTMAPI/custom-animals.json` | DTMAPI GameBridge | 是 | 严格 JSON 数组；读取 species/template/AI、动画模式、成年/幼体 animator key、帧清单路径及模板/自定义 Sprite 前缀 |
| `Content/DTMAPI/audio-replacements.json` | DTMAPI GameBridge | 是 | 严格 JSON 数组；按 `speciesId + stage + nativeSoundEvent` 作用域加载包内短 WAV |
| `Content/Sprites/*_frame_manifest.json` | 当前主要由作者/工具使用 | 只记录路径/诊断，不解析帧表 | 不决定实际 PNG 映射，不会按 `frame_count` 自动补图 |
| `Content/**/*.png` | 游戏官方 Mod 图片加载器；DTMAPI 桥请求它取图 | 间接 | 以文件名 stem 注册 Sprite；DTMAPI 按前缀把原版模板请求映射到这些 stem |
| 与 PNG 同 stem 的 `.json` | 游戏官方 `ModSpriteMeta` | 否 | 可定义 `pivot` 与 `pixels_per_unit`；不要和帧清单混淆 |
| `Content/Audio/*.wav` | DTMAPI 音频替换运行时 | 是 | 路径必须留在 owner 包内；缺失或不合规时放行原版声音 |

当前 `custom-animals.json` Runtime 不读取旧原型字段 `movementMultiplier`、`metabolismMultiplier`、`packageItemId`、`shopItemListId`。教程必须删除这些字段，或明确标为无效历史字段；移动/代谢、动物袋、商店分别由官方 `animal_tbanimal.json`、`item_tbitem.json`、`mod_tbmodstoreextension.json` 拥有。

### 本轮新增只读证据

- 当前 1.00.05 原生表：[animal_tbanimal.json](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/animal_tbanimal.json)、[animal_tbhusbandry.json](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/animal_tbhusbandry.json)、[animal_tbhusbandryenergy.json](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/animal_tbhusbandryenergy.json)、[item_tbitem.json](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/item_tbitem.json) 与 [recipe_tbrecipe.json](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/recipe_tbrecipe.json)。
- 最早保留构建的隐藏阈值：[23762374 `animal_tbhusbandry.json`](../../../../../references/doloc-town/reverse/builds/23762374_public_C416D4/asset-ripper-unity-project/ExportedProject/Assets/Configs/GenDatas/animal_tbhusbandry.json)。
- 原生图片、碰撞与设备行为：[ModInfo.cs](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Config/ModInfo.cs)、[AnimalInfo.cs](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Config/Animal/AnimalInfo.cs)、[Animal.cs](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Animal.cs)、[GarbageShredder.cs](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/GarbageShredder.cs) 与 [EquipmentManager.cs](../../../../../references/doloc-town/reverse/builds/24788406_public_F06183/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/EquipmentManager.cs)。
- DTMAPI 当前解析边界：[CustomAnimalAnimatorBridgeService.cs](../../../../../src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs) 与 [AnimalHusbandryNativeRuntime.cs](../../../../../products/first-party/AnimalHusbandryProgress/src/Native/AnimalHusbandryNativeRuntime.cs)。

### 本轮验证

- 当前/最早原生畜牧表、当前物品与普通配方表、四个 LocalLow 原型包的动物/掉落表均完成只读 JSON 解析与目标行核对。
- V0 的 4 组普通结果和 4 组隐藏结果均按当前 1.00.05 出售价重新计算并通过 8 条精确值断言。
- 本 Review 的 21 个相对链接全部可解析，尾随空白检查通过。
- `tools/scripts/check-doc-governance.ps1`：PASS，7040 checks。
- Chrome Wiki 页只用于交叉核对周期、占用空间与生产条件；价格、阈值、配方与源码行为以本地当前构建为准。
- 未启动游戏，未安装或改写 Mod，未运行任何存档接受测试；独立设备和 V0 经济仍是待实现、待游戏验收状态。

## 2026-08-27 追加审查：public 1.00.06、矿石与肥料

用户要求：先把新 public `1.00.06` 与 `1.00.05` 的相关代码/配置做一次复核，再列出可供 Hatch“产矿”路线选择的矿石，并确认 V0 中“肥料”究竟是哪一个物品。

### 1.00.05 → 1.00.06 相关差异

本次直接比较 `24788406_public_F06183` 与 `24966367_public_958EAF` 的恢复源码和 GenDatas。结论是：**畜牧与拟议分解设备的既有 native owner 没有行为变化，当前 V0 价格不需要因 1.00.06 重算。**

- 六张畜牧表 `animal_tbanimal`、`animal_tbanimaldocument`、`animal_tbanimalstate`、`animal_tbfeed`、`animal_tbhusbandry`、`animal_tbhusbandryenergy` 均逐字节相同。
- 动物生命周期、生产、AI、渲染、房间、动物袋、Animator/Sprite 覆盖、官方 Mod JSON/PNG 合并、畜牧进度 UI 的选定 owner 文件均逐字节相同；因此生产周期、产物 LUT、隐藏阈值/贡献、`sprite_size * 0.125` 碰撞换算和 DTMAPI 当前 Hook 目标都没有变。
- `GarbageShredder`、`EquipmentManager`、`EquipmentFuncGarbageShredder`、`TbDismantleRecipe`、`TbDismantleRecipeGroup` 与 `ItemSpawnInfo` 均逐字节相同；`equipment_tbequipment`、`recipe_tbdismantlerecipe`、`recipe_tbdismantlerecipegroup`、`item_tbitemspawn` 也均逐字节相同。
- 普通配方 owner `TbRecipe` 与 `TbRecipeGroup` 有一项正向变化：物品→配方和配方→组的派生 map 改为首次使用时按当前 `DataList` 构建，`PostResolve` 只清缓存。这修复了 Mod 追加普通配方后加工无人机/图鉴可能看不到的问题，但不改变独立分解配方组的执行合同，也不保证首次访问之后任意热修改会自动刷新缓存。
- 三张通用表虽然整体有差异，但逐 ID 核对后都与本方案无冲突：`item_tbitem` 只修改 `sound` 和四件野生蘑菇家具售价；`recipe_tbrecipe` 只移除 `sound` 配方并把四种甜点从 `cook` 调到 `bake`；`recipe_tbrecipegroup.equipment_workbench` 只移除 `sound`。
- 本方案引用的煤、全部标准矿石、矿物收集品、五种肥料/便便，以及毛、肉、燃料路线的现有原版物品行均逐字段相同；售价和买价没有变化。

这是一轮静态版本兼容核对，不是 `1.00.06` 游戏运行验收。没有启动游戏、取得 Runtime lock、改写 Mod 或接触存档。

### 1.00.06 矿石清单

官方物品表把“矿”分成两类。Hatch 普通产矿路线应优先使用 `material_ore` 的五种矿物素材；`special_mineral` 是图鉴/收藏向的矿物收集品，更适合作为稀有或隐藏结果。

| 原生分类 | ID | 中文名 | 出售价 | 买入价 | 设计备注 |
| --- | --- | --- | ---: | ---: | --- |
| `material_ore` | `coal` | 煤 | 10 | 50 | 原生也把煤归入矿物素材；同时具有 300 电能 |
| `material_ore` | `copper_ore` | 铜矿石 | 12 | 60 | 常规低阶矿石 |
| `material_ore` | `iron_ore` | 铁矿石 | 40 | 200 | 常规中阶矿石 |
| `material_ore` | `titanium_ore` | 钛矿石 | 100 | 500 | 常规高阶矿石 |
| `material_ore` | `gold_ore` | 金矿石 | 300 | 1500 | 标准矿石中单价最高 |
| `special_mineral` | `amber_ore` | 琥珀 | 125 | 250 | 矿物收集品 |
| `special_mineral` | `fossil_ferns_ore` | 蕨类化石 | 180 | 360 | 矿物收集品 |
| `special_mineral` | `beryl_ore` | 绿柱石 | 200 | 400 | 矿物收集品 |
| `special_mineral` | `meat_shaped_stone_ore` | 肉形石 | 225 | 450 | 矿物收集品 |
| `special_mineral` | `ammonite_ore` | 菊石化石 | 380 | 760 | 矿物收集品 |
| `special_mineral` | `obsidian_ore` | 黑曜石 | 425 | 850 | 矿物收集品 |
| `special_mineral` | `trilobite_ore` | 三叶虫化石 | 480 | 960 | 矿物收集品中单价最高 |

当前 Hatch V0 的 `iron_ore x3 + copper_ore x5 = 180` 使用的是标准矿石，不包含收藏品，也没有因 1.00.06 改价。

### “肥料”精确指向

V0 表中的 `fertilizer` 指原版普通作物肥料，**不是动物便便，也不是有机肥**：

| ID | 中文名 | 分类 | 出售价 / 买入价 | 原生效果 |
| --- | --- | --- | ---: | --- |
| `fertilizer` | 肥料 | `product_farm` | 25 / 50 | `ItemFunctionFertilizer`；普通作物，生长加成 `0.35`，持续 `576 TU` |
| `organic_fertilizer` | 有机肥 | `product_farm` | 50 / 100 | 普通作物的更高阶肥料 |
| `fertilizer_tree` | 树肥 | `product_farm` | 50 / 100 | 乔木用肥料 |
| `organic_fertilizer_tree` | 有机树肥 | `product_farm` | 100 / 200 | 乔木用更高阶肥料 |
| `faeces` | 便便 | `husbandry_animal_product` | 1 / 2 | 畜牧原料；可按原版配方加工为有机肥，不是肥料本体 |

原版普通 `fertilizer` 可由 `weeds x3` 或 `thorn x1` 在堆肥桶/肥料压制机生产，也可由两种骨鱼烘干产出；`faeces x1` 的原版配方产物则是 `organic_fertilizer x1`。因此现 V0 的 Drecko `fertilizer x2`、Mole `fertilizer x3` 应理解为普通 `fertilizer`，若改成 `organic_fertilizer`，对应价值会从每件 25 翻到 50，必须重新算经济表。

## 2026-08-27 追加审查：用户给定产物树与“小动物照料站”

用户本轮把产物树具体化为：

- Hatch：一天一个哈奇蛋；照料站处理为 `iron_ore x5 + copper_ore x5`；隐藏产物石化哈奇蛋处理为 `gold_ore x10 + titanium_ore x5`，阈值 60。
- Drecko：两天一个壁虎蛋；处理为 `wool x3`；隐藏产物按当前文字直接掉落 `wool_grease x3`，阈值 80。若用户实际意图是“另一种壁虎蛋处理为 3 个羊毛脂”，仍需补一个隐藏蛋名称；本审查不擅自新增。
- Mole：一天一个田鼠蛋；处理为 `meat x10`；隐藏产物是另一种田鼠蛋，处理为 `meat x5 + organic_fertilizer x30`，阈值 80。
- Oilfloater：一天一个浮游生物蛋；处理为 `coal x20 + resin x5`；隐藏产物是另一种蛋，处理为 `rubber x30 + plastic x30`，阈值 80。
- 额外处理设备中文名冻结为“小动物照料站”。

### 普通产物价值

以下只计算进入照料站后的原版物品出售价；四种自定义蛋本身的直接出售价尚未定义。

| 动物 | 周期 | 处理结果 | 每蛋处理价值 | 每日处理价值 | 占用空间 | 每空间每日价值 | 相对原版模板普通日产值 |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |
| Hatch | 1 天 | `iron_ore x5 + copper_ore x5` | 260 | 260 | 2 | 130 | 3.47x |
| Drecko | 2 天 | `wool x3` | 1500 | 750 | 3 | 250 | 4.50x |
| Mole | 1 天 | `meat x10` | 600 | 600 | 4 | 150 | 2.40x |
| Oilfloater | 1 天 | `coal x20 + resin x5` | 350 | 350 | 2 | 175 | 3.50x |

普通日产值为 260--750，最高/最低为 2.88x，按“每只动物”看不算大致一致。按畜棚容量归一后为 130--250，Drecko 仍是明显高点；其余三只为 130 / 150 / 175，已经相当接近。

最小调整是保留所有处理数量，只把 Drecko 恢复为三天一个壁虎蛋。此时四者每空间每日价值为 130 / 166.67 / 150 / 175，最高/最低仅 1.35x，可以合理称为“大致一致”，同时也保留了原版角羊驼三天生产节奏。若坚持两天周期，就应接受 Drecko 是普通收益最高的明确强势动物，而不是把四者宣传成同一价值带。

Oilfloater 单独看配比是合理的：`coal x20` 值 200，`resin x5` 值 150，树脂占整蛋处理价值 42.9%，煤与树脂都不是陪衬。

### 隐藏产物价值

阈值表示累计贡献点，不直接等于天数；达到阈值后还要等待下一次满足条件的普通生产。因此这里只使用“价值 / 阈值点”做静态可比指标，不伪造隐藏日产值。

| 动物 | 隐藏结果 | 单次价值 | 阈值 | 每阈值点价值 | 若每次贡献 10，达到阈值所需次数 |
| --- | --- | ---: | ---: | ---: | ---: |
| Hatch | `gold_ore x10 + titanium_ore x5` | 3500 | 60 | 58.33 | 6 |
| Drecko | `wool_grease x3` | 3000 | 80 | 37.50 | 8 |
| Mole | `meat x5 + organic_fertilizer x30` | 1800 | 80 | 22.50 | 8 |
| Oilfloater | `rubber x30 + plastic x30` | 6000 | 80 | 75.00 | 8 |

隐藏结果也不在同一价值带：每阈值点 22.50--75.00，最高/最低为 3.33x。最大意外来自 `plastic`：1.00.06 的原版塑料售价是 150，`plastic x30` 单项就值 4500；所以 Oilfloater 隐藏蛋远高于其余三种。Hatch 还同时享有更低的 60 阈值，实际强度也高于 Drecko。

若用户希望保留全部产量而只通过阈值平衡，以 Drecko 的 37.5 价值/点为锚，近似阈值应为 Hatch 90、Drecko 80、Mole 50、Oilfloater 160。若必须保留 60 / 80 / 80 / 80，则一组接近 37.5--38.75 价值/点的数量示例是：Hatch `gold_ore x6 + titanium_ore x5 = 2300`，Drecko 保持 `wool_grease x3 = 3000`，Mole `meat x10 + organic_fertilizer x50 = 3100`，Oilfloater `rubber x30 + plastic x10 = 3000`。这两组只是平衡参照，不覆盖用户当前给定值。

若延续上一版“自定义蛋直接出售约等于处理价值三分之二”的规则，普通蛋可先用 175 / 1000 / 400 / 235 做模拟，隐藏蛋则需先确定 Drecko 是否也有独立蛋，再统一冻结价格。当前不能只凭处理结果假定作者会禁止直接出售。

### `ONIExtracted/rancherstation` 素材盘点

本地源位于 `E:\DolocTownUnity\ONIExtracted\rancherstation`。只读盘点确认：

- atlas 一张 `512x512`；主状态帧为 `455x666`，UI 帧为 `178x249`，放置预览为 `455x666`；视觉上是带工具、瓶罐和照料用品的木制工作站，和“小动物照料站”名称匹配。
- `off 4 + on 4 + place 4 + ui 4 + working_pre 16 + working_loop 362 + working_pst 17 = 411` 张序列 PNG。
- 逐文件 SHA-256 揭示每个序列内部只有一个唯一图像；`off`、`on`、`working_pre`、`working_loop`、`working_pst` 五组又是同一哈希。也就是说当前提取物实际只有主静态图、放置预览和 UI 图三种栅格结果，411 张文件不构成已烘焙动画。
- 第一版可以把主图作为照料站静态 `scene_asset` / 工作图进行本地技术探针；若需要真正的开机/处理动画，必须重新取得已烘焙帧或自行制作变化帧，不能把重复文件伪装为动画。
- 该目录名与内容表明素材来自另一款商业游戏的本地提取物。它可以作为本地原型或视觉参考，但在授权、许可和来源闭环前不得复制进可分发的 DTMAPI/Workshop 包；公开版本应使用获授权素材或原创重绘。

## 后续实施边界

建议后续以一个 AnimalPack Update 管理真实文件变更，并按以下小阶段推进：

1. 从四个 LocalLow 包提取数据到仓库内统一 ContentPack 真源，保持现有玩法和 ID，不先改经济；完成静态验证与第七档 `NoNativeSave` 单包加载探针。
2. 在统一包内新增四个普通自定义产物、四个隐藏产物及处理路线；先冻结角色与价值带，再冻结数量、价格和公开 ID；完成 focused JSON/economy 检查与 disposable fixture 游戏验收。
3. 用经过验证的统一包派生一个精简的单动物教学 fixture 和多动物案例，修正飞书生成器并拆分快速教程、字段参考与进阶案例。

本 Review 不授权复制或发布游戏本体资源、第三方 PNG/WAV、反编译代码或当前来源未闭合的原型资产，也不把当前本地成功运行提升为 Workshop 发布授权。

## 2026-08-27 最终实施输入覆盖

用户随后明确允许第一版偏强，并用以下值覆盖本 Review 前文仍写作“两天 / 80 / 80 / 80”的中间提案：

| 动物 | 最终普通周期 | 普通蛋直接售价 | 照料站固定结果 / 价值 | 每日处理价值 | 每空间每日处理价值 |
| --- | ---: | ---: | --- | ---: | ---: |
| Hatch | 1 天 | 175 | `iron_ore x5 + copper_ore x5` / 260 | 260 | 130 |
| Drecko | 3 天 | 1000 | `wool x3` / 1500 | 500 | 166.67 |
| Mole | 1 天 | 400 | `meat x10` / 600 | 600 | 150 |
| Oilfloater | 1 天 | 235 | `coal x20 + resin x5` / 350 | 350 | 175 |

四条照料站路线相对直接出售的加工增值分别为 `48.57% / 50% / 50% / 48.94%`。按动物空间归一后为 `130 / 166.67 / 150 / 175`，最高/最低约 `1.35x`；这满足“产出尽量接近、但模组可以偏强”的最终设计口径。Drecko 的三天周期与原版角羊驼模板一致，壁虎普通蛋仍进入照料站，只有隐藏路线直接产物品。

| 动物 | 最终隐藏结果 | 单次处理/直接价值 | 最终阈值 | 每阈值点价值 |
| --- | --- | ---: | ---: | ---: |
| Hatch | 石化哈奇蛋 -> `gold_ore x10 + titanium_ore x5` | 3500 | 60 | 58.33 |
| Drecko | 直接 `wool_grease x3`，无隐藏蛋、无拆解配方 | 3000 | 60 | 50.00 |
| Mole | 稀有田鼠蛋 -> `meat x5 + organic_fertilizer x30` | 1800 | 40 | 45.00 |
| Oilfloater | 聚合物浮游生物蛋 -> `rubber x30 + plastic x30` | 6000 | 120 | 50.00 |

除 Hatch 略高外，四条隐藏路线已经收敛到 `45--58.33` 价值/贡献点；Oilfloater 的高单次价值由 120 阈值抵消。三个可加工隐藏蛋的直接售价最终为 `2300 / 1200 / 4000`，仍约为处理价值的三分之二；Drecko 没有中间蛋，原生隐藏畜牧直接给 `wool_grease x3`。

最终实现由 [Update 20260827-0003](../../../../reviews/updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md) 管理。统一包在 public `1.00.06` 的第七档完成加载/显示探针，但设备加工、生产周期和隐藏贡献仍需独立交互验收；因此上述数字是已实现的设计输入，不等于全经济链已经运行验证。

### 运行声音误判复核

用户在统一包探针末尾怀疑“击打原版沼泽兽也被田鼠 WAV 替换”。`GAME-SMOKE/20260827-231124/support-context.txt` 保留的相邻日志显示，最后几次 `PLAY_ANIMAL_PET_PANGOLIN` 替换前的实例均为 `title=田鼠 species=mole`，不是 `species=marsh_pangolin`。两种动物确实共享原版 Wwise 事件，但 DTMAPI 还要求当前 `Animal.PlayAnimalSound` 实例的 `protoName` 与 replacement `speciesId` 精确匹配；原版沼泽兽只共享事件名不会命中田鼠替换。本次没有发现原版沼泽兽误拦截。
