# 一键完成

依赖 `DolocTown SMAPI Runtime 0.1.0 / API 0.8.1` 的一键完成功能 Mod。它不加速动画，而是在一次原速动作或交互成功后补足对应任务。

## 功能

- `F11` 打开中文设置菜单，菜单位置会保存。
- 菜单内总开关和功能开关：上传版默认全部关闭，打开后才会生效。
- 一次动作砍掉树。
- 一次动作挖掉矿石。
- 一次动作割掉草/杂草。
- 一次动作挖掉镐子垃圾资源，例如建筑垃圾、机械垃圾等。
- 纸箱类资源走原版 `E` 键直接开启/掉落流程，不纳入镐子一键完成。
- 手持燃料右键燃料机后，一次交互加满或耗尽当前堆叠。
- 手持草料右键饲料槽后，一次交互加满或耗尽当前堆叠。
- 工具资源按完整命中次数补扣体力；低体力时沿用“允许完成当前动作，最低扣到 0”的游戏体验。

## SMAPI 信息

```json
{
  "UniqueID": "Yuuka.OneActionComplete",
  "MinimumApiVersion": "0.8.1",
  "EntryDll": "plugins/DolocTownOneActionComplete.dll",
  "EntryType": "Dlk.DolocOneActionComplete.OneActionCompleteMod"
}
```

发布包路径：

```text
packages\workshop\WorkshopPackages\DLK_OneActionComplete
```

本地测试安装路径：

```text
C:\Users\<User>\AppData\LocalLow\RedSawGames\DolocTown\MODS\DLK_OneActionComplete
```

配置路径：

```text
BepInEx\config\DolocTownSMAPI\Yuuka.OneActionComplete\config.json
```

## 构建

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\mods\OneActionCompleteMod\build.ps1
```

构建脚本会生成：

```text
src\mods\OneActionCompleteMod\dist\DolocTownOneActionComplete.dll
packages\workshop\WorkshopPackages\DLK_OneActionComplete\Content\DolocSMAPI\plugins\DolocTownOneActionComplete.dll
```

## 迁移说明

`1.1.0` 起改为原生 `IDolocMod`。SMAPI 负责加载、配置、Update/UI 事件、输入按钮注册、诊断报告和 Harmony 生命周期；资源命中、燃料机回调和饲料槽私有流程仍保留 Mod 自己的 Harmony patch。尤其是燃料机需要替换 `PowerGeneratorFuel.<OnInteract>b__0()`，因为 SMAPI 0.8.1 的机器辅助函数不能取消原版延迟的一格燃料回调。

`1.1.1` 起不再占用一键完成总开关热键，总开关只在设置菜单中切换；工具等级限制始终沿用原版判定，不再提供关闭开关。

`1.1.2` 起上传版默认关闭全部开关。
