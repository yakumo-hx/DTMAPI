# API 研究材料的使用边界

当前 API 状态由 [public matrix](../../api/public-api-matrix.md) 拥有，当前物理职责与保存规则由 [PROJECT](../../../PROJECT.md) 拥有；本文只合并历史阅读中反复出现的判断方法。阅读范围见 [API 清单](../../archive/migrations/20260908-workspace.json)。

## 分清材料实际证明什么

- `Found` 至少区分 native runtime owner、display-only、content-only、partial；public API 稳定性与“找到方法”分别判定。
- Mod 能运行、UI 有显示、HookStatus 就绪或一次 smoke 成功，都不能单独证明 native mutation、保存、owner cleanup 或多 Mod 共存。
- 第三方样例能证明需求和冲突面，不能证明实现许可或原生责任边界。旧自有实现也只提供语义线索，不恢复前身代码复制权限。
- 置信度数字属于当时评审的 semantic-to-owner 判断，不是统计概率、当前 API 等级或玩家验收。

## 已读研究轮次与其有效结论

[native 三轮研究](../../archive/reviews/api/2026/native-owner-domains/review-rounds/INDEX.md) 对初始 `23465763_workshop_38581E` 资料逐步补足 owner：第一轮找过度归并，第二轮补充精确方法，第三轮调低未被证明的能力。可复用的修正是：consume 与 DoEffects 分离；native Buff 重复添加可能只刷新 timer；直接 passive2 装备可能绕过 unlock；drone config/item 不等于多 active drone；load-time 数据库入口不等于 runtime 创建 API。这些是历史定位线索，未在本次阅读中重新做 native 验证。

这些既成轮次保留为历史，不要求每个新任务再走三轮或四轮；按当前任务的不确定性调用需要的报告与源码。

[native library index](../../reviews/api/native-owner-domains/INDEX.md) 与 [source index](../../reviews/api/native-owner-domains/SOURCE-INDEX.md) 说明初始 report build、后续 reverse capture 与 Native Function Map dataset 可以不同。索引标注的 `24456188` 是该索引维护时的指针；当前基线从 reference owner 解析，不能把未重建的聚合图当作新 build 数据。

[local-mod library](../../archive/reviews/api/2026/local-mods-native-owner/INDEX.md)、[source index](../../archive/reviews/api/2026/local-mods-native-owner/SOURCE-INDEX.md) 与 [confidence changes](../../archive/reviews/api/2026/local-mods-native-owner/confidence-changes.md) 描述 June 的 20 个 testmods 及样例集合。SecondMotor、Oil Hook、产品路径和 API 状态随后已有改变，这份表不能作为当前 Catalog/实现列表。其 display-only/content-only/diagnostic/sidecar/native 的分辨方法仍可用于新需求分类。

历史 local-mod [报告模板](../../archive/reviews/api/2026/local-mods-native-owner/REPORT-TEMPLATE.md) 的输入来源、语义、owner 和缺口字段可复用；固定四轮评分与旧 Goal 交接不成为新的前提。

[需求簇](../../archive/reviews/api/2026/local-mods-native-owner/api-demand-clusters.md)、[共享 owner 冲突](../../archive/reviews/api/2026/local-mods-native-owner/shared-native-owner-conflicts.md) 及其四轮记录基于初始 `23465763_workshop_38581E` 调研。它们最有用的结论是切开看似相同的需求：作物不是树/草/野生采集；鱼卵标题不是鱼类内容查询；动物查看不是照料或 AI；容器候选集必须同时用于预览与实际消耗；Oil 的官方内容数据不是旧煤炭 drop Hook；Mine 的内容/配方/供电不是 session scheduler。共享方法只提示潜在干涉，需要实际找到共同写入或恢复状态，才有共享 dispatcher/状态 owner 的理由。当前责任判定仍查 PROJECT。

[历史 implementation follow-ups](../../archive/reviews/api/2026/local-mods-native-owner/implementation-follow-ups.md) 中旧 manifest GameBridge 依赖、最低版本与固定多轮流程已不能直接执行。树/草覆盖、满背包、stale ID、重载和跨农场房间遍历等缺口可作后续检索线索，是否仍未完成由当前功能 owner 和最新证据决定。旧报告不为新任务自动增加全部测试门。

## SMAPI 生态需求研究的证据等级

[SMAPI 生态索引](../../archive/reviews/api/2026/smapi-ecosystem-map/INDEX.md)、[来源表](../../archive/reviews/api/2026/smapi-ecosystem-map/SOURCE-INDEX.md) 和 [语义映射](../../archive/reviews/api/2026/smapi-ecosystem-map/semantic-api-map.md) 的起点是用户粘贴的十个 Mod 摘要，当时没有逐一浏览上游实现。它们支持需求分类，不能据此声称实现对等、SMAPI/Content Patcher 兼容或能力已经交付。

可复用的分层是框架公共服务、UI host、只读 native 查询、事务、自动化、runtime 创建。查询目标与展示 host 分开；内容发现与资产编辑/token/condition 分开；容器集合查询、远程 UI、扣除事务和自动化调度分开；实体 registry 与原生可保存实例创建分开。多人能力只是未来预留，不是当前承诺。映射中的 SecondMotor experimental、旧 root helper 状态及全部脆弱内容归 GameBridge 的说法已被后续边界取代。

[gap map](../../archive/reviews/api/2026/smapi-ecosystem-map/dtmapi-gap-map.md)、[roadmap](../../archive/reviews/api/2026/smapi-ecosystem-map/priority-roadmap.md) 和四轮记录区分研究优先级与 API readiness：P0 只表明先研究；`UpdateTicked` 是 DTMAPI pump，不能声称是游戏模拟 tick；看到内容来源不能证明游戏已加载内容；枚举容器不能证明跨房间的稳定容器系统；原生脏信号未明时不能用轮询伪装稳定对象事件。

June 的 [领域库 Update](../../archive/updates/2026/20260613-0007-native-owner-domain-library.md)、[三轮 Update](../../archive/updates/2026/20260613-0009-native-owner-domain-review-rounds.md)、[local-mod Update](../../archive/updates/2026/20260613-0012-local-mod-native-owner-four-rounds.md) 和 [生态 Update](../../archive/updates/2026/20260613-0015-smapi-ecosystem-semantic-api-map.md) 说明三/四轮是用户当时明确要求的研究过程，并且这些文档变更只做文档验证，没有为了记录本身启动游戏或构建。旧 Follow-up 的每目标完整矩阵不成为今天的测试前提；是否需要运行、存档或长时验证由当前 workflow 及改变的风险决定。

## 框架、状态表与记录的边界

[0010 closure index](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md) 已明确复用 0008/0009 结论，仅审剩余域；这证明“补足未覆盖范围”即可，不需每轮全部重读。其索引、分卷和最终风险表重复投影 June API 状态，迁移后保留历史证据，当前状态只从 public matrix 读取。

[Config/registry review](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/02-config-localization-logging-registry.md) 说明纯 DTMAPI config、日志和 registry 合法性不依赖游戏 native owner。旧 UI preview 会临时应用 pending setter 后恢复，因此 setter/canEdit/isVisible 的 IO、注册或其他副作用可能越过取消边界。该风险应交给当前 ConfigMenu contract 核对，不能按旧 title-only UI 文字推断如今实现。

[UI/report review](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/03-ui-diagnostics-report.md) 分清了记录与证据：ExportLogs 只是复制存在的日志并打包；RecordEvidence 只是文本/日志附件；HookStatus snapshot 不是活对象。它们均不替代实际行为、截图完成或原生路径被执行的证明。生成更多 zip 不会补足缺失的行为观察。

[Framework review](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/01-framework-gameloop-event-input.md)、[migrated gameplay](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/04-migrated-gameplay-apis.md)、[debug](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/05-debug-yconsole-remaining-apis.md) 与 [Chest/StrongGun](../../archive/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md) 的旧职责表不能替代现在 ProductNative 边界。仍有用的是：policy 注册可能早于 Hook readiness；“通用完成动作”名称可能仅实现资源、燃料和喂养；display provider 不拥有数据；creative debug 旁路不能当成普通经济事务；消耗物品再 DoInteract 的路径必须说明异常和回滚归属；未验 water 不能由 seed/film/fertilizer 证据带过。

[Workshop index review](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/07-workshop-content-index-apis.md) 当时没有阅读 ModManager 正文，记录的 local/BepInEx 路由已过时；现行身份查 PROJECT。它能支持 source metadata、enablement、native table loaded 三事实分开。旧 [Vehicle review](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/06-vehicle-motor-api.md) 和 [risk ranking](../../archive/reviews/api/2026/20260607-0009-native-owner-special-audits/08-risk-ranking-and-rebuild-direction.md) 也不再提供待实施优先级：SecondMotor 已有后续退役决定，不能因历史 P0 把它重新列为必修任务。

[July 5 功能边界](../../archive/reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md)、[全 Mod 边界](../../archive/reviews/api/2026/20260705-0002-all-dtmapi-mod-boundary-review.md) 和 [SMAPI 分层研究](../../archive/reviews/api/2026/20260705-0004-smapi-style-boundary-split-research.md) 追清了当时 DTMAPI 承载大量产品逻辑的结构问题，但“任何 native/Harmony 都必须进 GameBridge”“先抽通用 primitive 再迁产品”等步骤已被后续 ProductNative 决定替代。可保留的是 framework 公共服务、产品策略、content owner schema、native authoritative simulation、QA 和 author tools 的角色区分；不能把旧推荐次序变成新任务先做平台设计的门槛。

## 兼容证明按实际发布字节与具体消费者收口

[July Workshop ABI review](../../archive/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md) 最有价值的负例是保留 DLL 与其 informational commit 的源码不一致：精确 0.5.2 Abstractions SHA `39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B` 包含源码没有的四个 Lamp 类型/92 signatures。源码扫描或 provider ID 相同不足以证明旧 DLL 可加载。该记录后续补记已恢复签名并关闭 exact retained AutoFishing Entry/Configure，July 28 又关闭三条 external ActualLoadLane；不能继续按开头 release blocker 重跑已关闭切片，也不能据此推断运动取消/GC等未测行为。

版本需要区分玩家产品、file/informational 与 assembly binding 身份；packaging 不应把当前构建版本强行改成每个 Mod 的 minimum。具体数值、已冻结身份与发布窗口只从当前 Catalog/版本 owner 取，不把旧 0.5.5 计划永久化。查不到外部消费者不等于所有私有历史 DLL 都不存在，也不应为不可获得私有二进制创建默认阻塞。

[CustomEntity promise review](../../archive/reviews/api/2026/20260713-0001-customentity-public-promise-review.md) 选定正式 JSON animal、冻结 C# umbrella；97 个投机 DTO 仅能存 dictionary 不是稳定平台能力。JSON 不能为了统一命名被强行映射回旧 registry。动物/怪物/攻击/无人机的未来需求应各自选 native owner，旧提案不承诺全部实现。

[public-consumer audit](../../archive/reviews/api/2026/20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md) 的边界仍有用：同实例两个 provider alias 不是两个真实消费者；显示 deprecation 文案不代表已发布 warning 周期；stability 与 disposition 必须由一个明确模型投影；helper 按 mutable/read-only/process service 分别决定 stale-owner/thread 约束，不为了对称性给只读服务套 facade。该时点 Phase 2/4/G7 与具体告警未完成情况，继续归 current matrix/contract/最新 Update；本迁移不重开整个 Release 或游戏门。

[fixed-12 review](../../archive/reviews/api/2026/20260713-0002-saveslots-fixed12-product-boundary-review.md) 将 MoreSaves 正式行为固定 12、命名/滚动留未来独立任务，SlotCount 广形状只作兼容。18 槽 UI 失败不成为当前 fixed-12 修复的前提；将来名字 sidecar 需绑定 durable archive identity，而非列表位置。当前保存语义由 PROJECT 拥有。

## 生成符号台账与人工判断的区别

2026-06-07 的 [0006 Update](../../archive/updates/2026/20260607-0006-native-responsibility-method-audit.md) 明说符号清单由 `src/DTMAPI.Abstractions/*.cs` 生成：82 行矩阵、1700 个符号、固定字段和重复索引。后续 [0007 人工审查索引](../../archive/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md) 将它定位为导航，并明确拒绝再生成 1013 行低风险属性模板作为人工审查的替代品。0006 的机器表保留原件即可；治理不应要求把每个 enum/DTO 字段再改写一遍，也不应把当时的 MatrixGap 自动变成当前待办。

本次单独登记 0006 的阅读依据：完整读取标题、Top Risks、覆盖规则及每种不重复的判断正文，检查符号/路径/固定字段与 1700 行投影一致性；没有声称逐行全文阅读机器重复区，也没有找到足以承诺精确重建的原生成器。其判断中仍值得保留的是注册成功与 native runtime 创建、UI 成功与 native owner、文件索引与实际加载三组区分。关于旧 Camera、Input.Suppress、所有 native 都归 GameBridge 等结论已受后续实现和架构调整影响，当前事实必须回到现行矩阵与专题 owner；该记录未固定单一可复验 native 构建，不应自行补成当前游戏基线。
## SMAPI 的启发式兼容扫描不等于 Hook 事务

[2026-08-02 失效 Mod 审计](../../archive/reviews/code/2026/20260802-0003-smapi-invalid-mod-classification-and-hook-boundary-audit.md) 固定 SMAPI `develop` 提交 `5689c8d6aeecf54f670559ffaaed6684a5febc25`（`4.5.2-54-g5689c8d6`）。其 Cecil 预读不执行 Mod，但无效成员检查是启发式，反射字符串、动态 Hook 目标和全部 CLR 类型情形不在完整证明内；实际类型枚举发生在程序集进入 AppDomain 之后。`AssumeCompatible` 还可覆盖已检测到的不兼容。

该基线中，正式启动状态是 `Found`/`Failed`；注册前失败会跳过，注册后的 `Entry` 崩溃只记日志，继续尝试 API，没有通用卸注册、Unpatch 或副作用回滚。事件异常逐处理器隔离，也没有自动全 Mod 禁用。required/optional Hook 分组、完整 owner 回滚与 feature degradation 是 DTMAPI 选择承担的生命周期契约，不能作为“SMAPI 也是这样”来论证。当前 DTMAPI 具体保证仍以实现、产品 admission 及 [当前矩阵](../../api/public-api-matrix.md) 为准。
## 产品归属纠偏的历史依据

[7 月 19 日 AutoFishing 审查](../../archive/reviews/api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md) 与[平台缺口/过剩审查](../../archive/reviews/api/2026/20260719-0011-smapi-api-gap-functional-surplus-and-content-host-review.md) 明确撤销“受 DTMAPI 管理就必须把所有 native 实现放 GameBridge”的普遍禁令。SMAPI 对照固定在 `5689c8d6`，Yet Another Fishing Mod 对照固定在 `e23b8d72ea1d5551cf770733801d712dec28e83f`，只用作架构证据。把文件挪入另一个必装 DLL 或机械保留单消费者 facade/session/QA 路由，都没有降低基础 Runtime 的产品负担；应按责任移动并删除原限制创造的脚手架。物理 LOC 是结构指标，不能直接当作 CPU/GC 结论。

无 DLL 内容包依然拥有发现、身份、版本、依赖、启用和 Manager 管理；内容 host 负责解释其领域 schema。可复用的动物桥也不因此必须常驻 Core，且内容模板的两个配置不等于两个独立消费者。两份 Review 是当时的建设方向，具体哪些 API 已提供、兼容窗口是否结束、Content Host 是否准入应读取现行 owner，不能由旧缺口列表重新立项。
[Hook 历史总表](../../archive/hook-map/2026/README-history-through-20260711.md) 中的失败还说明应先校验取证链：异步截图字段未通过但文件稍后存在、latest-report 指针仍指旧运行、测试存档本身没有目标内容，都可能使门失败却不表示产品行为错误。启动监控的人为 1ms 阈值只证明告警触发；已有正常启动样本不证明不存在偶发长停顿，外部启动超时也不能借旧日志构造新运行。只补足缺失的证据切片，不能因此循环扩大样本或重做全部隔离。
## 合同与迁移记录各保留一份事实

[public API matrix](../../api/public-api-matrix.md) 仍是当前稳定性合同；全文历史快照混入大量 dated Notes 和逐次 smoke 经过。矩阵状态、adoption disposition、产品身份、某一候选的游戏 PASS 是不同事实，宜分别由矩阵、Catalog 和对应 Update 拥有，以链接连接。Stable 晋升条件只适用于 API 晋升任务，不能把旧“通常第三槽”或两消费者条件套用到普通产品修复。旧 dated Notes 中已被补记替代的 pending、产品计数和版本只作历史。

## 按具体产品能力分析游戏版本漂移

[DolocPlus 1.4.0 / CT 1.9 review](../../archive/reviews/api/2026/third-party-mods/20260801-dolocplus-140-lua19-version-and-semantics.md) 比较了 `23762374` 与 `24456188_test_E861E0`，DolocPlus DLL SHA `BEAA313EFC99BC5ADDCC76C8220BFE19CDB479BF9F843005B3F592CB0E056EA1`，CT SHA `9350C937A6BBA3EFE69A46605424FE37F3B316D8B3D8F8E3CAB2CD372464003C`。这些第三方原件仅供本地研究，不是可分发源码或运行验收。

- 该 DLL 的版本不符行为是提示后继续初始化，不是拒载。186 个 game types 全可解析、313 个 member refs 中三个失效、65 个 Harmony target 中三个缺失；多数签名存在仍不能证明整 Mod 兼容。CT 的全局“加权失败少于 50%”也不能证明关键存档语义。
- 整程序集 hash/build 变化是身份漂移，不必然需要每个产品重编；固定已发 DLL 的相关成员、Hook、状态 owner 与失败路径可以单独证明。review 的能力分级是当时设计建议，当前 admission 仍以实际平台合同为准，不能自行打开继续加载或 override。
- 相同标签可能是不同功能：DolocPlus 改 `GameLoop.FixedUpdate` 内 archive clock cadence，DebugConsole 经 `DolocAPI.SetTimeScale` 改全局模拟速度；前者的暂停不能偷偷替代后者。CT 写 Battle success 与 DTMAPI 使用原生 skip 路由也不是同一事务。
- 该 build 的 weather 已迁移到 room/season group，旧 global field 和旧调用布局无效；tech available points 是加载时根据奖励与已解锁节点重算的 derived state，未花掉的注入值不等同于永久存档值。
- Chest 的 native `GetAvailableInventories` 同时服务 equipment、agent 和 backpack-with-boxes；只证明方法签名不能证明“设备消耗”承诺的 caller 范围。递归遍历、native IsShared、引用去重可保留，但扩大其他 caller 应明确决定；不能搬用可能漏清理的 thread-static 标志。

review 当时要求的 AutoFishing movement、DebugConsole weather/tech UI 与 Chest caller 修复，后续验收归当前产品 owner。本页不将当时的 GC/全套运行建议重新作为修复前提，也不声称第三方已在当前游戏运行通过。