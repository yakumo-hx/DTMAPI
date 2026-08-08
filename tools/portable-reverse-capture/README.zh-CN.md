# 多洛可小镇：正式版前基线全量保存与解包工具

这个压缩包用于在另一台 Windows x64 电脑上，把**当前安装的多洛可小镇 Steam 版本**冻结成一份可长期比较的本地研究基线。

压缩包本身只有 DTMAPI 自写脚本和说明，**不包含游戏文件、解包内容、反编译源码或第三方工具程序**。运行时会从官方地址下载并校验固定版本的 AssetRipper、ILSpy 和 .NET 运行时。

## 最简单的用法

1. 把整个压缩包解压到剩余空间至少 **12 GiB** 的磁盘；不要解压进游戏目录。
2. 退出多洛可小镇，确认游戏进程已经关闭。
3. 双击 `1_RUN_FULL_UNPACK.bat`。
4. 保持网络连接并等待完成。第一次会下载约数十 MiB 的工具，完整导出可能需要很久。
5. 成功后，窗口会显示 `Portable full capture completed` 和最终目录。

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

`-BuildRoot` 必须是专用目录，不能是游戏目录、游戏目录的父目录或子目录。脚本不会覆盖已有的冻结副本、Unity 导出或反编译目录。

## 中断后的安全续跑

如果已成功冻结原始文件、但下载或 AssetRipper 导出阶段失败，可在 PowerShell 中显式复用冻结副本：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-doloctown-full-capture.ps1 `
  -GameDir "D:\SteamLibrary\steamapps\common\Doloc Town" `
  -BuildRoot "E:\DolocTown-Reverse-Baselines\pre-release-build" `
  -ReuseSnapshot
```

如果 Unity 导出也已完成，只需继续反编译：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run-doloctown-full-capture.ps1 `
  -GameDir "D:\SteamLibrary\steamapps\common\Doloc Town" `
  -BuildRoot "E:\DolocTown-Reverse-Baselines\pre-release-build" `
  -ReuseSnapshot -ReuseExport
```

只有在确认现有反编译目录完整时才添加 `-ReuseDecompile`。脚本没有自动删除或强制覆盖功能；若留下不完整目录，应先人工核对，而不是盲目复用。

## 完整性与边界

- 脚本先读取 Steam `appmanifest_2285550.acf` 中 `UserConfig` 与 `MountedConfig` 的 `BetaKey`，要求两者一致，再结合 `Assembly-CSharp.dll` 哈希生成不会混淆的构建身份。已审核的一键分支为 `public`、`workshop` 和 `test`。
- 若 Steam 正处于已请求切换、但当前文件仍属于旧挂载分支的过渡态，默认仍拒绝捕获。只有明确保存当前挂载文件时，才可同时传入与 `MountedConfig` 匹配的 `-Branch` 和 `-AllowPendingBranchSwitch`；回执会分别记录请求分支、挂载分支和 pending 状态，绝不会把旧文件改标成目标分支。
- 不要用 `-Branch` 给实际分支改名；显式值只用于交叉校验。未来未知分支默认拒绝，确需保存时必须同时传入与 manifest 完全匹配的 `-Branch` 和 `-AllowUnknownSteamBranch`。
- appmanifest 会和游戏文件一起冻结；脚本要求开始读取、冻结副本和结束复查的 build、branch 与完整 SHA-256 一致。Steam 在采集过程中只切换 manifest 也会使本次采集失败，而不会把旧副本重新标成新分支。
- 冻结后会逐文件计算 SHA-256，并再次与当前游戏目录对比；游戏在过程中被更新会导致校验失败。
- 工具下载均有固定版本和强哈希校验；详见 `THIRD-PARTY-TOOLS.txt` 与 `PACKAGE-MANIFEST.json`。
- 脚本只读取原游戏目录，输出写入独立基线目录；它不会启动游戏，也不会安装或修改 DTMAPI/BepInEx。
- 结果含官方文件、提取资产和反编译源码，只能用于你自己的本地兼容性研究。**不要提交 Git、上传网盘、发布或转发结果目录。**

## “全量”的实际含义

这里的“全量”是可复处理基线：原始玩家文件被冻结，Unity 资源被尽量恢复，主要托管代码被反编译，并建立完整哈希索引。它不等于作者原始 Unity 工程：变量名、注释、工程组织和未编入发行包的源素材无法恢复；Wwise 音频银行不会自动拆成每个 WAV；编译后 Shader 也不是作者原始 Shader 源码。AssetRipper 对个别 Unity 设置对象可能报告解析错误，因此原始字节副本始终是最终兜底。
