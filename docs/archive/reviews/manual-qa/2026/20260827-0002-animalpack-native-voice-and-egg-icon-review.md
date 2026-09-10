# Manual QA Review: AnimalPack 原版动物声音隔离与蛋图标

## Review Header

- Time: 2026-08-27 23:27--23:31 +08:00
- Source: 用户在 `GAME-SMOKE/20260827-232755` 中用 Y 键控制台召唤动物，召唤后立即击打对应动物，再直接退出游戏并明确未保存；退出后补充的人工观察反馈。
- Scope: 仅审查两项反馈：原版沼泽兽叫声是否被田鼠 WAV 拦截，以及 Y 键控制台中的七种蛋物品为何显示完整动物。此 Review 不修改声音路由、物品 JSON 或图像资产。
- User constraints: 保留用户反馈顺序；无须做存档一致性恢复；图标稍后再改，当前先查问题。
- Related review/update/debug records: [AnimalPack 经济、教程与四动物合包 Update](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md)；[实施前 Review](../../code/2026/20260827-0001-animalpack-economy-tutorial-unification-preimplementation-review.md)；[运行烟测矩阵](../../../../debug/regressions/smoke-matrix.md)。
- Files/docs inspected: [`DTMAPI-latest.log`](../../../../debug/evidence/GAME-SMOKE/20260827-232755/DTMAPI-latest.log)；[`result.json`](../../../../debug/evidence/GAME-SMOKE/20260827-232755/result.json)；[`player-save-unchanged-before-cleanup.json`](../../../../debug/evidence/GAME-SMOKE/20260827-232755/player-save-unchanged-before-cleanup.json)；[`committed-sidecar-unchanged-before-cleanup.json`](../../../../debug/evidence/GAME-SMOKE/20260827-232755/committed-sidecar-unchanged-before-cleanup.json)；[`AudioReplacementService.cs`](../../../../../src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs)；1.00.06 逆向参考中的 [`Animal.cs`](../../../../../references/doloc-town/reverse/builds/24966367_public_958EAF/asset-ripper-unity-project/ExportedProject/Assets/Scripts/Assembly-CSharp/DolocTown/Animal.cs)；[`item_tbitem.json`](../../../../../products/first-party/AnimalPack/Content/item_tbitem.json)；[`DebugConsoleUi.cs`](../../../../../products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs)；[`DebugConsoleNativeActions.Helpers.cs`](../../../../../products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Helpers.cs)。
- Not inspected: 未录制或波形比对原版沼泽兽声音；未进行第三次游戏启动；未制作或替换蛋图标；未验证小动物照料站、产出周期或分解配方。

## Issue Review

### Issue 1: 原版沼泽兽叫声是否被田鼠替换

Original feedback:

- “Y键控制台召唤后面接的就是击打对应动物；鸡羊好像没问题，但沼泽兽我实在不能确定。直接退出了。”

Screenshot/log transcription:

- 用户没有附截图；可持续记录中的人工动作是“控制台每次召唤后立即击打相应动物”，鸡和羊听感正常，沼泽兽仅为无法凭记忆判断，并非用户确认串音。
- 日志在 `23:30:27.036` 记录 `marsh_pangolin.child`，在 `23:30:30.312` 和 `23:30:34.097` 两次记录 `marsh_pangolin.adult`；随后还记录了原版 `chicken.child/adult`、`goat.child/adult` 和 `slime.child/adult`。这证明本轮控制组确为原版模板物种，而不是把自定义田鼠误认作沼泽兽。
- 八条 AnimalPack WAV 在本轮启动时均进入 ready，声音 Hook 在 `23:28:21.329` 报告 `animalVoiceContextPrefix=True`、`animalVoiceContextPostfix=True`。
- 整份本轮日志中，成功/抑制替换事件 `AudioReplacement event ... played=True` 或 `suppressed=True` 的数量为 `0`；尤其从第一只原版沼泽兽召唤到退出之间没有田鼠替换命中记录。

Review record:

- User-confirmed facts: 用户确实按“召唤后击打”的顺序手测；鸡、羊未听出问题；沼泽兽只能判为听感不确定；本轮直接退出且未保存。
- Screenshot/log observations: Y 控制台日志明确给出原版物种 ID `marsh_pangolin`，而不是 AnimalPack 自定义物种 ID `mole`。本轮没有任何成功启动本地 WAV 或抑制原版音频的事件。
- Code/doc facts inspected: `AudioReplacementService.BeginAnimalSoundContext` 从当前动物实例建立作用域；`BuildAnimalSoundContext` 读取该实例的 `protoName`、幼年/成年阶段和对应声音事件；`IsEntryInScopeStatic` 要求 `AnimalVoice` 条目的 `speciesId` 与上下文 `SpeciesId` 完全相等，并同时核对期望事件及阶段。AnimalPack 田鼠虽然沿用 `PLAY_ANIMAL_PET_PANGOLIN`，其替换条目物种仍是 `mole`。1.00.06 `Animal.protoName` 返回 `proto.Id`，所以原版沼泽兽上下文是 `marsh_pangolin`。若某个声音从 `PlayAnimalSound` 之外直接发布而没有动物上下文，`AnimalVoice` 条目同样会因 `context == null` 而拒绝匹配。
- Codex inference: **本轮没有发现原版沼泽兽被田鼠声音拦截；按当前作用域代码，它也不应命中田鼠替换。** 两者共享 Wwise 事件名不足以构成匹配。用户听感上的不确定更符合“听到的是不熟悉的原版沼泽兽声音”，不是已证实的串音。
- Ownership: 声音作用域属于 GameBridge `AudioReplacement` SharedNative 边界；田鼠的声明式物种/事件配置属于 AnimalPack。当前两边的物种隔离契约一致，无需转移到 DebugConsole。
- Root-cause hypotheses: 当前没有确认的声音缺陷。若以后出现可复现串音，应优先检查动物上下文是否在异常路径残留、实际发布事件是否绕过 `Animal.PlayAnimalSound`，以及日志是否来自同一实例和同一时段。
- Rejected/unproven hypotheses: 已拒绝“只要共享 `PLAY_ANIMAL_PET_PANGOLIN`，原版沼泽兽就会被田鼠 WAV 替换”；已拒绝“本轮最后几次 Pangolin 事件必然属于原版沼泽兽”。用户的主观不确定不能单独证明声音身份，但也无须被当作失败。
- Required downstream updates: 当前不改 Hook map、公共 API 或声音实现。运行烟测矩阵应保留本轮原版 `marsh_pangolin` 控制证据，并把这项结论关联到本 Review。
- Acceptance checks: 若以后仍需把主观听感也闭合，应在一个隔离的 NoNativeSave 运行中依次触发原版 `marsh_pangolin.child/adult` 与自定义 `mole.child/adult`：前两者必须没有 DTMAPI 替换记录，后两者必须出现带 `species=mole` 的成功替换；必要时分别录制四段短音频供听觉 A/B，而不是只凭记忆判断。
- Blocker conditions: 当前路由结论无 blocker。只有“原版具体叫声听起来是什么”仍缺直接录音证据，但这不阻塞判定本轮未发生 DTMAPI 拦截。

### Issue 2: Y 控制台中的蛋图标显示完整动物

Original feedback:

- “而且目前图标也有问题，现在Y键控制台的蛋图标是完整生物，稍后修改，先查问题。”

Screenshot/log transcription:

- 用户没有附截图；视觉现象转录为：Y 键控制台的 AnimalPack 蛋物品卡片显示对应成年动物的完整精灵，而不是独立蛋形图标。
- 当前源包与本轮安装包的 `Content/item_tbitem.json` SHA256 均为 `8E9775B18D43CB89EC1C80E20D7C883AB7C6920F6316EFA53CC2FC1EB556A01C`，排除“安装目录仍是旧 JSON”这一分支。

Review record:

- User-confirmed facts: 七种蛋在 Y 控制台里呈现完整动物；用户要求暂不改图，先定位原因。
- Screenshot/log observations: 该现象与当前 JSON 一一对应，不是随机回退或某一个卡片绑定错位。
- Code/doc facts inspected: `item_tbitem.json` 将 `hatch_egg`、`petrified_hatch_egg` 指向 `anim_animal_hatch_adult_idle_0`，将 `drecko_egg` 指向 `anim_animal_drecko_adult_idle_0`，将 `mole_egg`、`fertile_mole_egg` 指向 `anim_animal_mole_adult_idle_0`，将 `oilfloater_egg`、`polymer_oilfloater_egg` 指向 `anim_animal_oilfloater_adult_idle_0`。DebugConsole 通过 `DolocAPI.GetItemSprite(item.Id)` 解析并绑定物品图标，目录读取也直接暴露原生 `UiSpriteAsset`；它没有把蛋 ID 改写为动物 ID。1.00.06 官方物品则使用 `icon_item_egg`、`icon_item_wool`、`icon_item_wool_grease` 等专用图标资源键。
- Codex inference: **这是 AnimalPack 物品 JSON/美术资产的已确认占位问题，不是 Y 控制台渲染 Bug。** 控制台只是忠实显示七条 `ui_sprite_asset` 所指定的成年动物帧。
- Ownership: AnimalPack 的物品数据与 PNG 资产。DebugConsole 不应加入按名称猜测“蛋图标”的产品特例。
- Root-cause hypotheses: 合包初版尚无七张专用蛋图标，因而临时复用了可解析的成年动物帧；当前源包和安装包完全一致，现象由该占位值稳定产生。
- Rejected/unproven hypotheses: 已拒绝“Y 控制台在卡片绑定时擅自换成完整动物”；已拒绝“本地安装包未同步最新 JSON”。是否可以只用一张通用蛋图暂未决定，属于后续美术方案而非本轮根因。
- Required downstream updates: 后续仍应回到现有 AnimalPack Update `20260827-0003` 完成接受阶段修正，不另建重复的实现生命周期。为七种蛋提供独立 `icon_item_*` PNG/资源键，至少保证普通产物与对应稀有产物可视觉区分；改动后无需修改 DebugConsole。
- Acceptance checks: 静态检查七条蛋记录不再引用任何 `anim_animal_*_adult_idle_0`；资源清单能解析全部新键；在 Y 控制台、普通背包/物品详情及小动物照料站配方界面各检查一次图标，确认没有粉色缺图、整只动物、裁切溢出或普通/稀有蛋混淆。
- Blocker conditions: 需要确定并取得七张蛋图的最终美术及来源记录。资源未准备好前可以保留现状，但不能把当前显示记为验收通过。

## Cross-Issue Summary

- Confirmed user facts: 本轮按“控制台召唤 -> 击打对应动物”手测后无保存退出；鸡羊听感正常，沼泽兽听感不确定；七种蛋卡片显示完整动物。
- Screenshot/log facts: 1.00.06 运行日志明确召唤原版 `marsh_pangolin`，但没有任何本地 WAV 成功播放或原版音频被抑制的替换事件；源包和安装包的蛋物品 JSON 完全相同。
- Code-path findings: 动物声音替换按实例 `protoName` 精确隔离，`mole` 不能匹配 `marsh_pangolin`；Y 控制台按物品 `UiSpriteAsset` 取图，七条蛋记录本身就引用成年动物帧。
- Risks: 继续仅凭听感可能反复怀疑共享事件名导致串音；继续使用成年动物帧会让控制台、背包和加工界面都呈现错误物品语义。相邻审查还发现四个 `sack_*` 物品也引用成年动物帧，但用户本轮只确认蛋图标有问题，动物袋是否更换专用图标应作为后续美术决定，不能混写成已确认缺陷。
- Suggested implementation scope: 本轮不实现。下一步只需在现有 AnimalPack 生命周期内完成专用蛋 PNG 和七条 JSON 键替换，再做小范围静态与实机视觉验收；声音路由保持不动。
- Items that should not be carried forward: 不要把共享 Wwise 事件名等同于共享替换作用域；不要在 DebugConsole 中添加 AnimalPack 特判；不要把本轮无替换日志夸大为已经录音识别原版声音。

## Implementation Record Decision

- Create/update an implementation update record: 本轮为 audit-only，不新建 Update，也不改实现状态；后续图标修正属于现有 [`20260827-0003`](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md) 的验收反馈，应继续由该 Update 持有直到验证。
- Additional debug/API/hook/smoke records required: 仅更新现有 AnimalPack 烟测行，加入第二次原版动物控制运行与本 Review；无已确认声音 Hook 缺陷，不新建 Debug issue，不改 API 或 Hook map。
- Suggested task titles: `AnimalPack 七种产物蛋专用图标与界面验收`；若以后仍需听觉证据，则单独使用 `原版沼泽兽与田鼠语音 A/B 诊断`。
- Completion standard: 七种蛋使用可追溯的独立图标并在控制台、背包和照料站均正确显示；声音项除非出现新的可复现反证，否则以“原版物种未命中自定义田鼠替换”为当前结论。

## 2026-08-28 User Clarification: Animal Bags

- 用户明确四个 `sack_*` 不需要各画一张动物图：游戏已有通用动物袋，袋子只是购买和出售动物的载体；每条自定义袋仍须保留自己的内部物种标签和名称。
- 用户指定凯涅尼木商店应出售四种幼体袋，并把成年动物基础售价冻结为对应幼体购买价的三倍。
- 这项澄清取代 Cross-Issue Summary 中“动物袋是否更换专用图标仍待决定”的开放项，但不改变 Issue 2 的七种蛋图结论：蛋仍需独立产物图，不能复用通用袋图。
- 实现和验证仍由现有 [`20260827-0003`](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md) 持有；本 Review 只保存用户约束，不复制实现叙事。

## 2026-08-28 User Clarification: Egg Icon Selection

- 用户提供本地研究索引 `E:/DolocTownUnity/ONIExtracted/creature_research_summary.csv`，要求从同一 `ONIExtracted` 素材树中定位蛋图标。
- 用户冻结哈奇的两种视觉身份：普通产物使用普通 Hatch Egg；“石化哈奇蛋”使用 Metal Hatch Egg，而不是最初候选的灰色 Heavy Hatch Egg。
- 用户把其余选择交给 Codex，并在看到候选说明后确认无异议：壁虎使用普通 Drecko Egg；田鼠使用普通 Shove Vole Egg / Delecta Vole Egg；浮游生物使用普通 Slickster Egg / Molten Slickster Egg。
- 这些仍是本地原型素材约束，不改变公开分发的资产来源 blocker。实现、哈希锁定和验证继续由现有 [`20260827-0003`](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md) 持有。

## 2026-08-28 Manual QA Extension: 小动物照料站视觉、掉落坐标与连续加工

### Extension Header

- Source: 用户在 1.00.06 本地运行中放置“小动物照料站”，依次取得 `fertile_mole_egg`、`oilfloater_egg`、`mole_egg` 后人工操作设备，并提交截图 `codex-clipboard-7d3f2a70-c0e1-4847-a312-193cf82e6ccb.jpg` 与刚结束运行的日志供审查。
- Time: 运行从 `2026-08-28 09:57:10` 左右开始，`10:03:26--10:03:28` 取得三种测试蛋，`10:03:29--10:04:59` 多次进入/退出 `GarbageShredderUiState`，`10:05:03` 前进程结束。
- Scope: 仅审查用户“问题一”的四个有序子项；本节不改 AnimalPack JSON、PNG、Runtime、游戏目录或存档，也不把这次人工退出当作保存语义证据。
- Evidence identity: 截图 SHA256 `13AB18CA666F06C8C9CE23BC95E3A34B97491C3F4CF9D0A30D03D3BDFE12857B`；运行后 `Player.log` SHA256 `B16F14B7A2295648A672B350C73CED4917544509CC6A712EE2571FB75DD238C6`；`BepInEx/LogOutput.log` SHA256 `7D7C8F14E6A0B12A177021F0784F01E984053EFB9DABD5038EBCF9937F2AB52F`。
- Related historical review: [`20260713-0002-garbage-shredder-last-run-log-review.md`](20260713-0002-garbage-shredder-last-run-log-review.md) 记录过第三方城市分解配方派生为零间隔并毒化单槽库存的旧问题；本节重新计算当前 AnimalPack 的实际间隔，不直接沿用旧归因。

### 问题一：设备视觉、产物生成点与第二次加工

Original feedback, preserving order:

1. 投入蛋后出现的图片太大；正常应只是设备上方一点点，并保持普通物品图标大小。
2. 设备没有贴地且大小不对；目标占地为 `2x4`，图像可见下界应接近图片底部，使设备落在地面上。
3. 分解产物的生成点高出设备约四格；目标是在设备所在位置生成。
4. 第一枚处理完成后，第二枚不再处理；用户判断纯 JSON 配置不应产生这种行为。

Screenshot/log transcription:

- 截图左下角明确显示游戏 `v1.00.06`。照料站可见图像悬在谷仓屋顶附近，`E 使用` 提示仍能出现；设备可见尺寸明显大于目标，底边没有落到场景地面。截图下方快捷栏仍有一枚田鼠蛋图标和数量 `1`；肉类数量为 `5`，这与本轮曾成功完成一条含 `meat x5` 的路线相容，但日志没有加工完成事件，不能仅凭快捷栏反推出第一枚的精确种类。
- 日志确认 Y 控制台分别成功给予 `fertile_mole_egg`、`oilfloater_egg`、`mole_egg` 各一枚，随后至少十次进入并退出原生 `GarbageShredderUiState`。整段尝试没有 `Exception`、`NullReferenceException`、越界、`垃圾分解机:掉落库不存在` 或 `垃圾分解机:无法分解道具` 记录。
- 日志没有设备内部 `IsWorking`、`Counter`、`inventory.FirstItemName/count`、`GetCurrentInterval` 或每次 `OnWorkDoneInternal` 的观测，因此它只能排除异常驱动的失败，不能证明第二枚是否成功进入内部单槽或工作计数器是否启动。

#### 1. 投入蛋后的提示图片过大

- Current data: 七张蛋 PNG 的画布为 `220x264` 到 `255x238`，不带同名 sprite 元数据 JSON；官方 `ModInfo.CreateSpriteFromFile` 因此使用默认 `pixelsPerUnit=8` 和中心 pivot。它们在世界渲染中的边长约为 `27.5--33` 个世界单位。1.00.06 原版垃圾分解输入图标均为 `28x28`、`8 PPU`，世界边长只有 `3.5`；当前蛋提示图约大了八至九倍。
- Native path: `GarbageShredder.RestartWork` 固定在 `base.Position + (0, 4.5)` 调用 `RaiseSpriteFadeUp(GetItemSprite(...))`。`recipe_tbdismantlerecipe*.json` 和 `equipment_tbequipment.json` 没有提示图缩放或偏移字段。
- Analysis: 已确认根因是高分辨率 ONI 图直接进入官方默认 `8 PPU` 世界精灵路径，不是 DTMAPI、Y 控制台或配方 JSON 把图放大。`+4.5` 是 1.00.06 原生硬编码；在设备恢复原版像素尺度后，它会回到原版分解机预期的设备上部位置。
- Minimal future boundary: 仍可保持纯官方 JSON/PNG。应把七张蛋图制作成原版物品图标尺度的画布，或为每张图提供能得到同等世界尺寸的同名 sprite 元数据；不能只依赖 UI 自动缩放，因为世界提示直接使用 Sprite 的 PPU。

#### 2. 设备未贴地且尺寸不是 `2x4`

- Current data: `equipment_tbequipment.json` 仍是早期探针继承的 `cover_size={x:6,y:4}`，不是用户冻结的 `2x4`。照料站 PNG 为 `178x249`；元数据为像素 pivot `(89,2)`、`32 PPU`。透明度实测可见边界为 `x=14..163`、`y=16..232`，因此画布本身还有约 14 像素的底部可见间隙。
- Native comparison: 原版农场垃圾分解机为 `71x48`、`8 PPU`、pivot 约 `(35,2)`、`cover_size=6x4`；其他原版四格高畜牧/工业设备同样约为 48 像素高。官方设备几何把 PNG 原始像素视为 `8 PPU` 设计空间。
- Analysis: 当前同时混用了“高分辨率 178x249 图”“32 PPU 视觉缩放”和“原垃圾分解机 6x4 占地”。放置碰撞仍按 `6x4`，图像却没有按 Doloc Town 原生约 48 像素高的设备画布重制，因而占地、可见底边和场景位置不再是一套坐标。现有 bottom-center pivot 方向本身正确，但不能抵消错误的画布尺寸与占地。
- Minimal future boundary: 把 `cover_size` 改为用户指定的 `2x4`；把源图按原版像素设备规格重采样到约四格高的画布并重新确定 bottom-center pivot，使可见底边只留约 1--2 像素。仅修改 `pixels_per_unit` 不足以修复后续几何计算。

#### 3. 产物生成点过高

- Native path: `GarbageShredder.DropAsync` 对每一个产物调用 `EquipmentPatch.CreateDropItem`；渲染中的固定目标是 `instance.PositionCenter`，配方和掉落 LUT 没有坐标字段。`Equipment.PositionCenter` 又来自 `CalcSizeInfo`，其高度直接按 `(proto.SpriteSize.y - 2) * 0.125` 计算；`EquipmentInfo.SpriteSize` 返回的是 `Sprite.rect.size` 原始像素，不读取 PPU。
- Exact current effect: 当前 249 像素高的照料站把生成中心算为 `base.Position.y + 15.3125`。原版 48 像素高垃圾分解机的对应中心只有 `base.Position.y + 2.75`。当前生成中心约为原版的 `5.57x`，与用户看到产物远高于设备一致。
- Analysis: 这是“高分辨率设备 PNG 进入原生按像素算设备几何”的确定性结果。提高 `pixels_per_unit` 只能缩小屏幕上的设备图，完全不会降低 `PositionCenter`；必须降低 PNG 的原始像素尺寸。设备图恢复到约 48 像素高后，原生生成点会落回设备内部/附近，无需 DTMAPI Hook，也没有可供 JSON 单独覆盖的 output-offset 字段。

#### 4. 第一枚之后第二枚不再处理

- Current derived value: 独立组 `time_ratio=1`，七条配方均为 `cost_time=12`，所以每条实际间隔都是 `RoundToInt(1*12)=12`，不会触发 2026-07 城市分解机的 `Work(0)` 静默失败。
- Native queue semantics: `GarbageShredder.totalCapacity=1`、`lineCapacity=1`。`max_craft_count=5` 是同一种物品在唯一槽内的最大堆叠数，不是五种不同物品的队列。第一种蛋仍在槽内时，第二种蛋不能同时排队。
- Native continuation semantics: 一次完成后，`OnWorkDoneInternal` 固定扣除槽内数量 `1`；若同一堆叠仍非空，立即调用 `RestartWork`。因此“两枚相同蛋作为一个堆叠提交”按 1.00.06 源码应自动连续处理，第一枚的 `DropAsync` 也以 fire-and-forget 方式运行，不会等待全部喷出后才启动下一枚。
- Analysis: 本轮已确认玩家可见的“第二枚不动”，但现有日志不足以把它定成配方错误。若第二枚是不同蛋，最先应核实它是否因原生单槽规则根本没有入槽；若第二枚已经可见地留在槽内且 `IsWorking=false`，才成立为 `OnContainerUiExit`/worker 重启的原生状态问题。当前没有证据支持 JSON 解析失败、零间隔、缺失 LUT 或异常中断。
- Required acceptance split: 下一次只需一个窄测试先提交同种蛋 `x2`，记录两次扣减/两次完成；再在设备清空后依次提交两种不同蛋。需要在 `RestartWork` 与 `OnWorkDoneInternal` 边界临时记录 item、count、interval、`IsWorking` 和 counter。只有“第二枚已入槽、interval=12、仍未启动”才能升级为机器状态 Debug issue；在此之前不改 GameBridge、不给纯 JSON 内容补产品特例。

### Extension Conclusion

- 子项 1 已定位：蛋 PNG 使用官方默认 `8 PPU`，世界提示约为原版输入图标的八至九倍。
- 子项 2 已定位：设备仍错误沿用 `6x4`，且 178x249 高分辨率画布没有转换到 Doloc Town 原生设备像素规格。
- 子项 3 已定位：原生按 PNG 原始高度计算 `PositionCenter`，249 像素把产物生成点推到 `+15.3125`；修改 PPU 无效，必须重采样设备 PNG。
- 子项 4 仍是有边界的未决项：已排除异常、缺表、缺 LUT 和零间隔；尚缺“第二枚是否真正入槽”及 worker 状态证据。原生只支持一个物品种类槽，但同种堆叠应自动连续加工。
- Implementation record decision: 本节只追加 Manual QA 审查，不创建新 Update 或 Debug issue。后续几何/图标修正继续由现有 [`20260827-0003`](../../../../updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md) 持有；连续加工只有在窄复现证明原生状态失配后再建 Debug issue。

### 2026-08-28 User Clarification And Authorized Fix Boundary

- 用户把子项 1 与子项 3 归并为同一个视觉修正：应直接缩小蛋 PNG 到原版物品图标尺度，不为这两个现象增加 Runtime Hook 或额外坐标配置。设备 PNG 同时缩到原生约 48 像素高后，原生按图片高度计算的 `PositionCenter` 也会随之回到设备附近。
- 子项 2 不只是改 `cover_size`：必须裁掉照料站源图四周透明边，尤其让可见底边贴近输出画布底部；目标仍为 `2x4` 占地和 bottom-center pivot。
- 子项 4 的人工步骤已进一步明确：第一种单件蛋已成功加工；设备清空后，用户把另一种单件蛋放进设备可见槽位，但设备没有再次启动。该澄清排除了“第二种蛋只是与第一种同时排队”的用户操作情形，但日志仍没有内部 inventory/worker 状态，不能据此选择 JSON 或原生状态机修法。
- 本轮授权只实现前三个视觉/几何修正。子项 4 不改配方、容量、Runtime 或 GameBridge，由用户在新图像包上再做一次窄手测；若仍能复现，再按上一节列出的状态观测边界升级调查。

## 2026-08-28 Manual QA Follow-Up: 第二枚蛋完成显示后无掉落

### Follow-Up Header

- Source: 用户在前三项图像修正后的 1.00.06 本地包上再次人工验证“小动物照料站”，随后直接退出并要求检查最新日志。
- User-confirmed order: `1/2/3` 当前整体表现正常；设备视觉更接近 `2x3` 而不是目标 `2x4`，但用户明确留待稍后调整。第 `4` 项中，第一种单件蛋完成加工并已收获；设备清空后放入另一种单件蛋，设备显示运行，显示时间到达终点后没有产物并保持卡住。
- Scope: 本节只审查最新日志、当前部署 JSON 与 1.00.06 原生加工/跳时路径；不继续调整占地或 PNG，不修改配方、Runtime、GameBridge、游戏目录或存档。
- Evidence identity: `Player.log` 路径 `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/Player.log`，末次写入 `2026-08-28 15:50:36 +08:00`，SHA256 `0A81F9EE488DB0308C331E1A24A0C0A4661035C29596E0F13F67FAC2C4CA3631`；`BepInEx/LogOutput.log` 末次写入 `15:50:33 +08:00`，SHA256 `0C4DB64CDAA3280A5934AD40B13AF15C6B3D81B62499D07763E9190D96EF5984`。
- Runtime identity: 本轮加载 UI 第七个存档位置，对应原生 `slot=6`；AnimalPack 从 `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_AnimalPack` 加载。当前源目录与部署目录的 `recipe_tbdismantlerecipe.json`、`recipe_tbdismantlerecipegroup.json`、`item_tbitemspawn.json`、`equipment_tbequipment.json` SHA256 分别一致。

### 按用户顺序记录

1. 投入蛋后的提示图片：用户本轮确认当前看起来正常；不再重复前一节已经完成的像素根因分析。
2. 设备落地与大小：用户确认落地等前三项当前基本正常，但可见体量更像 `2x3`；JSON 的逻辑 `cover_size` 仍是 `2x4`。本轮只登记“可见美术尺寸与逻辑占地观感仍不一致”，按用户要求稍后再调。
3. 产物生成点：用户本轮确认当前看起来正常；本节不再改坐标或图片。
4. 连续加工：第一枚单件蛋成功完成并收获；第二枚是设备清空后提交的另一种单件蛋，已可见进入运行态，随后出现“显示时间结束、无掉落、机器卡住”。这项人工事实取代上一节“第二枚是否真正入槽/启动仍未知”的旧边界，但日志仍没有机器内部字段，尚不能仅凭外观判断具体停在完成回调、扣料还是掉落阶段。

### 最新日志时间线

- `15:49:46.364--15:49:51.577`，Y 控制台依次给予 `animal_care_station` 与七种测试蛋各一件。日志随后记录第一次 `GarbageShredderUiState` 进入/退出；用户确认这一轮的蛋已正常加工并收获。
- `15:50:14.661`，第一次加工之后、第二次设备 UI 之前，Y 控制台执行一次 `time-next-weather-period`，推进 `225` 游戏分钟 / `180` 原生秒；当前 `扩展农场_3` 被清空渲染后重新渲染。
- 第二次 `GarbageShredderUiState` 进入/退出发生在 `15:50:14.851` 之后、`15:50:20.442` 之前。结合用户动作说明，这是第二种单件蛋提交并启动的窗口。
- `15:50:23.178`，第二枚仍处于其正常 `12` 秒窗口内时，Y 控制台再次执行 `time-next-weather-period`，推进 `715` 游戏分钟 / `572` 原生秒；`扩展农场_3` 再次执行 `ClearRender -> Render`。第二次 UI 最早只能在 `15:50:14.851` 后启动，因此这次跳时必然早于十二秒实时加工上限，直接穿过了第二枚的预期完成边界。
- `15:50:23.534` 之后出现第三次 `GarbageShredderUiState` 进入/退出，用户在 `15:50:30.175` 前结束检查，随后返回标题并正常退出。
- 两份日志都没有相关 `Exception`、`NullReferenceException`、`垃圾分解机:掉落库不存在` 或 `垃圾分解机:无法分解道具`。日志也没有记录第二枚具体 item ID、设备 `IsWorking/IsIdle`、counter、内部槽数量、扣料结果或 DropItem 数量。

### 数据与原生路径复核

- 当前七条配方全部是 `cost_time=12`，独立组 `time_ratio=1`，实际间隔固定为 `RoundToInt(1*12)=12`。安装包与源包一致，AnimalPack 静态验证再次通过；这不是 2026-07 城市分解配方的零间隔问题。
- 七个输入都在同一个已解析配方组中，七个 LUT 均存在，输出物品 ID 也通过 1.00.06 本地表验证。当前没有证据支持“第二种蛋缺配方”“第二个 LUT 未合并”或安装目录残留旧 JSON。
- 1.00.06 `IEquipmentWorker.UpdateWorkerNoRender` 在 counter 完成时先把 `IsWorking=false`，再调用 `FinishWorkNoRender`；`GarbageShredder.OnWorkDoneNoRender` 应进入 `OnWorkDoneInternal(false)`，扣除槽内一件并启动掉落。控制台跳时调用原生 `ArchiveDataHandle.PassTimeNoControl`，其流程是先对当前房间 `BeforeTimePass/ClearRender`，逐秒调用 `UpdateNoRender`，最后 `AfterTimePass/Render`。
- 原生 `DropAsync` 不是整批瞬间生成，而是每 `200ms` 生成一个单位。当前批量为 `3/10/15/25/35/60`，整批喷完约需 `0.6/2/3/5/7/12` 秒。第二次跳时后到退出约七秒，所以若第二枚是 `35` 或 `60` 单位配方，不能据本轮时长要求整批都已喷完；但原生完成路径应立即创建第一个单位并清掉输入，这个异步节奏本身不能解释用户确认的“完全无产物且机器仍卡住”。
- Repo 静态搜索没有 ActionSpeed、OneActionComplete 或其他 DTMAPI 产品修改 `GarbageShredder`、其配方间隔、worker counter 或内部库存；本轮日志也没有适用于该设备的加速/一键完成记录。

### 根因状态

- Confirmed: 第二枚不是与第一枚争用单槽；它在设备清空后作为不同的单件输入提交，设备进入了玩家可见运行态。配置间隔为正，源包与部署包相同，配方与 LUT 静态有效，运行期间没有相关异常。
- Leading hypothesis: **第二枚加工与 Y 控制台的 `PassTimeNoControl -> ClearRender -> UpdateNoRender -> Render` 边界重叠，是本轮最强的差异项。** 第一枚按实时路径成功；第二枚在十二秒内被 `572` 原生秒跳时跨过，随后出现卡住。现有原生日志没有机器阶段遥测，因此这只是有直接时序证据的首要嫌疑，尚不能宣称 DebugConsole 或原版无渲染完成路径已被证明有 Bug。
- Rejected: 零间隔；第二枚仅因单槽没有入槽；源包/部署包不一致；缺配方；缺 LUT；已记录异常；ActionSpeed/OneActionComplete 介入。
- Still unproven: counter 是否在跳时循环内到达完成；设备是否因电力进入 `IsIdle`；`FinishWorkNoRender` 是否被调用；输入是否已扣除；第一个 no-render DropItem 是否写入房间 DropItemManager；重新渲染后机器外观是否只是残留工作态。当前日志无法区分这些阶段。

### 下一次最小判别测试

1. 使用同一设备，第一枚完成并收获后再放入另一种单件蛋；第二枚全程不使用 Y 控制台跳时，至少等待 `15` 秒。若成功，问题收窄为跳时/无渲染完成边界；若仍失败，则进入普通 `OnContainerUiExit -> RestartWork -> FinishWork` 路径调查。
2. 只在需要复现跳时分支时，第二枚启动后再执行一次 `time-next-weather-period`，与第 1 步做 A/B；不要把两种路径混在同一个结论里。
3. 若任一路径再失败，下一次实现应只增加临时观测：`OnContainerUiExit/RestartWork` 的 item、count、interval、`IsWorking/IsIdle`、counter；`FinishWork(NoRender)`；`OnWorkDoneInternal` 扣料前后；`DropAsync` 首个/最后一个单位及房间 DropItem 数。先定位阶段，再决定是否需要产品代码；不先改容量、配方或补第二枚特例。

### Record Decision

- 本轮为 audit-only，只追加现有 Manual QA Review；不新建 Update，不改现有 AnimalPack 实现状态。
- 由于本轮第二枚路径含跳时混杂且缺内部状态，暂不新建机器 Debug issue。若“不跳时也失败”或 A/B 明确只在 `PassTimeNoControl` 失败，再建立单一 Debug issue，并由现有 AnimalPack Update 持有后续实现与游戏验收。

### 2026-08-28 User Correction: 两枚蛋都通过 Y 跳时完成

- 用户补充确认：第一枚正常产出的蛋同样使用了 Y 控制台跳时，并非实时等待完成。此前“第一枚按实时路径成功，第二枚才与跳时重叠”的 Leading hypothesis 因此失效；本节明确取代上一节对应推断，但保留原文字作为审查过程记录。
- 修正后的顺序是：第一次设备 UI 提交后，`15:50:14.661` 执行 `time-next-weather-period`，推进 `180` 原生秒，第一枚正常扣除、产出并由用户收获；第二次设备 UI 提交另一种单件蛋后，`15:50:23.178` 再执行同一动作，推进 `572` 原生秒，第二枚出现显示完成但无产物并卡住。
- 1.00.06 设计复核与用户理解一致：`ArchiveDataHandle.PassTimeNoControl` 在房间清除渲染期间逐秒调用农场 `UpdateNoRender`；房间先更新 `ElectricSystem`，再更新 `EquipmentManager`，`IEquipmentWorker.UpdateWorkerNoRender` 应正常推进这类农场器械。第一枚在同一路径成功也是直接运行反证，因此“Y 跳时普遍不支持照料站”已排除。
- 两次仍可确认的差异只剩：跳时长度与所跨时段（`180s` 对 `572s`）、输入蛋/配方和输出批量不同，以及这是同一设备的第一轮与第二轮。两次都远超 `12s` 配方间隔。照料站阈值为每设备秒 `1`；其完成一轮只需约 `12` 点电，而此前同一农场面板为 `4500/4500`，所以长跳时造成完成前断电目前只是低概率、且没有日志支持的分支。
- 官方启用状态复核：本轮只有 `Local.DTMAPI_AnimalPack` 启用；旧的 `HatchAssets`、`DreckoAssets`、`MoleAssets`、`OilfloaterAssets` 均为禁用，安装 JSON 中目标配方/LUT ID 也只由 AnimalPack 定义。因此多包重复注册或旧单动物包覆盖当前配方不是本次故障来源。
- 原生 `GarbageShredderUiState` 每次 `HandleStartUpArgs` 都会重新绑定容器、时间读取器与退出回调，`Hide` 每次都会调用当前 `onCraft`；日志也记录了第二次和第三次正常 `Enter/Exit`。静态源码未发现“UI 只在第一次关闭时重启设备”的固定缺陷。
- 当前日志仍不记录用户在两轮中实际选中的蛋 ID，也没有 worker counter、`IsIdle`、扣料和首个 DropItem 事件。因此不能把故障可靠归到某一条 JSON 配方，也不能证明是连续第二轮状态机问题；继续猜修会同时改动七条已通过静态解析的配方，缺少依据。

#### 修正后的最小复现

1. 记录本次失败的那枚蛋名称；清空或重新放置设备后，把它作为**第一枚**投入并照常用 Y 跳时。
2. 若它作为第一枚也失败，故障优先归到该蛋的配方/输出或特定时段；若它作为第一枚成功、但任意另一枚完成后再投入就失败，才成立为设备连续第二轮状态问题。
3. 失败后先不要退出，重新打开设备确认蛋是否仍在槽内，并观察左上角是否出现缺电；大批量输出最多再等 `12s` 实时，以排除 `DropAsync` 每单位 `200ms` 的喷出延迟。
4. 若再次复现，下一轮只加阶段遥测，不先改 JSON：记录所选 item ID、`RestartWork` 的 interval/worker 状态、`FinishWork(NoRender)`、扣料前后，以及 `DropAsync` 首个单位。这个证据可以一次区分配方特异、第二轮重启、断电待机和掉落阶段。

#### Corrected Record Decision

- 本次更正不授权实现，仍为 audit-only；不新建 Update 或机器 Debug issue。
- 在取得“失败蛋作为第一枚”的判别结果前，不再以跳时本身作为首要根因，也不修改容量、配方、LUT、Runtime 或 GameBridge。

## 2026-08-28 Manual QA Closure: 酸雨按原版机制暂停设备

### 用户最终反馈

1. 用户重新进行普通等待测试，连续加工无问题。
2. 用户重新进行 Y 控制台跳时测试，加工同样无问题。
3. 前一次看似“时间结束但无产物”的根因是跳时跨入酸雨时段；小动物照料站按农场器械机制在酸雨期间不工作。用户确认设备、普通时间推进和跳时路径均正常。

### 代码与配置复核

- AnimalPack 的 `equipment_tbequipment.json` 明确为照料站配置 `WDP_AcidRain_Worker`，`durationTu=1`；该处理器不是 DTMAPI Hook，也不是为本设备单独编写的状态机。
- public `1.00.06 / 24966367` 的 `Equipment.WeatherDecoratorManager.SetWeatherType` 在酸雨属于恶劣天气时启用对应 `WD_AcidRain_Worker`。装饰器存在期间，设备管理器把正常 `Update/UpdateNoRender` 路由给天气装饰器；`WD_AcidRain_Worker.Update/UpdateNoRender` 在酸雨仍进行时不调用原设备更新，因此 worker counter 按设计暂停。酸雨结束后，恢复计数器达到本包配置的 `1 TU` 即移除装饰器，后续重新进入原设备更新。
- 这条原生责任链完整解释了先前现象，也与“普通等待成功、非酸雨跳时成功、跨入酸雨时暂停”的最终 A/B 结果一致。无需为 `GarbageShredder`、Y 控制台、配方、掉落或连续第二轮增加补丁。

### Closure Decision

- 连续加工、Y 跳时和 no-render 完成路径当前均判定正常；先前的第二枚“卡住”不升级为 Debug issue。
- 保留 `WDP_AcidRain_Worker`，因为它正是照料站应遵守的原版农场器械天气规则。后续说明文档应直接告知玩家“酸雨期间暂停”，避免把预期天气状态再次误判为配方或设备故障。
- 本 Review 的设备问题至此关闭。AnimalPack Update 仍可因尚未覆盖全部七条产物、隐藏贡献、商店购买与售价验收而保持 `Runtime Validation: partial`；这一点与本问题是否正常无冲突。
