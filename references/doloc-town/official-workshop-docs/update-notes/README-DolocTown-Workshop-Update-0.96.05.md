# Doloc Town Workshop Update 0.96.05 Notes

记录时间：2026-05-17

## 本次重新抓取

新的飞书抓取目录：

- `research/创意工坊说明/飞书抓取_20260517`
- `research/创意工坊说明/飞书抓取_20260517_55页_含图片.zip`

抓取结果：

- 新版：55 页，98 个图片资源，0 个失败。
- 旧版：51 页，77 个图片资源。
- 新增页面：
  - `09载具`
  - `10综合案例五（新增平台）`
  - `08平台`
  - `13ID对照表（平台）`

飞书更新记录显示 2026-05-15 新增/调整：

- 美化模组新增平台、载具。
- 基础内容模组新增综合案例五（新增平台）。
- 创建模组文档新增模组本地化说明。
- 上传模组文档调整更新模组说明。
- 贴图锚点说明新增锚点概念及表示说明。

## 本地 Steam / 游戏状态

当前游戏分支：

- `appmanifest_2285550.acf` 仍在 `workshop` beta 分支。
- 当前 build id：`23249387`。

当前核心程序集：

- 文件：`D:\Steam\steamapps\common\Doloc Town\DolocTown_Data\Managed\Assembly-CSharp.dll`
- SHA256：`247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367`

`DLKFunctionalPatcher` 已知 hash 列表现在包含：

- `51D76664F6C914EDF189C32B2C8A60FACF6359F4A569B2860D81E7D274CD5D64`
- `247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367`

本地官方示例模组订阅目录仍是旧内容：

- 目录：`D:\Steam\steamapps\workshop\content\2285550\3705665433`
- `info.json` 仍写 `version: 0.96.04`
- 目录中尚未出现 `平台`、`载具`、`综合案例五`。
- `appworkshop_2285550.acf` 中该项 `NeedsUpdate=0`，所以 Steam 当前认为本地 Workshop 内容已是最新，但实际还没拿到飞书所述示例更新。后续可用“验证游戏完整性”或重新订阅官方示例模组确认。

## ModManager 反编译变化

游戏仍是 Unity Mono，`Assembly-CSharp.dll` 可用 Mono.Cecil 读取。

关键类型仍存在：

- `DolocTown.Config.ModManager`
- `DolocTown.Config.ModInfo`
- `DolocTown.Config.ModManifest`
- `DolocTown.Config.ModWorkshopInfo`
- `DolocTown.Config.SteamWorkshopUploader`

`ModManager.ReloadMods()` 仍然扫描：

- 本地 MODS 根目录。
- Steam Workshop 订阅目录。
- 然后调用 `UpdateCache()`。

`ModManager.UpdateCache()` 仍然无参数，末尾仍是可注入点；官方没有新增 Workshop DLL 加载逻辑。搜索程序集调用时，未发现官方 ModManager 路径中有 `Assembly.LoadFrom` / `Assembly.LoadFile` 之类的插件 DLL 加载。

### info.json 格式变化

`ModManifest` 当前字段：

```json
{
  "name": "",
  "author": "",
  "version": "",
  "description": "",
  "tags": [],
  "localized_name": {
    "schinese": "",
    "tchinese": "",
    "english": ""
  },
  "localized_description": {
    "schinese": "",
    "tchinese": "",
    "english": ""
  }
}
```

反编译确认：

- `nameL10n` 对应 JSON 字段 `localized_name`。
- `descriptionL10n` 对应 JSON 字段 `localized_description`。
- 上传用语言 id 为 `schinese`、`tchinese`、`english`。

### workshopId / priority 迁移

官方新增了 `ModManager.TryMigrateData()`，加载旧 `info.json` 时会：

- 读取旧字段 `workshopId` 或 `workshop_id`。
- 将创意工坊物品 ID 写入独立 `workshop.json`。
- 从 `info.json` 移除旧字段：
  - `workshopId`
  - `workshop_id`
  - `priority`
- 自动补齐：
  - `localized_name`
  - `localized_description`

`ModWorkshopInfo` 的 JSON 字段为：

```json
{
  "workshop_id": 3726025980
}
```

结论：后续我们自己的打包脚本应该停止把 `workshopId` 和 `priority` 写进 `info.json`；如果是本地已有创意工坊条目的更新包，应在本地模组根目录写 `workshop.json`。

2026-05-17 已处理：

- `scripts/tools/package_functional_mod_loader.ps1` 已改为新 `info.json` 格式，并写独立 `workshop.json`。
- `scripts/tools/package_split_workshop_mods.ps1` 已改为新 `info.json` 格式，并写独立 `workshop.json`。
- `DolocTownSMAPI.Core` 已增加官方模组 ID alias：`Workshop.3726025980` 会兼容旧式 `3726025980`，`Local.xxx` 会兼容旧式 `xxx`。
- 已重新构建并重新生成本地/工作区前置包和 `钓鱼测试` 包。

### 生效顺序变化

官方新增/保留了手动顺序相关方法：

- `MovePrev`
- `MoveNext`
- `Move`
- `CanMovePrev`
- `CanMoveNext`
- `RefreshPriority`

`GetAllEnabledModInfos()` 仍按 `priority` 降序生成 `EnabledMods`。

`UpdateCache()` 对 `EnabledMods` 反向遍历，以保证高优先级模组最后覆盖缓存。这说明官方的普通 JSON/PNG 内容模组已经有明确顺序语义。

## 对我们现有两条功能性路线的影响

### 路线 A：BepInEx + Workshop Bridge

当前仍可用的原因：

- `ModManager.UpdateCache()` 仍存在且无参数。
- `ModManager.ReloadMods()` 仍存在且无参数。
- `ModManager.EnabledMods` 仍存在。
- `ModInfo.id`、`ModInfo.rootPath`、`ModInfo.priority` 仍存在。
- Workshop 订阅目录仍进入官方 EnabledMods 链路。

潜在要调整：

- Bridge 当前读取 `priority` 主要用于日志；如果之后多个功能性 mod 有加载顺序依赖，需要明确是按 `EnabledMods` 正序加载，还是模拟官方数据覆盖顺序反向加载。
- 我们公开包的 `info.json` 仍含旧字段，官方能迁移，但新上传最好改成新格式。
- 前置包描述应从“Steam 不自动更新，需要取消订阅重订”改成更贴近官方文档：玩家订阅模组会自动更新；如未自动更新，可关闭游戏后验证游戏完整性强制更新。

### 路线 B：Native Functional Patcher

当前真实游戏未 patch：

- `DLKFunctionalPatcher.exe status` 显示 `Patched: False`。
- `DolocTown_Data/Managed/DLKFunctionalModLoader.dll` 不存在。

在仓库临时目录做了新版 DLL 注入测试：

- 临时目录：`research/reverse/patch_test_20260517_09605`
- 使用当前 0.96.05 `Assembly-CSharp.dll`
- `--allow-unknown --force` 注入成功。
- 注入后 patch 状态为 `Patched: True`。

结论：

- 工程上，0.96.05 仍可沿用 `ModManager.UpdateCache()` 注入点。
- `src/loaders/FunctionalModPatcher/src/Program.cs` 已更新为多 hash 守卫，包含 0.96.05 hash；`src/loaders/FunctionalModPatcher/build.ps1` 已重新构建通过。

## 建议的后续修改

优先级 1：

- 重新拉取官方示例模组 `3705665433`，确认平台、载具、综合案例五的实际文件结构。
- 用新示例补充数据模组研究，尤其是平台配置、贴图锚点、设备灯光结构变化。

优先级 2：

- 对 Bridge / SMAPI Core 增加官方版本/hash 日志。
- 在功能性插件加载日志里记录官方模组顺序，后续若有功能性 mod 依赖顺序，可按官方顺序决策。
