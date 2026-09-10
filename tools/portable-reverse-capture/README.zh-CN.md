# 多洛可小镇：基线捕获与分阶段续跑

这个压缩包用于在另一台 Windows x64 电脑上，把**当前安装的多洛可小镇 Steam 版本**冻结成一份可长期比较的本地研究基线。

压缩包本身只有 DTMAPI 自写脚本和说明，**不包含游戏文件、解包内容、反编译源码或第三方工具程序**。运行时会从官方地址下载并校验固定版本的 AssetRipper、ILSpy 和 .NET 运行时。

## 最简单的用法

1. 把整个压缩包解压到剩余空间至少 **12 GiB** 的磁盘；不要解压进游戏目录。
2. 退出多洛可小镇，确认游戏进程已经关闭。
3. 双击 `1_RUN_FULL_UNPACK.bat`。
4. 保持网络连接并等待完成。第一次会下载约数十 MiB 的工具，完整导出可能需要很久。
5. 成功后，窗口会显示 `Portable capture completed (scope=full)` 和最终目录。

默认结果位于：

```text
references\doloc-town\reverse\builds\<SteamBuild>_<分支>_<Assembly哈希前6位>\
```

结果目录里最重要的内容：

- `raw-snapshot/`：当前官方玩家文件的逐字节冻结副本，不含 BepInEx、DTMAPI、Mods 和存档；
- `asset-ripper-unity-project/`：AssetRipper 恢复的 Unity 工程，包含场景、贴图、配置、预制体、Addressables/StreamingAssets 等；
- `decompiled/Assembly-CSharp/`：ILSpy 恢复的主要游戏 C# 工程；
- `decompiled/Assembly-CSharp-firstpass/`：若该程序集存在，则一并反编译；
- `full-baseline-inventory/`：文件 SHA-256 清单、原文件/冻结副本一致性、场景映射和工具回执；
- `asset-ripper/`、`code-decompile/`：工具身份和运行日志。
- `stage-receipts/`：阶段输入、固定工具身份、参数、输出哈希与 complete/running/failed 状态；`.stage-failures/` 保留重跑前的不完整或损坏结果。

## 自动找不到游戏时

在本工具脚本旁创建 UTF-8 文本文件 `GAME_PATH.txt`，内容只写游戏目录的绝对路径，例如：

```text
D:\SteamLibrary\steamapps\common\Doloc Town
```

也可以在 PowerShell 中明确传入路径和输出目录：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-doloctown-full-capture.ps1 `
  -GameDir "D:\SteamLibrary\steamapps\common\Doloc Town" `
  -BuildRoot "E:\DolocTown-Reverse-Baselines\pre-release-build"
```

`-BuildRoot` 必须是专用目录，不能是游戏目录、游戏目录的父目录或子目录。默认拒绝覆盖已有结果；显式续跑会验证完整结果，失败阶段先保留原残留，再单独重做。

## 状态、续跑和只处理代码

查看已有回执，不定位游戏、不启动工具、不下载、不计算文件树哈希，也不改写任何清单：

```powershell
powershell.exe -NoProfile -File .\run-doloctown-full-capture.ps1 -BuildRoot "E:\DolocTown-Reverse-Baselines\pre-release-build" -Status
```

Status 明确标记 `not-checked-by-status`；旧 summary 仅显示历史记录，目录存在不能证明完整。

继续处理已冻结的输入，默认不再要求它匹配 Steam 更新后的当前安装：

```powershell
powershell.exe -NoProfile -File .\run-doloctown-full-capture.ps1 -BuildRoot "E:\DolocTown-Reverse-Baselines\pre-release-build" -Resume
```

只需要反编译时，在同一命令加 `-CodeOnly`。已有新回执时仅验证托管代码输入，跳过资源导出和资源树验证；普通符号查询直接查已有 metadata/decompiled，无需运行本工具。旧 `-ReuseSnapshot`、`-ReuseExport`、`-ReuseDecompile` 都作为 Resume 兼容入口，统一执行验证，不再只判断目录。

复用需要输入身份、工具版本/包哈希、参数、complete 状态和对应输出的路径集合/长度/SHA-256 一致。只有受影响阶段重跑，其他完整阶段保留；缺失/损坏/多余文件、工具参数改变、中断 running/failed 均不能直接复用。旧基线只在现存原始清单和工具回执足够、输出实核通过时导入新回执，不给缺证目录补写 PASS。

- 快照阶段中断或损坏时，加 `-GameDir` 指向原来相同的来源来重试；必须与该阶段记录的 manifest/主程序集身份一致。无法证明同一输入时保留原目录，选择新的 BuildRoot。
- `-ReuseBuildRoot "另一份基线"` 可复用另一份已验证的反编译结果；要求程序集、可解析的托管引用闭包、ILSpy 身份和参数全同。Windows PowerShell 在进程内按已验证 DLL SHA 缓存引用名称，避免重复 reflection-only 加载扩大依赖范围；同名不同内容或宿主不支持只读反射时保守比较全部 managed 输入。结果复制到独立目录，原来源不变。
- `-InventoryOnly` 保留为主动完整性核查，会哈希并写回清单；它不是状态查询，也不会修复失败阶段。

成功条件是所请求阶段完成并有输出证明。`scope=code` 不声称资源已导出；known AssetRipper 解析限制仍单列在 summary。失败时查看当前 stage receipt 与对应工具日志，无需重做所有前置阶段。
## 完整性与边界

- 脚本先读取 Steam `appmanifest_2285550.acf` 中 `UserConfig` 与 `MountedConfig` 的 `BetaKey`，要求两者一致，再结合 `Assembly-CSharp.dll` 哈希生成不会混淆的构建身份。已审核的一键分支为 `public`、`workshop` 和 `test`。
- 若 Steam 正处于已请求切换、但当前文件仍属于旧挂载分支的过渡态，默认仍拒绝捕获。只有明确保存当前挂载文件时，才可同时传入与 `MountedConfig` 匹配的 `-Branch` 和 `-AllowPendingBranchSwitch`；回执会分别记录请求分支、挂载分支和 pending 状态，绝不会把旧文件改标成目标分支。
- 不要用 `-Branch` 给实际分支改名；显式值只用于交叉校验。未来未知分支默认拒绝，确需保存时必须同时传入与 manifest 完全匹配的 `-Branch` 和 `-AllowUnknownSteamBranch`。
- appmanifest 会和游戏文件一起冻结；新建快照要求 source-start、frozen、snapshot-end 的 build、branch 与完整 SHA-256 一致。Steam 在冻结窗口切换 manifest 会使该阶段失败；之后离线导出/反编译只消费已冻结身份，不把旧副本重新标成新分支。
- 冻结后会逐文件计算 SHA-256，并再次与当前游戏目录对比；游戏在过程中被更新会导致校验失败。
- 工具下载均有固定版本和强哈希校验；详见 `THIRD-PARTY-TOOLS.txt` 与 `PACKAGE-MANIFEST.json`。
- 脚本只读取原游戏目录，输出写入独立基线目录；它不会启动游戏，也不会安装或修改 DTMAPI/BepInEx。
- 结果含官方文件、提取资产和反编译源码，只能用于你自己的本地兼容性研究。**不要提交 Git、上传网盘、发布或转发结果目录。**

## “全量”的实际含义

这里的“全量”是可复处理基线：原始玩家文件被冻结，Unity 资源被尽量恢复，主要托管代码被反编译，并建立完整哈希索引。它不等于作者原始 Unity 工程：变量名、注释、工程组织和未编入发行包的源素材无法恢复；Wwise 音频银行不会自动拆成每个 WAV；编译后 Shader 也不是作者原始 Shader 源码。AssetRipper 对个别 Unity 设置对象可能报告解析错误，因此原始字节副本始终是最终兜底。
