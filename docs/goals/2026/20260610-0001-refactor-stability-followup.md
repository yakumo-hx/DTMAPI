# DTMAPI Refactor Stability Follow-Up Goal

Status: in-progress
Created: 2026-06-10
Target branch: `Refactor`
Target version: no version bump

## Source Request

The user requested implementation of the Refactor stability follow-up plan from the latest review package. The plan starts from clean `Refactor` commit `df04686` and executes independent `codex/...` branches, each validated and merged back with `--no-ff`.

## Fixed Branch Order

1. `codex/fix-lifecycle-callback-isolation`
2. `codex/chore-audit-package-markdown-cleanup`
3. `codex/refactor-smoke-fishroe-case`
4. `codex/api-diagnostics-snapshot-mod-status`
5. `codex/refactor-chestlocator-feature`
6. `codex/qa-camera-view-manual-play`
7. Final `Refactor` full/web audit package refresh and tag `refactor-stability-followup-20260610`

## Global Constraints

- Do not merge to `main` or `master`.
- Do not commit external package directories, package zips, report zips, complete evidence payloads, DLL/EXE/PDB artifacts, `bin`, `obj`, or `.tools`.
- Use local save slot 3 for game smoke unless a branch explicitly does not need game validation.
- Keep public API status conservative: diagnostics stays Diagnostic, gameplay features stay Experimental unless a separate review proves promotion gates.
- Update `docs/updates/INDEX.md` plus one update record for each non-trivial branch. Update hook map, smoke matrix, and API matrix when a branch changes hook/API/runtime evidence.
- If a required build/test/smoke fails, stop that branch before merging and record blocker facts.

## Branch Acceptance Gates

- Lifecycle isolation: isolate save/load/title/agent-state/fishing callbacks with diagnostics error logging; preserve hook IDs/status/smoke schema; run build/test plus AutoFishing, ActionSpeed, OneAction, InstantSave, and TitleButtonLifecycle smokes.
- Audit package Markdown cleanup: add controlled package generation/self-audit; reject hashtable interpolation and control-character path output; regenerate full/web packages without committing them.
- FishRoe smoke case: move only FishRoe smoke code into a case file; keep service behavior and result fields unchanged; run experimental hooks smoke.
- Diagnostics snapshot mod status: add Diagnostic `IDtmDiagnosticsSnapshot.Mods`/`IDtmModStatusInfo` while keeping `LoadedMods`; cover loaded/disabled/error mod rows in unit tests and Camera/ActionSpeed smoke.
- ChestLocator feature split: move `IChestLocatorEnhancerApi` registration into feature/service/hook bridge; preserve API/status/smoke behavior; run ChestLocator smoke.
- Camera QA gate: record manual-play checklist and keep CameraView Experimental; do not mark manual pass without user-supplied results.
- Final packages: regenerate full and web audit packages using the cleaned script; full keeps complete evidence/report, web keeps compact source/docs/key logs and states omissions.

