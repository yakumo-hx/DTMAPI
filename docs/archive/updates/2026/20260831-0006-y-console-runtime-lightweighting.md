# 20260831-0006：Y 键控制台运行轻量化

## Metadata

- Update ID: `20260831-0006`
- Date: `2026-08-31`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime, player`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: `debugconsole/productnative/performance/ui-lifecycle/catalog/reflection/input/native-spawn/technical-debt`
- Source: 用户要求在先提交现有工作树后，综合侧边审计分批修正 Y 键控制台运行消耗；关闭/重开采用保留 Canvas、结构和 30 cells，同时清除动态 payload、tooltip、文本和 sprite 引用的折中路线。玩家随后确认 `1.1.2` 手测未发现显著问题，要求将未完成项转入路线图，并把后续 Y-console fixture 固定为 UI 第十存档（原生索引 `9`）。

## Source Request

用户要求保持功能和可见语义，降低玩家绝大多数未开启 Y 键控制台时的内存占用，并同时降低一定程度的重开成本。修正按批次提交并执行必要测试。用户提供的长文本是其侧边对话审查总结，作为本项输入，不是外部文档指令。

在 `1.1.2` 候选进入上传目录后，用户进一步报告当前手测未发现显著问题；经官方
prefab/代码核对，截图中的巨大深色矩形是 `space_ship` 的世界空间船体 Tilemap，
不是控制台 UI。用户要求本 Update 以该玩家观察收口，把剩余维护项列入后续路线图，
并将后续所有 Y 键控制台测试迁移到 UI 第十存档（runner `-SaveSlot 10`、原生
archive 索引 `9`），绕开第三存档当前的饰品状态。

## Owning Review

- [Y 键控制台运行轻量化与折中生命周期审查](../../reviews/code/2026/20260831-0006-y-console-runtime-lightweighting-review.md)
- Input regression authority: [ISSUE-014](../../../debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md)

## Implementation Boundary

- Batch 1: cache stable RawInput/Screen/Unity/EventSystem reflection metadata, preserve physical input sampling and hot-plug semantics, and avoid unchanged-layout allocation and normal EventSystem scene scans.
- Batch 2: replace broad dirty boolean transitions with explicit regions; retain Canvas/structure/actual page cell pool across Close while releasing tooltip, dynamic payload/text/state and optional sprite cache; full rebuild only for geometry or language change.
- Batch 3: reduce catalog chrome/cell rebinding allocations and repeated full-catalog sorting/source projection without changing ordering, counts, availability or input actions.
- Batch 4: reduce Native reflection-key, movement fast-path, entity-diff and animal-position transient cost without changing snapshot/postcondition/partial-success semantics.
- Keep Product version, public API, package/release authority, current Harmony ownership and old Compatibility retirement outside this Update.

## Changed Files

- UI and input:
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleRawInput.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Actions.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Catalog.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Layout.cs`
  - `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Reflection.cs`
- Native actions:
  - `products/first-party/DebugConsole/src/Native/DebugConsoleMovementHooks.cs`
  - `products/first-party/DebugConsole/src/Native/DebugConsoleNativeAccess.cs`
  - `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Advanced.cs`
  - `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Core.cs`
  - `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Helpers.cs`
  - `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Spawn.cs`
- Tests and records:
  - `src/DTMAPI.GameBridge.DolocTown.QA/QaHostParticipant.cs`
  - `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/DolocTownGameBridge.G5Fixtures.cs`
  - `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/DebugConsoleActionFixtureAdapter.cs`
  - `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/QaScenarioController.cs`
  - `tests/DTMAPI.UnitTests/Program.cs`
  - `tests/DTMAPI.QaUnitTests/Program.cs`
  - `tools/scripts/run-game-smoke.ps1`
  - `tools/scripts/test-game-smoke-save-modes.ps1`
  - `tools/scripts/test-noqa-deadline.ps1`
  - `tools/scripts/candidate11-source-transaction.ps1`
  - `tools/scripts/test-candidate11-source-transaction.ps1`
  - `tools/scripts/README.md`
  - `AGENTS.md`
  - `PROJECT.md`
  - `docs/debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md`
  - `docs/reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md`
  - `docs/reviews/code/2026/20260831-0006-y-console-runtime-lightweighting-review.md`
  - `docs/updates/2026/20260831-0006-y-console-runtime-lightweighting.md`
  - `docs/updates/INDEX-2026-08.md`
  - `docs/debug/regressions/smoke-matrix.md`
  - `docs/debug/regressions/smoke-matrix-history-accepted-20260811.md` (size-bound router archive only; moved rows are unchanged)
  - `docs/debug/evidence-retention-allowlist.json` (mechanically regenerated after the runtime evidence references were added)
- External manual-test preparation:
  - `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole` now contains the exact validated `1.1.2` candidate plus its byte-preserved existing `workshop.json`.
  - The displaced `1.1.1` tree and a transaction receipt are retained under `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\.dtmapi-official-backups` for bounded rollback.

## Implementation Result

- Closed/default path: an ordinary closed frame requests no raw key state. Close hides the retained root, removes the temporary EventSystem, destroys catalog chrome plus world/advanced dynamic content, releases their listeners and input fields, destroys the tooltip, clears status/dynamic cell text, clears each cell's item/monster/animal DTO and sprite reference, and clears the sprite/source caches. The Canvas, panel/header, region hosts and exact current-page cell pool remain for reopen; a wide 1920x1080 layout retains 30 cells rather than the former fixed 48-cell pool.
- Open-frame path: legacy Input metadata, boxed keys and argument arrays are process-cached; one frame snapshot preserves legacy-first OR InputSystem behavior while `Keyboard.current` remains dynamically read for hot-plug. Screen reflection metadata and unchanged geometry results, common Unity reflection members/values, `EventSystem.current`, and unchanged active/layout state are reused.
- UI invalidation: the former broad dirty boolean is replaced by explicit Layout/Catalog/World/Advanced/Status regions. Non-wide tab switches reuse or lazily create retained regions and do not force a whole panel rebuild. Tooltip roots and catalog cell callbacks are reused while open; callbacks read the cell's replaceable current DTO and are bound once.
- Catalog/query path: source groups and stable inventory ordering are cached only across their existing invalidation boundary; item, monster and animal queries normalize search/source once and fill the requested page in one traversal. Monster/animal catalogs keep the same DTO ordering and refresh only dynamic availability instead of cloning the full array for every query.
- Native path: reflection caches now use structured Type/member/method keys rather than repeated assembly-qualified strings; common invoke arrays and exact native members are reused. Movement returns immediately for exact 1x before resolving the native owner. Spawn diffs use reference identity sets, and animal placement uses integer coordinate keys and imperative rings while retaining the reviewed radius/candidate order and final native postconditions.
- Runtime harness compatibility: the current QA source no longer implements the reduced `ITeleportDebugApi` directly against the installed Runtime `0.6.1` legacy vtable; it calls the retained teleport members through a concrete fixture facade and keeps retired CSV export non-requested. G5 world mutations wait for a continuous `Gameplay + archive/currentRoom/RoomInfo/agent` readiness window. MouseGive retains the read-only G4 lifecycle observer while the runner performs real clicks, projects the current first-cell layout into the Win32 client, and verifies the current ProductNative `item-give` receipt rather than the obsolete Bootstrap log shape. These are test-owner changes and do not alter candidate Product bytes or player-visible semantics.
- Deliberately unchanged: raw Y/Escape ownership and two-clean-frame Escape drain, dynamic player/room/EventSystem lookup, catalog order/count/availability, 1x/10x spawn requests, Old City Guardian `1 root -> 3 entities`, visible partial-success behavior, save semantics, Product/API/version and the 19 ProductNative Harmony patches.
- Future test fixture: the smoke runner now classifies all Y-console routes before shared-state work and requires explicit `-SaveSlot 10`; this includes staged DebugConsole UI/actions, external-player/no-QA gates, Y-console root-isolation profiles, save acceptance and DebugConsole owner deactivation. Other product routes keep their established fixture rules. Candidate11's no-QA contract and focused tests use the same value.

## Validation Plan

- Focused DebugConsole Product Unit and any new deterministic lifecycle/catalog/native tests.
- DebugConsole native trace and Product source/localization contracts.
- Author SDK validate/build/package and relevant netstandard2.0 builds.
- `tools/scripts/check-doc-governance.ps1` and `git diff --check`.
- The completed final bounded `NoNativeSave` route used the then-authoritative third-save disposable fixture. After player handoff, every future Y-console replay uses explicit `-SaveSlot 10` (UI tenth slot/native index `9`) while preserving those historical receipts unchanged.

## Validation

- PASS: focused `DTMAPI.UnitTests` with `DTMAPI_UNIT_TEST_FOCUS=debugconsole-product`, including the retained-shell/cell-pool, one-frame raw-input, dynamic `Keyboard.current`, cache and native-allocation source contracts.
- PASS: every project in `tools/scripts/build.ps1 -Configuration Release` compiled with `0` warnings and `0` errors. The subsequent complete Unit run reached the pre-existing unrelated `PreviewVersionMetadataIsConsistent` Catalog/source-versus-published-artifact assertion and stopped there; the same stale assertion was already recorded by Update `20260831-0001`, so it is not counted as a full-suite PASS.
- PASS when run independently after that stop: `DTMAPI.QaUnitTests`, `DTMAPI.InstallDoctor.Tests`, `DTMAPI.MultiPlatformInstaller.Tests`, and `DTMAPI.AuthorSdk.Tests` (the latter included its advanced-reference fixture).
- PASS: `tools/scripts/test-dtmapi-060-debugconsole-native-trace.ps1`; it retained 19 atomic Product hooks, 15 Compatibility transactions, exact Guardian composite recognition, visible partial success without rollback/circuit, unchanged UseTool/UseItem and movement-final-result policies.
- PASS: governed synthetic `doloctown-24456188-debugconsole-v1` reference fixture followed by Catalog `y-console` Author SDK validate/build/pack. Result: 51 validated project files, 25 compiled source files, `netstandard2.0`, no bundled native dependency, entry SHA-256 `28B91DE1CBD8F01308BC3162D988D22C2ABCDA0BE6E812A48358C810BF065600`, deterministic package SHA-256 `BA813525D912906A572B0875CB47E52364E4FD7470B2E9A6BEAA9F5ED4D937AF`.
- PASS: `tools/scripts/check-product-catalog.ps1` (`27` products, `11` public products, `22` Workshop items, `48` API rows), `tools/scripts/check-test-artifact-governance.ps1 -RunCleanupFixture`, evidence-retention allowlist regeneration/check, document governance, and `git diff --check`.
- PASS after a game path became available: the shared Runtime lock and an exact local-product transaction staged the same `28B91DE1...5600` candidate. `GAME-SMOKE/20260831-141146` passed the isolated G4 product-owned Y/Escape lifecycle: eight opens/eight closes, ten short Y taps, a 1.8-second hold without flicker, one Canvas initialization, screenshot, title cleanup, no-fatal and clean exit. The disposable third-save fixture passed archive/committed-sidecar immutability and was deleted.
- PASS: `GAME-SMOKE/20260831-143221` passed all six G5 action groups after the QA ABI/readiness corrections. Inventory, weather, teleport, time, movement and AdvancedDebug remained ProductNative. Old City Guardian `space_ship` returned `requestedRoots=1/succeededRoots=1/addedEntities=3` and `10/10/30`; no rollback, circuit breaker or batch-wide pseudo-transaction was present.
- PASS final combined route: `GAME-SMOKE/20260831-145440` passed real OS-level left-click give-1 and right-click give-10 on `old_pickaxe`, the complete Y/Escape/ten-tap/held-Y matrix, screenshot and title cleanup, all six G5 action groups, QA lifecycle/cleanup, no-fatal and clean process exit. The retired teleport CSV result was correctly `Skipped`. Player archives and committed sidecars were unchanged before cleanup, no archive writeback or routine save backup/restore occurred, and the disposable fixture/profile/product mirror were removed.
- PASS environment restoration: the exact pre-smoke local Y-console tree was restored (`tree=410ECB41...357A`; entry `784FD83E...E050`), `mod_infos.json` remained `3AA17BB7...D08C`, `workshop.json` remained `514C3829...FA6F`, the candidate transaction and disposable fixture were removed, the post-orphan-recovery sidecars still matched their captured hashes, no game process remained, and the shared Runtime lock was released.
- PASS manual-test upload-tree preparation after that restoration: under a fresh shared Runtime lock, a validated same-volume stage atomically replaced only `MODS/DTMAPI_YKeyConsole`. The prepared `1.1.2` tree has `27` files / `627499` bytes / retained-tree SHA-256 `D146F9AD...5882`, entry SHA-256 `28B91DE1...5600`, and byte-preserved Workshop control SHA-256 `514C3829...FA6F` for item `3742714442`. The displaced `1.1.1` tree remains exact at `.dtmapi-official-backups/DTMAPI_YKeyConsole-1.1.1-before-1.1.2-manual-20260831-154542326` (`27` files / `610335` bytes / tree `410ECB41...357A`), with the adjacent transaction receipt. `SAVE/mod_infos.json` stayed byte-identical at `3AA17BB7...D08C`; Local remains enabled at priority `10`, Workshop remains disabled at priority `-1`; the staging root is empty, no game process remained, and the lock was released. This prepared the local upload/manual-test source only: Codex did not submit to Steam, modify the subscription cache, launch the game, or touch a player save.
- PASS player-visible closeout: the user manually exercised the prepared `1.1.2` candidate and reported no significant issue. The supplied screenshot's large dark rectangle matches the official `space_ship` root prefab's `106 x 11` world-space hull Tilemap; it is the Old City Guardian composite body's main hull rather than Y-console UI. This accepts current visible generation/interaction only and does not claim Boss save/reload persistence or Steam publication.
- PASS focused future-fixture guards: PowerShell parsing, `-ValidateQaG4RoutingOnly`, `test-noqa-deadline.ps1`, and `test-game-smoke-save-modes.ps1` accept Y-console `SaveSlot=10` and reject the former `SaveSlot=3` before game/shared-runtime work. The isolated Candidate11 smoke-contract check likewise accepts `10` and rejects `3`; its complete fixture suite currently stops before those cases at the stale `9 source / 2 retained` Catalog-count assertion, so no full Candidate11 PASS is claimed here.
- PASS after the smoke-harness correction: PowerShell parse, `-ValidateQaG4RoutingOnly`, and `DTMAPI.QaUnitTests`. A repeated complete `DTMAPI.UnitTests` execution again stopped only at the pre-existing unrelated `PreviewVersionMetadataIsConsistent` Catalog/source-versus-published-artifact assertion; it is not reported as a full-suite PASS.
- NOTE on the first live-slot attempt `GAME-SMOKE/20260831-135626`: player archive bytes remained unchanged, but a pre-existing disabled MoreEquipmentSlots orphan journal legitimately recovered `straw_hat x1` and `grandmas_button x1` and advanced its committed sidecar generations. Because that was real owner/orphan recovery rather than Y-console behavior, subsequent acceptance used AutoCloud-isolated disposable fixtures. The post-recovery live sidecars remain byte-identical to the captured baseline (`3BDD7A5E...0952`, 2294 bytes; previous `F5C1EAAB...6C46`, 2295 bytes); the earlier generations were not fabricated or restored.

## Evidence

- Baseline existing workspace changes were committed first as `68e6e0ba`.
- Pre-implementation facts and rejected directions are frozen in the Owning Review.
- Batch commits:
  - `751fe24d` — open-frame input, screen and Unity reflection caches.
  - `a270b7ff` — retained lightweight closed shell and exact page-size cell pool.
  - `e994767d` — reusable catalog cells, callbacks, DTO/query snapshots and page accumulators.
  - `fef1446a` — structured native reflection caches and lower-churn movement/spawn/placement paths.
  - `a99ac008` — current Runtime QA ABI/readiness repair plus real-click lifecycle, coordinate and receipt hardening.
- Runtime evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260831-141146` — isolated G4 product UI/input lifecycle.
  - `docs/debug/evidence/GAME-SMOKE/20260831-143221` — isolated G5 ProductNative action matrix.
  - `docs/debug/evidence/GAME-SMOKE/20260831-145440` — final combined G4/G5 and real mouse give acceptance.
- Local ignored build receipt: `temp/y-console-runtime-lightweighting-20260831-final1/summary.json`; it is reproducible build output, not publication or runtime evidence.
- Local external preparation receipt: `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\.dtmapi-official-backups\DTMAPI_YKeyConsole-1.1.1-before-1.1.2-manual-20260831-154542326.receipt.json`; it records the exact preimage, candidate, Workshop-control and enablement-profile identities. It is manual-test/upload-folder evidence, not Steam publication evidence.

## Rollback Notes

Each implementation batch is a separate Git commit. Revert the affected batch rather than restoring the former broad dirty state piecemeal. Input sampling order, Escape drain, dynamic host checks and spawn postconditions are indivisible semantic boundaries during rollback. The QA/harness commit is independently revertible and does not change the packaged Product entry.

## Follow-Up

The current `1.1.2` behavior is accepted. The complete deferred checklist, the distinction between
already-retained Catalog state and still-recreated Catalog structure, and the three partially
optimized carry-over directions are normalized in
[Y 键控制台后续路线图归一化](../../../updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md).
That documentation-only successor owns later-roadmap wording; this Update remains the owner of the
implemented behavior and its runtime/player evidence.

Quantitative allocation/profiler measurement remains optional evidence for a later performance
task; the completed smoke and player acceptance prove behavior/lifecycle, not exact Mono/Unity
allocation deltas. Steam submission remains outside this Update and still requires explicit user
authority.
