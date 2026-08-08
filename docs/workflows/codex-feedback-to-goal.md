# Codex Feedback Review Workflow

Status: active workflow under a legacy filename.

This workflow turns user manual-test feedback into durable facts, root-cause analysis, acceptance checks, and implementation records.

## When To Use

Use this file when the user asks to:

- review or explain manual-test findings;
- investigate why a repeated bug survived an earlier fix;
- turn screenshots, logs, or numbered feedback into durable constraints;
- separate confirmed facts from Codex inference;
- prepare a complex implementation boundary that must survive context compaction;
- route API/native-owner work into `docs/workflows/codex-api-rebuild.md`.

If the user asks to implement, the same Codex may continue after the review unless the user explicitly requests review-only work. Follow `AGENTS.md`, the relevant review/debug records, and the update-record rules.

## Artifact Policy

Use the smallest durable artifact that matches the request:

- Discussion only: answer in chat; create no file unless requested.
- Repeated, high-risk, manual-QA, API, lifecycle, hook, input, save/load, UI, machine, vehicle, or official-content issue: create or update a task-specific `docs/reviews/...` record before implementation when durable root-cause analysis is needed.
- Implementation: create the required `docs/updates/YYYY/YYYYMMDD-NNNN-short-slug.md` record with `proposed` or `in-progress` status, then update that same record through `implemented`, `verified`, `blocked`, or `reverted`.
- Runtime investigation: update the relevant `docs/debug/issues/...` record and evidence/smoke records only when their facts change.
- API or Hook work: update the public API matrix or Hook Map only when that boundary or its evidence changes.

## Required Inputs

Read the relevant current truth before producing a durable review or implementation record:

- `AGENTS.md`
- `PROJECT.md`
- `docs/reviews/README.md` for manual-QA or root-cause work
- the latest task-specific review, if one exists
- `docs/updates/INDEX.md` and the latest relevant update
- `docs/debug/INDEX.md` and the relevant issue record for recurring runtime problems
- `docs/debug/regressions/smoke-matrix.md` for runtime validation work
- `docs/hook-map/README.md` for Hook work
- `docs/api/public-api-matrix.md` for API work
- `docs/workflows/codex-api-rebuild.md` for API rebuild, native-owner follow-up, or GameBridge boundary redesign
- task-specific official docs, research notes, and reverse references

Do not recursively read all history by default. Start from current indexes and task-specific records.

## Review-First Gate

Manual QA should pass through these layers:

1. **Record**: preserve the user's numbered issue order and translate screenshots/logs into text.
2. **Review**: inspect relevant current docs, prior evidence, and code paths. Attach analysis immediately under each issue.
3. **Implement**: if authorized, open an in-progress update record, make the smallest useful change, and validate it.
4. **Close or block**: finish the update record and synchronize only the specialized ledgers whose facts changed.

Do not skip the Review layer for:

- regressions after a claimed fix;
- issues that survived smoke evidence;
- UI flicker, stale text, or lifecycle-dependent layout;
- input/hotkey unreliability;
- save/load, reload, transition, or process lifecycle problems;
- GameBridge/Harmony/reflection hooks;
- official-local or Workshop content indexing/loading;
- vehicles, machines, placed objects, item/mail systems;
- broad symptoms such as unrelated objects changing;
- API/native-owner gaps supported only by UI, registry, debug-console, or smoke-helper evidence.

For API rebuild work, identify the native responsibility function or state holder before runtime changes, or record the missing owner as a blocker.

For save-related feedback, keep three things separate before implementation:

- the player-facing save/rollback semantics confirmed by the user;
- the actual native `SaveGame` and DTMAPI `SaveSaving` / `SaveSaved` / `SaveLoaded` hook mapping;
- Codex inference about sidecars, journals and recovery windows.

Classify each affected datum and state what should happen when no native save succeeds. Diagnostic InstantSave, direct `SaveSaved` invocation, or a sidecar-only assertion can be focused technical evidence, but cannot replace a normal native-save acceptance plus a no-save return/exit rollback case.

Also classify the test itself before collecting evidence:

- `NoNativeSave` is the default for ordinary behavior, Hook, UI, lifecycle, GC and long tests. It must avoid every native-save entry, record only length/hash/mtime by default, and prove player archive plus relevant committed sidecar state unchanged before any runner or external file restoration. Its green path neither creates a routine byte backup nor writes one back; an explicitly justified emergency restoration makes the run non-acceptance.
- `NativeSaveExpected` and `ArchiveMutation` are reserved for an intentional native save, archive management or startup migration. They require a disposable fixture isolated from live Steam AutoCloud; cleanup/restoration is not evidence that rollback or commit behavior passed.
- Runtime locking and restoration of deliberately changed deployment, profile, configuration and non-save test assets remain separate obligations.

## Manual Feedback Format

### Review header

- Time
- Source
- Scope: discussion, durable review, implementation, or validation
- User constraints
- Related review/update/debug records

### Per issue

Preserve every user-numbered item and put its analysis immediately below it:

```text
问题 <number>：<short title>
原始反馈：
- <user wording>
- 图片转写：<visible screenshot details or 无截图>

审查记录：
- 用户确认事实：
- 截图/日志观察：
- 代码/文档事实：
- Codex 推断：
- 反证/未证实：
- 归属：
- 数据分类与官方提交边界（仅保存相关问题）：
- 需要更新：<review/update/debug issue/smoke/API/hook docs only when applicable>
- 验收点：
- blocker 判定：
```

## Strong Rules

- Keep user-confirmed facts, screenshot/log observations, and Codex inference separate.
- Translate screenshot-only details into text so compaction does not erase the issue.
- A smoke pass supports evidence but cannot replace the exact player-visible acceptance condition.
- Do not copy the same completion narrative into Review, Update, Debug Index, Issue, Smoke Matrix, API Matrix, and Hook Map. Each fact should have one canonical owner; other records should link to it.
- Review records are pre-implementation reasoning. Completion and blocker outcomes belong in the update record; long-running runtime state belongs in the issue record.
- Keep smoke evidence and user manual QA separate. Write `smoke verified` only for automated evidence and `user verified` only when the user said so.
- Use fixed target versions when a version change is required. If the workspace is already at the target, do not bump again without a new explicit request.
- Third-party, official-local, and Steam Workshop content is read-only unless the user explicitly authorizes a scoped modification with backup/restore.
- If game validation is required, use the third local save unless the task says otherwise.
- If a required feature cannot be verified, finish the update as blocked or partially verified with exact evidence gaps; do not claim completion.
