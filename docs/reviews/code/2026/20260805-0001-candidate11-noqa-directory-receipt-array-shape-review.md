# Candidate11 no-QA 目录回执数组形状根因审查

## 记录信息

- 日期：`2026-08-05`
- 状态：`recorded`
- 性质：最终 Candidate11 / ISSUE-011 游戏事务在进程启动前失败的代码根因审查
- Audited HEAD：`4547fd7313365a63ae0b0fa67d3b1abb6b844f6b`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 失败证据：[`CANDIDATE11/20260805-031322-cc035e27`](../../../debug/evidence/CANDIDATE11/20260805-031322-cc035e27/99-result.json)
- 失败位置：`tools/scripts/run-game-smoke.ps1:5946` 调用的 `Get-SmokeDirectoryReceipt`，实际异常发生在 helper 的 `$directories.Count`

本 Review 只冻结失败事实、同族 PowerShell 输出形状根因、被拒假设和有界修复/验收门。实现文件、提交、验证和最终 Local11 状态只由 owning Update 维护。

## 1. 已确认事实

1. clean `4547fd73` 已从第 1 行通过 canonical Release；同一提交的 Runtime + 九个源码候选由原子 builder 完整生成，再加入 exact retained Manbo/MoreEquipment，PowerShell 7 与 Windows PowerShell 5.1 preflight 均得到 `11 / 9 / 2`、`AdvancedBindings=9`。
2. Candidate11 事务在共享状态变化前通过 installed Runtime exact preflight，随后只交换 11 棵 Catalog-mapped 产品树。child smoke 在任何 `DolocTown.exe` 进程出现前退出 `1`，错误为 `The property 'Count' cannot be found on this object`。
3. 失败时三个 legacy no-QA evidence 目录 `DEBUG-CONSOLE-UI`、`ANIMAL-001`、`EQUIPMENT-SLOTS-UI` 都不存在；这正是普通 no-QA 初始状态，而不是缺少必需 QA 证据。helper 应把每个缺失目录记录为 `Existed=false / DirectoryCount=0 / FileCount=0`，供运行前后 exact comparison 使用。
4. `Get-SmokeDirectoryReceipt` 把目录和文件枚举写成 branch-local `@(...)`：

   ```powershell
   $directories = if ($exists) { @(Get-ChildItem ...) } else { @() }
   $files = if ($exists) { @(Get-ChildItem ...) } else { @() }
   ```

   PowerShell 会再次枚举外层 `if` 的 success stream；零项变成 `$null`，单项也可变成单对象，因此变量没有稳定 array contract。StrictMode 在缺失目录的普通绿色路径读取 `$directories.Count` 时立即停止。
5. 该根因与 [`20260804-0013`](20260804-0013-release-autofishing-managed-root-cleanup-array-shape-review.md) 已记录的 statement-output unrolling 同族，但发生在另一个 helper；既有 no-QA self-test 只用手工构造的 `PSCustomObject` 测 `Compare-SmokeDirectoryReceipt`，没有实际调用 `Get-SmokeDirectoryReceipt` 的缺失目录路径，所以完整 Release 未能发现。
6. Candidate11 `99-result.json` 证明 post-smoke 产品树仍匹配、11 棵原树全部 `RestoredExact=true`、候选源与 Runtime source 未变、installed Runtime pre/post 都 exact、游戏进程前后均 absent，最后释放共享锁；没有 recovery receipt、存档访问、Steam launch、游戏日志或玩家 UI 行为。

## 2. 根因与被拒绝假设

根因是 helper 自身的 PowerShell array-shape contract，而不是候选包、Runtime、产品树、玩家目录或 ISSUE-011 回执设计。

- **候选缺少 legacy QA evidence。** 拒绝；最终普通 no-QA lane 要求 QA activation/root/DLL absent，三个历史 evidence 目录允许不存在，只需证明运行前后未新增或改变。
- **Candidate11 SDK/retained/Runtime binding 失败。** 拒绝；`00`、`05`、`10`、`20`、`40`、`41`、`50`、`60`、`61` 回执均已生成并证明 exact binding、postflight 与恢复。
- **游戏启动或短时 native crash。** 拒绝；没有 `DolocTown.exe`，child 在 launch 之前的 no-QA preflight 停止，ISSUE-011 acceptance receipt 因 child 未进入运行阶段而正确缺失。
- **把 `.Count` 消费点改为 truthiness 或删除 legacy comparison。** 拒绝；zero/single/many 都应是稳定数组，且三个目录的 existence/directory/file/hash 前后比较是当前 no-QA 无 QA 活动证明的一部分。
- **仅重跑即可通过。** 拒绝；三个目录保持 absent 时失败可确定重现，重跑不会改变代码根因。

## 3. 授权的有界修复

1. 只把 `$directories` 和 `$files` 的整个 `if` 输出包入外层 `@(...)`；保留路径解析、存在性、递归 ordinary enumeration、relative path、length、SHA-256 和 exact comparison 语义。
2. 扩展既有 `Test-SmokeNoQaReceiptComparison`，实际调用 helper 读取一个确认不存在的受管测试路径，并要求 `Existed=false`、两个 count 均为 `0`、两个集合均为空；不得新建持久 `%TEMP%\DTMAPI-*` 根。
3. `test-noqa-deadline.ps1` 必须读取该真实 self-test 结果，并静态锁住两个 outer-array 赋值，防止以后恢复为 branch-local `@(...)`。
4. 不改变 Runtime/产品 DLL、候选 package、Catalog、receipt schema、no-QA 产品集合、ISSUE-011 acceptance 内容、存档模式、Steam/游戏输入或共享锁规则。

## 4. 验收边界

- PowerShell 7 与 Windows PowerShell 5.1 都通过 `test-noqa-deadline.ps1`，真实 absent helper self-test 得到 `0 / 0`，既有 ISSUE-011 fresh/repeated-input/stale-last-give 正负矩阵保持通过。
- 两宿主继续通过 `test-game-smoke-save-modes.ps1`、Candidate11 focused transaction、Catalog、test-artifact governance、文档治理和脚本 AST；`git diff --check` 无 whitespace error。
- 修复 clean commit 必须从头通过 canonical Release；随后由该 commit 重建 exact mixed candidate、安装同一 Runtime，并重新执行最小 Candidate11 slot 3 `NoNativeSave` / Local11 / ISSUE-011 游戏事务。
- 失败 run `20260805-031322-cc035e27` 只能作为安全 prelaunch/root-cause 证据，不能与后续游戏 run 拼接为 PASS，也不计作一次游戏启动。
