# 从 0.7.0 迁移到 0.7.5

0.7.5 是一个破坏性版本。用 0.6.0 及以后的 API 编译、又没用到下面删掉的接口的代码 Mod，不用改就能继续加载。用到了删掉的接口，或者想换新的 manifest 格式，按这里改。

## 工程

1. 用普通的 `Microsoft.NET.Sdk` 工程，目标 `netstandard2.0`。删掉旧 SDK 要求的 Author props/targets、`NoStdLib`、`dtmapi.author.json` 和编译时的检查 target。
2. DTMAPI、游戏、Unity、Harmony、BepInEx 的程序集都用普通 `Reference`，设 `Private=false`。要用游戏的非公开类型，用 `dtmapi-author native-references` 生成引用，见[游戏程序集引用](NATIVE-REFERENCES.md)。
3. 先 `dotnet build`，再整理包目录。`pack` 只压缩已有的目录，不再替你编译；旧的 `--build-output`、`--offline` 和 policy 参数都没有了。
4. SDK 只带 0.7.5 的引用程序集，不再能选旧的 API 版本编译。

## manifest

新格式写 `"ManifestVersion": 2`，名称、作者、版本、说明放进包根的 `info.json`：

```json
{ "name": "Hello", "author": "YourName", "version": "1.0.0", "description": "一句话说明。" }
```

- 新格式只有 DTMAPI 0.7.5 能读。玩家还在用 0.7.0 时，用新格式的 Mod 不会加载。
- 旧格式（不写 `ManifestVersion`）照样能读，版本仍从 manifest 读。
- 新格式不再需要 `CodeModKind`、`NativeContractVersion`、`DependencyContractVersion`。旧包里生成的依赖清单、引用记录文件也不再需要。
- 新格式默认的 Harmony ID 是 `<UniqueID>.Native`。想用别的，写 `HarmonyOwner`。

## 部署

- `deploy` 用目录或 ZIP 都行。它覆盖包里有的文件，包里没有的旧文件留着；要清掉旧文件，加 `--clean`。两种方式都不备份。
- `--clean` 失败时，按报错里给的完整命令重试，保留其中的 `--target`。报错里提到的临时目录装的是新包，不是旧安装的备份。
- 旧的 `--replace` 只是确认覆盖，不等于 `--clean`。

## 删掉的接口

| 删掉的 | 改用 |
| --- | --- |
| `IContentQueryHelper` 的 `GetIndexedItems`、`GetAllIndexedItems`、`GetIndexedItem`、`GetAnyIndexedItem`，以及 `IContentItemInfo` | 直接读游戏的原生物品表，名称读当前的原生 Title。`FindAssets`、`GetKnownContentTypes`、`TryReadTextAsset` 还在，它们只查已加载的 Mod 包里的文件。 |
| `IMailDeliveryApi`、`MailItemDeliveryRequest`、`MailItemDeliveryResult` | 自己调用游戏的邮件接口。 |
| `CropHarvestScope` 和旧的 Include 过滤开关 | `ICropHarvestingApi` 还在。`MaxHarvests` 大于 0 时按你给的数量收，省略或不大于 0 时全收。 |
| `IEquipmentSlotsApi` | 更多装备槽 2.0 不再通过平台提供接口。 |
| `DtmApiDisposition` 和它的 attribute；`DtmApiStatus` 的 `Proposed`、`Verified`、`Disabled`、`StableCandidate` | 只剩 `Stable` 和 `Experimental`，见 [API 状态](API-STATUS.md)。 |
| 作者会话和 SDK 的 `session` 命令 | 改完重新编译、部署、重启游戏，看日志。标题页的命令输入框和 `IDtmCommands` 还在。 |
| 0.7.5 删掉的其他旧类型 | 清单在 [RetiredPublicTypes.txt](../../tests/DTMAPI.Core.Tests/RetiredPublicTypes.txt)，不会恢复，也没有转发的空壳。 |

## 行为变化

**勾选后当场加载。** 在标题页的 Mod 页面勾选、关闭页面后，新勾选的 Mod 当场加载：先执行 `Entry`，再只给它补发一次 `GameLaunched`。别的 Mod 不会再收到 `GameLaunched`，也不补发 `SaveLoaded`。之后发出 `ModListChanged`。

- 需要在游戏一启动就打补丁或初始化的 Mod，在 manifest 写 `"RequiresRestart": true`。
- 依赖的 Mod 要等重启，依赖它的 Mod 也一起等。缺的依赖装上以后再加载。
- 取消勾选的 Mod 这次继续运行，重启后停用；重启前勾回来，就不用重启。换版本或换来源要重启。
- `ModRegistry.IsLoaded` 和 `GetApi` 反映这次实际加载了什么。

**事件和线程。** 公开 API 只能在主线程调用；后台线程的结果用 `IDtmScheduler` 交回主线程。事件在主线程上同步发出，回调里新增或取消的订阅从下一次事件开始生效。`UpdateTicked` 每帧只发一次。

**自定义动物和换音效。** 0.7.0 的 `Content/DTMAPI/custom-animals.json`、`audio-replacements.json`，以及 `ICustomAnimalApi`、`IAudioReplacementApi` 都删掉了，0.7.5 不再读这两个文件。改用 `PackData/content.json` 里的 `animals`、`audio` 两节，或者代码里的 `IAnimalContentApi`、`IAudioContentApi`：

- 动物没有 `aiTemplate` 了。AI 日程由官方动物表的 `schedule_id` 决定；PNG 动物直接用原生的成年、幼体动画机地址。见[畜牧](ANIMAL-CONTENT.md)。
- 换音效写成 `audio.events` 里带 `replaceNativeEvent` 的事件。见[音频](AUDIO-CONTENT.md)。

**怪物。** 怪物模块是 0.7.5 新加的，还是 Experimental，见[怪物](MONSTERS.md)。
