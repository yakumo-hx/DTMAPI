# 玩家反馈与存档修复

| 类型 | 首先收集 | 执行路线 |
| --- | --- | --- |
| 操作或预期差异 | 预期、操作步骤、设置 | 解释或修正文档；确需改行为才进入产品实施 |
| Mod 缺陷 | 实际加载来源/版本、最小复现、相关日志 | 对应产品、既有 Issue 和 [产品验证](product-change-validation.md) |
| 安装环境 | 失败阶段、路径、Doctor 输出 | [安装器边界](../architecture/runtime-workshop-installer-boundary.md)对应路线 |
| 游戏本体问题 | 游戏 build、原生表现、Mod 是否介入 | 匹配原生事实和版本差异，不先归因 Mod |
| 存档损坏 | 原件、槽、已知进度和明确修复范围 | 离线分析、派生修复、允许差异验证、交付读档 |

## 先分流，再取证

默认使用 [日志采集](../../tools/scripts/collect-logs.ps1)或 PlayerDoctor 只读诊断。保持用户编号与“观察 / 推断”分离。日志已能定位的问题不索要存档；只有存档相关案件才使用[存档/崩溃采集器](../../tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1)。

`-SlotIndex 0` 只采集原生第一槽当前 `.data` 及其 `.prevN`、`.bak` 家族，同时保留原有日志采集。索引采用原生 `0–11`，不是界面 `1–12`；可传多个索引。其余 SAVE、临时档和嵌套内容不进入指定槽采集，缺当前档会标记 `Incomplete`。不传该参数时，完整 SAVE 入口及原 BAT 行为保持兼容。采集摘要记录实际范围，不能把指定槽采集写成完整 SAVE 取证。

```powershell
tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1 -SlotIndex 0
```

先关闭游戏，采集一次稳定快照。真实修档保留提交的原件，所有修复都派生到新目录。游戏保存语义由 [PROJECT](../../PROJECT.md) 持有：普通 `NoNativeSave` 功能测试不因此增加例行存档备份。玩家此后主动保存产生新哈希时，旧恢复包必须拒绝，重新采集并判断，不能只修改旧收据中的哈希。

## 已知缺信模式：Inspect → Prepare → Verify

[维护者工具](../../tools/scripts/invoke-player-save-repair.ps1)只支持 `ruinedcity-mail-v1`、原生 slot `0`、已复核的 `1.00.02` / `1.00.06` 保存格式。依据是两次[旧城补信](../archive/updates/2026/20260809-0001-ruined-city-player-save-repair.md)与[星辰神帝案件](../updates/2026/20260905-0001-xingchen-ruined-city-save-repair.md)，不是通用任务编辑器。格式模块和本地 profile 边界见[适配器说明](../../tools/scripts/player-save-repair/README.md)；工程状态由 [20260908-0003](../updates/2026/20260908-0003-player-support-and-save-repair.md) 持有。

准入要求：root 和 baseData 槽号均为 `0`；湿地主线、子任务及装饰节点已完成；正常前置信已读、未回收且已跨日；旧城前置对话访问大于零、无待处理对话；旧城任务链、子任务及装饰节点未开始或完成；未访问相应一次性迁移。邮件包括回收站和其他同任务附件在内都必须缺失。已有信返回 `NoRepairNeeded`；任务已开始、条件不完整、字段/格式不明均拒绝，不自动扩大修复。

维护者固定案件 ID、提交源 SHA-256 和已复核的本地 format profile。profile 可跨案件复用，无需重新解包。示例中的路径和 SHA 必须替换为本案离线输入；工具不输出明文或格式材料。

```powershell
$case = @{
    CaseId = 'PLAYER-CASE-ID'
    SourceFile = 'D:\Support\case\doloc-save-0.data'
    ExpectedSourceSha256 = '<本次提交原件的 SHA-256>'
    FormatProfile = '<已复核的本地 ignored profile 路径>'
}
tools/scripts/invoke-player-save-repair.ps1 -Action Inspect @case
tools/scripts/invoke-player-save-repair.ps1 -Action Prepare @case -PreparedDirectory 'D:\Support\case-derived'
tools/scripts/invoke-player-save-repair.ps1 -Action Verify @case -PreparedDirectory 'D:\Support\case-derived'
```

采集包输入用 `SupportZip` 替换 `SourceFile`，包内必须只有一个明确的 slot0 当前档。三个阶段沿用同一组参数，不能换槽或重写玩家身份。Prepare 要求目标目录全新；其中的 `doloc-save-0.data`、`candidate.zip`、`source-slot0.zip` 与 `repair-receipt.json` 都是派生证据，源快照不变。它只在邮件列表首部插入一封未读原生恢复信，使用存档当时日期，附件等待首次阅读时由原生逻辑接受任务。

Verify 重读最终 `.data` 和 `candidate.zip`，分别解密；删除唯一允许的插入段后，必须逐字节还原原始明文。它同时复核案件、源 hash、槽、profile hash、build/save version 和收据，再调用已有 [slot0 恢复包](../../tools/scripts/build-player-slot0-recovery.ps1)生成 `verified-slot0-recovery.zip` 与 `verification.json`。重复 Verify 会重查并复用未变产物。没有成功验证收据的中途输出不能当成交付。恢复包保留原件哈希、幂等和玩家继续保存后的拒绝保护；不能把明文 profile、源支持包或内部分析材料一起发送给玩家。

## 测试触发与结束条件

改格式、准入、变换或包装行为时，运行[修档离线测试](../../tools/scripts/test-player-save-repair.ps1)；改采集范围时，运行[指定槽测试](../../tools/scripts/test-player-save-collector-slots.ps1)及原采集入口测试。改恢复包装器本身才重跑其完整矩阵。已证明且输入未变的离线结果可复用，不按阶段反复跑同一套检查。

首次交付新修复模式、保存格式变化、最终派生件变化或仍有明确读档门时，才需要真实游戏验收。使用最终派生件的原槽号，在获得共享锁后按[产品验证流程](product-change-validation.md)处理已授权 fixture；不得改 index 伪装到另一槽。修档 fixture 的安装是本案明示数据变更，按 runner 的最小范围保护并恢复它实际影响的文件，普通功能测试仍用原位 `NoNativeSave`。

验收只做：成功读档 → 看到唯一恢复信 → 首次读信由原生逻辑接受旧城任务 → 检查命名的任务/UI结果和相关日志 → 退出并完成测试资产清理。全程不触发原生保存。成功加载仅证明格式和读档兼容；原生接受任务未验证时必须单独写明。新模式第一次真实验收未通过前，不把合成测试写成玩家可用证明。

这些门完成后停止。重复失败才回到匹配 Issue/Review；不重启平台审计，不扫描全库追求更多 PASS。相同已知根因复用既有分析；案件 Update 只记录本次实施、实际验收和交付。玩家继续任务或收到确认是独立事实，未收到确认不能写成“玩家已解决”。
