# 1.00.01 当前 test build 24567135 兼容性审查

## 记录状态

- 日期：2026-08-05
- 状态：`recorded`
- 性质：只读官方 player 捕获、完整 raw/managed inventory 比较、DTMAPI/第一方产品消费者审查与 policy 决策；不是 Runtime、Hook、Mod 或游戏实现
- 旧基线：`references/doloc-town/reverse/builds/24456188_test_E861E0`
- 新基线：`references/doloc-town/reverse/builds/24567135_test_08C846`
- Source：0.6 路线图在第八轮审核后保留“本机 live build 24567135 与 tracked 24456188 policy 不同”的发布阻断，要求先完整捕获、比较并作有界 policy 决策
- Owning Update：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

本次没有安装、卸载或启动 DTMAPI/游戏，没有修改官方 `MODS`、Workshop、存档或共享 Runtime，也不需要取得 Runtime lock。官方 bytes、导出资源、反编译源码和比较 JSON 只位于 Git 忽略的本地 reverse 区；Git 只保存 DTMAPI 自己撰写的计数、兼容结论和边界。

## 一、最终裁决

`24567135_test_08C846` 可以准入为当前本地 `1.00.01 test` 兼容研究基线。它关闭“本机 live 24567135 尚未捕获和比较”的阻断，但**不是 Steam public 分支证据**：开始、冻结和结束三处 appmanifest 身份均为 build `24567135`、branch `test`。

相对 `24456188_test_E861E0`，完整 ILSpy inventory 只有两个同路径文件改变，且都是方法体内的一行级修订：

1. 官方 Workshop 创建新 item 时仍设置 tags，更新已有 item 时不再重写 tags；
2. 官方存档版本 patch 在执行前按版本号排序。

DTMAPI 生产 Hook 使用的是同类中**未改变**的 `SteamWorkshopUploader.ResolveUploadPlan`；`VersionPatcher` 只被可选 QA continuation probe 观察，两个目标签名均未改变。其余 3,664 个主反编译 inventory 文件及全部 29 个 firstpass 文件逐字不变，因此当前 DTMAPI 与第一方 ProductNative 所消费的原生成员没有在这次 build delta 中变化。

按路线图已冻结的轻量兼容规则，本次结论是：

- 不为纯 game drift 追加 `24567135` Author policy；
- 不批量重签或重发现有 `24456188` 产品包；
- 这些包在 `24567135` 上继续走已经实现和测试的 `Drift -> 真实 Entry/Hook/产品事务` 路径；
- package、receipt、reference、owner 和 payload 身份门仍保持严格；
- 后续真实 public payload 若与本基线不同，仍需按条件分支另行捕获和比较。

## 二、捕获身份与完整性

| 边界 | 结果 |
| --- | --- |
| Steam 身份 | app `2285550`；build `24567135`；branch `test`；manifest SHA-256 `72D3EC2E...B51E` |
| 游戏版本 | `globalgamemanagers` 同一 PlayerSettings 槽为 `1.00.01`；旧基线为 `1.00.00` |
| 主程序集 | 6,384,128 bytes；SHA-256 `08C8466F...697E`；旧版同长度、不同 hash |
| raw snapshot | 542 files / 1,804,584,282 bytes；捕获内 source parity exact |
| AssetRipper | `1.3.14`；51,988 files / 4,381,946,659 bytes；90 个场景、195 张配置表、181 个 managed assembly |
| ILSpy main | `9.1.0.7988`；3,666 files / 9,479,114 bytes |
| ILSpy firstpass | 29 files / 137,684 bytes |

捕获使用由 clean HEAD `5f724ad7` 生成的便携包；包 ZIP SHA-256 为 `D80314D68C85365631BC0459CABCF4E2916D9CF2680D477918FF575E8F20A045`。脚本只冻结 `DolocTown.exe`、两个 Unity 根文件、`DolocTown_Data` 与 `MonoBleedingEdge`，没有把当前已安装的 BepInEx/DTMAPI 混入基线。

捕获完成后又独立重算实际结果树：raw `542/542`、AssetRipper `51,988/51,988`、main `3,666/3,666`、firstpass `29/29` 均为 missing `0`、extra `0`、hash/length mismatch `0`；raw 中 BepInEx、Mods、Saves、`Player.log` 和 `LogOutput.log` 命中数为 `0`。

## 三、分层差异

| 层 | 24456188 | 24567135 | 精确差异 | 结论 |
| --- | ---: | ---: | --- | --- |
| raw official files | 452 | 542 | 新增 93、删除 3、同路径改变 187、不变 262 | 90 个 `level*.resS` 回归及三个 content-hash bundle 换名是打包布局变化 |
| managed assemblies | 181 | 181 | 改变 1、不变 180 | 只有 `Assembly-CSharp.dll` 改变 |
| ILSpy main | 3,666 | 3,666 | 改变 2、不变 3,664；`+2/-1` 行 | 无类型文件增删；只有两个小方法体变化 |
| ILSpy firstpass | 29 | 29 | 0 改变 | 精确不变 |
| Build Settings scenes | 90 | 90 | 0 增、0 删、0 index/path 移动 | 没有新场景身份 |
| GenDatas | 195 | 195 | 11 改变、184 不变 | 两个食用效果、一个房间刷怪范围、反馈链接和音频物品本地化修订 |
| AssetRipper export | 51,988 | 51,988 | 新增 7、删除 7、同路径改变 45,784、不变 6,197 | 大量 GUID/fileID/YAML/序列化噪声，不可作为功能数 |

11 张配置表中没有表增删。非本地化变化只有两个食用效果记录和一处房间怪物数量范围；其余是 bug-report 链接和多语言音频物品命名。仓库的 Runtime、第一方产品、测试和 Author SDK 对这些配置表/键没有直接引用，不能据此推导产品修复。

AssetRipper 把 old-photo 材质/Shader 投影替换为 television-grey-scale 名称，并重新生成大量导出元数据；这不改变 90 个 build scene 的 path/index 集。兼容判断优先使用 raw、managed assembly 和相同 ILSpy 版本的代码 inventory，不把 45,784 个导出 hash 变化写成 45,784 项功能变化。

## 四、消费者与 Hook 边界

### 4.1 `SteamWorkshopUploader`

生产 GameBridge 仍只按类型名、方法名和参数数目 Hook `ResolveUploadPlan(ModInfo, Action<WorkshopUploadPlan>)`。两版反编译中该方法、字段和 callback 事务逐字不变；变化位于后续 `UploadUpdate` 的 `SetItemTags` 条件，DTMAPI 不 Hook 或替换该方法。

因此本次不修改现有 busy fallback、known-id watchdog 或官方 upload ownership。官方对已有 item 不再重写 tags 是原生上传语义变化，不是 DTMAPI source arbitration、Author local install 或 managed-product lifecycle 的新授权。

### 4.2 `VersionPatcher`

`LoadAllVersionPatchesBeyond(string)` 增加版本排序，但类型、方法名、参数和返回类型不变。DTMAPI 生产 Runtime 不依赖其返回顺序；只有可选 QA native-continuation probe 对 `LoadAllVersionPatches` / `LoadAllVersionPatchesBeyond` 安装无状态 Prefix/Postfix breadcrumb，签名保持可解析。

该官方修订与 MoreSaves 轻量迁移不冲突：MoreSaves 不修改或清除 `convert_data_100`，也不介入官方 patch 调用时序；它仍只在自身加载、公布 12 槽之前按文件角色补做旧 6--11 名称迁移。

### 4.3 第一方 ProductNative

除上述两个文件外，主反编译树逐文件 hash 不变。AutoFishing、DebugConsole、MoreEquipmentSlots、MoreSaves 以及其余当前第一方产品使用的原生类型/成员均位于不变文件；本次配置变化也没有仓库直接消费者。没有证据支持修改产品源码、增补 native surface、提高 Runtime floor 或生成新 receipt。

## 五、Policy 决策与阻断边界

Author SDK policy 的 build/hash 仍代表“该包基于什么精确参考编译”，不能因为玩家当前安装变为 `24567135` 就改写旧 policy 或伪造新 receipt。0.6 Core 已把当前游戏 identity 单独投影为 `Exact/Drift/Unknown`，只有真实类型、Entry、Hook 或产品事务失败才阻断单个 Mod。

所以本审查关闭的是：

- current live `24567135_test` 未捕获；
- current live 与 `24456188` 的 managed delta 未知；
- 是否必须新增 policy/批量重签尚未决定。

仍未关闭的是：

- 尚未出现/捕获的 Steam `public` payload 条件核验；
- MoreSaves 当前 `1.0.1` 的玩家 cold-start 反馈；
- MoreEquipment disposable 两进程 native-save/cold-recovery acceptance；
- 冻结最终候选的完整 Release、最终 Local11/ISSUE-011 与实际 Workshop publication。

## 六、验证与证据

- 既有 full-capture workflow 完成 start/frozen/end Steam identity 与 source parity 检查，退出码 `0`。
- 独立复核四组完整 inventory，全部路径、长度与 SHA-256 exact。
- 对 raw、managed assemblies、AssetRipper、ILSpy main/firstpass、build scenes 与 GenDatas 做同路径 hash 集合比较。
- 对两个变化的反编译文件执行 `git diff --no-index`，总计 `+2/-1` 行；没有文件增删。
- 对仓库生产源码、第一方产品、测试、Author SDK 和工具做消费者扫描；唯一生产相关引用是未变化的 `SteamWorkshopUploader.ResolveUploadPlan`，`VersionPatcher` 引用只属于可选 QA probe。
- 未运行游戏、完整 Release 或产品行为 smoke；本审查没有产品源码变化，也不把静态兼容审查冒充玩家验收。

本地详细比较：`references/doloc-town/reverse/builds/24567135_test_08C846/full-baseline-inventory/comparison-to-24456188.json`。该文件及其官方输入均为 ignored local evidence，不得提交或发布。
