# 缺氧动物包 Workshop 订阅目录一致性审计

## 记录状态

- 日期：2026-08-28
- 状态：`recorded`
- 性质：audit-only；本记录只保存上传后订阅字节校对结果，不修改订阅目录、本地 Mod、Runtime、启用配置或存档，也不创建新的 Update
- Source：用户在游戏内成功上传“缺氧动物包”后，要求校对 Steam 订阅目录是否与本地候选一致
- Workshop item：`3791474574`
- Steam manifest：`8336118746041049876`
- 订阅目录：`D:/Steam/steamapps/workshop/content/2285550/3791474574`
- 订阅目录时间：`2026-08-28 20:40:11 +08:00`

## 上传提交证据

当前 `Player.log` 记录了完整成功链：

- `[MOD] Workshop item created: 3791474574`；
- `[MOD] Workshop base update submitted successfully, continue uploading localized texts.`；
- 繁中与英文 localized text 均提交完成；
- `[MOD] Workshop upload success!`；
- 成功回调随后写入 `MODS/DTMAPI_AnimalPack/workshop.json` 和游戏的 `SAVE/mod_infos.json`。

因此本审计不是仅凭目录出现来推断上传成功；游戏上传回调、持久化 Workshop ID、Steam ACF manifest 和实际订阅字节形成四项相互独立的一致证据。

## 审计边界

玩家实际收到的 Steam 订阅目录是主审计对象。对照对象为：

1. 仓库组装包 `products/first-party/AnimalPack/.local-build/DTMAPI_AnimalPack`；
2. 当前本地开发包 `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_AnimalPack`，比较时排除上传成功回调之后才写入的本地身份文件 `workshop.json`。

本产品是 `OfficialJsonContentPack`，订阅包不含 Runtime 安装 BAT/PowerShell，因此不套用 Runtime Workshop 安装器压力矩阵；改用 AnimalPack 自身的 JSON、引用、图像、经济和 public 1.00.06 基线静态检查，并额外在 Windows PowerShell 5.1 下执行同一检查。

## 一致性结果

| 树 | 文件数 | 总字节 |
| --- | ---: | ---: |
| 仓库组装包 | 211 | 1,898,757 |
| 本地开发包（排除 `workshop.json`） | 211 | 1,898,757 |
| Steam 订阅目录 | 211 | 1,898,757 |

以相对路径、文件长度和 SHA-256 逐文件比较：

- 仓库组装包 vs. Steam 订阅目录：`missing=0 / extra=0 / mismatch=0`；
- 本地开发包 vs. Steam 订阅目录：`missing=0 / extra=0 / mismatch=0`；
- 订阅 `preview.png`：`849,062` 字节，SHA-256 `743206EE3A57402CA20611F6160A738E82E61292EE31400911D866A099ADC96F`，与仓库候选完全相同；
- 本地 `workshop.json` 精确记录 `workshop_id=3791474574`。该 33 字节文件在上传提交成功后才写入，未出现在本次订阅内容中是正确行为，不属于缺失。

## 内容有效性

- PowerShell 7：对订阅目录运行 `products/first-party/AnimalPack/test-static.ps1 -PackageRoot <subscription>`，PASS；
- Windows PowerShell 5.1：对同一订阅目录运行同一检查，PASS；
- 检查覆盖四个 species、七种蛋与分解配方、普通/隐藏产出数值、商店/动物袋/照料站引用、PNG/WAV 引用、三语说明、最低 DTMAPI 版本及 public 1.00.06 原版物品基线。

## 裁决

Workshop `3791474574` 当前已下载的 manifest `8336118746041049876` 与仓库组装候选逐字节一致，订阅交付校对通过。没有订阅侧缺文件、多文件或内容漂移。

本裁决只回答上传内容与订阅内容是否一致，不扩张为公开发布授权、外部素材再分发许可或实机玩法验收结论。
