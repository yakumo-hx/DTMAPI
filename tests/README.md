# 离线测试

在公开源码根目录运行：

```powershell
./build/test-unit.ps1 -List
./build/test-unit.ps1 -Configuration Release
./build/test-unit.ps1 -Focus core
./build/test-architecture.ps1 -Configuration Release
```

`test-unit.ps1` 运行[范围表](DTMAPI.UnitTests/suites.json)中原有的默认 Unit 范围。`-Focus` 选择其中一组，`-NoBuild` 直接运行已经生成的测试。

其余离线测试保留现有独立入口，未加入默认范围。`build.ps1` 仍运行原有的安装器测试。`dotnet test DTMAPI.sln` 只执行三项架构检查。

构建和游戏编译引用的准备方法见[构建说明](../build/README.md)。
