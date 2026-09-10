# 地图、入口与转场研究边界

当前 API 等级由 [public matrix](../../api/public-api-matrix.md) 拥有，身份/保存由 [PROJECT](../../../PROJECT.md) 拥有。本文提炼 [July 25 native review](../../archive/reviews/api/2026/20260725-0001-new-scene-travel-and-map-content-native-owner-review.md)，比较的确切基线为 `23762374_public_C416D4` 与 `24256979_test_7A1907`；不是当前 Mod 地图加载能力声明。原件校验见 [API 阅读清单](../../archive/migrations/20260908-workspace.json)。

该次新增的湿地农场、湿地木屋、旧城市废墟、旧城农场、旧城废屋是五个独立 Unity built scenes（85–89）；旧城市废墟一个scene包含85个Dungeon Rooms。scene、Room和map_id不是同一种身份，不能用表行或坐标变更多算一个可加载地图。

内容注册、入口/解锁、转场请求与settled/失败四项分开。Gate/station/Yarn go最终调用同一mark-point/room transport；NPC对话是入口调用者，不是独立传送owner。门/portal JSON不能自行在既有scene生成碰撞或GameObject。官方新场景依赖Build Settings、preload RoomSO/DungeonSO、Luban表和真实scene组件同时注册；AssetBundle或mark-point表本身不足以证明外部场景可接入。

IndieFarmManager在该build呈现table-driven农场更新/天气/电力/保存能力，同时会移除不在表中的农场状态。官方固定内容下成立的删除语义不能直接用于可禁用Mod，需要独立owner/orphan保留设计；不能用停用立即保存规避native保存语义。

DoTransport bool是请求接受；callback虽在Room进入后，仍不保证NPC/fade/state-pop/旧scene卸载全部完成。异步失败可能只写日志，Yarn等待可能不结束，票钱可能已扣。readonly discovery、native phase观察、外部scene loader和入口注入应分别按需求设计，不将现有ITeleportDebugApi直接改名为通用地图API。

该review的独立农场→自有室内→官方scene注入次序属于未来方案，没有授权G7/Content Host或新产品。真正需要地图能力时从native基线与精确生命周期缺口继续；不把整套未来验收加入普通mod维护。
