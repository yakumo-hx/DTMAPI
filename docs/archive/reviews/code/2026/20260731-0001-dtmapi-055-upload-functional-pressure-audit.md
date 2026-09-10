# DTMAPI 0.5.5 上传目录与全功能产品压力审计

Status: recorded

Date: 2026-07-31

Scope: 冻结玩家包上传目录、实际目录玩家矩阵、订阅目录备份与源码可追溯性、0.5.5 加载全部当前功能产品时的短时主动压力和一小时标题挂机回归。

Source request: 用户要求只把冻结的 30 个 Runtime 文件同步到实际上传目录，额外保留原 `workshop.json`，随后直接对该目录执行玩家包矩阵；同时备份全部 Doloc Town Workshop 订阅、盘点可恢复源码，并在不修改 DTMAPI/Mod 生产源码的前提下评估全部当前功能产品的运行压力和 GC 风险。

## 审计边界

- 本轮不修改 Runtime、GameBridge、Core、产品 Mod 或 Compatibility Host 源码。
- 本轮不重新构建或重冻 Runtime；测试输入是既有冻结
  `dist/prerelease-055-final-candidate/DTMAPI`。
- 游戏路线均按 `NoNativeSave` 分类；不得把外部恢复玩家存档当成通过依据。
- 主动 GC 焦点路线不强制 GC。10 秒测量窗只用于检查计数边界和明显的单调增长，不建立长期内存预算。
- 一小时路线使用现有 QA Host 只驱动标题等待和存档进入，并由独立 OS 采样器每 30 秒记录资源；QA Host 是测试观察者，不属于玩家产品集合。
- Workshop 订阅包只读检查，不测试其中旧的一方 Mod。功能组合使用当前源码生成的产品候选；唯一例外是用户明确要求保留的旧 MoreEquipmentSlots `0.3.1`。

## 冻结玩家包与实际上传目录

冻结输入：

- 路径：`dist/prerelease-055-final-candidate/DTMAPI`
- 文件数：`30`
- 总字节：`71,552,124`
- `release-manifest.json` SHA-256：
  `911CC09047C815E8412DD872D74BF5ABE951C25CACA2CFC803134A36F3BAD4A2`
- Runtime 版本：`0.5.5`
- Runtime `BuildCommit`：`f96c9cc61bf2`
- 根目录 BAT：仅 `1_install_dtmapi.bat` 至
  `4_collect_dtmapi_logs.bat`；没有 `0_probe`。

同步结果：

- 实际上传目录：
  `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- 最终文件数：`31`，即冻结 30 文件加原上传目录
  `workshop.json`。
- 最终总字节：`71,552,157`。
- 保留的 `workshop.json`：33 字节，SHA-256
  `D6D9206A4A58B88CC985EE72832D57F226FF58767EF6E606A2731B0D604EF98D`。
- 原上传目录字节备份：
  `E:\Python_project\DTMAPI-retained-artifacts\upload-staging\20260731-005023\original`
- 同步收据：
  `E:\Python_project\DTMAPI-retained-artifacts\upload-staging\20260731-005023\sync-receipt.json`
- 玩家矩阵之后的最终复核发现上传目录 `info.json` 曾被外部元数据流程
  追加空的 `localized_name`，其余 29 个冻结文件和 `workshop.json`
  未变化。本轮只把冻结候选的 `info.json` 复制回上传目录；最终逐文件
  SHA 复核为冻结 30 文件全部匹配、额外文件仅原 `workshop.json`，
  总字节重新为 `71,552,157`。

实际上传目录玩家矩阵已通过：

- PowerShell 5.1 对 10 个打包脚本的文件级解析：`10/10`。
- 缺失游戏目录安装：按预期失败。
- 空游戏目录安装和状态检查：按预期失败。
- 带空格及中文路径的有效安装：通过。
- 安装后状态检查、根 BAT 日志收集：通过。
- 卸载及卸载后状态检查：通过。
- 阻断项：`0`。
- 证据：
  `tmp/test-runs/workshop-player-matrix/DTMAPI Workshop Audit 20260731-005106/Results/stress-summary.md`
- 打包 Doctor 入口补充检查：通过；证据：
  `tmp/test-runs/workshop-player-matrix/packaged-doctor-20260731-005230`。

## Workshop 订阅备份

订阅源：

`D:\Steam\steamapps\workshop\content\2285550`

只读备份：

`E:\Python_project\DTMAPI-retained-artifacts\subscriptions\workshop-2285550-20260731-004038`

备份收据：

- 订阅目录：`44`
- 文件：`4,897`
- 字节：`78,638,015`
- reparse point：`0`
- 源/备份树 SHA-256：
  `0644A44850D29C9F11E9F6AAB147ACD81F78AA4A9F9AF9F934700B2EAEB176AD`
- 明细哈希：
  `E:\Python_project\DTMAPI-retained-artifacts\subscriptions\workshop-2285550-20260731-004038\SHA256SUMS`
- 备份元数据：
  `E:\Python_project\DTMAPI-retained-artifacts\subscriptions\workshop-2285550-20260731-004038\backup.json`
- 完成游戏测试后，再按 `SHA256SUMS` 对源订阅和备份各自逐文件复核：
  `4,897/4,897` 命中，源缺失、备份缺失、源哈希不符、备份哈希不符、
  源额外文件和备份额外文件均为 `0`。

## 源码可追溯性

下列当前一方产品都有完整的 `.csproj` 和 C# 源码：

| Workshop | 产品 | 当前源码 |
| --- | --- | --- |
| `3742714442` | Y 键控制台 | `products/first-party/DebugConsole` |
| `3742717440` | Zoom | `products/first-party/Zoom` |
| `3742763050` | MoreSaves | `products/first-party/MoreSaves` |
| `3742763309` | ActionSpeed | `products/first-party/ActionSpeed` |
| `3742763540` | OneActionComplete | `products/first-party/OneActionComplete` |
| `3742763706` | FishBreedingAssistant | `products/first-party/FishBreedingAssistant` |
| `3742763843` | AnimalHusbandryProgress | `products/first-party/AnimalHusbandryProgress` |
| `3742765514` | ChestLocatorEnhancer | `products/first-party/ChestLocatorEnhancer` |
| `3743799721` | AutoFishing | `products/first-party/AutoFishing` |
| `3744059735` | MoreEquipmentSlots | `products/first-party/MoreEquipmentSlots` |
| `3746319981` | ManboCardboardAudio | `products/first-party/ManboCardboardAudio` |

补充：

- Runtime `3743016467` 的源码在 `src` 及对应工程中。
- 旧 MoreEquipmentSlots `0.3.1` 的源码可从 Git 历史
  `71304214^:testmods/MoreEquipmentSlotsMod` 恢复。
- 旧 AutoHarvest `3742771572` 有
  `author-sdk/samples/api-demand/AutoHarvest` 和 Git 历史
  `testmods/AutoHarvestMod` 的源码谱系，但本轮没有证据证明这些源码能逐字节重建当前订阅 DLL；因此记为“有谱系、精确发布字节来源未证明”。
- 其余无 DLL/可执行压缩包的订阅项是内容/贴图/文本包；订阅载荷本身已完整备份，不另行宣称存在 C# 源码。

在工作区、订阅载荷、订阅压缩包和已保存发布元数据中，未找到下列程序载荷的源码或明确源码归档：

| Workshop | 程序载荷 |
| --- | --- |
| `3742618545` | `DolocDevModeMod.dll` |
| `3743621104` | `DolocStorageExpansionMod.dll` |
| `3743644065` | `DolocStoreCapacityMod.dll` |
| `3754869009` | `DolocTownQoL.dll` |
| `3759797170` | `Mxx_DolocTownMod_Installer.dll` 与其外部插件 |
| `3744793227` | `DolocMultiplayer.dll` 压缩包 |
| `3750891996` | `ComboSwordMod.dll` 压缩包 |
| `3756810338` | TimeStopButton 插件压缩包 |
| `3759066788` | “小莫的神奇旅途”两个插件 DLL |

“未找到”只说明当前可访问工作区、订阅内容和发布元数据中没有可归档源码，不证明作者从未公开源码。

## 全功能产品测试输入

联合配置绑定 Runtime 0.5.5 冻结候选，并加载：

1. 当前 DebugConsole
2. 当前 Zoom
3. 当前 MoreSaves
4. 当前 ActionSpeed
5. 当前 OneActionComplete
6. 当前 FishBreedingAssistant
7. 当前 AnimalHusbandryProgress
8. 当前 ChestLocatorEnhancer
9. 当前 AutoFishing
10. Workshop `3744059735` 的旧 MoreEquipmentSlots `0.3.1`
11. 当前源码构建并经正式通用打包路径生成的 ManboCardboardAudio

9 个 Advanced 产品都使用冻结 1.0.0 候选，由冻结 Author SDK
安装；每个启动日志都必须为 `identity=Advanced CodeMod`、
`source=Local`。旧 MoreEquipmentSlots 必须为
`identity=Strict CodeMod`、`source=Workshop`、
`workshopId=3744059735`。Manbo 必须为 `source=OfficialLocal`。

标题短启动 `GAME-SMOKE/20260731-011320` 已证明上述 11 个产品各
加载和 Entry 一次，Manbo WAV 进入 ready，且无 Fatal 窗口、进程正常
退出。此前 `010613`、`010831` 和 `011109` 分别缺少部分当前产品或
Manbo，只保留为诊断，不作为联合配置验收。

## 主动 GC 焦点

证据：

`tmp/test-runs/all-functional-mods-20260731/active-gc-focus`

整体结果：

- `Status=Passed`
- 外层收据 authority 为 `DTMAPI-0.5.5-focused-current-candidate`；各游戏
  子阶段保留 `RuntimeResult.status=NonAuthoritativeCompleted`。因此下面
  只作当前候选的有界焦点结果，不把单个短窗提升为正式长期内存预算。
- ActionSpeed：`8/8`
- AutoFishing：`L1/L3/L4/L5`
- `ForcedGc=false`
- 12 次游戏进程均正常退出。
- 每阶段都在清理前证明玩家存档和相关 committed sidecar 未变化。
- ActionSpeed、AutoFishing 产品目录、部署日志和 10 项 Author
  `source-state` 在全部阶段后逐字节恢复。
- 测试前后 `source-state` SHA-256 均为
  `74226BA4B4B034BAEDBA905F8D2E035913B170C6C586C4847A160E952C6760BB`。

AutoFishing 四档的实际完成鱼数和进程私有内存端点变化：

| 档位 | 完成鱼数 | owner/input/event/API/demand 根 | 私有内存端点变化 |
| --- | ---: | --- | ---: |
| L1 | 15 | 稳定 | `+2,588,672` |
| L3 | 18 | 稳定 | `-38,707,200` |
| L4 | 17 | 稳定 | `+1,077,248` |
| L5 | 10 个测量单位并完成标题/重载边界 | 稳定 | 见阶段收据 |

这些短窗既出现小幅上升，也出现明显回落；它们没有显示一致的单调
保留趋势，但 `QuantifiedProcessMemoryBudget=not-established`，不能把
结果解释为长期 GC 风险已经消失。

焦点租约和七个跨进程中断恢复边界另由
`tools/scripts/test-prerelease-active-gc-focus.ps1` 复跑通过。

## 静态热路径结论

| 产品 | 常驻路径 | 风险判断 |
| --- | --- | --- |
| AutoFishing | 仅启用自动钓鱼后订阅 `UpdateTicked`，每帧刷新原生状态并执行标量决策 | 主动场景风险最高，已由四档短测覆盖；字典/HashSet 是会话缓存，关闭和标题边界有清理收据 |
| ActionSpeed | 仅 AutoFill 开启时订阅每帧更新；其他动作由原生 Hook 驱动 | AutoFill/高动作吞吐需要焦点测量；普通关闭状态无产品 tick |
| DebugConsole | SaveLoaded 后订阅更新；关闭 UI 且没有 movement lease 时主要走快速返回 | UI 打开、物品枚举和高级诊断会产生临时集合；标题页 session 不活跃时立即返回 |
| AnimalHusbandryProgress | 常驻订阅更新，但只有一次 next-frame guard 处于 pending 时改写已有行 | 稳态只检查 guard；面板建立/刷新才分配列表和字典 |
| MoreSaves | 只在原生管理器应用/恢复失败、需要有限重试时订阅更新 | 正常稳态没有产品 tick |
| ManboCardboardAudio | WAV pending 时启用 demand updater；ready 后 pending 列表和 demand 清零，实际播放由 Hook 回调 | 启动载入是短时工作，不是持续目录扫描 |
| Zoom、OneActionComplete、FishBreedingAssistant、ChestLocatorEnhancer | 原生 Hook、输入或 UI 事件驱动 | 没有产品 `UpdateTicked` 常驻路径；功能触发时仍可能有短时反射/遍历分配 |
| 旧 MoreEquipmentSlots | 旧 Mod 只在 Entry/SaveLoaded 注册兼容 API；Compatibility Host 的 UI 更新由原生回调驱动 | 没有旧 Mod 每帧更新器；保存/迁移一致性风险与 GC 风险分开处理 |

## 一小时标题挂机与存档进入

结果：`Passed`。

运行形状：

- Steam 启动，主菜单连续等待 `3600` 秒；
- 上述 11 个功能产品均保持精确来源：9 个当前 Advanced 产品来自
  `Local`，旧 MoreEquipmentSlots 来自 Workshop `3744059735`，
  当前 Manbo 来自 `OfficialLocal`；
- `Code mod load-source` 恰好 `11` 条，Runtime 报告
  `committedTransactions=11`、`rolledBackTransactions=0`；
- Manbo WAV 只发起一次并进入一次 `LocalWavReady`，结束时
  `pendingRequests=0`、`asyncOperations=0`、`pendingUpdaterEntries=0`；
- 一小时后进入第三存档一次，原生 `LoadGame` 约 `2,152 ms`，
  `SaveLoaded=Passed`，在存档内停留 `15` 秒后返回标题；
- `SaveLoadCycle=Passed`，`requests/nativeEnter/nativeReturn/saveLoaded`
  均为 `1`，`duplicateRequests=0`；
- 路线是 `NoNativeSave`。第三存档 current/prev/bak 的长度、SHA-256
  和 mtime 全部在任何 runner/外部恢复之前保持不变；装备栏 committed
  sidecar 也保持不存在，没有 routine byte backup 或玩家存档写回；
- Fatal 窗口、可归因 Unity crash 和残留 `DolocTown.exe` 均为 `0`；
  QA Host、原生续接 Probe、事件和 owner 清理均通过。

OS 采样器每约 30 秒记录一次，共 `120` 个采样，其中 `119` 个属于
连续标题挂机、`Responding=false` 为 `0`：

| 窗口 | 工作集端点 | 工作集线性斜率 | 私有字节端点 | 私有字节线性斜率 |
| --- | ---: | ---: | ---: | ---: |
| 全部约 59.6 分钟 | `2,984.621 -> 3,050.152 MiB` | `+0.181234 MiB/min` | `4,116.988 -> 4,207.988 MiB` | `+0.217154 MiB/min` |
| 第 5 分钟后 | `3,046.668 -> 3,050.152 MiB` | `+0.061287 MiB/min` | `4,200.980 -> 4,207.988 MiB` | `+0.087078 MiB/min` |
| 最后 30 分钟 | `3,050.137 -> 3,050.152 MiB` | `+0.000257 MiB/min` | `4,207.980 -> 4,207.988 MiB` | `-0.000032 MiB/min` |
| 最后 15 分钟 | `3,050.152 -> 3,050.152 MiB` | `+0.000113 MiB/min` | `4,207.988 -> 4,207.988 MiB` | `+0.000227 MiB/min` |

全窗句柄范围 `1,490-1,513`、结束 `1,503`；线程范围 `127-136`、
结束 `128`。启动后约五分钟完成主要预热，最后 30 分钟的内存端点和
回归斜率接近水平，没有观察到句柄、线程或进程内存单调累积。

Runtime 最终健康快照还显示：

- `featureCount=14`、`failedFeatures=0`、`autoDisabledFeatures=0`；
- demand dispatch `670,332`、失败 `0`、reentry bypass `0`；
- event handler call `437,687`、失败和 quarantine 均为 `0`；
- ResourceLifecycle `titleIdleGrowthWarnings=0`、`errors=0`；
- 标题返回后 `saveOpen=false`，产品 owner 没有 cleanup failure 或
  needs-restart。

两项观察不应被隐藏：

1. 启动后约 7 秒出现一次 Input System frame-driver stalled 与一次
   missing-frame fallback 警告；约 4 秒后重新订阅并安装稳定
   PlayerLoop driver，此后整小时没有重复。它没有导致本轮失败，但仍是
   一次真实的启动恢复事件。
2. 旧订阅 `3759066788` 的 `item_tbitem.json` 被 ContentQuery 拒绝一次，
   `DomainGenerationRejected=1`。这不是上述 11 个功能产品的加载失败，
   但说明该旧内容载荷仍有无效 JSON。

证据：

- 原始 OS 采样与 runner 结果：
  `tmp/test-runs/all-functional-mods-20260731/one-hour-title-route`
- 游戏验收：
  `docs/debug/evidence/GAME-SMOKE/20260731-013049`

结论只适用于该精确候选、产品组合和时间窗。它复核了过去的“一小时主
菜单挂机后首次进档”已知路线，并未复现 Fatal GC；结合主动焦点短测，
未见当前产品拥有的 root、句柄、线程或预热后进程内存持续增长。它不
建立长期 gameplay 或 Mono/Unity 分区内存预算，也不关闭 `ISSUE-010`。

## 当前限制

- 一小时标题路线只能回答该已知“标题长挂机后首次进档”回归，不覆盖
  任意长时间 gameplay、房间反复切换、装备 UI 长时间操作或数百次钓鱼。
- OS 端点/趋势不能区分 Unity 原生保留、Mono managed heap、资源缓存和
  驱动层波动；主动焦点中的 Mono/Unity/根计数收据用于补充，但测量窗仍短。
- `ISSUE-010` 应保持 open。绿色结果只能说明测试配置和时间窗未复现
  Fatal GC，不能表述为“GC 问题已解决”。

## 恢复与交付检查

已完成：

1. `DolocTown.exe` 数量为 `0`。
2. 原游戏 `Mods`、`DTMAPI`、`BepInEx/plugins`、本地 Manbo、本地
   MoreEquipmentSlots 和 Author SDK 安装状态六棵树全部恢复到租约中
   的精确文件数、字节数和 tree SHA-256。
3. 根 `mod_infos.json` 恢复为
   `CDCA12350E29D2FF608B8AF05332F6CB92EFD9EDA7518BE3CF6A7B0381882402`；
   原生 `SAVE/mod_infos.json` 恢复为
   `267C8E5017D0A4735B3FFCFE9BD9106C8CD4233C7FEA0F5B67AB6A7318ECC30A`。
4. Workshop 源和备份的 `4,897` 个文件再次逐字节通过，零额外文件。
5. 实际上传目录保留新内容，并最终再次证明是冻结 30 文件加原
   `workshop.json`。
6. 共享 Runtime lock 已释放。

本轮没有修改 Runtime、DTMAPI 或任何功能产品的生产源码；新增和改动
仅限测试输出、审计记录与既有 GC 调查证据。
