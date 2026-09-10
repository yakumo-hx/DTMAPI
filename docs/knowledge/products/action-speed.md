# 动作加速：原生域与性能证据

历史经验；当前行为查 [ActionSpeed](../../../products/first-party/ActionSpeed/README.md)，长期 GC 状态归 [ISSUE-010](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)。

2026-07 用户明确将 AutoFishing 与 ActionSpeed 设为两个并行研究域。Fishing 的 Ready/Cast/Pull 与 Tool/Interact/Eat/Continuous-use 不等于同一个 Animator 冲突；只有观测到真实共用状态或未来范围变化，才打开 owner 仲裁问题。

ActionSpeed 的单一工具循环不能证明全部动作族。性能需要完成动作数、实际倍率、每分钟/每动作双分母，及禁用/标题边界后的 Animator 快照、pending marker、回调与日志键范围。结构有界不能独自证明 Unity 原生或 Mono 长期稳定；进程内存无需恰好回到启动字节数。

来源：[速度阶梯与发布结论边界](../../archive/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md)。

7 月第三产品迁移的关键边界是九个原子 Hook 与旧兼容 executor 的互斥。只检查 Harmony owner 会漏掉 AutoFillBottle-only 的旧 updater，因此互斥也要覆盖实际 updater/policy 启用边界。配置禁用允许 Hook 保持安装但不执行，Manager owner 停用则要求实例、回调和精确 owner 补丁都释放；内部计数不能代替真实 Harmony 目标清单。

当 Core 已进入 Deactivating，正常 Dispose 再调用 owner-bound 事件代理的 `-=` 曾触发生命周期异常；当时修正由 Core 清除平台注册，产品清本地标记并独立尝试状态恢复、回调脱离与 unpatch。Entry 回滚是另一阶段，仍可显式取消订阅。此经验应回到当前生命周期 owner 验证，不能按旧方法名照搬。

错误的 Strict 源选择、G6 提前中断 G5、全局 `AgentStateBase` 被写成带命名空间类型，分别是部署、runner 与产品目标错误。修正后重跑同一最小短验收已在原授权内；“一次短测试”不等于只能启动一个进程。旧权重审查也区分了 ProductNative 归属迁移与保留 Compatibility 导致的总量增加。只供 QA 使用的 LastSummary 字符串与 List 分配应显式开启后才存在。

来源：[第三产品准入](../../archive/reviews/code/2026/20260722-0002-actionspeed-third-product-admission-review.md)、[编排失败与恢复](../../archive/reviews/code/2026/20260722-0003-actionspeed-runtime-acceptance-orchestration-review.md)、[权重与遗留 executor](../../archive/reviews/code/2026/20260722-0004-actionspeed-weight-and-next-product-audit.md)、[停用根因](../../archive/reviews/code/2026/20260722-0005-actionspeed-owner-deactivation-platform-root-review.md)、[常驻 QA 分配](../../archive/reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)。

8 月 31 日审计基线已为 11 Hook（含独立手动浇水状态），不能沿用早期九目标当前值。主要成本是 Continues 每帧分类在 policy 关闭后才退出、AutoFill 标题仍订阅且失败 probe 不推进 cooldown、重复反射/params/日志和 restore 吞错清账。单产品可用保存后的 immutable snapshot 与功能位，反射前 no-demand return；每次 probe 推进单调 nextProbeAt；metadata 缓存不缓存 Unity 对象；失败恢复分类 live/dead/success，不能“字典零”冒充恢复。

历史 ActionSpeed ladder 的连续 Prefix 最多约 50 次/600 秒，与真实按住输入密度不同；Debug profile 加 QA summary 更不能证明普通玩家低分配。当前产品 Engine 的测试曾实际调用 frozen service，必须补直接 ProductNative 行为，保留 11 目标原子 owner，采用真实持续输入的短窗口而非重放旧完整 L0–L5。此审查仍是待接手整改输入；体积重合不授权让 frozen Host 依赖当前产品。

来源：[8 月 31 日 ActionSpeed 压力审查](../../reviews/code/2026/20260831-0004-actionspeed-technical-debt-runtime-pressure-audit.md)。

2026-06-16 的互动反馈强调触发时序：已施肥/已覆膜仍会先进入原生互动再报告失败，不能用结果过滤抹掉已经发生的动作动画；动物与房间连接器的 OnInteract 又可能早于通用 Interact 状态。待执行 owner 标记若在状态切换中的通用 ToolExit 被清除，会遗漏该类动作。具体选择物品格应绑定 native tip/cell anchor，不能只依赖全局 SelectedEquipment。树脂属于一次性原生互动，空内容应明确 no-op；原先“已通过”的笼统描述后来按实际日志和手测补证。来源：[原生互动后续审查](../../archive/reviews/manual-qa/2026/20260616-0002-actionspeed-native-interaction-followup.md)。

手动水壶动画不经过 AgentStateTool，而有独立 AgentStateWater.OnEnter/OnExit。2026-08-06 修正把两处 Hook 纳入既有工具倍率和精确 Animator 恢复，数量从 9 到 11；不新增设置、公共 API 或 Compatibility 功能。验证需要水壶中有水并真正进入 Water 状态，不能用斧/镐/镰刀通过替代。来源：[手动浇水 Review](../../archive/reviews/manual-qa/2026/20260806-0002-actionspeed-manual-watering-animation.md)。

早期 feature-host 拆分曾把 SaveLoaded/ReturnedToTitle 方法留下空实现，虽然分发本身 ready，原有 Animator 恢复仍只在状态退出发生；随后补上两个生命周期调用。这说明搬出服务后须核对真实生命周期接线，不能以 feature ready 或普通 OnExit 恢复日志代替存档/标题边界。来源：[FeatureHost 拆分](../../archive/updates/2026/20260609-0013-actionspeed-feature-host-split.md)、[补齐生命周期恢复](../../archive/updates/2026/20260609-0018-actionspeed-lifecycle-restore.md)。
