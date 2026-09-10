# 原生工具类产品的历史边界

历史研究导航；现行身份与能力只查 [Catalog](../../../tools/release/dtmapi-product-catalog.json)、受影响产品源码、API/Hook owner。这里不复写版本和产品名单。

Mine 的旧 2x 缩放污染说明，对象池复用必须恢复非目标 renderer；已放置机器的遥测与截图不能证明玩家手持放置预览。重复研究解锁和科研费用属于官方数据链，要验证真实 TechTree UI。Y 的 save-then-immediate-load 曾留下场景残留，旧安全修正仅提供 save-only，不能换名称重新开放同一重载路径。

Oil 的原独立额外掉落与官方煤矿 weighted LUT 是不同语义；静态 JSON 能表达原生抽取候选，不代表保留旧独立额外赠送概率。7 月第一次“重平衡 amber”方案随后被明确的 append-only weight 25 决策取代。Mine 的静态 item/equipment/recipe 与周期、输出、能量、存档和真实预览应区分责任；当前物理归属沿 PROJECT，旧“两层 GameBridge”设想不是永久归属规则。

来源：[Mine/Y 早期问题](../../debug/issues/ISSUE-007-20260605-mine-yconsole-026.md)、[Oil/Mine 与 AutoHarvest 决定](../../archive/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md)。

OneActionComplete 的资源完成必须建立在一次有效原生击打与工具准入后；燃料/饲料补满须在原生首个物品已消耗并加入后继续自己的计量。它与 ActionSpeed 同在 AgentStateInteract.OnExit 打补丁，前者处理物品账、后者恢复动画，不因目标方法相同就需要公共 dispatcher 或共享 gameplay owner。新产品与旧兼容 executor 必须避免双重结算。

AutoFishing 与 OneAction 的首次横向比较只提炼了 Catalog 驱动的 SDK/包构建：真实行为、QA 形态和状态机不相同。两处都调用配置、日志或 Hook，不构成再创建通用 wrapper/session/QA ABI 的理由。

来源：[OneAction native owner](../../archive/reviews/code/2026/20260721-0005-oneactioncomplete-second-product-admission-review.md)、[两个产品的最小共享边界](../../archive/reviews/code/2026/20260721-0006-autofishing-oneaction-platform-comparison.md)。

OneAction 的野生植被/蒲公英属于原生 VegetationRenderer.OnFell → CheckToolConstraints，既不走 DungeonResourceRenderer，也不应获得额外完成伤害；正确工具移除、错误工具不移除且两者 OneAction application delta 都为零，才是该例的成功边界。工具种类/等级由原生 proto 读取，不硬编码蒲公英必为镰刀。来源：[植被例外专项](../../archive/updates/2026/20260531-0013-oneaction-vegetation-exception-smoke.md)。

Oil 2026-07-13 实施进一步纠正了原生概率算法：最终表按 coal 990（无限）、amber 10（最多一件）、Oil 25（无限）排序，已达上限 amber 命中会继续向后扫描，可能落到 Oil；不能使用独立 Bernoulli 近似当作精确回归。3/4 抽至少一件 Oil 的当时精确结果约 7.168% / 9.459%，均值约 8.313%。确定性算法测试与游戏自然 world drop 分别证明概率逻辑和 native owner，不等于最终经济平衡已验收。positive 中四个 OneAction 场景也证明去掉 Oil 回调后的共存；后续仅 runner profile 判据硬化直接重放已保存结果，没有再进游戏。来源：[官方 JSON 与解耦实施](../../archive/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md)。

6 月 Oil 实验先因不存在的 `ItemFunctionNone` 无法载入，改原生空 `ItemFunction` 后才形成 item；覆盖复制又遗留废弃根 JSON/资源，源目录正确不等于现场输入正确。应清确切产品陈旧文件、保留他人内容。采矿 postfix 时 renderer/collider 可能已被 native 清理，需 prefix 捕获本次目标；固定随机数 smoke 只证明实际采矿到背包，不证明自然掉率。`UiSpriteAsset.ToString()` 的类型名也不能当图标可见证据。旧燃料最高值对比和独立赠送概率只是当时研究，不覆盖现行官方 LUT。来源：6 月 3 日 Oil Updates，原件见[迁移清单](../../archive/migrations/20260908-workspace.json)。
