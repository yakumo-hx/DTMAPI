# Codex Feedback To Goal Workflow

This workflow is for turning the user's manual test feedback into durable Codex constraints. It separates the feedback-organizing Codex from the DTMAPI-upgrading Codex.

## When To Use

Use this file before answering or editing when the user asks to:

- 审查、排查、代码级审查、解释为什么某个 bug 修不好。
- 整理提示词、约束、下一轮 `/goal`。
- 把手动测试问题转成 Codex 可执行任务。
- 只讨论下一轮目标、验收指标、blocker、debug 记录。
- 区分“整理反馈的 Codex”和“升级 DTMAPI 的 Codex”。
- 把 API 审查结论转为 API 重做目标时，先切到 `docs/workflows/codex-api-rebuild.md`。

Do not use this workflow to implement runtime/API/gameplay changes. If the user asks to implement, follow `AGENTS.md`, the exact `docs/goals/YYYY/...md` file named by the prompt, debug docs, and update-record rules instead.

## Role Split

### Feedback Organizer Codex

The feedback organizer only:

- reads the user's feedback, screenshots, logs, and local project docs;
- separates confirmed user facts, screenshot observations, and Codex inferences;
- performs code-path/root-cause review before asking an implementation Codex to fix repeated, lifecycle, hook, UI, input, save/load, vehicle, machine, or official-content issues;
- creates or updates `docs/reviews` records when the review needs to survive context compaction or feed a future implementation goal;
- creates or updates a dedicated `docs/goals/YYYY/...md` task ledger when requested;
- stores the exact short `/goal` prompt beside it as `docs/goals/YYYY/....goal.txt`;
- outputs a short `/goal` prompt for the implementation Codex;
- records durable constraints that prevent repeated already-solved issues.

The feedback organizer must not:

- start a `/goal` on behalf of the user;
- implement code, hooks, UI, scripts, packages, or game runtime changes;
- run game smoke as proof of a new implementation;
- mark implementation work complete;
- rewrite third-party Workshop or official content mod files.

### DTMAPI Upgrade Codex

The upgrade Codex receives the short `/goal`, reads the exact goal file named in that prompt, implements changes, validates in game, updates evidence, and follows `AGENTS.md`.

## Required Inputs

Before producing a new prompt or task breakdown, read the relevant current truth:

- `AGENTS.md`
- `PROJECT.md`
- `docs/goals/README.md`
- `docs/reviews/README.md` when doing manual-QA review or root-cause review
- the latest relevant `docs/reviews/manual-qa/YYYY/...` record if one exists
- the exact `docs/goals/YYYY/...md` file if this turn is updating or producing an implementation handoff
- `docs/updates/INDEX.md`
- the latest relevant `docs/updates/YYYY/...` record
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md` for hook/API work
- `docs/api/public-api-matrix.md` for API work
- `docs/workflows/codex-api-rebuild.md` for API rebuild, native-owner follow-up, or GameBridge boundary redesign
- relevant official docs / research notes / decompiled references named by the user

If the user says “只讨论”, “不用实现”, “整理为提示词”, or “给 codex 目标”, treat the turn as feedback organization unless they explicitly ask for file edits.

## Review-First Gate

Manual QA should pass through these layers in order:

1. **Record**: preserve the user's numbered issues and translate screenshots/logs into text.
2. **Review**: inspect the relevant docs, update records, debug records, and code paths. Attach analysis immediately under each issue.
3. **Convert**: only after review, create/update one dedicated goal file and produce a short `/goal` if the user asks for an implementation handoff.
4. **Implement**: a separate DTMAPI Upgrade Codex executes the goal, validates in game, and updates evidence.

Do not skip the Review layer for:

- regressions after a claimed fix;
- issues that survived smoke evidence;
- UI flicker, stale text, or layout changing across lifecycle events;
- input/hotkey unreliability;
- save/load, reload, map transition, or process lifecycle problems;
- GameBridge/Harmony/reflection hooks;
- official-local or Workshop content indexing/loading;
- vehicles, machines, placed objects, or item/mail systems;
- broad symptoms such as "random other objects changed".
- API/native-owner gaps, especially when previous work succeeded only through UI, registry, debug console, or smoke-helper evidence.

For these cases, identify at least the likely render/input/save/hook paths and the evidence gap before generating a fix goal. A goal that says only "fix it" is too weak.

For API rebuild cases, the review must additionally identify the native responsibility function or state holder, or explicitly mark that discovery as the first required task. Use `docs/workflows/codex-api-rebuild.md` before writing the goal.

## Review File Policy

Not every discussion creates a goal file. Use the smallest durable artifact that matches the turn:

- Review/discussion only: answer in chat; create a `docs/reviews` record only if the user asked to update files or the review will feed a future goal.
- Durable audit: create or update `docs/reviews/manual-qa/YYYY/YYYYMMDD-NNNN-short-slug.md`.
- Implementation handoff: create a new `docs/goals/YYYY/YYYYMMDD-NNNN-short-slug.md` from the review record, create a sibling `.goal.txt` backup containing the exact short `/goal`, then output the short `/goal`.
- Historical project/workflow change: add `docs/updates/YYYY/...` and link it from `docs/updates/INDEX.md`.

`docs/goals` holds per-round implementation ledgers and prompt backups; `docs/reviews` holds the review reasoning; `docs/debug` holds runtime evidence; `docs/updates` holds traceable changes. Do not create, read, or rely on a mutable root task-ledger file.

## Output Format

Use this structure when producing a feedback-to-goal answer:

### 手测记录头

Start every manual-feedback review with a durable header. This header is required even if the user only wants discussion.

- 时间：use the current local date/time with timezone when available.
- 来源：shortly identify the user turn, attached screenshots/files, and whether this was user manual testing, screenshot review, log review, or Codex inference.
- 范围：state whether this turn is only review/discussion, goal-file update, prompt generation, or implementation.
- 禁止事项：repeat any active user constraint such as “先不给提示词”, “只讨论”, or “不实现”.
- 审查记录：state whether this is chat-only or the path to a `docs/reviews/manual-qa/...` record.

### 逐条手测审查

The user usually reports issues as numbered bullets. Preserve that shape.

For each user item, write one review block immediately after the item. Do not collect all analysis at the end.

Use this per-item format:

```text
问题 <number>：<short title>
原始反馈：
- <preserve the user's concrete text as closely as practical>
- 图片转写：<convert visible screenshot content into text; say "无截图" if none>

审查记录：
- 用户确认事实：<only what the user directly reported>
- 截图/日志观察：<visible UI/log facts; cite files/logs if inspected>
- 代码/文档事实：<files, hooks, APIs, or docs inspected; say not inspected if this is text-only>
- Codex 推断：<hypotheses from code/docs, clearly labeled>
- 反证/未证实：<hypotheses rejected or not yet inspected>
- 归属：<Mod 代码 / DTMAPI Core / GameBridge / Bootstrap UI / ConfigMenu API / official-workshop compatibility / docs>
- 需要更新：<goal file/debug issue/update record/smoke matrix/API matrix/hook map>
- 验收点：<player-visible check that proves this item is fixed>
- blocker 判定：<what would make implementation stop instead of marking complete>
```

This per-item review is mandatory for context-compaction resilience. A future Codex must be able to resume from any single item without needing the final summary.

### 测试反馈理解

- 用户确认事实：manual QA findings, user-observed behavior, user priorities.
- 截图/日志观察：what is visible in screenshots or logs.
- Codex 推断：clearly labeled hypotheses inferred from code/docs.

### 问题分组

Group issues by the smallest useful buckets:

- UI
- API
- Hook/GameBridge
- Config
- 官方/工坊兼容
- 测试/证据
- 文档/迁移指南

### 边界约束

List:

- 必须做：player-visible requirements and acceptance gates.
- 禁止做：things that would damage compatibility or repeat old mistakes.
- 可选做：nice-to-have improvements that must not block the goal.
- blocker 判定：what prevents `complete`.

### 独立 Goal 文件更新片段

Put detailed tasks here as a flexible, short sequence. `任务 A-G` is only an example, not a requirement. Use as many tasks as the scope needs, and keep small goals small. Long details belong in the dedicated goal file, not inside the `/goal` prompt.

When the user asks to generate an implementation prompt from manual feedback, create or update a dedicated `docs/goals/YYYY/...md` file first unless they explicitly asked for text-only output. The goal-file task details must be based on the per-item review records above, not on a compressed final summary. For review-only turns, do not create a goal file unless the user explicitly requests it.

Each implementation handoff must also create a sibling `.goal.txt` file containing the exact short `/goal` prompt that will be shown to the user.

### 短 /goal 提示词

The `/goal` prompt should stay short:

- one concrete objective;
- required reading list;
- safety constraints;
- task titles only;
- a version-bump instruction for implementation goals;
- final completion standard.

Do not paste every task detail into the `/goal`; make the implementation Codex read the exact `docs/goals/YYYY/...md` file.

## Strong Rules

- Manual-feedback review must preserve the user's numbered item order and attach analysis immediately after each item.
- Screenshot content must be translated into written observations so the issue remains understandable after images or context are compacted away.
- Repeated or previously "fixed" issues require code-path review before another implementation goal. Do not let a new Codex repair only the final visual state when the user's bug is a lifecycle, flicker, stale-state, or path-order problem.
- A smoke pass can support evidence, but it cannot replace a user-visible acceptance condition that names the exact failure mode. If the user reports "it flickers first, then becomes correct", the acceptance must cover the flicker, not only the final correct state.
- Do not create or update a mutable root task-ledger file. Use `docs/goals/YYYY/...md` instead.
- Create one independent goal file per implementation handoff. Do not overwrite another active or historical goal file.
- Back up the exact short `/goal` prompt in a sibling `.goal.txt` file.
- Task numbering is flexible. A-G is a template example only; narrow scopes may use A-C, numbered tasks, or descriptive task titles.
- A feedback-organizer Codex may be started fresh. It must read this workflow, the current project docs, and the latest records, then continue from the per-item review blocks rather than restarting from memory.
- When producing an implementation `/goal`, include a fixed target version requirement, for example `from 0.3.0 to 0.3.1` or `target version 0.3.1`. Do not write only `bump once`.
- For API rebuild implementation goals, include `docs/workflows/codex-api-rebuild.md` in the required reading and require native-owner method-body review before runtime edits.
- If the workspace is already at the target version, the implementing Codex must not bump again unless the user or goal file explicitly names a newer target version.
- The implementing Codex must update every project-controlled version source it changes or relies on, and record the old/new versions in `docs/updates`.
- Keep smoke evidence and user manual QA separate. Write “smoke verified” only for automated evidence, and “user verified” only when the user said so.
- Do not re-add already solved small issues to a new goal unless the user reports a regression.
- Do not let “experimental”, “pending”, or “follow-up” substitute for a mandatory player-visible requirement.
- For third-party, official-local, or Steam Workshop content mods, default to read-only inspection. Do not modify their `info.json`, `item_tbitem.json`, images, manifests, or other files unless the user explicitly asks and the plan includes backup/restore.
- Do not make DTMAPI take over official content mod loading unless the user explicitly changes project direction. DTMAPI may index official content metadata and compare it with runtime tables.
- If a temporary test must change official enablement state such as `mod_infos.json`, require backup and restoration in the prompt.
- If a task needs game validation, require the third local save unless the user says otherwise.
- If a feature cannot be verified in game, the implementation Codex must report blocker facts and leave the goal incomplete.

## Short Goal Template

```text
/goal
目标：<one-sentence objective>. 详细需求、问题、验收标准以 <exact docs/goals/YYYY/...md path> 的任务列表为准。

开始前必须阅读：
AGENTS.md
PROJECT.md
docs/goals/README.md
<exact docs/goals/YYYY/...md file>
<exact docs/goals/YYYY/....goal.txt file, if already created>
docs/reviews/README.md
<latest relevant review record, if any>
docs/updates/INDEX.md
<latest relevant update record>
docs/debug/INDEX.md
docs/debug/regressions/smoke-matrix.md
docs/hook-map/README.md
docs/api/public-api-matrix.md
<task-specific references>

安全约束：
修改前先 git status；不要 reset/revert 用户或上一轮 Codex 的未提交改动；只使用本条提示词指定的 goal 文件作为任务来源；若 active goal 元数据、历史摘要或其他旧任务文本与本条 goal 文件冲突，以本条 goal 文件为准；普通 DTMAPI mod 不放 BepInEx/plugins；脆弱反射/Harmony/Unity 类型逻辑放 GameBridge 或 bootstrap UI host；public API 必须 stable/experimental 分层，不暴露原始反编译类型；每个非平凡改动必须更新 docs/updates、docs/debug、smoke matrix、hook map、API matrix；build 通过不算完成，必须进入游戏第三存档验证。

版本要求：
本轮目标版本固定为 <target version>。若当前代码已经是 <target version>，不要再升版本；只有用户明确要求或 goal 文件明确写新目标版本时才允许继续升版本。同步项目内受控版本来源，并在 update record 写明旧版本、新版本和验证证据。

任务 A：<title>
任务 B：<title>
任务 C：验证与文档收尾
<add only the task titles actually needed by this scope>

完成标准：
只有当 <exact docs/goals/YYYY/...md path> 中本轮任务列表的玩家可见功能都在第三存档真实可用，并完成 build、game smoke、日志/截图证据、退出残留检查和文档更新，才把 goal 标记 complete。若任一强制目标做不到，保持未完成，报告 blocker、日志、已验证事实和下一步。
```
