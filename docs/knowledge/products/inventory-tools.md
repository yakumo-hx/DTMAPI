# 跨箱取物与种植枪：原生库存的边界

现行行为、身份与 API 状态分别查产品源码、[Catalog](../../../tools/release/dtmapi-product-catalog.json) 和 [API 矩阵](../../api/public-api-matrix.md)。这里只保留历史审查的原生责任和证据区别。

ChestLocatorEnhancer 在 `ArchiveDataHandle.GetAvailableInventories` 扩大返回的原生 LinearInventory 集合；物品、Count/Cost 事务、持久化及 UI 仍由游戏负责。不同房间树可能交叠，应按对象身份去重；useBox、autoUseBox、Case/Shelf/ItemBox 开关要执行行为测试，不能只搜 token。曾发现 query 热路径把不用的 Vector2Int 装箱、重复反射和建集合、每次成功追加就写日志；后续纠正缓存 metadata、复用 scratch，并限定首次/状态变化日志。这个查询不是已证明每帧执行，不应夸大成永久 updater。

过程常驻 Hook 可以在 SaveLoaded/title 只清观察状态，禁用/owner 停用再移除；生命周期契约不要求每次标题重装。旧“两个物理 owner 顺序均通过”的结论后来被限定为真实 Harmony fixture 与注入兼容观察的组合证明，并非两个生产 executor 在 Unity 原生目标上的完整集成。两种证据的范围应如实保留。

StrongPlantingGun 的五个具体 patch 是两个构造器、工具使用、两条 UI 转移。游戏序列化每把枪的 LinearInventory；产品扩容量并快照共享 ItemFunctionFarmingGun.Capacity，不得收缩掉已占槽位。种子、薄膜、肥料的固定三槽不因迁移加入水。其历史没有真实已发布旧消费者，所以冻结 API 警告壳不等于要增加 Compatibility executor 或虚构双序测试。只有找到具体旧二进制才重开该边界。

Strong 的 SaveLoaded 修复只证明背包顶层已反序列化枪恢复三槽，不能泛化为仓库及嵌套容器即时修复。Title 后同一已观察 exact owner/目标/patch 方法在原计数内重装，是当时 Core 生命周期修正的有限范围；不能放宽任意重复 Hook。其物品保存场景确实触发原生存档专项，普通元数据修正则不继承整套流程。

来源：[Chest 准入](../../archive/reviews/code/2026/20260723-0005-seventh-product-chestlocator-admission-review.md)、[初次最终审查](../../archive/reviews/code/2026/20260723-0006-chestlocator-seventh-product-final-review.md)、[证据与热路径纠正](../../archive/reviews/code/2026/20260723-0007-seven-product-commit-range-audit.md)、[横向比较](../../archive/reviews/code/2026/20260723-0008-seven-product-horizontal-comparison-and-eighth-candidate-decision.md)、[组合证据限定](../../archive/reviews/code/2026/20260723-0010-eighth-product-admission-runtime-weight-and-api-reuse-audit.md)、[Strong 准入](../../archive/reviews/code/2026/20260724-0003-ninth-product-strong-planting-gun-admission-review.md)、[Strong 保存重入边界](../../archive/reviews/code/2026/20260724-0004-eighth-ninth-product-split-closeout-audit.md)。

作物收获 6 月手测只确认 F9 单株/F10 多株且物品进背包；未成熟、重复、跨房间、vine/mushroom/bush、tree-basin scan-only、野草/野树拒绝、满背包及重载仍是当时未测项。不能把普通 PlantBasin.Harvest 的通过泛化为所有植物，更不能与后续 Y 催熟的三类 DEBUG_SetLevel 混同。当前 API 能力由 API owner 决定，旧清单不另立活跃状态表。

来源：[作物收获部分手测](../../archive/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md)。

Crops/Harvesting 的早期有效边界是作物容器而非野树/草自动收获：普通 PlantBasin.Harvest(bool,bool) 执行前应重验 basin 级 CouldHarvest/IsCropMature；Crop.isMature 只作诊断。每个房间先物化 equipment 集合，因为原生收获会改变集合；无可执行成熟目标是成功 no-op。TargetId 是同会话即时使用的临时句柄，不能持久化。TreeBasin/可可与草类当时只扫描，独立 native owner 未审前不得混进普通 Harvest。来源：[首次实现](../../archive/updates/2026/20260612-0002-crops-harvesting-api.md)、[语义加固](../../archive/updates/2026/20260612-0003-crops-harvesting-api-hardening-1.md)。

开发专用 CropHarvestingQaMod 的 F8/F9/F10 是扫描/单次/批量判别夹具，加入 developer build 不表示加入发布产品。其启动通过只证明 QA 可加载；当次玩家只确认普通单次/多次收获和背包产出，没有覆盖清单上其余家族、满背包、标题重载。当前扩展与未完事实继续向 API/任务 owner 追踪。来源：[真实农田手测交接](../../archive/updates/2026/20260612-0004-crops-harvesting-manual-qa-handoff.md)。

官方 JSON 作者工具的一个历史过严验证曾拒绝空 growth_months，官方语义实际为空即不限季节；只验证已提供的值为 1–4 整数。修复保留 JSON 空数组，用浏览器导出/非法值检查即可，不触发游戏或 Runtime。该 7 月 15 日 r2 handoff 后续又因单层包装压缩包、近似表名和 nested-shop 往返问题被替代，旧浏览器 PASS 与 ZIP 哈希不能当现行交付候选。授权 fork/第三方原件及再分发许可仍归 references owner。来源：[crop month 修复与被替代的 handoff](../../archive/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md)。

StrongPlantingGun 的 2026-07-24 验收确实触发原生保存和冷重载，因为目标是已反序列化枪在 SaveLoaded 修复容量；这是保存相关具名门，与普通 UI/Hook 游玩不同。它还通过实际 installer/callback/Dispose 的五个 Harmony target，以及 native mutation 之后注入异常，证明原物品/数量/slot-lock 恢复和后续接收者仍同步。相同源码 token 或仅 helper 测试不能替代这一执行链。来源：[Strong 原生保存与物理 fixture](../../archive/updates/2026/20260724-0001-strong-planting-gun-ninth-advanced-product.md)。
