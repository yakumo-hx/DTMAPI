# 旧装备数据迁移：格式、权威与失败顺序

本页合并 2026-07-29 起重复审查中可复用的推理。它不是新迁移协议或现行完成状态；实施与实测仍由 [原 Update](../../archive/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)及对应 Issue/当前源码拥有。历史审查的发现、修正和后来否证均保留原文。

## 先分类，再采用

真实旧 global writer 曾只有 ownerId/savedAt/slots，未写 schemaVersion；不能只测试人工 schema 2/3。精确无版本历史形状与带身份的 flat schema 1–3、nested Product v3、future/ambiguous/invalid 是不同输入。数值 schema 相同不代表 JSON 合同相同。

身份完整输入须保留 archive、玩家、storageScope 和原生保存时钟校验；物理路径为 global 不豁免明确错误身份。无身份 pre-schema 的一次性采用是特定历史兼容例外，不能概括成“任意旧 JSON 可以认领”。

canonical scoped 文件存在时，即使损坏/未来/歧义，也不能再回落同 owner global 发放物品。live 与 eligible previous 必须从发现、demand 到实际加载全链路可达；future、wrong-scope 或 revision-regressed live 不能借 previous 绕过。

## 必须保留的失败顺序

20260729-0004 至 0010、20260730-0001 的审查反复暴露了不同顺序，不能折叠成一句“claim 已测试”：

- Product 发布后才归档/认领，另一存档能抢先采用同一无身份 global。
- 枚举 claim 后看到新文件便早退、不再验证 owner，会产生双 Product；同进程 static lock 不能证明跨进程正确。
- claim/source/Product 不可见的组合被当成真正首次安装，错误建立空 authority。
- 删除唯一完成 claim 留出迟到发布窗口；输家在自撤前崩溃会让赢家也不可加载。
- 历史 Product＋archive 无 claim，需要与已有精确 authority 一致的完成屏障；per-hash 独立凭证不足以表达 game-root 单赢家。
- 使用过期 exists 布尔值 Replace 可覆盖其他 scope；需跨进程操作仲裁并验证实际被替换 authority。
- global 的预检哈希与删除/归档有 TOCTOU；原子捕获后仍需核对捕获字节。捕获后进程退出必须有明确恢复入口，不能依赖 catch。
- 完成前验证必须包括 active global 已消失；重新出现的 global、错误 archive 和无法判定的 capture 要保留并可见失败。
- 已存在 winner 应先判定，迟到输家证据不得阻断合法赢家；还没有 winner 时必须把其他 scope、其他 source hash、eligible previous 全部纳入竞争 census。
- capture-only/claim residue 不能允许 CreateEmpty；Product 缺席时应能触发 Host 或明确恢复诊断，不能让物品在磁盘上却永远不可达。

精确旧字节备份应临时写入、flush、回读、原子发布；坏最终备份不是理由删除唯一源。迁移 archive、备份与 completed 记录是证据，不能继续当活动物品 authority 参与发放。

## 生命周期与测试层级

曾经单独修好 resident Host 的 Notify→Recover，但真实 Hook 又从专用通知与 feature fanout 各派发一次 SaveLoaded，第二次清除第一次恢复 session，后续 SaveSaved 无法收口。测试需要分别证明生产分发恰好一次与真实 Host 事务终态；不强制合并成巨型端到端 fixture。

测试名字不是覆盖证据：等待 100ms 的同进程线程不证明第二进程竞争；反射注入 fake backend 只调用一次 service 不证明生产 fanout；成功归档后重新放回 global 不证明发布后崩溃。应以确定性窗口、真实子进程退出及终态字节/authority 断言表达所声称的边界。

普通迁移成功、claim 崩溃、旧消费者兼容、Product 缺席恢复和迁移后玩法保存是不同验收。沿同一需要保留的阶段复用 fixture；改变 converter、cold Host 或 runner 时只重跑受影响阶段。旧 staged U2 成功不得升级为未运行的 C0/U1/U3/U4 PASS。

## 后续收敛与反向过度保护

20260730-0002 至 0008 显示，只补前一处条件容易制造另一条入口差异：pending evidence 和 pending winner 仍需先 census；但 completed winner、archive 和备份是合法永久证据，不能永久唤醒 Host 或阻止另一个新档使用空 Product。无身份 global 只能被采用一次，不等于全 game-root 只能有一个存档使用产品。

迁移时间 T0 是不可变历史证据，正常保存后的 Product T1 是可变修订；终态验证应证明同一身份、T1 不旧于 T0、精确 stamp/archive 与 global 缺失，不能把普通当前存档的防超前规则反用在 T0。A 自身冷载、B 新空档、optional evidence 冲突三条入口都必须使用一致的 terminal 判定。带身份 GlobalFlat 也需能用唯一 Product stamp 解释 archive；缺失保存时钟不能当通配值。

只读冷需求在相关 SaveLoaded 重算即可，不能用每帧扫描解决 process-lifetime 缓存；加载产品应抑制正常终态证据的 orphan 假警告。

## 验收成本的具体收缩

20260730-0009 至 0011 区分真实游戏执行与断言质量：U1 用观察值当预期值会让重复物品通过；只统计合计 occupied 或漏算盾牌邮件也不足以证明每件物品。固定/基线推导的 per-item 背包＋未领取邮件＋active sidecar 守恒方程才匹配承诺。

随后明确撤回了游戏内 claim 强杀门：文件事务窗口由确定性跨进程测试负责，游戏负责原生加载/保存整合；撤回门槛必须显式记录，不能默默从摘要消失。已接受的后续 cold observer 只需修复最后一段，不重跑 U1/准备/保存或完整 Release。

通用 cold observer 的预期应来自已有 baseline 和 committed slots；把一次两件物品场景硬编码成所有场景预期会拒绝合法空档/单盾档。这个 QA 重用问题不反向否定那次真实两物品 PASS。仅文档/QA 收尾也不自动改变旧生产包或强制重新跑完整 Release；要诚实记录实际测试 tree 含哪些未跟踪文件，之后修成 clean checkout 可重现。

20260805-0003 Branch B 明确采用同 index 的 committed 表示迁移，不先把旧槽全部转背包/邮件再装备。精确旧值、盾耐久与原始字节备份保留，原子发布后验证；它不引入玩法变化，因而无需为格式转换立即调用 native SaveGame。Product 缺席的 owner recovery 是独立出口。一个本地空侧车不能推导所有玩家历史迁移都可删除。

## 超长生命周期记录应冻结，不重新投影每个检查点

[20260723-0008](../../archive/updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md) 的 111,043 字符正文完整串起初始拆分、未保存回档反复纠正、格式迁移、多轮 claim/胜者修正、冷邮件 oracle、生产结果不明以及最后延期。中间多次 verified/closed 被后段明确撤回；最后状态是旧 Runtime/旧 ABI 定点接受、新 Product 与五项 P1 延期，后来的直接替换生命周期另有 owner。不得读到某个 PASS 就把整个文件当现行权威。

其实际可缩减成本有明文证据：广域 test.ps1 在 120/300 秒两次窗口超时；原完整 Release 到证据 allowlist 失败后，用户明确不重跑昂贵前缀，只补修复门与此前未跑尾部；relative output root、字面/正则混用、未撤 Author deployment、空 cold sidecar、缺匹配 Host/manifest、modal 暂停 QA 帧、隔离程序集和输入回退均曾造成重复游戏尝试。最后旧 ABI 运行 scoped target 通过，aggregate 因外层仍持有 stderr 文件导致 cleanup 失败，保留 partial，不能伪装整个 runner PASS。

正确分层已有先例：纯 QA oracle 改变不重建 Product/Runtime；同一个生产包的已通过行为证据只更新受影响断言；三态放置、不可读邮件、跨进程竞争在确定性测试中验证，游戏只补原生 owner 路线。源码默认装载减少与总源码/包体变小也必须分开，该记录量化的是前者，后者实际没有下降。
