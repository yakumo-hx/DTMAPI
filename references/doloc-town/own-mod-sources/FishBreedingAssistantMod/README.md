# FishBreedingAssistantMod

多洛可小镇游戏内「鱼卵信息显示」SMAPI Mod。这个目录现在只保留 Mod 运行与构建相关内容。

## 当前目录职责

- `src/`：Mod C# 源码和生成的 `FishBreedingLookup.g.cs`。
- `build.ps1`：构建 `dist/DolocTownFishBreedingAssistant.dll`。
- `dist/`：本地构建 DLL 与审计构建输出。
- `release/`：历史 Mod 发布 zip。
- `reports/fish_roe_display_debugging_notes.md`：鱼卵显示链路排障记录。
- `MAINTENANCE.md`：Mod 维护入口。

## Wiki 计算器已迁移

鱼类繁殖计算器、Lua/JS/CSS、wiki 发布包、表格图片、游戏外计算器和数据抽取脚本已整理到：

```text
src/wiki/FishFarmingAnalyzer
```

常用入口：

- `src/wiki/FishFarmingAnalyzer/README.md`
- `src/wiki/FishFarmingAnalyzer/wiki_source/README.md`
- `src/wiki/FishFarmingAnalyzer/dist/wiki_dynamic_admin_package/`

如果需要重新生成 Mod 使用的鱼类查找表，运行 wiki 项目的：

```powershell
python src\wiki\FishFarmingAnalyzer\tools\build_fish_outputs.py
```

该脚本会继续写回：

```text
src/mods/FishBreedingAssistantMod/src/FishBreedingLookup.g.cs
```

## 构建 Mod

从仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\FishBreedingAssistantMod\build.ps1
```
