# Author SDK 第三方载荷说明

桌面 SDK 包含 NuGet.Protocol、NuGet.Packaging、NuGet.Configuration、NuGet.Common、NuGet.Frameworks 和 NuGet.Versioning 7.9.0，copyright Microsoft Corporation，采用 Apache-2.0。准确上游 NuGet.Client 提交为 `977537e19c6be57fead1411e6cf05f936bf1baf4`，许可证位于 `licenses/NuGet-7.9.0-LICENSE.txt`。这些仅供桌面使用的组件负责 NuGet 版本/框架解析及包内容/签名验证；restore 由随包标准 .NET SDK 执行。它们不是游戏 Runtime 依赖。

NuGet 的桌面依赖 Newtonsoft.Json 13.0.3 采用 MIT 许可，许可证位于 `licenses/Newtonsoft.Json-13.0.3-LICENSE.txt`。Microsoft System.Security.Cryptography.Pkcs 8.0.1 和 ProtectedData 8.0.0 由随 SDK 分发的 .NET 许可及第三方声明覆盖。它们用于桌面工具，不是普通 Mod 载荷。作者恢复的依赖必须附带自己明确选择的许可材料，工具声明不授予任意作者类库的分发许可。

SDK 的 .NET 8 元数据工具包含 Mono.Cecil 0.11.6，由 Jb Evain and contributors 以 MIT/X11 许可提供。上游许可证位于 `licenses/Mono.Cecil-0.11.6-LICENSE.txt`。它仅用于作者工具和 Doctor，五个游戏加载的 Runtime 程序集不依赖 Mono.Cecil。本机生成的游戏引用视图留在作者电脑上，不进入 Mod 或 SDK 分发。

SDK 包含 Microsoft `NETStandard.Library` 2.0.3 的引用程序集，仅用作离线编译引用。发行载荷必须保留该包的 `LICENSE.TXT` 和 `THIRD-PARTY-NOTICES.TXT`，并将两者纳入 `compatibility.json` 哈希。此前的 SDK 构建均为内部版本，未公开发布。

源码树不跟踪这些 `.nupkg` 或引用 DLL 二进制。发行暂存必须使用固定包版本并保留声明，不能从玩家游戏安装目录获取 `DTMAPI.Abstractions.dll`。

完整 SDK 还在 `toolchain/dotnet` 中包含未经修改的 .NET SDK 8.0.421 工具链，包括 MSBuild、Roslyn 及其随附许可/声明。`offline-packages` 中选定的基础及 analyzer 开发 nupkg 保留上游许可元数据。SDK203 analyzer 面向 Roslyn 4.11.0，MSBuild 适配器使用宿主 MSBuild task API。这些是作者构建工具，不是 Mod 运行依赖。
