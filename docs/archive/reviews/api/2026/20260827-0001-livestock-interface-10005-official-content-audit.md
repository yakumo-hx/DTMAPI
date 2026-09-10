# 1.00.05 畜牧接口、作者文档与官方内容能力审查

## 记录状态

- 日期：2026-08-27
- 状态：`recorded`
- 性质：API / native-owner / 官方内容静态审查；不是实现、发布授权或当前版本游戏验收
- Source：用户要求读取工作空间必读文档，定位 DTMAPI 畜牧接口与作者说明，比较当前 `1.00.05` 和最早本地反向版本，并判断是否存在无需 DTMAPI 的官方 JSON / PNG / WAV 新养殖动物路线
- 当前反向观察头：`references/doloc-town/reverse/builds/24788406_public_F06183`，Steam build `24788406`，public，Application version `1.00.05`
- 最早本地代码基线：`references/doloc-town/reverse/builds/23249387_workshop_247ACD`，Steam build `23249387`，来源目录标识 `0.96.05`
- 最早本地完整畜牧配置基线：`references/doloc-town/reverse/builds/23762374_public_C416D4`
- 直接前版对照：`references/doloc-town/reverse/builds/24650773_public_76C24E`，Application version `1.00.03`
- 官方作者文档快照：`references/doloc-town/official-workshop-docs/feishu-crawl-20260715`
- 新运行验证：未运行游戏；未安装或修改 Runtime、游戏、官方本地 Mod、Steam 订阅内容或玩家存档；未取得 Runtime lock

## 审查边界与安全条款

- 官方 DLL、AssetRipper 恢复项目与反编译源码仅作本地研究证据，不复制到 DTMAPI，不提交，不分发。
- 本审查从 native responsibility function / state holder 出发，区分公开 C# ABI、第一方 ProductNative UI 产品和内部 ContentPack bridge；不以 registry 成功、文档存在或旧 smoke 单独证明当前版本运行有效。
- `23249387_workshop_247ACD` 没有完整 GenDatas 捕获，所以“最早版本”可以直接比较代码，官方表内容只能从最早本地完整配置基线 `23762374_public_C416D4` 开始比较。不得把后者伪称为 0.96.05 配置。

## 一、工作空间中实际存在的三条“畜牧接口”

| 名称 | 当前身份 | 实际能力 | 当前裁决 |
| --- | --- | --- | --- |
| `ICustomAnimalApi` | `Experimental / Frozen since 0.5.5` 的公开 C# 兼容 ABI | 注册/查询 DTMAPI Core registry 元数据；`RequestSpawn` 在进入任何 native animal owner 前返回 `runtime-creation-blocked` | **不是有效的新动物创建接口**；只能按 registry-only 兼容面理解，禁止新 Mod 采用 |
| `IAnimalViewerApi` | `Experimental / Deprecated / Frozen` 的旧显示兼容 ABI | 历史畜牧进度 UI facade；当前第一方 `AnimalHusbandryProgress` 不再消费它 | 只保留旧二进制兼容；不是动物创建、生产、繁殖或状态修改 API |
| CustomAnimals JSON / PNG / WAV bridge | DTMAPI 内部 ContentPack / GameBridge 能力，不是公开稳定 C# API | 官方 JSON 负责动物表、动物袋、商店、产物、图鉴与原生生命周期；DTMAPI 负责模板 Animator、模板 AI、按物种隔离 loose PNG 帧和按物种/阶段/原生事件隔离 WAV | 作者可做到“内容包内无 DLL”，但玩家运行时仍需要 DTMAPI；这是目前真正可工作的自定义养殖动物路线 |

### 1.1 `ICustomAnimalApi` 为什么不能算有效创建接口

- 声明位于 `src/DTMAPI.Abstractions/CustomEntities.cs`，注解已明确写为 registry-only、Frozen、无 native host。
- 实现位于 `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`。
- `RequestSpawn` 只完成定义、owner 与请求校验，然后发布 `SpawnRequested` registry 事件并返回失败：
  - `Succeeded = false`；
  - `FailureReason = runtime-creation-blocked`；
  - `RuntimeStatus = RuntimeCreationBlocked`；
  - 不创建 `Animal`，不进入 `AnimalManager`，不取得 room/home/feed/produce/breed/save ownership。
- `docs/api/public-api-matrix.md` 的 Custom Entities 行与实现一致；现有旧 smoke 只证明 registry 与 blocked result，不证明 native animal。

推荐状态不变：`Experimental / Frozen / registry-only`。不得重新宣传为作者可调用的“生成动物 API”。

### 1.2 `IAnimalViewerApi` 与畜牧进度产品

- 旧接口声明位于 `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`，只包含配置特殊产物进度显示与查询状态。
- 当前第一方产品在 `products/first-party/AnimalHusbandryProgress` 内拥有 ProductNative 实现：
  - `AnimalFullInfoData(Animal)` postfix；
  - `AnimalViewer.Show(AnimalFullInfoData)` prefix/postfix；
  - `AnimalPanelUiState.Unregister()` postfix；
  - 只派生、克隆和清理显示行，不写动物、产物、繁殖或存档状态。
- 最早 `23249387`、直接前版 `24650773` 与当前 `24788406` 的 `AnimalFullInfoData.cs`、`AnimalViewer.cs`、`AnimalPanelUiState.cs` 均逐字节相同。

因此，产品的三个 native target 对 `1.00.05` **静态兼容**；但本轮没有在 `1.00.05` 启动游戏，不能把静态兼容写成当前版本 UI 运行验收。

## 二、作者说明文档位置与真实合同

当前作者总入口 `author-docs/README.md` 链接到：

- `author-docs/content-packs/custom-animal-json-png-wav.md`：完整、当前的“JSON + PNG + WAV 自定义小动物指南”；
- `author-docs/content-packs/new-farm-animal-draft/README.md` 与 `draft.md`：本地飞书底稿和生成材料，不是比主指南更新的独立运行合同。

主指南的边界写得准确：

1. 作者包不写 DLL，使用官方 `info.json` 与官方动物、物品、商店、图鉴 JSON；
2. DTMAPI 额外读取：
   - `Content/DTMAPI/manifest.json`；
   - `Content/DTMAPI/custom-animals.json`；
   - `Content/DTMAPI/audio-replacements.json`；
3. `custom-animals.json` 以 `templateSpeciesId` / `aiTemplate` 复用四种原版模板，以 `pngSpriteOverride` 将模板动画请求的帧名按物种映射到自有 PNG；
4. `audio-replacements.json` 的 `AnimalVoice` 以 `speciesId + stage + nativeSoundEvent` 限定 WAV，避免污染复用相同原生事件的原版动物；
5. 该路线不提供完全独立的新 AI，也不是公开稳定 C# API。

已有运行证据 `docs/debug/evidence/GAME-SMOKE/20260701-073005` 在 Steam build `23762374` 的 slot 7 验证了 Hatch 幼体/成年 WAV 路径，且现有用户手测记录补充了 pet/hit 与原版鸡不污染。它是历史有效性证据，不是 `1.00.05` 的新验收。

## 三、官方 Workshop 文档实际公开了什么

最新本地官方快照中的相关页面是：

- `03 小动物`：`references/doloc-town/official-workshop-docs/feishu-crawl-20260715/pages/POkcwjhF3iPzmikWRZ2cn8n9ncf/content.md`；
- `03 ID对照表（小动物）`：`references/doloc-town/official-workshop-docs/feishu-crawl-20260715/pages/UdOwwqfOLiFcFMkW5rrchKVUnpd/content.md`。

官方文档公开的是现有四种动物的**美化/贴图替换**：

- `chicken`；
- `goat`；
- `marsh_pangolin`；
- `slime`。

页面列出帧数、动画 ID、原动物 ID、PNG 文件名、图鉴图标与详情图尺寸，示例是调整 `slime` 颜色。页面没有公开：

- 新物种 schema 或新物种示例；
- 新物种 Animator controller 加载；
- 把未知 species 映射到某一原版专用 AI；
- WAV / SoundBank / Wwise 事件作者入口；
- 按物种隔离声音或 loose PNG 的规则。

所以官方文档只足以支持“替换四种现有动物外观”，不支持“新增第五种独立养殖动物”。

## 四、官方代码中的未文档化能力与硬缺口

### 4.1 官方确实存在的通用内容能力

`ModInfo.UpdateCache()` 从包内 `Content` 递归读取：

- `*.json`，由 `ModManager.LoadWithMods` 按表名验证并合并；
- `*.png`，按不带扩展名的全局 Sprite 名称缓存。

因此，技术上可以用未公开的通用表合并机制追加 `animal_tbanimal`、动物袋 Item、商店扩展、产物与图鉴记录。官方 native `AnimalManager`、`Animal`、成长、喂食、繁殖、生产、房间容量与存档随后仍会处理读入的表记录。这是代码事实，但不是官方承诺的稳定作者 API。

### 4.2 为什么它仍不能独立完成 JSON / PNG / WAV 新物种

#### Animator 缺口

- `AnimalLevelData.animator` 最终进入 `AnimatorAsset.TryLoadAsset`。
- `AnimatorAsset.TryLoadAsset` 只调用 `DolocAPI.GetAsset<RuntimeAnimatorController>(url)`。
- `DolocAssetCache.GetAsset<T>` 的官方 Mod 文件覆盖分支只在 `T == Sprite` 时调用 `LoadSpriteFromFile`；`RuntimeAnimatorController` 没有文件 Mod override。
- 新物种若写新 animator key，官方 Addressables 中没有该 controller，加载失败。
- 若直接复用原版 controller，它请求的仍是原版帧名；把自有 PNG 命名为这些原版帧名会形成全局 Sprite 覆盖，同时污染原版模板动物，而不是物种隔离的新外观。

DTMAPI 的 `AnimatorAsset.TryLoadAsset` 与 `SpriteOverrideHandler.TryGetModOverrideSprite` bridge 正是在补这两个缺口。

#### AI 缺口

`AnimalAI.GetDefaultAnyState(string animalId)` 在最早与当前版本中都只显式识别四个 ID：`chicken`、`slime`、`goat`、`marsh_pangolin`；未知 ID 回落到 `Normal_FreeTimeState`。官方 JSON 没有字段把一个新 species ID 映射为四种专用默认状态之一。DTMAPI 的 `aiTemplate` bridge 补的是该映射，不是重写整套原生 AI。

#### WAV 缺口

- `ModInfo` 只扫描 JSON 与 PNG；官方 `Assembly-CSharp` Mod 加载路径没有 `*.wav` 或 `AudioClip` 文件入口。
- `WwiseSoundManager` 从 Addressables 加载官方 SoundBank，并按原生事件名播放；它不是内容包 WAV loader。
- DTMAPI 的 `AudioReplacementService` 才读取 `Content/DTMAPI/audio-replacements.json`，加载 WAV，并在 `Animal.PlayAnimalSound()` 上下文中做物种、阶段和原生事件三重限定。

因此，最新版本不存在一条“官方没有写文档、但已经能完全不装 DTMAPI”的 JSON + PNG + WAV 新养殖动物路线。

## 五、`1.00.05` 与版本基线比较

### 5.1 `1.00.03` → `1.00.05`

对以下 19 个反编译文件逐一计算 SHA-256：19/19 相同，0 个变化：

- 核心：`Animal`、`AnimalAI`、`AnimalController`、`AnimalSystem`、`AnimalRenderer`、`AnimalData`、`AnimalManager`、`ItemAnimalPackage`；
- 资产/加载：`AnimatorAsset`、`SpriteOverrideHandler`、`ModManager`、`ModInfo`、`Tables`；
- 配置类型：`AnimalInfo`、`TbAnimal`、`TbAnimalDocument`；
- 畜牧进度 UI：`AnimalViewer`、`AnimalFullInfoData`、`AnimalPanelUiState`。

六张畜牧 GenDatas 表中五张逐字节相同：

- `animal_tbanimal.json`；
- `animal_tbanimalstate.json`；
- `animal_tbfeed.json`；
- `animal_tbhusbandry.json`；
- `animal_tbhusbandryenergy.json`。

只有 `animal_tbanimaldocument.json` 变化，且只是四种原版动物的图鉴解锁计数下调；物种 ID、schema、动画、AI、生产和 Mod 加载能力均未变化。

结论：`1.00.04–1.00.05` 累计更新没有引入官方新动物作者桥，也没有静态破坏 DTMAPI 当前 CustomAnimals / AnimalHusbandryProgress 目标。

### 5.2 最早本地代码 `0.96.05` → `1.00.05`

不能写成“畜牧代码全部一致”。逐字节相同的关键文件包括：

- `AnimalInfo.cs`、`TbAnimal.cs`、`TbAnimalDocument.cs`；
- `AnimalData.cs`、`AnimalManager.cs`；
- `AnimatorAsset.cs`、`SpriteOverrideHandler.cs`、`ModInfo.cs`；
- `AnimalViewer.cs`、`AnimalFullInfoData.cs`、`AnimalPanelUiState.cs`。

发生变化的 native 行为文件及含义：

| 文件 | 累计变化 | 对 DTMAPI 自定义动物边界的含义 |
| --- | --- | --- |
| `Animal.cs` | 增加 `currentRootRoom`；房间切换从硬编码 `MainFarm` 泛化到 `RootRoom`；z-order 精度调整；Wwise 注册从 render 生命周期移出 | 多根农场/房间与音频生命周期修复，不是新物种加载能力 |
| `AnimalRenderer.cs` | Wwise 注册/注销移到 `OnCreated` / `OnDestroy` | 生命周期修复；相关 `OnRecycle`、`OnFell`、`PlayAnimation`、`FixedUpdate` 目标仍在 |
| `AnimalAI.cs` | 天气改读动物 home root room；睡眠时 FreeTime 决策等待；其余有局部变量重命名 | 多根天气与睡眠修复；四 ID default-state switch 未扩展 |
| `AnimalController.cs` | room searcher 天气改读动物 home root room | 多根天气修复 |
| `AnimalSystem.cs` | 单例 `AnimalMapForBuilding` 改为按 room 取得 map | 多根房间/地图修复 |
| `ItemAnimalPackage.cs` | 首次放养时补发 `ANIMAL_BREED_NEW` 事件 | 图鉴/事件修复，不是新物种 authoring schema |

DTMAPI CustomAnimals 当前依赖的 14 个 native method declaration 在最早与当前版本中签名全部相同：

- `AnimatorAsset.TryLoadAsset`；
- `AnimalAI.GetDefaultAnyState`；
- `Animal.OnRender`、`DEBUG_SetAdult`、`Sleep`、`WakeUp`、`CallToRoom`；
- `AnimalRenderer.OnRecycle`、`OnFell`、`PlayAnimation`、`FixedUpdate`；
- `SpriteOverrideHandler.TryGetModOverrideSprite`；
- `AnimalAI.AnimalAIState.MakeDecision_FreeTime`；
- `AnimalController.OnUpdate`。

`ModInfo.cs` 从最早到当前逐字节相同；`ModManager.cs` 与 `Tables.cs` 有其他领域和通用验证变化，但差异中没有新的 Animal、WAV、RuntimeAnimatorController 或动物专用 loader 分支。

### 5.3 最早完整配置 `23762374` → `1.00.05`

- `animal_tbanimal.json` 始终只有四行：`slime`、`chicken`、`goat`、`marsh_pangolin`；四种动物的逃跑基础/增量概率从 `0.05` 调为 `0.02`。
- `animal_tbanimaldocument.json` 始终只有四个物种，调整了排序与解锁计数。
- `animal_tbfeed.json` 新增 `eden_flower`。
- `animal_tbhusbandry.json` 将四种隐藏畜牧产物阈值分别从 `60/100/120/45` 调为 `50/80/100/35`。
- `animal_tbhusbandryenergy.json` 新增 `seed_eden_flower`。
- `animal_tbanimalstate.json` 逐字节相同。

这些都是既有四物种的平衡、喂食与图鉴调整，没有新增第五个官方动物 ID、表类型或作者协议。

## 六、最终裁决

### 6.1 “接口是否有效”

- **公开 `ICustomAnimalApi`：否。** 它只对 registry 元数据有效，对 native animal 创建明确无效。
- **旧 `IAnimalViewerApi`：仅旧显示 ABI 有效。** 当前产品行为已经由 `AnimalHusbandryProgress` ProductNative 自有，不应推荐新作者采用旧接口。
- **JSON / PNG / WAV CustomAnimals 内容桥：静态上仍兼容 `1.00.05`。** `1.00.03` 到 `1.00.05` 的 19 个选定目标文件全部相同；最早到当前的 14 个 Hook 签名全部相同，关键 schema/manager/asset loader 稳定。已有 build `23762374` 运行证据证明过该路线，但本轮未产生 `1.00.05` 游戏内验收，所以状态应写为 `current-baseline statically compatible / runtime acceptance pending`，不能写 `1.00.05 verified`。

### 6.2 “最新版本是否已有官方隐藏的新动物路线”

- **现有四种动物换皮：有，官方支持，PNG 即可，不需要 DTMAPI。** 这是官方文档明确公开的范围。
- **只靠官方 JSON 追加表记录：代码层面部分可行，但未文档化、未承诺，且不足以形成独立新物种。** 数据、袋子、商店、产物可能进入 native 表和生命周期。
- **独立新 species ID + 自有外观 + 模板专用 AI + 自有 WAV：没有。** 官方缺少 Mod Animator controller 入口、按物种隔离 loose PNG 的映射、JSON 可选模板专用 AI 的映射和 WAV loader/scoping。
- 作者指南中的“只需要 JSON、PNG、WAV”指**作者包不写 DLL**，不指**玩家无需安装 DTMAPI**。DTMAPI 正是补齐 Animator、AI、PNG 隔离和 WAV 的运行桥。

## 七、风险、验证与下一步

- 本轮是只读静态审查，没有修改 API matrix、Hook Map、Debug issue 或 smoke matrix，因为它们的当前状态没有发生变化。
- 没有运行构建：未改源码，文档治理检查足以匹配本轮风险。
- 没有运行游戏：当前结论不依赖启动游戏，也不应把历史 build `23762374` 收据冒充 `1.00.05` 收据。
- 若后续要把“静态兼容”升级为“`1.00.05` 运行验证”，应复用既有 Hatch 内容 fixture，先按相关 Review/Update 确认 slot 7 权威与 `NoNativeSave` 分类，再取得 Runtime lock，最小验证：内容发现、Animator/AI/PNG、幼体/成年 WAV、原版鸡不污染、title/exit 清理、无残留进程与日志收集。不得为了这一个兼容确认直接跑完整 Release。

## 八、后续运行解析（2026-08-27）

- 后续 [Update 20260827-0001](../../../updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md) 已在 Steam public build `24788406` / 画面版本 `1.00.05` / DTMAPI Runtime `0.6.1` 上完成第七档“大型畜棚”验收；本 Review 第 6.1 节的 `runtime acceptance pending` 是静态审查形成时的历史状态，现由该 Update 解析为当前安装边界的 `passed`。
- [手动可视运行](../../../../debug/evidence/GAME-SMOKE/20260827-103445) 观察到 Hatch、Oilfloater、Mole、Drecko 四套自有 PNG 外观及夜间睡眠/清晨活动；日志逐种验证 Animator、AI、PNG 与睡眠诊断，四包 8 个 WAV 均 ready，`MissingFrameFallbackWarningCount=0`。
- [Hatch 声音运行](../../../../debug/evidence/GAME-SMOKE/20260827-104337) 实际触发幼体与成年事件，两者均 `played=True suppressed=True`、`fallback=false`。两次运行均在任何恢复前证明第七档 current/bak/prev 与目标已提交侧车不变，且 profile、QA host、进程和 Runtime lock 清理完成。
- 该运行结果只升级 CustomAnimals JSON / PNG / WAV 内容桥的当前版本兼容状态；不改变 `ICustomAnimalApi` Frozen / registry-only、`IAnimalViewerApi` deprecated，以及“官方仍无无需 DTMAPI 的独立新物种路线”的裁决。

## 九、追加审查：多个 Mod 复用 `chicken` 是否冲突

需要区分“复用鸡模板”和“把自己的物种 ID 也写成鸡”，两者结论相反。

### 9.1 多个新物种都复用鸡模板

若每个包都有独立的 `speciesId`，则它们可以同时使用：

- `templateSpeciesId = chicken`；
- `aiTemplate = chicken`；
- `schedule_id = chicken`；
- 鸡的幼体/成年原生声音事件。

这些模板值不是 DTMAPI 的跨包唯一键。Animator bridge 以 animator key 注册，AI 和 PNG 上下文以新物种 `speciesId` 隔离，`AnimalVoice` 以 `speciesId + stage + nativeSoundEvent` 隔离。因此，两个包可以分别表现为不同 PNG、不同 WAV 和不同产物，而共同采用鸡的行为路线。

必须独立的值包括：

- 包 `UniqueID`；
- `speciesId` / `animal_tbanimal.id`；
- 成年和幼体 animator key；
- custom sprite prefix；
- 动物袋 item ID、普通产物 item ID、produce spawn LUT ID、图鉴等官方表主键；
- 同一 owner 内的 audio replacement ID。

这不表示它们在玩法层完全独立。所有 `chicken` 路线动物都会寻找鸡窝，共享原版 10 个物品单位的容量；鸡窝只保存产物物品 ID，不记录来源物种。因此不同产物可以同时存在于同一鸡窝，收取体验会混合，但这不是贴图、AI 或声音注册污染。给每个物种独立产物 item ID 后，产物数据本身仍可区分。

### 9.2 多个包都把 `speciesId` / `animal_tbanimal.id` 写成 `chicken`

这是不受支持的冲突配置：

- 官方 `ModManager.MergeArray` 对普通配置表按 `id` 建索引；后续同 ID 行会整行替换已有行，不是逐字段合并。两个包和原版鸡争用 `chicken` 时，最终整行由加载优先级/顺序决定，形态、产物和相关引用可能形成混合或不一致配置。
- DTMAPI 的 PNG 投影每个 `speciesId` 只有一个槽位；两个 owner 共用 `chicken` 时无法保持两套物种上下文。
- 相同 animator key 会让后来的 owner generation 被拒绝；相同的非自映射 AI species 也会冲突。
- 两个启用原生抑制的 `AnimalVoice` 若具有相同 `speciesId`、重叠 stage 和相同原生事件，其声音 scope 冲突，后来的 audio owner generation 会被拒绝。
- 即使某一层没有立即拒绝，原版鸡也可能被改图、改声或改产物，所以作者指南明确要求新动物复用鸡时写新的 species ID，不能写 `chicken`。

### 9.3 两个独立物种复用 `chicken` 的追加运行探针

`GAME-SMOKE/20260827-135817` 已补上 9.1 节此前缺少的专门运行组合：保留 Hatch 的 `speciesId=hatch`，临时把 Mole 的模板从 `marsh_pangolin` 同步改成 `chicken`，形成 `hatch -> chicken` 与 `mole -> chicken` 同时启用；Drecko 和 Oilfloater 继续作为另外两条模板的对照。

- Mole 的 `speciesId=mole`、成年/幼体 animator key、自有 PNG 前缀、动物袋、产物和其他官方表主键均保持独立。临时配置同步修改 `templateSpeciesId`、`aiTemplate`、`schedule_id`、`templateSpritePrefix`、帧清单模板前缀，以及幼体/成年 chicken 声音事件；没有把 Mole 自身 ID 改成 `chicken`。
- 静态预检确认 chicken 模板需要的 44 个 PNG 后缀全部存在于 Mole 的 45 帧素材中，缺失数为 0；因此本探针没有用缺帧回退掩盖模板冲突。
- 第七档“大型畜棚”成功加载并显示四类动物，夜间睡眠画面没有粉块、空白或原版鸡贴图回退。日志同时注册 `aiTemplates=drecko->goat,hatch->chicken,mole->chicken,oilfloater->slime`，并分别验证 Hatch、Mole 都命中 `DolocTown.AnimalAI+Chicken_FreeTimeState`。
- Animator / PNG 也按新物种分别解析：Hatch 从 `game_anim_animal_chicken` 投影到 `anim_animal_hatch`，Mole 从 `game_anim_animal_chicken_child` 投影到 `anim_animal_mole`；`PngSpriteBridge` 分别记录 `chicken -> hatch` 与 `chicken -> mole` 首帧映射。两者没有覆盖对方。
- Hatch 与 Mole 两个 audio owner 都以相同 chicken 幼体/成年原生事件完成 generation commit；本次遵照用户“只加载”的边界，没有逐只互动，所以只证明声音 scope 可共存注册，不把它写成两种 WAV 的实际播放验收。
- 运行结果为 `RunStatus=Passed`、`MissingFrameFallbackWarningCount=0`、`ProcessExited=Passed`、`NoFatalInstanceWindow=Passed`。`NoNativeSave` 在任何恢复前证明第七档 archive 与已提交侧车不变；没有例行存档备份、回写或恢复。测试后 Mole 本地包按 58 个文件逐项哈希恢复为探针前的精确内容，Mod profile 恢复，游戏进程退出，共享 Runtime lock 释放。

因此 9.1 节关于“独立 `speciesId` 可共享 chicken 模板”的注册、AI、Animator 和 PNG 隔离结论，已从静态裁决升级为当前 public `1.00.05` / Runtime `0.6.1` 的运行验证。这个仅加载探针足以回答“是否因两个 Mod 都选 chicken 模板而立即冲突”，但没有验证产物成熟/收取、繁殖、设备满载和两种 WAV 逐一播放；这些仍不能由“加载成功”自动推出。

## 相关记录

- `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md`
- `docs/reviews/api/2026/20260705-0003-custom-animal-content-boundary-review.md`
- `docs/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md`
- `docs/reviews/api/2026/20260802-0001-walkman-vehicle-animal-monster-native-owner-followup.md`
- `docs/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md`
- `docs/reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md`
- `docs/updates/2026/20260823-0002-public-10005-full-reverse-capture.md`
- `docs/updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md`
- `docs/api/public-api-matrix.md`
