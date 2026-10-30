# DTMAPI

DTMAPI 是《多洛可小镇》的 Mod 运行时与作者工具。游戏内程序集面向 `netstandard2.0`，源码工具与测试使用仓库固定的 .NET 8 SDK。

**当前分支为 `0.7.5-review` 源码评审分支。** 本次不合并主线；默认分支 `main` 与 [Releases](https://github.com/yakumo-hx/DTMAPI/releases) 保持 0.7.0。本分支包含已实现的 0.7.5 候选源码，不代表完整发行验收或安装包发布。主要变化与迁移边界见[公开变更](CHANGELOG.md)。

## 从这里开始

- 玩家：从 [Releases](https://github.com/yakumo-hx/DTMAPI/releases) 获取正式运行时与安装说明。此仓库是源码，不能直接当作游戏插件目录安装。
- Mod 作者：阅读[作者指南](author-sdk/README.md)与[迁移说明](author-sdk/MIGRATION.md)。普通工程可以直接引用 API；SDK 是可选便利工具，不签发 Runtime 准入证明，`pack` 只打包已有目录。
- 源码贡献者：阅读 [贡献指南](CONTRIBUTING.md)与[测试入口](tests/README.md)。
- 英文入口：[English](README.en.md)。

## 0.7.5 评审范围

本分支包含按实际依赖加载的 Runtime、可选 SDK、内置按需畜牧/怪物/音频模块、音频准备工具、配置与诊断界面改进，以及安装/卸载工具和独立 Mod 源码。

0.7.5 删除了旧公共 API 和诊断 DTO，新 `ManifestVersion=2` 需要配套 Runtime。Y 控制台和现用畜牧、怪物、音频消费者须配套更新；旧独立 Host 和已替换的内容格式不提供兼容层。请先按[迁移说明](author-sdk/MIGRATION.md)调整工程与包，再验证实际使用场景。

## 源码支持范围

| 环境 | 支持范围 |
| --- | --- |
| Windows | `PublicWindows` 分类中的构建、纯规则、合成 fixture、SDK 和脚本测试 |
| Linux / WSL | `Generic` 分类中的构建与无游戏测试；使用 Linux 原生 dotnet、pwsh 和独立文件系统 |
| 游戏运行、安装器发布、实体按键与原生保存 | 独立接受范围；源码测试通过不代表这些操作通过 |

仓库不包含游戏程序集、个人配置、内部调试证据或未授权媒体。可选 Manbo 音频素材未公开，不能把整个 solution 的构建当作本仓库支持承诺。测试入口根据[唯一分类表](tests/DTMAPI.UnitTests/suites.json)选择实际工程；不会将未执行项计为通过。

现有定向测试和 Windows 游戏验证不覆盖所有 Mod 玩法。MES 旧装备选择弹窗、部分怪物玩家验收及动物缺包恢复仍有未完成验证；Proton、Wine、CrossOver 的游戏运行也未因此获得验证。

## 构建与测试

安装 `global.json` 指定的 .NET SDK；Windows 使用 PowerShell，Linux 使用 PowerShell 7。进入仓库后运行：

```powershell
./tools/scripts/test-public-source.ps1 -Profile Generic -Configuration Release
# Windows 的完整公开范围：
./tools/scripts/test-public-source.ps1 -Profile PublicWindows -Configuration Release
```

查看选择用 `-List`；只构建用 `-BuildOnly`。详细要求、Debug 验证和缓存说明见[贡献指南](CONTRIBUTING.md)。许可边界见 [NOTICE](NOTICE.md)；源代码使用 [MIT](LICENSE)。
