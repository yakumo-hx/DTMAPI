# Batch 6 G2 Advanced Synthetic Vertical Slice 前置审查

## 记录状态

- 日期：2026-07-20
- 状态：`recorded / corrected Phase 0 passed / synthetic G2 implementation admitted / real products blocked`
- 性质：G2 原子实现前置审查，不是 Advanced 通道完成声明或真实产品迁移授权
- 范围：manifest、Author SDK、reference receipt、package/deploy、Core classifier/loader、Harmony owner、Doctor、Manager 与 synthetic fixture
- Source：用户要求在修正 Phase 0 阻断后完成 G2 Advanced CodeMod 原子 vertical slice，并且只用 synthetic fixture 验证完整链路
- Owning Update：[20260720-0007 G2 Advanced Synthetic Vertical Slice](../../../updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md)
- 前置权威：
  - [Batch 6 Managed Mod Identity Contract](../../../../architecture/batch6-managed-mod-identity-contract.md)
  - [Batch 6 Phase 0 Machine Gate Correction](../../../updates/2026/20260720-0005-batch6-phase0-machine-gate-correction.md)
  - [Batch 6 Boundary Correction Prerequisite](20260719-0012-batch6-boundary-correction-prerequisite.md)

Phase 0 correction 已在提交 `1239aa577d74e4f0c29131ba644e8a751eebbf5f` 上完成提交后复验：保留字段在 G2 前 fail closed、消费者证据和收据时间可核、mandatory Runtime ProductNative 零增量由 baseline-to-audit 源码差异执行、API/路线权威已对齐。因此本 Review 只准入一个可整体回滚的 G2 synthetic slice；AutoFishing 和其他真实产品仍被阻断。

## 一、原子边界与裁决

G2 不能通过单独删除 `SDK160`、手写 manifest、手工组包、仅让 DLL 成功加载或仅让 Harmony 补丁生效来验收。以下面必须在同一 Update 中共同成立：

1. `Type=CodeMod` 与正交 `CodeModKind=Strict|Advanced` 进入同一版本化 schema/model；省略 `CodeModKind` 兼容归一为 Strict，ContentPack 携带该字段或代码字段必须拒绝。
2. Author SDK 必须从显式项目意图建立 Advanced lane，并只从用户显式提供的 game root 与受跟踪 reference policy 解析引用；不得从 PATH、包缓存、旧工作区、临时目录或环境中猜测程序集。
3. Advanced reference receipt 必须绑定当前 game build、逐引用游戏相对路径/长度/SHA-256、manifest、entry payload 与预期 Harmony owner；包中不得复制或分发 Assembly-CSharp、Unity、Harmony 或 BepInEx 原生依赖。
4. Core 的唯一 Managed Mod classifier 必须在 `Assembly.LoadFrom`、owner/API/registry 发布和 Harmony patch 之前完成声明、来源、位置、收据、hash 与 game-build 分类；任一未知或不匹配输入保持零发布。
5. Advanced Entry 前后由 Runtime 监督规范 Harmony owner `dtmapi.mod.<uniqueid invariant-lower>`；错误 owner、重复 patch、Entry failure 与后续 owner drift 必须可诊断、隔离并标记 restart-required，不能宣称 Mono assembly 可卸载。
6. Doctor 与 Manager 必须独立展示 declared/effective identity、provenance、placement、native-reference risk、game compatibility 与 restart policy；不能从 DLL 名、路径、产品名或源码文本推断 Advanced。
7. 唯一正向载荷为 `DTMAPI.AdvancedFixture`。G2 不得修改 AutoFishing manifest、源码、包、安装状态或第五存档权威。

任何一面未完成都使整个 slice 保持 blocked；不允许留下一个被 Runtime 接受但 SDK/Doctor/Manager 不认识的部分 Advanced wire。

## 二、Synthetic fixture 的 native-owner 审查

### 2.1 精确原生目标

- 游戏构建：Steam public build `23762374`。
- 程序集：`Assembly-CSharp.dll`，reverse metadata SHA-256 `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`。
- 目标方法：`System.Boolean DolocAPI::Has087DemoData()`，metadata token `0x060000A0`。
- 方法体：只把固定前缀 `doloc-archive-` 传给 `DataPersistenceManager.HasDataStartWith` 并返回布尔值。
- 原生责任：标题/存档兼容流程中的旧 demo archive presence query。
- 权威状态持有者：`DolocAPI.dataPersistenceManager` 及其持有的持久化文件索引；fixture 不拥有也不修改该状态。
- 证据路径：`references/doloc-town/reverse/builds/23762374_public_C416D4/metadata/build-info.json`、`metadata/methods.csv`、`asset-ripper-unity-project/.../DolocAPI.cs`。

### 2.2 为什么适合作为最小证明

fixture 只调用该只读 query，并以 canonical Harmony owner 安装一个不改返回值、不写存档、不持有 Unity 对象的观察 patch。它证明 Advanced 能直接引用当前游戏程序集、进入受管 Entry、安装受监督 patch 并发布日志；它不提供公共 API，不把该方法提升为 SharedNative，也不承诺未来游戏构建兼容。

拒绝的替代方案：

- patch 保存、载入、玩家状态或 UI 状态机：副作用与生命周期面过大，不适合验证通道本身；
- 只反射解析类型而不安装 patch：无法证明 Harmony owner 与失败清理合同；
- 把该 query 包成 Abstractions/GameBridge API：只有一个 synthetic 消费者，既不是稳定 API 需求也不是两个真实消费者共享的 native owner；
- 使用 AutoFishing 方法作为 fixture：会在 G2 通过前偷渡真实产品迁移。

## 三、身份与收据合同

### 3.1 Manifest 和作者项目

- manifest：`Type=CodeMod`；`CodeModKind` 仅允许 `Strict`、`Advanced`；省略等于 Strict；字段大小写与未知值不容错；ContentPack 禁止 `CodeModKind`、`EntryDll`、`EntryType`。
- author project：使用新的版本化 schema，`projectKind=CodeMod` 与 `codeModKind=Strict|Advanced` 正交；旧项目省略时保持 Strict。
- Strict：继续只允许 Abstractions 且 `SDK160` 原样生效。
- Advanced：必须显式选择，并提供 game root；选择 Advanced 不是跳过 Strict scanner，而是进入独立的 receipt-bound reference policy。
- 两种游戏加载目标仍为 `netstandard2.0`。

### 3.2 Reference 与 package receipt

规范包内文件名冻结为 `dtmapi-advanced-references.json`。精确 JSON 字段由 SDK、Core 与 Doctor 的共享合同/测试同时冻结，但语义至少包括：schema version、managed identity、UniqueID、game build、reference policy id/version、逐引用相对路径/长度/SHA-256、entry DLL 相对路径/length/SHA-256、manifest SHA-256 与 expected Harmony owner。

该收据是 provenance 与 compatibility receipt，不是签名或安全沙箱。Runtime 仍需对实际包内容和本机 game root 重新计算；不能因为 JSON 自述一致就接受。package/deploy/recover/status 必须保持该文件与 payload 的事务绑定，并拒绝路径逃逸、额外 native DLL、hash drift、identity drift 或 journal drift。

## 四、Runtime 与生命周期风险

- classifier 输出至少包含 Strict/Advanced/ContentPack、declared/effective kind、verified/unverified provenance、managed placement、native-risk、game-build compatibility、expected Harmony owner 与 restart requirement。
- legacy/无 receipt CodeMod 始终是 Strict/unverified provenance，不会因引用、路径、DLL 名或 Harmony 使用升级为 Advanced。
- Advanced 在加载后默认 restart-required。停用可以清理由 DTMAPI owner ledger 持有的 event/input/config/command roots并尝试只清理 expected Harmony owner，但不得宣传 CLR unload 或任意 native side effect 已逆转。
- Entry failure：先保留原始错误，再尝试 canonical-owner unpatch 与通用 owner rollback；若出现未知 owner 或无法证明清理则保留 restart-required。
- sibling isolation：fixture 的 wrong-owner、duplicate、Entry-failure 或 late-drift 模式不得阻止一个独立 Strict sibling 的 discovery/Entry/status。
- external BepInEx plugin 只读诊断，永不进入受管 cleanup 或启停承诺。

## 五、验收矩阵

| 面 | 正向门 | 负向门 |
| --- | --- | --- |
| Schema/model | Strict 省略兼容；显式 Strict/Advanced 一致；ContentPack 保持无 DLL | unknown kind、ContentPack+kind/code fields 拒绝 |
| SDK/build | Advanced 只用显式 game root + tracked policy，netstandard2.0 构建 | Strict native 引用仍 SDK160；缺收据、ambient probe、hash/build/TFM drift 拒绝 |
| Pack/deploy | identity/reference/payload receipt deterministic 且事务绑定 | native DLL 捆绑、路径逃逸、手改 manifest/entry/receipt/journal 拒绝 |
| Core | 全部 compatibility checks 在 load/publication 前完成 | 未知/缺失/伪造/错 build 零 assembly/owner/API/registry/Harmony root |
| Harmony | canonical owner patch 被观察并可 owner-scoped cleanup | wrong owner、duplicate、late drift、Entry failure 可诊断且 sibling 隔离 |
| Doctor/Manager | 玩家可见 Strict/Advanced/native risk/game compatibility/restart | 不从路径、DLL、产品名或 source 推断身份 |
| Runtime game | synthetic Entry/query/patch/log/config/disabled cold start/update restart/clean exit | 无残留进程；失败 fixture 不污染 sibling 或下一次 clean restart |

静态、source、unit 与 package matrix 通过后才能运行游戏。游戏操作必须取得共享 Runtime lock，默认第三存档；运行记录进入 active smoke matrix。G2 的所有正负 fixture 都必须是 synthetic，不得安装 AutoFishing Advanced 包。

## 六、回滚和后续准入

G2 必须形成一个独立、可整体按逆序 revert 的有界提交集。回滚必须同时移除 live discriminator、Advanced SDK/reference/package合同、Core classifier branch、Harmony supervision、Doctor/Manager 投影、fixture 和测试，使旧显式/省略 CodeMod 继续走 Strict，且不改变一个 bootstrap + 四个 co-located Runtime dependencies 的玩家拓扑。

只有 G2 的完整静态、包、Doctor/Manager、游戏与 clean-restart 证据通过，才能创建新的 AutoFishing pilot Review/Update。该后续准入仍只允许 AutoFishing 一个真实产品，并必须独立完成 G3/G4 与相关 G5/G6；本 Review 不授权其他产品、Content Host G7 或 0.5.5 发布。

## Resolution

Synthetic G2 的实现、九例运行矩阵、双收据与最终门禁结果由 [20260720-0007 Update](../../../updates/2026/20260720-0007-batch6-g2-advanced-synthetic-vertical-slice.md) 统一拥有。本 Review 的准入边界不变；AutoFishing 仅在独立收口提交和提交后永久门通过后成为唯一 admitted-but-not-yet-migrated pilot。
