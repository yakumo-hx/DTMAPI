# 作者工具第三方载荷说明

当前桌面 CLI、独立 Doctor 和原生引用转换器使用 Mono.Cecil 0.11.6（Jb Evain and contributors，MIT/X11）；许可证见 `licenses/Mono.Cecil-0.11.6-LICENSE.txt`。它只用于桌面元数据工具，五个游戏加载 Runtime 程序集不依赖 Mono.Cecil。派生游戏引用留在作者电脑上，不随 Mod 或 SDK 分发。

明确发布前检查生成的作者发行包以 .NET 8 self-contained 方式发布，保留其 LICENSE 与 ThirdPartyNotices。该路径的 `-IncludeToolchain` 才额外复制 .NET SDK 及随附许可；`-IncludeAnalyzer` 才加入可选分析器。本地 CLI 只使用普通 framework-dependent build 输出，不复制工具链或封包。普通工程的 restore 由作者选定的 .NET SDK 完成，发行工具包不再携带 NuGet.Protocol 系列或离线 NETStandard.Library 包。

本仓库不跟踪上游 nupkg 或游戏 DLL。作者自行选择的第三方依赖仍需保留各自许可材料；这些声明不授予游戏程序集的分发许可。当前 SDK 重建为本地候选，未公开发布。
