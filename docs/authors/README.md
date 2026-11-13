# DTMAPI 作者指南

DTMAPI 让你给《多洛可小镇》写两种 Mod：用 C# 写的代码 Mod，和只放 JSON、图片、声音的内容包。两种都放进游戏的官方 MODS 目录，在游戏自带的 Mod 页面里启用。

## 写一个代码 Mod

需要：Windows，.NET 8 SDK，装好 DTMAPI 0.7.5 的游戏。

1. 下载 `DTMAPI-AuthorSdk-0.7.5.zip` 并解压。`dtmapi-author.exe` 是作者工具；`references/` 里是编译时引用的 DTMAPI 程序集。
2. 新建工程：

   ```powershell
   .\dtmapi-author.exe new codemod D:\MyMods\Hello --id YourName.Hello
   ```

   生成的工程已经引用好 `references/DTMAPI.Abstractions.dll`，入口在 `ModEntry.cs`。
3. 编译：`dotnet build D:\MyMods\Hello`。编译出的 DLL 会复制到 `package/Content/DTMAPI/`。
4. 关掉游戏，部署：

   ```powershell
   .\dtmapi-author.exe deploy D:\MyMods\Hello\package --game-root "D:\Steam\steamapps\common\Doloc Town"
   ```

5. 启动游戏，在标题页打开 Mod 页面，勾选你的 Mod，关闭页面。Mod 当场加载。日志 `<游戏目录>/DTMAPI/logs/latest.log` 里会有 `Mod loaded.`。

最小的入口是这样：

```csharp
using DTMAPI.Abstractions;

namespace DtmApiMod
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.Monitor.Log("Mod loaded.");
            helper.Events.GameLoop.GameLaunched += (_, __) => helper.Monitor.Log("标题页已就绪。");
        }
    }
}
```

改了代码以后：重新编译、关掉游戏、再部署、再启动。

## 包里有什么

```text
package/
  info.json                    名称、作者、版本、说明
  Content/DTMAPI/manifest.json DTMAPI 读的加载信息
  Content/DTMAPI/YourMod.dll   入口程序集
  lib/private/                 只给自己用的 DLL（可选）
  lib/shared/                  给依赖你的 Mod 用的 DLL（可选）
  PackData/content.json        动物、怪物、声音内容（可选）
```

`manifest.json`：

```json
{
  "ManifestVersion": 2,
  "UniqueID": "YourName.Hello",
  "Type": "CodeMod",
  "MinimumDTMApiVersion": "0.7.5",
  "EntryDll": "Content/DTMAPI/YourName.Hello.dll",
  "EntryType": "DtmApiMod.ModEntry"
}
```

| 字段 | 说明 |
| --- | --- |
| `ManifestVersion` | 写 2。名称和版本从 `info.json` 读。 |
| `UniqueID` | 全局唯一的 ID，建议用"作者名.Mod 名"。 |
| `Type` | `CodeMod` 或 `ContentPack`。 |
| `MinimumDTMApiVersion` | 你的 Mod 需要的最低 DTMAPI 版本。玩家的版本更低时，Mod 不加载，管理页会写原因。 |
| `EntryDll`、`EntryType` | 入口程序集和入口类。只有代码 Mod 需要。 |
| `RequiresRestart` | 写 `true` 时，勾选后要重启游戏才加载。省略就是勾选后当场加载。 |
| `Dependencies` | 依赖的其他 Mod，见[DLL 与依赖](PACKAGE-DEPENDENCIES.md)。 |

编译时引用的 DTMAPI 程序集、游戏程序集、Unity、Harmony、BepInEx 都由游戏和 DTMAPI 提供，引用时设 `Private=false`，不要复制进包里。要引用游戏自己的类型，见[游戏程序集引用](NATIVE-REFERENCES.md)。

## 加载和重启

- 在标题页的 Mod 页面勾选、关闭页面后，新勾选的 Mod 当场加载：先执行 `Entry`，再只给它补发一次 `GameLaunched`。标题页要做的初始化放在这两处。
- 要在游戏一启动就打补丁或初始化的 Mod，在 manifest 写 `"RequiresRestart": true`。
- 取消勾选、换版本、换来源，都要重启游戏才生效。一次运行里，同一个 Mod 只加载一次。
- `Entry` 抛出异常时，Mod 页面会显示原因，这次运行不再重试。

## 作者工具命令

| 命令 | 作用 |
| --- | --- |
| `new codemod DIR --id ID` | 新建代码 Mod 工程。`--references` 可以换一个引用目录。 |
| `new contentpack DIR --id ID` | 新建内容包目录。 |
| `build` / `restore` | 原样转给 `dotnet build` / `dotnet restore`。 |
| `deploy DIR_OR_ZIP --game-root GAME` | 把包装进官方 MODS 目录。游戏要先关掉。 |
| `pack DIR --output FILE.zip` | 把包目录压成 ZIP，不编译。 |
| `validate DIR` | 检查 manifest 和入口路径，说明哪里不对。 |
| `status` / `withdraw` / `recover ID --game-root GAME` | 查看、移走、放回已部署的包。 |
| `native-references DLL... --output DIR` | 生成编译用的游戏程序集引用。 |
| `symbols DLL [--pdb PDB]` | 确认 PDB 和 DLL 出自同一次构建。 |
| `audio-prepare FILE --use effect --output DIR` | 把一个音频文件预先做成游戏用的声音包，见[音频](AUDIO-CONTENT.md)。 |

`deploy` 会覆盖包里有的文件，包里没有的旧文件留在原处。删过或改过名的 DLL 想一起清掉，加 `--clean`。两种方式都不备份旧文件；目标目录里已有的 `workshop.json` 会保留。

## 内容

- [内容包](CONTENT-PACKS.md)：不写代码，只用 `PackData/content.json`。
- [畜牧](ANIMAL-CONTENT.md)、[怪物](MONSTERS.md)、[音频](AUDIO-CONTENT.md)：三个内置内容模块。
- [Content Host](CONTENT-HOSTS.md)：让别的 Mod 给你的代码 Mod 提供内容。

## 其他

- [API 状态](API-STATUS.md)：哪些接口稳定，哪些还会变。
- [从 0.7.0 迁移](MIGRATION.md)
- [工程和还原](PROJECTS-AND-RESTORE.md)、[平台服务](PLATFORM-SERVICES.md)、[弹窗](UI-MODAL-SESSIONS.md)、[反射](REFLECTION.md)
- 从源码构建 SDK 和 Runtime，见 [build/README.md](../../build/README.md)。
