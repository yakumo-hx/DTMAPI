# 音频与原生内容资产边界

本文合并历史定位线索，当前 API 等级查 [public matrix](../../api/public-api-matrix.md)，责任划分查 [PROJECT](../../../PROJECT.md)，原文与基线校验查 [API 阅读清单](../../archive/migrations/20260908-workspace.json)。旧实现提案没有在本次重新核验。

## 声音替换的作用点

[纸箱 native review](../../archive/reviews/api/2026/20260617-0001-audio-replacement-native-owner-review.md) 把 E 交互的 particle/drop/removal 与音频发出分开：DungeonResourceModelPaperBox.OnInteract 仍拥有玩法，窄 PLAY_RESOURCE_PAPER_BOX 通过 WwiseSoundManager.InternalPostSoundEvent 发出。不能为了换声音重做掉落，也不能误换 PLAY_ITEM_BOX 或垃圾事件。该短 review 未单独声明精确 game build，不补造基线。

[event-map/backend research](../../archive/reviews/api/2026/20260617-0002-audio-replacement-event-map-backend-research.md) 的长远方案是按 Wwise event ID/name 分类，并非每种声音建一个 gameplay Hook。短 SFX 的播放成功不证明 loop、STOP、callback、3D、BGM、RTPC 或 bus；原声抑制必须以替换确实可播放为前提。SoundPlayer 当时仅证明本地文件/Hook 链，不拥有 Unity/Wwise mixer、暂停或音量。

其 Wwise custom bank redirect 与 Unity pool 是研究方案，不能写成已实施 backend。自定义 Init.bnk、同名事件碰撞、将 STOP 映射成普通文件，都没有被该研究证明安全；Wwise bank 需匹配游戏 runtime。后续最小 probe 的是否完成与当前可用分类由实际音频 owner 决定，不因历史建议自动启动一次完整研究。

## 内容表与实际 UI/实例的差异

[Lightning Chicken blocker](../../archive/reviews/api/2026/20260618-0001-lightning-chicken-native-store-release-blocker.md) 记录一个确定失败：TbStoreItemList 看到袋子，但打开的 StoreUiState.storeItemCaches 没有，显式 RefreshStore 仍未出现。该记录没有单独声明 native build，不将其字段状态推成当前版本。可能的 table merge → save Store.currentItems → UI cache 断点是线索；不是已证明根因。

没有买到/释放/渲染实例，就不能把静态 AI 映射和 AnimatorOverrideController 计划写成 runtime 验收。直接 GenerateItem 不能替代当次用户指定的买入路径；反之，这项特殊购买要求不是每次动物内容变更都要重跑的默认前提。后续解决状态由 CustomAnimals/该动物产品的最新记录拥有。

## 自定义动物从研究到可用内容路线

[June 17 AI/animator research](../../archive/reviews/api/2026/20260617-0004-custom-animal-json-ai-animator-research.md) 明确基线 `23762374_public_C416D4`，当时只有四种既有模板，并未导出完整 controller/clip/prefab 树。其 JSON AI DSL 与 sprite-frame player 是提案；官方 Sprite override 不证明 RuntimeAnimatorController 可由普通 JSON 加载。registry RequestSpawn 与后续内容袋子释放始终是两条路径。

[July 5 content review](../../archive/reviews/api/2026/20260705-0003-custom-animal-content-boundary-review.md) 已有 Hatch/Mole/Drecko/Oilfloater 的 JSON+PNG+WAV 实例：pack 拥有身份、资产和数值，DTMAPI 提供模板/逐 renderer 图像/短音频桥，native 拥有袋子释放、成长、照料、生产和保存。该表是 July 本地包库存，不是当前 Catalog。新物种复用 native template 不等于新增 AI/第五种生产设备；漏帧/错 sound event 会 fallback，不能因没有报错就认为全部自定义资产被使用。

[July production config review](../../archive/reviews/api/2026/20260701-0001-custom-animal-production-config-review.md) 的来源未固定到单一 build，因此其数值须按当前基线核对。有效语义：普通 produce_spawn_entry 和隐藏 husbandry progress 分开；默认 feeder 只给能量，携带具体食物 ID 的摄食路径才可能推进隐藏产物；room space、地面 size、stage sprite_size 各有不同含义。设备按模板/可达性找而非按物种专属，混养会共享产物容量，容量按 item unit 计算；繁殖判断同 protoName。288 TU/天及 10/20 容量都是该时点的观察，不能脱离能源、心情、设备条件保证日产量。隐藏产物没有在该 pass 做自定义 pack runtime 验证，当前验证归对应内容 owner。

[July short-SFX decision](../../archive/reviews/api/2026/20260713-0003-short-sfx-content-and-api-boundary-review.md) 选择 JSON + reviewed SoundKey 路线，Manbo 是静态数据迁移的 Canary，身份不另建；schema、受保护产品行为和 narrow Experimental backend 分别评估。该时点 SoundPlayer 不应用 Volume，因此不能把字段存在当音量支持。长期成本来自每帧扫描/sort/File.Exists/signature 和事件期重算，不来自声明性 JSON 本身。事件/lifecycle generation 刷新、显式 author reload 与临时 pending pump 是当时选定的优化方向，是否已实现看 current owner。

[BGM T0 decision](../../archive/reviews/api/2026/20260713-0004-bgm-native-lifecycle-project-boundary-review.md) 没有承诺 BGM schema、文件格式或产品。177 events、47 MUSIC/130 SFX 和 Wwise 2023.1.0 字符串是当时未单列 build 的观察，不能由版本字符串证明外部 bank 兼容。Native callback/playing-id/STOP/随机间隔是状态机；短音频成功不能换成长期音乐支持。

[Oil native pool review](../../archive/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md) 的后补决定优先于正文示例：选定仅 append Oil weight 25，接受 amber 从 1% 到约 0.976%，没有采用示例重配 coal/amber 向量。coal flat LUT 与 fishing rarity buckets 不同；加一项不能保证原项绝对概率，也不是独立额外掉落；max_count=0 在该 native 语义是 unlimited。列表 remove/append 顺序和 capped amber fall-through 影响实际分布。P0 已由确定性算法覆盖加自然 world-drop 及 Oil-off、OneAction 解耦关闭，大统计样本只属于经济平衡/提升，不是继续阻塞所有权修复的门。该 review 未单列精确 build，本次不重验概率或推广到未来基线。

## 1.00.00–1.00.05 的动物、音乐与模板资源复查

[8 月 2 日定向复查](../../archive/reviews/api/2026/20260802-0001-walkman-vehicle-animal-monster-native-owner-followup.md) 比较 `24256979_test_7A1907` 与 `24456188_test_E861E0`。随身听/CD 仍编排已有 Wwise event，JSON 新 event 名不创建媒体；通用 loader 可命中内部 CD/BGM 表，不等于官方作者契约。自定义媒体仍需 callback、STOP、loop、音量、bank 和场景恢复所有权。CD 反序列化会过滤当前表不存在的 ID，禁用内容后保存有丢失解锁/playlist 的风险。摩托仍为一个 `DolocAPI.Motor`、一个 controller、一份保存和全局参数；皮肤表不是车辆实例 registry。

同一对比中，动物主要变化是逃跑点增加 `CurrentEnv` 限制、Wwise 注册从 render/unrender 移到 pooled renderer 创建/销毁。AI/Animator/畜牧表和保存主链没有全面漂移，因此原 Review 明确要求定向回归而非重做动物路线。模板怪物则至少要分开新内容 ID、prefab/pool alias 和 AI name；生成与回收必须使用一致 alias，未知 name 不自动继承模板 AI。仅有 `TbMonster.prefab` 字段或新 JSON 行不能证明可运行的新怪物。

[8 月 27 日畜牧审查](../../archive/reviews/api/2026/20260827-0001-livestock-interface-10005-official-content-audit.md) 的当前观察头为 `24788406_public_F06183` / `1.00.05`，直接前版 `24650773_public_76C24E` / `1.00.03`。选定 19 个 native 文件全部相同；最早代码 `23249387_workshop_247ACD` 没有完整 GenDatas，不能拿 `23762374_public_C416D4` 的最早完整表伪称为 0.96.05 配置。该文第 8 节已经用 [owning Update](../../archive/updates/2026/20260827-0001-custom-animal-10005-slot7-runtime-acceptance.md) 把前文 runtime pending 解析为当前安装边界 passed：四物种 PNG/Animator/AI、8 个 WAV ready、Hatch 幼体/成年实际播放且抑制 native，两个运行无缺帧回退。

多个独立 `speciesId` 可以共享 `chicken` 模板：必须独立的是包 ID、species、animator key、sprite prefix、物品/产物/表主键及 owner 内音频 ID；AI 模板和原生声音事件本身不是唯一键。追加探针 `GAME-SMOKE/20260827-135817` 已验证 Hatch 与 Mole 同时复用鸡模板时的注册、AI、Animator、PNG 隔离，没有存档备份/回写，临时修改的是 Mole 包并按 58 文件哈希恢复。该“仅加载”探针不证明产物/繁殖/设备满载和两种 WAV 的逐只实际播放。共用鸡行为也会共享原生鸡窝容量，收取产物可能混合，这与贴图/声音注册污染不同。

官方文档仍只公开四原动物 PNG 美化；未知新 species 的 Animator 加载、物种 PNG 隔离、专用 AI 映射和 WAV 加载由 DTMAPI 内容桥补齐。“作者包不含 DLL”不等于“玩家无需 DTMAPI”。公开 `ICustomAnimalApi` 仍只是 Frozen registry，不能用内容桥运行成功给旧 C# spawn 接口解封。作者当前合同在 [自定义动物指南](../../../author-docs/content-packs/custom-animal-json-png-wav.md)。
[Hook 历史总表](../../archive/hook-map/2026/README-history-through-20260711.md) 还保留两类易失真的原生资产经验。June 28 自定义动物路径对闭合泛型 `DolocAssetCache.GetAsset/CheckAsset<RuntimeAnimatorController>` 的 Hook 污染 Unity/Mono 共享泛型缓存，导致天气与 UI prefab 异常；后续缩到非泛型 `AnimatorAsset.TryLoadAsset` 的已注册 key。该记录相关动物基线为 `23762374_public_C416D4`，旧实现细节不直接证明当前游戏仍需相同 Hook。

动物睡眠问题也分两层：proto 名与 `schedule_id` 不匹配会选错 `GetDefaultAnyState`；即使进入 `Normal_SleepState`，已存在的移动任务仍可能继续。历史修复限定自定义物种、夜间且 `isSleep`，避免影响去床途中、早晨或工具唤醒。June 30 的五夜用户确认已关闭此前 pending；不能把旧 pending 重新当作动物接口发布阻塞。

音频替换的 cooldown 与未准备好是不同结果：已准备好的替换正在 cooldown 时可以是 `played=false, suppressed=true`，防止重复播放；文件缺失、失效或不支持的 emitter/callback 才走 native fail-open。后续后端与正式合同仍以当前音频 owner 为准。