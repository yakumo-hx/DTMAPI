# 矿机：会话调度与原生资源事务

现行内容、程序与发布准备度查 Mine 产品、[Catalog](../../../tools/release/dtmapi-product-catalog.json) 和 [PROJECT](../../../PROJECT.md)。本页保留 2026 年 7 月迁移与修正的知识，临时井图、2x 缩放和当时 RebuildBlocked 不能自动解读为今天仍未完成。

7 月 26 日用户明确拒绝“必须跨退出保存生产周期”的推断。Mine 首版采用 session-derived scheduler：SaveLoaded 从当前 TotalTUs 加周期，title/停用清空；只有原生 Case 库存和电量随官方保存。此决定消除了不必要的侧车、generation 和 journal。静态 item/equipment/recipe/group 归官方 JSON，原生 Case/LinearInventory 与电器 Launch 负责物品和固定 10 电量，产品只持有周期、权重、session due 及临时视觉/配方/科技事务。

测试前提应观察真实 scheduler entry，再推进原生时间；设备需按原生 ChargeToFull 建立电量。官方内容表已可证明设备存在时，受无关第三方内容影响的可选 Registry 不是第二个必需 owner。对实际 Threshold getter 的反射不可用也不能误报为电力规则不符；用已审查的原生无参 Launch 执行其自身阈值。早期仅一台充电矿机出煤的成功证据后来被限定，双机、低电、满仓、移动/拆除/索引复用和热开关另有最终专项证明。

调度身份应跟随 live object，不依赖可复用 room/index；完整权威枚举结束后才剪除未见对象。资源失败在相同 TU 也可能恢复，TU 不是重试键，已有半秒 poll 足以限频。满仓/拒绝输出在 Launch 前检查，低电不先复制库存或制造抛弃物品；仅实际写窗口需要 snapshot 作为最后回滚保护。阻塞指纹、缓存与专用 Mine 模型可减重，不能把旧通用 Machine engine 搬回 GameBridge。

原值恢复成功的项可移出 ledger，失败项必须保留精确原值与顺序。后续配置不能把 cleanup-failed 掩成 waiting-for-save，重新激活必须先解决未完成恢复。历史 Unit 一开始只手工调用 DeactivateSession，不足以证明完整 activate → restore failure → blocked reactivation → retry 链；补齐真实链后无需重跑未改变的成功游戏矩阵。源码通过也不代表 SDK 生成的玩家 DLL 已更新，包需要针对当前源码重建和检查。

来源：[准入与逐次失败归因](../../archive/reviews/code/2026/20260726-0002-eleventh-product-mine-admission-review.md)、[scheduler/资源前置及减重](../../archive/reviews/code/2026/20260726-0003-c1-manager-fixes-and-mine-split-audit.md)、[恢复账丢失](../../archive/reviews/code/2026/20260727-0001-mine-fixes-and-debugconsole-split-audit.md)、[失败状态与测试链](../../archive/reviews/code/2026/20260727-0002-mine-and-debugconsole-fix-recheck.md)、[真实链补齐与旧包](../../archive/reviews/code/2026/20260727-0003-mine-debugconsole-transaction-fix-audit.md)。

6 月用户区分睡觉、容器内跳时间、同农场跳时间的差异。旧 Y 按钮确走 native PassTimeNoControl，产品当时发现时才设 due、仅当前房间扫描、失败也推进 due，不能用改 Y 原生时间路径遮掩 Mine 调度问题。后来 session-derived 首次发现语义是明确产品选择，并不承诺跨退出精确周期；旧“必须全世界/跨存档补偿”条件不再恢复。

来源：[早期跳时间差异](../../archive/reviews/manual-qa/2026/20260605-0002-mine-time-skip-production-review.md)。

早期 `LastElectricPowerCost=10` 只是产品字段，官方电力面板仍 0.0；6 月 6 日同时补 EComProtoAppliance 元数据与真实 Launch，低电拒绝保留 due，才建立可见 native 消耗。只回滚元数据或执行其中之一会退回空壳。catch-up 测试只强制 observation/poll，不强制 due 伪造生产。官方 EquipmentFuncCase 的 16 格库存、科技树及 recipe 是另一些消费者，临时 spawn 出煤不能证明玩家可研究/放置或手持预览。来源：6 月 3–6 日原反馈与 [ISSUE-007](../../debug/issues/ISSUE-007-20260605-mine-yconsole-026.md)。
