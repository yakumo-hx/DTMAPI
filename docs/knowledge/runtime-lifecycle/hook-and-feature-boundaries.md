# Hook、feature 与原生操作边界

当前 Hook 位置和能力由 [Hook map](../../hook-map/README.md)、对应 focused 页及 [公共 API matrix](../../api/public-api-matrix.md)负责。本页记录结构演进和测试能证明什么，不维持另一份符号清单。

2026-06-08/10 的 SaveSlots 两步变化不同：先把 ExperimentalBridge 的同一实现机械移入 partial 文件，后来才把策略状态和刷新责任交给 SaveSlotsFeature/Service。搬文件不等于解除原服务依赖；验收中的 archiveFileCount=12、panelSlotCount=12、renderedSlots=12 也只证明 UI 路径，不能独自证明建档、删档、复制和重启。后续存档行为见 persistence 领域知识及现行 Issue。

20260609-0010 把既有 SetEnvCamera Hook 的安装状态迁入 CameraFeature，保留回调、目标和参数形状；没有同时改变 CameraView lease、缩放或背景/雾责任。4x/2x 动态 smoke 与 reset 证明那个样本的应用/仲裁行为，历史字段 nativeRefresh=not-called-playable 和 uiScale=unchanged 限定了结论。

共享 AgentState.OnExit 从 ActionSpeed 移出，使 ActionCompletion 不再把别的功能启用状态当共享生命周期就绪状态。GameBridgeNativeHelpers 随后把通用反射访问独立，旧 partial 保留薄兼容包装；消费者直接依赖共同能力，不应依赖不相关业务服务的静态入口。

20260610-0001 发现低层 native/Harmony 回调中的直接调用链仍可被首个异常打断，即使 feature host 已隔离也无济于事。修正是逐步骤隔离，确保后面的 restore、cleanup 和事件仍运行；最外层一个 try/catch 无法做到。Fishing Pull exit 的动画恢复必须在通知失败后仍执行。五条正常游戏路线没有出现失败日志，只能说明正常路径，异常隔离语义应由可控的抛错测试证明。

20260610-0008 为一般回调定义独立 fallback：结果返回原值，prefix 放行原生，void postfix 不把异常泄漏给 Harmony。反射调用 helper 的单元测试覆盖这些值。0016 对同一 operation 的错误采用首条完整记录、后两条短警告、后续窗口汇总；限频不能改变 fallback 或自动卸 Hook，也不能把日志不再刷屏写成底层错误消失。

20260609-0002 的 Suppress 清理只解决一帧 helper 状态：pressed/suppressed 清掉，而 held 直到释放才清；没有新原生 Hook，也不阻挡游戏工具、物品和菜单输入。此类 Core 状态修复当时就没有进游戏要求。原生输入隔离若变化，应验证它自己的消费者和 UI 冲突，而不是复用 Suppress 的 PASS。

20260610-0018 曾把 SaveSlots 每帧重复写改为 pending 750ms、已配置 3s 心跳，并在 SaveLoaded 强制刷新。它证明减少重复工作的方向，但这些旧间隔不是当前调度要求。0025 再次明确文件、加载/删除/复制和 UI 布局仍由原生拥有；旧的 24 槽扩展想法也不能覆盖后来产品的固定能力边界。

OilCoalDrop 的待处理命中缓存属于存档/标题/环境瞬态；0031 用预置缓存单元测试验证三个边界清空，只在确实移除条目时记日志。0032 将 ToolCollider 的安装权收归共同桥，0038 才进一步解决内部回调的异常隔离：ActionCompletion 返回未处理或抛错回退 false 后，Oil apply 仍运行；已处理则只清已捕获状态。独立 operation key 和实际抛错单元测试比两条正常 smoke 更直接证明这一顺序。

Fishing 的 Ready/Cast/Wait/MiniGame/Pull/Cooldown 观察与干预是不同责任；强制鱼 smoke、动画加速恢复与原生正常出鱼也不能混为一谈。0044 解除业务服务对 ExperimentalBridge 的静态依赖，0048 去掉 save/title 低层重复 animator restore，由 feature 的 Reset 统一负责，而 AgentState/Pull exit 的即时恢复保留。0053 又修正文档的旧路径；应保留原判断发生时的上下文，以当前 owner 路由消除并列的“现在实现”文本。

20260613-0013 的标题/暂停菜单两列错位，曾被全局 GridLayoutGroup setter 规范化遮住。临时 setter stack 后来证明 SaveSlots Hook 命中了继承的 DolocGridUI.Select，把其他菜单也当存档面板，按数量计算出两列。真正修正是删去固定 12 槽不需要的 Select Hook、增加 GameDataPanel 目标检查，并移除全局补丁。目标方法继承关系与实际 instance 类型需要一起校验，不能以全局 UI 修复掩盖业务越界。

20260613-0019 的鱼卵显示回退暴露“provider 注册成功但真实数据为空”：公开生成 lookup 本来就是空占位，smoke 自带 fallback provider 又掩盖它。修复在 GameBridge 对实际 native fish identity 查询原生标题，同时移除测试专用假 provider。Hook healthy 不证明用户可见内容正确。

20260615-0003 的第二辆车失败包含原车外观被改、跨档残留和 Init/Reset 空引用。它先改私有素材名，克隆后立即登记以便任何后续步骤失败都可清理，再验证组件属于克隆；首轮仍遗漏耐久进度 UI，在 reset 阶段失败后补全。保留 ensure-stage 这种精确失败点。该产品随后归档，不能因旧 smoke 通过而恢复现行支持。

20260615-0014 纠正 LoadGame/SaveGame 返回 bool 而非 void 的精确签名，并使 asset/text 默认查询与 item 一样排除 disabled 来源；还限定 Suppress 只能挡之后同帧 helper 分发，不能撤回已在执行的 handler。20260616-0003 再拆 feature 与 hook partial 只是文件责任整理，不改变这一边界。

20260618-0005 是从 animal 分支导入并重新编号的历史阻塞，不能当主线已实现能力。静态 shop append 里有 Lightning Chicken 包，但两次原生商店缓存都没有它，即使主动 RefreshStore 仍失败；没有购买释放，就没有后续渲染/独立动画证明。不能用直接生成动物绕过要求，也不能把静态表通过当原生 store/ItemAnimalPackage 路线通过。

20260620-0003 把 pending cast 放到 native UseFishRod 调用前，避免同步阶段回调已确认成功却被返回后的 pending 覆盖；超时释放 pending 并短暂 backoff，不永久卡住。确认计数来自真实阶段前进，不来自调用没有抛错。UI 清理也增加 loaded archive、active host 检查，避免全局 FindObjectsOfTypeAll 在标题重新拾取旧对象。

20260623 的 EventSystem 修正先复用 native，再等 Input System ready 才创建 fallback，无法创建时 pending 重试而不是永久 quarantine；DTMAPI 自建对象随所属 UI 结束或 native owner 出现释放。不能让高频环境 reset 摧毁仍活动对象，也不能把仅首帧 inactive 当作全部旧 UI 可删除。
