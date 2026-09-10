# 平台作者路线的历史交接

本页提炼 2026-09-07 前后已经落实的修正与被替代的先后关系，不拥有任务规格、公共契约或当前队列。下一步始终查 [status](../../planning/platform-next/status.md)；目标查 [platform-next](../../architecture/platform-next.md)，具体行为分别查作者、Runtime、数据内容领域 owner。原件范围见[迁移清单](../../archive/migrations/20260908-workspace.json)。

## M0 的三项修正已有真实 source/package 验收

PN-001 核对 08-09 与 08-20 发布来源后，确认普通 Unit 复制了过期 tree hash；没有回滚 Catalog 迎合测试。它拆开源码版本关系与精确发布产物验证，相关 release selector 也已去掉旧九产品/MES1.0.0 限制。完整 Unit 已从头通过，被首失败遮住的后续测试已实际执行。此历史失败不再是当前阻塞，具体实现见 [Update 0002](../../updates/2026/20260907-0002-platform-version-projection-baseline.md)。

PN-002 用真实 SDK prepare→Core reader 复现 0.5.5 编译目标与 0.6.1 Host 精确比较失败，随后把 API target、SDK release、Host release、session protocol 分开。schema 2 通过认证 hello 固定协商结果；schema 1 保留已知 0.5.5 wire alias，并用真正旧 SDK executable 验证响应读取。未知 Host 和超时不会猜成升级要求。队列、凭据、一次消费、重放及关闭语义保留；无 descriptor 不开 listener。它完成托管协议和本地 SDK 包验收，未证明实际 Mono 作者旅程，也未发布新 SDK。精确协议归 [SESSION-PROTOCOL](../../../author-sdk/SESSION-PROTOCOL.md)，实施见 [Update 0003](../../updates/2026/20260907-0003-platform-author-session-handshake.md)。

PN-003 已引入唯一 target catalog 与共享 BCL reader，链接到现有 SDK/Core/Doctor，不增加 Runtime DLL。available 与 planned 分开，冻结 0.5.5 contract/payload 原字节不改；新 writer 的严格 floor 与旧 Strict/ContentPack 低 floor reader/recovery 兼容分别验证。保留 marker 的包不能通过删除 CodeModKind 绕过验证；schema 1/2 由实际读者一致处理。0.7.0/SDK0.2.0 仍是未来候选，不创造不存在的载荷、不改发布记录。实施见 [Update 0004](../../updates/2026/20260907-0004-platform-sdk-target-catalog.md)。

## 后续顺序由长期路线取代初版“先反射”

初版 Review 的“PN-004/反射优先”与任务卡保留的原规格受后续迁移节覆盖。快照中的 M1 顺序是 PN-015 CLI/IDE BuildPlan→PN-004 官方 Local→PN-016 原生生命周期事实→PN-017 实际调试→PN-008 外部作者闭环；反射已移至 M3。PN-014 只穿插必要的内部职责抽取，不把全局清理设成前置。

M2 公共 target 分 a 候选、产品实测、R2、b 冻结，防止“先冻结才能测、先测才能冻结”的循环。M3 依赖/原生格式可以分开的有界复盘；M4 非持久内容无需等 SaveData；SaveData 先证明持久身份和原生成功窗口，不能拿 slot 或 save epoch 当持久身份。详细 A/AD/RT/D 决定直接回领域 owner，本页不复制三十五任务或 E01–E08 表格。

## 哪些精简有依据，哪些能力仍未证明

当前单向程序集骨架、Entry checkpoint、owner 清理重试、事件快照/有界队列、零需求路径已有真实责任，应保留。Compatibility 源通过 Compile Remove/Link 编译到可选组件，磁盘重复不是 mandatory 加载重复；Mono 已加载程序集不能冒充卸载。优先合并重复版本、分类 reader、状态投影与已无用途开关；先移动责任，再单独改加载/来源/Hook 行为。

BuildPlan 必须以外部工程的真实 CLI/IDE 入口证明相同输入和产物；不能以 csproj 字符串存在或维护者复制 DLL 补流程。PDB 存在、源码行可定位、debugger attach、断点命中是四种证据；未知能力不记通过。E 编号是按任务选择子项的家族，不是每次跑全套或逐条写免测说明。阶段集成与候选固定的完整作者旅程保留，普通实现只验实际变化。R1–R6 是对应契约/原生事实的有界复盘，不要求再读全仓或建立第二套 PASS 收据。

上述判断来自完整阅读初始 Review、四份实施/规划 Update、主架构与全部六份平台规划页。真正开放事项由 status 唯一记录；路线目标不等于当前 API、Mono 支持或已发布版本。

## 已接受设计仍需各自的实施与原生证据

[Runtime 设计](../../architecture/platform-runtime-contracts.md) 分开 Runtime/Owner、SaveSession、World epoch 与持久 SaveIdentity；GameLaunched/SaveLoaded 的发布时点不承诺所有 native 对象 ready。scheduler 复用 Core 唯一帧源，并设计容量、公平、deadline 与终态释放；取消不能中断已开始的同步 callback。设计中的 1024/128/64/2 ms 是内部保护候选值，不是当前公共性能保证。

[数据内容设计](../../architecture/platform-data-content.md) 分配置、全局作者数据、随档 Working/Committed 和只读包资源。SaveSaving 当前 failureThreshold=0，并无“异常反复后放过原生保存”的证据。未来 required prepare/commit/abort 与普通 observer 熔断分开，先处理旧兼容 callback 再冻结候选；原生成功后 sidecar 提升失败不能宣称原生已回滚。可靠 SaveIdentity、durable prepare 和 native 成败关联尚须按 M4 进入门证明，slot/名字/路径不能代替身份；未知 schema 或坏 journal 不能被默认值覆盖。

ModContent 的资源租约与 GameContent 的官方最终结果分开；清缓存不等于活对象已传播更新，登记 ContentPack 不等于 Host 已接纳。首 Host、两个独立 pack/作者及真正分发 SDK/Mono 旅程有各自进入门。未证明的动物/实体平台、AssetBundle 卸载或作者任意副作用事务不能从旧合成 G2/单产品 PASS 推出。

历史 Full G1、Content Host、通用 protected storage、泛用 Advanced 与反射优先清单只提供来由，当前顺序归 status。9 月 8 日入口和测试图工程另有 [0001](../../updates/2026/20260908-0001-development-entrypoint-simplification.md)、[0005](../../updates/2026/20260908-0005-workspace-test-projects-and-ci.md)；历史文字迁移自身不完成 PN 工程，也不证明新 API 已运行。
