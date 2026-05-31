# 牧铃显示隐藏产物进度 1.0.0

这是一个依赖 `DolocTown SMAPI Runtime 0.2.1 / API 0.8.22` 的原生 SMAPI Mod。

功能很窄：打开牧铃动物详情界面时，在官方的“饱食”“心情”进度条下面追加一条“特殊产物”进度条，显示当前房间内动物的特殊产物进度。

## SMAPI Manifest

```json
{
  "Name": "牧铃显示隐藏产物进度",
  "UniqueID": "Yuuka.AnimalHusbandryProgress",
  "MinimumApiVersion": "0.8.22",
  "EntryDll": "plugins/DolocTownAnimalHusbandryProgress.dll",
  "EntryType": "Dlk.DolocAnimalHusbandryProgress.AnimalHusbandryProgressMod"
}
```

## Data Source

本 Mod 不反射游戏私有字段，也不直接 Patch 游戏 UI。

它只使用 SMAPI 0.8.21/0.8.22 新增的：

- `helper.Experimental.Animals.ViewerRendering`
- `AnimalViewerRenderingEventArgs.HusbandryProgress`
- `AnimalViewerRenderingEventArgs.AddProgressBar(...)`
- `AnimalViewerRenderingEventArgs.AddProgressBar(..., Color fillColor)`

SMAPI Runtime 负责从游戏本体读取：

- 当前牧铃界面显示的动物；
- 动物身份；
- `husbandryValues` 特殊产物隐藏进度；
- `TbHusbandry` 中对应特殊产物阈值；
- 牧铃详情面板的进度条扩展。

## Display Rule

如果动物存在多个特殊产物进度，本 Mod 只显示当前进度比例最高的一项：

```text
特殊产物  current/threshold
```

例如 `37/100`。当进度超过阈值时，进度条按满格显示，右侧仍保留真实数值。默认颜色为橙色。

如果动物没有配置特殊产物，则不添加额外进度条。

## Build

构建前需要：

- 本地游戏目录：默认 `D:\Steam\steamapps\common\Doloc Town`
- 已构建的 `src\smapi\DolocTownSMAPI.SDK\dist\DolocTownSMAPI.SDK.dll`

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\AnimalHusbandryProgressMod\build.ps1
```

构建输出：

- `dist\DolocTownAnimalHusbandryProgress.dll`
- `release\DolocTownAnimalHusbandryProgress-SMAPI-1.0.0.zip`
- `packages\workshop\WorkshopPackages\DLK_AnimalHusbandryProgress\...`
- 本地测试目录 `%AppData%\LocalLow\RedSawGames\DolocTown\MODS\DLK_AnimalHusbandryProgress\...`

## Test Checklist

1. 确认 `DolocTown SMAPI Runtime` 已安装，Runtime/API 至少为 `0.8.22`。
2. 启用 `牧铃显示隐藏产物进度`。
3. 进入游戏，在有动物的房间使用牧铃。
4. 选择能产生特殊产物的成年动物。
5. 检查详情面板在“心情”下方出现“特殊产物 current/threshold”。
6. 喂食能增加特殊产物进度的食材后，再打开牧铃确认数值变化。

## Maintenance Notes

- 这个 Mod 应保持为 SMAPI 事件订阅型 Mod，不要重新引入私有 Harmony Patch。
- 如果看不到进度条，优先检查 Runtime 日志里是否有 `Registered animal viewer handler` 和是否真的运行在 SMAPI `0.8.22+`。
- 如果有动物明明应该有特殊产物但 `HusbandryProgress` 为空，问题应优先回到 SMAPI 的 `TryBuildAnimalHusbandryProgress` / `TbHusbandry` 解析链路排查。
