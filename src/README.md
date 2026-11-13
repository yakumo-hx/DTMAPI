# 源码目录

| 工程 | 负责什么 |
| --- | --- |
| Abstractions、ModConfigMenu、内容契约 | Mod 作者使用的 API 和内容格式 |
| Core | Mod 发现、加载、事件、配置和诊断 |
| GameBridge.DolocTown、BepInExBootstrap | 游戏接点和运行时入口 |
| RuntimeModules、Modules.Animals、Monsters、Audio | 内置内容模块 |
| AuthorSdk、Author.Build、Author.Analyzers | 作者命令行工具、编译支持和分析器 |
| NativeReferences、Shared/NativeReferenceSurface.cs | 从本机游戏生成编译引用 |
| AudioPrepare、AudioPreparation | 音频准备工具和共用处理 |
| MultiPlatformInstaller | 玩家安装、卸载、状态查询和日志收集 |

工程引用和职责见[架构](../docs/architecture.md)，第一方 Mod 在 [mods](../mods)。运行在游戏中的程序集使用 `netstandard2.0`，作者工具和安装器使用 .NET 8。
