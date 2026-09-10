# DTMAPI Debug 系统原始规划对话（冻结）

- Lifecycle Status: `frozen`
- Document Role: 2026-06-13 原始 Debug 体系需求与当时规划回答的历史审计材料；不是当前 Debug、实施或验证权威
- Baseline captured: `2026-06-13`
- Current handoff: [DTMAPI Debug 原始规划交接页](../Debug.md)
- Retirement review: [20260831-0005](../../reviews/code/2026/20260831-0005-new-task-context-and-skill-routing-audit.md)
- Owning Update: [20260831-0004](../../updates/2026/20260831-0004-new-task-context-routing-and-agent-rules-slimming.md)
- Editing rule: 原始正文停止接收当前实施进度；任何重新开启的工作从当前 Debug owner 和新的有界 Review/Update 开始

## 原始正文

这个作为长期vibe coding 的debug文件。
主要是开发多洛可小镇功能性mod的SMAPI。
想使用一套debug记录。因为codex在升级代码中出现了以及修复过的 退出游戏 但是 steam卡在“等待游戏退出”。
过去测试出问题codex也是从头查一遍，而且要反复修改三四回。
我的设想是一套可视可编辑的debug文档。
我想知道官方怎么处理、成熟的程序员如何debug、成熟的vibe coding开发怎么处理这些内容、或者是vibe coding 的游戏开发者怎么处理这个问题。

API/GameBridge 方向的重复调查还需要先查固定 native-owner 领域资料库：`../reviews/api/native-owner-domains/INDEX.md`。它记录未来功能 API 的原生责任函数、状态 holder、官方内容/美化支持边界和 blocked/gap 状态，避免 Codex 每次从 UI 成功或 debug console 成功重新猜 API 稳定性。







([Past chat](https://chatgpt.com/c/6a0c8584-aaf4-8321-ad2c-1212d14bad7b))([Past chat](https://chatgpt.com/c/6a0a0336-6e9c-8320-b73b-730d6d98ce07))([Past chat](https://chatgpt.com/c/6a06d42f-1c34-8320-97fe-eb6442f48b31))([Past chat](https://chatgpt.com/c/6a0b3584-5a48-8322-84bd-34f2a7859819))([Past chat](https://chatgpt.com/c/6a13f393-5738-8322-8c70-3597014f90e1))([Past chat](https://chatgpt.com/c/6a1323cf-3f10-8323-8c03-6d01194ab39f))([Past chat](https://chatgpt.com/c/69f9e3fd-321c-839b-bb91-2531edc040c6))([Past chat](https://chatgpt.com/c/6a13f9b4-2988-8320-b467-f5c89b446c5e))([Past chat](https://chatgpt.com/c/69f962c8-af60-8324-acf4-0b6223726dd6))([Past chat](https://chatgpt.com/c/6a10938e-5e94-8320-a5fa-be9d961c6c5a))

你的设想是对的，而且这类项目**必须有长期 debug 文档**。但它不应该只是“给人看的日志”，而应该变成一套：

**可视化 issue ledger + 证据归档 + Codex 强制读取规则 + 回归验收矩阵 + 失败经验库。**

你现在这份 `DEBUG-DolocTown-SMAPI.md` 已经有成熟雏形：它要求“一个反复症状一个 issue section”、每次用户测试按日期追加、记录 runtime 版本、启用 mod、是否完整重启游戏/Steam、status 脚本、BepInEx 日志、结果状态，并且保留错误方向而不是删除。这个方向非常像成熟工程里的 bug ledger / incident log，而不是普通聊天记录。

## 1. 官方/成熟项目通常怎么处理

成熟项目不会靠“记忆”处理 bug，而是靠**结构化 issue + 复现步骤 + 环境 + 日志 + 验收条件**。GitHub 官方 issue template 示例里就有 Current Behavior、Expected Behavior、Steps To Reproduce、Environment、Anything else 等字段；issue form 也会把用户填写内容转成 Markdown issue 正文，方便后续检索、引用和协作。([GitHub Docs](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/syntax-for-issue-forms))

游戏/Mod 生态尤其依赖日志。Unity 官方文档明确把 log files 用作定位问题发生位置的诊断材料；BepInEx 文档也强调插件日志很重要，并会写入 console、Unity `output_log.txt` 和 `BepInEx/LogOutput.log`。([Unity Documentation](https://docs.unity3d.com/6000.4/Documentation/Manual/log-files.html)) ([BepinEx Docs](https://docs.bepinex.dev/v5.4.11/articles/dev_guide/plugin_tutorial/4_logging.html)) 星露谷 SMAPI 生态更典型：它有 SMAPI log parser，用户遇到问题后会被引导去找 `SMAPI-crash.txt` 或 `SMAPI-latest.txt`，上传/分享日志链接给协助者。([SMAPI](https://smapi.io/log))

所以如果你做 DolocTown SMAPI，真正成熟的形态不是“Codex 每次帮我看日志”，而是你也做出类似：

```
DLK status.ps1` → `collect-debug.ps1` → `LogOutput.log` → `debug issue` → `回归矩阵
```

这条链路。

## 2. 成熟程序员怎么 debug

成熟程序员的核心不是“会猜”，而是**减少变量**：

先复现，再记录环境；先判断进程是否还活着，再判断是 runtime 问题还是 Steam 状态问题；先确认 full restart，再判断 mod 禁用是否有效；一次只改一个方向；修完以后必须把复现步骤变成回归验收项。

你这个 “退出游戏但 Steam 卡在等待游戏退出” 问题已经写出了正确的分叉：如果没有 `DolocTown / Unity / BepInEx` 进程，倾向 Steam 客户端 stale state 或 app state mismatch；如果进程还活着，才继续查非后台线程、Unity/BepInEx 对象、mod loop；并且文档已经提醒不要从任意 `OnDestroy` 调 full shutdown，因为 Unity 场景切换也会 destroy runtime component，可能破坏 mod loading。

这就是成熟 debug 的关键：**先分类，再修复**。不要让 Codex 看到“Steam 卡住”就直接去乱改退出逻辑。

## 3. 成熟 vibe coding / Codex 开发怎么处理

OpenAI Codex 官方最佳实践里，其实已经给了这套方法：把 Codex 当成“可配置、可逐步改进的队友”，而不是一次性助手；提示词最好包含 Goal、Context、Constraints、Done when；并且用 `AGENTS.md` 存放可复用规则、项目结构、运行命令、测试命令、工程约束和验收方式。([OpenAI开发者](https://developers.openai.com/codex/learn/best-practices))

对你的项目来说，`DEBUG-DolocTown-SMAPI-CN.md` 不能只是“你知道有这个文件”。它必须被写进 `AGENTS.md`，变成 Codex 每次处理 runtime / mod / Steam / stutter / config menu 问题前必须读取的上下文。

OpenAI 还专门提到长任务的 durable project memory：用 spec、plan、constraints、status 等 Markdown 文件让 Codex 可以反复回看，避免漂移；`Plan.md` 要有里程碑、验收标准、验证命令、失败就修的规则、决策记录；`Documentation.md` 用来记录当前状态、决策、运行方法、已知问题。([OpenAI开发者](https://developers.openai.com/blog/run-long-horizon-tasks-with-codex)) 这和你的需求几乎完全重合。

## 4. 你的 debug 文档应该升级成什么结构

现在不建议只保留一个巨大 `DEBUG-DolocTown-SMAPI-CN.md`。一开始可以，但随着 issue 变多，Codex 会读不全、漏重点、把旧结论当新结论。

我建议这样拆：

```text
docs/
  debug/
    INDEX.md
    DEBUG-DolocTown-SMAPI-CN.md          # 总账，可继续保留
    issues/
      ISSUE-001-steam-stopping.md
      ISSUE-002-stutter-after-save.md
      ISSUE-003-fish-roe-display.md
      ISSUE-004-config-menu-ux.md
    protocols/
      steam-exit-protocol.md
      stutter-perf-protocol.md
      mod-disable-restart-protocol.md
    regressions/
      smoke-matrix.md
      release-checklist.md
    evidence/
      ISSUE-001/
        2026-05-25-runtime-0.8.33/
          status.txt
          process-check.txt
          game-path-process-check.txt
          logoutput-tail.txt
          appmanifest.txt
          codex-summary.md
    lessons.md
```

其中 `DEBUG-DolocTown-SMAPI-CN.md` 作为**总索引/总账**，每个复杂问题拆到 `issues/ISSUE-XXX.md`。证据文件不要都塞进 Markdown；Markdown 只写摘要和判断，原始日志放 `evidence/`。这样 Codex 下次不会只看到“我记得修过”，而是能看到“当时 runtime 是 0.8.32，进程检查为空，StateFlags 是 4，所以那次不是确认的残留进程问题”。

## 5. 每个 issue 文件建议固定模板

可以把每个 issue 写成这样：

```md
# ISSUE-001: 退出游戏后 Steam 仍显示“等待游戏退出”

## 当前状态
- Status: open / pending confirmation / mitigated / solved / regressed
- Last verified: 2026-05-25
- Last runtime tested: public 0.2.11 / internal 0.8.33
- Severity: high
- Regression risk: high

## 症状
一句话描述用户看到什么。

## 影响
为什么重要：影响退出、Steam 状态、玩家体验、开发测试循环。

## 最小复现步骤
1. 重启 Steam。
2. 启动 Doloc Town。
3. 使用指定 runtime / 指定 mod 组合。
4. 进入存档。
5. 正常退出。
6. 等待 30 秒。
7. 观察 Steam 状态。

## 必须采集的证据
- status.ps1 输出
- 游戏/Unity/BepInEx 进程检查
- 游戏路径进程检查
- BepInEx/LogOutput.log tail 120
- Steam appmanifest StateFlags/buildid/UserConfig
- enabled mods
- 是否完整重启游戏
- 是否重启 Steam

## 判定树
- 如果仍有游戏进程：查非后台线程、Unity 对象、BepInEx/mod loop。
- 如果无游戏进程：优先查 Steam stale state / appmanifest / Steam client 状态，不要先改 Runtime exit code。

## 已知错误方向
- 不要从任意 OnDestroy 调 full shutdown。
- 不要把“禁用 mod 但未重启”当成有效排除。
- 不要在没有进程证据时假设 runtime 挂住了进程。

## 尝试记录
### 2026-05-25 / Runtime 0.8.33
- Change:
- Evidence:
- Result:
- Next test:

## 验收标准
只有满足以下条件才能标 solved：
- 用户确认 clean game restart 后不复现；
- Steam 重启后测试通过；
- 进程检查为空；
- 日志出现正常退出或可解释的退出路径；
- release-checklist 已加入该项。
```

你现在文档里“不要标 solved，除非 clean restart 后用户确认”这一条非常关键，应该继续保留。

## 6. AGENTS.md 里应该强制写的规则

这一步最重要。否则 Codex 还是可能从头查。

```md
# DolocTown SMAPI Debug Rules

Before modifying Runtime, shutdown, input, UI, config menu, event dispatch,
BepInEx integration, Harmony patches, or mod loading behavior:

1. Read `docs/debug/INDEX.md`.
2. If the task touches a known recurring issue, read the matching file under `docs/debug/issues/`.
3. Summarize the current known facts before changing code.
4. Do not re-investigate already disproved hypotheses unless new evidence contradicts them.
5. Do not mark an issue solved unless the issue's acceptance criteria are met.
6. If manual game testing is required, write an exact user test script and mark status as `pending user confirmation`.
7. After every fix attempt, append a dated attempt to the issue file:
   - runtime public/internal version
   - changed files
   - enabled mods
   - restart state
   - evidence files
   - result
   - next action
8. For Steam-stopping issues, never change shutdown code before classifying:
   - game process still alive
   - no game process but Steam stale state
9. For stutter/performance issues, remember that disabling a mod without full restart is not reliable because DLLs/Harmony patches may remain loaded.
```

Codex 官方建议 `AGENTS.md` 放 repo layout、运行方式、build/test/lint 命令、工程约束、PR 预期、done 的定义；当 Codex 重复犯同一个错误时，应做 retrospective 并更新 `AGENTS.md`。([OpenAI开发者](https://developers.openai.com/codex/learn/best-practices)) 你这个项目里，“Codex 又从头查 Steam 卡退出”就是应该被写进 `AGENTS.md` 的重复错误。

## 7. 你真正需要的是“回归矩阵”，不是只写日志

比如 `docs/debug/regressions/smoke-matrix.md`：

```md
# DolocTown SMAPI Runtime Smoke Matrix

| Case | Runtime | Mods | Steam restarted | Game restarted | Steps | Expected | Status |
|---|---:|---|---|---|---|---|---|
| EXIT-001 | 0.8.33 | none | yes | yes | launch -> enter save -> exit | Steam stops within 30s | pending |
| EXIT-002 | 0.8.33 | AutoFishing only | yes | yes | launch -> fish -> exit | Steam stops within 30s | pending |
| PERF-001 | 0.8.33 | none | n/a | yes | enter save | no stutter | pending |
| PERF-002 | 0.8.33 | ActionSpeed only | n/a | yes | enter save/open inventory | no per-frame exception stutter | pending |
| UI-001 | 0.8.33 | config-capable mod | n/a | yes | title screen/settings/save | gear only on title homepage | pending |
```

你文档里 Issue 002 已经有类似测试计划：无功能 mod、仅 OneActionComplete、仅 ActionSpeed、仅 AutoFishing，然后分别记录进入存档、打开背包/箱子、悬停物品、自动化、退出时是否卡顿。这个应该从 issue 文本升级成固定 smoke matrix。

## 8. 对 Codex 的工作流应该变成固定闭环

成熟的 AI 编程不是“让 AI 一直写代码”，而是：

**Review → Repair → Validate → Record → Regression。**

OpenAI 的 Codex repair loop cookbook 也把流程描述成：先 review 当前 artifact，结构化发现问题；再 repair 做聚焦修改；最后 validate 跑检查，并把剩余问题反馈到下一轮。([OpenAI开发者](https://developers.openai.com/cookbook/examples/codex/build_iterative_repair_loops_with_codex)) 另一个 agent improvement loop 也强调用 traces、反馈、evals 和 handoff 文件，把一次运行中学到的东西转成可复用改进，而不是散落在聊天里。([OpenAI开发者](https://developers.openai.com/cookbook/examples/agents_sdk/agent_improvement_loop))

你可以给 Codex 固定用这段提示词：

```text
你现在处理 DolocTown SMAPI 的已知回归问题。

开始前必须读取：
- AGENTS.md
- docs/debug/INDEX.md
- docs/debug/issues/ISSUE-001-steam-stopping.md
- docs/debug/regressions/smoke-matrix.md

任务：
1. 先总结 ISSUE-001 当前已知事实、已排除方向、危险修法。
2. 不要从头重新猜测原因。
3. 只提出最小代码修改方案。
4. 修改后运行可自动执行的 build/test/status 检查。
5. 如果需要我手动进游戏测试，输出精确测试步骤。
6. 最后追加一次 dated attempt 到 ISSUE-001，不允许标 solved，除非验收标准全部满足。
```

这比“帮我修 Steam 退出卡住”强很多。因为它明确了上下文、约束、完成标准和记录义务。

## 9. “可视可编辑”怎么做最实际

最稳的方案不是 Notion，也不是单独文档软件，而是：

**VS Code / Cursor / Codex + Markdown + Git。**

原因是 Codex 最可靠读取的是 repo 里的文件。Notion 可以作为展示层，但不要作为唯一真相来源。你的 canonical source 应该在仓库里：

```text
docs/debug/
AGENTS.md
tools/collect-debug.ps1
```

可视化可以这样补：

~~~md
```mermaid
flowchart TD
  A[Steam shows Stopping] --> B{Game/Unity/BepInEx process alive?}
  B -->|Yes| C[Check foreground threads / Unity object / mod loop]
  B -->|No| D[Check Steam stale state / appmanifest / Steam logs]
  C --> E[Patch runtime or mod owner]
  D --> F[Do not change runtime shutdown first]
  E --> G[Clean restart verification]
  F --> G
GitHub、Obsidian、VS Code Markdown Preview 都能看 Mermaid。GitHub Issues/Projects 可以作为看板，但对你这种单人+Codex 的项目，先用 repo Markdown 就够了。

## 10. 我建议你下一步直接做 4 件事

第一，把当前 `DEBUG-DolocTown-SMAPI-CN.md` 放进仓库的 `docs/debug/`，不要放在聊天记录或临时下载目录。

第二，新建 `docs/debug/issues/ISSUE-001-steam-stopping.md`，把 Steam 卡退出问题从总账拆出去，保留当前“无进程 vs 有进程”的判定树。

第三，在 `AGENTS.md` 加一条硬规则：凡是涉及 shutdown、runtime lifecycle、BepInEx、Unity object、input polling、config menu、event dispatch 的修改，必须先读对应 debug issue。

第四，写一个 `tools/collect-debug.ps1`，让证据采集一键化。你现有文档已经列了 status、进程检查、LogOutput tail、appmanifest 检查命令；现在只是把它们变成脚本输出到 `docs/debug/evidence/ISSUE-XXX/yyyy-mm-dd-runtime-x/`。:contentReference[oaicite:13]{index=13}

你现在真正要避免的不是“bug 复发”，而是**bug 复发时又失忆**。  
长期 vibe coding 的核心资产不是提示词，而是这些可以被 Codex 重新读取、被你审计、被 Git 记录、能转成验收矩阵的工程记忆。
::contentReference[oaicite:14]{index=14}
~~~
