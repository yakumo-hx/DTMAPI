# 平台结构与兼容设计的历史证据

本文合并 Feature Host、SMAPI 版本研究、CodeMod 生命周期和本体技术债审查。它不拥有当前路线或测试门；当前职责与保存语义查 [PROJECT](../../../PROJECT.md)，API 状态查 [public matrix](../../api/public-api-matrix.md)。原件与提取去向见[迁移清单](../../archive/migrations/20260908-workspace.json)。

## 机械分文件与真实责任迁移

6 月 8 日 partial 只移动方法；随后 Feature Host 才统一注册/状态/生命周期，将 production Update 移出 SmokeHarness，Camera API 注册离开 ExperimentalBridge。按 feature/operation 隔离异常、继续后续消费者有实际价值，但不需要为内部状态再扩 public-like interface。ActionCompletion、Oil 和燃料/喂食当时共用 native touchpoint 的顺序不能被文件拆分忽略；当前 ProductNative/SharedNative 归属以 PROJECT 为准，不恢复旧 service 清单。

早期 Loader 已通过 Core fixture 证明多 DtmMod DLL 的显式 EntryType、包内 EntryDll、依赖环拒载及 owner+delegate 退订；只按 delegate 会误删另一个 owner。CustomEntity 的四 registry、句柄和清理成立，与真实创建 `runtime-creation-blocked` 可以同时成立。6 月先由 Stable 改 StableCandidate，后又收口为 Experimental/Frozen；metadata 修正不需要用合成登记为原生创建补票。

7 月 ContentManifestRegistry 的名字虽有 authoritative index，实际仍 diagnostic-only、RegistryTakesOver=false；它曾漏掉只在 runtime registry 中存在的平台 provider。补真实输入才解决假 missing dependency。增加 owner ledger 只让 Mod→GameBridge→native 的责任分裂可见，不会自行减少实际常驻对象或改变加载。

来源：6 月 8–10 日 Feature/Loader Updates；[8A index](../../archive/updates/2026/20260704-0001-content-manifest-registry-index.md)、[责任与崩溃相关性](../../archive/reviews/code/2026/20260705-0002-issue010-boundary-correlation-research.md)。诊断及配置事务的具体失败合并在[诊断与 UI](../runtime-lifecycle/diagnostics-and-ui.md)。

7 月 DLL/Entry 审查后半明确撤回前半“已符合目标”：产品只持 config/input、基础 Runtime 仍持专用 native 实现，达不到默认加载减重。稳定 Abstractions 不暴露 native 类型，不等于产品自身不能引用 native；文本政策连注释都拒绝 `advanced` 只会固化遗漏。相同 Hook target、相似源文件或普通 GetApi 调用均不足以建立 SharedNative；需要实际独立消费者及共同 owner，不能为无 retained consumer 的 Mine/StrongPlantingGun新增重 Host executor。

来源：[完整模型纠正](../../archive/reviews/code/2026/20260719-0008-dll-mod-entry-and-migration-boundary-audit.md)、[冻结身份附件](../../architecture/batch6-managed-mod-identity-contract.md)。

## 先确认哪个阶段已经关闭

[July 18 能力审查](../../archive/reviews/code/2026/20260718-0002-smapi-version-capability-batch5-route-review.md) 的 DTMAPI 截面是 `c93c460e`，SMAPI 是 `5689c8d6aeecf54f670559ffaaed6684a5febc25`。它记录了当时 eager GameBridge、每帧内容文件签名、事件快照分配和永久产品 tick；这些不是今天尚未整改的清单。

[August 1 收尾复比](../../archive/reviews/code/2026/20260801-0001-dtmapi-055-closeout-smapi-capability-recomparison.md) 在 DTMAPI `2bda4a60`、已验玩家源 `d389da0fe89b38fcc0257fb5213153c9c123cb32` 上明确关闭 Batch 5：不可变事件成员、512 上限队列、按需 producer、内容 generation/last-good、config 原子替换、已准入 ProductNative 和旧 ABI 均已有对应证据。无需求运行是 300 warm-up 加 10000 measured frames、18 项 optional work 零增量；这证明对应路径静默，不证明全游戏 GC 根因或数值预算。旧独立 GC ladder 和十一产品组合属于该阶段验收，不能成为每次修复的默认门。

“有接口”“有内部机制”“普通作者可用”“两个实际消费者”“已发布”需要分开。所有产品都会 GetApi 不表示存在跨作者服务生态；QA fixture 和两个同实例 alias 也不是两个消费者。后续能力扩展应从实际缺口出发，不以追平 SMAPI 的 API 数量为目标。

## SMAPI 历史研究可支持的范围

上述研究使用固定本地 Git 历史，不是对今天 SMAPI 最新版本的判断。1.x 已有依赖和异常隔离，2.x 逐步加入内容和跨 Mod 服务，3.0/3.14/4.0 经分代替换与告警窗口迁移事件/内容合同。其现代 HasListeners 路径仍可能每 tick 更新 watcher；不能说 SMAPI 已给出完整 Hook producer 激活方案。

August 1 复比纠正了 July 研究的两处事实：Private Assemblies 在 4.1.0 引入后由 4.1.4 的 `4af917335b424a77edd33aa132aad55dd7979233` 移除；4.5.2 attestation 链接是在 tag 后的 `4d44e661381296a4e2d0a84b6dc18b042f6cff8d` 加入。引用版本能力时必须区分短暂功能、tag 和后加文档。

真实 Mod 的 MemberRef、manifest 和内容文件只证明打包依赖及复杂度，不证明每条分支运行通过，也不是生态普及率。SMAPI multiplayer、Stardew asset namespace、Content Patcher DSL、自动存档修复和每日备份不因参考平台具备就成为 Doloc 需求。

## 停用合同与服务移除不是同一件事

[GMCM 停用审查](../../archive/reviews/code/2026/20260803-0001-smapi-gmcm-codemod-deactivation-contract-review.md) 固定 SMAPI 上述 commit；GMCM 1.16.0 commit `c90f93f65eee9f9704852b7dfa503f4b6d415950`、DLL SHA `E6ED782BB8B1BAC1DEEB1B3A09DB8EB3758BDC7CA510F50E7CCFB7D2C1D9479C`。它证明：

- SMAPI 的 Dispose 是整个进程退出时的清理，不是同进程逐 Mod 禁用协议。
- GMCM Unregister 只移除注册字典项；已打开菜单仍可能保留旧配置对象和 callback。六个真实调用点都是 Unregister 后立即 Register，用于重建服务注册。
- DTMAPI 在审查截面已经有更强的 owner cleanup 和 stale facade/page 失效；零已知 roots 仍不等于 Mono 程序集已卸载。

可复用的设计区分是：准备失败时仍 Active；开始清理后的失败应报告 inactive-with-errors/restart-required；任意作者 callback 的重复副作用和平台幂等 cleanup 必须分别决定。review 推荐的 typed reason/transaction、最多一次 callback、重试规则只是当时设计输入，不能被本知识页写成已发布 API。当前执行顺序与未完成项由具体 lifecycle contract/整改 Update 拥有；旧反射方法名也不因文档出现就成为作者接口。

## 产品版本、最低 Runtime 与绑定身份分别维护

[版本历史审查](../../archive/reviews/code/2026/20260803-0002-smapi-mod-runtime-assembly-version-history-audit.md) 使用 DTMAPI `abe9596cbdde2d0d3b2a66d34f8b867b5af0beac` 与固定 SMAPI commit，已发 SMAPI DLL SHA `5E4C51BB3CD6616F5ADCB804769DBB7C6872B2478FAB786FB4CA00C72C7456BF`。SMAPI 通常随发行提高 assembly version，同时有 simple-name fallback、Cecil rewrite 和 compatibility policy；不能只模仿其版本增长而假定 DTMAPI 具有同样 binding 机制。

DTMAPI release/API、file/informational 和 assembly compatibility 是独立轴，打包器不可由 release label 自动推导绑定身份。固定 assembly identity 也不能代替 manifest 的真实 minimum：新 API 或只有新 Runtime 认识的 exact policy，都可能要求更高 floor。最低可执行 Runtime 与冻结 SDK 编译目标允许不同，应通过实际 pre-load 拒载证明。[08-02 冻结实施记录](../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)是当时 0.6 marker/policy 决策与验收的历史来源，不是现行路线。当前具体版本由 [Runtime/ABI 版本源](../../../tools/release/dtmapi-runtime-version.props)拥有，身份和保存边界由 [PROJECT](../../../PROJECT.md)拥有，继续建设读取 [当前平台交付入口](../../planning/platform-next/README.md)；本页不复制数值或 PASS 台账。

## 本体去重应消除重复事实，而非改写全部层次

[August 31 技术债审查](../../archive/reviews/code/2026/20260831-0003-dtmapi-body-technical-debt-smapi-clean-room-audit.md) 的截面为 `98d75c6c51300768e3d7a8b209c858c9ad110493`，开始时已有其他未提交修改。它把实际正确性问题、可靠性风险和结构债分开：optional 环与真实数组泄漏是源码可证明缺陷；设备热替换、count-only Hook 在新 overload 下的后果是待复现风险。不得把这些全部叫现有玩家故障，或仅凭旧 Review 认定当前尚未修复。

保留的改造方向：

- 由 Loader 产生结构化 decision code/stage/evidence，Manager/diagnostics 格式化；不要从中英文错误句子反推机器状态，再另写一套依赖判定。
- 一个 per-owner record 派生视图；先说明删除了哪份重复状态及保留的 checkpoint/rollback，再抽编排流程，不为拆大类创建多个并行 Manager。
- 工程 inventory 对齐标准构建、脚本和产品入口；纯单元可拆 fixture，ABI、native、进程和游戏 harness 保留各自用途。生产文件被多项目 Link 编译时，要确认测试针对的是最终发布类型。
- correctness invariant 不应是假开关；只作描述且不驱动行为的 feature flag 不该报告 contract 已验证。保留实际安全机制，合并重复 lifecycle summary/counter，不再堆一层 ledger。
- Compatibility 的 dormant-shipped 策略和其源码 Link/同名遮蔽是两件事。可以修编译边界，不能用瘦身名义附带删除 Frozen ABI。
- ContentQuery 独立推算 winner 不等于 native final table；Frozen CustomEntity 的 blocked verbs 不构成补齐大引擎的需求；Audio 必须区分内容、单产品策略和真实共享 adapter。

该 Review 的数万行统计是审阅半径，不是耗时/分配/默认加载证据。其根 solution 九个失效项目与完整 Unit 旧 hash 失败都有明确当时基线；focused PASS 不覆盖新缺陷，旧失败也不能覆盖当前建设后的验证。整改状态归各 implementation Update，历史审查只保留根因和简短 resolution link。
