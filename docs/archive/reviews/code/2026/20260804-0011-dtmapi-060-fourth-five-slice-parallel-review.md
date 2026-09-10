# DTMAPI 0.6.0 第四组五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 第四组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖 AutoFishing 朝向前置、portable PowerShell 状态判定、1.00 当前存档 helper 修正、MoreSaves 迁移核心，以及 MoreSaves `1.0.1` policy/package
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查范围：`757e4190..bc1fece1`

本 Review 只保存第四轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证结果与 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. AutoFishing 正式 fixture、MoreSaves 启动时序、存档隔离、NoNativeSave 与迁移失败语义；
2. MoreSaves `1.0.1`、Release artifact contract、保护行为投影与文档治理；
3. Author SDK deploy、current/history policy、Runtime/API 双版本轴、Catalog 与发布 metadata。

并行审查去重得到 `7×P1 + 6×P2 + 1×P3`。执行有界修复时又由 Windows PowerShell 打包路径暴露一项相邻 P1，因此本周期最终记录 `8×P1 + 6×P2 + 1×P3`。审查阶段未安装 Runtime、未启动游戏、未读写玩家存档或 Steam 订阅目录，也未获取 Runtime lock。没有全局阻断；MoreEquipment Branch B 仍是既有局部范围阻断。

## 2. 发布阻断发现

### R1：Disposable save guard 安装晚于 Runtime Mod Entry（P1）

Bootstrap 先执行 `runtime.Start()`，MoreSaves 已可在 Entry 中迁移存档，之后 QA participant 的 `Start()` 才安装 disposable redirect。标为 `ArchiveMutation` 的运行因此仍可能在隔离生效前触碰 live SAVE。

有界修复：把 receipt-bound QA save guard 提升为显式 pre-Runtime participant boundary；它必须在 Core Mod discovery/Entry 前安装，失败时不得进入 `runtime.Start()`，Bootstrap 外层失败边界负责重试释放尚未由 participant 生命周期接管的 owner。

### R2：QA redirect 仍绑定已不存在的 `get_dataDirPath`（P1）

当前 build `24456188` 的 `LocalSave` 使用 `cloudDirPath`，旧 getter 无法安装。仅看 Harmony 返回成功也不能证明所有当前 archive helper 已落入 disposable root。

有界修复：绑定 exact `DolocTown.GameData.LocalSave.get_cloudDirPath`，并在 Runtime Mod loading 前从 `DolocAPI.dataPersistenceManager.fileDataHandler` 调用 current/backup/prev/temp path helper，逐项证明 index 6 指向 exact disposable SAVE；任何类型、成员、返回值或路径不匹配都 fail-closed。

### R3：NoNativeSave 未识别 MoreSaves Entry 的隐式启动迁移（P1）

旧门只在显式 MoreSaves UI exercise 时分类 ArchiveMutation。其他 profile 可以实际启用 Workshop 或 Local MoreSaves，而 18 个旧 6–11 current/prev/bak 候选又被 NoNativeSave 的现行 family 比较有意排除，于是 live legacy 文件可能被改名而门仍判绿。

有界修复：在启动前读取临时 profile 写入后的真实 `mod_infos.json`；只要 exact MoreSaves ID 已启用且 effective SAVE 中存在任一精确 legacy 候选，就强制 `ArchiveMutation + disposable fixture + StageQaHost + DirectExe + !UseSteam`，否则在启动游戏前拒绝并写 preflight receipt。

### R4：移动后损坏或异常可在下一启动被零源 no-op 绕过（P1）

旧实现已经把 source 移到 destination 后，若 post-move fingerprint 不匹配便抛错；最后一个 legacy source 因此消失，下一启动会把零源判为成功并公开 12 槽。`File.Move` 已完成后再报告异常也没有明确的可重放状态语义。

有界修复：本次新建 destination 若与预检 fingerprint 不同，安全反向移动回 legacy 名并精确验证回退；若 move 报错但 source 已消失、destination 与预检 fingerprint 完全一致，则本次激活继续失败，而下一启动可从 exact destination 推断已完成。增加 final-member exception、post-move corruption 和 second-pass 回归，不新增 journal authority。

### R5：Author SDK deploy 可接受手工降低 policy Runtime floor 的重绑包（P1）

正常 SDK build 会把 policy floor 写进 manifest，但 deploy 只检查 package floor 不高于 SDK 支持上限。攻击性修改可以把 24456188 包的 manifest 降为 `0.5.5`，同步重算 receipt/marker hashes 后发布一个旧 Runtime 必然无法加载的包。

有界修复：`ValidateReceipt` 必须将 packaged `MinimumDTMApiVersion` 与 resolved current policy registration 的 `minimumDtmApiVersion` 作数值下界比较。负例从合法 AutoFishing 包出发重算完整绑定，仍须在创建 Mod destination 和 deployment journal 前以 SDK404 拒绝。

### R6：Release contract 把所有公开产品 targetVersion 固定为 1.0.0（P1）

MoreSaves 当前 Catalog/manifest 已是 `1.0.1`，而 release checker 仍硬断言全部产品为 `1.0.0`，完整 Release 必然停止。

有界修复：使用精确闭集：MoreSaves 唯一为 `1.0.1`，其余十个公开产品保持 `1.0.0`；不得退化成无约束的 Catalog 自比较，并由 Unit 锁住一项/十项计数。

### R7：current/history policy 权威遗漏 MoreSaves（P1）

Batch 6 amendment、路线图当前投影和 rollback 仍写成只有 AutoFishing/DebugConsole 使用 current `24456188` / Runtime `0.6.0`，与已登记的 MoreSaves current/history policy 冲突；保护行为 contract 同时误称 MoreSaves 本轮玩家验收已经关闭。

有界修复：只修当前 canonical 段为 AutoFishing/DebugConsole/MoreSaves 三个 current 24456188/0.6 policy 与三个 old 23762374 history identity；其余 current policy 才保持 0.5.5。ISSUE-019 继续 open，历史 Review 当时的“两项”结论不改写。

### R8：Windows PowerShell singleton staging 被标量 `.Count` 阻断（P1）

`build-release-workshop-packages.ps1` 在 Runtime-only 输出只有一个目录时，条件表达式会把结果塌缩成标量；Windows PowerShell 严格路径随后不能可靠使用集合 `.Count`，使合法单目录 staging 在最终 exact-set 核对处失败。

有界修复：在整个条件表达式外显式数组化实际 staging directories，并用提交后的 Windows PowerShell Runtime-only 单目录包构建覆盖真实路径；不能放宽 exact directory-set 比较。

## 3. Assurance、迁移与一致性发现

### R9：不存在 SAVE 目录时不是真正 no-op（P2）

迁移在统计 legacy sources 前要求 root 已存在，新玩家或尚未建立 SAVE 目录会被错误阻断。

有界修复：路径形状仍须精确，但 root 缺失且全部 legacy 文件不存在时不得创建目录或报错；一旦进入 active migration，root 消失则必须失败。

### R10：deferred activation failure 没有统一回收 owner（P2）

manager 首次缺失后由 `Update` 或 lifecycle boundary 重试时，迁移异常不经过 Configure 的恢复路径，可能留下 native-six writer lease 或持续重试。

有界修复：所有启用路径共用一个 activation-failure 事务：清除 pending、禁用当前配置、恢复 native 6、释放精确 owner；cleanup 自身失败则保留聚合异常，不吞掉原失败。

### R11：单次 `FixArchiveIndex=false` 不能证明真实索引（P2）

当前 native 对无 index 字段的可解析内容也可能返回 false；旧反射 wrapper 还把 non-Boolean/null 返回当成安全 false，因此“无需重写”不足以证明 archive 已绑定目标 index。

有界修复：严格要求方法返回 `bool` 和有效 out string；对 target index 必须为 false，对一个 different index 必须为 true，才接受 native-recognized target identity。整个探针保持只读，任何不确定值在首个 move 前失败。

### R12：root/candidate reparse 只做一次预检（P2）

preflight 后到 fingerprint、move 与 post-state 之间仍可能替换 root 或候选节点，旧一次性检查不能覆盖这些边界。

有界修复：每次 fingerprint、实际 move 前后与 rollback 前后都重新验证 root/candidate node 类型和 reparse 状态；候选变成目录或 reparse 时 fail-closed，不扩大允许路径域。

### R13：文档把 fixture reflection 过报为真实 native binding（P2）

Unit 使用自建 `DolocAPI`/private-method fixture，focused PowerShell 只确认 exact decompile token；两者尚未执行真实玩家进程中的 current private reflection binding。

有界修复：ISSUE-019 和 owning Update 收窄为“fixture reflection shape + exact current decompile token”，把真实 binding、redirect 和官方 UI/load 证明继续留给 disposable `ArchiveMutation` player acceptance。

### R14：same-content conflict 与内容冲突统计缺少精确证明（P2）

文档称 same/different destination 均已覆盖，但 Unit 只有 differing-content 用例；实现又把仅 mtime 不同计入 differing content。

有界修复：冲突内容比较只使用 length + SHA-256，mtime 仍用于 move 的 exact pre/post fingerprint；补同字节、不同 mtime 的双路径保留测试，并确认 differing counter 不递增。

### R15：MoreSaves official 与 Author SDK tags 漂移（P3）

Author publish metadata 包含 `Functional`，official metadata 缺失；候选 `info.json` 因而与官方元数据 taxonomy 不一致，而旧 release checker 只看版本。

有界修复：给 official metadata 补 exact `Functional`，release contract 精确比较两边完整有序 tag 集；不借此整理其他产品既存 tag 债务。

## 4. 非发现与剩余边界

- AutoFishing 当前 movement/facing 切片未发现新的 P0–P2：180 ms `D` 前置、前景 `SendInput`、禁止 `PostMessage`、key-up 早于 toggle key-down仍保持。
- MoreSaves 的 ProductNative 归属、零 Harmony、官方 UI/load/save/copy/delete 回交、旧源/冲突不覆盖不删除和不新增 journal/receipt/SharedNative/API 的边界不变。
- current/history policy hash、唯一 ID、0.5.5 API compile target 与 0.6 executable floor 双轴未发现新的身份漂移。
- 仍真实开放：MoreSaves disposable player acceptance、AutoFishing 第五档行为矩阵、MoreEquipment Branch B 范围决定、ChestLocator 人工确认、Manbo 普通激活、ISSUE-011 clean smoke、Steam 实物 identity、最终集成与完整 Release。
- 本轮不授权 live-save 测试、一般化 Advanced authoring、Content Host G7、新公共 API、Compatibility breaking 或产品范围扩张。

## 5. 关闭条件

R1–R15 完成上述有界修复并通过对应 Release build、MoreSaves/QA/full Unit、Author SDK exact-reference、双 PowerShell save-mode/source/release-set/语法、提交后 singleton Runtime package、Catalog 与文档治理后，本审核周期关闭。Review 保持 `recorded`；commit、验证、player/release evidence 和剩余风险只追加到 owning Update。
