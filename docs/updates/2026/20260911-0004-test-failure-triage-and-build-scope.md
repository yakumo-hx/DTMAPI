# 20260911-0004: 测试失败分流与 Runtime 构建范围收缩

## Metadata

- Update ID: `20260911-0004`
- Date: `2026-09-11`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求先拆清 test 失败类别、剔除不必要内容；沿用 [执行审查](../../reviews/code/2026/20260911-0001-sdk-execution-process-audit.md)与[原实施证据](20260910-0012-sdk-msbuild-first-release.md)，不重新审查模型能力或重演历史失败。

## Summary

把测试失败和流程浪费分开处置。Runtime 单独打包只重建 Bootstrap、Catalog 可选组件及其实际依赖，取消无关 SDK、产品工程与测试工程的第二次编译；保留已提交输入校验、强制 Rebuild、构建中输入变化拒绝和正式确定性双打包。测试失败的排查顺序回到现有验证流程，不新增必读文件、运行台账或自动扩大验收范围。

### 已核实的分类

| 类别 | 本次审查样本 | 处置 |
| --- | --- | --- |
| 真实产品/兼容缺陷 | V2 最终装载分支仍限 V1；旧同名 package marker 误拒 16 个现有包；D6 字段 static/instance 指令不匹配；src 子目录工程根错误 | 已在原工程修复。保留整条装载链、原样旧包、最终 IL 字段和工程布局反例；全套 PASS 不能替代这些反例 |
| 测试随契约迁移滞后 | r2 版本投影仍按旧 schema 读取；G0/G2 引用已删除的旧 SDK 实现 | 已改为当前 schema4 行为与历史冻结来源分别核对。本次不再重修或删除冻结证据 |
| 运行前置/采集错误 | r1 外层 PowerShell 5.1 把 native stderr 警告当异常；r4 子进程误用 .NET 9；遗漏 `--focus`、旧目录不存在、探针输出碰撞 | 已有受控 .NET 8 和快速 focus 拒绝；采集边界已有修正说明。先复核同一命令/宿主/路径，再跑失败入口，不追加新的产品测试 |
| 环境故障，根因未明 | repair-r1 与仓库 tmp focused 的 Access denied，系统 Temp focused 通过 | 保留失败与前提差异；沿用 `-TestTempRoot`。没有证据归因杀毒软件，不改 ACL、不吞异常 |
| 有效快速守卫 | r3 证据保留投影过期，4.51 秒、构建前拒绝 | 保留；修正投影即可，不视为产品回归，也不优化这几秒检查 |
| 可删除的流程工作 | 初次 solution Build 之后，RuntimeOnly 包装再次 solution Rebuild；每轮重复等待/日志查询；旧 Candidate11 历史事务已退出现行全套 | 本批缩小 RuntimeOnly 构建图。等待方式与 Candidate11 路由已有修正，不重复实施；保留来源证明所需的 Runtime Rebuild |

七次记录中的 r1/r2/r3/r4/repair-r1 未完成，r5 和 repair-r2 完整通过；不是七次相同的成功测试。两次完整 PASS 分别对应 D6、D7，返修计划明确要求后一次完整验收；不能据此认定 High 擅自追加。r4/r5 外层窗口重叠，不能相加为模型耗时或断言两个测试体并行。SDK 摘要相同也不证明测试、脚本与宿主输入未变。无普通 Mod 小修样本，不把平台迁移的结论推广为已验证的小修收益。

## Changed Files

- [build.ps1](../../../tools/scripts/build.ps1)、[test-common.ps1](../../../tools/scripts/test-common.ps1)：复用已有 solution filter，实现 `-Projects` 与 `-Rebuild`；所选构建必须显式 `-SkipTests`，避免局部构建暗中进入全套。Unit/public-source 两个调用方仅同步内部函数名。
- [Workshop builder](../../../tools/scripts/build-release-workshop-packages.ps1)：RuntimeOnly 把来源证明中的同一组项目交给构建入口；其余包装模式不变。兼容宿主链接的产品源码仍正常编译及检查来源。
- [Runtime 来源 fixture](../../../tools/scripts/test-runtime-build-source.ps1)：在原夹具中验证选择范围、共享依赖只编译一次、过期输出被 Rebuild 替换、编译失败传递及临时 filter 清理；保留原来源拒绝条件。
- [验证流程](../../workflows/product-change-validation.md)、[脚本入口](../../../tools/scripts/README.md)：失败分流、范围和停止条件。

## Validation

- PASS：`test-runtime-build-source.ps1`（Windows PowerShell 5.1），包括实际 .NET 8 单根/双根构建、共享依赖一次编译、刻意损坏且时间戳更新的 DLL 被重建还原、无关工程排除、所选编译失败向上传递，以及原有已提交输入/新增删除 glob/构建中漂移/SkipBuild 拒绝。日志里的无关工程编译错误是显式负例，脚本最终 exit 0。
- PASS：`test-unit-routing.ps1` 的 11 项现有路由检查；`test-test-focus-routing.ps1 -EntryGuardsOnly` 的完整入口保护；真实 `test-unit.ps1 -Focus moresaves-product` 编译并运行通过，用于验证共享构建入口调用方，不作为新的 Mod 修复样本。
- PASS：实际两根 Runtime 开发构建，7 个工程各输出一次，0 warning/error；MSBuild 4.29 秒，含入口与工具链解析的调用 4.67 秒。旧入口选择包含 42 个 C# 工程的整个 solution；本次没有为了测速重新执行旧全图，不能据此给出总耗时或额度节省百分比。
- 已拒绝 / 未完成包验证：实际 RuntimeOnly 打包在编译前拒绝本批开始时已存在的未提交 `tools/release/dtmapi-product-catalog.json`，没有产生候选包。Catalog 是实际嵌入的构建输入，拒绝正确；本批不提交他人改动、不放宽来源门、不复制整个工作区绕行。因此尚未执行基于新打包入口的包内校验/安装器矩阵，不声明正式包装验收通过。
- PASS：文档治理、月表状态同步/只读核验、3 份受影响正文的 15 个本地文件链接及差异空白检查；相对本批开始时原件复核实际改动，未覆盖无关改动。
- 不触发：完整 SDK/全平台 Release、游戏或安装到共享目录；本批没有改变游戏行为或发行字节契约。根必读文件未增加。

## Evidence

历史失败及 PASS 的原件保持原位：`artifacts/pn041/full-release-*-result.json`、相邻日志与原 Update 0012。此处只记录分类及处置，不复制历史运行明细或重标验收结果。

本批局部验证输出在忽略目录 `tmp/test-failure-triage-20260911/`：`runtime-source.log`、`unit-routing.log`、`entry-guards.log`、`focused-unit.log`；真实构建与拒绝的打包各有日志和 `*-result.json`。最终实际结果在本页 Validation 汇总，不新增每次任务必须生成的清单。

本批排查自身有一次交互 PowerShell 命令误用空 `$PSScriptRoot`，改为已解析的脚本目录后语法检查通过；这是调用错误，未启动构建，不归为产品失败。

## Rollback Notes

修改前已保存本批涉及文件的原字节、SHA256 和工作树状态于 `tmp/test-failure-triage-20260911/before/` 及同级清单。逐段回退本批改动；不恢复整棵工作树，不覆盖并行 SDK、发布或 Wiki 改动。未移动或删除历史证据。

## Follow-Up

代码与局部验证完成。Catalog 所属改动正常提交后，在下一次已授权 Runtime 打包中验证新入口及包检查即可；当前保持 `implemented`，不伪造打包验收或自动追加全平台 Release。其他已修正的历史测试不再返工；临时目录 Access denied 的系统根因仍未核实，不声称本批已解决。
