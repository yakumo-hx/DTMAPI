# 更新记录

## 0.7.6

### 玩家

- Steam Deck / Proton 上加载 Mod 时不再中断启动。
- 重新安装、装回旧版本、修复旧安装时，安装器不再拒绝，直接覆盖 DTMAPI 自己的文件。

## 0.7.5

### 玩家

- 在标题页的官方 Mod 界面勾选 Mod，关闭界面后就能用，不用重启游戏。取消勾选、换版本要重启才生效。需要重启或加载失败时，会弹窗说明是哪个 Mod、为什么。
- 标题菜单里可以显示“Mod 配置”入口，用方向键和确认键进入，文字跟随游戏语言。多平台包默认打开，Windows 包默认关闭，可在 DTMAPI 输入设置里打开。
- 内置动物、怪物和音效模块：内容包可以加新动物、换音效。音效支持 WAV、MP3、FLAC、AAC。
- Windows 包和多平台包改用同一个安装程序。卸载时删掉 DTMAPI 的全部文件，保留旧装备文件、官方 Mod 和存档。
- 第一方 Mod 都更新到需要 DTMAPI 0.7.5。创意工坊更新完成后，要再运行一次 `1_install_dtmapi.bat`。
- 更多装备槽 2.0：三个额外属性装备槽，装备随游戏存档保存。发现 1.0.1 的旧装备时，确认后导入。
- 更多存档：存档槽增加到 60 个，分 5 页；可以复制、删除、改名存档。
- Y 键控制台：物品列表直接读游戏的物品表，来源只分本体和模组。
- 新 Mod：强力农用喷枪、缺氧动物包（内容包示例）。

### 作者

这是一个破坏性版本，具体改法见[迁移说明](docs/authors/MIGRATION.md)。

- 新的 manifest 格式（`"ManifestVersion": 2`）：名称、作者、版本、说明放进包根的 `info.json`。旧格式照样能读。
- SDK 改用普通工程：`dtmapi-author new codemod` 新建，`dotnet build` 编译，`deploy` 部署。SDK 只带当前版本的引用程序集。
- API 状态只剩 Stable 和 Experimental 两级，见 [API 状态](docs/authors/API-STATUS.md)。
- 新增畜牧、怪物、音频三个内容模块，都是 Experimental。旧的 `custom-animals.json`、`audio-replacements.json` 和对应的 API 删掉了，改用 `PackData/content.json` 或新的 API。
- 删掉 198 个旧公共类型，清单在 [RetiredPublicTypes.txt](tests/DTMAPI.Core.Tests/RetiredPublicTypes.txt)。其中包括 `IContentItemInfo`、`IMailDeliveryApi`、`CropHarvestScope`、`DtmApiDisposition`。
- 删掉作者会话和 SDK 的 `session` 命令。改完代码后重新编译、部署、重启游戏。
- 公开 API 只能在主线程调用；事件在主线程上同步发出；`UpdateTicked` 每帧只发一次。
- 勾选即加载：新勾选的 Mod 先执行 `Entry`，再只给它补发一次 `GameLaunched`。要在游戏启动时就初始化的 Mod，在 manifest 写 `"RequiresRestart": true`。

### 还没验证的

- 未验证：在 Proton、Wine、CrossOver 上运行游戏。多平台安装程序本身验过。

## 0.7.0

平台 API、作者 SDK 和多平台安装工具的源码。
