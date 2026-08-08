# Hatch / Shell Crab Eat And Sleep Manual QA Review

时间：2026-06-30 19:36:26 +08:00

来源：用户第七档手测反馈与截图 `codex-clipboard-5a3c7f53-7a38-41b7-909d-2191654b7a05.png`，本地 `Player.log`，Hatch / Shell Crab 内容包，DTMAPI GameBridge 自定义动物桥接代码，Doloc Town build `23762374_public_C416D4` 反编译参考。

范围：本记录只做根因研究和后续诊断约束，不实现运行时代码，不同步本地 MODS 内容包，不启动游戏验证。

禁止事项：不修改鸡/羊/山羊原生 controller，不 patch `DolocAssetCache<T>`，不把普通 DTMAPI mod 放入 `BepInEx/plugins`，不把 ShellCrab 的未测实现直接复用到 Hatch，不把“视觉最终正确”当成“无切换/无闪回”通过。

审查记录：`docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`

## 逐条手测审查

### 问题 1：产物、长大、消耗

原始反馈：

- 产物正常、长大正常，消耗等大概率是正常的，游戏内无直观数值可见。

审查记录：

- 用户确认事实：第七档中 Hatch 的产物路线与长大流程已被手动观察为正常；消耗没有直观 UI 数值，只能按行为推断大概率正常。
- 截图/日志观察：当前 `Player.log` 里能看到 `hatch_meat` 与 `sack_hatch` 来自 `Local.DTMAPI_HatchAssets` 的 DebugConsole hover / give 记录，说明自定义物品表至少被游戏与 DTMAPI 调试 UI 识别。
- 代码/文档事实：Hatch 内容表 `animal_tbanimal.json` 保持 `schedule_id: chicken`，`custom-animals.json` 保持 `aiTemplate: chicken`，`produce_spawn_entry.spawn_lut = hatch_produce`。日志中 `CustomAnimals.AiTemplateBridge.hatch = verified` 映射到 `DolocTown.AnimalAI+Chicken_FreeTimeState`。
- Codex 推断：产物、成长和普通 AI 主路线当前不应作为首要 blocker。`hatch_meat` 自定义产物已经通过手测，比最初计划里的“必要时退回原版 meat”更进一步。
- 反证/未证实：消耗数值没有 UI 或日志计量证据；若未来要证明消耗，应加动物 hunger/energy/feed consume 调试记录，而不是从视觉状态推断。
- 归属：Hatch 内容包 + GameBridge AI template bridge。
- 需要更新：如果后续生成实现目标，可把 `hatch_meat` 从 pending product gate 改为 user-verified manual QA；但本记录不更新 smoke matrix。
- 验收点：重进第七档后仍能查到/产出 `hatch_meat`，成长阶段切换后仍使用 Hatch 外观，消耗诊断若新增则显示 feed/energy 变化与鸡模板一致。
- blocker 判定：只有产物查询失败、产出回落原版/空物品、成长后丢外观，才阻塞 Hatch PNG 动物路径；当前手测没有报告这些 blocker。

### 问题 2：进食时回复鸡，方向左右反，资源已更新

原始反馈：

- 进食的时候会回复原本的鸡，不排除是切换状态就会掉一次。
- 此外动画左右反了。
- 截图转写：上一轮处理记录写到，方向问题不是 DTMAPI 加载反，而是 Hatch PNG 默认朝向和原版动物翻转方向相反；`prepare_hatch_asset_pack.py` 已加水平镜像并重生成 Hatch PNG。碰撞/交互框改 `animal_tbanimal.json`，`sprite_size` 从 young `28x24` 到 `32x26`，adult `32x32` 到 `36x34`。截图还写到当时校验为 38 张 Hatch PNG，adult/young 各 19 张。

审查记录：

- 用户确认事实：吃饭时出现鸡视觉帧；方向反的问题已在资源生成侧处理过；碰撞框也做过调整。
- 日志观察：本地 `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player.log` 明确记录 `anim_animal_chicken_young_eat_0..3` 映射到 `anim_animal_hatch_young_eat_0..3` 成功，随后 `eat_4`、`eat_5`、`eat_6` 缺失并把 `CustomAnimals.PngSpriteBridge.hatch` 标为 degraded。典型行：`missing mapped sprite anim_animal_hatch_young_eat_4 for anim_animal_chicken_young_eat_4`，同类记录也出现在 `eat_5` 和 `eat_6`。
- 内容包观察：源包 `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\Sprites` 当前已经是新版，Hatch PNG 总数 44，young/adult 各有 7 张 eat 图，`hatch_frame_manifest.json` 也声明 eat `frame_count: 7`。但游戏本地实际读取路径 `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets\Content\Sprites` 仍是旧包，Hatch PNG 总数 38，young/adult eat 都只有 `0..3`。本地 MODS 的 `animal_tbanimal.json` 也仍是旧 `sprite_size`：young `28x24`，adult `32x32`，而源包已是 young `32x26`，adult `36x34`。
- 代码/文档事实：`CustomAnimalAnimatorBridgeService.TryResolvePngSpriteOverride` 只做模板帧名到 Hatch 帧名的直接映射；加载失败时调用 `MarkPngSpriteDegraded` 并返回 `NoOverride()`，因此该帧会保留鸡模板 sprite。`MapPngSpriteName` 只处理 prefix 替换、`child_ -> young_` 和 `jump_ready -> jump_0`，不读取 manifest 里的帧数或别名来兜底 eat 帧。
- Codex 推断：这次“吃饭回复鸡”的直接根因是本地游戏 MODS 包仍旧，缺少鸡模板 eat 动画实际访问的 `eat_4..6`。它不是整只 Hatch 注册失败，也不是方向镜像问题。源包已经修到 7 eat 帧后，重新同步/安装内容包并重启游戏应能消除已记录的 eat 段回落。
- 反证/未证实：日志只证明 young eat 缺帧；adult eat 在当前运行里未见同类行，但旧本地 MODS 也只有 adult eat `0..3`，所以成年吃饭时同样会有风险。若同步后仍掉鸡，则下一步要查是否还有其他状态名或隐藏帧，例如 `jump_ready` 以外的模板状态。
- 归属：Hatch 内容包安装/同步状态 + GameBridge PNG sprite override 缺失帧回落策略。
- 需要更新：若进入实现轮，应先在 runtime lock 下同步 `DTMAPI_HatchAssets` 到本地 MODS，再重新跑第七档；日志验收必须没有 `CustomAnimals.PngSpriteBridge.hatch = degraded`，且 `eat_4..6` 都映射成功。
- 验收点：释放/喂食 young 与 adult Hatch，吃完整段动画都不出现鸡帧；日志出现 `anim_animal_chicken_*_eat_4..6 -> anim_animal_hatch_*_eat_4..6` verified；本地 MODS 包 PNG 总数与源包 44 一致，`animal_tbanimal.json` 尺寸也一致。
- blocker 判定：如果同步后仍 missing mapped sprite，则是 PNG 命名/官方 sprite 加载索引问题；如果日志 verified 但视觉仍鸡，则要查 `SpriteOverrideHandler` 生命周期或 SpriteRenderer 覆盖顺序。

### 问题 3：Shell Crab 和 Hatch 半夜睡觉后，进入畜棚短暂时间又站起；重进又睡

原始反馈：

- 螃蟹和哈奇都有概率在半夜睡觉时候，玩家进入畜棚短暂时间后从睡觉变成起来。
- 重进又睡了。
- 原版动物没有遇到。

审查记录：

- 用户确认事实：问题发生在两个自定义动物上，原版动物未观察到；触发点是半夜进入畜棚后等待短暂时间；重新进入又可见睡眠状态。
- 日志观察：当前 `Player.log` 有 Hatch sleep sprite 映射成功记录：`anim_animal_chicken_young_sleep_0 -> anim_animal_hatch_young_sleep_0` 和 `anim_animal_chicken_adult_sleep_0 -> anim_animal_hatch_adult_sleep_0`。日志也验证 `shell_crab->goat`、`hatch->chicken` 的 AI template bridge 均已注册。但当前日志没有 `Animal.WakeUp`、`Animal.Sleep`、`AnimalRenderer.OnFell` 或 AI current-state 调用者记录，不能从现有日志定死是谁把动物叫醒。
- 代码/文档事实：原生 `Animal.OnRender()` 会按 `isSleep` 播放 `_GetCurrentAnimation()` 并显示睡眠特效。`Animal.Sleep()` 设置 `isSleep=true` 并强制播放 `sleep`；`Animal.WakeUp()` 设置 `isSleep=false` 并播放 `idle`。`AnimalAI.Normal_SleepState.GetNextState()` 只有在 `!ShouldSleepNow` 时才返回 `defaultAnyState`，`OnExit()` 才调用 `WakeUp()`；`ShouldSleepNow` 判断的是 `CurrentDayPeriodType == Night`。`AnimalRenderer.OnFell()` 在睡眠时会直接 `animal.WakeUp()`，面向玩家并切到等待任务。`Animal.CallToRoom()` 如果跨房间 call 且动物在睡，也会 `WakeUp()`。普通 `Animal.Fondle()` 对睡眠动物会直接 return。
- 代码/文档事实：房间进入路径 `TemplateRoom.OnEnterRoom()` 调 `animalSystem.SetCurrentRoom(this)`，随后每只动物 `RefreshRenderer()`；这会让已保存的 `isSleep` 先渲染为睡眠。房间/建筑 update 后 AI 或工具/交互路径仍可能改变当前内存状态。
- Codex 推断：现有证据能排除“sleep PNG 缺失导致站起”和“AI template 完全未注册”。更可能的方向有三类：一是自定义动物进入畜棚后 AI state/current task 生命周期有短暂错配，导致从 sleep state 退出或播放 idle；二是玩家进入/靠近/交互时触发了工具碰撞 `OnFell` 或 call-to-room 类路径；三是 renderer 重建后先按保存 `isSleep` 显示 sleep，随后内存中的 controller/task 状态把动画切回 idle，但离开重进又从保存/房间状态重新渲染 sleep。
- 反证/未证实：不能把该问题归咎于 Hatch PNG `pngSpriteOverride`，因为 Shell Crab 走 AssetBundle controller 也会出现。也不能直接归咎于旧的 Shell Crab sleep-exit bug，因为那次是“早上不醒”，而本次是“夜里短暂站起”。当前没有证据说明半夜被判成非 Night；反编译日周期表显示夜间覆盖 19:00 到次日 05:00 的小时段。当前也没有证据说明普通摸动物会叫醒，因为 `Fondle()` 有 sleep guard。
- 归属：GameBridge 自定义动物 AI/template 生命周期诊断，或原生工具/房间 call-to-room 唤醒路径；暂不归属 PNG/AssetBundle 外观资源。
- 需要更新：如果进入实现/诊断轮，先加短期诊断而不是直接改 AI：围绕 `Animal.Sleep`、`Animal.WakeUp`、`Animal.OnRender`、`AnimalRenderer.OnFell`、`Animal.CallToRoom`、AI current state/task 做 Hatch/ShellCrab 限定日志，记录 species、adult/young、hour/dayPeriod、room、`isSleep` before/after、current AI state、调用来源。再在第七档分别测试“只进入不交互/不持工具”和“交互或工具接触”。
- 验收点：半夜进入畜棚后至少等待一段时间，Hatch 和 Shell Crab 都保持 sleep 动画和睡眠特效；若被工具/召回主动唤醒，日志必须显示明确来源且可与原版一致。重进畜棚不应出现先站起再睡的视觉闪变。
- blocker 判定：没有 `WakeUp` 来源、AI state、时间段和交互/工具状态日志时，不应直接提交行为修复；否则很容易把原生可唤醒行为或房间生命周期误改坏。

## 结论

1. 吃饭掉鸡已有硬根因：本地游戏实际加载的 Hatch MODS 包仍是旧 38 PNG 包，缺 `eat_4..6`；源包已更新为 44 PNG 和 7 eat 帧。先同步本地 MODS 再复测。
2. 方向反和碰撞框调整目前看是资源包同步问题，不是 DTMAPI 运行时镜像逻辑问题；但本地游戏路径仍旧，复测前必须确认 runtime 包和源包一致。
3. 睡觉短暂站起还没有硬根因。已排除 sleep sprite 缺失、AI template 完全未注册、普通 Fondle 直接唤醒。下一步应做 Hatch/ShellCrab 限定的 `Sleep/WakeUp/OnRender/OnFell/CallToRoom/AIState` 诊断日志，再决定是否修 AI 生命周期或交互碰撞。

## 2026-06-30 后续执行

- 已在 runtime lock 下将 `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets` 同步到本地运行时 `MODS\DTMAPI_HatchAssets`。同步后运行时包为 44 张 Hatch PNG，young `sprite_size` 为 `32x26`，adult 为 `36x34`。
- 已加入 `CustomAnimals.SleepWakeDiagnostics` 诊断 hook，覆盖 `Animal.OnRender`、`Animal.Sleep`、`Animal.WakeUp`、`Animal.CallToRoom`、`AnimalRenderer.OnFell` 前后缀，并只记录注册的自定义动物物种。
- `tools/scripts/test.ps1 -Configuration Release` 与第七档 `GAME-SMOKE/20260630-195052` 均通过；烟测验证诊断 hook 已 patched，且没有 `missing mapped sprite` / `CustomAnimals.PngSpriteBridge.hatch = degraded`。该烟测没有复现半夜站起，也没有触发 Hatch 吃饭动画，手测复现仍是下一步。

## 2026-06-30 手测日志回读

用户反馈：

1. Hatch 左右方向已改好，确认是 PNG 方向问题。
2. Hatch 吃东西不再掉回鸡，确认是旧运行时包帧数量问题。
3. 本次运行第一次进入畜棚时遇到一只 Shell Crab 醒来；后面进入时的醒来是挥舞工具造成；之后也观察到可正常再次入睡。

日志分析：

- Hatch 吃饭路径已被日志验证：`Player.log` 中 `anim_animal_chicken_adult_eat_4/5/6` 均映射到 `anim_animal_hatch_adult_eat_4/5/6`，没有 `missing mapped sprite` 或 `CustomAnimals.PngSpriteBridge.hatch = degraded`。
- 19:10/19:15 夜间入睡正常：Hatch 与 Shell Crab 都记录了 `event=Animal.Sleep`，`sleep=true`，`aiState=Normal_SleepState`。
- 第一次进棚附近的异常不是显式 `WakeUp`：0:00 的 Shell Crab `Animal.OnRender` 记录为 `sleep=true`，但 `aiState=Goat_FreeTimeState`，没有同一窗口的 `Animal.WakeUp`、`AnimalRenderer.OnFell` 或 `Animal.CallToRoom`。这把“第一只醒来”收窄为睡眠 flag 与 AI state/render animation 的状态错配，而不是原生 WakeUp 调用。
- 后续工具叫醒是清晰的原生路径：0:20 记录 `AnimalRenderer.OnFell.Prefix ... tool=ItemTool:steel_sickle`，随后 Hatch 与 Shell Crab 都记录 `event=Animal.WakeUp sleep=false`，再进入 `AnimalRenderer.OnFell.Postfix`。
- 再次入睡也正常：0:40 Hatch 记录 `Animal.Sleep`，0:45 Shell Crab 记录 `Animal.Sleep`。6:00 的 `Animal.WakeUp render=false period=Daytime` 属于早晨正常醒来。

结论更新：

- Hatch PNG 路径的方向和吃饭掉鸡问题可以归为资源生成/同步问题，当前手测与日志均支持已修复。
- Shell Crab/Hatch 的夜间“站起”问题不应再优先怀疑 `OnFell` 或 `CallToRoom`，除非日志出现对应事件；本次第一次进棚异常更像 `isSleep=true` 但 `aiState` 仍为模板 free-time state 的生命周期错配。下一轮代码研究应围绕进入房间/renderer refresh 后，为什么睡眠动物的 AI state 可能暂留 `Goat_FreeTimeState` / `Chicken_FreeTimeState`。

## 2026-06-30 根因诊断收窄

用户反馈：

- 不要做窄修复，先查根因。
- 新日志中有一次 Hatch、一次两个 Shell Crab 的夜间进棚异常。

日志/代码事实：

- 新日志中，0 点后进入畜棚时，Hatch 与 Shell Crab 都出现过 `sleep=true`、`aiState=Chicken_FreeTimeState` / `Goat_FreeTimeState`、`rendererState=...animState=idle@0...sprite=grassslime_eat_0` 的组合。
- 同一窗口没有 `Animal.WakeUp`、`AnimalRenderer.OnFell`、`Animal.CallToRoom`，所以这仍不是一个已证明的唤醒调用。
- 原生 `Animal.OnRender()` 在 `isSleep=true` 时会调用 `Renderer.PlayAnimation("sleep")`；原生 `AnimalRenderer.OnRecycle()` 不清理当前 sprite 或 runtime animator controller，只清理移动/吃饭/跳跃 flag 与 sleep effects；原生 `Animal.__OnDayChanged()` 会 `RefreshAI()`，这解释了半夜时间跳转后 AI 可能回到模板 free-time state。
- 短暂构想过的强制 sleep renderer 稳定器已从源码撤回，未安装到运行时。当前实现只增加诊断，不写动物状态、不强制播放动画、不替换原版 controller。

根因假设更新：

- 优先假设是“过日/时间跳转刷新 AI + 进入房间复用 renderer + Animator 同帧采样/池化 sprite 残留”的生命周期错位。可见站起可能是首帧或短窗口仍显示 pooled `idle`/`grassslime_eat_0`，也可能是 `PlayAnimation("sleep")` 后仍未进入 sleep state。
- 需要下一轮日志证明是哪一种：如果 `AnimalRenderer.FixedUpdateAfterRender` 已经变成 `animState=sleep`，问题更像一帧池化/采样残留；如果 FixedUpdate 仍是 `idle`，再查 AI state transition owner 或 Animator controller/state name。

新增诊断：

- `AnimalRenderer.PlayAnimation` postfix：只记录自定义动物的 `animName=sleep`，用于观察 native `PlayAnimation("sleep")` 后的即时 renderer state。
- `AnimalRenderer.FixedUpdate` postfix：只对被 suspicious `Animal.OnRender` 标记过的 renderer 记录一次下一帧状态，然后清理 pending context。
- 预期下一轮对比同一 `animal=ref/index/dataIdx/cell` 的三条链：`Animal.OnRender`、`AnimalRenderer.PlayAnimation`、`AnimalRenderer.FixedUpdateAfterRender`。
- 第七档 smoke `GAME-SMOKE/20260630-205912` 已验证这两个新增 hook 安装成功：`animalRendererPlayAnimation=True`、`animalRendererFixedUpdate=True`，且 HookProbe、SaveLoaded、退出、无残留 `DolocTown.exe` 均通过。

## 2026-06-30 新手测日志回读（二）

用户反馈：

- 新手测已完成，主要看最后两次：一次肉眼观察到一只 Shell Crab + Hatch，另一次只有 Hatch。
- 怀疑 Y 键调试控制台跳时间更容易触发；用跳时间检查原版动物仍未观察到半夜醒来。
- 问题看起来不影响游戏体验，但视觉上奇怪。

日志分析：

- 最后一轮 21:12:31.514 从 2-2-15 18:10 跳到 2-2-16 00:00，随后 21:12:32.409-21:12:32.411 进入畜棚时，三只 Shell Crab 与 Hatch 都记录为 `sleep=true`，但 renderer 仍是 `animState=idle@0` 且 `sprite=grassslime_eat_0`；Shell Crab 的 AI 为 `Goat_FreeTimeState`，Hatch 为 `Chicken_FreeTimeState`。同一窗口没有 `Animal.WakeUp`、`AnimalRenderer.OnFell`、`Animal.CallToRoom`。
- 同一批动物在 21:12:32.435-21:12:32.437 的下一次 `AnimalRenderer.FixedUpdateAfterRender` 已全部变为 `animState=sleep@0.016...` / `sleep@0.023...`，并且仍保持 `sleep=true`。这证明至少该轮不是持续站立，也不是 gameplay wake。
- 21:12:40.348 重新进棚时，Shell Crab 3 的 `AnimalRenderer.PlayAnimation("sleep")` postfix 立即读到的 `afterPlayRendererState` 仍是 `idle@0` 与旧 sprite；但 21:12:40.360-21:12:40.361 下一次 `FixedUpdateAfterRender` 中三只 Shell Crab 与 Hatch 已全部是 `sleep@0`。这直接证明 native `PlayAnimation("sleep")` 后，当前帧立即采样仍可能读到旧 animator/sprite 状态。
- 21:12:45.114 先跳到 18:00，21:12:45.712-21:12:45.714 所有自定义动物正常 `Animal.Sleep`；21:12:45.837 再跳到 00:00 后，21:12:45.933-21:12:45.935 的 follow-up 中四只动物全部已是 `sleep@0`，`onRenderRendererState` 仍记录了进入渲染时的 `idle@0` 旧状态。21:12:48.885-21:12:48.886 再次进棚也相同：进入渲染时有旧 `idle` 快照，下一次 fixed update 已经 sleep。
- 早晨 6:00 的 `Animal.WakeUp` 仍是正常日周期；工具挥舞触发的醒来仍有明确 `AnimalRenderer.OnFell ... tool=ItemTool:steel_sickle -> Animal.WakeUp` 链路，和本次半夜视觉闪变不是同一类事件。
- 2026-06-30 追加校正：用户确认不是“只闪一帧”，而是肉眼看到特定个体真的持续醒来，甚至可能短距离移动。因此上述 `FixedUpdateAfterRender` 只能证明“首个采样点已经回到 sleep”，不能排除随后 free-time/task 又把 renderer 或动物移动任务拉起来。现有诊断只采第一帧并清理 pending，且 `PlayAnimation` 只记录 `sleep` 请求，确实会漏掉后续 `idle/move` 动画或位置变化。

根因结论：

- 当前硬证据只排除了显式 `WakeUp/OnFell/CallToRoom` 作为最后两次的直接来源；不能再把问题结论写成纯单帧视觉 artifact。
- 更合理的当前假设是“睡眠 flag 与 AI state/task 脱钩”：半夜进入畜棚时 `isSleep=true`，但 AI state 可停在 `Goat_FreeTimeState` / `Chicken_FreeTimeState`，如果后续 task 继续执行，就可能让某只动物持续播放 idle/move 或移动一小段，同时没有 `WakeUp` 事件。
- Y 键跳时间更容易触发，仍可能是因为它把过日/过时段、`Animal.__OnDayChanged()` 的 AI refresh、房间切换、renderer 回收复用和进入畜棚压在很短时间内；但待证点从“首帧残影”改为“AI/task 是否在 sleep=true 下继续运行”。
- “为什么不是每只动物都发生”：目前不能用“玩家只看到显眼的一两只”解释。更可靠的待证解释是不同个体当时的 `CurrentTaskType` 不同，例如 `LinearTaskEmpty`、`LinearTaskWaitInt` 或 `AnimalMove`，只有拿到继续采样日志才能判断哪些 task 会把睡眠动物拉起。
- “为什么不是每天稳定发生”：可能取决于夜间进入时的 AI refresh 时机、个体当前 task、房间/renderer 生命周期、是否刚跨日或刚跳时间。原版动物没复现说明这更可能是自定义物种模板映射/自定义 renderer 生命周期暴露的问题，不应简单归为原版本体现象。

后续建议：

- 新增持续诊断：可疑 `sleep=true` 自定义动物进入渲染后继续追踪最多 180 个 `FixedUpdate`，只在状态变化或第 1/10/30/60/120/180 帧采样；同时记录睡眠状态下收到的非 `sleep` 动画请求。
- 下一次复现要看同一 `animal=ref/index/dataIdx/cell` 是否在 `sleep=true` 下出现 `task=AnimalMove`、`rendererState.moving=true`、`animName=move/idle` 或 `cell/ws/rendererPos` 变化。只有这些证据出来后，才能决定是修 AI sleep-state 归位、task 停止，还是 renderer hygiene。

## 2026-06-30 根因复查（三）

用户反馈：

- 不要只顺着日志事件名查，前一轮“首帧残影/可见动物”思路可能有偏差。
- 用户明确观察到不是只有闪一下，而是真的只有特定个体持续变成醒来状态，甚至可能短距离移动。

复查结论：

- 前一轮“玩家只看到最明显的一两只/更像一帧残影”的解释应撤回。`Player-prev.log` 已有更强证据：21:12:48.886 的 Hatch 记录为 `sleep=true`、`period=Night`、`aiState=Normal_SleepState`，但 `task=DolocTown.AnimalMove`。同一窗口另外三只 Shell Crab 是 `task=none`。这符合用户看到“只有 Hatch 一只异常”的反馈。
- 这仍不是 `WakeUp`：同一复现窗口没有 `Animal.WakeUp`、`AnimalRenderer.OnFell`、`Animal.CallToRoom`。动物可以保持 `isSleep=true`，但 controller/current task 已经持有移动任务。
- 代码级根因链更像：`Animal.__OnDayChanged()` 会 `RefreshAI()` 创建新的 `AnimalController`；`DecisionMaker.Update()` 先调用 AI state machine，再在 `CurrentTask == null` 时立刻 `MakeDecision()`；原生 `RedSaw.AI.StateMachine.StateMachine.Update()` 在 `currentState == null` 的第一 tick 只设置默认 FreeTime state 并直接 return，不会同 tick 执行 `GetNextState()` 的夜间/睡眠检查；于是 `Chicken_FreeTimeState` / `Goat_FreeTimeState` 的 `MakeDecision_FreeTime()` 可在 `animal.isSleep == true` 时返回 `WanderEx()`/移动任务。后续 state 虽然可切回 `Normal_SleepState`，但 `DecisionMaker` 不会因为 state 已变更而自动中断已有 task。
- `Animal.Move()` 没有 `isSleep` 防护，执行 `AnimalMove` 时会 `Renderer.MoveTo(...)` 并 `Renderer.PlayAnimation("move")`。所以如果这条 task 被执行，就会是真移动/持续站起，而不是单纯 PNG 或 renderer 首帧旧 sprite。
- “为什么不是每只”：`WanderEx()` 本身随机，早期约一半返回 idle、一半尝试水平移动；路径可达性、当前 task 是否为空、房间切换时机也不同。因此同一夜间窗口里只有一两只自定义动物持有/执行 `AnimalMove` 是合理结果。
- “为什么不是每天稳定发生”：需要 day-change/时间跳、AI refresh、room entry/render refresh、当前 task 结束/为空、随机 wander、路径可达这些条件叠在一起；Y 键跳时间更容易把这些时机压缩到玩家进棚窗口，所以更容易触发。
- “原版为什么没看到”：当前不能证明原版绝不受影响。代码路径在 native 层也存在，但自定义动物通过模板 `schedule_id` + `aiTemplate` 被特别观察、日志只覆盖自定义物种，且第七档自定义动物密集复现。更准确归类是“原生 AI/task 初始化窗口被自定义模板动物暴露”，不是 PNG/AssetBundle 路径问题，也不能简单定性为已证明的原版本体 bug。

后续修复方向约束：

- 不应修 renderer 强制 sleep；那只能遮住 `AnimalMove` 已进入 current task 的事实。
- 更合理的修复点应在自定义动物的睡眠/task 边界：当注册自定义动物 `animal.isSleep == true` 且当前时间为 Night 时，阻止 FreeTime 决策产生 wander/move，或清理/替换睡眠状态下的移动 task。具体 hook 点需要单独评估，避免影响原版动物和正常早晨 `WakeUp`。

## 2026-06-30 稳定修复实现记录

实现结论：

- 已新增 `CustomAnimals.SleepTaskBoundary`，作为自定义动物模板 AI 的底层睡眠/task 边界，而不是 renderer/PNG 层补丁。
- 决策层：patch `AnimalAI/AnimalAIState.MakeDecision_FreeTime()`。仅当动物是已注册自定义物种、`isSleep=true`、当前时段为 `Night` 时，直接返回原生 `LinearTask.WaitFrames(5)`，阻止 FreeTime 生成 `WanderEx()`/移动任务。
- 任务层：patch `AnimalController.OnUpdate(float)` postfix。native AI update 后、current task 执行前，如果注册自定义动物已经睡着且夜间仍持有 `AnimalMove` / `AnimalJump` / `AnimalEnterRoom`，调用原生 `DecisionMaker.StopTask()`，让下一轮重新进入睡眠等待。
- 范围约束：原版动物不进入该边界；去睡觉路上不影响，因为 `isSleep=false`；早晨醒来不影响，因为不再是 `Night`；工具叫醒不影响，因为原生 `WakeUp()` 会清掉 `isSleep`。
- 同时收口 `AnimalRenderer.PlayAnimation` 诊断日志：同一动物/原因/动画名只记录一次，避免 normalizedTime 变化造成导出日志膨胀。

验证状态：

- `tools/scripts/test.ps1 -Configuration Release` 已通过，`DTMAPI.UnitTests: OK`。
- `git diff --check` 通过，仅 CRLF normalization warnings。
- 第七档 smoke `GAME-SMOKE/20260630-222709` 已通过：`CustomAnimals.SleepTaskBoundary=verified`，`animalAIMakeDecisionFreeTime=True`，`animalControllerOnUpdate=True`，HookProbe/SaveLoaded/退出/无残留 `DolocTown.exe` 均通过。
- 第七档 no-tool 午夜进棚手测仍需补证据；目标是不再出现持续的 `sleep=true period=Night task=DolocTown.AnimalMove` 或可见 Hatch/Shell Crab 半夜站起/短距离移动。

## 2026-06-30 手测通过与日志收口

用户反馈：

- 手测五夜无问题；跳时间、0:00 后进入畜棚，Hatch 和 Shell Crab 都保持睡眠。
- 要求复查日志，并收口额外诊断日志，避免玩家/测试者导出的日志被短时间 FixedUpdate 跟踪刷大。

日志复查：

- 最新 `Player.log` 中 `CustomAnimals.SleepTaskBoundary=verified`，并记录 `animalAIMakeDecisionFreeTime=True`、`animalControllerOnUpdate=True`。
- 未再看到自定义动物在夜间 `sleep=true` 时残留 `task=DolocTown.AnimalMove`、`AnimalJump` 或 `AnimalEnterRoom`。
- 0:00 附近有预期的 `CustomAnimals.SleepTaskBoundary event=FreeTimeDecisionGuard ... replacement=WaitFrames(5)`，分别覆盖 Shell Crab 与 Hatch。
- `AnimalRenderer.OnFell -> Animal.WakeUp` 日志对应玩家挥舞工具，6:00 `Animal.WakeUp` 对应原生早晨起床，二者不属于异常复现。
- 日志体积主要来自 `CustomAnimals.SleepWakeDiagnostics event=AnimalRenderer.FixedUpdateFollowUp`：renderer 已经处于 `animState=sleep`、AI 已经是 `Normal_SleepState`/`无状态`、task 已经是 `LinearTaskWaitInt`，但 `animState=sleep@normalizedTime` 每帧变化，旧签名逻辑把它当成新状态持续输出。

收口方向：

- 不修改睡眠行为修复本身；继续保留 `SleepTaskBoundary` 的 FreeTime guard 和 movement-task cleanup。
- 诊断层在 renderer 已稳定睡眠时立即清理 follow-up context，不再继续采样。
- follow-up 签名忽略 `animState=...@normalizedTime` 的时间部分，只保留动画名和布尔状态。
- 未稳定的异常 follow-up 上限从 180 fixed frames 收至 60 fixed frames；仍保留能证明移动任务、FreeTime 状态或非 sleep 动画的日志。
