# 20260711-0013 First-Party Zoom Owner Lifetime

## Metadata

- Update ID: `20260711-0013`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Source: user requested ZoomMod first-party productization after Owner Lifetime commit `b914983`; 2026-07-12 user manual QA supplied the product-operation follow-up.

## Scope

- move ZoomMod source ownership from `testmods/ZoomMod` to `first-party-mods/ZoomMod`;
- preserve every user-visible feature, public API dependency, config/i18n/default, version, ID, assembly/package name, official folder, and Workshop identity;
- keep ZoomMod an ordinary Abstractions-only `netstandard2.0` Mod;
- update build/install/release/publish wiring and architecture tests;
- validate the real Zoom owner, Camera lease, and same-process deactivate/restart-required boundary.

## Source Review

- `docs/reviews/code/2026/20260711-0002-zoom-first-party-owner-lifetime-audit.md`
- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`
- `docs/reviews/code/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`
- `docs/reviews/code/2026/20260707-0003-dtmapi-hotkey-rebuild-implementation-test-audit.md`
- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md`

## Safety Boundary

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The Camera native owner is already established in the cited reviews and is not changed here. ZoomMod remains only an ordinary consumer of the existing owner-bound `ICameraViewApi`; no Hook, Unity/reflection, native state, content format, or public API change is in scope.

## Product Invariants

- Unique ID: `DTMAPI.ZoomMod`
- Mod version: `0.4.2-dtmapi`
- source assembly / manifest entry: `ZoomMod.dll`
- packaged DLL: `DTMAPI.Zoom.dll`
- official local folder: `DTMAPI_Zoom`
- Workshop ID: `3742717440`
- target framework: `netstandard2.0`
- project dependency: `DTMAPI.Abstractions` only
- config, i18n, preview/icon, keybind defaults, CameraView request semantics, and optional ModConfigMenu behavior: unchanged

## Changed Files

- moved the complete tracked product tree from `testmods/ZoomMod` to `first-party-mods/ZoomMod`; `ModEntry.cs`, project/API target, manifest/config/i18n/assets, versions, IDs, dependency floors, source/package DLL names, official folder, and Workshop identity remain unchanged;
- updated `DTMAPI.sln`, `tools/scripts/build.ps1`, `tools/scripts/release-common.ps1`, canonical/companion publish metadata, the repository layout README, and product README for first-party source ownership;
- added a generic internal Camera-lease owner-deactivation smoke seam in Core plus `-AutoExerciseZoomOwnerLifetime` GameBridge/script orchestration. CameraPlayable completes first; the real loaded product owner then deactivates and the still-enabled source is reconciled without re-entry;
- added real built-assembly integration tests and architecture/metadata gates in `tests/DTMAPI.UnitTests/Program.cs`;
- updated this Update, its source Review/monthly ledger, the active smoke matrix, Camera API evidence, and ISSUE-010 owner-root evidence. No Hook Map entry changed because no Hook lifecycle/signature changed.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with zero warnings/errors; `DTMAPI.UnitTests: OK`.
- Real-product unit coverage passed Entry-once, three Event roots, demand-local Input watches with zero persistent keybind registrations, ConfigPage/API-facade/Content/Camera roots, SaveLoaded and ReturnedToTitle preservation/reset semantics, official disable to zero roots, stale Camera facade rejection, same-process restart-required/no re-entry, clean-runtime re-entry, and shutdown cleanup.
- Architecture/identity gates passed `netstandard2.0`, Abstractions-only, no Harmony/reflection/native/internal API, unchanged config/i18n/product metadata, first-party solution/build/release/publish paths, and Workshop item `3742717440`.
- Temporary Mods-only release staging passed as `DTMAPI-Zoom` with install folder identity `DTMAPI_Zoom`, package `DTMAPI.Zoom.dll`, normalized `info.json`, manifest, icon, preview, and both translation files. Source dependency floors remain `0.5.1-alpha`; the staged manifest correctly normalizes DTMAPI/Bridge/ConfigMenu floors to current `0.5.3-alpha` release-builder policy.
- PowerShell parsing passed for `tools/scripts/run-game-smoke.ps1`.
- Final `tools/scripts/test.ps1 -Configuration Release` passed with zero compiler warnings/errors and `DTMAPI.UnitTests: OK`; standalone document governance passed `3937` checks. PowerShell/JSON parsing and scoped/full `git diff --check` passed before commit.
- 2026-07-12 user manual QA passed Zoom in/out, hotkey modification, ordinary enlarged-view gameplay, building transition, map boundary, and return-to-title minimum-size restoration. The supplied farm screenshot shows the expected orthographic-only limitation: the illustrated background remains a smaller centered rectangle with gray uncovered space around it. The user accepts that visual difference as normal and reports no gameplay impact; it is evidence that background/fog/panorama synchronization remains absent, not that synchronization was implemented. Reload/reacquire was not separately stated.

## Runtime Evidence

- Shared runtime lock was acquired and released for both runs; neither left `DolocTown.exe`.
- Final third-save run `docs/debug/evidence/GAME-SMOKE/20260711-233720` used `-IncludeHookProbe -AutoExerciseZoom -AutoExerciseZoomOwnerLifetime -OfficialModProfile CoreOnly -OfficialModProfileExtraEnabledIds Local.DTMAPI_Zoom -TimeoutSeconds 360` and passed `RunStatus`, HookProbe, SaveLoaded, CameraPlayable/Zoom, ZoomOwnerLifetime, GameBridge cleanup/final health, no-fatal, normal exit, and official-profile restoration.
- The selected real product was `officialId=Local.DTMAPI_Zoom`, `source=OfficialLocal`, rooted in `MODS/DTMAPI_Zoom`; its completion log count was exactly one.
- CameraPlayable verified real 4x and fallback 2x arbitration for 30 seconds/31 samples each, restored the retained product lease to vanilla, and kept `nativeRefresh=not-called-playable` / UI scale unchanged.
- Real owner deactivation proved Core roots `19 -> 0`, Camera leases `1 -> 0`, Event `3`, current demand-local Input watches `2`, ConfigPage `1`, Content `8`, Registry/loaded/facade `3`, instance `1`, and GameBridge Camera participant `1` removed; `coreCleanupFailures=0`, `participantCleanupFailures=0`, authoritative `remaining=0`.
- The process-lifetime Camera provider remained registered. A same-process Workshop reconciliation of the still-enabled local source kept roots/leases at zero, retained `restartRequired=True` / `StatusCode=restart-required`, and did not execute Entry again.
- `GAME-SMOKE/20260711-233404` is retained as the first runtime pass; it exposed only an auxiliary result-summary timing classification for a pre-deactivation FinalHealth snapshot. The corrected final run `233720` supersedes that reporting attempt without changing runtime behavior.

## Rollback

Restore the ZoomMod project tree and path wiring to `testmods/ZoomMod`. Do not change the product IDs, package identity, or Camera API, and restart the game after any run which loaded the product assembly.

## Follow-Up

No API/version promotion. Existing manual evidence plus the 2026-07-12 first-party product follow-up pass scoped playable Zoom operation, hotkey modification, enlarged-view gameplay, building transition, map boundary, and title restoration. The observed background/gray-area coverage difference is an accepted product limitation caused by intentionally absent background/fog/panorama synchronization; reload/reacquire remains an unstated edge. `ICameraViewApi` therefore stays Experimental. The broader ISSUE-010 native/Mono crash class remains open; this bounded owner-root proof does not close it.
