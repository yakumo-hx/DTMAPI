我要为Doloc Town这款游戏制作完整功能、多mod的Doloc town ModdingAPI，简称DTMAPI。
DolocTown现在官方有创意工坊、并且只支持json文件的物品、配方、美化等增加物。但不支持功能性mod。
我手头有：多位不同作者写好的基于BeplnX5的功能性mod（n网/讨论区），我自己的多个功能性mod，游戏全部反编译信息，星露谷物语SMAPI。
我需要的：
codex搭建骨架，codex进行实际游戏测试hook，codex把现有的mod进行DMAPI更改，codex完成完整的DTMAPI（只需要支持Windows版本），实现全部SMAPI功能。
骨架必须可拓展，功能齐全（界面、自带功能性菜单mod接口）。
请你给出指导性意见。










## 总结判断

DTMAPI 最合理的形态不是“从零写一个替代 BepInEx 的加载器”，而是**用 BepInEx 作为底层注入/启动层，在其上建立 Doloc Town 专用的稳定 API、事件、内容包、配置菜单、日志、兼容性与工具链**。SMAPI 的价值也不是某几个类名，而是它把“加载代码 Mod、提供事件/API、错误拦截、兼容性检查、更新检查、内容包、日志工具”做成了完整生态；SMAPI 官方 README 也明确把这些列为核心职责。([GitHub](https://github.com/pathoschild/SMAPI))

你现在的优势很大：已有多作者 BepInEx 功能性 Mod、自己的功能性 Mod、反编译信息、ModDoctor、长期 debug 文档、SMAPI 可参考。但也正因为材料多，**最危险的路线是让 Codex 直接“实现全部 SMAPI 功能”**。应该先让 Codex 做一个可测试、可迁移、可回归的骨架，然后用现有 Mod 倒逼 API 成熟。

------

# 1. 先改目标：不是“复刻 SMAPI”，而是“功能等价 + Doloc Town 化”

Doloc Town 官方 Steam Workshop 方向目前公开描述的能力主要是替换纹理、制作物品、装饰、作物、配方、鱼、资源等内容型 Mod，这与你说的“官方只支持 json 内容扩展、不支持功能性 Mod”是吻合的。([蒸汽社区](https://steamcommunity.com/app/2285550)) 公开 Nexus 生态里也已经出现了 BepInEx 5 功能性 Mod，例如 ExpandedEncyclopedia；同时也有作者使用 BepInEx 6.x x64 Mono 做 Doloc Town 功能性 Mod。([Nexus Mods](https://www.nexusmods.com/doloctown/mods/2?tab=description)) 这说明 DTMAPI 必须处理两个现实问题：**官方内容 Mod 体系要兼容，现有 BepInEx 功能性 Mod 生态也要能迁移或共存**。

我的建议是把目标改成：

> DTMAPI = BepInEx Bootstrap + Doloc Town GameBridge + SMAPI-like Mod API + Content/Config/Menu/Log/Compatibility Toolchain。

不要一开始承诺“100% SMAPI API 兼容”。SMAPI 是 Stardew Valley/MonoGame 的生态，Doloc Town 是 Unity 游戏；你应该复刻的是**能力层级**，不是命名空间和实现细节。SMAPI 文档列出的基础 API 包括 Manifest、Events、Configuration、Content/Data、Input、Logging、Reflection、Translation、Update checks、Utilities，高级 API 包括 Content packs、Console commands、Mod integrations、Harmony patching；DTMAPI 可以按这个清单做自己的等价能力。([Stardew Valley Wiki](https://stardewvalleywiki.com/Modding%3AModder_Guide/APIs))

------

# 2. 推荐总架构

项目应拆成这些层：

```text
DTMAPI/
  src/
    DTMAPI.Abstractions/          # 公开给 Mod 作者引用的稳定 API；尽量少依赖游戏内部类
    DTMAPI.Core/                  # Mod 加载、Manifest、依赖排序、日志、配置、事件总线
    DTMAPI.BepInExBootstrap/      # 唯一放进 BepInEx/plugins 的启动插件
    DTMAPI.GameBridge.DolocTown/  # 所有 Doloc Town 反编译/Hook/Unity 适配都集中在这里
    DTMAPI.ModConfigMenu/         # 内置配置菜单/功能性菜单接口
    DTMAPI.ContentPatcher/        # 官方 json + DTMAPI content pack 的扩展加载器
    DTMAPI.ConsoleCommands/       # 控制台命令、调试命令、dump 命令
    DTMAPI.TemplateMod/           # 给作者复制的示例 Mod
  tools/
    DTMAPI.ModDoctor/             # 你已有 ModDoctor 的升级版
    DTMAPI.Packager/              # 打包、manifest 校验、发布前检查
    DTMAPI.LogParser/             # 类似 smapi.io/log 的本地日志解析器
  tests/
    DTMAPI.UnitTests/
    DTMAPI.IntegrationTests/
  testmods/
    HelloDtmMod/
    BrokenManifestMod/
    ConfigMenuExample/
    HookProbeMod/
  docs/
    architecture.md
    api-design.md
    hook-map.md
    compatibility.md
    migration-guide-bepinex-to-dtmapi.md
    DEBUG-DolocTown-SMAPI-CN.md
```

运行时目录建议：

```text
Doloc Town/
  BepInEx/
    plugins/
      DTMAPI/
        DTMAPI.BepInExBootstrap.dll
        DTMAPI.Core.dll
        DTMAPI.GameBridge.DolocTown.dll
  Mods/
    Author.ModName/
      manifest.json
      Author.ModName.dll
      config.json
      i18n/
      assets/
      data/
  DTMAPI/
    config.json
    compatibility.json
    logs/
    crash-reports/
    cache/
    reports/
```

关键点：**只有 DTMAPI 自己作为 BepInEx 插件启动；普通 DTMAPI Mod 不再放到 `BepInEx/plugins`，而是放到 `Mods/`**。这样你才能拥有类似 SMAPI 的 manifest、依赖管理、禁用 Mod、错误隔离、更新检查、内容包识别等能力。BepInEx 官方文档说明它本身已经提供插件加载、配置、日志、Harmony 运行时补丁等基础能力，所以 DTMAPI 不需要重复造最底层注入轮子。([BepInEx Docs](https://docs.bepinex.dev/))

------

# 3. DTMAPI 功能矩阵

| SMAPI 能力        | DTMAPI 对应设计                                              | 优先级 |
| ----------------- | ------------------------------------------------------------ | ------ |
| Mod 加载          | `Mods/<UniqueID>/manifest.json + EntryDll`，DTMAPI 自行加载 DLL | S      |
| Manifest          | `Name/Author/Version/UniqueID/EntryDll/Dependencies/UpdateKeys/MinimumApiVersion/MinimumGameVersion` | S      |
| 依赖排序          | 拓扑排序；缺依赖则跳过并给友好日志                           | S      |
| GameLoop 事件     | `GameLaunched/UpdateTicked/OneSecondUpdateTicked/SaveLoaded/SaveSaving/ReturnedToTitle` | S      |
| Input 事件        | 键鼠/手柄输入状态、按键抑制、快捷键注册                      | S      |
| Logging           | 每 Mod 独立 logger；全局日志；异常归属到 Mod                 | S      |
| Config            | `config.json` 自动读写、默认值、版本迁移                     | S      |
| 内置配置菜单      | 类似 Generic Mod Config Menu 的声明式 API                    | S      |
| Mod Registry      | `IsLoaded/Get/GetAll/GetApi<T>`，支持 Mod 间 API             | S      |
| Reflection Helper | 缓存字段/属性/方法访问，带友好错误                           | A      |
| Content/Data      | 读取、替换、编辑官方 JSON/图片/数据表                        | A      |
| Content Pack      | `ContentPackFor`，让非程序作者给 DTMAPI Mod 提供 JSON/图片   | A      |
| Console Commands  | `dtmapi list/errors/reload-content/dump-items/dump-scenes`   | A      |
| Update Checks     | Nexus / GitHub / Steam Workshop / 自定义 manifest            | B      |
| Compatibility DB  | 已知坏版本、替代版本、游戏版本警告                           | B      |
| Harmony 管理      | 允许高级 Mod 使用，但推荐通过 GameBridge 事件减少冲突        | B      |
| Save Data         | 全局数据、每存档数据、版本迁移                               | B      |
| Multiplayer       | 若 Doloc Town 无联机，可先 stub，保留接口                    | C      |
| Cross-platform    | 你明确只支持 Windows，可不做 Linux/macOS 兼容重写            | C      |

SMAPI 的 Manifest 设计非常值得借鉴：它要求每个 Mod 或内容包有 `manifest.json`，并用 `EntryDll` 或 `ContentPackFor` 区分代码 Mod 与内容包，还支持最低 API/游戏版本、依赖、更新检查等字段。([Stardew Valley Wiki](https://stardewvalleywiki.com/Modding%3AModder_Guide/APIs/Manifest)) DTMAPI 的 manifest 可以基本沿用这个思路，但字段名里把 `MinimumApiVersion` 理解为 `MinimumDTMApiVersion`。

------

# 4. 最小可用版本不要贪大：先做 DTMAPI 0.1

DTMAPI 0.1 的目标不是“所有功能齐全”，而是证明生态闭环：

**DTMAPI 0.1 验收标准：**

1. 游戏启动时 BepInEx 能加载 `DTMAPI.BepInExBootstrap`。
2. DTMAPI 能扫描 `Mods/`，解析 manifest，加载 `HelloDtmMod`。
3. `HelloDtmMod` 能收到 `GameLaunched`、`UpdateTicked`、`SaveLoaded` 至少三个事件。
4. 一个示例 Mod 能读写 `config.json`。
5. 内置配置菜单能显示该示例 Mod 的 bool/int/float/string/keybind 选项。
6. 一个故意报错的 Mod 不会直接炸掉游戏，而是被 DTMAPI 捕获、记录、必要时禁用。
7. `dtmapi list`、`dtmapi errors`、`dtmapi dump-hooks` 三个命令可用。
8. 你的一个自有功能性 Mod 被迁移为 DTMAPI Mod。
9. 退出游戏后不残留 `DolocTown.exe`，不复现你之前遇到的 Steam“等待游戏退出”问题。
10. `DTMAPI/reports/latest.zip` 能收集 `BepInEx/LogOutput.log`、`Player.log`、DTMAPI 日志、Mod 列表、hook 状态。

这一版完成后，DTMAPI 才算有资格扩展到内容包、兼容性数据库、更新检查、更多功能性菜单。

------

# 5. Hook 策略：GameBridge 是核心资产

你手头有全部反编译信息，但不要让每个 Mod 都直接依赖反编译类。正确做法是：

```text
游戏内部方法
   ↓ Harmony / Reflection / Unity callback
DTMAPI.GameBridge.DolocTown
   ↓ 稳定事件与服务接口
DTMAPI.Abstractions
   ↓
普通 DTMAPI Mod
```

也就是说，**所有脆弱 hook 都集中在 `DTMAPI.GameBridge.DolocTown`**。普通 Mod 作者只面对稳定 API，例如：

```csharp
public sealed class ModEntry : DtmMod
{
    public override void Entry(IDtmHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
    }
}
```

Harmony 补丁优先级建议：

| 场景                         | 推荐方式             |
| ---------------------------- | -------------------- |
| 只想知道某方法发生了         | Postfix              |
| 需要修改输入参数或阻止原方法 | Prefix               |
| 需要兜底捕获异常             | Finalizer            |
| 需要改 IL                    | Transpiler，最后手段 |

Harmony 官方文档把 Prefix、Postfix、Transpiler、Finalizer 作为主要 patch 类型；Transpiler 是高级场景，用于修改原方法 IL。([Harmony](https://harmony.pardeike.net/articles/patching.html)) 所以 Codex 做 hook 测试时，默认原则应是：**Postfix 先行，Prefix 谨慎，Transpiler 必须有 hook-map 记录和回归测试**。

每个 hook 都要登记到 `docs/hook-map.md`：

```markdown
## Hook: GameLoop.UpdateTicked

- Public event: helper.Events.GameLoop.UpdateTicked
- Game method:
- Patch type: Postfix
- Why this method:
- Tested game version:
- Tested DTMAPI version:
- Evidence:
  - Log line:
  - Screenshot:
  - Save file:
- Failure behavior:
  - If method missing, DTMAPI logs warning and disables this event.
- Mods depending on this hook:
  - Author.AutoFishing
  - Author.ActionSpeed
```

------

# 6. 内置功能性菜单：不要让每个 Mod 自己画 UI

DTMAPI 应内置一个类似 Generic Mod Config Menu 的“声明式配置菜单 API”。GMCM 在 Stardew Valley 生态中的作用就是给其他 Mod 提供游戏内配置 UI，并且只有“主动适配它的 Mod”才会显示配置。([Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/5098?utm_source=chatgpt.com)) DTMAPI 可以直接把这个能力做成核心组件，而不是额外依赖第三方菜单 Mod。

建议 API：

```csharp
public interface IDtmConfigMenuApi
{
    void Register(
        IManifest mod,
        Action reset,
        Action save,
        bool titleScreenOnly = false
    );

    void AddSectionTitle(IManifest mod, Func<string> text);
    void AddParagraph(IManifest mod, Func<string> text);

    void AddBoolOption(
        IManifest mod,
        Func<string> name,
        Func<string> tooltip,
        Func<bool> getValue,
        Action<bool> setValue
    );

    void AddNumberOption<T>(
        IManifest mod,
        Func<string> name,
        Func<string> tooltip,
        Func<T> getValue,
        Action<T> setValue,
        T min,
        T max,
        T interval
    );

    void AddTextOption(...);
    void AddKeybindOption(...);
    void AddChoiceOption(...);
    void AddButton(...);
}
```

菜单实现可以分两层：

```text
DTMAPI.ModConfigMenu
  ├─ 普通配置项页面
  ├─ Mod 启用/禁用页面
  ├─ 错误报告页面
  ├─ Hook 状态页面
  ├─ 日志导出按钮
  └─ 安全模式开关
```

你之前遇到过 UI/配置菜单边界问题，所以这里一定要把“配置菜单 API”和“自由绘制 UI API”区分开。普通 Mod 用声明式配置菜单；高级 Mod 才能用 `IDtmMenuApi` 创建自定义页面。

------

# 7. 内容包系统：把官方 Workshop JSON 纳入生态，而不是另起炉灶

官方已经有 JSON 内容 Mod 方向，DTMAPI 不应该和它对立。正确路线是做三件事：

第一，**读取并索引官方内容包**。DTMAPI 可以扫描本地官方 Mod/Workshop 目录，建立 item、recipe、texture、fish、crop 等索引，让功能性 Mod 能查询“当前所有内容”。

第二，**提供 DTMAPI ContentPatcher**。借鉴 Content Patcher 的思想：内容包是一个文件夹，包含 `manifest.json`、`content.json`、`assets/`，通过 `Action/Target/FromFile/When/Priority` 描述修改。Content Patcher 文档中的内容包结构就是 `manifest.json + content.json + assets/`，并用 `Format` 控制格式版本。([GitHub](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md))

第三，**把你已有 ModDoctor 升级为发布前校验器**。你之前已经把 ModDoctor 定位为“作者发布前必跑工具”，这应该成为 DTMAPI 生态的核心，而不是附属脚本。建议新增：

```text
dtmapi doctor <mod-folder>
dtmapi doctor --all
dtmapi doctor --workshop-dir <path>
dtmapi pack <mod-folder>
dtmapi validate-manifest <manifest.json>
dtmapi dump-official-schema
```

------

# 8. Codex 的工作方式：必须“任务小、证据硬、可回归”

Codex 官方说明里强调，它可以读取、修改并运行当前目录代码，也能运行测试、linters、type checkers；同时 OpenAI 也明确建议用户通过终端日志和测试结果验证 Codex 的工作，并人工审查代码。([OpenAI开发者](https://developers.openai.com/codex/cli)) 所以你不能给 Codex 一个巨型任务：“做完整 DTMAPI”。你要给它一连串**可验收的垂直切片**。

你的仓库根目录应该有 `AGENTS.md`，强制 Codex 每次先读规则：

```markdown
# AGENTS.md

你正在开发 Doloc Town ModdingAPI（DTMAPI）。

## 必读文件
- docs/architecture.md
- docs/api-design.md
- docs/hook-map.md
- docs/DEBUG-DolocTown-SMAPI-CN.md
- docs/migration-guide-bepinex-to-dtmapi.md

## 禁止事项
- 不允许把普通 DTMAPI Mod 放进 BepInEx/plugins。
- 不允许在 DTMAPI.Abstractions 暴露大量 Doloc Town 反编译内部类型。
- 不允许新增 Harmony Transpiler，除非同时更新 docs/hook-map.md 并添加回归测试。
- 不允许删除已有 debug 记录。
- 不允许硬编码我的 Steam 游戏路径；必须使用 local.settings.json 或环境变量。
- 不允许把游戏 DLL、反编译源码、第三方闭源 Mod 打进发布包。

## 每次任务必须完成
1. 说明修改目标。
2. 编译项目。
3. 运行可用测试。
4. 若涉及游戏测试，运行 scripts/run-game-smoke.ps1。
5. 更新 docs/hook-map.md 或 DEBUG 文档。
6. 输出修改摘要、测试结果、未解决问题。
```

Codex 的本地脚本建议：

```text
scripts/
  build.ps1
  test.ps1
  install-to-game.ps1
  run-game-smoke.ps1
  collect-logs.ps1
  kill-game.ps1
  status.ps1
  package-report.ps1
```

`run-game-smoke.ps1` 不应该让 Codex“玩游戏”，而是做自动化冒烟测试：

1. 复制 DTMAPI 到游戏目录。
2. 启动游戏。
3. 等待 `BepInEx/LogOutput.log` 出现 DTMAPI 启动行。
4. 等待 `DTMAPI/logs/latest.log` 出现 `GameLaunched`。
5. 可选：检测主菜单场景或指定 UI 文本。
6. 关闭游戏。
7. 检查进程是否退出。
8. 打包日志。

Codex App/CLI 支持终端、Git diff、worktree、PowerShell/Windows sandbox 等工作流，所以你这个项目非常适合用“每个 hook 一个 worktree / 每个迁移 Mod 一个任务”的方式推进。([OpenAI开发者](https://developers.openai.com/codex/app/features))

------

# 9. 给 Codex 的阶段任务拆分

## 阶段 A：骨架

给 Codex 的任务：

```text
请搭建 DTMAPI 解决方案骨架，但不要实现复杂 hook。

要求：
1. 新建 src/DTMAPI.Abstractions、DTMAPI.Core、DTMAPI.BepInExBootstrap、DTMAPI.GameBridge.DolocTown、DTMAPI.ModConfigMenu。
2. DTMAPI.BepInExBootstrap 是唯一 BepInEx 插件。
3. 启动时输出 DTMAPI 版本、游戏路径、BepInEx 版本、Mods 路径。
4. 创建 Mods/Author.HelloDtmMod 示例，但先不动态加载 DLL，只生成结构和 manifest。
5. 添加 scripts/build.ps1、scripts/install-to-game.ps1、scripts/collect-logs.ps1。
6. 添加 docs/architecture.md、docs/hook-map.md、AGENTS.md。
7. 编译通过。
```

验收：游戏启动日志中能看到：

```text
[DTMAPI] Bootstrap loaded.
[DTMAPI] GamePath = ...
[DTMAPI] ModsPath = ...
[DTMAPI] API Version = 0.0.1
```

## 阶段 B：Manifest + Mod 加载

```text
实现 DTMAPI manifest 解析和代码 Mod 加载。

要求：
1. 支持 Name、Author、Version、Description、UniqueID、EntryDll、MinimumApiVersion、MinimumGameVersion、Dependencies、UpdateKeys、ExtraFields。
2. 支持 Mods/<folder>/manifest.json。
3. 实现 DtmMod 基类和 IDtmHelper。
4. 加载 HelloDtmMod.dll，并调用 Entry(helper)。
5. 缺依赖、重复 UniqueID、manifest JSON 错误时不崩溃，输出友好错误。
6. 添加 BrokenManifestMod、MissingDependencyMod 测试样例。
```

SMAPI 的 manifest/dependency 设计是很好的参照：依赖缺失时不加载并给玩家友好信息，可选依赖则存在时优先加载。([Stardew Valley Wiki](https://stardewvalleywiki.com/Modding%3AModder_Guide/APIs/Manifest))

## 阶段 C：GameLoop 最小事件

```text
实现第一批 Doloc Town GameBridge hook。

要求：
1. GameLaunched：所有 Mod Entry 完成后触发。
2. UpdateTicked：游戏 update loop 触发，但要限流日志，避免刷屏。
3. OneSecondUpdateTicked：每秒触发。
4. ReturnedToTitle 或 MainMenuLoaded：能找到则实现，找不到就在 hook-map.md 记录原因。
5. 所有事件 handler 必须 try/catch，异常归属到具体 Mod。
6. 更新 docs/hook-map.md。
```

## 阶段 D：配置与内置菜单

```text
实现 DTMAPI config 读写和内置配置菜单 API。

要求：
1. helper.ReadConfig<T>()、helper.WriteConfig<T>()。
2. config.json 不存在时写入默认值。
3. config 版本迁移接口。
4. DTMAPI.ModConfigMenu 提供 Register/AddBoolOption/AddNumberOption/AddTextOption/AddKeybindOption。
5. ConfigMenuExample 展示所有选项。
6. 菜单打开、保存、重置均有日志。
```

## 阶段 E：迁移第一个自有 Mod

```text
把我的一个自有 BepInEx 功能性 Mod 迁移到 DTMAPI。

要求：
1. 保留原功能。
2. BaseUnityPlugin 改为 DtmMod。
3. BepInEx Config.Bind 改为 helper.Config。
4. ManualLogSource 改为 helper.Monitor。
5. Harmony PatchAll 尽量改为 DTMAPI 事件；无法替代的 patch 通过 helper.Patching 注册，并写入 hook-map.md。
6. 添加 migration-notes.md，列出迁移前后差异。
7. 游戏中实际测试并收集日志。
```

------

# 10. 现有 BepInEx Mod 迁移规则

迁移时不要一上来改所有 Mod。先建表：

| Mod                  | 作者 | 权限   | BepInEx 版本 | 是否有源码 | 功能类型 | Hook 类型      | 迁移难度 | 处理策略                |
| -------------------- | ---- | ------ | ------------ | ---------- | -------- | -------------- | -------- | ----------------------- |
| AutoFishing          | 自己 | 可改   | 5            | 有         | QoL      | 输入/钓鱼      | 中       | 迁移为 DTMAPI           |
| ActionSpeed          | 自己 | 可改   | 5            | 有         | 数值修改 | Prefix/Postfix | 中       | 迁移为 DTMAPI           |
| ExpandedEncyclopedia | 他人 | 看权限 | 5            | 未必       | UI/百科  | UI/数据        | 高       | 先做兼容检测            |
| Infinite Hover       | 他人 | 看权限 | 6            | 未必       | 载具能力 | Prefix/Postfix | 中       | 不直接改包，做 API 对照 |

公开 Nexus 页面里，有些 Doloc Town Mod 明确写了“不得修改/转换/重新上传”等权限限制。([Nexus Mods](https://www.nexusmods.com/doloctown/mods/5)) 所以公开发布 DTMAPI 版迁移时，要优先迁移**你自己的 Mod**，第三方 Mod 更适合作为“兼容性测试样本”和“API 需求来源”；除非作者授权或源码许可证允许，不要把别人的 Mod 改完重新发布。

迁移模式建议：

| BepInEx 写法                     | DTMAPI 写法                                                  |
| -------------------------------- | ------------------------------------------------------------ |
| `BaseUnityPlugin.Awake()`        | `DtmMod.Entry(IDtmHelper helper)`                            |
| `Logger.LogInfo(...)`            | `helper.Monitor.Log(...)`                                    |
| `Config.Bind(...)`               | `helper.Config.Get/ReadConfig/WriteConfig`                   |
| `Update()`                       | `helper.Events.GameLoop.UpdateTicked`                        |
| `Keyboard.current` / Unity input | `helper.Input`                                               |
| `Harmony.PatchAll()`             | 优先 DTMAPI 事件；必要时 `helper.Patching`                   |
| `AccessTools.Field(...)`         | `helper.Reflection.GetField<T>(...)`                         |
| 自己画配置 UI                    | `helper.ModRegistry.GetApi<IDtmConfigMenuApi>()` 或内置 `helper.ConfigMenu` |

------

# 11. 兼容性与安全模式必须早做

DTMAPI 要比普通 BepInEx 插件更“生态化”。至少要有：

```json
{
  "KnownBrokenMods": [
    {
      "UniqueID": "Author.ModName",
      "Versions": ["<1.2.0"],
      "Reason": "Breaks save loading on Doloc Town 0.95.16",
      "SuggestedFix": "Update to 1.2.0 or disable this mod."
    }
  ],
  "KnownConflicts": [
    {
      "A": "Author.FastAction",
      "B": "Other.Author.SpeedPatch",
      "Reason": "Both patch the same action speed method."
    }
  ]
}
```

启动时支持：

```text
--dtmapi-safe
--dtmapi-disable-all-mods
--dtmapi-only Author.ModName
--dtmapi-dump-hooks
--dtmapi-no-update-check
```

再加一个玩家能理解的错误报告：

```text
DTMAPI detected 2 mod errors.

1. Author.AutoFishing
   Error: NullReferenceException in GameLoop.UpdateTicked
   Action: Disabled this event handler after 3 repeated failures.

2. Author.FastAction
   Error: Missing game method PlayerAction.DoAction
   Action: Hook disabled. This mod may need an update for current Doloc Town version.
```

SMAPI 的错误拦截、兼容性提示、更新提示是它成功的重要原因之一；DTMAPI 如果只做 hook，不做错误归属和恢复，玩家会把所有崩溃都归咎于 DTMAPI。

------

# 12. 不建议做的事

第一，不建议让 DTMAPI 一开始替代 BepInEx。BepInEx 已经解决注入、日志、配置、Harmony 集成等底层问题；DTMAPI 应该站在它上面做 Doloc 专用抽象。([BepInEx Docs](https://docs.bepinex.dev/))

第二，不建议普通 Mod 继续散落在 `BepInEx/plugins`。那样 DTMAPI 无法统一处理 manifest、依赖、禁用、错误隔离、更新检查。

第三，不建议让 Codex 直接跑“全自动大改”。Codex 很适合搭骨架、迁移小 Mod、写测试、整理日志，但每一步都要通过编译、日志、游戏冒烟测试和人工 review。OpenAI 也明确建议通过测试结果、终端日志验证 Codex 输出，并人工审查 agent 生成代码。([OpenAI](https://openai.com/index/introducing-codex/))

第四，不建议在公开 API 里暴露大量 Doloc Town 反编译内部类型。游戏更新时这些类型最容易变，应该用 `GameBridge` 适配。

第五，不建议一开始追求热重载代码 Mod。BepInEx/Mono 下 DLL 卸载很麻烦，先支持“内容热重载”和“配置热重载”，代码 Mod 改动要求重启游戏。

第六，不建议直接复制 SMAPI 源码。SMAPI 是开源项目，但它的仓库标注为 LGPL-3.0 license；学习架构没问题，直接复制代码会带来许可证义务和适配成本。([GitHub](https://github.com/pathoschild/SMAPI))

------

# 13. 最推荐的推进顺序

你现在最应该按这个顺序做：

1. **DTMAPI 0.0.1：BepInEx Bootstrap + 日志 + 目录结构。**
2. **DTMAPI 0.0.2：manifest 解析 + HelloDtmMod 动态加载。**
3. **DTMAPI 0.0.3：GameLaunched / UpdateTicked / SaveLoaded 三个事件。**
4. **DTMAPI 0.0.4：config.json + 内置配置菜单。**
5. **DTMAPI 0.0.5：迁移一个你自己的最小功能性 Mod。**
6. **DTMAPI 0.1：迁移 3 个自有 Mod + ModDoctor 集成 + 日志打包。**
7. **DTMAPI 0.2：ContentPatcher / Workshop JSON 索引 / 内容包系统。**
8. **DTMAPI 0.3：兼容性数据库 / 更新检查 / 安全模式。**
9. **DTMAPI 0.4：公开模板、文档、示例 Mod、作者指南。**

你想做“完整功能、多 Mod 的 DTMAPI”，真正的起点不是写很多 hook，而是先把**Mod 生命周期、Manifest、日志、配置、错误隔离、HookMap、Codex 回归测试**定下来。只要这个骨架稳定，后面 AutoFishing、ActionSpeed、OneActionComplete、百科扩展、UI 菜单、内容包加载、创意工坊校验都能逐个迁移进去。











问题一：“不建议让 DTMAPI 一开始替代 BepInEx 的意思是”，可以替代么？比如SMAPI？
问题二：不应该让 Codex“玩游戏”，是否说明codex没法自动测试游戏内更多的功能、更多的hook？
问题三：现在需要的是创意工坊下载、允许允许一次安装脚本，然后可以打开DTMAPI的界面看已安装的mod；所有功能性mod在创意工坊下载后，自然可以被识别到。
问题三额外：最好可以走官方的mod开启关闭路径，不用单独在DTMAPI的界面进行。
问题四：/goal模式下，你认为一次目标最大可完成度、需要触发人审核是什么情况。









## 问题一：DTMAPI 可以替代 BepInEx 吗？像 SMAPI 那样？

**可以，但不建议第一阶段就替代。**

要分三层看：

```text
底层注入层：把代码塞进游戏进程
中层运行层：加载 Mod、打补丁、隔离错误、管理日志
上层生态层：Manifest、配置菜单、内容包、更新检查、作者工具
```

BepInEx 主要强在前两层：它本身就是 Unity / XNA / .NET 游戏的插件与 patching 框架，支持 Unity Mono、IL2CPP 等方向；并且 BepInEx 已经内置 HarmonyX / MonoMod.RuntimeDetour 一类运行时 patch 能力。([GitHub](https://github.com/bepinex/bepinex)) ([BepinEx 文档](https://docs.bepinex.dev/master/articles/dev_guide/runtime_patching.html))

SMAPI 更接近“完整生态层 + 自己的启动器”。Windows 上 SMAPI 安装后可以通过 `StardewModdingAPI.exe` 启动，也可以配置 Steam 默认先启动 SMAPI，再启动游戏，以保留 Steam overlay、成就和游戏时间统计。([Stardew Valley Wiki](https://stardewvalleywiki.com/Modding%3AInstalling_SMAPI_on_Windows))

所以 DTMAPI 有三种路线：

| 路线                | 形态                                          | 适合阶段   | 判断                          |
| ------------------- | --------------------------------------------- | ---------- | ----------------------------- |
| A. BepInEx 上层 API | DTMAPI 作为 BepInEx 插件启动                  | 现在最适合 | 最稳、最快、最容易让 Codex 测 |
| B. DTMAPI 启动器    | `DolocTownModdingAPI.exe` 启动游戏并注入/加载 | 中后期     | 更像 SMAPI，但工程量大        |
| C. 完全自研注入器   | 不依赖 BepInEx，自己做 Doorstop/注入/patch    | 不建议早做 | 风险高，收益低                |

我的建议是：**第一阶段用 BepInEx；第二阶段把 BepInEx 封装进 DTMAPI 安装器；第三阶段再考虑是否做独立启动器。**

最终玩家感知可以是：

```text
玩家下载 DTMAPI Installer
↓
Installer 自动安装 BepInEx + DTMAPI Bootstrap
↓
玩家从 Steam 正常启动 Doloc Town
↓
DTMAPI 自动识别创意工坊功能性 Mod
```

也就是说，**技术上仍用 BepInEx，产品上叫 DTMAPI**。这比“立刻替代 BepInEx”更合理。

------

## 问题二：Codex 不能“玩游戏”，是不是不能自动测试更多功能和 hook？

不是。我的意思是：**不要让 Codex 像真人一样靠肉眼玩游戏、随机操作、主观判断功能是否正常。**

Codex 能做的是：

```text
启动游戏
读取日志
运行测试脚本
安装/卸载测试 Mod
触发输入
等待场景变化
检查进程是否退出
检查 hook 是否命中
检查存档/配置/导出文件是否变化
收集截图或日志
```

所以 Codex 可以自动测试更多 hook，但前提是你要给它做一套 **HookProbe / SmokeTest / TestMod**。

比如：

```text
testmods/
  DTMAPI.HookProbe/
    manifest.json
    HookProbe.dll
```

HookProbe 启动后记录：

```text
[HookProbe] GameLaunched OK
[HookProbe] MainMenuLoaded OK
[HookProbe] SaveLoaded OK
[HookProbe] InventoryOpened OK
[HookProbe] ItemAdded OK
[HookProbe] RecipeCrafted OK
[HookProbe] SceneChanged: Farm -> Town
[HookProbe] PlayerInput: F8
```

然后 Codex 的测试标准不是“我看起来游戏能玩”，而是：

```text
scripts/run-game-smoke.ps1
scripts/run-hook-probe.ps1
scripts/assert-log.ps1 DTMAPI/logs/latest.log "InventoryOpened OK"
```

这样 Codex 可以测试很多游戏内功能。它不能稳定完成的是：

```text
像真人一样判断 UI 好不好看
随机探索游戏并发现隐藏 bug
靠视觉判断复杂交互是否正确
长时间游玩验证平衡性
```

所以你要把“玩游戏”改造成“游戏内自动化验证”。

------

## 问题三：创意工坊下载后自然识别功能性 Mod，是否可行？

**可行，而且这应该成为 DTMAPI 的核心路线。**

推荐架构是：

```text
一次性安装：
  DTMAPI Installer
    ├─ 安装 BepInEx
    ├─ 安装 DTMAPI Bootstrap
    ├─ 写入 DTMAPI 配置
    └─ 验证 Steam 启动路径

后续 Mod 安装：
  玩家在 Steam 创意工坊订阅功能性 Mod
    ↓
  Steam 下载 Workshop Item
    ↓
  DTMAPI 启动时扫描官方 Workshop 目录
    ↓
  识别 dtmapi.manifest.json
    ↓
  加载 DLL / 内容包 / 配置菜单项
```

Steamworks 的 UGC API 支持获取用户订阅项目、已下载项目、项目安装路径、项目状态；`GetSubscribedItems` 会返回当前用户订阅的项目，默认排除本地禁用项目，`GetItemInstallInfo` 可以拿到已安装创意工坊项目的本地文件夹路径。([Steamworks](https://partner.steamgames.com/doc/api/ISteamUGC)) ([Steamworks](https://partner.steamgames.com/doc/api/ISteamUGC))

功能性 Mod 的 Workshop 包建议长这样：

```text
WorkshopItem/
  dtmapi.manifest.json
  Author.ModName.dll
  assets/
  i18n/
  config.schema.json
  preview.png
  official-content-marker.json   # 可选，用于让官方 Mod 列表识别
```

`dtmapi.manifest.json` 示例：

```json
{
  "Name": "Auto Fishing",
  "Author": "AuthorName",
  "Version": "1.0.0",
  "UniqueID": "AuthorName.AutoFishing",
  "Type": "CodeMod",
  "EntryDll": "Author.AutoFishing.dll",
  "MinimumDTMApiVersion": "0.1.0",
  "MinimumGameVersion": "0.9.x",
  "LoadFrom": "Workshop",
  "Dependencies": [
    {
      "UniqueID": "DTMAPI",
      "MinimumVersion": "0.1.0",
      "Required": true
    }
  ]
}
```

但是有一个关键现实：**Steam Workshop 能下载文件，但不能替玩家自动把 BepInEx/DTMAPI Bootstrap 安装进游戏根目录。** 所以“第一次安装脚本”仍然必要。之后功能性 Mod 走创意工坊订阅即可。

------

## 问题三额外：最好走官方 Mod 开启关闭路径，不在 DTMAPI 界面单独开关

这个方向非常对。推荐策略是：

```text
官方 Mod 管理器 = 启用/禁用入口
DTMAPI 界面 = 状态、配置、错误、依赖、日志、说明
```

实现方式：

1. DTMAPI 启动时通过 Steam UGC 获取订阅项目。
2. 默认只加载“未被官方/Steam 本地禁用”的 Workshop Item。
3. DTMAPI 界面显示这些 Mod，但不作为主要启停入口。
4. 如果用户要禁用，提示去官方 Mod 界面或 Steam 创意工坊管理。
5. 只有手动安装到 `Mods/` 的非 Workshop Mod，才在 DTMAPI 里提供本地禁用。

Steam UGC API 本身有“本地禁用”概念：`GetNumSubscribedItems` 默认会排除 locally disabled items，`SetItemsDisabledLocally` 也允许把项目设置为本地禁用状态。([Steamworks](https://partner.steamgames.com/doc/api/ISteamUGC))

所以 DTMAPI 可以尊重官方路径：

```text
官方启用 → DTMAPI 加载
官方禁用 → DTMAPI 不加载
DTMAPI 检测到依赖缺失 → DTMAPI 报错但不越权启用/禁用
```

需要注意一点：如果 Doloc Town 官方 Mod 列表只识别官方 JSON 内容包，不识别 DLL 包，那就需要功能性 Mod 同时带一个“官方内容包壳”。例如：

```text
official.json      # 让官方列表显示这个 Mod
dtmapi.manifest.json
Author.Mod.dll
```

这样玩家在官方 Mod 界面里看到它，DTMAPI 再从同一个 Workshop 文件夹里识别功能性部分。

------

## 问题四：`/goal` 模式下，一次目标最大可完成度、什么时候需要人审核？

Codex Goals 适合“目标明确、但路径不确定”的多步任务。官方说明里，Goal 的核心是定义完成条件、验证方式、约束、边界、迭代策略和停止条件；它不是无边界后台自动化，而是一个有证据标准的完成契约。([OpenAI 开发者](https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex))

对 DTMAPI 来说，**一次 `/goal` 最大合理目标**大概是一个“垂直切片”，不是整个 DTMAPI。

### 合理的一次 `/goal`

例如：

```text
/goal 完成 DTMAPI 0.0.2：实现 manifest 解析、Mods 目录扫描、HelloDtmMod 动态加载、重复 UniqueID/缺依赖/JSON 错误处理，并通过 build、unit tests、run-game-smoke.ps1 验证。不得修改 GameBridge hook，不得引入 Transpiler。若游戏启动失败或无法确认日志证据，停止并报告 blocker。
```

这个目标很适合 Codex，因为它有：

```text
明确产物：manifest + loader + 示例 Mod
验证方式：编译、测试、游戏日志
边界：不碰复杂 hook
停止条件：游戏启动失败或证据不足
```

### 不合理的一次 `/goal`

```text
/goal 实现完整 DTMAPI，支持所有 SMAPI 功能，迁移所有 Mod，并发布到创意工坊。
```

这太大，Codex 会开始“看起来很努力”，但很难保证每一步都是可验证的。

------

## 我建议的 `/goal` 最大颗粒度

| 目标                                                  | 是否适合一次 `/goal` | 原因                              |
| ----------------------------------------------------- | -------------------- | --------------------------------- |
| 搭建 DTMAPI 解决方案骨架                              | 适合                 | 文件结构和编译可验证              |
| 实现 manifest + loader                                | 适合                 | 单一闭环                          |
| 做 GameLaunched / UpdateTicked / SaveLoaded 三个 hook | 适合                 | hook 证据清楚                     |
| 实现配置系统 + 配置菜单                               | 适合，但偏大         | UI 需要人工验收                   |
| 迁移一个简单 BepInEx Mod                              | 适合                 | 功能边界清楚                      |
| 迁移 5 个 Mod                                         | 不适合               | 变量太多                          |
| 实现完整 SMAPI 等价功能                               | 不适合               | 范围失控                          |
| 创意工坊识别 + 官方启停兼容                           | 适合拆成 2-3 个 Goal | Steam/官方 UI/DTMAPI 三方交互复杂 |
| 做独立 DTMAPI 启动器                                  | 单独 Goal            | 风险高，不能和其他目标混做        |

------

## 什么时候必须触发人工审核？

这些情况应该强制 Codex 停下，让你看：

1. **要改启动链路时**
   包括 Steam 启动参数、BepInEx 安装路径、Doorstop 配置、游戏根目录文件。
2. **要新增 Transpiler 或 IL Hook 时**
   Postfix/Prefix 可以较快尝试；Transpiler 可能在游戏更新后直接炸，必须人工确认。
3. **要改存档、经济数值、物品生成、背包数据时**
   这类 bug 可能破坏玩家存档，必须先备份和人工确认。
4. **要加载 Workshop DLL 时**
   功能性 Mod 是可执行代码，必须有安全提示、manifest 校验、日志归属。不能让 Codex 随便“自动信任所有 DLL”。
5. **要改官方 Mod 启用/禁用逻辑时**
   DTMAPI 应该尊重官方路径，不要越权覆盖官方状态。
6. **测试结果只有“编译通过”，没有游戏日志证据时**
   对 DTMAPI 来说，编译通过只算 30%。必须有游戏实际启动日志、hook 命中日志、退出清理日志。
7. **Codex 判断“功能完成”，但没有明确证据文件时**
   官方 Goals 文档也强调，Goal 的完成必须对照文件、测试、日志、benchmark、产物等具体证据，而不是模型觉得“大概完成了”。([OpenAI 开发者](https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex))

------

## 最适合你现在的决策

你现在应该把产品路线定成：

```text
DTMAPI 不是替代创意工坊；
DTMAPI 是让创意工坊获得功能性 Mod 能力。

第一次：
  玩家运行 DTMAPI Installer

之后：
  玩家照常从 Steam 创意工坊订阅 Mod
  官方 Mod 列表负责启用/禁用
  DTMAPI 自动识别其中的功能性 Mod
  DTMAPI 界面负责配置、错误、依赖、日志、诊断
```

技术路线则是：

```text
短期：BepInEx 底层 + DTMAPI 生态层
中期：DTMAPI Installer 隐藏 BepInEx 复杂度
长期：可选 DTMAPI 独立启动器，但不急
```

这条路线最像“SMAPI 的用户体验”，但不会一开始就背上“重写 BepInEx”的巨大风险。