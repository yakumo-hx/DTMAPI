# 20260827-0001：1.00.05 四动物内容桥第七存档运行验收

## Metadata

- Update ID: `20260827-0001`
- Date: `2026-08-27`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 用户要求沿用过去的游戏测试方法，确认正确的畜棚存档位并在当前游戏版本中实际检查 Hatch、Oilfloater、Mole、Drecko 四个官方本地动物内容包是否能够正常加载和按预期显示。
- Review: `docs/reviews/api/2026/20260827-0001-livestock-interface-10005-official-content-audit.md`

## Accepted Boundary

- 权威 fixture 是 UI 第七存档“**大型畜棚**”；游戏内部加载索引为 `6`。第五存档属于 AutoFishing，不用于本次畜棚验收。
- `Local.DTMAPI_MoreSaves` `1.0.1` 只是把原生存档 UI 扩展到 12 格、从而进入第七档的 fixture 访问依赖；它不是第五个动物包，也不是本次行为验收对象。
- 四个被测内容包是 `Local.DTMAPI_HatchAssets`、`Local.DTMAPI_OilfloaterAssets`、`Local.DTMAPI_MoleAssets`、`Local.DTMAPI_DreckoAssets`。运行时隔离关闭其他官方/本地产品，清理阶段恢复原始 `mod_infos.json`。
- 运行分类为 `NoNativeSave`：没有进入任何原生保存路径；退出前比较 UI 第七档对应的 `doloc-save-6.data`、`.bak`、`.prev0` 及相关已提交侧车的长度、SHA-256 和 mtime；绿色路径不创建例行备份、不回写玩家存档，也不依赖外部恢复。
- 验收对象是 Steam public build `24788406` / 画面版本 `1.00.05` 与当前安装的 DTMAPI Runtime `0.6.1`。本记录不是未来发布或上传授权。

## Changed Files

- 本 Update 与 2026-08 月度 ledger 记录本次运行边界和结果，并追加 Hatch、Mole 同时复用 chicken 模板的最小运行探针。
- 前序静态 API Review 增加后续运行解析链接。
- Active smoke matrix 增加四动物可视 + Hatch 声音运行行和双 chicken 模板运行行。
- 没有修改 Runtime 源码、Steam 订阅内容或玩家存档。双 chicken 探针只临时修改本地 Mole 包的五个配置文件，退出后已恢复为探针前的精确 58 文件树。

## Validation

### 包与部署预检

- 四包仍是历史验收过的完整本地素材：Hatch `11 JSON / 44 PNG / 2 WAV`，Oilfloater `11 / 48 / 2`，Mole `11 / 45 / 2`，Drecko `11 / 36 / 2`。
- 旧的 `-IncludeHookProbe` 联合命令先完成当前工作树构建（0 error，DebugConsole 单元构建保留 16 条既有 nullable warning），随后被当前安装边界以 `DTM-E1999` 在部署前阻止；没有发生 Runtime、官方 `MODS` 或启用状态变更。
- 改用受支持边界 `tools/scripts/install-to-game.ps1 -SkipBuild -SkipOfficialLocalMods`，返回 `DTM-S1001`；五个 Runtime DLL 与安装状态验证通过。

### 第七档人工可视验收

- `GAME-SMOKE/20260827-103136` 是安全的入口预检：仅启用四动物包时原生 UI 只显示 1–6 档，未加载任何存档即退出；该 PASS 只证明无保存/清理基线，并确认第七档需要 MoreSaves 访问依赖，不算动物验收。
- `GAME-SMOKE/20260827-103445` 临时只启用四动物包和 MoreSaves。UI 显示第七档“**大型畜棚**”，在确认官方“模组变更”提示后成功进入；日志证明 `SaveLoaded slot=6`。
- 夜间加载时四类动物都在畜棚底部执行睡眠状态；等待至清晨后，画面可直接分辨 Hatch 的白紫拱背、Oilfloater 的紫色团块、Mole 的粉色卷体、Drecko 的青色四足体。各类均显示自有像素素材并活动，没有粉块、空白帧或原版模板外观回退；只悬停观察到“浮游生物”交互提示，没有按下互动键。
- 日志逐种验证 `AnimatorBridge`、AI template 与 `PngSpriteBridge`：`hatch -> chicken`、`drecko -> goat`、`oilfloater -> slime`、`mole -> marsh_pangolin`，四种 custom sprite 首帧都映射成功；睡眠诊断分别观察到 `hatch`、`drecko`、`oilfloater`、`mole`。最终健康快照为 `failedFeatures=0`，custom animals `degraded=0, warnings=0`，结果中的 `MissingFrameFallbackWarningCount=0`。
- 四包共 8 个成年/幼体 WAV 都在启动阶段进入 `local WAV ready`；这一条证明文件与平台播放后端可用，不冒充四种动物逐一互动播放证据。
- 从游戏内“退出游戏”选择确认丢弃未保存数据；测试器在任何 runner/external restore 前证明 `doloc-save-6.data`、`.bak`、`.prev0` 与两条目标侧车均不变。`PlayerArchiveWritebackPerformed=False`、`RoutinePlayerSaveByteBackupCreated=False`、`PlayerSaveRestoreRequired=False`，进程退出、无新 fatal window、原始 Mod profile 恢复均 PASS。

### Hatch 成年/幼体声音事件

- `GAME-SMOKE/20260827-104337` 以同一第七档、相同隔离 profile、`StageQaHost + AutoExerciseHatchAnimalVoice` 运行。
- 幼体 `PLAY_ANIMAL_PET_CHICKEN_CHILD` 与成年 `PLAY_ANIMAL_PET_CHICKEN` 均记录 `played=True suppressed=True`，分别命中 `hatch_pet_young.wav` 与 `hatch_pet_adult.wav`；`Smoke.HatchAnimalVoice = verified`，`fallback=false`。
- 该运行的 `HatchAnimalVoice`、`SaveLoaded`、QA host lifecycle/cleanup、NoNativeSave 存档不变、进程退出、无 fatal window 和 profile 恢复全部 PASS；没有原生存档写回。

### Hatch 与 Mole 同时复用 chicken 模板

- `GAME-SMOKE/20260827-135817` 临时保留 `speciesId=mole` 及 Mole 的 animator key、PNG 前缀、袋子/产物/表主键，只把其 `templateSpeciesId`、`aiTemplate`、`schedule_id`、模板 sprite 前缀、帧清单和阶段声音事件成组切换到 `chicken`；Hatch 继续使用 chicken，Drecko 与 Oilfloater 保持原模板。
- chicken 模板需要的 44 个 PNG 后缀全部由 Mole 的 45 帧素材覆盖，静态缺失数为 0。第七档“大型畜棚”成功加载，四类动物均在夜间睡眠位显示，没有粉块、空白帧或原版 chicken 外观回退。
- 日志同时注册 `hatch->chicken,mole->chicken`，并分别验证两者的 `Chicken_FreeTimeState`。Hatch 的 chicken controller / sprite 请求映射到 `anim_animal_hatch`，Mole 的 chicken child controller / sprite 请求映射到 `anim_animal_mole`；两个物种没有覆盖对方。两个 audio owner 也对相同 chicken 幼体/成年事件分别完成 generation commit。
- 本次按用户要求只做加载观察，没有互动、等待生产、收取产物、繁殖或测鸡窝满载；因此它验证的是重复模板的注册、AI、Animator、PNG 和声音 scope 注册不冲突，不将“加载成功”扩大为全部畜牧生命周期验收。
- `RunStatus=Passed`、`MissingFrameFallbackWarningCount=0`、`PlayerSaveUnchangedBeforeCleanup=Passed`、`CommittedSidecarsUnchangedBeforeCleanup=Passed`、`PlayerArchiveWritebackPerformed=False`、`RoutinePlayerSaveByteBackupCreated=False`、`PlayerSaveRestoreRequired=False`、`ProcessExited=Passed`、`NoFatalInstanceWindow=Passed`、`OfficialModProfileRestored=True`。
- 游戏退出后，五个临时配置从探针前备份恢复；Mole live/backup 均为 58 个文件，逐路径长度与 SHA-256 完全相同，树摘要为 `8C3F37E06E3314534F8FCB06BE1EB55B83FFAE58E5F68B843E8487C2B9AF889A`。最终无 `DolocTown.exe`，Runtime lock 已释放。

### 文档收口

- `tools/scripts/check-doc-governance.ps1`：PASS，`Document governance: OK (7040 checks)`。
- 对本 Update、月度 ledger、API Review 和 active smoke matrix 执行 `git diff --check`：exit `0`，没有 whitespace error；仅提示两个既有已跟踪 ledger 文件未来由 Git 接触时会按工作区规则转换 LF/CRLF。

## Evidence

- 入口预检：[GAME-SMOKE/20260827-103136](../../../debug/evidence/GAME-SMOKE/20260827-103136)
- 第七档可视运行：[GAME-SMOKE/20260827-103445](../../../debug/evidence/GAME-SMOKE/20260827-103445)
  - [DTMAPI-latest.log](../../../debug/evidence/GAME-SMOKE/20260827-103445/DTMAPI-latest.log)
  - [player-save-unchanged-before-cleanup.json](../../../debug/evidence/GAME-SMOKE/20260827-103445/player-save-unchanged-before-cleanup.json)
  - [committed-sidecar-unchanged-before-cleanup.json](../../../debug/evidence/GAME-SMOKE/20260827-103445/committed-sidecar-unchanged-before-cleanup.json)
  - [official-mod-profile-restore-verification.json](../../../debug/evidence/GAME-SMOKE/20260827-103445/official-mod-profile-restore-verification.json)
- Hatch 声音运行：[GAME-SMOKE/20260827-104337](../../../debug/evidence/GAME-SMOKE/20260827-104337)
  - [DTMAPI-latest.log](../../../debug/evidence/GAME-SMOKE/20260827-104337/DTMAPI-latest.log)
  - [result.json](../../../debug/evidence/GAME-SMOKE/20260827-104337/result.json)
- 双 chicken 模板运行：[GAME-SMOKE/20260827-135817](../../../debug/evidence/GAME-SMOKE/20260827-135817)
  - [DTMAPI-latest.log](../../../debug/evidence/GAME-SMOKE/20260827-135817/DTMAPI-latest.log)
  - [result.json](../../../debug/evidence/GAME-SMOKE/20260827-135817/result.json)
  - [player-save-unchanged-before-cleanup.json](../../../debug/evidence/GAME-SMOKE/20260827-135817/player-save-unchanged-before-cleanup.json)
  - [committed-sidecar-unchanged-before-cleanup.json](../../../debug/evidence/GAME-SMOKE/20260827-135817/committed-sidecar-unchanged-before-cleanup.json)

## Rollback Notes

- 三次有效运行均已恢复测试前 `mod_infos.json`，没有需要回滚的存档变化；双 chicken 探针临时修改的 Mole 配置已逐文件恢复并通过整个 58 文件树的精确比较。
- 当前 Runtime `0.6.1` 安装由受支持安装器完成并保留；本次验证未证明需要回退。
- QA host 已由 runner 清理；最终没有 `DolocTown.exe` 残留，共享 Runtime lock 已释放。

## Follow-Up

- 前序静态 Review 的 `runtime acceptance pending` 已由本 Update 解析为当前 public `1.00.05` / 当前 Runtime `0.6.1` 上的运行 PASS。
- 本次只实际触发 Hatch 幼体/成年声音事件；其余三种的 WAV 已验证加载就绪，但若将来要求逐种交互听感验收，应另建一个最小、明确的声音观察任务。
- 这份兼容性验收不改变公开 `ICustomAnimalApi` 的 Frozen / registry-only 裁决，也不证明存在无需 DTMAPI 的官方独立新物种路线。
