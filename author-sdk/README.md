# DTMAPI Author SDK

Author SDK 通过 `dtmapi-author` 创建、校验、构建、打包、部署和诊断 Mod。SDK 工具版本为 0.7.0，默认使用完整 M3 API 0.7.0，需要 Runtime 0.7.0；保留的旧 target 按各自目录范围使用。Windows x64 完整 ZIP 包含自包含 .NET 8 CLI 和 .NET SDK 8.0.421 工具链，生成的游戏加载程序集保持 `netstandard2.0`。

Windows 与多平台 Runtime 0.7.0 已在工坊发布，包含 marker 文件开头的 UTF-8 BOM 兼容修复，见[玩家包发布记录](https://github.com/yakumo-hx/DTMAPI/blob/main/docs/updates/2026/20260911-0011-runtime-y-hotfix-publication.md)。本工具包基于已验收的 D7，随包 Doctor 纳入主线的 UTF-8 BOM marker 修复；开发工具的支持范围为 Windows Developer Preview。SDK 下载与 SHA-256 见[0.7.0 发行说明](https://github.com/yakumo-hx/DTMAPI/releases/tag/v0.7.0)。SDK 是作者开发工具，不能代替玩家 Runtime，也不安装到 `BepInEx/plugins`。

新工程使用 author schema 4 和标准 MSBuild。普通 DLL Mod 从 Strict CodeMod 开始；需要本机游戏原生类型时，阅读 [Advanced 原生引用](NATIVE-REFERENCES.md)。0.7.0 新项目使用 `NativeContractVersion=2`，支持任意合法作者 ID；旧 V1 与 receipt 包保留各自校验路径。[包依赖与共享 DLL](PACKAGE-DEPENDENCIES.md)说明当前依赖格式，旧二进制和包 reader 仍遵循原契约。

## 从解压到第一个包

1. 将完整 SDK ZIP 解压到独立开发目录，不要在压缩包内运行，也不要放入游戏目录。确认同一 SDK 根目录下有 `dtmapi-author.exe`、`Enter-DtmApiEnvironment.ps1` 和 `toolchain`。
2. 在 SDK 根目录打开 PowerShell，点调用环境脚本。开头的点和后面的空格都要保留。
3. 切换到准备存放源码的目录，创建工程、恢复依赖并打包。下面的目录仅为示例，请换成自己的开发目录。

```powershell
# 当前目录是完整解压后的 SDK 根目录。
. .\Enter-DtmApiEnvironment.ps1

# 换成自己已有的源码目录。
Set-Location 'D:\ModProjects'
dtmapi-author new codemod MyMod --id Author.MyMod --name "My Mod" --author Author
dtmapi-author validate MyMod
dtmapi-author restore MyMod --json
dtmapi-author pack MyMod --json
```

`MyMod` 是作者根目录，保存 `dtmapi.author.json`、`manifest.json` 和新建工程。修改生成的入口源码即可开始写 Mod。默认版本的包输出为 `MyMod/dist/Author.MyMod-0.1.0.zip`，准确路径和哈希以 pack 报告为准。`pack` 已经编译一次，不必先重复运行 build；只想检查编译时，可单独运行 `dtmapi-author build MyMod`。

Debug 与 Release、离线恢复分别使用下列命令。它们是不同用途的示例，不要求依次执行：

```powershell
dtmapi-author build MyMod --configuration Debug --json
dtmapi-author pack MyMod --configuration Release --symbols true --json
dtmapi-author restore MyMod --offline true --json
dtmapi-author pack MyMod --offline true --json
```

Debug 包包含 portable PDB；Release 需要 `--symbols true` 才包含。离线模式仅使用随包本地源和已有缓存，其他作者依赖应提前准备。新工程需要先 restore 生成并核对 `packages.lock.json`，pack 不会替你静默选择新依赖版本。

从加载了 SDK 环境的 PowerShell 启动 IDE，例如已安装 VS Code 时运行 `code MyMod`。CLI 与 IDE 使用同一 MSBuild 后端。工具链、子目录工程、普通库、生成器、输出报告及已测 IDE 限制见[工程与恢复](PROJECTS-AND-RESTORE.md)。生成符号不代表游戏 shipping Mono 支持断点。

## 选择 API target

[target-catalog.json](target-catalog.json)定义默认及可选 target、每份载荷的不可变契约和支持的包 Runtime 范围。省略 target 使用 0.7.0；`new --api-target 0.5.5` 显式选择保留目标。目录中“可用”表示可构建，不表示该历史版本单独公开过。`build` 和 `pack` 读取工程的 `targetDtmApiVersion`，不从已安装游戏或 Runtime 推断 target。作者输入 schema 与 API target 相互独立，首发 SDK 只接受一种作者输入格式。

保留的内部 0.6.2 契约合并 Experimental [平台服务](PLATFORM-SERVICES.md)与[反射](REFLECTION.md)。当前 API 不提供绑定玩法存档的数据能力。更早未发布的 M2/反射候选只保留为仓库历史，不是随 SDK 分发的 target；旧哈希和包不会重新标记身份。

新包声明的最低 Runtime 必须位于所选 target 的目录范围，低于编译 API target 会报错。reader 继续兼容此前 SDK 0.1.0 / API 0.5.5 Strict、ContentPack 包的旧最低版本行为及 receipt 恢复；Advanced 策略要求仍生效。这不会改变冻结载荷。显式 `--compatibility-root` 或 `DTMAPI_AUTHOR_COMPAT_ROOT` 指定的目录是唯一选定来源；文件缺失、不匹配或被修改会失败，不会暗中改用其他目录。可执行文件内嵌目录及原始兼容契约，修改分发 JSON 不能授权新 target。

`session prepare --api-target <version>` 请求同一个可用 API target，同时独立协商会话协议。旧 Advanced 引用策略仍绑定原 target 和 receipt。

Strict 用于公共 DTMAPI/BCL 与受支持的托管依赖。任意合法作者 ID 都可通过显式本机游戏引用使用当前 Advanced 原生契约路线；旧 receipt 路线仍与产品身份绑定，复制其他产品策略或 receipt 会被拒绝。两条路线都不允许分发游戏/平台 DLL。

Strict 编译引用及最终 PE 依赖闭包会拒绝未获准的宿主程序集。工程求值和诊断由标准 MSBuild 负责，不会根据注释或同名用户类型猜测引用。[工程与恢复](PROJECTS-AND-RESTORE.md)说明标准扩展点、运行资产限制和发布检查。

`DTMAPI.Abstractions` 是公共程序集，不代表每个 public 类型都 Stable。使用前请同时查看随包 [API 状态](API-STATUS.md)中的 stability 和 disposition，它由仓库权威矩阵生成。Diagnostic、Frozen、Disabled、DTMAPI-internal 和 Proposed 契约不应作为新普通 Mod 的依赖。已有冻结 API 使用者请看[迁移指南](MIGRATION.md)，其中区分玩家产品替代、不可用能力和保留 ABI/数据，也记录历史 CSV 删除例外，没有宣布新移除日期。

## 构建、打包与诊断命令

更多创建方式：

```powershell
dtmapi-author new codemod NativeObserver --id Pine.NativeObserver --name "Native Observer" --author Pine --code-mod-kind Advanced --game-root 'D:\Games\Doloc Town'
dtmapi-author new contentpack MyPack --id Author.MyPack --name "My Pack" --author Author
dtmapi-author hash MyMod/dist/Author.MyMod-0.1.0.zip
dtmapi-author doctor MyMod/dist/Author.MyMod-0.1.0.zip --json
```

`--json` 输出机器可读报告。Strict build 使用完整 SDK 自带 .NET SDK 8.0.421 的标准 MSBuild/Csc 和哈希固定的 API/BCL 引用，不要求系统 SDK 或游戏目录。普通 restore/build/pack 可能访问作者配置的 NuGet 源；`--offline true` 只使用本地源和已准备缓存，但不能限制任意作者 target 自行访问网络。

当 `projectFile` 在子目录时，元数据留在作者根目录，普通 items 和中间输出遵循 csproj 目录，详见[工程路径](PROJECTS-AND-RESTORE.md)。Advanced 使用 `new --game-root` 保存或命令中显式提供的本机游戏根目录。原生契约记录准确宿主身份、路径和哈希，并从编译输出推导必需签名；旧 receipt 工程保留 tracked policy/构建限制。CLI 不扫描其他游戏安装或无关工作区来寻找原生程序集。已测游戏兼容范围见[原生引用](NATIVE-REFERENCES.md)。

Doctor 不构建、不执行作者代码，只读取包及程序集元数据；成功不证明任意 IL 或包可以在游戏运行。pack 根据实际引用成员及字段访问指令检查最终运行实现，实际加载、行为和调试仍需代表性游戏验证。

调用方同时需要 DLL/PDB 时，使用 `pack MyMod --build-output path/to/compiled --json`，不要先重复 build。报告保留 ZIP 的 `outputPath`/`sha256`，并提供 `values.buildOutputPath`、`buildOutputSha256`、`buildFactsPath`、`buildFactsSha256`、`buildBackend` 和 `compilerSha256`，对应实际编译。ContentPack 不接受 `--build-output`。

仓库产品 wrapper 先运行一次 `prepare-author-sdk.ps1`，再通过 `-AuthorSdkRoot` 复用明确输出。准备过程检查 SDK 源码/依赖图、编译器、构建属性及打包输入；普通产品源码改变不重建 SDK。`preflight-workspace.ps1 -Operation BuildProduct -CatalogId more-saves` 只报告缺失环境及分开的编译引用/测试游戏路径，不创建输出。正式发行验证仍会打包两次检查确定性；这些维护者脚本不属于随包 CLI。

## 包结构与身份

`pack` 生成官方包结构：

```text
Package/
  info.json
  dtmapi-dependencies.json   # 当前依赖契约包
  dtmapi-native-build.json   # 当前 Advanced V1/V2 包
  Content/DTMAPI/
    manifest.json
    Author.Mod.dll          # 仅 CodeMod
    dtmapi-package.json     # 仅元数据，不是所有权收据
    ...content files
```

生成的依赖/原生元数据路径相对于包根目录，并记录在 `dtmapi-package.json`。旧 receipt 包保留原布局，应读取 marker 中的实际路径，不要手动移动生成文件。

marker 文件名早于 SDK 就存在。Runtime 和 Doctor 能识别有界历史安装器/内容元数据及第三方 ID/版本形式，但不会授予 SDK 绑定或安装所有权；这些旧包不需重写 marker。带 schema 的 SDK marker、依赖/原生契约或损坏绑定仍使用准确校验器，不能退回历史格式。

`manifest.json` 拥有 Mod 身份、Mod 版本、依赖及最低 Runtime。`dtmapi.author.json` 只是 SDK 专用的 pre-1.0 构建/发布输入，不替代 Runtime manifest。包内 `info.json` 从两者生成，不要在作者工程中手工维护。

当前 Advanced 工程无需第一方 registry 行。生成的来源绑定覆盖包、准确本机宿主引用、必需签名、`netstandard2.0`、Harmony owner 和最低 Runtime；受支持私有/共享托管库由依赖清单描述。游戏、Unity、Harmony、BepInEx 和 DTMAPI 平台 DLL 不能作为作者依赖入包，生成的元数据引用视图只用于编译。

旧 receipt Advanced 工程保持准确 tracked policy 和现行作者 registry；Core/Doctor 也保留已发 receipt 的历史接受 registry，但历史行不能授权新 receipt。这些身份专属规则不限制当前自助原生契约路线。SDK 没有 upload、credential、force 或 adopt 操作。受管 Strict/Advanced Mod 不得在 `BepInEx/plugins` 下创建、构建、打包或部署；直接放在那里的是 DTMAPI 所有权范围外的 External BepInEx Plugin。

## 部署到游戏与撤回

先按玩家渠道安装 Runtime，构建 Mod 包后退出游戏，再部署。下面的 `$gameRoot` 是示例游戏安装路径，应替换为本机实际路径：

```powershell
$gameRoot = 'D:\Games\Doloc Town'
dtmapi-author deploy MyMod/dist/Author.MyMod-0.1.0.zip --game-root $gameRoot
dtmapi-author deployment-status Author.MyMod --game-root $gameRoot
```

部署后从 Steam 正常启动游戏，在官方 Mod 界面启用该 Mod。SDK 不编辑 `SAVE/mod_infos.json`。磁盘文件存在或部署成功不证明当前进程已加载；代码更新后应重启，再核对实际加载来源和版本。

更新时先修改 manifest 版本、restore/pack 新包，退出游戏，再运行：

```powershell
dtmapi-author update MyMod/dist/Author.MyMod-0.2.0.zip --game-root $gameRoot
dtmapi-author withdraw Author.MyMod --game-root $gameRoot
# 仅在恢复已准备但中断的事务时使用：
dtmapi-author recover Author.MyMod --game-root $gameRoot
dtmapi-author doctor $gameRoot --json
```

这些是独立管理操作，不应依次全部执行。`deploy` 创建新的受管包；`update` 要求已有准确 receipt。`install-local` 另要求预期 ID、版本和 ZIP SHA-256。官方安装与 Runtime 使用同一 `DTMAPI_DOLOC_PERSISTENT_ROOT` 覆盖及 Windows LocalLow 默认值解析 profile 根目录。`deployment-status` 分开报告磁盘清单和官方启用状态；选定来源、驻留 DLL 需要活动 `session snapshot`，否则不可用。

官方 journal schema 4 使用已有事务/清单字段，不带历史复合 `localInstall`，receipt 绑定 `OfficialLocal/<UniqueID>`。reader 核对当前解析的官方根目录、保留完整清单及准确前置事务；未知文件、错误根目录、访问失败或 journal 不匹配都会保留现场并拒绝。暂存与恢复留在官方 MODS 卷。Windows 冷态修改在提交/回滚期间持有 SDK 操作锁和游戏可执行文件独占句柄，拒绝游戏运行中或同时启动的情况。当前不支持非 Windows 修改操作，不能由 netstandard2.0 推断平台支持。

0.6.0 起不再发现历史 `game/Mods/<UniqueID>` 开发目录。当前 `deploy`、`update` 和 `install-local` 写入官方 profile 的 `MODS/<UniqueID>`，识别为 `Local.<UniqueID>`；任意合法作者 ID 均可使用，无需 Catalog 注册。历史来源的 `source local select` 仍暂停并返回 `SDK003`，不能把旧目录当作玩家加载或发布证据。

## 旧 receipt 部署恢复

已有 receipt 绑定部署仍可恢复。`deployment-status`、`install-local-status` 只读；`recover` 恢复已准备但中断的事务；`withdraw` 将精确核验的已提交包移入同卷保留恢复区；`source local clear` 可移除陈旧的历史选择，但不移动包文件。这些兼容命令不会让 `game/Mods` 重新成为 Runtime 来源。

历史包内 `.dtmapi-author-receipt.json`、包外已提交 journal 和完整部署清单必须完全一致；清单包括未知文件和空目录。每个游戏根目录有独占操作锁。历史 staging、destination、failed 和 recovery 目录保持在 `Mods` 卷，使已准备事务能按原原子移动语义恢复。不匹配会保留证据并拒绝；缺失、伪造、错根、错路径、错 ID、错 kind、漂移、并发、仅 receipt 或仅 journal 的状态都不会被接管。

Current journal writes performed by legacy recovery/withdraw use schema 3.

旧恢复/撤回写入 schema 3 journal，保留 schema-2 的部署身份和清单字段，并新增可空 `localInstall` 事务，记录已准备复合本地安装的准确 journal/source-state 前像及 SDK 所有路径。

Package-local receipts and package markers remain schema 2.

包内 receipt 和 package marker 仍是 schema 2，不随 journal 写入版本改变。

reader 只为玩家机器上已存在的准确 SDK 0.1.0 schema-1 及此前 schema-2 journal 保留有限升级路径。历史 schema-1 CodeMod 一律解释为 Strict，ContentPack 仍不含代码。journal、包内 receipt、旧 marker、身份、目标路径、包哈希、载荷哈希和完整清单必须一致。`deployment-status` 不改写历史状态；`recover`/`withdraw` 可写 schema-3 journal，同时保留旧 receipt 权威。历史 journal 不能授权官方更新，先恢复/撤回旧部署。schema 1 不能声明或升级自身为 Advanced；未知或混合版本字段拒绝处理。

`DTMAPI_AUTHOR_STATE_ROOT` 可迁移包外测试/CI 状态。未指定时，状态位于 `%LOCALAPPDATA%/DTMAPI/AuthorSdk/state/installations/<game-root-key>`。key 为规范化并转大写的游戏根路径之 SHA-256 前 16 字节的小写表示。

## 来源模式

以下是参数形式说明，尖括号和方括号不是要原样输入的路径：

```text
dtmapi-author source local select Author.MyMod <game/Mods/Author.MyMod> --game-root <game>
dtmapi-author source local clear Author.MyMod --game-root <game>
dtmapi-author source workshop prepare Author.MyMod --game-root <game>
dtmapi-author source workshop clear Author.MyMod --game-root <game>
dtmapi-author source reproduction begin --game-root <game>
dtmapi-author source reproduction restore <snapshotId> --game-root <game>
dtmapi-author source status [Author.MyMod] --game-root <game>
```

历史 Local Development 在外部 `source-state.json` 记录准确 `DTMAPI-FileTree-SHA256-v1` 摘要；0.6.0 起仅用于恢复，不授予文件所有权或玩家来源选择。新 local select 暂停，status/clear 仍可用。Player Reproduction 先写外部快照，再原子清除所有 override；直到显式恢复准确快照前，拒绝新选择。Workshop Validation 只准备状态并报告 Runtime 捕获的原生快照是否可用；离线目录存在不等于原生 Workshop 验证成功。

## 显式 Runtime 会话

`session prepare` 必须在启动游戏前运行。它写一次性 schema 2 `author-session.json` 和受保护 `author-session-client.json`，再通过认证 hello 协商协议/能力。编译 target 与 hello 返回的实际 Host 版本相互独立；有界 schema-1 适配器保留已知旧 SDK 线路值。字段、兼容、生命周期和超时诊断见[会话协议](SESSION-PROTOCOL.md)。

```powershell
# 在启动游戏前执行；gameRoot 沿用前面设置的本机安装目录。
dtmapi-author session prepare --game-root $gameRoot
# 然后从 Steam 启动游戏。下面使用默认官方 profile 路径；
# 如设置了 DTMAPI_DOLOC_PERSISTENT_ROOT，请改为实际选定的 Mod 根目录。
$selectedRoot = Join-Path $env:USERPROFILE 'AppData\LocalLow\RedSawGames\DolocTown\MODS\Author.MyMod'
dtmapi-author session snapshot Author.MyMod $selectedRoot --game-root $gameRoot
# 仅支持的非代码内容可重载；代码包可能返回 restart-required。
dtmapi-author session reload Author.MyMod $selectedRoot --game-root $gameRoot
dtmapi-author session clear --game-root $gameRoot
```

Runtime 只原子消费一次启动描述符。客户端凭据在到期或 `session clear` 前用于显式 snapshot/reload。到期且匹配的状态在下次 prepare/request 清除；格式错误或不匹配的状态保留，需显式 clear。token 只在认证 JSONL 管道请求中传送，并从人类/JSON 报告、Runtime 消息及响应值中脱敏。

每个业务请求都包含准确 UniqueID、选定根目录绝对路径、新 request ID 和当前文件树哈希。响应的协议/Host/会话/请求/操作/owner 身份必须匹配协商会话。传输失败报告 `host-unavailable`、`handshake-timeout`、`pipe-response-timeout`、`pipe-response-identity-mismatch` 等稳定代码；仅缺少 listener 不意味着必须升级。Runtime 结果仍为 `ok`、`rejected`、`restart-required` 或 `error`；有效 `restart-required` 带明确警告，仍需重启。

CLI 不启动游戏、不安装 watcher、不轮询 Runtime 启动、不上传 DLL/原生数据，也不在缺少有效受保护 token 时发送请求。Custom Animals、CodeMod DLL、官方原生 JSON 和未知格式仍需重启；只有 Runtime 拥有经审查的 reload 实现。

## Doctor、符号与实际加载状态

`dtmapi-author symbols <DLL> [--pdb <path>] --json` 对照 DLL 的 CodeView GUID/stamp 核验 portable PDB，报告双方哈希、可与驻留模块比较的 MVID 和源码文档名。符号缺失或不匹配返回 SDK191；它证明产物配对，不证明调试器能连接。源码路径遵循标准编译和作者 PathMap，应把准确源码、构建报告与 DLL/PDB 一起保存。

已测 Windows shipping Mono 标准 MSBuild 产物中，Entry fixture 定位到实际抛出行 `ModEntry.cs:6`，事件 fixture 定位到 `ModEntry.cs:13`。更早内部 SDK fixture 即使符号匹配也曾报告回调结束行，因此不承诺任意回调/构建都能精确定位抛出行。结合 owner、事件名、错误信息和配套源码判断。事件错误保留在 `helper.Diagnostics.GetErrors()` 和既有 `ExportLogs()` 报告中，不一定重复写入文本日志，应在进程仍运行时查看 Errors 页或导出。

导出报告保存在本机，可能含可识别机器路径、历史日志及包含内存的 crash dump，分享前检查 ZIP；不会自动上传。早期受测导出未发现 archive、完整 Mod 配置或会话凭据文件条目，但这不是对 dump 内容的隐私保证。PDB 生成不承诺断点连接能力。

活动 `session snapshot` 分开显示 `officialEnabled`、`ownerActive`、`diskEntrySha256`、`diskEntryMvid` 和 `residentEntryMvid`。`residentMatchesDiskMvid` 比较模块身份，不是 Mono 内存哈希；无法观察驻留状态时会明确报告。Entry 失败后，即使 owner 不活动，程序集也可能仍驻留。代码或符号替换需要重启。

生成包的 `info.json` 包含当前原生上传的全部本地化字段，避免原生发现过程迁移文件而破坏准确 receipt。若旧候选已被游戏改写，status/update 正确拒绝这种漂移；不要 force/adopt。保留证据，只在原授权范围内恢复准确已知测试资产后再恢复事务。新生成包无需此项修复。

`dtmapi-author doctor <path>` 默认输出人类可读报告，`--json` 使用 Doctor 报告 schema。Doctor 是单一 Author SDK CLI 内的 0.1.0 支持库，不是第二个 apphost。它用 PEReader 和共享读取文件流，不使用 Assembly.Load，也不移动、删除、接管、启停或执行被检查二进制。

## 冻结兼容载荷

发行流程在自包含 CLI 旁的 `compatibility/0.5.5` 放置以下内容：

- 带准确相对路径/SHA-256 清单的 `compatibility.json`；
- 由 DTMAPI 自有源码为 Runtime 0.5.5 构建的 `DTMAPI.Abstractions.dll`，不从玩家安装复制；
- `DTMAPI.Author.props`；
- `NETStandard.Library 2.0.3` 的 netstandard2.0 引用程序集及许可/声明文件。

CLI 内嵌 `compatibility.contract.json` 作为独立信任依据，校验固定 Abstractions、props、许可、声明、准确 114 文件引用清单、每个 release-manifest 哈希、必需 kind 和没有多余内容的完整文件树。即使同时替换 `compatibility.json` 和载荷，也不能替换内嵌契约；缺失或修改过的兼容载荷会被拒绝。

以下仅供源码维护者使用，随包使用者无需重建引用。Runtime 源码继续演进时，0.5.5 作者载荷保持冻结。仓库 `tools/scripts/prepare-author-sdk-compatibility.ps1` 根据 `author-sdk/compatibility/0.5.5/source-build.json` 的冻结 DTMAPI 自有输入构建准确 DLL，在 `.tools/author-sdk-compatibility/0.5.5` 准备完整载荷，浅克隆或源码归档即可使用。recipe 固定原 16 项编译输入、Release 设置、PathMap 和 .NET SDK 8.0.421，结果必须匹配既有契约 SHA-256；不会把当前 Runtime 接口重新编译后冒充旧 target。

完整 SDK builder 调用同一准备步骤并复用已验证载荷。显式 `-FrozenAbstractionsDll`/`DTMAPI_AUTHOR_SDK_FROZEN_ABSTRACTIONS_DLL` 仍接受准确 SHA 校验，不隐式搜索 dist、相邻历史产物或玩家安装。`-Check` 只检查，不构建或下载。定向源码测试先准备载荷，可用 `DTMAPI_AUTHOR_COMPAT_ROOT` 指定另一明确输出根。NETStandard.Library 2.0.3 从公共 NuGet 源进入本机缓存，按既有引用/许可/声明哈希核验；冻结源码目录不跟踪第三方二进制或游戏材料。
