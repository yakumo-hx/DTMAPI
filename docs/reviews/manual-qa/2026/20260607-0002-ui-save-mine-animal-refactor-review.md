# Manual QA Review: UI Save Mine Animal Refactor

## Review Header

- Time: 2026-06-07 03:05:16 +08:00
- Source: User manual QA feedback in this thread, four attached screenshots, local DTMAPI docs/code inspection, and read-only behavior comparison against the old DLK AnimalHusbandryProgress records.
- Scope: Review, durable record, and implementation goal conversion only. No runtime, hook, UI, mod, or package implementation was performed in this step.
- User constraints: "先记录存储起来。接下来要做底层重构。" Preserve the user's numbered issue order. Do not copy or imitate old DLKsmapi implementation; old DLK is behavior reference only.
- Related goal/update/debug records: `docs/goals/2026/20260606-0002-040-stable-custom-entity-apis.md`, `docs/updates/2026/20260606-0014-040-stable-custom-entity-apis.md`, `docs/reviews/manual-qa/2026/20260606-0004-031-regression-new-content-review.md`, `docs/debug/INDEX.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`.
- Files/docs inspected: `AGENTS.md`, `PROJECT.md`, `docs/workflows/codex-feedback-to-goal.md`, `docs/goals/README.md`, `docs/reviews/README.md`, `docs/reviews/templates/manual-qa-review.md`, `docs/updates/INDEX.md`, `docs/debug/INDEX.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`, `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`, `testmods/MineMod/ModEntry.cs`, `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\README.md`, `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\MAINTENANCE.md`, `E:\Python_project\DLK\docs\releases\DolocTownSMAPI-0.8.21-animals-api.md`.
- Not inspected: Live game state, generated logs for this exact report, and the current compiled binary behavior. Screenshot observations are user-provided manual QA evidence, not new smoke proof.

## Issue Review

### Issue 1: MoreSaves save UI overflow at 24 slots

Original feedback:

- 更多存档 mod：存档数量可以自定义，但是现在统一放在这个初始界面；12 个存档还可以，24 和存档 UI 就有问题了。
- 保留 12 存档（官方原生为 6 存档），更多存档给存档界面加入滑动菜单。

Screenshot/log transcription:

- 图一显示官方标题页的 `旅程记录` 存档界面。
- 可见槽位编号从 `#2` 到 `#24`，布局被直接扩展成多列多行。
- 左侧和右侧槽位内容被屏幕边缘裁切；`#5/#10/#15/#20` 等右侧槽位只有部分可见。
- 底部仍显示官方 `删除`、`复制`、`加载` 按钮，右下角语言区域与界面同时存在。

Review record:

- User-confirmed facts: 12 slots is acceptable; 24 slots breaks the save UI; more than 12 should use a scrollable save menu.
- Screenshot/log observations: The save panel has no visible scrollbar or viewport clipping; slots overflow horizontally and vertically instead of staying inside the initial menu.
- Code/doc facts inspected: `ISaveSlotsApi` currently raises `DolocAPI.gameManager.archiveFileCount`; `ComputeRequestedSaveSlotCount` allows enabled requests up to 60; hook status documents `DolocAPI.gameManager.archiveFileCount -> LocalSave.GetAllArchiveInfo -> GameDataPanel.Render`; hook map evidence only names `archiveFileCount=12`, `panelSlotCount=12`, and `renderedSlots=12`.
- Codex inference: The current design expands the official static save panel rather than adding a DTMAPI-owned scroll viewport. That is enough for 12, but 24 exposes the static layout limit.
- Ownership: GameBridge save-slot hook plus official title/save UI adaptation. Public `ISaveSlotsApi` may remain stable/experimental, but fragile UI work belongs in `DTMAPI.GameBridge.DolocTown` or the bootstrap UI host.
- Root-cause hypotheses: The official `GameDataPanel.Render` path lays out every slot at once; DTMAPI changes count but does not add scroll state, viewport masking, or page/window navigation.
- Rejected/unproven hypotheses: Not enough evidence that save files themselves are broken; the observed failure is UI containment/accessibility. No live load/delete/copy regression was tested in this review.
- Required downstream updates: Goal file, update record, hook map after implementation, smoke matrix with 12-slot and 24-slot save UI evidence.
- Acceptance checks: 6 vanilla slots remain normal, 12 DTMAPI slots remain normal, and 24 DTMAPI slots are reachable through a scrollable/paged menu without off-screen cards; load/delete/copy still target the selected slot correctly.
- Blocker conditions: If official save UI cannot be safely adapted without corrupting load/delete/copy behavior, implementation must stop and report the exact blocker instead of claiming completion.

### Issue 2: DTMAPI title button should match official pixel UI and be named 模组设置

Original feedback:

- DTMAPI 本体：游戏界面左上角 UI 要改动。
- 走官方像素文字、“开始旅程”、“简体中文”等按钮的 UI 形状，可以不放图片。
- 就叫模组设置。

Screenshot/log transcription:

- 图二显示标题页左上角一个 dark rectangular DTMAPI button。
- 按钮内左侧有白色方块图标区域，右侧文字为 `DTMAPI`。
- 当前样式不像官方 `开始旅程` 或 `简体中文` 按钮的像素文字/按钮形状。

Review record:

- User-confirmed facts: The button should be renamed to `模组设置`; image/icon is not required; visual shape and text should follow official title-page buttons.
- Screenshot/log observations: The current button is a dark rectangular custom overlay with icon and Latin label.
- Code/doc facts inspected: `ReflectedTitleMenuSettingsUi.CreateTitleButton` calls `CreateButton(..., "DTMAPI", ...)`, sets a 172x54 dark rectangle, and creates `DTMAPI.TitleSettings.Button.Icon` from `iconSprite`. The same file uses generic reflected Unity UI text/buttons rather than reusing an official button template.
- Codex inference: The title entry was built as an independent overlay, which made it functional but visually separate from official UI. The next implementation needs either official-style sprites/fonts where safely available or a close reflected-UI approximation without icon.
- Ownership: Bootstrap title UI host, not ordinary mods.
- Root-cause hypotheses: The existing title entry optimized for reliability on `HomePageUiState`, not official visual integration. It still logs/positions correctly but fails the user-facing style requirement.
- Rejected/unproven hypotheses: No evidence that the click/open behavior is broken. The reported issue is visual/name/style.
- Required downstream updates: Goal file, update record, and title UI screenshot evidence after implementation.
- Acceptance checks: On the title homepage, the entry reads `模组设置`, does not require an image, uses official-like pixel text/button proportions, opens the same DTMAPI settings menu, and disappears when the settings panel is open.
- Blocker conditions: If official font/sprite references cannot be resolved by reflection, implementation may use a close generated/reflected pixel-button style, but must document that fallback and still satisfy the player-visible label/shape.

### Issue 3: Mine mod pure-electric regression, config scrolling, mod-list truncation, and Mine scale lifecycle

Original feedback:

- 矿井 mod：过去版本要求去掉燃料消耗以及有关设置，改为纯电力消耗；现在似乎没有实现，或者只是配置菜单没有改。
- 当 mod 设置太多超过 UI，就会挤出去、不在 UI 内显示，必须加入滚动条。
- 严重怀疑左侧开启的 mod 数量也超过现在显示的数量，但是 UI 截断了。
- 曾经好像修复了但是又出现的问题：切换场景（进出房间、主要是出房间），会看到矿井贴图缩小到原本的大小。
- 图四也是反复提到的问题：手持矿井预览图贴图是原本的贴图大小。

Screenshot/log transcription:

- 图三显示 DTMAPI config menu。左侧 `已启用的 DTMAPI Mod` 列表包含多项 mod，当前选中 `DTMAPI 矿井`。
- 右侧 Mine config page 标题为 `DTMAPI 矿井 (DTMAPI.MineMod)`，可见 `保存`、`重置`、`取消`。
- Mine config visible options include `默认模式 electric`、`燃料容量 7200`、`燃料模式消耗 120`、`耗电模式燃料 20`、`每周期耗电 10`、`使用石油配方`、output weights 等。
- 右侧配置项延伸到面板底部之外，未见滚动条。
- 左侧 mod list 显示到 `一键完成（资源/加料已验证）`，未见更多项入口或滚动条。
- 图四显示手持/放置预览中的矿井半透明贴图尺寸仍为原始小尺寸，没有保持 2x visual scale。

Review record:

- User-confirmed facts: Mine must be pure-electric with no fuel consumption/settings; config page and enabled-mod list both need scroll; placed Mine shrinks during scene transitions; held placement preview is still original size.
- Screenshot/log observations: The Mine config menu still exposes fuel mode/capacity/cost and electric-fuel cost; the config page/list lack visible scroll controls; the placement preview is unscaled.
- Code/doc facts inspected: `testmods/MineMod/ModEntry.cs` registers fuel-related config options and sets `AllowFuelMode=true`, `AllowElectricMode=true`, `DefaultMode=config.DefaultMode`, `FuelCapacity`, `FuelOnlyFuelCostPerCycle`, and `ElectricModeFuelCostPerCycle`. `NormalizeConfig` preserves `DefaultMode`, clamps fuel capacity, and ensures electric mode still has fuel cost. `BuildStatusText` includes remaining fuel and fuel costs. `DolocTownExperimentalBridgeApi.NormalizeMachineDefinition` clamps fuel capacity to at least 1 and production still checks DTMAPI fuel before output when fuel cost is positive. `ReflectedTitleMenuSettingsUi.RenderConfig` renders only `Math.Min(15, pages.Length)` left-side pages and only `page.Items.Take(13)` on the right. Mine visual scale is applied during machine runtime observation and builder preview hooks exist, but the user-visible lifecycle reports show this is not sufficient.
- Codex inference: This is a combined API/mod/UI lifecycle regression. The Mine test mod still encodes hybrid mode from a previous requirement; ConfigMenu lacks scrolling by design; visual scaling likely happens after at least one render path or misses the held builder indicator path.
- Ownership: Mine test mod config/definition, ConfigMenu UI in bootstrap/Core, and GameBridge machine visual/electric runtime hooks.
- Root-cause hypotheses: Pure-electric requires both MineMod cleanup and GameBridge support for electric-only machines without a dummy fuel state; config overflow comes from hard-coded list/page caps; scene-shrink comes from scale being applied by runtime polling instead of every relevant render/reuse/room-transition path; preview shrink comes from builder preview identification or activation order not covering the actual held indicator.
- Rejected/unproven hypotheses: Not yet proven whether the scale flicker occurs for one frame only or persists until the production loop; not yet proven whether all enabled mods are loaded but hidden or whether ordering/filtering also contributes.
- Required downstream updates: Goal file, update record, debug notes for machine/config lifecycle if implementation touches hooks, hook map for Mine visual containment, smoke matrix for Mine production/config/scale/preview and ConfigMenu scrolling, API matrix if machine/config contracts change.
- Acceptance checks: Mine config no longer shows fuel mode/capacity/fuel-cost/electric-fuel settings; status/logs do not present fuel as a required resource; production consumes only official electric power at the configured/default 10 per cycle; too many config pages and too many options remain scrollable; Mine placed sprite and held preview stay 2x immediately after room transitions and while placing.
- Blocker conditions: If GameBridge cannot support electric-only machines without a legacy fuel state, this must be treated as a bottom-layer blocker and documented; final-state-only screenshots are not enough for the scene-transition shrink.

### Issue 4: AnimalHusbandryProgress still flickers from 心情 to hidden produce

Original feedback:

- 牧铃信息显示 mod 也是反复提到的问题：点击一个动物首先出现 `心情` 字样，然后变为对应的隐藏产物。
- 需要分析 DLKsmapi 老的牧铃信息显示怎么实现的（`E:\Python_project\DLK` 工作空间不同）、之前是否修改了。

Screenshot/log transcription:

- No new animal screenshot was attached in this turn. The user reports a visible text-order flicker: first the animal detail row shows `心情`, then it changes to hidden-produce information.

Review record:

- User-confirmed facts: The flicker is repeated and previously discussed; the first visible text is `心情`; later it changes to the intended hidden-produce display.
- Screenshot/log observations: No screenshot for this item in this turn; the visible symptom is described by the user.
- Code/doc facts inspected: Old DLK behavior reference shows the mod itself subscribed to `helper.Experimental.Animals.ViewerRendering`, selected the best `HusbandryProgress`, and called `AddProgressBar`; the old Runtime owned fragile mapping, reading `Animal.husbandryValues`, cloning/positioning bars, and a documented flicker fix. The old maintenance note says cached extension bars are hidden in an `AnimalViewer.OnShow` prefix, clones are instantiated inactive, title/progress/color are updated while inactive, then activated; it also says cloned `UILocalization` components can restore the mood title on first enable and must be stripped/reapplied. Current DTMAPI code maps `AnimalFullInfoData` to progress rows, has a legacy `ApplyAnimalProgressSinglePassData` method that writes `moodInfo`/`moodProgress`, and currently uses `AnimalViewerShowPrefix` plus `AnimalViewerShowPostfix` to render independent cloned progress bars. The inspected DTMAPI animal path did not show an equivalent `UILocalization` strip in the matched search.
- Codex inference: The old DLK lesson is not "copy old code"; it is that the Runtime must own an inactive-update-activate lifecycle and protect cloned bars from localization reapplying the native `心情` title. Current DTMAPI may still allow a visible tick between native mood render and postfix overlay render, or may let cloned localization reset the title on first enable.
- Ownership: GameBridge animal viewer hook/runtime UI lifecycle, plus AnimalHusbandryProgress mod only as the ordinary consumer.
- Root-cause hypotheses: Native `AnimalViewer.Show` writes mood UI after prefix and before postfix; the cloned progress bar may become active before its custom title is stable; cloned localization components may restore the native title; evidence capture may only prove final state, not the initial flicker.
- Rejected/unproven hypotheses: Not proven that the AnimalHusbandryProgress mod itself is wrong; old DLK records suggest the mod should stay event-driven and the Runtime should handle fragile UI details.
- Required downstream updates: Goal file, update record, hook map and smoke matrix after implementation; debug notes if the hook order or UI lifecycle changes.
- Acceptance checks: On a fresh launch and on repeated animal selections, the hidden-produce row is never visibly labeled `心情` before correction; the native mood row remains intact; the custom hidden-produce row is independent and prefilled before activation.
- Blocker conditions: If the flicker cannot be observed by automated screenshot timing, the implementation must still require a user-visible/manual or video/evidence gate instead of relying only on final screenshot text.

## Cross-Issue Summary

- Confirmed user facts: MoreSaves 24-slot UI breaks; title button should become official-style `模组设置`; Mine must be pure electric and needs config scrolling plus scale lifecycle fixes; AnimalHusbandryProgress still has a `心情` flicker.
- Screenshot/log facts: Save slots overflow the title save panel; DTMAPI title entry is a custom dark icon button; Mine config still exposes hybrid fuel/electric settings and overflows; Mine placement preview remains original size.
- Code-path findings: SaveSlots only raises archive count; title/config UI has hard-coded styling and page/item caps; MineMod still registers hybrid fuel/electric config and definition; machine runtime still has legacy fuel gating; Mine visual scale is not guaranteed on every render/preview lifecycle; animal overlay has the known old-DLK flicker pattern risk around active clones/localization/native mood timing.
- Risks: A narrow "make screenshot final state look right" fix would miss lifecycle flicker, scene-transition shrink, save UI operations, and stale persisted Mine fuel config.
- Suggested goal shape: One 0.4.1 bottom-layer refactor goal covering title/config UI, MoreSaves scroll UI, pure-electric Mine runtime/config, Mine visual lifecycle, and Animal viewer flicker. This should be implemented as DTMAPI platform work, not as isolated mod tweaks only.
- Items that should not be carried forward: Do not re-add unrelated DolocPlus research items, 0.4.0 custom entity API work, or older solved manual-QA issues unless the user reports a fresh regression.

## Readme/Goal Decision

- Update goal file: yes. The user explicitly requested review, record, and goal conversion before bottom-layer refactor.
- Generate short `/goal`: yes. The implementation handoff must use one exact goal file and sibling `.goal.txt`.
- Suggested task titles: Baseline/version, MoreSaves scrollable save UI, DTMAPI title/config menu UI refactor, Mine pure-electric runtime/config cleanup, Mine visual scale lifecycle, Animal viewer flicker root fix, validation/docs.
- Completion standard: The next implementation Codex may mark complete only after build, third-save game smoke, screenshot/log evidence for each player-visible failure mode, clean exit/no leftover process, and required docs updates.
