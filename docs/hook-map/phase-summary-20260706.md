# Hook Map Phase Summary - 2026-07-06

Status: docs-only synthesis

Source: `docs/hook-map/README.md`

## Core Rule

Every DTMAPI hook must be registered before it can back a stable public API event or helper. A registered hook, a HookStatus line, or one successful smoke run is not stability proof.

## Risk Map

| Area | Hook style seen in map | Public surface relationship | Current debt |
| --- | --- | --- | --- |
| Runtime lifecycle and shutdown | cleanup hooks, generation ledgers, lifecycle snapshots | Mostly internal diagnostics and ISSUE-010 investigation | Needs continued long-idle/root-set evidence before refactor stages are declared safe. |
| Save/load and title return | request coordinator, ReturnHome/LoadGame boundaries, SaveLoaded closure | Supports smoke and lifecycle diagnostics more than stable public API | ISSUE-010 remains open; do not treat bounded short smoke as full lifecycle proof. |
| Hook/event scheduling | main-thread scheduling, status publication, off-thread rejection | Internal safety layer for hooks/events | Needs regression evidence whenever new hooks/events are added. |
| Input and Y console | key-state fallback, console open/toggle, mouse/right-click behavior | Diagnostic/user-tooling surface, not stable gameplay API | Smoke has keyboard/mouse injection paths but they are flaky and not equivalent to human input. |
| Camera | CameraView experimental; CameraZoom obsolete compatibility | CameraView is experimental; CameraZoom failed/obsolete | Playable view needs manual movement/background/sync evidence. |
| Inventory, machines, farming | chest locator, planting gun, crops, action speed, action completion, fishing | Many experimental gameplay helpers | Needs native-owner plus manual third-save behavior, not just forced smoke logs. |
| Equipment and storage | equipment slots, hat/accessory/shield storage behaviors | Experimental/high risk | Cross-save pollution, duplicate state, and native UI/storage alignment remain recurring risks. |
| Animals and custom content | animal viewer, custom animal bridges, animator/AI/audio hooks | Content-pack route is healthier than arbitrary runtime creation | Custom content needs sleep/eat/voice/manual overnight evidence per animal/template. |
| Audio replacement | voice and audio replacement hooks | Experimental bridge/content behavior | Needs owner/signature-aware lifecycle cleanup and manual checks for native voice leakage. |
| Workshop/installer | No gameplay hook role | Packaging workflow only | Keep separate from Hook Map proof. Installer success does not prove runtime hook stability. |

## Hook Policy

- Prefer Postfix or read-only reflection.
- Use Prefix only when the native behavior must be guarded or redirected.
- Use Transpiler only with explicit review and regression evidence.
- Keep fragile Unity/Harmony/reflection logic inside `DTMAPI.GameBridge.DolocTown`.
- Keep `DTMAPI.Abstractions` free of raw decompiled, Unity, Harmony, or BepInEx types.

## Current Technical Debt

- The Hook Map is useful but too large for onboarding by itself. Read this summary first, then jump to the exact hook family.
- Some hooks are diagnostic scaffolding for lifecycle work. They should not accidentally become public APIs.
- Input automation remains a special risk: external smoke key injection can miss the foreground window or reuse stale key-down state, so manual QA still matters for keyboard/mouse features.
- Long-title-idle and repeated same-process save/load behavior remain under ISSUE-010 investigation.
- Hook additions should update the smoke matrix and the relevant debug/review record in the same change.

## Handoff Checklist

Before changing a hook-backed feature:

1. Read the exact Hook Map section.
2. Read the matching debug issue or manual QA review.
3. Check `docs/debug/regressions/smoke-matrix.md`.
4. Decide whether the hook backs public API, diagnostics, or an internal product feature.
5. Add evidence after validation, including game build, save slot, log line, and clean exit status.
