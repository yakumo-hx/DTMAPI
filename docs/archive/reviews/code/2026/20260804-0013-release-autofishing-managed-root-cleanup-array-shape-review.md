# Release AutoFishing 受管测试根清理数组形状根因审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded / root cause closed / bounded runner fix authorized`
- 性质：第四轮已加入的 test-artifact lifecycle 边界在正式 Release 中重复失败后的代码根因审查
- Audited HEAD：`8e27d953b88a16809cde1d9ba287985c066716f1`
- Implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 失败入口：`pwsh -NoProfile -File tools/scripts/test.ps1 -Configuration Release`
- 失败位置：`tools/scripts/test.ps1:488`

本 Review 只冻结失败事实、PowerShell 输出形状根因、拒绝方案和有界修复/验收边界。实施文件、验证与 Release 状态只由 owning Update 维护。

## 1. 已确认事实

1. `test-batch6-autofishing-behavior-matrix.ps1` 与 `test-batch6-autofishing-manager-lifecycle.ps1` 都已在专用 `release-autofishing-acceptance-*` 根内通过；其各自 `finally` 已清理子根。父根泄漏检查也已得到零项，因此失败不是 AutoFishing 产品、behavior/Manager 静态合同或子测试泄漏。
2. 父根仍是 `DTMAPI_TEST_TEMP_ROOT` 的直接子目录，唯一剩余项是 `.dtmapi-release-autofishing-test-owner`；root 本身与 descendants 均不是 reparse point。owner marker、直属父目录和 ordinary-directory 三个前置条件没有被反证。
3. 清理代码把分支结果写成：

   ```powershell
   $reparseDescendants = if ($ordinaryRoot) {
       @(Get-ChildItem ... | Where-Object { ... })
   }
   else { @() }
   ```

   PowerShell 会枚举 `if` 语句写到 success stream 的结果；分支内部的 `@(...)` 不会跨越外层赋值保留数组容器。零匹配时赋值结果因此是 `$null`，单匹配时也可能是单个对象，而不是稳定数组。
4. 正式 PowerShell 7 Release 在零 reparse descendant 的正常绿色路径读取 `$reparseDescendants.Count` 时，StrictMode 报 `The property 'Count' cannot be found on this object`。异常发生在 owner/path/reparse 判断之后、`Remove-Item` 之前，所以安全条件没有被绕过，但父根未能删除。
5. 失败根已在进程退出后按 exact path、直接受管父目录、32 位十六进制 owner marker、ordinary root、唯一普通 marker child 与零 reparse descendant 重新核对，再删除该单一根；没有触碰其他历史 `tmp/test-runs` 内容。

## 2. 根因与被拒绝假设

根因是 PowerShell statement-output unrolling，而不是文件系统状态：`@(...)` 只数组化分支内部管道，外层 `if` 仍把元素逐个输出给赋值。正确边界必须数组化整个 `if` 表达式的输出，或在所有消费点显式使用 `@($value).Count`；前者能让变量本身始终具有 array contract，更适合后续安全检查。

以下假设被证据拒绝：

- **子测试泄漏了 artifact。** 拒绝；父级 leak check 已通过，失败根只剩 owner marker。
- **检测到 junction/reparse point。** 拒绝；root 和 descendants 的 attributes 都没有 `ReparsePoint`。
- **owner marker 或直属父目录不匹配。** 拒绝；异常发生在最终布尔表达式读取 `.Count` 时，而不是进入拒绝分支；事后 exact 检查也成立。
- **放宽清理条件即可修复。** 拒绝；marker、direct child、ordinary root 与 zero-reparse 四个条件都是 destructive cleanup 的必要安全边界，不能删除或降级。
- **交给通用 stale-session cleanup。** 拒绝；该专用父根不是 `DtmApiTestSession` receipt session，通用 cleanup 正确没有把它识别成候选。生命周期 owner 仍是 `test.ps1` 的专用 `finally`。

## 3. 授权的有界修复

只允许修改 Release test runner 与其现有 test-artifact governance 静态门：

1. 将整个 `if ($ordinaryRoot) { ... } else { ... }` 的输出包在外层 `@(...)` 中，使零、单、多 reparse descendant 都保持数组形状。
2. 保留 owner marker 内容比较、直属受管父目录比较、ordinary root 检查、递归 descendant reparse 扫描与零计数条件；不得使用 truthiness 代替 exact count，也不得把异常改成无条件删除。
3. 在 `check-test-artifact-governance.ps1` 锁住外层数组化源码边界，防止以后恢复成 branch-local `@(...)`。
4. 不修改 AutoFishing QA/产品、行为矩阵、Manager lifecycle、受管会话 schema、cleanup allowlist 或 Runtime/package。

该修复是第四轮后的第三个独立功能切片；它不与 AutoFishing 原位置 no-input 预检或 Player Doctor Unicode 路径切片合并计数。

## 4. 验收边界

- PowerShell 7 与 Windows PowerShell 5.1 都能解析 `test.ps1`，并通过 `check-test-artifact-governance.ps1 -RunCleanupFixture`。
- 静态门必须同时看到专用 root prefix、owner marker、leak failure、外层 `$reparseDescendants = @(` 与最终 `.Count -eq 0`。
- 正式 PowerShell 7 `test.ps1 -Configuration Release` 必须再次运行 behavior/Manager 静态门，在零 child leak、零 reparse descendant 的普通成功路径删除专用父根并继续到后续 Release 门。
- 若 marker、direct child、ordinary root 或 reparse 检查任一失败，runner 必须继续保留根并抛出明确拒绝，不得为了通过测试删除未知路径。

在上述完整门通过前，本次失败不能记为完整 Release PASS；它也不影响此前已通过的 AutoFishing focused contract，只阻断候选 Release lifecycle 关闭。
