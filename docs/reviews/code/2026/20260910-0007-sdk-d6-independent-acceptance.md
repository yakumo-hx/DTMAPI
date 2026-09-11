# 20260910-0007: 0.7.0 SDK D6 独立验收

- Scope: 当前工作树、提交 `cc7044a7` / `1002ae05` 的相关来源，准确 SDK D6、Windows Runtime r6、多平台 r2。
- Request: 用户要求验收 0.7.0；有问题写清步骤交回同一任务完成。
- Disposition: 原D6不放行结论保留；**2026-09-11实施方已完成PN-041返修，准确D7交回复核与发布决定**，详见文末。标准 MSBuild 主线成立；本轮不改变架构方向。Runtime / 现有 Mod 兼容与安装器的有效证据保留，没有发现需要重做玩家载荷的新增问题。
- Ownership: 实施及收口仍归 [Update 0012](../../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)；连续执行步骤归 [PN-041 独立验收返修](../../../planning/platform-next/execution-sdk-msbuild.md#独立验收返修)。本 Review 记录判断，不建立新的任务或凭证体系。
- Resolution（2026-09-11）：**D7独立复核通过，R01–R03关闭；D6的不放行结论及失败证据保留。** 准确包与原反例/布局复测、有效IDE/CI/完整Release/Mono证据见[独立复核](../../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/independent-acceptance.md)。0.7.0按既定Windows Developer Preview范围技术接受，未公开发布。

## 已验证范围

准确输入、外部探针源码、命令与原始结果在 `artifacts/pn041/independent-acceptance/`；新作者工程位于仓库外 `E:/Python_project/DTMAPI-sdk-probes/20260910-independent-acceptance`。使用 D6 的既有解压目录前，核对其 inventory 与 D6 相同，并逐项检查文件长度和 SHA256。没有调用旧 SDK 或仓库内编译器冒充交付工具。

| 范围 | 本次结果与边界 |
| --- | --- |
| 准确 SDK ZIP | `check-author-sdk-release.ps1 -PackagePath artifacts/pn041/sdk-msbuild-distribution-d6/DTMAPI-Author-SDK-0.7.0-win-x64.zip` PASS；包含完整包、6177 文件和确定性 ZIP 检查。记录 `sdk-release-check.log`、`probe-sdk-inventory.json` |
| 外部作者常规路径 | 空的独立 NuGet 缓存、随包工具链和离线源；new、restore、Debug build、Release pack＋PDB、Doctor 均成功。`NETSTANDARD2_0` 生效；EditorConfig 的 CS0168=error 按预期失败；普通 C#10 辅助库及合法委托恢复、打包成功。记录 `probe-summary.json` 及各命令 JSON |
| Windows 安装器 | 对准确 r6 重新执行 `test-runtime-workshop-installer-061.ps1 -PackageRoot artifacts/pn041/runtime-070-candidate-r6/DTMAPI` PASS。脚本名称中的061是既有入口名称，实际输入为0.7.0；包含假游戏和复杂路径等脚本自有矩阵。记录 `windows-installer.log` |
| 多平台结构 | 对准确 r2，以 r6 为 AcceptedPackageRoot、SourceKind=Candidate 执行现行 package audit PASS；Windows PE、Linux ELF、四个 shell 入口、PowerShell5.1/Bash语法及同一 Runtime 载荷成立。记录 `multiplatform-structure.json/.log`。安装升级/撤回的 Windows/WSL 结果复用0012，包和宿主 hash 未变；本轮未重复执行该生命周期矩阵 |
| Runtime / 原 Mod | 核对0012的准确0.6.1 surface差分、r6原包/Steam/owner关闭及恢复日志；除已授权的20项CSV删除外额外删除0，历史marker退化已修正。原始日志有新旧作者包、V1/V2/receipt、实际内容和正常退出观察。未由这些样本推出所有未知Mod绝对兼容 |
| IDE / Mono / 完整 Release | 核对D6实际VS Code/C# Dev Kit截图、CLI/CI对比、准确作者ZIP与r6输入对应，以及完整Release的真实exit0。相关输入未变，复用其具名范围；本轮没有启动游戏或重新跑完整Release，下面的新反例使原测试覆盖不足以放行SDK |

本轮只修改审查/计划/状态和隔离探针，没有修改生产代码，没有触碰共享游戏、官方MODS、live upload或玩家存档。SDK仍是独立开发工具ZIP，未并入玩家安装包。实体手柄、非Windows游戏注入、shipping Mono断点/locals的未证实范围不晋级。

## R01 — 最终运行字段与调用指令不匹配仍能打包（P2，B05/B07）

`src/DTMAPI.AuthorSdk/ManagedRuntimeSurface.cs:37` 校验实际 MemberRef，且第48行检查方法的静态/实例匹配；字段只有解析及部分可访问性检查，没有核对实际字段指令要求。

独立构造同身份、同版本的两个合法 netstandard2.0 库：编译引用的 `Birch.FieldApi.Value.Number` 为 `public static int`，实际交付库中为 `public int`。主Mod源码读取该字段；标准 AfterBuild 选择后者的准确 DLL/PDB，pack 捕获最终产物。结果：

- `field-final-pair-pack.json`：pack exit0，成功交付该依赖；`field-final-pair-doctor.json`：Doctor exit0、没有错误。
- `field-surface-result.json`：主Mod实际指令为 `ldsfld ...Value::Number`，交付库该字段 `IsStatic=false`。
- 从该准确ZIP解压后，用 Get-DotNetExe 选择的受控.NET8宿主调用实际Mod方法，`field-runtime.json` 记录 `System.InvalidProgramException`，host8.0.27、exit2。这是.NET实验，不冒称实测Mono。

第一版探针只替换DLL时被既有PDB配对检查正确拒绝，保留在 `field-pack.json`；补齐对应PDB后上述缺口仍存在。不是把错误符号、源码推测或普通委托当成成员兼容缺陷。

该首个失败报告还把实际0.7.0工程的targetRuntimeVersion写为默认0.5.5。返修时一并让失败诊断保留已解析的真实API target，不另建任务。

**决定：** 在现有SDK最终程序集检查中按实际IL字段访问补上静态/实例约束；只检查被使用的运行成员，不要求两个DLL全API一致。保留合法不同ref/lib、泛型/继承及标准后处理。Doctor继续承担现有只读包/元数据检查，不在本次新增完整IL验证器，也不得把Doctor成功表述为可运行的充分条件。当前问题由pack的已承诺门承担，不因此改公共包格式或Runtime加载契约。

## R02 — 作者根与 csproj 根混用（P2，B01/B04/B07）

schema4 的 `projectFile` 接受并描述为相对路径。把模板csproj放到 `src/Birch.Acceptance.csproj`、manifest及author JSON仍留在作者根，仅修改projectFile：`subdirectory-validate.json` PASS，`subdirectory-build.json` 则报 SDK204，错误地到src查找manifest。

`author-sdk/build/DTMAPI.Author.props:17` 把作者根默认设为 `MSBuildProjectDirectory`；`DTMAPI.Author.targets:25` 的中间文件仍为相对路径，而 `StandardBuildIntegration.PrepareCommand` 等以作者根重新解释。显式设置正确的 `DtmApiAuthorProjectRoot` 后仍失败，见 `subdirectory-explicit-root-build.json`：引用清单写入父级obj，MSBuild在src/obj读取，最终出现缺System/BCL的编译错误。不能仅传一个根目录参数便宣布修好。

**决定：** 区分作者元数据根、所选csproj根、输出根及包内路径；保留projectFile的子目录能力。默认作者根通过标准MSBuild从当前工程目录向上定位最近的dtmapi.author.json，显式值优先且错误时不fallback；校验该元数据指向当前所选工程。跨MSBuild/CLI的文件参数及facts路径采用绝对路径，不依赖调用者cwd，不扫描游戏或其他作者工作区。CLI、实际IDE、CI共用这一规则，不要求复制manifest或将所有csproj搬回根目录。

## R03 — 首发材料仍含旧后端描述（P2，B12）

D6实际README第54行仍称最小Strict构建“compiles in-process”，与现在启动标准MSBuild的实现及同包工程指南矛盾。THIRD-PARTY-NOTICES中的“published Author SDK”也应改为准确的当前交付措辞，避免旧SDK已发布的暗示。既有API迁移说明和真实安装journal恢复能力仍可保留，不能借文字清理删掉玩家兼容reader。

**决定：** 随返修更新首次使用资料，说明CLI使用随包工具链、普通restore可能访问作者配置的NuGet源、显式offline的范围、作者根与工程根的路径规则及Doctor边界。对下一份准确ZIP检查本地链接、示例、工具链/inventory/许可。只改仓库文档不能关闭交付材料问题。

## 收口

按PN-041返修步骤依次完成R01、R02、R03及准确SDK发行/外部作者门，在同一Update继续记录。R01/R02是SDK局部实现问题，没有反证推翻标准MSBuild路线。保持Runtime r6、多平台 r2和已有兼容证据；仅真实修改其共有代码/载荷时重建受影响投影。CSV保持删除，旧SDK从未发布，不增通用迁移器；不启动M4、不上传。详细项完成后统一交回。

## 2026-09-11 实施返修交回

[0012](../../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)完成同批F1–F4，[D7补验](../../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/README.md)保存实际结果。本段是实施方补验交回，不冒充重新进行全部独立审查：

- R01：实际运行IL六种字段指令匹配，含嵌套/泛型/继承与合法ldtoken；复制原反例、配对正确DLL/PDB，用准确D7报SDK302与0.7.0 target，未产出错误ZIP，原ZIP保护保持。
- R02：最近祖先作者根与显式优先、所选csproj身份、绝对中间文件/facts；根/src/中文空格/外部cwd/两配置/Clean、XML/PDB/内容及无父obj通过。准确D7的外部CLI、实际VS Code Release Rebuild、公开CI三产物相同。
- R03：准确D7随包标准MSBuild/首次公开/恢复路径与Doctor边界、链接/inventory/许可通过。SDK与玩家包分离。
- F4：完整Release repair-r2从头含build，exit0、2552.98秒、前后输入相同；新Spruce作者字节在准确r6上真实Mono补验 `GAME-SMOKE/20260911-001047` Passed，源码第13行、正常owner/QA关闭及原资产恢复通过。r6/r2和PN-042具名范围复用。

完整repair-r1和仓库tmp focused两次Windows临时目录Access denied保留为失败；系统Temp focused与使用现有TestTempRoot的新完整运行通过，未改权限/禁用保护，没有确证具体系统占用根因。该观察与R01/R02产品缺口分开记录。当前交回D7，原D6不会被追改为通过。
