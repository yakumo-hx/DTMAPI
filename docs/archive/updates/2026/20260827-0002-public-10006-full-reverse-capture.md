# 20260827-0002：正式版 public 1.00.06 全量逆向捕获与公告核对

## Metadata

- Update ID: `20260827-0002`
- Date: `2026-08-27`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求捕获官方公告所述的多洛可小镇 `1.00.06` 最新内容，并核对实际 payload。

## Scope

- 冻结当前 Steam `public` payload，执行 AssetRipper 全量 Unity 工程恢复与 ILSpy 主程序集/firstpass 反编译。
- 对 raw snapshot、AssetRipper export 和反编译输出建立完整哈希清单，并验证冻结源与安装源一致。
- 与最近的 public 观察头 `24788406_public_F06183 / 1.00.05` 做分层静态差异。
- 按用户提供的官方公告逐项核对 25 项修复和 3 项已知问题，区分代码/配置/资源证据与静态包内无法独立证明的运行行为。
- 官方字节、恢复资源和反编译源码只保存在 Git 忽略的本地 reverse 目录，不提交、不打包、不发布。

## Start Identity

- Steam app: `2285550`
- Steam build: `24966367`
- Branch: `public`；`UserConfig` 与 `MountedConfig` 均为 `public`，无 pending branch switch。
- Source manifest SHA-256: `BBFA6BCA27BCCD42B64955D9374B1A05C03C7EDC529AA4BE295EDF9B43094CF2`
- PlayerSettings/Application version slot: `1.00.06`
- `Assembly-CSharp.dll`: 6,400,000 bytes；SHA-256 `958EAFA0D141B5CD00E1D4E0DB77F789C990A5795B05E9F59EA40AA001734607`
- 身份探测时没有运行中的 `DolocTown.exe`；本任务不需要主动关闭游戏。

## Changed Files

- `docs/updates/2026/20260827-0002-public-10006-full-reverse-capture.md`
- `docs/updates/INDEX-2026-08.md`
- ignored local capture under `references/doloc-town/reverse/builds/24966367_public_958EAF/`

## Capture Result

| Layer | Result |
| --- | --- |
| Frozen raw snapshot | 542 files / 1,881,782,272 bytes；542 个 payload 文件与冻结源逐文件长度/SHA-256 exact，含 manifest 的 543 项 source parity exact |
| AssetRipper 1.3.14 | 52,089 files / 4,456,193,061 bytes；90 scenes、195 config tables、8 bundles、181 managed assemblies |
| ILSpy 9.1.0.7988 main | 3,671 files / 9,505,273 bytes |
| ILSpy firstpass | 29 files / 137,684 bytes |
| Known AssetRipper limitation | 93 errors；仍为 90 个逐 scene Cubemap size mismatch 与 3 个 custom-version `globalgamemanagers` setting-object read error，没有新增错误类别 |

全量捕获退出码为 `0`。source-start、frozen、source-end 的 build、branch、manifest 和主程序集身份一致；游戏安装内容在任务期间只读，捕获结束后共享 Runtime lock 已正常释放。

## Difference From 1.00.05

### Packaging and stable boundaries

| Layer | Added | Removed | Changed | Same | Size delta |
| --- | ---: | ---: | ---: | ---: | ---: |
| Raw snapshot | 4 | 4 | 187 | 351 | +75,384,854 bytes |
| AssetRipper export | 24 | 8 | 45,910 | 6,155 | +73,754,345 bytes |
| ILSpy main | 0 | 0 | 15 | 3,656 | +1,940 bytes |
| ILSpy firstpass | 0 | 0 | 0 | 29 | 0 |

- Raw 的 4 added / 4 removed 都是内容哈希进入文件名后的 bundle 替换；文件总数仍为 542。181 个 managed assemblies 中只有 `Assembly-CSharp.dll` 变化，另外 180 个逐字节相同。
- 90 个 built scene 的原始路径、索引和导出映射完全一致，没有新地图或室内场景。90 个 raw/export scene 哈希都因 Unity 重打包变化，不能把它们当成 90 项场景修改。
- 8 个 bundle 中 4 个换成新哈希名、4 个逐字节相同。三个 Wwise bundle 全部相同，`sound_tbcd.json` 也相同，因此本补丁没有新增或替换 BGM/CD/音频字节。
- 195 张配置表中 16 张变化、179 张相同；主反编译没有新增或删除类型，只有 15 个既有类型变化，diff 规模为 146 insertions / 83 deletions。

### Native code and declarative content

15 个变化类型集中在以下边界：

- 节日/任务/迁移：`DolocAPI`、`FestivalState`、`NpcTaskRenderController_Lightman`、`CommandDefines`、`VersionPatchFunctions`。
- 无人机/房间/农场：`AutomateBotDecisionMakerLogistics`、`AutomateCaseRecorder`、`AutomateParamProcessing`、`BuildingManager`、`ArchiveDataHandle`、`ArchiveOperationCity`。
- 配方/图鉴/烹饪：`TbRecipe`、`TbRecipeGroup`、`PlantDocumentManager`、`Dish`。

内容侧可稳定区分为：

- 8 个 Yarn 文件变化：`archive_chip_guide`、`archive_plant_guide`、`motor_guide`、`trash_can`、`claud_default`、`siren_default`、`zenis_favorability`、`evernight_festival`。
- 16 张配置变化：钓鱼 2 张、物品 1 张、配方及配方组 2 张、房间 1 张、任务 1 张、9 张本地化表。
- 俄语/韩语字体修复不是字符表扩容：两个韩文字体分别仍为 19,689 / 21,152 个 codepoint，既有 `zpix` 仍为 22,235 个。新包增加两个等字符表的 `zpix_base` 字体资产及两张 atlas/两份材质，并把韩文字体的空 fallback 补成 `zpix_base -> SourceHanSansJP`，同时把既有 `zpix` 中的空 fallback 替换为可解析的韩文/日文字体引用。
- 新增两份野生蘑菇灯光遮罩 sprite 及对应 atlas 变化；设备图集以新哈希资源替换。
- 成就图 `_serializedGraph` 仍为 81 个节点，只有 `_000_020_030 / 躺平农场主` 一个节点发生语义变化：20 个自动无人机计数条件新增 `compareMethodOfInt: 3`。
- `dungeon_环境改造器洞穴` 仍是 build index 78，没有新增 scene；其 CompositeCollider 外边界、Tilemap（1,783 -> 1,851 tiles，增加 x=42/43 列）及洞口 schedule `roomId` 明确变化，和公告中的卡地形修复相符。

## Official Announcement Cross-Check

以下“直接”表示 1.00.05 -> 1.00.06 静态差异能定位到对应 owner；它证明补丁实现存在，不等于已完成玩家运行验收。25 项修复中，23 项有直接差异，1 项只有相关生命周期差异，1 项实现已在上一 payload 中存在。

| # | 公告项 | 静态结论 | 1.00.06 实际证据 |
| ---: | --- | --- | --- |
| 1 | 小飞象章鱼黄条速度 | 直接 | `dumbo_octopus.note_speed_multiplier` 从 `8` 降到 `6.5`。 |
| 2 | 垃圾桶可能翻不出垃圾 | 直接 | `trash_can.yarn` 为未命中既有分支的路径增加普通小垃圾掉落跳转。 |
| 3 | 年夜饭偶发未进入节日状态 | 相关、非独立证明 | 新增回家时清除残留 `currentFestivalInfo` 的生命周期收口；年夜饭入口 routine graph 本身未产生唯一对应差异，仍需运行复现才能把这一条单独闭环。 |
| 4 | 年夜饭 NPC 层级异常 | 直接 | `FestivalState.InitNpcInFestival` 初始化节日 NPC 时新增 sorting 信息重置。 |
| 5 | 长夜节克劳德美味炖菜任务无法完成 | 直接 | `claud_default.yarn` 将 `hult_favorability1` 完成触发移出礼物等级 2 的单一分支，使其它合格礼物路径也能收口。 |
| 6 | 长夜节重读档后传送仍受限 | 直接 | `DolocAPI.ReturnHome` 在恢复输入前调用 `FestivalState.ClearOnReturnHome`，清除传送白名单读取的残留节日信息。 |
| 7 | 泽尼瑟好感剧情选项未保存 | 直接 | `zenis_favorability.yarn` 对应返回农场选择后新增 `save_game`。 |
| 8 | “拾荒者的秘密行动”无法结束 | 直接 | `motor_guide.yarn` 补发缺失的 `hult_communicator` 获得事件；`UpdateTo10006` 同时为已提交但缺事件的旧档补广播。 |
| 9 | “售货机里的机械体”无法结束 | 直接 | `siren_default.yarn` 在两个任务事件间增加 `0.1s` 时序间隔；1.00.06 迁移也为已到达目标对话的进行中旧档补事件。 |
| 10 | “躺平农场主”不触发 | 直接 | 81 节点成就图中仅该节点改变，为 20 台自动化无人机条件补 `compareMethodOfInt: 3`。 |
| 11 | Mod 作物影响“博古通今” | 直接 | 新成就校验命令按芯片已提交数 `>=14` 和植物已解锁文档数 `>=30` 分开广播；芯片/植物图鉴 Yarn 和旧档迁移均调用，不再按可被 Mod 扩展的总表行数判定。 |
| 12 | 物流无人机反复拿取 | 直接 | 输入容器搜索明确排除 `equipment == OUT`，并收紧标签/可放入检查。 |
| 13 | 运输无人机异常导致存档/房间加载失败 | 直接 | 输入箱查找不再无条件返回成功，而以实际容器非空为准；加载后还会移除配置中已经消失的保存配方 ID。下游存档/读房结果未做运行验收。 |
| 14 | 地窖内保存后回到主农场 | 直接 | `BuildingManager.QueryBuildingRoom` 在 GUID 字典未命中时回退按房间 `Title` 查找。 |
| 15 | 旧档地窖等级未升级/邮件补凭证 | 上一 payload 已存在 | 对应 `UpdateTo10005` 地窖升级卡补发逻辑已在 1.00.05 基线，本次 15 个变化类型里虽有 `VersionPatchFunctions`，但没有新增这段逻辑；公告不能被误记为 1.00.06 新字节。 |
| 16 | 湿地/旧城农场一周期更新两次 | 直接 | `ArchiveDataHandle.UpdatePerSec` 移除当前 Farm 根不是主农场时对主农场根的第二次更新。 |
| 17 | 湿地室内天气异常 | 直接 | 房间表移除旧的独立女巫小屋行，新增废水处理厂、女巫小屋、蓄水池三个湿地室内房间，并统一 `season_group: wetland`。 |
| 18 | 自由配方读档后不能生成美味食物 | 直接 | 自由配方价值计算从只加单件售价改为 `售价 × 原料数量`。 |
| 19 | “废土音响”出现在设备工作台 | 直接 | `sound` 从设备工作台配方组和配方表移除；物品仍保留，卖价从 `-1` 调为 `150`。 |
| 20 | 改造器洞穴卡地形 | 直接资源证据 | build index 78 的 collider、tilemap 边界和洞口 `roomId` 明确改变；未启动游戏验证实际碰撞。 |
| 21 | 野生蘑菇灯灯光异常 | 直接资源证据 | 新增两份灯光遮罩 sprite，并替换设备 atlas；未做视觉运行验收。 |
| 22 | Mod 配方不能被加工无人机加工 | 直接 | `TbRecipeGroup` 的派生 recipe map 改为按当前 `DataList` 懒构建；加载无人机时清理已不存在的保存配方 ID。 |
| 23 | Mod 配方图鉴异常 | 直接 | `TbRecipe` 与 `TbRecipeGroup` 均将派生 map 改为懒构建，`PostResolve` 负责清空缓存；这改善加载阶段追加配方，但不承诺首次访问后任意运行期突变会自动使缓存失效。 |
| 24 | 韩语/俄语像素字体缺字 | 直接资源证据 | 字符表数量不变；修复来自新增 `zpix_base` 资产、atlas/material 与完整 fallback 链，属于引用/回退修复。视觉字形未运行验收。 |
| 25 | 文本及本地化 | 直接 | 9 张本地化表和 1 条任务文本变化；包含地牢农场引导、英文档案/基因/物品文本、俄日韩等文本修订。8 个非简中表还移除同一组 32 个 `bgm_cd_title_*` 键；`sound_tbcd.json` 与 Wwise 不变，这是重复/回退键清理，不是删除 CD 或音乐。 |

公告列出的 3 项已知问题与 payload 相符，均不应在本次静态捕获中误报为已修复：

| 已知问题 | 1.00.05 -> 1.00.06 观察 |
| --- | --- |
| “希望”基因作物被采集无人机收获后，睡醒时返还种子未及时保存 | 15 个变化类型和 16 张变化配置中没有对应种子/睡醒保存 owner 的修订；公告给出的等待一秒再手动保存只是临时规避，不形成 DTMAPI 保存语义。 |
| 年夜饭开始后触发部分支线导致节日状态异常 | 没有能关闭该竞态的唯一 routine/code 差异；本补丁的节日清理只能解释已发布修复的一部分，不能覆盖这项公开未解决问题。 |
| 帕依雅商店岛屿家具配方不能解锁 | 商店/岛屿家具解锁表和对应剧情没有相关变化，保持公开未解决。 |

## DTMAPI Boundary

- 对 15 个变化类型做生产源码/QA/tests 消费者扫描。DTMAPI 生产代码现有 `ArchiveDataHandle.GetAvailableInventories` Hook 仍存在，但本补丁改的是同类型的 `UpdatePerSec`，目标方法与签名未改；`TbRecipe` / `TbRecipeGroup` 的直接读取只出现在 Content QA fixture。没有发现当前生产代码调用本次新增/变化成员，亦没有 public signature removal。
- 这次官方补丁改善了 JSON 追加配方在加工无人机和图鉴中的官方消费路径，是内容 Mod 的正向兼容信号；它不自动证明任意动态注入时序，也不自动形成 DTMAPI Content Host、配方 API 或 SharedNative owner 的准入依据。
- 公共节日、无人机、图鉴、房间和字体 owner 都属于官方游戏内部实现。本次无需修改 DTMAPI Runtime 或第一方产品源码。
- 捕获只把 `24966367_public_958EAF` 推进为最新本地 public reverse 观察头；Author exact policy、compatibility receipt、产品 admission、订阅清单和发布授权均不随静态捕获自动变化。

## Validation

- Full capture exited `0`；source-start / frozen / source-end Steam identity 一致，raw source parity 为 exact。
- 独立重新枚举并 SHA-256 校验四棵树：raw `542/542`、AssetRipper `52,089/52,089`、main `3,671/3,671`、firstpass `29/29`；均为 0 missing、0 extra、0 length mismatch、0 hash mismatch。首次校验命令把 Windows 分隔符替换模式写成双反斜杠，产生了纯路径假 mismatch；立即改为单反斜杠后 exact，通过清单和真实文件哈希确认没有捕获缺失。
- 分别对 raw、AssetRipper export、managed assemblies、built scenes、bundles、config、Yarn、ILSpy main/firstpass、成就序列化图、洞穴 scene、字体字符表与 fallback 引用做稳定路径/语义比较。
- 对 DTMAPI `src`、`mods`、`tests` 做变化 owner 消费者扫描，确认唯一相关生产 Hook 的目标方法未变。
- 文档治理通过 7,071 项检查，目标文档 `git diff --check` 通过；最终确认 `DolocTown.exe` 未运行、共享 Runtime lock 为 free、新 reverse 目录仍由 `.gitignore` 隔离。
- 不启动游戏，不执行存档、玩家行为、完整 Release 或 Workshop 发布测试；静态证据充分用于捕获与差异解释，但公告中标为运行行为的结果仍需后续按需做最小 smoke。

## Evidence

- Capture root: `references/doloc-town/reverse/builds/24966367_public_958EAF/`.
- Capture summary: `full-baseline-inventory/summary.json` and `portable-full-capture-summary.json`.
- Source parity: `full-baseline-inventory/snapshot-source-parity.json`.
- Full inventories: `raw-snapshot-files.json`, `asset-ripper-export-files.json`, `Assembly-CSharp-decompiled-files.json`, `Assembly-CSharp-firstpass-decompiled-files.json`.
- Built scene identity: `full-baseline-inventory/built-scenes.json`.

## Related Records

- Previous public observation head: `docs/updates/2026/20260823-0002-public-10005-full-reverse-capture.md`.

## Rollback Notes

- Delete this Update's navigation row and the new ignored local capture directory to roll back the capture record; do not overwrite or delete any existing reverse build.
- This task does not modify Runtime, the game installation, official `MODS`, Workshop subscriptions, or saves.

## Follow-Up

- 后续逆向/原生责任研究以 `24966367_public_958EAF` 作为最新 public 观察头；保留 `24788406_public_F06183` 作为直接差异基线。
- 不因本次静态捕获更改当前发布兼容权威。若要正式准入 1.00.06，应另开 bounded compatibility lifecycle，并只验证实际受影响的 Hook/产品。
- 三项官方已知问题保持开放；尤其“希望”基因种子涉及睡醒与保存时序，后续如调查必须遵守 `PROJECT.md` 的 native save commit / NoNativeSave 边界，不能用手工回写玩家存档替代语义证据。
