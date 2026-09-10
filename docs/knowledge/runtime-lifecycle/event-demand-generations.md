# 事件、需求和内容代际的历史验证边界

当前 Mod 身份、ProductNative/SharedNative 和存档语义由 [PROJECT](../../../PROJECT.md)拥有，owner 撤销由[现行契约](../../design/mod-owner-lifetime-contract.md)拥有。本页合并 Batch 5 的失败轴与证据适用范围；早期“所有原生实现都在 GameBridge”仅描述当时 Strict 路径，不能约束后来 Advanced 与可选 Content Host。

需求协调器为零不代表整条调用链静默。旧 Core Update 帧首、帧尾都在空队列重建 event/Hook 快照、字典与格式化摘要，旧 10,000 帧测试却只调用 GameBridge 调度器。修正后无变化先标量判断；保留的 CustomAnimals/Audio/Camera/shared Hook 必须在装箱、反射、服务调用和诊断之前退出。Equipment 孤儿恢复到终态撤销需求，证据专用产品 tick 移出玩家路径。物理补丁仍在、逻辑休眠和需要重启三个维度分开，不能以一个 active 布尔值代替。

Event 发布准备冻结不可变成员；普通退订只影响下一次发布，权威 owner cleanup/quarantine 仍可阻止尚未调用的失效 owner。订阅与退订使用同一 Runtime 线程与 owner guard，不能只保护 +=。确定性 barrier 测试覆盖发布准备之后、dispatch 之前的并发窗口；只在 handler 内增删的测试漏掉这个窗口。CoreLifecycle 状态也需要真实基础 Hook closure probe，清理数要计入 demand roots，不能只看协调器字典已空。

内容代际的共同事务是：完整准备 candidate，权威 live swap 与 terminal receipt 一起提交，随后观察者各自隔离。旧 CustomAnimals/Audio 先换 live 再写日志，后置异常把已公开内容 requeue；后来又发现 ContentQuery 的 prepare 已经发布、CustomAnimals rejection/status 先于 terminal、后置异常跳过 owner demand reconcile。解决日志错误不能回滚真实已提交状态，也不能吞掉下一 owner 的必需协调。故障注入应落在提交前、提交后、stale、rejection 和 successor generation，而不是只验证 happy path。

2026-07-18 人工反馈第一份保留 3 项顺序：1 用户以实体 Escape 暂停；2 已有通过证据与两个正式失败的普通无 QA AnimalViewer 结果不同；3 后续继续应先检查中断恢复。它是历史授权边界，不对当前审查发出继续或暂停指令。随后用户 9 项为：1 第三档出生点移到大畜棚右缘、短按 A/E 可交互；2 Zoom 位置落到可见地面以下；3 空队列诊断；4 零需求仍有回调；5 内容代际原子性；6 退订与冻结成员；7 物理 Hook/清理统计；8 两条正式 AnimalViewer 失败；9 GC 只有计划、L4/L5 不独立、Catalog 过度声明。后续通过附在原编号下，不改写旧失败。

普通 no-QA lane 没有内部自动载档，缺人工 UI 握手就会在 SaveLoaded 前超时。034102 是 Local11 通过，最终 214048 是绑定候选的两条因果动物渲染、原生关闭、无 QA 与正常退出通过；010116 是 Published11 漂移前提失败，Local11 不能替代其来源验收。截图晚于截止的观察不能自行变成正式总 PASS，基础设施中断也不能算产品失败。fixture 协议应把待人工载档单独显式暴露，而不是让人等待未知 gate。

真实 no-demand 300 帧预热/10,000 帧测量支持的是可选调用与节拍静默，mandatory Core UI/content drain/native UI 仍运行；Unity 分配计数不可用，零当前线程分配来自离线组合 Core+Bridge 测试，不是整个游戏零分配。EventZeroListenerBypasses 正增长是正常快路遥测，不能套零 delta。7 月 24 日通用 QA 测量移除了已退休的 AutoFishing 参与者，并使用成功烟测的离线严格收据完成；因此没有重跑完整 Release 或长梯度。

ActionSpeed 的 24 个 600 秒子阶段有各自终态，parent 因后续 AutoFishing continuation 失败不能抹掉这些子结果。AutoFishing 体力维护 1000 ms 在 4x 不足，QA 在 native cast 能量不足之前停止，属于 fixture 频率问题；250 ms 修复后当时从头完成六阶段。分配能力仍 blocked，只有有界结构/生命周期/趋势结论，没有量化内存或每动作预算，也不关闭 ISSUE-010/011。旧全套重跑要求属于那次正式候选可比性，不能泛化为今天任一测试前提修正都必须重跑所有有效阶段。

Phase 0 后审查保留五项机器门教训：1 schema/model 没有 Advanced token 不等于 hostile manifest 被拒绝；2 planned AnimalPack 字符串与 count 一致不等于真实消费者；3 多文件复制同一个未来采集时间不等于真实 provenance；4 目标 delta=0 常量不等于 baseline→审计 HEAD 实测零增量；5 版本与 AutoFishing-first 路线的重复权威仍会冲突。后续已由原 correction Update 处理，这里不重开旧门。对今日工程，检查应连到行为、源工件或差异，避免写只镜像声明的测试。

同一历史还出现 G2 要真实产品证明、G3 却等全部 G0–G7 通过的循环。正确拆分是规范先行→最小纵向 fixture→唯一明确 pilot→完整产品证明→其他迁移。该结构可复用，旧 AutoFishing-first admission 只属于当时迁移计划。不要让最终完成条件反过来阻止建立它所需的最小实验。

8 月 8 日玩家 slot0 记录给出了重要反例：LoadGame=true、SaveLoaded 和 HUD 已出现仍不能证明 farm block/room 初始化成功。最初 scene callback race 高置信解释后来被新日志和对象证据降级：ChickenNest 与 AutomateBotStation 重叠，冷读档拥挤移除发生在 bot 初始化之前，必然空 decisionMaker；原生按块捕获异常仍返回加载成功。DebugConsole随后误放行保存，把空 currentRoom 写成坏摘要；恢复 prev0先解决列表却不解决潜伏重叠。原始硬闪退仍未归因。完整修档知识和失败保留在[玩家恢复](../persistence/player-recovery.md)，本页不增加修档权限。

8 月 27 日官方 Hope 返种审查保留用户排除隐藏无人机库存、边缘盆的条件。同步睡眠整段快进内安排的200 ms continuation都等实际PlayerLoop，醒来SaveGame却先提交；等一秒只有随后再保存才改磁盘。9 月 7 日 public1.00.07新字节已同步CreateDropItem再表现，静态根因关闭，原native-save冷读档总量A/B仍未实施。该待验收不自动成为工作空间建设或任何Mod小修的必跑矩阵；它属于[官方Hope审查](../../reviews/manual-qa/2026/20260827-0001-official-hope-seed-wake-save-race.md)的显式后续。可靠模式是权威状态同步提交、视觉效果可延迟，不用全局保存等待掩盖一个基因的问题。
