# DTMAPI 可选作者工具

SDK 提供便利命令，不参与 Runtime 准入。`netstandard2.0` 是本仓库游戏程序集的构建目标及作者推荐，不是第三方包的运行资格。普通 `netstandard2.0` 工程只需 API DLL 引用；游戏原生类型可引用本机程序集或使用独立原生引用转换器。无需 `dtmapi.author.json`、生成证明、SDK registry 或强制分析器。

## 最短路径

1. 准备 `references/DTMAPI.Abstractions.dll`，在 csproj 中用 `Reference` 引用并设置 `Private=false`。
2. `dotnet build MyMod.csproj`；将代码及内容整理成下面的包目录。
3. 将目录复制到官方 MODS，在游戏原生 Mod 页面启用；可选用 `dtmapi-author deploy package --game-root GAME`。

```text
package/
  info.json
  Content/DTMAPI/manifest.json
  Content/DTMAPI/MyMod.dll
  lib/private/                 # 按需私有依赖
  lib/shared/                  # 提供给声明依赖方的契约
  PackData/                   # 可选 Host 内容
```

```json
{"ManifestVersion":2,"UniqueID":"Author.MyMod","Type":"CodeMod","MinimumDTMApiVersion":"0.7.5","EntryDll":"Content/DTMAPI/MyMod.dll","EntryType":"Author.MyMod.ModEntry"}
```

`info.json` 保存 `name`、`author`、`version`、`description` 和官方展示信息。新格式只有这一份产品版本。旧格式继续从 manifest 读取版本。

## 可选命令

| 命令 | 作用 |
| --- | --- |
| `new codemod DIR --id Author.Mod --references API_DIR` | 创建普通工程和 package 目录 |
| `new contentpack DIR --id Author.Pack` | 创建内容目录；`--host` 可声明 Host |
| `build PROJECT` / `restore PROJECT` | 转交标准 dotnet 命令及参数 |
| `pack DIR --output FILE.zip` | 仅压缩现有目录，不编译；覆盖使用 `--replace` |
| `validate DIR` / `doctor DIR_OR_ZIP` | 按需检查并说明检查范围，不签发准入证明 |
| `deploy DIR_OR_ZIP --game-root GAME [--clean] [--target MODS_DIR]` | 默认覆盖本次产物；`--clean` 明确清空旧安装后重装，两者均不自动备份；`--target` 指定 MODS 下的安装目录 |
| `status ID` / `withdraw ID` / `recover ID` | 加 `--game-root GAME` 查询、撤回或恢复；不要求旧树摘要 |
| `native-references DLL... --output DIR` | 生成不随产品分发的编译引用 |
| `inspect-symbols` / `audio prepare` / `session` / `source-state` | 按各子命令帮助使用的独立辅助工具 |

部署不会代替玩家启用。普通覆盖会留下本次包未包含的旧文件，包括已经删除或改名的 DLL、资源；需要干净环境时使用 `--clean`，工具不维护历史文件归属账本。同名包内配置采用本次产物；目标已有根 `workshop.json` 始终保留，包外配置和启用状态不动。旧部署 `--replace` 仅为冗余覆盖确认，不代表备份或清空。

清空重装先在目标之外准备新包并带入工坊身份，再清空目标。失败遗留的 staging 是待安装的新包，不是旧安装备份，不能恢复清空前的设置；准备完成的 staging 可作为重试输入。使用错误提示中的完整重试命令，保留 `--target` 指定的原安装目录，即使旧 manifest 已删除，也不会改装到以 UniqueID 命名的另一目录。普通覆盖失败可能已写入部分文件，修正具体文件错误后重试即可。需要主动保存整份目录时使用 `withdraw`；`recover --replace` 仍会保留当前目录再恢复已存在的备份。status 中的恢复记录只表示上次实际保留/恢复操作。普通检查不计算目录树哈希，`doctor --deep` 才执行显式完整树比较。

## 分别交付

- API DLL/XML：仓库 `tools/scripts/build-api-references.ps1`。
- 本地作者 CLI：`tools/scripts/prepare-author-sdk.ps1` 或 `build-author-sdk.ps1` 只做标准增量构建，直接使用 `src/DTMAPI.AuthorSdk/bin/Release/net8.0`；按脚本输出的命令，用 `Get-DotNetExe` 选定的 .NET 8 执行 `dtmapi-author.dll`，普通验证不生成完整 SDK。转交 build/restore 时以 `DTMAPI_DOTNET_EXE` 指定同一 host。
- 完整 SDK：仅明确发布前检查时运行 `build-author-sdk.ps1 -ReleasePackage`（或 `prepare-author-sdk.ps1 -RequireReleaseArchive`），在固定 `dist/author-sdk` 更新一份目录和 ZIP。分析器与 .NET SDK 仅在该路径由 `-IncludeAnalyzer`、`-IncludeToolchain` 选择加入。
- 独立 Doctor：`DTMAPI.PlayerDoctor`，发行 SDK 的 `doctor/` 可单独使用，本地 CLI 准备不 publish 它。
- 原生引用转换器：`DTMAPI.NativeReferences`，不依赖作者 CLI。
- Runtime：构建 Bootstrap、RuntimeModules 及三个 Modules 原生实现，`install-to-game.ps1` 默认只安装 Runtime。

[迁移](MIGRATION.md) · [工程与引用](PROJECTS-AND-RESTORE.md) · [DLL 与依赖](PACKAGE-DEPENDENCIES.md) · [原生引用](NATIVE-REFERENCES.md) · [Content Host](CONTENT-HOSTS.md)

内置内容模块：[畜牧](ANIMAL-CONTENT.md) · [怪物](MONSTERS.md) · [音频](AUDIO-CONTENT.md)。普通包使用统一 content.json，高级作者直接使用纯契约 API。

仓库便利脚本 `build-mod-project.ps1` 仅在 `-ProjectPath` 明确指向 csproj 且未给出 `-BuildOutput` 时编译；目录输入不寻找工程。`-BuildOutput` 仅表示普通编译输出，依据 manifest 的 EntryDll 文件名取入口并放入声明路径，不求值工程或准备工具链。制作源中的 Content、PackData、lib 保持原义，Host 和 CodeMod 身份不转换它们。成品包直接交给 pack/deploy，原布局完整保留。脚本仍支持 `-BuildOnly`、`-NoZip` 和编译时显式 `-PrepareReferences`。0.7.5 已删除研究确定的旧公共 API 与诊断 DTO；当前能力以 API matrix 为准。
