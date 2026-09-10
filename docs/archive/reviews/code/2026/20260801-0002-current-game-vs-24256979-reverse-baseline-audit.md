# 1.00.00 最后测试分支全量反向基线与 24256979 差异审计

## 记录状态

- 日期：2026-08-01
- 状态：`recorded`
- 性质：只读官方 player 快照、AssetRipper 导出、ILSpy 反编译、结构化差异与本地基线准入审计；不是 Runtime、Hook、Mod 或游戏实现
- 旧基线：`references/doloc-town/reverse/builds/24256979_test_7A1907`
- 接受基线：`references/doloc-town/reverse/builds/24456188_test_E861E0`
- Source：用户要求全量解包、反编译本地最后测试版，将其作为未来基线，并先核对相对上一版的修改量
- Owning Update：[20260801-0004 Final Test Build Full Reverse Baseline](../../../updates/2026/20260801-0004-final-test-build-full-reverse-baseline.md)

本次没有安装或启动 DTMAPI/游戏，没有修改 Official MODS、Workshop、存档或共享 Runtime，也不需要取得 Runtime lock。所有官方 bytes、导出资源和反编译源码仍位于 Git 忽略的本地 reverse 区；Git 只记录 DTMAPI 自己撰写的计数、结论和边界。

## 一、最终裁决

本审计的完整性和差异证据**支持将 `24456188_test_E861E0` 准入为此后 DTMAPI 新设计、兼容性比较和 native-owner 复查的当前本地反向基线**。准入生命周期和最终验证状态由 owning Update `20260801-0004` 持有。

这份安装有两层必须同时保留的身份：

- Steam appmanifest 明确记录 build `24456188`、branch `test`，manifest SHA-256 为 `E2C5D50E6172C962CB02F58BC0E9931279FAEBC12EBFAFBD386309ECFFD85D67`；
- 两版 `globalgamemanagers` 的同一 PlayerSettings 序列化版本槽（文件偏移 `0x1278`）从 `0.99.08` 变为 `1.00.00`，而游戏标题页、存档和日志代码读取的正是 Unity `Application.version`。

因此可以把它称为 **1.00.00 最后测试分支基线**，也可以确认用户所说的“程序版本号已经是 1.0”。但不能把它写成从 Steam `public` 分支抓到的正式发布包；当前 manifest 仍是私有 `test` 分支。程序集自身的 Assembly/File/ProductVersion 仍为 `0.0.0.0`，不能拿 DLL 元数据代替游戏版本。

修改量不能压成一个总数。最可信的尺度是：**1 个游戏主程序集变化；180 个反编译文件触及；44/195 个配置表变化；19/133 个 Yarn 剧情脚本变化；Build Settings 场景身份 0 增、0 删、0 移动。**

## 二、捕获与完整性

| 边界 | 结果 |
| --- | --- |
| Steam 身份 | app `2285550`；build `24456188`；branch `test`；Unity `2021.3.16f1c1` |
| 主程序集 | 6,384,128 bytes；SHA-256 `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923`；MVID `f414ae62-d6e5-4a8d-9751-0800a80e9747` |
| raw snapshot | 452 files / 1,792,746,265 bytes；源目录开始、冻结快照和结束状态精确一致；另核对同一 Steam manifest |
| AssetRipper | 1.3.14；51,988 files / 4,381,941,141 bytes；22,747/22,747 对象导出并完成 post-export |
| ILSpy main | 9.1.0.7988；3,666 inventory files / 9,479,080 bytes |
| ILSpy firstpass | 29 files / 137,684 bytes |
| 恢复索引 | 90 个 Build Settings 场景、195 个配置表、8 个 Addressables bundle、181 个 managed assembly |

捕获脚本的 `snapshot-source-parity.json` 检查 452 个 game files 加 appmanifest，结果 missing/extra/mismatch 均为 0。完成捕获后又独立重新计算四组清单的每个 SHA-256，并对实际目录路径集合复核：raw `452/452`、AssetRipper `51,988/51,988`、main `3,666/3,666`、firstpass `29/29`，四组均为 missing 0、extra 0、mismatch 0。

raw snapshot 中没有 `BepInEx`、Mods、Saves、`Player.log` 或 `LogOutput.log`，也没有 0-byte 文件；本地 Runtime/Mod/存档没有混入官方基线。

AssetRipper 日志有 3 条非致命 `globalgamemanagers` 读取错误，涉及 type 129/PlayerSettings、BuildSettings 和 UnityConnectSettings；另有字体纹理和无效 Editor Script ID 警告。旧基线的 90 条 Cubemap 长度错误在这次没有重现。原始 bytes 完整，90 个场景原始路径和 index 均可精确恢复，但导出工程仍不是作者原始 Unity 工程。

## 三、分层修改量

| 层 | 旧版 | 新版 | 精确差异 | 可用结论 |
| --- | ---: | ---: | --- | --- |
| raw player 文件 | 542 | 452 | 新增 5、删除 95、同路径改变 192、不变 255 | 总体积净增 69,779,860 bytes；文件数下降主要是打包布局改变 |
| managed assemblies | 181 | 181 | 改变 1、不变 180 | 只有 `Assembly-CSharp.dll` 改变，依赖程序集集合未换 |
| ILSpy main inventory | 3,666 | 3,666 | 新增 1、删除 1、同路径改变 178、不变 3,487 | `git diff --no-index` 为 180 files、`+1,738/-615` 行 |
| ILSpy firstpass | 29 | 29 | 0 改变、29 不变 | firstpass 精确不变 |
| Build Settings scenes | 90 | 90 | 0 新增、0 删除、0 index 移动 | 没有新增原生场景身份 |
| GenDatas 配置表 | 195 | 195 | 44 改变、151 不变、0 增删表 | 有正式版收尾配置变化，但不是大规模换表 |
| Yarn | 133 | 133 | 19 改变、114 不变、0 增删 | 既有剧情/对白修订，没有新增 Yarn 文件 |
| AssetRipper export | 50,098 | 51,988 | 新增 1,961、删除 71、同路径改变 44,206、不变 5,821 | 仅作定位层，不能把 44,206 当成功能改动数 |

raw 层的 95 个删除恰好包括 90 个 `level*.resS` 和 5 个旧 content-hash bundle；新层加入 5 个新 hash 名 bundle。90 个 `level` 本体也都改变，但 90 个 scene path、index 和 `levelN` 映射完全一致。这说明资源被重新序列化/内联打包，不能写成“修改了 90 张地图”。

五个变化的 Addressables 域是 configs、GU assets、GU sprites、UU assets 和 Wwise data；另三份 bundle 不变。Wwise data bundle 从 1,038,478,992 增至 1,115,784,624 bytes，净增 77,305,632 bytes，证明封装音频数据发生变化，但在没有 bank 内逐事件/媒体解析前不能把体积差写成新增了多少首曲目。

## 四、主程序集与反编译源码

旧 `Assembly-CSharp.dll` 为 6,366,720 bytes、SHA-256 `7A19071CECBCC8F8F1174CC30A59E9A571F7C0A95EC5EE8E7FAFD9B5168E5B08`；新 DLL 增加 17,408 bytes，并有新 MVID。Mono.Cecil 0.10.4.0 递归计数结果：

| 指标 | 24256979 | 24456188 | 差值 |
| --- | ---: | ---: | ---: |
| 顶层类型 | 3,674 | 3,674 | 0 |
| 全部类型（含 nested） | 5,108 | 5,117 | +9 |
| 方法 | 45,770 | 45,887 | +117 |

ILSpy 新增 `DolocTown/CaptureScreenHelper.cs`，删除 `DolocTown.GameData/ArchiveOperationExtra.cs`。新类实现分块捕获完整房间并保存 PNG；与新增 `ui_tip_capture_full_room`、`ScreenManager`/`PhotoState` 变化共同构成完整房间截图能力。删除的旧扩展只说明 DLC 记录入口重组，不能单凭文件消失断言功能被删除。

其余 178 个同路径文件发生文本变化。较大的变化集中在官方 `ModManager`、屏幕/照片、怪物、环境渲染、无人机、机器/电力、物品/装备、种植、钓鱼、房间、版本迁移和 UI 等域。这个 178 是相同 ILSpy 版本生成的可复现文本差异，但仍是反编译结果，不等于作者原始源码提交数或独立功能数。

ILSpy inventory 的 3,666 不是 3,666 个类型文件：实际为 3,665 个 `.cs` 加 1 个 `.csproj`。Cecil 顶层类型中还有不生成独立文件的特殊元数据类型，以及四组泛型/非泛型同名类型共用文件，因此两组计数不矛盾。

## 五、配置和内容语义差异

配置行比较只统计 `Assets/Configs/GenDatas/*.json` 的顶层记录。普通数组优先使用稳定 `id`；无 `id` 的表使用自然键；`room_tbmaparea` 使用 `(map_id, area_id)`；`settings_tbusersetting` 使用 `title.key`，比较设置定义时排除单纯的位置性 numeric id 重排。按这个口径：

| 配置域 | 旧记录 | 新记录 | 新增 | 删除 | 修改 | 不变 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 全部 195 表 | 67,225 | 67,512 | 319 | 32 | 2,640 | 64,553 |
| `localization_*` | 57,616 | 57,876 | 292 | 32 | 2,398 | 55,186 |
| 非本地化 | 9,609 | 9,636 | 27 | 0 | 242 | 9,367 |

主要可见内容变化包括：

- 新增 4 封 `code_game_studio_1..4` 邮件；
- 新增 `apple_pie`、`lemon_soda`、`moonlight_song` 三组 item/eating-effect 记录；
- 新增 8 个 `dried_fish_*` 配方和 `easter_egg_exchange_shop`；
- 新增 `ruined_city_industrial_zone_1` 怪物生成记录；
- 新增两项设置：路灯夜间闪烁效果、动画中显示无人机，默认都为 true；106 个既有设置只发生 numeric id 重排，不能把这 106 个 numeric-id 变化误报成设置定义改变；
- `sound_tbcd` 仍为 32 行，但 32 行全部改变，CD title 从本地化对象改为普通字符串；简中 mapper 相应移除 32 个 `bgm_cd_title_*` 键；
- `room_tbmaparea` 只有旧城市 `ruined_city_200..208` 九条修改：均启用 `play_sound`，其中 206/207 另改为需要解锁；`room_tbroom` 有 22 条修改，主要为 spawn 信息；
- `room_tbscene` 仍为 93 行，仅 `city_湿地室内_女巫小屋` 的标题键变化。

19 个 Yarn 修改覆盖 `fish_guide`、`ruinedcity_main`、city/ruined_city/terraforming 交互、六份 NPC default、六份 NPC favorability、`zenis_cup` 和 `version_patch`。这是既有内容修订，不是 19 条新剧情。

上一基线的 `achievements.asset` 与新基线 SHA-256 均为 `37E558CC9D9197AB657B4908D90237ABA28BD3620947F444C67CD1141E28F4B1`，精确不变；本轮没有新增或删除游戏内成就任务图。

## 六、AssetRipper 噪声边界

两版都使用 AssetRipper 1.3.14、同一 archive SHA-256 `808CDDF66DD0357AD6B36B97DE3A2AEF5E3552E63AF3EE0610F9A03A0378101C`，所以工具版本差异已排除。但导出仍重新生成 Unity GUID/fileID/YAML 和部分 atlas 命名：共同路径中 24,396 个 `.meta`、18,030 个 `.asset`、90 个 `.unity` 都改变。

因此 `44,206` 只能回答“导出树有多少同路径文件 hash 不同”，不能回答“开发者实际改了多少资源或功能”。未来地图/API 研究应优先使用 scene identity、配置主键、raw bundle/level、native owner 和任务/portal 关系，而不是对导出 YAML 总差异做功能计数。

## 七、当前基线使用规则

1. 新 API、Hook、地图、官方 ModManager、版本迁移和 native-owner 研究先在 `24456188_test_E861E0` 重新打开相关类型/方法/配置；历史报告头部仍保留原作者基线，不倒改历史。
2. `24256979_test_7A1907` 继续保留为 1.00.00 前的重要差异基线，不清理。
3. Native Function Map 的 checked-in generated data 没有在本任务重建；它仍是自己的历史数据基线，不得把旧统计冒充 24456188 统计。
4. 若 Steam `public` 分支之后出现不同 build，再做 `24456188_test_E861E0 -> public build` 的同口径差异，而不是覆盖本基线。
5. 反向资料只用于本地兼容性和架构研究；不得发布官方 player、DLL、AssetRipper 工程或反编译源码。

## 八、验证

- 捕获脚本完成源目录开始/冻结/结束身份与哈希一致性检查。
- 对 raw、AssetRipper、ILSpy main、ILSpy firstpass 四组 inventory 做第二次全量路径、长度和 SHA-256 复核，全部 exact。
- 以相同 AssetRipper/ILSpy 工具版本做分层 hash 和 `git diff --no-index`；以 Mono.Cecil 对 DLL 元数据做独立类型/方法计数。
- 对 Build Settings、GenDatas、Yarn、managed assemblies、Addressables/Wwise、PlayerSettings 版本槽和成就任务图做定向语义比较。
- 未运行游戏；未修改 Runtime/Official MODS/Workshop/存档；未把官方或反编译文件加入 Git。
