# API 状态

DTMAPI 的公开接口只分两级：

| 级别 | 意思 |
| --- | --- |
| **Stable** | 以后的版本保持兼容。要改，只在明确的破坏性版本里改，并写进 CHANGELOG 和迁移说明。 |
| **Experimental** | 能用，但以后还会改。改的时候写进 CHANGELOG。 |

接口上的 `[DtmApiStatus]` 标的就是它的级别。没有标的类型，跟着用到它的接口走：事件参数跟着它的事件，配置页面的条目跟着配置菜单，`DtmMod` 算 Stable。

## 哪些是 Stable

| 用来做什么 | 接口 |
| --- | --- |
| 入口和 manifest | `DtmMod`、`IDtmHelper`、`IManifest`、`IManifestDependency` |
| 日志 | `IMonitor` |
| 读写自己的配置 | `IConfigHelper` |
| 翻译 | `ITranslationHelper` |
| 查其他 Mod、拿它们提供的 API | `IModRegistry` |
| 游戏循环事件：`GameLaunched`、`UpdateTicked`、`OneSecondUpdateTicked`、`ReturnedToTitle` | `IEventsHelper`、`IGameLoopEvents` |
| 配置菜单 | `IDtmConfigMenuApi`，用 `helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")` 取得 |

## 哪些是 Experimental

`DTMAPI.Abstractions` 里除了上表的其他接口，包括：

- 读档、保存、输入、菜单、创意工坊、诊断这几组事件。
- 快捷键和输入范围、模态弹窗。
- 配置菜单的弹窗、重启提示、默认快捷键。
- 调度器、命令、运行时上下文、随 Mod 一起释放的资源。
- Mod 自己的数据文件、全局数据、带版本的设置。
- 反射、包内资源查询、Content Host。
- 物品名称（`IItemDisplayNameApi`）、作物收获（`ICropHarvestingApi`）。

另外三个内容程序集整体是 Experimental：`DTMAPI.AnimalContent`（畜牧）、`DTMAPI.MonsterContent`（怪物）、`DTMAPI.AudioContent`（音频）。

## 支持哪些旧 Mod

- 用 0.6.0 及以后的 API 编译的 Mod 在支持范围内。0.7.5 的改动不让这些 Mod 加载失败。
- 0.6.0 之前的 API 和用它编译的 Mod 不再维护。
- 0.7.5 删掉的旧类型列在 [RetiredPublicTypes.txt](../../tests/DTMAPI.Core.Tests/RetiredPublicTypes.txt)，不会恢复。程序集版本号还是 `0.5.3.0`，这不表示仍支持 0.5.x 的接口。
- SDK 只带当前版本的引用程序集。要对照旧版本的接口，看 git 的版本标签。

各项改动和替代做法见[迁移说明](MIGRATION.md)。
