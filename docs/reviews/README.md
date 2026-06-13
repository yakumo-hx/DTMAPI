# DTMAPI Review Records

This directory stores pre-implementation review records. A review record is not an update record and is not proof that a bug is fixed. It is the durable bridge between user manual QA and the next implementation goal.

## Purpose

Use review records when manual QA reveals regressions, ambiguous behavior, lifecycle bugs, UI flicker, state pollution, hook uncertainty, or repeated failures that need code-path understanding before another Codex starts implementation.

The review should answer:

- What did the user actually observe?
- What did the screenshot or log show?
- Which mod, DTMAPI layer, hook, UI lifecycle, config path, or official content path is likely involved?
- Which code paths must be inspected before a fix is attempted?
- What acceptance check would prove the exact user-visible bug is gone?
- What evidence gap would keep the implementation goal incomplete?

## Relationship To Other Docs

- `docs/reviews`: pre-implementation analysis and root-cause/path review.
- `docs/reviews/api`: API native-owner, status, and rebuild-readiness reviews.
- `docs/reviews/api/native-function-map`: generated native symbol/call-graph coverage data plus a static workbench for seeing reverse-map and native-owner report coverage.
- `docs/reviews/api/local-mods-native-owner`: four-round local mod semantic/API-demand to native-owner review library for current testmods, legacy local own-mod sources, and local third-party sample groups.
- `docs/reviews/api/smapi-ecosystem-map`: four-round clean-room semantic map from mature SMAPI ecosystem mod capabilities to DTMAPI candidate API layers and research priorities.
- `docs/reviews/code`: branch-wide or focused code-level audits that are not only user manual-QA transcription and not an API native-owner review.
- `docs/goals/YYYY/...`: immutable implementation task ledgers for future `/goal` prompts.
- `docs/goals/YYYY/....goal.txt`: backups of the exact short `/goal` prompts.
- `docs/debug`: runtime investigation, evidence, regressions, and known bug state.
- `docs/updates`: traceable records of document, runtime, workflow, or project-direction changes.

Do not use a mutable root task-ledger file for review output or implementation handoff. Reviews feed dedicated files under `docs/goals/YYYY/`.

Do not use a review record to claim a fix is complete. Completion belongs to update/debug evidence after implementation and validation.

## File Layout

```text
docs/reviews/
  README.md
  manual-qa/
    YYYY/
      YYYYMMDD-NNNN-short-slug.md
  api/
    native-owner-domains/
      INDEX.md
      NN-domain-report.md
    native-function-map/
      README.md
      index.html
      data/
    local-mods-native-owner/
      INDEX.md
      rounds/
    smapi-ecosystem-map/
      INDEX.md
      rounds/
    third-party-mods/
      INDEX.md
      ROUND-1-agent-e-mod-semantics.md
    YYYY/
      YYYYMMDD-NNNN-short-slug.md
  code/
    YYYY/
      YYYYMMDD-NNNN-short-slug.md
  templates/
    manual-qa-review.md
```

Use `docs/reviews/manual-qa/YYYY/...` for durable user manual-test reviews. The year folder may be created only when the first review for that year is needed.

Use `docs/reviews/code/YYYY/...` for durable code-level audits when the request spans multiple modules, documentation ledgers, debug records, or release-readiness checks and does not fit a single manual-QA issue list.

Use `docs/reviews/api/YYYY/...` for API native-owner reviews. These reviews should name the public API symbol or domain, the native responsibility function, the authoritative state holder, the current DTMAPI bridge path, ordinary-mod usability, and the concrete failure mode if the API is exposed too early.

Use `docs/reviews/api/native-owner-domains/INDEX.md` as the fixed long-lived library for future API/domain native-owner discovery. Reports there map broad gameplay/content goals to native responsibility functions, official Workshop support boundaries, blocked/gap status, and stable DTO/adapter concepts. They are pre-implementation discovery records, not API stability proof.

Use `docs/reviews/api/native-function-map/README.md` as the generated visual index for reverse metadata coverage. It helps compare all native functions against system maps and native-owner report tags, but it does not replace method-body review or public API matrix promotion gates.

Use `docs/reviews/api/local-mods-native-owner/INDEX.md` as the fixed local mod demand/native-owner integration index. It records the four-round review of current `testmods`, legacy local own-mod sources, and local third-party sample groups. It is useful before an API rebuild because it shows which existing local mods stress the same native owners, but it is not API stability proof.

Use `docs/reviews/api/smapi-ecosystem-map/INDEX.md` as the clean-room ecosystem API research map. It translates mature SMAPI mod ecosystem semantics into DTMAPI candidate API layers, gaps, and research priorities. It is not SMAPI compatibility, does not copy SMAPI or third-party code, and does not promote any DTMAPI public API status.

Use `docs/reviews/api/third-party-mods/INDEX.md` for read-only local third-party mod sample reviews. These reports extract semantic API demand and compatibility risk from visible files, metadata, and docs only; they are not migration permission and do not allow third-party code to be copied into DTMAPI.

## When To Create A Durable Review

Create or update a durable review record when:

- the user asks for an audit, review, code-level review, or workflow-backed prompt;
- the same issue has survived a previous claimed fix or smoke pass;
- the issue involves UI lifecycle, input, hotkeys, map transitions, save/load, official/Workshop loading, machines, vehicles, config persistence, or GameBridge hooks;
- the next step is to generate a dedicated goal file or short `/goal` for a fresh Codex.

For pure discussion, a chat-only review is acceptable unless the user asks to update files. If an implementation goal will be produced, prefer a durable review first for complex or repeated issues.

Create an API review record before an API rebuild goal when:

- an API is classified as `Gap`, `Blocked`, `debug-only`, `registry-only`, or `DTMAPI-internal`;
- the feature currently succeeds only through UI, debug console, registry/index state, or smoke-helper evidence;
- the fix requires GameBridge/native-owner work;
- the API touches global state, save/load, transitions, equipment, vehicles, machines, camera, input, or custom entity runtime creation.

## Minimum Per-Issue Review

Each user-numbered item must keep its own analysis directly under it:

- original feedback;
- screenshot/log transcription;
- user-confirmed facts;
- code/doc facts inspected;
- likely ownership layer;
- root-cause hypotheses;
- rejected or unproven hypotheses;
- acceptance checks;
- blocker conditions;
- downstream docs to update if implementation happens.

Do not move all analysis to the end. This is required so a compacted context or fresh Codex can resume from any single issue.

## API Review Minimum

Each API/native-owner review block should include:

- public symbol or domain;
- current matrix status and recommended status;
- declaration and implementation paths;
- native owner and native state holder, or a clear "not found yet";
- hook/bridge path and whether it is native-owner proof or only UI/debug proof;
- save/load, transition, disable/re-enable, and multi-mod risks;
- ordinary-mod usability;
- exact blocker conditions;
- recommended next action: document, downgrade, targeted native-owner deep dive, GameBridge rebuild, or no action.

The fixed native-owner domain reports must also preserve the user's fuzzy semantic target and mark each subtopic as `Found`, `Partial`, `Not found`, or `Blocked` so a future goal can avoid redoing the same reverse lookup.
