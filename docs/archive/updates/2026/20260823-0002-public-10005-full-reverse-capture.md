# 20260823-0002：正式版 public 1.00.05 全量逆向捕获与分层差异

## Metadata

- Update ID: `20260823-0002`
- Date: `2026-08-23`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求捕获最新游戏内容。

## Scope

- 冻结当前 Steam `public` payload，执行 AssetRipper 全量 Unity 工程恢复与 ILSpy 主程序集/firstpass 反编译。
- 对 raw snapshot、AssetRipper export 和反编译输出建立完整哈希清单并验证冻结源一致性。
- 与最近的 public 观察头 `24650773_public_76C24E / 1.00.03` 做分层静态差异，记录本次更新实际触及的代码与内容边界；由于没有保留中间 `1.00.04`，以下是 `1.00.04–1.00.05` 的累计变化，不能把每一项强行归入其中一个版本。
- 官方字节、恢复资源和反编译源码只保存在 Git 忽略的本地 reverse 目录，不提交、不打包、不发布。

## Start Identity

- Steam app: `2285550`
- Steam build: `24788406`
- Branch: `public`；`UserConfig` 与 `MountedConfig` 均为 `public`，无 pending branch switch。
- Frozen manifest SHA-256: `071D890C9078705F7AE9B5381FF4832F51C6546D7A541F9FEEC170EE3A67719D`
- PlayerSettings/Application version slot: `1.00.05`
- `Assembly-CSharp.dll`: 6,398,976 bytes；SHA-256 `F06183B53D4F85A6A7D8D4073C73B9894B521E0718BBC302BF80581FEAB0F24C`
- 身份探测时 `DolocTown.exe` 进程为 PID `34684`；确认版本后按用户此前授权正常关闭，冻结后保持关闭。
- 运行中预探测曾看到 manifest SHA-256 `F54C0501…E94E2282`；Steam 在进程退出时重写了 manifest 元数据。正式捕获的 source-start、frozen 和 source-end 均为 `071D890C…3A67719D`，build、branch 和程序集身份未变，因此没有把退出时的 manifest 元数据写入误判为 payload 漂移。

## Changed Files

- `docs/updates/2026/20260823-0002-public-10005-full-reverse-capture.md`
- `docs/updates/INDEX-2026-08.md`
- `tools/portable-reverse-capture/common.ps1`
- ignored local capture under `references/doloc-town/reverse/builds/24788406_public_F06183/`

## Capture Result

| Layer | Result |
| --- | --- |
| Frozen raw snapshot | 542 files / 1,806,397,418 bytes；543 项含 manifest 的 source parity 精确通过；0 个 BepInEx、DTMAPI、MODS 或 doorstop 污染路径 |
| AssetRipper 1.3.14 | 52,073 files / 4,382,438,716 bytes；90 scenes、195 config tables、8 bundles、181 managed assemblies |
| ILSpy 9.1.0.7988 main | 3,671 files / 9,503,333 bytes |
| ILSpy firstpass | 29 files / 137,684 bytes |
| Known AssetRipper limitation | 93 errors，仍为 90 个 Cubemap size mismatch + 3 个 globalgamemanagers setting-object read error；没有新错误类别 |

## Difference From 1.00.03

### Packaging and scene boundary

- Raw 文件数仍为 542，体积增加 1,840,865 bytes。路径层面新增/移除各 4 个带内容哈希的 Addressables bundle 名称，另有 188 个共同路径哈希变化；这是 Unity 全量重打包与实际内容更新的混合结果，不能按 raw 数量直接推导功能数量。
- 181 个 managed assemblies 中只有 `Assembly-CSharp.dll` 变化；另外 180 个逐字节相同。`Assembly-CSharp-firstpass.dll` 及其 29 个反编译文件全部相同。
- Built scene 仍为 90 个，索引和原始 scene path 顺序完全相同，没有新增或删除地图/室内场景。90 个 scene 原始/导出字节均随 Unity 重打包变化；配置层只给旧城市废墟与湿地增加了“烈旱季 / 深雨季”副标题，不能把本次重打包当成新地图证据。
- 四个未改 bundle 中包含全部三个 Wwise bundle；`sound_tbcd.json` 仍为 32 行且 BGM event 不变，只把 `Woudn't It Be Lovely` 与 `The Bow Of Milk` 更正为 `Wouldn't It Be Lovely` 与 `The Bowl of Milk`。本次没有本地证据显示新增 BGM、CD 或音频资源。
- 本地 `AchievementSystem.cs` 逐字节相同，任务表无新增行；没有发现新的 Steam 成就本地定义。该结论不代表检查了 Steam 后台配置。

### Native code

- 主反编译从 3,666 增至 3,671 文件：新增 `AgentEquipmentFunctionBird`、`AgentEquipmentFuncProtoBird`、`AutomateTaskGatherTreeCrop`、`MonsterDecoratorBallDrone`、`PhotoBackground`，另有 60 个既有类型变化。
- 新“小鸟帽”是完整原生功能：物品、帽子、装备技能、动画与九种本地化均已加入；装备时触碰环境小鸟不再使其飞走，卸下时会重新触发当前接触中的小鸟。城市电话兑换码 `12315` 发送 `code_celebrate_1` 邮件并附带帽子，同时解锁奥兰多商店内以 3 个玉米兑换的条目。
- 自动无人机收到成组修复：空房间/空 inventory 防护，移动改为可失败，跨房间取料后直接继续填充发电机、动物槽和鱼缸，运输标签与容器可放入检查修正，种子加工保留同一基因组，并新增可重复树作物自动收获。鱼缸接口新增总容量语义，树作物公开成熟/可重复状态并增加无渲染收获路径。
- 全房间拍照从分块拼接主路径转为缩放相机路径，新增河谷、林地、湿地完整背景、照片背景实体、水面 UV 重算与恢复；普通拍照改用 confirm 的本帧边沿触发。
- Native save metadata 新增 `BaseArchiveData.historyVersions`，保存时累积先前版本；构造函数因此增加参数。新增 `1.00.04` 文档进度修复和 `1.00.05` 酒窖升级卡补发迁移，原 `0.96.07` 同逻辑被移到 `1.00.05`。
- 其余可见修订包括：无人机组件替换不再吞掉旧组件、背包/设备面板按字体和宽高比分语言偏移、数量输入上限 999、窗口分辨率不超过显示器、建筑扩展后二阶段回调、节庆/对话传送时抑制重复房间消息、球形无人机动画和年兽尾 AI 修正。

### Declarative content

- 195 个 GenDatas 中 31 个哈希变化，164 个完全相同。新增 1 封邮件、1 个物品、1 个物品排序、1 个装备技能、1 顶帽子、1 个商店解锁；九种语言各新增 8 个文本键。
- 经济发生明确再平衡：111 个已有物品的买价变化（99 上调、12 下调），103 个卖价变化（102 上调、1 下调）；无人机辅助组件明显降买价，多数食品、鱼和加工品卖价上调。
- 钓鱼/养鱼配置有 39 个 farm-fish 记录、3 个钓鱼参数记录和 4 个鱼池记录变化；闪电装备伤害从 50 提到 80，另有配方产物、场景季节副标题和多语言文本修正。

### DTMAPI boundary

- 静态消费者扫描没有发现当前生产代码调用本次发生签名变化的 `AutomateBot.Move`、`IFishTank.TotalEnergyCapacity`、`IDropItemHost.GenerateDropItemsWithBuff` 或 `BaseArchiveData` 构造函数。
- AutoFishing 使用的六个 `DolocUserInput` getter 未改；ChestLocator 使用的 `ArchiveDataHandle.GetAvailableInventories` 未改；MoreEquipmentSlots 读取的 `baseDataOnLoad` 既有字段以及其 `AgentEquipmentManager` / `AccessoriesBar.RenderPassiveItems` Hook 未改；CropHarvesting/ActionSpeed 只按既有树盆类型和既有动作路径工作。静态比较未产生必须立即修改 DTMAPI/第一方产品源码的结论。
- 这仍不是玩家行为验收，也没有自动迁移 Author exact policy、compatibility receipt 或发布权限。`24788406_public_F06183` 只成为最新 public reverse 观察头；任何兼容准入仍需独立的最小运行验证和权威更新。

## Validation

- Full capture exited `0`；source-start / frozen / source-end Steam identity 一致，raw source parity 为 exact。
- 独立重新枚举并 SHA-256 校验四棵树：raw `542/542`、AssetRipper `52,073/52,073`、main `3,671/3,671`、firstpass `29/29`，全部为零 missing、extra、length mismatch 和 hash mismatch。
- 对 raw、managed assembly、built scene、bundle、GenDatas、ILSpy main/firstpass 与 AssetRipper export 做路径/哈希分层比较；主代码结果为 5 added / 0 removed / 60 changed / 3,606 same，firstpass 为 29 same。
- 对 31 个变化配置表按稳定 `id` / localization `key` 比较，并单独核对新增帽子、邮件、商店、经济、钓鱼、scene subtitle、CD 和 Wwise 结果。
- 捕获入口首次预检暴露 portable `common.ps1` 缺少 `steam-appmanifest-identity.ps1` 所需的 `Get-DtmApiFileSha256`。补齐与仓库公共实现一致的只读流式 SHA-256 后，隔离身份探测返回 exact `24788406/public/071D890C…`；portable path-safety test 和 PowerShell parser 均通过。
- 文档治理、`git diff --check` 与最终 worktree/process/runtime-lock 检查通过。
- 不启动游戏，不执行玩家行为、完整 Release 或 Workshop 发布测试；本任务只形成静态 reverse 观察证据。

## Evidence

- Capture root: `references/doloc-town/reverse/builds/24788406_public_F06183/`.
- Capture summary: `full-baseline-inventory/summary.json` and `portable-full-capture-summary.json`.
- Source parity: `full-baseline-inventory/snapshot-source-parity.json`.
- Full inventories: `raw-snapshot-files.json`, `asset-ripper-export-files.json`, `Assembly-CSharp-decompiled-files.json`, `Assembly-CSharp-firstpass-decompiled-files.json`.

## Related Records

- Previous public observation head: `docs/updates/2026/20260810-0001-public-10003-full-reverse-capture-and-diff.md`.
- Previous derived difference audit: `docs/reviews/code/2026/20260811-0001-public-10003-baseline-difference-audit.md`.

## Rollback Notes

- 回退 `tools/portable-reverse-capture/common.ps1` 的单一 SHA-256 helper，并删除本 Update 的导航和新建的 ignored local capture directory 即可回滚；不得覆盖或删除既有 reverse build。
- 本任务不修改 Runtime、游戏安装内容、官方 `MODS`、Workshop 订阅或存档。

## Follow-Up

- 后续原生责任、资源或兼容研究以 `24788406_public_F06183` 作为最新 public reverse 观察头。
- 保持 `24456188` Author exact policy 和当前产品 receipt 不变；若要准入 1.00.05，另开 bounded compatibility / SDK / release lifecycle，不从本静态捕获自动继承。
- 如果需要精确区分 `1.00.04` 与 `1.00.05`，必须取得中间 payload；本记录不会根据最终累计状态倒推不存在的中间证据。
