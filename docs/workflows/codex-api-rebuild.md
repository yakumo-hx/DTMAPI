# Codex API Rebuild Workflow

This workflow is for rebuilding DTMAPI APIs after a native-owner audit. It is separate from manual QA feedback organization and from ordinary mod bug fixing.

Use it when the user asks for:

- API 重做、底层 API 重做、GameBridge 重做、原生责任函数审查后的实现；
- deciding whether an API is stable, experimental, debug-only, registry-only, internal, or blocked;
- turning `docs/reviews/api` findings into an implementation goal;
- fixing a mod bug whose root cause is an untrustworthy API boundary rather than mod logic.

Do not use this workflow for pure manual-QA triage unless the manual issue points at a public API, GameBridge boundary, or native-owner gap.

## Required Reading

Before creating or executing an API rebuild goal, read:

- `AGENTS.md`
- `PROJECT.md`
- `docs/goals/README.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `docs/reviews/api/smapi-ecosystem-map/INDEX.md` when the API is an ecosystem-level surface inspired by mature mod-loader patterns, UI/HUD/content pipelines, or cross-mod integration
- `docs/reviews/api/native-function-map/README.md` when function-map coverage, call relationships, or all-method context would help scope the native-owner review
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`

For implementation, also read the task-specific reverse maps, decompiled build, official docs, and research notes named by the goal.

## Current API Truth

The 0008/0009/0010 audits close the current DTMAPI public API surface. They cover existing APIs, not every possible future API and not every Doloc Town native method.

For future domains that are not already an existing public API row, start from `docs/reviews/api/native-owner-domains/INDEX.md`. That fixed library records native responsibility candidates and blocked/gap status for broad user-facing goals such as world refresh, NPCs, animals, birds, drones, vehicles, maps, equipment, effects, stacks, follower pets, and ranged weapons. It is discovery evidence only; it does not promote API stability.

For future APIs that are motivated by an existing local mod, also read `docs/reviews/api/local-mods-native-owner/INDEX.md`. That library maps current `testmods`, legacy local own-mod sources, and local third-party sample groups to semantic demand, native-owner candidates, shared owner conflicts, and final review confidence. It helps avoid rebuilding an API from one mod's convenience behavior while missing another local mod that shares the same native owner.

For ecosystem-level surfaces inspired by mature mod-loader patterns, also read `docs/reviews/api/smapi-ecosystem-map/INDEX.md`. That library maps SMAPI ecosystem semantics to DTMAPI candidate layers such as Core, UI host, GameBridge read-only query, mutation, Diagnostic, Blocked, and Future-reserved. It is clean-room research only and must not be used as SMAPI compatibility or public API stability proof.

The generated native function map under `docs/reviews/api/native-function-map/` can be used before or during Phase 1 to inspect all methods, system-map tags, native-owner report coverage, and internal call relationships. A colored or connected node is still only a research signal; method-body review and runtime evidence remain required.

Future APIs should start from the native owner, then move outward:

```text
native responsibility function / state holder
  -> GameBridge adapter
  -> Abstractions contract
  -> Core service / mod usage
  -> third-save validation
  -> docs and API matrix
```

Do not start from a mod convenience method and then search for a native hook later. That pattern created several APIs that looked successful in UI or smoke tests but did not own the true game state.

## API Status Vocabulary

Use these categories consistently:

- `stable open`: ordinary mods may depend on it.
- `experimental open`: ordinary mods may try it, with documented limits and evidence gaps.
- `debug-only`: console/test/user-debug path only; ordinary mods must not rely on it for stable gameplay.
- `registry-only`: safe definition/index/status contract, but no runtime native creation or mutation promise.
- `DTMAPI-internal`: usable by DTMAPI's own migrated or diagnostic mods, not a general author API.
- `blocked-rebuild`: public wording or current implementation is misleading or unsafe until native-owner adapters are redesigned.

Changing an API's status is a project-facing change and must update the public API matrix, developer docs, update record, and any relevant goal/review records.

## Rebuild Phases

### Phase 0 - Pick One Boundary

API rebuild goals should be narrow. Pick one domain or one tightly related set of APIs. Good first targets are:

- CameraZoom background/fog/room rendering;
- MachineProduction scheduler/storage/electric integration;
- EquipmentSlots native UI/storage/save transaction;
- Vehicle second motor/native multi-instance feasibility;
- CustomEntity runtime verbs;
- Input suppression native consumer.

Do not combine all high-risk APIs into one implementation goal.

### Phase 1 - Native Owner Method-Body Review

Before runtime changes, inspect the relevant native method bodies or maps. The review must answer:

- Which native function actually performs the player-visible effect?
- Which object owns the authoritative state?
- Which hooks currently fire, and which are only UI/debug evidence?
- What happens on save/load, returned-to-title, room transition, map transition, disable/re-enable, and game exit?
- What singleton/global state can be polluted?
- What would break when two ordinary mods use the same API?
- Which evidence would prove the API is connected to native state?

If the native owner cannot be identified, stop and mark the goal blocked or docs-only. Do not patch a mod around the missing owner.

### Phase 2 - Contract Correction

Before or during implementation, correct API semantics:

- split stable DTO/registration contracts from experimental runtime adapters;
- downgrade debug/internal/blocked APIs in docs and matrix;
- remove or annotate DTO names that imply runtime success when they only report DTMAPI registry/status;
- add owner-token, priority, stacking, and restore rules for global mutable features;
- make failure reasons explicit and user/developer visible.

### Phase 3 - GameBridge Rebuild

Runtime/native work belongs in `DTMAPI.GameBridge.DolocTown` or the bootstrap UI host when UI ownership truly lives there.

The GameBridge layer must own:

- fragile reflection/Harmony/Unity type references;
- native state reads/writes;
- hook install/uninstall and lifecycle restore;
- transition boundaries;
- evidence logging for native owner success/failure.

Public APIs must expose stable DTOs and result objects, not raw decompiled game types.

### Phase 4 - Validation

Build success is not enough. For any runtime rebuild, require:

- Release build and unit tests where available;
- third local save slot game smoke;
- before/after logs showing native owner state, not only UI state;
- screenshot or log evidence for the exact player-visible issue;
- returned-to-title or clean exit check when lifecycle/global state is touched;
- no leftover `DolocTown.exe`;
- updated `docs/updates`, `docs/debug` when runtime/debug paths changed, smoke matrix, hook map, and public API matrix.

## Rebuild Goal File Requirements

Every API rebuild implementation handoff must create:

- one detailed `docs/goals/YYYY/YYYYMMDD-NNNN-short-slug.md`;
- one sibling `.goal.txt` containing the exact short `/goal`;
- one update record for the workflow/goal creation if files were changed.

The goal file must include:

- target version, or an explicit docs-only/no-version statement;
- exact API/domain scope;
- prior review records to read;
- native owner questions that must be answered before code changes;
- API status changes expected;
- GameBridge implementation boundaries;
- player-visible and developer-visible acceptance checks;
- blocker rules that prevent marking complete.

## Strong Rules

- Existing API audits cover existing DTMAPI APIs only. New APIs still need native-owner discovery.
- Do not treat "hook fired", "UI displayed", "registry contains item", or "smoke helper passed" as native-owner proof.
- Do not stabilize a mutating API until save/load, transition, disable/re-enable, and multi-mod ownership are understood.
- Do not fix ordinary mod bugs by piling code into the mod when the failing dependency is a blocked or sidecar DTMAPI API.
- Do not expose debug-only APIs as ordinary author conveniences.
- Do not copy decompiled source or third-party mod code into DTMAPI.
- If a task cannot find a native owner, the correct result is a blocker with evidence, not a speculative runtime patch.

## Short Goal Addendum

For API rebuild goals, add this line to the short `/goal` required reading:

```text
docs/workflows/codex-api-rebuild.md
```

Add this safety clause:

```text
先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。
```
