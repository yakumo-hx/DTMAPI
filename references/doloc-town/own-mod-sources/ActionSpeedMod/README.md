# 动作加速

依赖 `DolocTown SMAPI Runtime 0.1.0 / API 0.8.1` 的动作加速功能 Mod。

## 功能

- `F10` 打开中文设置菜单，菜单位置会保存。
- 菜单内总开关和功能开关：上传版默认全部关闭，打开后才会生效。
- 自动装水：手持塑料瓶且人在水中时自动装水，可在菜单中开启。
- 斧头、镐子、镰刀工具动画加速，最高 `4x`。
- 塑料瓶装水、吃喝、机器加燃料/草料、作物收获、野外采集、树脂收集器动画加速。
- 长按真实鼠标右键连续喝瓶装水；只有当前物品实现 `IEatable` 且名称在 `GlobalParameter.WaterItems` 中时触发，避免连续吃其他食物。

本 Mod 只改动画速度或重复调用原游戏的使用动作，不修改工具伤害、资源血量、体力消耗、掉落表、食物效果、Buff 或机器结算。

## SMAPI 信息

```json
{
  "UniqueID": "Yuuka.ActionSpeed",
  "MinimumApiVersion": "0.8.1",
  "EntryDll": "plugins/DolocTownActionSpeed.dll",
  "EntryType": "Dlk.DolocActionSpeed.ActionSpeedMod"
}
```

发布包路径：

```text
packages\workshop\WorkshopPackages\DLK_ActionSpeed
```

本地测试安装路径：

```text
C:\Users\<User>\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_ActionSpeed
```

配置路径：

```text
BepInEx\config\DolocTownSMAPI\Yuuka.ActionSpeed\config.json
```

## 构建

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\ActionSpeedMod\build.ps1
```

构建脚本会生成：

```text
src\mods\ActionSpeedMod\dist\DolocTownActionSpeed.dll
packages\workshop\WorkshopPackages\DLK_ActionSpeed\Content\DolocSMAPI\plugins\DolocTownActionSpeed.dll
```

## 迁移说明

`1.3.0` 起改为原生 `IDolocMod`。SMAPI 负责加载、配置、Update/UI 事件、输入按钮注册、诊断报告和 Harmony 生命周期；动作时序敏感的采集/装水/机器加料前缀仍由本 Mod 自己的 Harmony patch 保留，因为 SMAPI 0.8.1 的 `ObjectInteracting` 事件目前是 postfix，单独使用会有概率晚于 `AgentStateInteract.OnEnter`。

`1.3.1` 起不再占用动作加速总开关热键，总开关只在设置菜单中切换。

`1.3.2` 起不再占用自动装水热键；上传版默认关闭全部开关。
