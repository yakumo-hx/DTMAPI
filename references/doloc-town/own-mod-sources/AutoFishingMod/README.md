# 自动钓鱼

自动钓鱼是一个依赖 `DolocTown SMAPI Runtime` 的功能性创意工坊 Mod。
This is a native DolocTown SMAPI auto fishing mod for Doloc Town.

当前公开版入口：

```text
Name: 自动钓鱼
Author: Yuuka
UniqueID: Yuuka.AutoFishing
EntryType: Dlk.DolocAutoFishing.AutoFishingMod
EntryDll: plugins/DolocTownAutoFishing.dll
MinimumApiVersion: 0.8.24
Version: 1.4.2
```

## 功能

- 按 `F8` 开启或关闭自动钓鱼。
- 按 `F9` 打开游戏内鱼类信息菜单。
- 自动钓鱼设置已迁移到 `DolocTown SMAPI Runtime 0.8.24` 的统一配置菜单。
- 使用当前快捷栏选中的鱼竿，走游戏原本的工具使用、体力消耗、鱼池、结算流程。
- 默认跳过抛竿蓄力。
- 自动等待咬钩、收杆、处理钓鱼小游戏的绿色长条和黄色短条。
- 钓鱼结束后自动再次抛竿。
- 移动、跳跃、冲刺、打开菜单或取消输入会直接关闭自动钓鱼，相当于按了一次 `F8`。
- 可选 `SkipMiniGame`：跳过钓鱼小游戏，保留正常体力消耗和鱼池。
- 可选 `InstantBite`：抛竿落水后快速咬钩，保留正常体力消耗和鱼池。
- 可选 `FastAnimations`：在 `SkipMiniGame` 或 `InstantBite` 开启时，将抛竿和收杆相关动画加速，可在 2/3/4/5 倍间切换，默认 3 倍。
- `F9` 鱼类信息窗口使用内置 Wiki 数据显示本月鱼类、水域、竹鱼竿以上要求、特定天气、特定时间和未解锁阶段标注。
- 热键由 `DolocTown SMAPI 0.8.24` 的 `helper.Input` 统一处理，不再依赖本 Mod 自己的 native hotkey thread。
- 钓鱼阶段事件使用 `helper.Experimental.Fishing`，不再使用 0.8.0 的顶层兼容别名 `helper.Fishing`。
- 启动时通过 `helper.Diagnostics` 上报自动再抛、自动小游戏、跳过小游戏、秒咬钩、快速动画和鱼类信息页的可用性/风险。

`SkipMiniGame`、`InstantBite` 和 `FastAnimations` 默认关闭，可在 SMAPI 统一配置菜单或配置文件里开启。`F9` 只负责鱼类信息，不负责设置或启停自动钓鱼。

## 玩家安装

需要先安装 `DolocTown SMAPI Runtime 前置组件`。

1. 订阅 `DolocTown SMAPI Runtime`。
2. 打开 Runtime 创意工坊条目的本地文件夹。
3. 运行 `1_install_loader.bat`，安装 BepInEx + DolocTown SMAPI Core/SDK。
4. 订阅本 Mod。
5. 在游戏内官方 Mod 菜单启用本 Mod。
6. 进入存档后，手持鱼竿站在可以钓鱼的水边，按 `F8` 开启自动钓鱼。

Runtime 条目只负责安装前置组件，不需要在游戏内启用。本 Mod 禁用后代码 DLL 不能完全热卸载，建议重启游戏后再继续游玩或切换功能性 Mod。

English quick start:

1. Install the `DolocTown SMAPI Runtime` workshop item with its `1_install_loader.bat`.
2. Subscribe to this mod and enable it in the in-game mod menu.
3. Hold a fishing rod near fishable water and press `F8`.
4. Press `F9` for fish info. Use the SMAPI unified config menu for settings.
5. Press `F8` again, move, jump, dash, open the menu, or cancel to disable auto fishing.

## 创意工坊包结构

上传包使用官方 0.96.05 `info.json` 格式，并把代码放在 SMAPI 内容目录：

```text
packages/workshop/WorkshopPackages/DLK_AutoFishing/
  info.json
  icon.png
  preview.png
  Content/
    DolocSMAPI/
      manifest.json
      plugins/
        DolocTownAutoFishing.dll
```

`info.json` 不再写 `workshopId`、`workshop_id` 或 `priority`。如果已经上传过同一个条目，创意工坊 ID 写入独立的 `workshop.json`。

## 构建

构建前需要本地已有游戏目录和 SMAPI SDK：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\smapi\DolocTownSMAPI.SDK\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\AutoFishingMod\build.ps1
```

构建脚本会：

- 编译原生 `IDolocMod` DLL。
- 不引用 `BepInEx.dll`。
- 输出到 `src\mods\AutoFishingMod\dist\DolocTownAutoFishing.dll`。
- 同步复制到 `packages\workshop\WorkshopPackages\DLK_AutoFishing\Content\DolocSMAPI\plugins\`。
- 同步复制到本地 `%AppData%\LocalLow\RedSawGames\DolocTown\MODS\DLK_AutoFishing\Content\DolocSMAPI\plugins\` 便于测试。

完整 SMAPI 发行校验：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\tools\build_smapi_release.ps1
```

这个脚本会构建 SDK/Core/Hello/Motor/钓鱼测试/自动钓鱼，重新打包 Runtime 和功能性 Mod，生成 `DolocTownAutoFishing-SMAPI-1.4.2.zip`，并验证 Auto Fishing 的 DLL 不引用 `BepInEx.dll`。

## 配置

SMAPI 配置路径由 Runtime 分配。当前兼容层会写入：

```text
<ModRoot>\config.json
```

常用选项可通过 SMAPI 统一配置菜单编辑，也可直接改配置文件：

- `General.ToggleKey`: 默认 `F8`
- `Menu.MenuToggleKey`: 默认 `F9`
- `General.AutoRecast`: 默认 `true`
- `General.StopOnManualMove`: 默认 `true`
- `General.RequireSelectedFishingRod`: 默认 `true`
- `Timing.CastReleaseProgress`: 默认 `0`，跳过抛竿蓄力
- `Timing.RecastDelaySeconds`: 默认 `0.25`
- `Features.SkipMiniGame`: 默认 `false`
- `Features.InstantBite`: 默认 `false`
- `Features.FastAnimations`: 默认 `false`
- `Features.FastAnimationMultiplier`: 默认 `3`，游戏内可选 `2`、`3`、`4`、`5`
- `General.VerboseLogging`: 默认 `false`

鱼类信息菜单的数据内置在 DLL 中，整理自 Wiki 抓取结果和野外鱼类出没信息表。当前版本不会读取或修改外部 JSON/CSV，也不会持续轮询当前鱼类状态；当前月份、天气、鱼竿和鱼类解锁状态只在打开菜单或点击 `刷新` 时刷新。

调试用触发文件：

```text
<ConfigPath>\com.dlk.doloctown.autofishing.toggle
<ConfigPath>\com.dlk.doloctown.autofishing.enable
<ConfigPath>\com.dlk.doloctown.autofishing.disable
```

日志文件：

```text
BepInEx\LogOutput.log
```

原生 SMAPI 加载成功时应出现类似日志：

```text
Started DolocTown SMAPI mod Yuuka.AutoFishing
```

0.8.24 还应出现 capability 摘要，例如：

```text
Capability report: Yuuka.AutoFishing Runtime input helper [Available]
Capability report: Yuuka.AutoFishing Fishing phase events [Experimental]
```

如果日志出现 `Started legacy BepInEx plugin`，说明上传包中的 DLL 不是原生 SMAPI 构建。

## 兼容性

请勿与以下 Mod 同时启用：

- `Yuuka.FishingTest`
- 旧版 BepInEx 自动钓鱼：`com.dlk.doloctown.autofishing`
- 早期钓鱼测试 GUID：`com.yuuka.dlk.fishingtest`

## 未来计划

- 更完整的体力消耗控制和钓鱼加速细项。
- 识别当前指向/所在水域，并显示该水域鱼池候选鱼。
- 研究游戏内部 `FishingPool` / `FishingPoolInfo` 权重，显示当前水域的鱼池概率或权重。

Planned:

- More configurable stamina and speed options.
- Current-water fish pool detection.
- Fishing pool probability or weight display after the game's pool mapping is understood.
