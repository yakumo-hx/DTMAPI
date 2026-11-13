# 构建与测试

在公开源码根目录运行命令，也就是包含 `DTMAPI.sln` 的目录。构建脚本按 `global.json` 选择 .NET SDK。

## 编译和单元测试

```powershell
./build/build.ps1 -Configuration Release -SkipTests
./build/test-unit.ps1 -List
./build/test-unit.ps1 -Configuration Release
./build/test-unit.ps1 -Focus core
./build/test-architecture.ps1 -Configuration Release
```

`build.ps1` 编译解决方案；不传 `-SkipTests` 时，还运行现有默认单元测试和安装器测试。首次构建会从仓库自带的 BepInEx 压缩包准备离线测试引用。

只编译指定工程及其依赖：

```powershell
./build/build.ps1 -Projects src/DTMAPI.AuthorSdk/DTMAPI.AuthorSdk.csproj -SkipTests
```

`test-unit.ps1 -NoBuild` 直接运行已经生成的测试。修改源码或缺少产物时，去掉 `-NoBuild` 重新构建。测试范围见[范围表](../tests/DTMAPI.UnitTests/suites.json)。`dotnet test DTMAPI.sln` 只执行架构检查。

## 内置模块和第一方 Mod

内置模块和部分 Mod 需要本机游戏程序集。设置 `DTMAPI_GAME_DIR`，或在源码根目录的 `local.settings.json` 中填写 `GameDir`。

```powershell
./build/build-runtime-modules.ps1 -Configuration Release
./build/build-mod-project.ps1 -ProjectPath ./mods/MoreSaves/DTMAPI.MoreSaves.csproj -BuildOnly
```

Mod 工程的实际名称以其目录中的 `.csproj` 为准。游戏编译引用由构建脚本从本机游戏生成，不随源码分发。

## 作者工具

`prepare-author-sdk.ps1` 构建本地作者 CLI。传入 `-RequireReleaseArchive` 创建作者分发包；`-IncludeAnalyzer` 和 `-IncludeToolchain` 分别附带分析器和工具链。

```powershell
./build/prepare-author-sdk.ps1
./build/prepare-author-sdk.ps1 -RequireReleaseArchive
./build/build-audio-tool.ps1
```

作者用 `validate` 检查 Mod 包。基础示例在 [sdk/examples](../sdk/examples/README.md)，使用方法见[作者指南](../docs/authors/README.md)。

## Runtime 和 Mod 打包

创意工坊制包显式接收产品清单、源码根目录和图稿目录：

```powershell
./build/build-release-workshop-packages.ps1 -RuntimeOnly -ProductsPath <products.json> -ProductSourceRoot <source-root> -AssetRoot <artwork-root>
./build/build-release-workshop-packages.ps1 -ModsOnly -Mod MoreSaves -ProductsPath <products.json> -ProductSourceRoot <source-root> -AssetRoot <artwork-root>
```

产品清单是 JSON 数组，每项包含 `sourceRoot`、`workshopId` 和 `uploadFolder`。`sourceRoot` 相对于 `ProductSourceRoot`，后者默认为公开源码根目录。产品名称、说明、版本和最低 Runtime 版本从各自的 `info.json` 读取。

`-RuntimeOnly` 生成 Windows 和多平台 Runtime 包。两个包共用运行时与 C# 安装器；多平台包还包含 Linux 安装程序。Runtime 文件清单在 `windows-installer/RuntimePackageFiles.cs`。

`-ModsOnly` 只生成 Mod。`-Mod` 可用源码文件夹名、UniqueID 或上传文件夹名选择产品。不传这些选项时，生成清单中的全部产品。

单独打一个 Mod，可以使用 `build-mod-project.ps1 -ProjectPath <csproj-or-source-directory> -AssetRoot <artwork-directory>`；`-BuildOnly` 只编译，`-BuildOutput` 整理已有编译产物。

## 要注意的

- 完整 Runtime 包含游戏模块，构建和制包需要安装本机游戏。
- 图稿和内容包素材按参数显式提供；源码不包含第三方素材。
- 制包中的构建标识使用当前源码快照的 Git 提交，不需要私有工作历史。
