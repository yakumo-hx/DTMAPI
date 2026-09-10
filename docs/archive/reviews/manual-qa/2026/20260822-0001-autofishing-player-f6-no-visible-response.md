# AutoFishing 玩家按 F6 无可见反应排障审查

## Review Header

- Time: `2026-08-22`（支持包按玩家 `+10:00` 时区采集于 `2026-08-23 00:15:11`）
- Status: `recorded`
- Source:
  - 用户提供的聊天截图；
  - 只读玩家支持包 `D:\下载\DTMAPI-player-support-20260823-001506-822-cb178e55满堂花`。
- Scope: 核对 Runtime 安装、官方 Mod 启用、AutoFishing 发现与加载、F6 输入、ProductNative session 和停用原因；不实施修复。
- User constraints: 排查“进入存档手持鱼竿按 F6 完全没有反应”。
- Related review/update/debug records:
  - [AutoFishing / Manager 玩家界面与循环停滞手测审查](20260731-0001-autofishing-manager-player-ui-and-loop-stall.md)
  - [AutoFishing L5 re-entry direct-neutral refresh race review](../../code/2026/20260804-0015-autofishing-l5-reentry-direct-neutral-refresh-race-review.md)
  - [AutoFishing behavior initial non-product cast race review](../../code/2026/20260804-0016-autofishing-behavior-initial-nonproduct-cast-race-review.md)
  - [AutoFishing legacy native and Runtime fallback closeout](../../../../updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md)
- Files/docs inspected:
  - 支持包的 `collection-summary.txt`、文件清单、Runtime install state/release manifest、`flags.json`、`mod_infos.json` 与文本日志；
  - `products/first-party/AutoFishing/src/ModEntry.cs`；
  - `products/first-party/AutoFishing/src/Native/FishingNativeMovementPolicy.cs`；
  - `products/first-party/AutoFishing/src/Native/FishingNativeAdapter.cs`；
  - 上列既有 AutoFishing Review/Update。
- Not inspected:
  - 未打开或解析支持包中的二进制存档正文；
  - 支持包没有收集玩家实际订阅的 AutoFishing DLL，因而未取得该 DLL 的精确 hash；
  - 未启动玩家游戏，也未修改其游戏目录、配置或存档。

## Issue Review

### Issue 1: 进入存档手持鱼竿按 F6 没有可见反应

Original feedback:

- “您好，这边尝试了安装，DTMAPI和DTMAPI自动钓鱼模组，也用bat安装了，状态是ok的。”
- “但是进入存档手持鱼竿按F6完全没有反应。”

Screenshot/log transcription:

- 截图只包含上述玩家反馈，没有可执行指令或额外状态画面。
- `collection-summary.txt` 显示采集完成：`FilesCopied=15`、`Errors=0`；没有发现 Unity crash 目录。
- Runtime state 显示 DTMAPI `0.6.1` / binary `0.6.1.0`，source commit `db5e518a6d7f`。
- 官方 `mod_infos.json` 显示：
  - `Workshop.3743016467`（DTMAPI）`enabled=true`；
  - `Workshop.3743799721`（AutoFishing）`enabled=true`。
- DTMAPI `latest.log` 显示 AutoFishing 从 Workshop `3743799721` 成功加载，22 个 ProductNative fishing patch 全部安装，状态机 ready，`Toggle=F6`，`Mod Entry completed`。
- 同一启动记录为 `gameCompatibility=Drift`：产品编译游戏 build `24456188`，玩家已安装 build `24788406`；Advanced compatibility activation 通过，但这不等于已证明新 build 上完整钓鱼行为。

Review record:

- User-confirmed facts:
  - 玩家认为 BAT 安装状态为 OK；
  - 玩家在进入存档并手持鱼竿后按 F6，主观上没有看到反应。
- Screenshot/log observations:
  - 第一次载入后，`00:07:06.976` 收到 F6，`00:07:06.986` 自动钓鱼已启用；`00:07:08.235` 又收到 F6，并于 `00:07:08.238` 以 `hotkey F6` 正常关闭。开启窗口约 `1.25s`。
  - `00:07:08.792` 再次收到 F6 并启用，`00:07:09.011` 再按 F6，约 `0.22s` 后关闭。
  - 第二次载入后，`00:10:40.443` 启用，`00:10:41.181` 以 `manual-move inputMultiplier=-1` 自动关闭，开启窗口约 `0.74s`。
  - `00:10:46.164` 再次启用，`00:10:48.526` 又以 `manual-move inputMultiplier=-1` 自动关闭，开启窗口约 `2.36s`。同期 `Player.log` 记录从初始农场经场景切换进入“郊区-码头小径”，随后进入“郊区-农场近郊”。
  - `00:10:57.194` 再次启用，`00:10:58.490` 被下一次 F6 关闭；之后在约一秒内连续多次按 F6，使状态在启用和关闭之间快速切换。
  - 日志没有 AutoFishing exception、输入注册失败、Hook 安装失败或 session acquisition 失败；也没有形成一次“静止且持续启用”的受控测试窗口。
- Code/doc facts inspected:
  - F6 是 Gameplay scope 的 owner-bound toggle；第二次按下会关闭自动钓鱼。
  - 当前移动策略在启用并完成 neutral arming 后，一旦原生 `inputMultiplier` 非零就关闭 automation；`-1` 是完整的非中性水平移动值，不是轻微接近零的浮点噪声。
  - 当前代码只把启用、关闭及原因写入日志，没有给玩家显示 HUD/toast。
  - 没有可用水域、未选中鱼竿或抛竿失败时，抛竿路径按 `1s`/`2.5s` 延迟重试；这些正常拒绝不会给玩家显示提示。
- Codex inference:
  - “F6 完全没有反应”在输入和状态机层面不成立：每次 F6 都被 Runtime 收到，并成功切换 automation。
  - 玩家看到的“无反应”主要由三个行为叠加形成：产品没有屏幕反馈；玩家在很短时间内再次按 F6 把功能关掉；另两次启用后发生水平移动，产品按设计自动停用。
  - `inputMultiplier=-1` 与同期实际转场相互印证，最可能是玩家仍在移动或过图；它不像小幅手柄漂移。支持包不能完全排除控制器或原生状态残留，但当前没有证据把问题归因于漂移。
  - 当前证据尚不能确认 build `24788406` 上实际抛竿循环是否兼容，因为所有明确启用窗口都过短或被移动/F6 终止，没有一次满足“站定、面向可钓水域、只按一次 F6、持续等待”的验证条件。
- Ownership:
  - 安装、官方启用和 DTMAPI 输入链本次均正常。
  - 无可见状态、移动停用说明、无水域/未选鱼竿提示属于 AutoFishing ProductNative 的玩家体验与 Workshop 文案边界。
  - build drift 下的真实抛竿兼容性仍属于 AutoFishing 产品验收边界，不能由安装器状态代替。
- Root-cause hypotheses:
  - 已证实：多次 F6 产生正常开关切换；两次明确因 `manual-move inputMultiplier=-1` 停用。
  - 高可信推断：缺少玩家可见反馈，使正常切换和自动停用被感知成“完全无反应”。
  - 待受控复测：玩家 build `24788406` 上，在稳定启用条件下是否能完成首次自动抛竿。
- Rejected/unproven hypotheses:
  - 安装器没有安装成功：被 Runtime `0.6.1` 实际启动与完整加载证据否定。
  - 官方 Mod 界面没有启用：被 `mod_infos.json` 的两个 `enabled=true` 否定。
  - F6 未收到、按键冲突或 owner-bound input 失效：被逐次 F6 pressed、session acquired 和 automation enabled 记录否定。
  - AutoFishing Hook 未安装：被 `count=22` 与 Entry 完成记录否定。
  - 杀毒软件拦截导致本次功能不工作：支持包没有对应拦截、缺文件或载入失败证据；本次运行链已成功进入产品代码。
  - 已证明新游戏 build 不兼容：只有 Drift 标记，没有一次合格的稳定启用测试，暂不能下此结论。
- Required downstream updates:
  - 本轮不实施代码或 Workshop 文案变更。
  - 若后续授权修复，优先增加玩家可见的“已开启/已关闭/因移动停用”提示，并对“未选中鱼竿、未面向可钓水域”给出节流提示；Workshop 描述应明确“按一次 F6、保持静止、面向可钓水域，移动会自动关闭”。
- Acceptance checks:
  1. 进入存档后走到明确可钓水域，选中鱼竿并面向水面。
  2. 松开所有移动键；如连接了手柄，先确保摇杆回中，排障时可临时拔除手柄。
  3. 只按一次 F6，之后至少 `10s` 不移动、不切场景，也不再次按 F6。
  4. 期望日志只出现一次 `automation enabled reason=hotkey F6`，期间没有 `manual-move` 或第二次 `hotkey F6` 停用，并观察到首次抛竿/钓鱼阶段前进。
  5. 若仍不抛竿，立即返回标题后重新收集支持包；下一轮需同时取得精确玩家 AutoFishing DLL hash/config，并增加或启用有节流的 cast rejection/phase 诊断。
- Blocker conditions:
  - 当前支持包没有玩家 AutoFishing DLL bytes/hash；不能证明玩家订阅字节与本地候选完全相同。
  - 当前日志没有稳定启用窗口；不能用本轮证据验收 build `24788406` 的完整自动钓鱼循环。

## Cross-Issue Summary

- Confirmed user facts: 玩家安装后进入存档手持鱼竿测试 F6，但没有得到可见反馈。
- Screenshot/log facts: DTMAPI 与 AutoFishing 已启用并加载；22 个 Hook 已安装；F6 每次都被收到；状态随后被第二次 F6 或移动关闭。
- Code-path findings: AutoFishing 当前没有玩家可见开关/停用提示，移动会关闭 automation，无水域/未选鱼竿等抛竿拒绝只重试而不提示。
- Risks: 产品编译 build 与玩家安装 build 存在 Drift，且本轮没有合格的稳定测试窗口；不能把“加载成功”扩大解释为“新 build 完整行为已验证”。
- Suggested implementation scope: 一个有界的 AutoFishing 玩家反馈/文案改进；若受控复测失败，再单独开展 build `24788406` ProductNative 兼容性审查。
- Items that should not be carried forward: 不把本次定性为安装器、官方启用、F6 注册、Hook 安装或杀毒软件故障；不把未验证的 build drift 直接定性为兼容性缺陷。

## Implementation Record Decision

- Create/update an implementation update record: no；本轮是只读诊断，没有实施变更。
- Additional debug/API/hook/smoke records required: no；当前证据没有发现新的 Runtime/API/Hook 故障，也没有执行游戏 smoke。若受控复测确认 build drift 下的产品行为故障，再创建对应 ProductNative root-cause Review/Update。
- Suggested task titles:
  - `AutoFishing 玩家可见启停与环境提示`
  - `AutoFishing build 24788406 稳定启用兼容性复测`
- Completion standard: 先完成上述受控玩家复测；任何后续实现必须证明 F6 单次启用可见、移动停用原因可见、无水域/未选鱼竿可诊断，并在当前目标游戏 build 上完成真实首次抛竿与循环证据。

## 2026-08-22 玩家复测结果

### 后续原始反馈

- “成功了，猜测之前是移动中按的或者连按的。此外在当前加载的游戏是可以正常运行的。”

### 追加审查记录

- 用户确认事实：
  - 按受控方式重新测试后，AutoFishing 已成功运行；
  - 在玩家当前加载的游戏环境中，该功能可以正常运行。
- 与既有日志的对应关系：
  - 先前日志明确记录了两次 `manual-move inputMultiplier=-1` 自动停用；
  - 先前日志也明确记录了多组间隔极短的 F6 启用/关闭切换；
  - 因此用户关于“移动中按下或连续按键”的猜测同时具有现场日志支持。
- Codex 结论：
  - 本次“F6 完全没有反应”按玩家复测关闭为操作条件与不可见状态共同造成的误判，不是安装器、官方启用、输入注册、Hook 安装或当前游戏 build 的已复现功能故障；
  - 不能仅凭旧日志区分最初主观观察究竟由移动停用还是连续按键单独造成，因为两种行为都真实发生过；
  - build `24788406` 上的当前玩家可见 AutoFishing 路径记为 `user verified`。该结论覆盖本次玩家正常运行确认，不扩大为全部配置组合、长时间稳定性或完整发布矩阵验收。
- 验收门结果：
  - “站定、只按一次 F6 后正常运行”的玩家可见验收已通过；
  - 先前“缺少稳定启用窗口”的功能判断 blocker 已解除；
  - 支持包未包含玩家 AutoFishing DLL hash 仍是精确包字节归因缺口，但不再阻止本次玩家问题关闭。
- 后续边界：
  - 本次无需创建实现 Update、Debug issue、Hook 或 smoke 记录；
  - 玩家可见启停/移动停用提示与 Workshop 操作说明仍可作为独立 UX 改进讨论，但不属于本次功能恢复的必需修复。
