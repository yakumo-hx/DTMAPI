# 20260615-0001 - Codex Handoff / Project Space / Maintainability Audit

Status: recorded
Date: 2026-06-15
Branch: `Refactor`
Source request: user asked to switch back to the main worktree branch and run parallel sub-agents to study how hard it is for a fresh Codex to take over, whether the project space is worth optimizing, and to scan DTMAPI core code readability, maintainability, redundancy, and likely garbage code.

## Required Context Read

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/reviews/README.md`
- Prior code audit: `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md`

## Method

- Confirmed the main worktree at `E:\Python_project\DTMAPI` is on branch `Refactor`.
- Spawned three read-only sub-agents:
  - New Codex handoff difficulty.
  - Project-space optimization value.
  - DTMAPI core code readability / maintainability / redundancy scan.
- Ran local read-only scans with `git status`, `git branch`, `git worktree list`, `rg`, `Get-ChildItem`, and targeted `Get-Content`.
- No runtime code was changed. No game smoke was run.

## Executive Summary

DTMAPI is not hard to enter because it lacks documentation. It is hard because it has a lot of durable memory, smoke evidence, historical failed paths, and experimental API history. A fresh Codex can take over safely if it follows the required context and current ledgers, but it can easily go wrong if it scans everything recursively or treats old smoke history as current truth.

The project space is worth optimizing, but the first target should not be deleting review/update/goal history or weakening `testmods`. The first target should be the local evidence layout and onboarding/current-state navigation.

The codebase has a healthy high-level boundary (`Abstractions`, `Core`, `GameBridge`, `ModConfigMenu`, `BepInExBootstrap`, `testmods`), but several current maintainability risks remain:

- `docs/debug/evidence` is enormous locally and smoke collection appears to duplicate historical evidence.
- `DTMAPI.sln` still references archived `testmods\SecondMotorMod`, although the active build script no longer does.
- `ReflectedImGuiOverlay` is likely dead legacy UI.
- MotorVehicle/SecondMotor API and hooks remain active experimental runtime code after the sample mod was archived.
- `ContentQueryService` / content helper semantics need an enabled-vs-diagnostic boundary.
- `IInputHelper.Suppress` is misleading unless it is either renamed/documented or wired into real native input suppression.
- `DtmApiRuntime`, `DolocTownGameBridge`, `SmokeHarness`, and `ConfigMenuRegistry` are too large and should be split by responsibility in future refactor slices.

## Findings

### P1 - Fresh Codex can take over, but needs a current-truth path

Evidence:

- Entry rules are strong: `AGENTS.md`, `PROJECT.md`, `README.md`, `src/README.md`, `tools/scripts/README.md`.
- Durable systems exist: `docs/debug`, `docs/reviews`, `docs/goals`, `docs/updates`, `docs/api/public-api-matrix.md`.
- Prior workflow docs already separate review from implementation: `docs/workflows/codex-feedback-to-goal.md`, `docs/goals/README.md`, `docs/reviews/README.md`.

Risk:

A fresh Codex may read too much history and infer the wrong present state: archived SecondMotor evidence, Experimental APIs with successful smokes, DirectExe-vs-Steam launch differences, and old UI routes can all look current unless the model reads the latest update/debug/API rows.

Recommendation:

Add a small current-state handoff doc, for example `docs/CURRENT.md` or `docs/onboarding/current-state.md`, containing:

- current branch and version source;
- canonical build/test commands;
- "use scripts, not `DTMAPI.sln`, until solution is cleaned";
- active vs archived testmods;
- current high-risk APIs;
- latest smoke evidence pointers;
- where not to search by default (`docs/debug/evidence`, generated native map JSON, official HTML crawl).

### P1 - Local evidence space is the biggest project-space problem

Evidence:

- Local directory size scan found `docs/debug` at roughly `126 GB`.
- Largest files are many `docs/debug/evidence/GAME-SMOKE/*.zip` files, commonly 200-350 MB.
- Project-space sub-agent counted roughly 404k files under `docs/debug/evidence`, with `GAME-SMOKE` dominating.
- `.gitignore` ignores `docs/debug/evidence`, so Git is protected, but local recursive scans and backup/copy tasks are slow and noisy.
- `tools/scripts/collect-logs.ps1` was reported to copy `$dtmapiDir\evidence` recursively into each smoke evidence folder, causing repeated historical evidence capture.

Risk:

New Codex sessions, local package scripts, and human file explorers can be overwhelmed by ignored but present evidence. Recursive search can become slow or misleading. Repeated smoke evidence can grow superlinearly if historical runtime evidence keeps being copied into each new evidence folder.

Recommendation:

Create a dedicated evidence retention goal:

- keep repo docs as canonical summaries/indexes;
- move full raw evidence to an external artifact root by default;
- stop smoke collection from copying entire historical `DTMAPI/evidence`;
- copy only current-run screenshots/logs/report pointers unless an explicit archival flag is used;
- add a small README under `docs/debug/evidence/` explaining the local-only artifact policy.

Do not delete evidence in this audit. This should be a separate user-approved cleanup because the local evidence may be useful for current investigations.

### P1 - Public content helper likely exposes disabled or conflicting content

Evidence:

- Sub-agent flagged `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`, including the content rebuild path and `TryReadTextAsset`.
- Reported behavior: content assets are indexed from discovered mods broadly, while `TryReadTextAsset(relativePath)` reads by relative path and returns the first match.

Risk:

Disabled official/Workshop content may become visible to ordinary DTMAPI mods. Relative path collisions such as `manifest.json`, common content filenames, or duplicated assets can return a wrong source. This can break the product rule that DTMAPI respects official/Steam enablement.

Recommendation:

Split content visibility:

- runtime enabled content for ordinary mod APIs;
- diagnostic/all content for Manager/debug/reporting;
- source-aware reads with owner/workshop/local package identity;
- warning rows for collisions.

This should be a high-priority implementation goal before DTMAPI treats content helper behavior as stable.

### P1 - `IInputHelper.Suppress` is semantically misleading

Evidence:

- Sub-agent found `IInputHelper.Suppress` in `src/DTMAPI.Abstractions/Helpers.cs`.
- Runtime input suppression is only one-frame helper state and is cleared by `Input.ClearFrame`.
- Bootstrap registered-button polling does not consume suppressed state, and native input isolation is currently debug-console/UI-specific.
- `docs/debug/INDEX.md` already has a known note that `IInputHelper.Suppress` is not native input isolation.

Risk:

Ordinary mods may believe this suppresses Doloc Town input when it does not. That is a public API contract trap.

Recommendation:

Either:

- downgrade/rename/document it as helper-local diagnostic state, or
- wire it through Bootstrap/GameBridge/native input gates so it actually suppresses gameplay/UI input where promised.

### P2 - `DTMAPI.sln` has a stale active `SecondMotorMod` project

Evidence:

- `DTMAPI.sln` still references `testmods\SecondMotorMod\SecondMotorMod.csproj`.
- `Test-Path testmods\SecondMotorMod\SecondMotorMod.csproj` returned false.
- The archived project exists under `archive/second-motor-20260615/testmods/SecondMotorMod`.
- `tools/scripts/build.ps1` no longer includes `SecondMotorMod`, and the archive update record says it was removed from active build/project definitions.

Risk:

New Codex or IDE users may run solution-level build and hit a false failure. They may also assume SecondMotor is active.

Recommendation:

Remove the stale solution project entry or move it under an explicit archived solution folder that is not part of active builds. This is a small cleanup with high handoff value.

### P2 - MotorVehicle/SecondMotor runtime code remains active after sample archive

Evidence:

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs` still exposes `IMotorVehicleApi`.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs` still registers `IMotorVehicleApi`.
- `DolocTownGameBridge.InstallHarmonyHooks` still installs native motor hooks and reports `Vehicle.MotorApi`.
- `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs` still contains the custom/second motor implementation.
- `docs/updates/2026/20260615-0004-second-motor-archive.md` explicitly says DTMAPI vehicle API code may remain because no active mod will call it.

Risk:

This is not automatically garbage code, because it is retained research and Experimental API history. But it is active hook surface for a feature whose only sample was archived after severe texture pollution. Active hooks increase maintenance cost and can confuse hook-readiness/status interpretation.

Recommendation:

Choose one of two explicit policies:

- keep `IMotorVehicleApi` and hooks active, but mark them in a current-state doc as retained Experimental research with no active consumer; or
- gate motor hook installation behind a config/dev flag or registered-consumer detection, so archived vehicle research does not participate in normal runtime hook readiness.

Do not delete the vehicle code casually; future vehicle work needs a new native-owner slice.

### P2 - GameBridge is still too much of a God object

Evidence:

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs` has large fields mixing feature registration, hook flags, smoke state, UI evidence paths, upload fallback state, and runtime lifecycle.
- File-size scan found:
  - `Smoke/SmokeHarness.cs`: about 4070 lines.
  - `DolocTownGameBridge.cs`: about 1255 lines.
  - `Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`: about 2265 lines.
  - `Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`: about 2210 lines.
  - `Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`: about 1766 lines.

Risk:

The composition root is doing too much. This makes hook changes risky because unrelated concerns sit in the same field/status/update loop. It also makes fresh Codex scans noisy.

Recommendation:

Keep `DolocTownGameBridge` as composition root, but split:

- hook installation by domain (`LifecycleHookInstaller`, `WorkshopHookInstaller`, `InputHookInstaller`, `FeatureHookInstaller`);
- smoke orchestration into a `SmokeCoordinator`;
- vehicle/archived experimental hook readiness out of global `AllHookTargetsReady`;
- status publication into small feature-owned status publishers.

### P2 - Hook target matching is too weak for overload drift

Evidence:

- Sub-agent flagged `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`.
- Reported behavior: target lookup chooses by method name and parameter count.

Risk:

If Doloc Town adds an overload with the same parameter count, DTMAPI may patch the wrong method. This is a high-risk class of silent hook breakage.

Recommendation:

Extend patch target descriptors to include parameter type names and return-type checks for high-risk hooks. Log candidate signatures and final selected signature in diagnostics when resolution is ambiguous.

### P2 - `DtmApiRuntime` should be split before loader behavior grows further

Evidence:

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs` handles mod discovery, dependency/load planning, DLL activation, event dispatch surfaces, config locks, manager snapshots, diagnostics export, and status code inference.
- Sub-agent flagged that some status classification is inferred from error message substrings.

Risk:

Loader status semantics can drift with wording/localization changes. New mod-loading work will be difficult to review because discovery, planning, loading, and status rendering share one class.

Recommendation:

Split into:

- `ModDiscoveryService`;
- `ModLoadPlanner`;
- `CodeModLoader`;
- `ModStatusBuilder`;
- structured diagnostic error codes instead of message-substring classification.

### P2 - Duplicate UniqueID handling is too quiet

Evidence:

- Sub-agent flagged `src/DTMAPI.Core/Manifesting/ManifestReader.cs`, where source-managed duplicates are preferred by source priority.

Risk:

Local developer packages can be silently shadowed by official-local or Workshop packages with the same `UniqueID`. This is especially confusing during upload/resubscribe testing.

Recommendation:

Keep priority if desired, but emit a warning listing the winner and suppressed roots. Show it in Manager diagnostics.

### P2 - Config menu registry is too monolithic and callback isolation is incomplete

Evidence:

- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs` is a single file carrying API registration, item implementations, editing/preview/save/cancel state, validation, and callback invocation.
- Sub-agent reported `Name`/`Tooltip` have safe invocation, but display/commit/preview/save callbacks are not uniformly isolated.
- Preview applies setters and rolls back, which can cause side effects if a mod setter has behavior beyond assigning config fields.

Risk:

A bad mod callback can break a page transaction or entire UI render. Preview may trigger side effects that cannot be rolled back.

Recommendation:

Split item factories and transaction helpers. Centralize safe callback invocation and diagnostics. Revisit whether `Save()` should leave `IsEditing = true`, and document preview setter side-effect constraints.

### P3 - `ReflectedImGuiOverlay` is likely dead legacy UI

Evidence:

- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs` defines a complete old overlay.
- `rg` found no active instantiation; `BootstrapPlugin` constructs `ReflectedTitleMenuSettingsUi` and `ReflectedDebugConsoleUi`, not `ReflectedImGuiOverlay`.
- The old overlay still uses first-N truncation (`Take(18)`, `Take(16)`) and older dev-diagnostics text.

Risk:

Future Codex may patch or reason from this old UI path by mistake. If re-enabled, it would reintroduce old truncation behavior.

Recommendation:

Confirm no reflection-only entry exists, then remove it or move it to a clearly archived diagnostics file outside active runtime compilation.

### P3 - Roadmap-only source directories look like active projects

Evidence:

- `src/DTMAPI.ContentPatcher`, `src/DTMAPI.ConsoleCommands`, and `src/DTMAPI.TemplateMod` currently contain only README placeholders and no `.cs` files.

Risk:

They are useful roadmap markers, but can mislead scanners into assuming these components exist.

Recommendation:

Mark them as roadmap-only in current-state/onboarding docs, or move placeholder planning notes under docs until a dedicated implementation goal starts.

### P3 - Generated/reverse support files need search guidance

Evidence:

- `docs/reviews/api/native-function-map/data/methods.json` is about 28 MB and `links.json` about 5 MB.
- Official Workshop HTML crawl is useful but much larger than the markdown/text extracts.

Risk:

Full-text search over generated JSON/HTML can swamp normal code review.

Recommendation:

Add search guidance: ordinary work should use markdown indexes and generated workbench docs first, and only search raw JSON/HTML when native-owner or official-doc deep dive requires it.

## Redundant / Garbage / Retained Research Classification

Likely removable after a small confirmation:

- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs` as an uninstantiated old UI route.
- Stale `SecondMotorMod` entry in `DTMAPI.sln`.

Retained research, not garbage:

- `IMotorVehicleApi` and MotorVehicle GameBridge implementation. It is active risk, but also vehicle research history. Gate or clearly label it before deleting.
- `archive/second-motor-20260615/**`. This is useful rollback/research history.
- `docs/reviews`, `docs/goals`, `docs/updates`, `docs/debug` summaries. These are project memory and should not be compressed away.
- `testmods`. These are functional acceptance slices, not sample noise.

Space issue, not source garbage:

- `docs/debug/evidence/**` local raw evidence and repeated `GAME-SMOKE` payloads. Move/retention policy is needed, not blind deletion.

## Recommended Next Plan

1. Small cleanup goal:
   - remove stale `SecondMotorMod` from `DTMAPI.sln`;
   - remove/archive `ReflectedImGuiOverlay`;
   - add current-state handoff doc;
   - document roadmap-only source directories and generated-data search guidance.

2. Evidence-space goal:
   - change smoke/log collection so each run does not copy historical `DTMAPI/evidence`;
   - support external artifact root as default or documented option;
   - keep repo-side summaries and selected small evidence only.

3. Runtime semantics goal:
   - fix content helper enabled-vs-diagnostic boundary;
   - add duplicate `UniqueID` diagnostics;
   - clarify or implement `IInputHelper.Suppress`.

4. Maintainability refactor goal:
   - split `DtmApiRuntime` loader responsibilities;
   - split GameBridge hook installers and smoke coordinator;
   - strengthen Harmony hook signature matching;
   - split ConfigMenu registry transactions/callback isolation.

## Validation

- Read-only scans only.
- No build/test/game smoke run, because this audit created no runtime code change.
- This review should be treated as a planning/audit artifact, not as proof of fixes.

