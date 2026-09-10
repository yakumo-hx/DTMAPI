# 20260908-0011: CLI 与 IDE 共用构建计划

## Metadata

- Update ID: `20260908-0011`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: PN-015 / AD-01、AD-02，沿已接受设计实施。

## Summary

提取内部 BuildPlan，Debug/Release 分离，新模板 IDE Build 委托公开 CLI；SDK160 按结构/真实依赖判定，保留 SDK161 与产物检查。

## Changed Files

- AuthorSdk builder、validator、模板、packager 与相关测试、作者文档。

## Validation

- PASS：`pack-build` 与 `platform-sdk-targets`；Debug/Release 分离、确定性、结构化宿主引用/间接闭包拒绝、注释/字符串/同名类型允许、真实编译错误、额外工程输入拒绝、显式迁移旧文件备份。
- PASS：最终 SDK 候选在仓库外中文/空格路径，公开 pack 与普通 `dotnet build -c Debug` 的 build input、DLL、PDB 三项相同。IDE 委托自包含 CLI；编译输出一次供 pack 使用，独立 IDE 构建只用于等价验证。

- 2026-09-09 独立验收：上述已执行控制组保留；当前候选在合法非默认源码目录迁移、未支持 Content/权威属性冲突方面存在 [A2/A3 反例](../../reviews/code/2026/20260909-0001-platform-m1-acceptance-continuation.md)。重新打开本卡，修复前不再视为完整退出。另两个符号负例误用了命令参数，沿 PN-017 的 A1 同批纠正；原 pack-build PASS 不证明这两项错误语义。

- 2026-09-09 返修通过：迁移按 author JSON 的 sourceDirectory 生成并转义 Compile glob，保留独立 RootNamespace；候选验证先于备份/替换，重复迁移按完整工程结构判定。逐项检查项目输入，Content/None/自定义复制项明确 SDK180，AssemblyName 对齐 author JSON 而非 UniqueID。冻结 props 未改。
- 新候选 `pack-build`、`platform-sdk-targets` PASS。独立进程公开 CLI 验证匹配/错配/缺符号、三类项目项的 build/pack 拒绝、程序集冲突、真实旧模板与中文 sourceDirectory 的迁移；迁移前后 input/DLL/PDB/package 相同，普通 IDE Debug 构建同字节。隔离 SDK 的损坏候选模板返回 SDK180，原 csproj、备份数量与临时文件状态不变。A2/A3 的生产返修已完成；独立验收记录仍由原审核方维护。

## Evidence

- 外部执行与等价比较：`E:/Python_project/DTMAPI-author-validation-20260908/ide-cli-equality.json`、`ide-v2-debug.log`、`journey-v2-pack.json`。
- 候选包与符号真实 Mono 证据由 [PN-017](20260908-0014-platform-author-debugging.md) / [PN-008](20260908-0015-platform-author-journey.md) 路由；没有改冻结 target。
- 返修公开复验：`temp/platform-m1-repair-20260909/probe.ps1`、`summary.json`、各命令 JSON 和 `ide-build.log`；固定 SDK ZIP SHA-256 `1381637ae034c96994a2a39313d95551127a0f3626acde83c2d7f7452bfe6b52`。专项日志 `artifacts/m1-repair-pack-build.log`、`m1-repair-targets.log`。原验收反例目录未改。

## Rollback Notes

撤回本片内部与新模板变更；冻结 props/target 保持原字节。显式迁移保留作者旧文件。

## Follow-Up

接续 PN-016/017/008 的实机缺口；已验收官方安装事务不重复。M1/R1 后按最新授权连续推进 M2，外部输入等待期间仅推进获准独立内部片。
