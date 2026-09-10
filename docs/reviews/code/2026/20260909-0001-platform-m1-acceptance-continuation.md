# M1 独立验收与连续实施边界

- Date: `2026-09-09`
- Status: `recorded`；本页记录验收反例和后续执行决定，不代表返修通过。
- Owning Update: [0001](../../../updates/2026/20260909-0001-platform-m1-acceptance-continuation.md)。
- Scope: 当前实施任务 `01a0816b-178f-72e2-a411-ceabbe7bd214` 的 PN-014/config、PN-015/004/016/017/008 与 [R1](20260908-0016-platform-m1-r1.md)。保留无关 Wiki/工作区改动，不重审其他产品。

## 验收结论

**部分接受，返修后继续；M1 产品出口仍未通过。** 配置修复、官方 Local 安装/更新/撤回的有界成果与已记录实机证据保留。PN-015 重新打开，PN-017 增加符号错误分类与负例纠正；跨档/同档重载、新游戏/加载失败、真实输入和驻留诊断的原缺口继续由原卡承担。不能把候选拒绝了错误输入笼统称作正确诊断，也不能将未执行目标命令的测试算通过。

已读取变更源码、相应测试与实际 SDK/Mono 记录；没有重新运行游戏。复用的游戏证据仍见 [M1 evidence](../../../debug/evidence/GAME-SMOKE/20260908-225439-platform-m1/README.md)，包括实际 IDE/CLI 相等、事件行号限制、原生 metadata 漂移修复、档案/sidecar 未变及现场恢复。已核验本机 SDK ZIP 与该记录的最终候选 SHA-256 一致。

## 需要修正的具体问题

### A1：符号错配报 internal，两个负例没有执行 symbols

`SymbolInspector.Execute` 为错配抛出 InvalidDataException，局部 catch 未包含它，最终由 AuthorApplication 转为 `SDK999`/exit 3。当前真实 `symbols-mismatch.json` 就是此结果。`PackBuildTests.cs` 的 missing/mismatched 两处调用把 `SDK191` 放进 `ExpectFailure` 的命令参数；helper 只有 `(label, params args)`，实际执行未知命令 `SDK191`，因 `SDK001`/exit 2 也满足“失败”而假绿。

本次公开 CLI 再现：错配为 `internal/SDK999`，缺文件为 `symbols/SDK191`，原测试参数形状为 `usage/SDK001`。修复须让预期符号错误返回 `symbols/SDK191`，保留 DLL/PDB 路径及修复指引；测试实际调用 symbols，并断言 command、预期退出类别、诊断码及配对控制组。不要用“任意非零”验证特定失败语义。沿 PN-017/0014 和原 pack-build 测试修复，不另建测试框架。

### A2：迁移合法 sourceDirectory 后工程不可构建

用历史原模板和合法 `sourceDirectory=code` 的旧工程，当前 CLI build 成功；migrate-build 返回成功，但写出的新模板硬编码 `Compile Include="src\\**\\*.cs"`，随后的 build 被 SDK180 拒绝。迁移只备份旧文件，没有在替换前验证目标工程是否仍消费原输入。

沿 PN-015/0011 让迁移按现有作者元数据投影源码路径；候选验证成功后才替换。CLI 中已经合法的输入不能被一次成功迁移变为非法。补真实旧模板、非默认源码目录、迁移前后 build/pack 与 IDE 输入一致性，以及候选验证失败时原工程未改的用例；保留冻结 props/target。

### A3：工程输入检查仍静默忽略部分项目意图

`ProjectBuildInputs` 只拒绝列出的若干元素，任意 AssemblyName/RootNamespace 值放行。公开探针加入根目录 loose.txt 的 Content/CopyToOutputDirectory，build 仍成功但该文件未进入输出；改 csproj AssemblyName 为 Different.Assembly，仍生成作者 JSON 指定的 Acceptance.BuildParity.dll，未报告冲突。不能据此称“所有不支持工程输入均有诊断”。

PN-015 的短期方案仍是一份 SDK 编译计划，不为这两个反例临时实现完整 MSBuild。统一验证允许的项目投影，未支持的资源/项目项和与权威相冲突的属性明确拒绝；已支持的输入则真正进入同一计划。覆盖正常模板、旧模板、允许的注释/用户类型，以及 Content/None/自定义项和权威冲突的实质负例；不把 XML 字节相等当作编译语义本身。

### 复现与证据

[公开 CLI 探针](../../../../temp/platform-m1-acceptance-20260909/probe.ps1)与[实际输出汇总](../../../../temp/platform-m1-acceptance-20260909/summary.json)保留在本次独立临时工作包。它只新建两个自己的作者工程并执行 new/build/symbols/migrate-build；没有写共享游戏或部署。每个 case 的完整 JSON 同目录保留。以上 A1–A3 均有当前候选的直接结果，不是仅由源码推测；缺符号、普通 Debug/Release、迁移前旧工程是成功/正确失败控制组。

## 输入与生命周期缺口如何接续

用户本次明确回答“没有试过”实体键盘。因此原任务的自动输入无响应尚不能归因为 Mod、游戏或物理键盘。先核对上一轮具体输入路径，再用现有 `tools/scripts/game-smoke/scenarios/desktop-input.ps1` 的前台检查、扫描码与保持时长能力做有界输入验证；实际游戏/日志反应才是证据，SendInput 成功返回不是。无需因此安装 QA、重建整个 runner 或默认要求用户手动完成所有测试。

可用正常 UI 的等价路径补齐真实生命周期；这只证明生命周期，不替代按键产品承诺。新游戏/受控加载失败沿已有隔离 fixture，不能因为 Escape 暂未解决就把其他独立验证全部停止。无法建立可靠输入且确需实体操作时，留下确切场景、最小动作和预期观察后再请求用户；不无限重复同一种失败注入。驻留不一致在隔离候选验证，不弱化 SDK 的冷更新保护。

## 连续执行决定

用户已将原派工“本批不进入 M2”的终点改为连续实施：修正 A1–A3 → 补 M1 → 更新 R1 → 结论支持时直接 M2 的 PN-005/009/018/019 → PN-007.a → PN-020 → R2 → PN-007.b。R1/R2 是同一 Astra 任务内的有界技术复盘，不要求用户再次回复“继续”。

R1 的阻断限定于依赖未证原生事实的公共行为和阶段产品出口。若真实输入仍有外部阻碍，可先做已定义的 PN-005 可选 ABI/owner 服务内部切片、PN-018 路径/全局 IO 与相应非游戏测试；单个切片通过不代表 M1/M2 产品通过，不提前公开 WorldReady、跨档取消保证或 available target。主卡的完整退出条件不变。

M2 完成也不是自动停点。继续领取已有明确输入、责任、关键语义、失败/恢复和验收的 ready 切片；有界原生实验按计划运行。若下一个条目只有方向，先在停止报告列明需要细化的工程包，不能自行发明长期公共承诺或无限扩张 PN-014 的连续清理范围。只有长期承诺待用户决定、必要外部条件在独立工作完成后仍阻碍，或当前细化任务已经用尽时才收口等待统一验收。

本次继续使用原实施任务、当前工作区和 Astra/high；不新开任务丢失已积累的现场知识。不授权 Workshop 上传、普通玩家存档破坏或另造治理流程。
