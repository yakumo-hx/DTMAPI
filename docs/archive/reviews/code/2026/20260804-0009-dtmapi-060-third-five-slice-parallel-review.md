# DTMAPI 0.6.0 第三组五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 第三组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖 all-Advanced exact-reference 路由、产品/Runtime 最低版本解耦、Release 诊断尾段、MoreEquipmentSlots 重叠决策门、AutoFishing 1.00 决策与实现
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查基线：`6726bc97`（`fix(autofishing): adapt product to current game`）

本 Review 只保存第三轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证结果与 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. AutoFishing current native movement、`AF-D2`、第五存档 fixture、NoNativeSave 与 smoke provenance；
2. 默认 Release 产品集合、MoreEquipment Branch B、文档治理、临时工件与路线图元数据；
3. Author SDK/marker、current/history reference policy、0.5.5 Runtime 双向兼容与 Batch 6 投影。

两路审查独立发现了同一项 Batch 6 policy 漂移；去重后共有 `6×P1 + 4×P2 + 1×P3`。本轮只读审查未安装 Runtime、未启动游戏、未写玩家订阅目录或存档，也未获取 Runtime lock。没有全局阻断；MoreEquipment Branch B 仍是已知局部范围阻断。

## 2. 发布阻断发现

### R1：两个 current 24456188 policy 产品错误声明 0.5.5 Runtime floor（P1）

AutoFishing 与 DebugConsole 的 current package 绑定只存在于 0.6 Runtime 的 `24456188` reference policy，但 manifest/author/Catalog 仍声明 `MinimumDTMApiVersion=0.5.5`。真实 0.5.5 Core 在 assembly load 前因 embedded registry 不认识这些 policy 而拒绝；公共 API 仍可绑定 0.5.5 并不能证明旧 Runtime 能识别新 package identity。

有界修复：保持冻结 Author SDK 的公共 API 编译 target `0.5.5`，正式解耦 policy-specific executable Runtime floor；只将 AutoFishing/DebugConsole current policy 与生成 manifest 的最低 Runtime 提升到 `0.6.0`，其余产品不批量提升。旧 0.5.5 Runtime 必须在 load 前拒绝新包，0.6 Runtime 必须接受。

### R2：0.6 Core/Doctor 会拒绝已发布的旧 AutoFishing/DebugConsole 包（P1）

两个已发布 0.5.5 包使用旧 `23762374` exact policy；这些 policy 已移入 history，但 Core/Doctor 只嵌入 current registry，且现有唯一 UniqueID 约束不允许把旧行直接塞回 Author SDK current registry。因此路线图承诺的旧 receipt + Drift/真实激活无法发生。

有界修复：将 Author SDK 的唯一 current authoring policy 与 Runtime/Doctor 的历史 exact acceptance 分开。旧 policy ID/hash 只供 Core/Doctor 严格接受历史包，不重新开放旧 authoring；真实旧 policy 被接受，伪造 hash 继续 fail-closed。

### R3：AutoFishing movement 漏掉 native 的精确非零边界并存在启用盲窗（P1）

当前 native Wait 在阈值检查后仍以 `VelocityX != 0` 判定 post-base movement；产品只检查 `abs(VelocityX)>0.001`，会让 tiny nonzero movement 触发原生收竿而 F6 仍保持。启用后固定一秒跳过 movement 检查，同时产品动作已经允许执行，也形成真实盲窗。

有界修复：有限值下任意 `velocityX != 0d` 都取消；加入正负 tiny-nonzero 反回归。固定时窗改成 neutral-arming：两个 native movement 信号归零前不执行产品动作，归零后立即武装取消。

### R4：NoNativeSave 未观察 1.00 的真实 archive backup family（P1）

runner 仍只快照旧 `-prev.data/-bak.data`，而当前 `LocalSave` 使用 `<current>.prev0…N` 与 `<current>.bak`，并先轮换再 replace。current 文件不变时，真实 prev 集仍可能已变化，现有门会误绿。

有界修复：按 current archive basename 比较 `.prev[0-9]+`/`.bak` 的完整前后集合与 length/hash/mtime，检测新增、删除和改写；保留 current 检查，并增加旧命名不能充当 current 证明的负例。该修复前不得运行第五档 NoNativeSave acceptance。

### R5：InstantBite smoke 缺少产品事务 provenance（P1）

fixture 以 `BiteReadyObserved` 发布 InstantBite verified，未要求产品事务实际提交；自然 native bite 也可进入同一状态。现有 native-prepared 计数还会把调用前已经 native committed 的缓存命中计为产品成功，并跨 session 累积。

有界修复：事务必须返回并记录 `OrderedCommit`、`ReconciledAfterWriteFault`、`AlreadyNativeCommitted` 等明确 provenance；只有前两类递增专用 InstantBite commit 计数。fixture 保存场景 baseline，并要求正增量才发布 verified 和完成 Instant 场景。

### R6：默认 Release 构建集合越过产品发布范围（P1）

默认 all-Advanced driver 以 `CodeModKind=Advanced` 选择全部产品，因而会构建 Branch B 明确禁止生成 1.0 package 的 MoreEquipmentSlots，以及本轮明确排除的非公开 StrongPlantingGun、Mine；release checker 也把通用 authoring 集误当本次发布 artifact 集。

有界修复：由 owning Update/Catalog 现有字段导出本次 release-contract exact artifact set：九个公开 ProductNative 更新进入重建集合，MoreEquipment 只走 retained `0.3.1-dtmapi` ABI/Compatibility，Manbo 走普通 retained 激活；三个被排除产品必须有“绝不调用 builder”负断言。不能仅按当前统一为 `RebuildBlocked` 的 `releaseEligibility` 筛选，也不新增产品准入或 receipt 家族。

## 3. Assurance、治理与一致性发现

### R7：AutoFishing movement smoke 不能证明逐 phase 与改键行为（P2）

fixture 只观察 session 消失，不核验 movement release reason，也未记录输入发生在哪个 phase；runner 固定发送 `A` 并主动等待越过旧盲窗。该证据不能支持 Ready/Cast/Wait/BiteReady/minigame 或实际改键路径的声明。

有界修复：为每个目标 phase 建立显式状态机，输入前记录 phase，结束后要求精确 manual/native movement release reason；每个目标独立出具 receipt，并覆盖实际改键路径。

### R8：Batch 6 当前 policy 权威仍写成所有产品都绑定 23762374（P2）

架构表把 G2 历史 proof 与 current product admission 混为一体，已与 AutoFishing/DebugConsole current `24456188` policy 不符。该漂移被版本审查与治理审查独立发现。

有界修复：加入 0.6 amendment，区分 G2/旧包历史 23762374、两个 current 24456188 authoring policy、Runtime/Doctor 历史 acceptance 与尚未完成的 player/release authority；不改写历史验收。

### R9：两份 Review 吸收了 implementation lifecycle 与 PASS 结果（P2）

Author SDK exact-reference Review 和版本历史 Review 后追加了实现链路、包结果与最终验证，重复了 owning Update 的职责；MoreEquipment Review 的未来处分边界也不够明确。

有界修复：Review 只保留根因、决策输入和关闭条件；实施、changed files、验证与发布状态回到 owning Update。MoreEquipment 后续只增加 resolution link，不在 Review 复制处分生命周期。

### R10：两个 AutoFishing 默认 Release 测试泄漏独立 GUID 临时根（P2）

behavior/Manager runner 未接入受管测试根，成功路径仍在系统 temp 留下 GUID 树；现有 test-artifact governance 没有覆盖该泄漏模式。

有界修复：顶层 Release 分配专用受管根并在 `finally` 清理，两个子脚本只在该根工作；治理门增加成功泄漏反回归。只删除本轮精确已知路径，不清空整个历史 temp 根。

### R11：路线图 Issue 元数据和 changed-file inventory 不完整（P3）

路线图把 ISSUE-011 明确设为发布门，但 Metadata/月份台账仍写 `none`；Changed Files 也漏记 MoreEquipment Branch B Review。

有界修复：Related Issue State 与月度台账同步为 `open`，Changed Files 纳入 MoreEquipment Review 和本并行 Review。

## 4. 非发现与剩余边界

- AutoFishing `AF-D2` 的 ordered write、after-write reconcile、post-commit diagnostics、fault-close owner cleanup 和 22-Hook 原子安装未发现新增实现缺陷。
- all-Advanced exact-reference fixture 仍严格验证安全 policy ID、唯一 reverse Assembly、Harmony exact entry、`copyLocal=false` 和临时输出边界；问题是本轮 release set 选择，不是 exact build 语义。
- 当前 registry ordinal、唯一 policy ID、Catalog/author binding、`netstandard2.0` 与无 bundled native DLL 边界未发现新漂移。
- 仍真实开放：MoreEquipment 范围决定、AutoFishing 第五档玩家验收、ChestLocator 人工确认、Manbo 普通激活、ISSUE-011 clean smoke、九项 Steam 实物 identity、最终集成/完整 Release 与 public 条件核验。
- 本轮不授权新公共 API、general Advanced authoring、Content Host G7、Compatibility breaking、产品范围扩张或游戏启动。

## 5. 关闭条件

R1–R11 完成上述有界修复并通过对应 Core/Doctor/Author SDK/Unit、旧新 Runtime compatibility、AutoFishing source/fixture/NoNativeSave、release exact-set、PowerShell 5.1、Catalog、文档与制品治理后，本审核周期关闭。Review 保持 `recorded`；commit、完整验证、player/release 证据和剩余风险只追加到 owning Update。
