# 普通工程与还原

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <Reference Include="DTMAPI.Abstractions" HintPath="$(DtmApiReferences)/DTMAPI.Abstractions.dll" Private="false" />
  </ItemGroup>
</Project>
```

`dotnet build -p:DtmApiReferences=API目录` 即可编译。包还原、ProjectReference、NuGet、锁文件、IDE 与编译器由标准 .NET 工具处理；作者工具不检查固定 SDK 摘要、目标目录或工程引用证明。

仓库维护者使用 `Get-DotNetExe` 选择 .NET 8。外部作者可自行安装适用的 .NET SDK；`dtmapi-author build/restore` 会转交命令行参数，支持 `DTMAPI_DOTNET_EXE` 指定主机。完整工具链是可选附带项。

`DTMAPI.Author.props/targets` 是可选引用便利层。分析器默认关闭，显式启用也仅提供建议。正常开发可重复构建到自己的输出目录；打包是独立步骤。仓库 `build-mod-project.ps1` 是整理现用工程目录的便利脚本，不是 Runtime 协议。
