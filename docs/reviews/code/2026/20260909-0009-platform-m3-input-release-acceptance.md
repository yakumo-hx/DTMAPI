# M3、配置输入与首发准备独立验收

- Date: 2026-09-09
- Status: accepted with required corrections before release candidate
- Scope: PN-036、PN-011/010/022/023、PN-033.a、PN-037.a/b、PN-031.a 当前源与固定证据；本次没有启动游戏、安装 Runtime 或生成发行包。
- Resolution owner: [本次修正与接续 Update](../../../updates/2026/20260909-0020-platform-m3-acceptance-continuation.md)；产品返修继续 PN-037.b / PN-031.a 原 Update。

## 接收范围

接收内部 M3 依赖/共享 CLR 类型、自助 Advanced、多工程与资源、锁定 NuGet 重放及弃用准备的有界实现。R3.shared/native 的架构选择继续成立；无需重做平台分层。SDK 0.6.5 的完整依赖解析/更新锁仍需作者显式使用 .NET SDK，工具自身只重放锁；这项限制已在随包说明中如实写明。

接收 PN-037.a 的代码与无设备/键盘出口，接收 PN-037.b **配置打开之后**的已证键盘操作；不接收“从原生菜单到配置的全程方向导航已完成”。M3 的完整公开 target、PN-031.a 真实包/升级/长期组合门仍未完成，因此本次不是 0.7.0 发行验收 PASS。实体手柄、Steam Input 和热插拔继续 pending-player。

## 必须修正的问题

### A1 / P2：方向导航缺少原生标题入口

`ReflectedTitleMenuSettingsUi.cs` 的 CreateTitleButton 在独立 Canvas 上创建图标，明确关闭 trackingRenderedObjects；`Navigation.cs` 的 TickNavigation 在配置未打开时直接返回，TrackNavigation 也不登记静态入口。当前入口只有鼠标和 ProcessEntryAction 的 F8/F9 绑定。现有记录的进入操作正是 F8/F9，不能代替 E07.input 的原生菜单可达性。

只读核对匹配 Steam build 25163613 / Assembly-CSharp `60489873c645886c5a523fd0d17c4d451a7d68df133f552c6667501245110ac6` 的 native 事实：HomePageUiState 持有 buttonActions 并由 RenderTextMenu 生成菜单；HomePage.BuildNavigation 只连接 textMenu / linkButtons / languageButtons；DolocGridUI 使用 Explicit 导航。当前平台没有扩展这些入口。由这些源码可确定尚无已实现的方向入口连接；本次没有实机重现“按键无效”，不能把推断写成新的玩家观察。

**决定：** GameBridge 在既有标题菜单动作列表中增加一个可归属的“Mod 配置”动作，复用原生布局/导航/确认；Bootstrap 只提供文字与打开回调。所有游戏类型、私有字段及 Hook 留内部，不发布通用菜单扩展 API。按精确对象身份去重和撤回，不重写原生列表、不改其他 Mod 回调；标题刷新/语言变化可重建，关闭平台模态后回到原生选中项。原图标及用户接受的 open-only 快捷键继续作为额外入口，游戏内配置仍不支持。具体实施与无快捷键验收见 execution-next 的收口步骤。

### A2 / P1：发行构建的 committed-source 检查遗漏真实输入

`build-release-workshop-packages.ps1:550` 的 runtimeSourcePaths 只含 Directory.Build.props、五个 Runtime 项目目录与 Catalog 中 optional component 项目目录。Core 实际还编译 `src/Shared` 的依赖/原生契约文件，嵌入 `author-sdk/target-catalog.json`、BCL 身份表、Advanced policies 和 Product Catalog；Directory.Build.props 导入 `tools/release/dtmapi-runtime-version.props`。可选 Compatibility 还链接项目外产品源码。这些路径没有被该 Git 检查覆盖。

因此“已提交五个项目目录”不能证明 BuildCommit 包含最终 DLL 的全部输入。当前工作区仍会因其他未提交源码被阻止，本次**没有**声称已经绕过整个脚本、产出错误包，或发现已发布 0.6.1 存在字节问题。这是首发前必须补齐的来源边界，不能仅在操作说明里要求维护者记住额外提交。

**决定：** 沿现有 builder/release-common 取得真实项目的 Compile、EmbeddedResource、ProjectReference 与仓库内构建导入闭包；检查新增/修改/删除及链接输入，排除生成的 obj/bin 和仓库外受锁依赖。现有版本/包 receipt 继续使用，不引入另一套收据或要求全仓干净。发布构建和 `-SkipBuild` 都须能证明输出来自该已提交输入，旧输出不可因工作树干净而冒充新构建。用隔离 Git fixture 演练只有 Shared、版本、embedded JSON 或外链产品源变脏时的拒绝，以及无关文档变脏仍允许。实际 Runtime/SDK 组合和安装矩阵由 PN-031.a 再证明。

### A3 / P2：当前 frozen target 检查被过时版本枚举中止

独立运行 `tools/scripts/test-author-sdk-compatibility.ps1 -ApiTarget 0.6.4 -NoProvision`：前面的冻结重建/摘要校验完成，随后第 62 行抛出 `Missing explicit frozen source count for target.`，退出 1。该测试仅硬编码了 0.5.5、0.6.2、0.6.3 的源码文件数，未支持当前 0.6.4；不是已发现 frozen DLL 字节不一致。

**决定：** 删除逐版本文件数断言，让已有 contract 摘要绑定的 source manifest 拥有输入集合；保留精确 DLL/版本、缓存篡改、源码篡改及未列入文件拒绝测试。不得为通过检查改写 0.6.4 冻结源、hash 或 source-build.json。当前修正和复跑结果由本次 Update 记录；公开 0.7.0 同样须实际执行这一路径。

## 独立验证与证据范围

本次重新构建选中测试项目，全部使用 tracked scripts / Get-DotNetExe 的 .NET 8 host：Core `platform-package-dependencies`；AuthorSdk `platform-project-graph`（含 locked restore）、`platform-package-dependencies`、`platform-native-contract`；RuntimeIntegration `platform-controller`。以上均 PASS。检查包括真正 Runtime shared Type/失败关闭/驻留冲突的 .NET fixture 与 SDK 包/Doctor，不只是源码结构；它们依然不能替代 Mono 和最终发行包证明。

复算 `artifacts/pn022/sdk-final/DTMAPI-Author-SDK-0.6.5-win-x64.zip`，与 PN-022 记录一致。重读 [最终输入证据](../../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)、对应 runner/result 与恢复记录：H 的 1→1→2 正对照成立；单次 Tab、改 F8 为 F9、取消/保存/冷启/关闭重开有用户确认；200357 是标题人工验收通过、计划读档未执行的 incomplete runner，未混成新的长测 PASS。恢复记录为 30 archive + 5 sidecar 未改、Runtime/选择/测试配置恢复；本次复用这些固定字节的事实。

Windows Runtime 包为本次选定的 audit lane，按现行 installer boundary / matrix 核对源流程。Catalog/subscription 仍记录已发布 0.6.1，releaseStop 保持 ActiveNoUploadAuthorization。未生成最终包，故 Windows installer、真实 0.6.1 升级恢复、多平台新候选注入和一小时 ISSUE-010 均未在本次执行。短 smoke、人工标题操作和 focused PASS 不能拼成完整 Release PASS。

## 接续决定

原实施任务在当前工作区连续完成 A1、A2 → 受影响内部组合复核 → PN-023 综合 R3 与新 0.7.0 target → PN-031.a 最终包、实际全量 Release、安装升级恢复和固定组合一小时门。具体规格、顺序和停止条件写入 [execution-next](../../../planning/platform-next/execution-next.md)，状态只由 [status](../../../planning/platform-next/status.md)路由。

不把已有 backend 能力全部打回，也不把原生入口漏项转移到“等手柄反馈”。先完成已经详细规划的收口，再统一验收；下一次架构复盘进入 M4 的 R4a 保存身份/提交窗口与 R4b 内容传播实验，不在本次顺手公开 SaveData、任意 GameContent 或通用菜单 API。
