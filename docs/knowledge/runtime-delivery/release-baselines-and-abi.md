# 发布基线与兼容字节

本文记录历史身份的证据用途，不定义当前版本或发布许可。当前机器事实由 [Catalog](../../../tools/release/dtmapi-product-catalog.json) 和版本 authority 拥有，流程入口查 [current-state](../../onboarding/current-state.md)。原件/hash 见 [阅读清单](../../archive/migrations/20260908-workspace.json)。

## 旧发布、当前源码与未来候选分别识别

[July 13 subscription review](../../archive/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md) 及其 [Update](../../archive/updates/2026/20260713-0009-workshop-subscription-and-prerelease-baselines.md) 明确三个不同基线：当时已发 0.5.2-alpha 原件、未发但已测试的 0.5.3-alpha 源码、尚无最终 artifact 的 0.5.5 目标。具体值已经是历史；这种区分仍适用于新交付，不能重编旧 Mod 后冒充原来玩家收到的二进制。

该次 snapshot 最后为 Runtime 加 11/11 第一方产品，另有四个 public 外部 DLL，共 15 个 Abstractions 消费者；Chest 是审查中才由 Steam 下载到位。Steam subscription 是可变 cache，目录和 digest 不会重建已丢失 bytes；它不是开发输出目录，也不能证明全部外部生态已收集。

旧 Runtime 在临时 copy/fake game 的 install/status/collect/uninstall 压力矩阵通过，只覆盖该矩阵；未测试 unrelated marker-owned 内容的案例不能豁免当时的卸载 ownership P0。控制测试的旧包不自动成为可给玩家回退的安全包。

## Catalog 冻结的是具体身份，不是全部历史计划

[Batch 0 Update](../../archive/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md) 创建多轴 Catalog，把当时 Runtime/11 产品的 UniqueID、Workshop ID、folder、DLL/config/sidecar path、retained version/hash 和 release lane 固定下来。public identity/path digest 用来防止 Catalog 和旧投影一起漂移；精简文档不能顺带改变这些身份。`DTMAPI.AnimalPack` 当时是 `FrozenReservedNoArtifact`，没有 artifact 不等于身份可任意改作其他用途。

该记录也明确 public metadata 不给语义 manifest version、creator 不等于当前账号控制；两项事实不能由一个 Web 查询替代。旧 public snapshot `2026-07-13T06:27:27Z` 与它的 digest 是 dated evidence，未来重新查询应新增事实，不覆盖原件。

Batch 0 所说五 production DLL 当时仍有 embedded QA 债；后续 optional QA/Compatibility 迁移才改变加载边界。旧 setter deletion、版本分离、P0 修复和发布门都由后续 Update 收口，不能因为本记录最后仍说 pending 就重新开展整套审查。0.5.5 已发的最终结果见 [发布收口](../../archive/updates/2026/20260801-0002-workshop-upload-release-closeout.md) 与 [已发元数据 owner](../../archive/updates/2026/20260801-0003-runtime-published-metadata-authority.md)。

## 长时证据只回答原调查问题

July 13 记录用户反馈为少数玩家长局 GC，并非测量普及率；标题一小时加十次载入通过，只表明那条路径得到缓解，不等于 active fishing/加速及全 Unity Mono GC 已解决。

[July 7 input proposal](../../archive/reviews/code/2026/20260707-0003-dtmapi-hotkey-rebuild-implementation-test-audit.md) 的一小时要求随后被 [具体 implementation Update](../../archive/updates/2026/20260707-0004-hotkey-rebuild.md) 的用户指定 5min/2min/20min 路线及后续 unsuppressed/no-virtual 证据衔接。初次功能 half-pass 的短按丢失后来由 edge sampling 修复；不能把 proposal、首次 pending 和后续 PASS 叠成三个常设门。旧 Major roll-forward 执行记录仅留历史，当前必须用仓库 .NET 8 resolver。

## 版本权威与离线 Release 的早期闭环

07-14 Batch 2 初片曾在 Git-only worktree 因 Oil 测试读取四个 ignored reverse 输入而失败；临时挂载私有引用后通过只证明工作空间条件。该问题已由 07-15 `0009` 关闭：默认 Unit 使用 DTMAPI 自写的归一化语义夹具，仅含通用抽取角色、权重、次数上限、范围和行为，不复制官方表、标识或反编译代码。私有 conformance 是显式 `-ReverseBuildRoot` 研究入口，原生基线变更才重跑；缺少 reverse 不能重新成为默认构建/测试前置条件。该版实际核对基线为 `23762374_public_C416D4`。

07-15 `0011` 建立当时的 Runtime props 单一权威及实际输出契约：release 0.5.5、file 0.5.5.0、assembly compatibility 0.5.3.0 是三个不同轴。MSBuild 属性应按条件投影求值，不能取第一个 PropertyGroup 或搜字面量。产品 sourceVersion、已发布最低版本和 future target minimum 分别记录；Oil current 0.3.1-dtmapi 与 blocked target 1.0.0 不应互相覆盖。源码 manifest、原生 info、Catalog、发布文案、项目元数据和真正 DLL 元数据的比对证明一致性，却不授权向 Steam 发布。

早期“发布 builder 少三个产品”“AutoFishing setter 被删除”“Git-only 不能完成 Release”等门槛后续已关闭，不能作为当前 pending 项反复跑完整套件。保留旧二进制绑定/Entry/configuration证据也不等于移动取消或 GC 行为证据；真正活跃事项由当前状态和对应 Issue 接管。

来源：`docs/updates/2026/20260714-0004-batch2-version-release-authority.md`、`docs/updates/2026/20260715-0009-hermetic-release-semantic-fixture.md`、`docs/updates/2026/20260715-0011-batch2-release-contract-gate.md`。
## Optional Host 与候选身份的实际含义

07-22 在十五个已取得消费者 DLL 中确认五个精确旧产品使用五个 frozen API，四个外部样本未用它们；这是有限样本，不是全生态无消费者。选择一个 dormant-shipped optional Compatibility Host，保持 `DTMAPI.GameBridge.DolocTown` provider、public DTO/member、managed/Harmony owner 身份。代理实际调用可唤起 Host，静态 TypeRef 扫描只用于提示，因为反射消费者可能没有静态引用。Mono 不能卸载已载程序集，停用清理 owner/demand 不等于卸载 Host；dormant-shipped 减少默认加载，不减少下载字节。后续包中已有该 Host，旧 Review 的“实现前九门”不是新的待实现计划。

07-27 路线与 07-28/29 审查反复划清候选：完整 Release 临时重建包与冻结玩家目录是不同输入；只有实际选择新字节才重冻相应候选。仅补 Changed Files/证据路径或修 SDK 不重建 Runtime。Core 曾嵌完整约 77 KB Catalog，而真实 legacy 准入仅约 1 KB，使无关文案也改变 Core/hash；最小 Runtime projection 必须从唯一 Catalog 派生，不能手写第二份权威。这是去重复的重要历史动机，其实现状态继续查后续 owner。

ABI 符号全部可解析仍不能证明旧 DLL 在 Unity Mono Entry/实际 API 调用通过；一次进程可覆盖多个精确消费者，但每个需要独立来源和行为结果。无 demand 时“无 Hook/无逐帧内容工作”也不等于没有平台空 feature 注册和 lifecycle refresh。旧 legacy native DLL 的 title return + natural process exit 不证明其自装 Harmony 已逐 owner 热卸载，必须保留这个证据限度。

07-29 四 BAT 包修复已接受；Standalone probe 与 dormant helper 保留，玩家 root 不再重新带回第五个 probe。该时点包内六个 runtime/optional DLL 未变，合理验收只重跑 installer/package focus 和重冻 tree。之后版本已再次变更玩家入口，当前四脚本/Doctor 等布局不能仅从这段历史恢复。

07-27 三语言发布文案有独立 frozen owner `docs/releases/0.5.5-workshop-update-copy.md`，网页逐语言粘贴/回读与包元数据更新不同；本页不复制文案或未勾选清单。旧路线曾把 MES 1.0.0 和通用受保护存储推迟，后来产品修复与新版路线接管，旧波次/硬停止不构成当前禁令。

来源：07-22 Review `0010`，07-27 prerelease roadmap，07-28 Review `0004`，07-29 Reviews `0001`、`0002`、`0003`。
## 0.6 的原生来源与兼容性分类

08-02 冻结实施记录中，官方双来源已取代旧 SDK source weight 0/1/10/30：官方 Local 与 native 验证的当前 Workshop 根先按 enabled 过滤；只有唯一最高 native priority 才能裁决同 UniqueID 多候选，缺失或平局应阻断该身份。Workshop 快照只证明路径来源，不赋予任意胜出优先级；订阅存在、manifest 版本和源标签均不能使禁用候选生效。准确的现行加载合同回到 Runtime source owner，不在历史页维护第二套规则。

Advanced 包的构建身份仍严格校验；当前游戏相对该身份被分类为 Exact/Drift/Unknown，不等于后两者自动拒绝。0.6 路线接受静默尝试加载，只有实际类型/Entry/该产品原子激活失败才隔离 owner。历史 22 Hook、九个冻结 API family 和局部第三方扫描零命中，不能证明可删除 ABI 或要求新全平台审计。程序集在 Mono 进程中固定的物理生命周期仍需尊重；当时的热启停矩阵不替代后续启动期边界。

来源：`docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`。基线为该轮 `24456188` 当前游戏与 `23762374_public_C416D4` 冻结引用的比较，不能无检查外推未来 build。
## 第三方订阅静态审查不能代替现场加载证据

08-09 的精确订阅截面以 29 DLL 分成六框架、十一第一方与十二非第一方，全部十二份做过本地技术审查。重要结论是某受管安装入口会将 helper 复制到 BepInEx/plugins；此后它是 External plugin，禁用/退订 Workshop 入口不能证明 helper 已退出。物理存在、Runtime 可发现、官方启用、实际加载须分别取证。

库存 mutation 后无保护 Postfix 抛错可截断下一步，分别造成“源已空而目标未放入”“背包已增加而世界掉落未删除”；同类 NewGame Postfix 可能阻断上层退出 Loading。对应 Mxx、StorageExpansion、MagicStorage 等只是该组确切 SHA 字节的静态候选，缺少故障当次加载和首异常，不能写成已确认玩家根因。Issue 5 的官方任务存档根因已独立闭合，不能因为新候选推翻。受影响版本的 sidecar 在 native save 前或未保存回标题写入也只是确认的设计风险，不自动证明玩家现场。

当前处理回 `docs/reviews/manual-qa/2026/20260808-0002-public-newgame-and-functional-mod-regressions.md` 及后续 issue。原审查中的强制隔离建议按现行测试 workflow 选择最小对照；不能默认所有反馈均需要全环境复制。第三方反编译源码未保留/分发，本知识页也不复制源码。

来源：`docs/reviews/code/2026/20260809-0001-subscription-third-party-dll-regression-audit.md`；结论限定 2026-08-09 订阅截面及原件列出的 SHA256，不外推后续版本。

## 0.6 审查的正反例不能只读标签

8 月 1 日“几乎全部 Mod 出错”主要是九个 Advanced 双来源产生 36 条 Entry 前精确引用警告，不是九类运行崩溃；旧 MES 进入旧 ABI 后的攻击签名失败是另一个层次。最初逐 build 重发全产品只是提案，后继按真实 native delta/消费者决定，见[基线比较](../reverse/research-baselines.md)。来源：[根因审查](../../archive/reviews/code/2026/20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md)。

旧 DLL hash 正确不等于 MemberRef 可绑定；静态可绑定也不等于 Loader 已允许 Assembly.LoadFrom 或真实 Entry。7 月 28 日 retained 外部消费者静态 23/23，仍被 Strict native-reference 门提前拒载，后继只对 Catalog 已冻结、原生订阅身份及精确字节全部匹配的入口放行，不伪装成 Advanced。SDK marker 编译 target、policy 最低 Runtime 与 Host release 是不同轴。

发布集合曾按 CodeModKind=Advanced 误选未授权 MES/Strong/Mine；源码受维护集合、本次可构建集合与发布 artifact 集不能互代。缺私有 exact 输入可在声明的开发 scope 记 unavailable，正式 Release 不得 SKIP 后整体 PASS。反编译旁 raw DLL hash 不证明文本完整，sibling 同名 token 不证明目标方法控制流；相关路径要有准确反例。

8 月 4 日九家族调查仍有四个 opaque archives 未扫；八个新 ProductNative 不引用旧 provider 不代表 DTO 可删，当时 DebugConsole 还消费 28 个公共 DTO/enum。Steam 最新订阅不证明离线旧 DLL 消失，Doctor 类型提示也依赖 retained metadata。后继释放某个旧消费者约束仍须沿当前产品/API owner，8 月 30 日 Y 旧消费者释放不能自动删除当前 DTO。

来源：[第一组](../../archive/reviews/code/2026/20260804-0001-dtmapi-060-first-five-slice-parallel-review.md)、[第三组](../../archive/reviews/code/2026/20260804-0009-dtmapi-060-third-five-slice-parallel-review.md)、[九家族原件](../../archive/reviews/code/2026/20260804-0019-dtmapi-060-nine-family-consumer-and-removal-review.md)。官方页面事务和选择问题只在[来源知识](official-source-selection.md)归并。
