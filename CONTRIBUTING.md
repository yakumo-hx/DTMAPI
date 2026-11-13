# 贡献源码

安装 `global.json` 指定的 .NET SDK。Windows 使用 PowerShell，Linux 使用 PowerShell 7。首次构建需要连接 NuGet。

```powershell
./build/build.ps1 -Configuration Release -SkipTests
./build/test-unit.ps1 -List
./build/test-unit.ps1 -Focus core
./build/test-architecture.ps1 -Configuration Release
```

按改动涉及的范围运行现有测试。`-NoBuild` 复用已生成的测试产物；源码变动后重新构建。完整命令见[构建说明](build/README.md)。

解决方案包含平台、作者工具和离线测试。内置游戏模块和第一方 Mod 使用各自的构建入口，需要从本机游戏生成引用。不要提交游戏 DLL、本机配置、日志、存档或凭据。

PR 说明改了什么、删了什么、怎么验证的。涉及游戏操作时，写清实际操作和观察结果。第三方材料注明许可、版本与来源，已有可再分发依赖见[许可目录](licenses/README.md)。
