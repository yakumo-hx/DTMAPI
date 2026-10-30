# 贡献源码

## 环境与命令

安装 Git 和 `global.json` 指定的 .NET SDK 8.0.421。Windows 的完整公开入口还需要系统自带 Windows PowerShell 5.1；Linux 使用原生 PowerShell 7。首次构建需连接 NuGet。不要提供游戏 DLL 或内部目录来补齐公开测试输入。

```powershell
git clone https://github.com/yakumo-hx/DTMAPI.git
cd DTMAPI
./tools/scripts/test-public-source.ps1 -Profile Generic -List
./tools/scripts/test-public-source.ps1 -Profile Generic -Configuration Debug
./tools/scripts/test-public-source.ps1 -Profile Generic -Configuration Release
# Windows 完整公开范围
./tools/scripts/test-public-source.ps1 -Profile PublicWindows -Configuration Release
```

WSL 请克隆到 Linux 文件系统，例如 `~/src/DTMAPI`；不要复用 `/mnt/c` 或 `/mnt/e` 的 Windows `bin/obj`、SDK 可执行文件与缓存。`Get-DotNetExe` 会验证主机和固定 SDK 策略。Linux 测试不启动游戏。两端使用同一份 C# 测试与 PowerShell 编排。

`-BuildOnly` 只构建，不计测试通过。`-NoBuild` 仅复用输入与产物均匹配的前一次构建；修改输入后重新构建。`-ReportPath <绝对路径>` 输出准确源码提交、树、选择和实际结果。构建生成物写入仓库 `tmp`、`.tools`、各工程 `bin/obj`。Linux 测试会话使用按用户隔离的短 `/tmp/dxx` 目录，以适应 Unix 管道地址长度；保持 lease、失败记录和清理规则。可用独立 `NUGET_PACKAGES` 记录冷恢复成本。普通文档改动只需链接与边界检查，不要求运行游戏。

## 选择与覆盖

[suites.json](tests/DTMAPI.UnitTests/suites.json) 是唯一测试分类。新增 suite、SDK focus、脚本或 fixture 时一次声明执行器、公开范围、Windows/Linux、依赖与用途。未知条目、未知 focus、选中项缺输入、失败或空选择必须返回非零；内部专项仍有明确入口。

`Generic` 选择公开且支持 Linux 的条目；`PublicWindows` 选择公开 Windows 条目。游戏、私有 Advanced 引用、真实旧 SDK/self-contained 客户端与正式发布矩阵分别保留内部入口。Manbo 可选 WAV 不在公开输入中，不支持依赖该资源的完整 solution 构建。

## 提交 PR

说明具体问题、改变后的行为和实际验证范围。测试直接运行自定义可执行程序；不要用空的 `dotnet test` 成功作为证据。原生行为、物理输入、保存、性能与规则测试分别说明。配置升级与退出恢复也是外部契约，请提供旧配置样本和失败反例。

第三方材料需提供可再分发许可、版本与来源；不能提交游戏反编译代码、前身实现、游戏 DLL、凭据或本机配置。既有 fixture 的逐成员清单和完整 LGPL 对应源码在 [许可目录](tools/public-source/licenses/README.md)。

贡献者只维护公开源码、文档与测试，无需写内部 Update 或月度记录。维护者使用[固定对象导出与回流工具](tools/public-source/README.md)保留公开作者历史；源码更新 PR 使用 merge commit 保留导出基线，不能 squash/rebase。
