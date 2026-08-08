# 正式版前测试分支全量反向基线审计

## 记录状态

- 日期：2026-07-21
- 状态：`recorded / baseline accepted with bounded AssetRipper limitations`
- 性质：只读内容差异、完整性与本地基线准入审计；不是 Runtime、Hook 或 Mod 实现
- 对比基线：`references/doloc-town/reverse/builds/23762374_public_C416D4`
- 接受基线：`references/doloc-town/reverse/builds/24256979_test_7A1907`
- Source：用户提供正式版前测试包，要求判断能否作为基线，核对 Steam 成就、地图、剧情、音乐和“CD”物品；可接受时清理原压缩包并移入项目私有反向资料区
- Owning Update：无。本任务只新增本 Review；正式游戏 bytes、反编译输出和本地基线 README 均位于已忽略的私有 reverse 区

本次没有安装或启动 DTMAPI/游戏，没有修改本地 Official MODS、Workshop、存档或共享 Runtime，也没有取得 Runtime lock。

## 一、最终裁决

**接受 build 24256979 作为正式版前的重要差异基线。** 它同时保留原始游戏快照、完整清单、ILSpy 输出和 AssetRipper 导出，原始快照及三个导出清单逐文件复核均无缺失或哈希漂移；新增旧城市废墟、两张农场、正式版主线、37 个预埋成就、BGM/CD 系统等内容证据完整，适合后续与 23762374 及正式版逐层比较。

接受范围不是“恢复了作者原始 Unity 工程”。AssetRipper 对 90 个 level 的 Cubemap 均报告长度错误，另有 3 个 `globalgamemanagers` 类型/设置错误；场景导出只能作为分析材料。原始 snapshot 完整，因此以后仍可用新版工具重跑。

规范目录名使用 `24256979_test_7A1907`，不是生成摘要中的 `24256979_public_7A1907`：随包保存的官方 `raw-snapshot/appmanifest_2285550.acf` 明确记录 `BetaKey "test"`，并把 build 24256979 归入用于 beta testing 的 `test` 分支。生成摘要和原始收据保持原样，不倒改历史证据；本地 README 记录了身份纠正。

## 二、完整性、导入与清理证据

| 边界 | 结果 |
| --- | --- |
| 原 ZIP | 3,339,070,086 bytes；SHA-256 `8D4CE50F6E020CA5E54D82E201998EC389C897BC02418B6E9C49F9216493E594`；55,118 个 central-directory entries；展开总字节 5,891,889,915 |
| 游戏身份 | Steam app `2285550`；build `24256979`；branch `test`；Unity `2021.3.16f1c1` |
| 主程序集 | `Assembly-CSharp.dll` 6,366,720 bytes；SHA-256 `7A19071CECBCC8F8F1174CC30A59E9A571F7C0A95EC5EE8E7FAFD9B5168E5B08` |
| raw snapshot | 542/542，缺失 0、额外 0、哈希不符 0 |
| ILSpy main | 3,666/3,666，缺失 0、额外 0、哈希不符 0 |
| ILSpy firstpass | 29/29，缺失 0、额外 0、哈希不符 0 |
| AssetRipper | 50,098/50,098 个清单文件存在且哈希一致 |
| 私人/运行时污染 | 未发现 `BepInEx`、Mods、Saves、`Player.log` 或 `LogOutput`；无 0-byte 文件项 |

ZIP 的中文文件名是未设置 Unicode flag 的 GBK 字节。PowerShell `Expand-Archive` 会按错误编码解释，造成中文名碰撞和不完整展开；接受版本改为从 central-directory 原始字节按 GBK 恢复，54,440 个原始文件全部展开。导出树中另有 90 个、合计 56,491 bytes、未进入任何生成清单的 `*.baiduyun.uploading.cfg`，它们是百度云传输临时侧车，不是游戏或反向输出；入库前精确删除，删除后 AssetRipper 实际文件数回到清单规定的 50,098。

入库、README 和最终程序集哈希复核通过后，再次核对原 ZIP 的 size/SHA-256，随后按用户授权删除 `D:\下载\DolocTown-Reverse-Baseline-24256979_public_7A1907.zip`。最终本地目录存在，原 ZIP 已不存在，incoming 临时目录已清空并删除。

## 三、Steam 成就差异

有新增，而且可从游戏内成就任务图精确证明：

- 23762374 的 `achievements.asset` 有 43 个成就 listener；24256979 有 80 个。
- 新基线新增 37 个 ID，旧 43 个没有删除，也没有修改触发定义。
- 新增域覆盖旧城市废墟与正式版流程、农场最大扩建、基础/20 台自动化无人机、湿地/旧城农场、作物基因、动物与异色鱼、五方势力声望、蘑菇/鲑鱼/新年节日以及若干隐藏行为。
- 代表性正式流程条件包括“抵达旧城市废墟”“完成最终广播”“见到庞卡”；这说明不是单纯新增 Steam 展示文本，而是游戏内触发任务已经预埋。
- 审计时 [Steam 官方全局成就页](https://steamcommunity.com/stats/2285550/achievements)仍公开显示 43 项。因此 80 项是测试分支侧的候选集合；其中新增 37 项尚不能写成公共 Steam 后台已经发布。正式版上线后应再次抓取 Steam schema/公开页，与这 80 个内部 ID 做一次最终对齐。

`AchievementSystem.cs` 和 `SteamHelper.cs` 本身未变；变化位于数据驱动的 `achievements.asset`。这也再次说明成就边界不能只做程序集文本 diff。

## 四、新地图资源

有明确的新地图资源，而且 Build Settings 层级没有重排旧场景：

- build scenes 从 85 增至 90；旧 85 个 scene path 没有删除，公共 scene index 没有位移。
- 新增 index 85-89：`farm_湿地农场`、`city_湿地室内_小木屋`、`dungeon_旧城市废墟`、`farm_旧城农场`、`city_旧城室内_废屋`。
- `room_tbscene` 从 88 增至 93，恰好增加上述 5 个场景。
- `room_tbroom` 从 125 增至 214，净增 89 个房间；旧城市废墟包含驻扎营地、郊野、住宅区、商业区、工业区、中央公园、下水道、发电站、死信号区及多组室内层级。
- `room_tbmaproom` 从 80 增至 165，净增 85 个可映射房间。

这些原生 scene path、房间配置与 Build Settings 的共同变化足以证明新地图已经进入候选构建。所有导出 level 的哈希都变化，但因 AssetRipper 重序列化和 Cubemap 错误，不能据此声称所有旧地图都发生了内容修改；精确旧图差异需进一步按 native object/配置做语义比较。

## 五、剧情与任务内容

有正式版主线延伸和大量配套剧情：

- `mission_tbmission` 从 89 到 96：新增 8、删除 1、修改 6。新增主线链包括 `ruinedcity_main`、`ruinedcity_siren` 和 `punk_main`，内容从湿地线索进入旧城市废墟、恢复区域供电、寻找并带回庞卡。
- `mission_tbfactionmission` 从 47 到 62，新增 15；包括旧城商业区/住宅区道路清理、旧城闲置地开辟、湿地码头通航、载具升级和多方势力交易。
- Email 从 170 到 200，新增 30；包含旧城继续线、广播、自动化、蘑菇节和多名镇民好感剧情。
- Yarn 从 125 个、1,015,516 bytes 增至 133 个、1,473,345 bytes；新增 8 个脚本，另有 55 个既有脚本内容变化。

本地证据与 [Steam 官方 1.0 公告](https://store.steampowered.com/news/app/2285550/view/677376450273215745)所述“旧城市废墟”和“主线结局”方向一致。基线足以研究新增剧情的任务图、事件、场景入口和 native owner，但 Review 不复制官方对话正文。

## 六、新音乐与 BGM 结构

有新增音乐内容，同时 BGM 配置架构发生整体重构：

- 旧 `sound_tbbgm.json` 被 `sound_tbbgmcondition`（784 rows）、`sound_tbbgmgroup`（31）、`sound_tbbgmoverride`（3）和 `sound_tbbgmplaymode`（3）替代。
- 在反编译代码与配置的联合事件集合中，旧基线有 27 个数字 BGM 事件（01-27），新基线有 32 个；新增 `PLAY_BGM_30`、`31`、`50`、`51`、`52`，没有丢失原 01-27。
- Windows Wwise data bundle 从旧 hash/name `...889dd8...` 变为 `...f08a7c...`，大小也变化，证明打包音频内容不是原包复用。
- 新增曲目中的 CD 标题包括 `Grounded`、`The Bow Of Milk`、`Whisper`、`Flamingo`、`Duallel Spirit`。

因此可以确认“至少 5 个此前不可按数字事件寻址的新 BGM 记录 + 新音频银行”。Wwise 媒体仍封装在 bundle，AssetRipper 没有导出可独立逐轨哈希的 WAV/WEM；在没有 Wwise bank 专用解析前，不把 bundle 差异夸大为逐音轨无损证明。官方开发日志曾预告 7 首新 soundtrack，但本审计本地能严格绑定到旧基线差异的是上述 5 个新增数字事件。

## 七、新物品“CD”

有，而且是完整可玩系统，不是占位物品：

- 新 `sound_tbcd.json` 定义 32 张 CD；32 项都能在 `item_tbitem.json` 找到对应 `special_cd` 物品和 `ItemFunctionCD`。
- 新增 `mp3`（随身听）物品，使用 `ItemFunctionCDPlayer`。
- 新增 CD 收藏/播放 UI 配置 `ui_tbcdmenu.json`，并有 CD panel、viewer、slot、playlist/all 分类资源。
- 程序侧新增 `CDManager`、`CDUiState`、`ItemCD`、`ItemCDPlayer`、`CDInfo/TbCD` 及对应 UI 类。
- 32 张 CD 覆盖 `PLAY_BGM_01-27` 的既有可收藏曲目和五个新增事件；`cd_18` 为默认解锁，其余可通过探索、交换、NPC 等来源获得。

这套数据对 DTMAPI 后续音频桥很有价值：游戏已经形成“曲目定义 / BGM 条件与播放模式 / 收藏物品 / 播放器 / UI”分层。后续 API 研究应先追踪 `CDManager` 和 BGM 条件解析的 native owner，不应继续以单个测试替换函数作为公共边界。

## 八、已知限制与后续建议

1. 用 24256979 做“正式版前差异基线”，并在 1.0 正式 build 出现后立即做 `24256979_test_7A1907 -> formal build` 的三层 diff：raw/config、decompiled native owners、scene/object inventory。
2. 修正便携抓取脚本的身份来源：分支名默认应从捕获的 appmanifest/Steam mounted config 推导；用户标签与 manifest 冲突时 fail closed 或显式记录两者，不能把 `public` 写成事实。
3. 便携包构建/验收应拒绝未进入 inventory 的 `*.baiduyun.uploading.cfg` 等传输侧车；中文路径必须验证 ZIP Unicode flag 或保存原编码元数据。
4. 正式版上线后复核 Steam 公共成就从 43 到 80 的后台映射；若数量或 ID 不同，以 Steam schema 为发布事实，以本基线任务图为测试分支历史事实。
5. 如要研究地图如何加入，优先比较五个新 Build Settings 条目、`room_tbscene/tbroom/tbmaproom`、portal/markpoint、对应 mission/Yarn 入口和场景加载 native owner；不要从 AssetRipper 生成的 Unity 工程结构直接推断作者原工程目录或制作流程。

## 九、验证

- 对 ZIP central directory 做路径安全、重复名、Unicode/GBK 和体积审计；纠正编码后完整展开。
- 对 raw、ILSpy main、ILSpy firstpass、AssetRipper 四组清单逐文件计算 SHA-256 并核对 missing/extra/mismatch。
- 对旧/新 Build Settings、配置表、Yarn、成就任务图、BGM event、Wwise bundle 和 CD/播放器类做差异解析。
- 最终目录再次核对 `BetaKey=test`、程序集 SHA-256、无百度云侧车；原 ZIP 删除前再次核对原 size/SHA-256。
- 未运行游戏；没有把官方 DLL、官方资源或反编译源码加入 Git。
