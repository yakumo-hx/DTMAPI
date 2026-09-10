# Author SDK 发布与玩家安装包分工

Owner: SDK 的作者交付流程。候选选择和剩余验收门查 [release-candidate](../planning/platform-next/release-candidate.md)，实际产物/hash/发布结果由其 Update 拥有。本页不记录当前发布状态，也不授权上传。

## 三种交付物

| 交付物 | 使用者及渠道 | 内容与用法 |
| --- | --- | --- |
| Windows Runtime 安装包 | 普通 Windows 玩家；既有 Steam Workshop Runtime 项 | BepInEx 与 DTMAPI 运行组件及四个玩家操作入口；沿现有零 EXE Windows 分发规则 |
| 多平台 Runtime 安装包 | Steam Deck / Linux / Windows 玩家及实验 CrossOver 路径；既有多平台 Workshop 项 | 同一准确 Runtime 载荷，加相应安装器 host；各宿主验证与实际游戏注入分别说明 |
| Author SDK ZIP | Mod 作者；项目 GitHub Releases 的独立下载附件 | 自包含 CLI、冻结 API 引用、模板、schema、指南与许可；解压到开发目录。SDK 的 win-x64 表示开发工具运行平台，不表示它是玩家 Windows 安装包 |

SDK 不进入两个玩家 Runtime 包，不作为受管 Mod 订阅，也不复制到游戏 BepInEx/plugins。玩家运行 Mod 需要 Runtime；只有开发者需要 SDK。发布页面及 Workshop 说明可以链接到 SDK 下载。当前选择 GitHub Releases 主分发，其他下载镜像只有逐字节相同才沿用验收，避免再维护一套 SDK Workshop 自动更新规则。

## 如何上传 SDK

1. 先选定通过验收的 SDK ZIP，附带 builder 生成的同名 `.zip.sha256`；完整指南和许可已经在 ZIP 内。不要把 `preparation.json`、临时输出、私有游戏 DLL 或作者探针当发行附件。
2. 在项目仓库的 0.7.0 Release 中把 **Author SDK** 列为单独附件，标题明确“Mod 作者开发工具 / Windows x64”，并标为当前适用的预览发行。只生成 GitHub 默认的 Source code ZIP 不等于分发 SDK；源码归档没有这个自包含工具包。
3. 发行说明写 SDK 工具版本、默认/可选 API target、最低 Runtime、作者宿主 OS、已验能力与剩余限制。给出解压→new→pack 的入口，指向包内 README；安装游戏 Runtime 另给玩家渠道。当前平台不能笼统宣称完整 MSBuild/NuGet 或所有设备支持。
4. 发布动作前核对附件名称、SHA 和来源；实际上传后再从公开附件下载，比较准确 SHA，并从新的解压目录运行一次最小作者命令。至此才记录公开 SDK 版本和下载 URL，不能用本地建包替代公开下载验收。
5. 已公开附件不原地换字节。后续 SDK 工具修订使用新的工具版本/附件身份；API target、Runtime 版本、原生签名格式分别沿各自兼容契约，不为更新工具改写冻结 API。

GitHub Releases 支持把二进制附件与说明一起发布；自动生成的源码归档与手动上传的发行附件是不同对象，当前单附件上限为 2 GiB。[GitHub 官方说明](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)。准确待上传文件由候选页选择，不在本页再维护版本/大小/hash。

## 验收与构建

SDK 使用 tracked `build-author-sdk.ps1`，包检查用 `check-author-sdk-release.ps1 -PackagePath <准确 ZIP>`。验收必须包含真实解压后的指南链接、CLI/IDE 和仓库外作者输入；源码单测不代替最终 ZIP。只修指南时比对所有执行文件/引用载荷与已验候选，输入未变的 Mono/完整 Release 可复用，不例行重跑全套。

多平台 Runtime 构建是另一条发行门，沿 [installer boundary](../architecture/runtime-workshop-installer-boundary.md)及 [matrix](workshop-package-subscription-test-matrix.md)。SDK 可以在 Windows 开发机上构建，不等于已生成 Linux SDK 或 0.7.0 多平台玩家安装器。每次发布明确列出两个 Runtime 分发实际版本，禁止把旧 0.6.1 多平台文件重标为 0.7.0。
