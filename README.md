# DTMAPI

DTMAPI 是《多洛可小镇》的 Mod 运行时与作者工具。游戏内程序集面向 `netstandard2.0`，源码工具与测试使用仓库固定的 .NET 8 SDK。

当前开发版本为 `0.7.5`。此目录是公开源码，安装包从 [Releases](https://github.com/yakumo-hx/DTMAPI/releases) 获取。主要变化与迁移说明见[变更记录](CHANGELOG.md)。

## 从这里开始

- 玩家：从 [Releases](https://github.com/yakumo-hx/DTMAPI/releases) 获取正式运行时与安装说明。此仓库是源码，不能直接当作游戏插件目录安装。
- Mod 作者：阅读[作者指南](docs/authors/README.md)与[迁移说明](docs/authors/MIGRATION.md)。普通工程可以直接引用 API；SDK 是可选便利工具，不签发 Runtime 准入证明，`pack` 只打包已有目录。
- 源码贡献者：阅读 [贡献指南](CONTRIBUTING.md)与[测试入口](tests/README.md)。
- 英文入口：[English](README.en.md)。

## 0.7.5 评审范围

本分支包含按实际依赖加载的 Runtime、可选 SDK、内置按需畜牧/怪物/音频模块、音频准备工具、配置与诊断界面改进，以及安装/卸载工具和独立 Mod 源码。

0.7.5 删除了旧公共 API 和诊断 DTO，新 `ManifestVersion=2` 需要配套 Runtime。Y 控制台和现用畜牧、怪物、音频消费者须配套更新；旧独立 Host 和已替换的内容格式不提供兼容层。请先按[迁移说明](docs/authors/MIGRATION.md)调整工程与包，再验证实际使用场景。

## 构建与测试

安装 `global.json` 指定的 .NET SDK，在源码根目录运行：

```powershell
./build/build.ps1 -Configuration Release -SkipTests
./build/test-unit.ps1 -Configuration Release
./build/test-architecture.ps1 -Configuration Release
```

`test-unit.ps1 -List` 列出原有默认测试范围，`-Focus <名字>` 运行其中一组。内置游戏模块和第一方 Mod 的完整构建需要本机游戏，命令见[构建说明](build/README.md)。

许可说明见 [NOTICE](NOTICE.md)，源码使用 [MIT](LICENSE)。
