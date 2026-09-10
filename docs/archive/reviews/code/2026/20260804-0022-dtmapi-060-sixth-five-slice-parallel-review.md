# DTMAPI 0.6.0 第六组五切片并行代码审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：0.6.0 第六组五个独立功能切片后的并行代码审查
- Source：用户要求每完成五个独立功能切片执行一次并行子智能体审核；本轮覆盖发布实物身份、九家族真实消费者与 removal 决策、Candidate11 九源码/两 retained 事务、独立 Workshop reference fixture 接线，以及 exact mixed Local11 marker/digest 预检
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 审查范围：`5aee83d0..8f327a2b`

本 Review 只保存第六轮并行审查发现、根因和有界关闭条件。修复生命周期、changed files、验证结果与 0.6 发布状态仍由 owning Update 维护；本轮发现及其有界修复属于同一审核周期，不计入下一组五个功能切片。

## 1. 审查范围与方法

三名独立审查者分别检查：

1. Author SDK identity、九个 current source package 的 binding receipt 与 mixed Candidate11 preflight；
2. Candidate11 Runtime/MODS 事务顺序、失败恢复、锁语义与最终 no-QA 游戏门；
3. standalone Workshop builder、发布候选目录生命周期、Review/Update 文档治理与双宿主兼容。

并行只读审查去重得到 `4×P1 + 3×P2`，没有 P0/P3 或全局阻断。当前真实 Local11 的九个 SDK 包另经只读抽查，其 marker、manifest、entry DLL 与 Advanced receipt 字节彼此一致；发现针对的是门不足，不能反推当前包已损坏。审查阶段未安装 Runtime、启动游戏、读写玩家存档或 Steam subscription，也未获取 Runtime lock。MoreEquipment Branch B 保持既有局部决策阻断，ChestLocator 保持人工确认门，AutoFishing GC 保持用户决定的非阻断。

## 2. 发布阻断发现

### R1：九个 current source package 只检查 marker 标签，没有重算 SDK payload binding（P1）

`candidate11-source-transaction.ps1` 只要求 schema 2、`CodeMod / Advanced` 和 authority 字符串，尚未精确检查 marker 属性集合、Author SDK `0.1.0`、target DTMAPI `0.5.5`、manifest `Type/CodeModKind`、Advanced receipt 的 policy/unique ID/Harmony owner/path，也没有重算 marker 中的 `manifestSha256`、`entryDllSha256` 和 `advancedReferenceReceiptSha256`。synthetic fixture 同样省略这些字段，因而不能反证 package marker 与 payload 分离篡改。

有界修复：复用 `DTMAPI.AuthorSdk.DeploymentPackage` 与 Catalog 已有 authority 的等价验证语义，不创建第二套 receipt/schema。Candidate11 在任何 staging 前要求 marker 精确属性集合，重算三个 SHA-256，验证 manifest/entry/receipt 路径、长度、identity、framework、policy 与 Harmony owner，并增加 missing/tampered/wrong target/wrong kind 负例；retained 两包仍只走 exact frozen tree 与 legacy marker 边界。

### R2：installed Runtime exact binding 在 11 个 MODS 被交换并完成 smoke 后才检查（P1）

事务当前先取得锁、移动玩家 MODS、安装 11 个候选并启动 child smoke，结束后才调用 `Get-Candidate11InstalledRuntimeBinding`。inner no-QA preflight 只检查五个文件名与 QA absence，故错误的初始 Runtime 可以先运行候选产品，违背 exact-candidate fail-before-mutation 边界。

有界修复：取得共享锁并确认无游戏进程后，立即核对 installed Runtime assembly set 与 manifest/state binding；只有精确匹配才允许触碰 MODS 或启动 child。保留 postflight drift 检查，并增加错误初始 Runtime 时 child 未调用、MODS 原树不变的负例。

### R3：游戏仍存活或恢复不完整时仍无条件释放共享 Runtime lock（P1）

Candidate11 已会在 `DolocTown.exe` 存活时拒绝恢复原 MODS，但 `finally` 仍调用通用 release 脚本。这样会把共享环境暴露给下一事务，同时原树仍在事务暂存区，且没有 durable recovery receipt 约束谁能继续恢复。

有界修复：只有游戏进程不存在、候选目录撤回、原 MODS/Runtime exact 恢复并经回执验证后才释放锁。否则保留锁并写入有 owner token、原/暂存路径和期望摘要的 durable recovery receipt；提供只允许同一 owner、进程已退出且摘要未漂移时执行的有界 recovery 入口。负例必须证明 live process 后锁保留、下一事务被阻断，恢复成功后才释放。

### R4：普通 no-QA Local11 退出 `0` 不能单独关闭 ISSUE-011（P1）

路线图要求 title settings、Y 搜索/给予/last-give、Input System repeated-error、fresh collector dump 或 missing reason、Steam/process clean exit。当前 no-QA receipt 只机械要求 Y 的 open/close/tap/hold，禁止原有 QA title/mouse-give 参数；collector 结果多为记录字段而非失败条件。即使 Candidate11 child 退出 `0`，也不能证明 ISSUE-011 的完整当前候选门。

有界修复：建立由当前 game evidence、运行日志、last-give breadcrumb、collector/fatal 结果与 clean exit 组成的单一 ISSUE-011 acceptance receipt；title settings 与 Y 搜索/给予可由正常游戏 UI 人工或 computer-use 驱动，但 Candidate11 必须机械验证回执的 exact evidence root、current candidate binding、slot 3、`NoNativeSave`、fresh timestamps 和全部必需结论。缺项、旧回执或仅 child exit code 均失败。历史 crash class 不因当前候选 PASS 被抹去。

## 3. 事务与治理发现

### R5：standalone builder 直接写最终 OutputRoot，失败会留下候选外观的半成品（P2）

builder 开始时清空并直接写最终目录；ActionSpeed `SDK202` 失败因此留下 `dist/dtmapi-060-candidate-d85bd07e`，再次执行还可覆盖此前有效候选。

有界修复：在 OutputRoot 同卷、同父目录的随机普通 staging sibling 完成全部 Runtime/九产品构建与 exact set/Catalog 校验，成功后再以备份交换事务发布；失败保持原 OutputRoot 字节不变并清理 staging。发布和回滚均须拒绝 reparse/path escape。既有半成品必须安全移出候选命名空间或删除，不能被任何发布入口接受。

### R6：Review `0020`/`0021` 错误拥有 implementation/PASS 生命周期（P2）

两份根因 Review 使用 `implemented`/`verified` 状态并承载提交、changed files 与验证叙述，违反 Review 只允许 `draft / recorded / superseded`、Update 唯一拥有实施生命周期的治理规则。

有界修复：两份 Review 改为 `recorded`，保留事实、被拒路径、根因和验收门，只添加指向 owning Update 的解决路由；实现、验证与最终状态全部留在路线图。

### R7：Candidate11 outer contract 未在取得锁前要求唯一的 slot 3 / NoNativeSave（P2）

inner runner 后续会拒绝错误值，但 outer transaction 在锁和产品交换前没有确认 `-SaveSlot 3 -SaveTestMode NoNativeSave` 的 exact、唯一参数，错误或重复参数会在共享状态已经改变后才失败。

有界修复：解析 child 参数并在锁前要求 `SaveSlot` 恰为 `3`、`SaveTestMode` 恰为 `NoNativeSave`，两者各出现一次且不允许替代 spelling/重复覆盖。增加缺失、错误、重复和 alternate mode 负例，证明 child 未调用且共享状态不变。

## 4. 已核对的非问题与剩余边界

- 九家族真实 retained 消费者仍支持 `KEEP / breaking removal NO-GO`；本轮没有发现可安全删除的 Compatibility Host、public API/DTO 或 mandatory lifecycle wiring。
- retained MoreEquipment/Manbo 的 exact tree、九/二分类、OrdinalIgnoreCase retained digest 与 Ordinal Runtime assembly-set digest 方向正确；固定跨宿主向量继续保留。
- Advanced reference fixture 的按 policy 构造与 `-GameDir` 接线正确；R5 只涉及最终目录 publication lifecycle。
- Review/Update 纠偏不改变历史构建或 preflight 字节事实，只修正事实所有权。
- 本 Review 不授权 MoreEquipment 1.0、一般 Advanced authoring、Content Host G7、新公共 API、玩家存档写入或用 QA fixture 替代最终普通 UI 验收。

## 5. 关闭条件

R1--R7 的有界修复必须通过 PowerShell 7 与 Windows PowerShell 5.1 focused positives/negatives、Catalog/test-artifact/document governance 和 `git diff --check`。完成后在 clean commit 上重建并 preflight exact mixed candidate；只有随后安装同一 exact Runtime 并取得 slot 3 `NoNativeSave` Local11 与 ISSUE-011 acceptance receipt，才能关闭当前候选游戏门。Review 始终保持 `recorded`；全部实施、验证、候选摘要和剩余风险只追加到 owning Update。
