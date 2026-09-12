# 标准工程、资源与依赖恢复

SDK 使用 .NET SDK 8.0.421 和标准 MSBuild/Csc 完成 CLI 与 IDE 构建。`dtmapi.author.json` schema 4 选择一个 Mod 的 `projectFile`、API target、Strict/Advanced 身份、原生引用意图和发布元数据。编译器属性、工程及包引用、资源、条件和 targets 由 csproj 及其标准导入管理；普通类库只需自己的 csproj。

## 开发环境

完整解压 Windows x64 SDK，在 PowerShell 中以点调用方式加载 `Enter-DtmApiEnvironment.ps1`，再从同一窗口启动 IDE 或 CI。脚本只设置当前进程及其子进程使用的工具链和 SDK 路径，不修改系统环境变量。CLI 的 build/restore/pack 能自行定位随包工具链；直接使用 dotnet 和 IDE 时需要加载环境。显式指定的 `DTMAPI_AUTHOR_DOTNET` 无效、上级目录的 `global.json` 不兼容或工具链缺失时都会报错。ContentPack 和只读 Doctor 不需要编译器。

保留模板的导入顺序：Microsoft.NET.Sdk props、DTMAPI.Author.props、作者属性及 items、Microsoft.NET.Sdk targets、DTMAPI.Author.targets。DTMAPI 接入层负责准备冻结引用、读取求值后的输出，不替代 Build、Restore 或 Clean。游戏加载的入口和运行库保持 netstandard2.0；工具、生成器工程可以使用 net8.0，多目标类库遵循标准工程引用选择规则。

Mod 模板启用 checked 算术、nullable 注解、确定性 portable PDB，以及标准框架和配置常量。`Version` 初始值与新 manifest 一致，此后程序集版本属性由 csproj 管理，manifest 的 Version 仍是 Mod/包版本。AssemblyName/TargetFileName 必须生成 manifest 的 EntryDll 所指定的文件。需要在不同绝对目录下重现产物时，请设置稳定的标准 `PathMap`，必要时也映射生成输入的路径；外部 targets 可能仍有自己的非确定性输入。

PathMap 匹配编译器实际使用的路径拼写，包括盘符大小写。已测试的 VS Code 工作区中，IDE 使用 `e:\...`，CLI 使用 `E:\...`；将这两个实际前缀映射到相同源码前缀后，CLI、IDE 和 CI 的最终 Release DLL、PDB 相同。比较输出时应检查编译参数和 portable PDB 的文档路径。GenerateDocumentationFile 等属性放在 Microsoft.NET.Sdk targets 导入之前，让标准默认值能读取到它们。

已测试的 IDE 环境是 Windows 上的 VS Code 1.107.1、C# Dev Kit 3.20.199 和 C# 2.140.9，覆盖设计时冻结引用、F12、生成源码、错误导航及标准 Debug/Release 构建。PDB 可用不等于调试器可以连接：当时的 coreclr attach 流程未能在 Unity shipping Mono 进程中取得受管栈、locals 或单步能力，因此不承诺该环境支持断点。Unity 的[受管调试指南](https://docs.unity.cn/2020.3/Documentation/Manual/ManagedCodeDebugging.html)说明了兼容适配器与开启 Script Debugging 的 Development Build；准备其他游戏调试宿主属于独立工作。

## 作者根目录、工程目录与输出

CLI 的目录参数是包含 `dtmapi.author.json` 和 `manifest.json` 的**作者根目录**。`projectFile` 相对于这个目录，可以指向 `src/MyMod.csproj`；不要因此把元数据复制到 `src`。CLI restore/build/pack 从所选 csproj 自己的目录调用工程，使上级工具链选择与 IDE 针对同一工程的选择一致。

直接使用 MSBuild 或 IDE 时，`DtmApiAuthorProjectRoot` 默认从 csproj 目录向上寻找最近的、含有 `dtmapi.author.json` 的目录。显式值优先；相对值以 csproj 目录为基准。显式指定的归属目录不存在或不正确会报错，不再退回自动查找。所选元数据的 `projectFile` 必须指向正在构建的工程。查找不会扫描相邻工作区或游戏。静态 `validate` 只检查元数据，不执行 imports，也不能证明 MSBuild 求值成功。

普通 `Compile`、`Content`、`HintPath`、`ProjectReference`、输出及中间目录属性保持标准 MSBuild 语义。MSBuild 与 SDK 之间传递的引用、资产、analyzer 列表、编译参数和 build-facts 文件名使用绝对路径。facts 记录实际工程目录及输出、中间目录、文档的绝对路径。SDK 构建报告和暂存内容位于所选工程的 `obj/dtmapi-author`，子目录工程不会另在父目录建立第二份 `obj`。普通类库无需作者元数据或 DTMAPI imports。

各类路径的基准如下：

| 字段或参数 | 相对路径的基准或要求 |
| --- | --- |
| `DtmApiLicenseFiles` | 作者根目录 |
| `nativeReferences.gameRoot` | 必须显式填写本机游戏安装目录的绝对路径；引用准备使用作者工程自己的原生缓存 |
| MSBuild `DtmApiCompatibilityRoot` | csproj 目录 |
| CLI `--compatibility-root` | 调用命令的 shell 当前目录 |
| CLI `build --output` / `pack --build-output` | 作者根目录；默认包输出在作者根目录的 `dist` |
| `DtmApiPackagePath` | 包内目标路径，与本机目录无关 |

这些 SDK 规则不改变普通 MSBuild item 的求值方式。

## 类库与包元数据

使用标准 ProjectReference、Reference/HintPath 和 PackageReference，例如：

```xml
<ItemGroup>
  <ProjectReference Include="../Logic/Logic.csproj" DtmApiDistribution="self-authored" />
  <ProjectReference Include="../Contract/Contract.csproj" DtmApiRole="shared-contract" DtmApiDistribution="self-authored" />
  <PackageReference Include="Example.Library" Version="1.2.3"
                    DtmApiDistribution="licensed-third-party" DtmApiLicenseFiles="licenses/Example-LICENSE.txt" />
</ItemGroup>
```

示例中的包名和版本只是占位，请替换为实际选择且有权分发的依赖。`DtmApiRole` 默认为 `private-managed`，共享契约必须显式声明 `shared-contract`。每个入包类库都需要 `DtmApiDistribution`。第三方分发还需要非空 `DtmApiLicenseFiles`，多个路径用分号分隔，以作者根目录为基准。没有直接声明的传递依赖，可在 DtmApiCollect 之前通过标准 target，给求值后的 ReferenceCopyLocalPaths items 补上相同元数据；冲突声明会被拒绝。PrivateAssets 控制标准引用传播，本身不会从 Mod 包中移除已选中的运行依赖。

NuGet 可能分别选择 ref 和 lib 程序集。编译使用选定的引用，打包检查最终运行实现的实际类型、成员、身份及依赖闭包。实际字段读写和取地址指令必须与最终实现的 static/instance 存储方式一致；未使用的 API 差异不要求整个 DLL 相同。构建任务、生成器和 analyzers 是编译输入，不是运行载荷。运行程序集必须是纯托管 netstandard2.0，文件名必须精确匹配。当前拒绝 native/RID 和 satellite 运行资产；某个包能参与标准构建，不代表其运行资产受到 Unity Mono 支持。

## 生成代码、资源与内容

标准 Directory.Build.props/targets、Directory.Packages.props、条件、Compile Include/Remove/Link、源码生成器、analyzers、AdditionalFiles、EditorConfig 和 resx/嵌入资源均由 MSBuild 处理。必需的 SDK203 分析会拒绝直接可识别的 async-void 平台回调，包括生成代码；关闭必需分析或跳过编译时，pack 会拒绝出包。

显式选择入包内容：

```xml
<ItemGroup>
  <Content Include="assets/**/*" DtmApiPackagePath="Content/DTMAPI/assets/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

内容路径不能越出包目录、出现大小写冲突、替换生成元数据，或把托管/原生宿主二进制伪装为普通内容。`GenerateDocumentationFile=true` 会将最终 XML 放在程序集旁边。Debug 包包含匹配的 portable PDB；Release 仅在指定 `--symbols true` 时包含。AfterBuild 后处理及 `DtmApiPreparePackageDependsOn` 完成后才采集输出；SDK 不会递归收集 bin 中的陈旧 DLL。

## 标准恢复与离线使用

```powershell
dtmapi-author restore MyMod --json
dtmapi-author restore MyMod --offline true --json
dtmapi-author build MyMod --configuration Debug --json
dtmapi-author pack MyMod --offline true --json
```

restore 和普通 build 使用 NuGet 的正常源、凭据、工程图及锁文件格式。模板要求 `packages.lock.json`；发布前显式运行 restore 并检查锁文件。pack 要求锁文件已存在，使用 locked restore；缺少锁文件或依赖声明改变时会失败并提示恢复，不会悄悄选择新依赖版本。凭据放在正常的 NuGet 配置或 credential provider 存储中，不进入源码或包内容。

离线模式只使用 SDK 本地源和已有包缓存，DTMAPI 在该模式下不下载。完整 SDK 包含固定工具链、基础 netstandard 包和 analyzer 开发依赖，不包含任意作者依赖或维护者的整个缓存。需要离线使用其他扩展包、工具时，先准备它们。作者自定义 targets 可以自行进行 I/O，因此离线模式不是网络沙箱。

NuGet 归档哈希、签名和提取后的文件字节会再次检查。最终运行库还需通过 PE/方法、依赖、许可及保留宿主检查。支持合法 CLR 委托，拒绝 P/Invoke、unmanaged/internal-call 方法和非 IL-only 运行 DLL。已恢复的私有 Newtonsoft.Json 可能与游戏驻留版本冲突；适用时使用明确支持的 [Advanced 宿主引用](NATIVE-REFERENCES.md)。

## 报告、失败处理与 CI

pack 只构建一次，然后将最终 DLL/PDB/XML、选定运行库及内容收集为私有快照。`--build-output` 选择这次构建的标准 OutDir。报告用哈希绑定实际输出、MSBuild facts、工具链/编译器和冻结引用，不为任意 MSBuild 工程承诺统一的源码/target 身份。同路径发布使用独占锁。pack 失败或取消会保留此前成功的 ZIP，但标准 MSBuild 的 bin/obj 不是事务存储。Ctrl+C 会停止 SDK 启动的 MSBuild 进程树。

`ci/verify-project.ps1` 使用同一随包 SDK，执行 validate、可选 restore、一次 pack，再对生成的 ZIP 运行只读 Doctor。显式提供的作者逻辑测试脚本可以检查解压产物。Strict CI 不需要游戏文件；Advanced 构建需要作者有权使用的游戏引用。实际 Mono 加载、源码行、owner 清理、重启及驻留程序集行为仍需另做游戏验证。

Doctor 只检查已有包及元数据契约，不执行 build targets 或作者 IL。它不是完整 IL 验证器，报告无错误也不能证明游戏行为正确。pack 的最终输出检查与实际游戏验证各有自己的职责。
