# DTMAPI

DTMAPI 是 Doloc Town 的 Mod 框架。BepInEx 负责启动，DTMAPI 提供受管 Mod 加载、作者 API、配置界面、Hook 和日志诊断。

Windows 与多平台 Runtime **0.7.0 已在 Steam 创意工坊发布**，包含 marker 文件开头的 UTF-8 BOM 兼容修复；Y 控制台 **1.1.3** 也已发布。已发布 Runtime 包的构建来源仍为 `7d26482a2a95`，本分支后续的源码或文档提交不改变这一记录。详见[发布确认](docs/updates/2026/20260911-0011-runtime-y-hotfix-publication.md)。

Author SDK D7 已完成 Windows 范围验收，**独立 GitHub Release 附件尚未发布**。SDK 与玩家安装包分开交付，具体范围见[SDK 候选交付说明](docs/planning/platform-next/release-candidate.md)。

## 玩家：安装与使用

从适合自己环境的创意工坊条目获取 Runtime，按条目及包内说明安装，然后从 Steam 正常启动游戏：

- [Windows Runtime](https://steamcommunity.com/sharedfiles/filedetails/?id=3743016467)
- [多平台 Runtime](https://steamcommunity.com/sharedfiles/filedetails/?id=3792681186)

受管 Mod 使用游戏官方的本地 MODS 或工坊订阅来源，并遵循官方启停状态。不要把受管 Mod DLL 放进 `BepInEx/plugins`；历史 `<game>/Mods` 目录不再参与发现。安装或启停后若提示重启，请退出游戏再启动。

多平台包已发布不代表所有宿主都完成了游戏运行验收；Steam Deck、Proton、CrossOver 的实际启动与运行范围仍以发布记录为准。遇到加载、配置或存档问题，可从[玩家支持入口](docs/workflows/player-support.md)查找排查步骤。

## Mod 作者：从 SDK 开始

[Author SDK](author-sdk/README.md)提供项目创建、校验、构建、打包、部署和诊断命令。新项目使用 schema 4 和标准 MSBuild 工程；游戏加载的程序集保持 `netstandard2.0`。

| 想做什么 | 入口与限制 |
| --- | --- |
| 使用 DTMAPI API 编写 DLL Mod | 从 Strict CodeMod 开始，先查 [API 状态](author-sdk/API-STATUS.md)。公开类型不代表所有成员都是 Stable。 |
| 为自己的 Mod 使用游戏原生类型 | 阅读 [Advanced 本机原生引用](author-sdk/NATIVE-REFERENCES.md)。0.7.0 的 `NativeContractVersion=2` 支持任意合法作者 ID，由 SDK 生成引用和包绑定；旧 V1 与 receipt 包保留各自校验规则。 |
| 制作内容包 | 阅读[内容包作者指南](author-docs/README.md)。现有指南面向具体内容领域，通用独立 Content Host 尚未完成。 |
| 配置工程、NuGet 或共享 DLL | 阅读[工程与恢复](author-sdk/PROJECTS-AND-RESTORE.md)及[包依赖](author-sdk/PACKAGE-DEPENDENCIES.md)。 |

Advanced 包不能携带游戏或平台 DLL，也不能手写准入凭证。Mod 身份、原生代码归属和存档提交规则由 [PROJECT.md](PROJECT.md)定义。SDK 的打包与分发规则见[交付流程](docs/workflows/author-sdk-release.md)；仓库说明更新不会替换已验收的 D7 ZIP。

## 源码贡献者：构建与验证

main 已更新为 0.7.0 源码主线，包含标准 MSBuild SDK 和相关记录；原源码 PR #3 已合并。正式公开目录、许可补件及干净公开源码验收尚未完成，见[公开交付设计](docs/reviews/code/2026/20260911-0003-public-source-delivery-design.md)。Linux 贡献者构建属于[后续支持计划](docs/planning/contributor-support.md)，目前不承诺 Linux 全仓构建或测试通过。

现有公开源码测试入口面向 Windows，需要 Python 3、Node.js 和 Windows PowerShell 5.1；脚本通过 `Get-DotNetExe` 选择兼容的 .NET 8 工具链。先阅读[脚本说明](tools/scripts/README.md)，再按修改范围选择检查：

```powershell
# 现有 Windows 公开源码测试范围，包含构建。
tools/scripts/test-public-source.ps1 -Configuration Release

# 仅编译常规 solution，不运行测试。
tools/scripts/build.ps1 -Configuration Release -SkipTests
```

这两个命令是不同用途的入口，无须依次执行。公开测试入口不等于完整 Release 验证，也不能证明游戏中的实际行为；当前远端 CI 结果应查看对应提交的检查记录，不能从已有发布验收推断通过。

维护者的完整 Release 入口是 `tools/scripts/test.ps1 -Configuration Release`，自带构建，不要在它之前再跑一轮完整构建。普通修复按[定向验证说明](tools/scripts/README.md#choose-validation)选择项目；纯文档修改只做文档检查。

[DTMAPI.sln](DTMAPI.sln)管理常规框架、示例和测试工程。Advanced 产品的构建入口由 [Product Catalog](tools/release/dtmapi-product-catalog.json)与 Author SDK 管理。`tools/scripts/status.ps1`只报告本机工具、路径和 Runtime 状态，不安装缺失的 SDK。

需要本机游戏路径时，使用 `DTMAPI_GAME_DIR`、未纳入 Git 的 `local.settings.json`，或脚本已有的路径解析；不要把个人 Steam 路径写入仓库。

## 仓库导航

| 目录 | 内容 |
| --- | --- |
| `src/` | Runtime、公共契约、Bootstrap、GameBridge、配置菜单及作者工具源码 |
| `products/first-party/` | 第一方产品源码，产品类型与构建路线查 Catalog |
| `author-sdk/` | SDK 指南、模板、`examples/` 与 `samples/` |
| `tests/` | 源码测试、兼容性工具及 fixture；`mod-fixtures/qa/` 是游戏 QA Mod |
| `tools/scripts/` | 构建、测试、安装、诊断和证据采集脚本 |
| `tools/release/runtime-workshop/` | Windows 玩家 BAT 入口及共用 CMD 分发脚本，规则见[安装器边界](docs/architecture/runtime-workshop-installer-boundary.md) |
| `docs/` | [文档入口](docs/README.md)、[当前任务路由](docs/onboarding/current-state.md)与历史记录 |

## 许可与资料边界

项目许可见 [LICENSE](LICENSE)，第三方说明见 [NOTICE.md](NOTICE.md)。DTMAPI 从零重建；游戏反编译资料、官方工坊文档、第三方 Mod 和 SMAPI 只作参考，不得将官方游戏 DLL、复制的反编译源码、私有调试证据或第三方 Mod 二进制作为 DTMAPI 源码发布。资料使用规则见 [references/README.md](references/README.md)。
