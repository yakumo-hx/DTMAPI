# Manual QA Review: Title UI, MoreSaves 12, AutoFishing Regression

## Review Header

- Time: 2026-06-12 +08:00
- Source: User manual QA feedback in this thread plus screenshot `C:/Users/ADMINI~1/AppData/Local/Temp/codex-clipboard-73e12657-916e-4a06-a83b-88a38e215149.png`; local code/docs inspection.
- Scope: Pre-implementation manual QA review and implementation handoff for the current `codex/bottom-layer-refactor-audit-20260612` worktree.
- User constraints: Keep existing bottom-layer refactor changes; do not revert DTMAPI config menu paging; fix only the reported title UI, MoreSaves fixed slot count, and AutoFishing single-feature/combination behavior.
- Review record: `docs/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md`.
- Related records: `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`, `docs/updates/2026/20260612-0012-bottom-layer-refactor-audit-implementation.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`.

## Issue Review

### 1. Title DTMAPI icon disappeared

Original feedback:

- "改完后的 dtmapi 左上角小图片没有了"
- Screenshot transcription: the title homepage shows a large dark top-left text button labeled `模组设置`; the prior small DTMAPI icon is not visible.

Review record:

- User-confirmed facts: The intended small top-left DTMAPI icon button is missing after the bottom-layer refactor.
- Screenshot/log observations: The attached title screenshot shows only the large text-only `模组设置` rectangle at top left.
- Code/doc facts inspected: `ReflectedTitleMenuSettingsUi.CreateTitleButton()` now creates a text button with `T("ui.titleButton", "模组设置")`. `LoadIconSprite()` and `iconCandidates` still exist, but no field stores the sprite and `LoadIconSprite()` is not called during initialization.
- Codex inference: This is a regression introduced by the title settings paging refactor, not an asset packaging failure by itself. The asset path can still be loaded if the UI host restores sprite loading.
- Ownership: `DTMAPI.BepInExBootstrap` reflected title UI host.
- Required updates: Bootstrap UI code, title smoke/screenshot evidence, update record, smoke matrix, hook map.
- Acceptance checks: Title homepage shows a compact top-left DTMAPI icon button using `assets/branding/dtmapi-icon.png`; if the icon cannot load, fallback is compact `DTMAPI` text, not the large `模组设置` button.
- Blocker conditions: If the icon asset is absent from both repo and installed runtime paths, report the missing asset path and do not claim icon restoration.

### 2. Official title menu became two columns

Original feedback:

- "下方变成两列了而不是正常单列"
- Screenshot transcription: the official lower title menu is displayed as two columns: `开启旅程` beside `游戏设置`, `模组` beside `关于`, with `退出` below, instead of the normal single vertical list.

Review record:

- User-confirmed facts: The official homepage menu layout changed from single-column to two-column after the refactor.
- Screenshot/log observations: The attached screenshot visibly shows the official title menu buttons arranged in two columns.
- Code/doc facts inspected: Bottom-layer refactor added paging/layout work for DTMAPI's reflected title UI, while native `HomePageTextMenu` should remain a `DolocVerticalUI` style single-column text menu. No public API should own this native title layout.
- Codex inference: A reflected/layout constraint side effect likely left the active `HomePageTextMenu` layout group in multi-column mode. The fix should guard only the title-homepage text menu while `HomePageUiState` is active.
- Ownership: Bootstrap UI host guard over native title-homepage layout; not ConfigMenu API and not ordinary mods.
- Required updates: Bootstrap UI code, title smoke/screenshot evidence, hook map, smoke matrix, update record.
- Acceptance checks: On `HomePageUiState`, the official homepage menu is single-column while DTMAPI config menu paging still works after opening the DTMAPI panel.
- Blocker conditions: Do not apply broad global layout resets that affect save UI, config menu paging, or other official panels.

### 3. MoreSaves extra slot count did not take effect; fixed 12 total slots

Original feedback:

- "存档额外数量并没有生效。我怀疑这部分改坏了。"
- "现在我需要固定额外存档数量为12。不给调整。"

Review record:

- User-confirmed facts: The configurable extra save-slot count is not acceptable now; the desired player-facing behavior is fixed 12 total official save slots and no slot-count setting.
- Screenshot/log observations: The screenshot is on the title page and does not show the save UI; this issue is user manual feedback.
- Code/doc facts inspected: `MoreSavesMod` still exposes `SlotCount` as a number option from 6 to 60 and passes `config.SlotCount` to `ISaveSlotsApi`. `SaveSlotsService.NormalizeSaveSlotsOptions()` clamps 6-60 and `ComputeRequestedSaveSlotCount()` takes the max enabled request. The bottom-layer branch added 18+/24 paging, but the new requirement supersedes that configurability for MoreSaves.
- Codex inference: The lowest-risk fix is to keep `SaveSlotsOptions.SlotCount` for experimental compatibility but normalize enabled runtime requests to 12 total slots; disabled remains vanilla 6. MoreSaves should ignore stale persisted `SlotCount` and remove the UI option.
- Ownership: `MoreSavesMod` config surface plus GameBridge SaveSlots policy normalization.
- Required updates: MoreSaves code/i18n/docs, SaveSlots service/tests/smoke, API matrix, hook map, smoke matrix, update record.
- Acceptance checks: With MoreSaves enabled, `archiveFileCount=12`, rendered official save slots are at least 12, no SaveSlots pager is active, and an extra slot can be selected/loaded through the official save UI. With disabled policy, requested/applied behavior returns to vanilla 6.
- Blocker conditions: Do not implement `6 + 12` or expose a player setting unless the user explicitly changes the requirement.

### 4. DTMAPI config menu paging is accepted

Original feedback:

- "dtmapi配置菜单的翻页应该是没问题。"

Review record:

- User-confirmed facts: DTMAPI's own config menu paging should be preserved.
- Screenshot/log observations: The screenshot does not show the opened DTMAPI config panel; user explicitly says the paging itself appears okay.
- Code/doc facts inspected: `ReflectedTitleMenuSettingsUi` now has internal paging for enabled mod list, config items, and Manager list pages.
- Codex inference: The fix must avoid reverting config/Manager paging while restoring the compact title button and guarding only the official homepage layout.
- Ownership: Bootstrap UI host.
- Required updates: Title UI smoke should include both icon/single-column title menu and DTMAPI config menu still opening.
- Acceptance checks: Opening the DTMAPI settings panel still reaches long config pages with pager controls after the title icon/menu fix.
- Blocker conditions: A fix that restores title layout by removing DTMAPI config paging is incomplete.

### 5. AutoFishing single features and combinations

Original feedback:

- "然后再修复钓鱼的问题。对单个功能进行修改。再确保组合是成功的。"
- Previous latest manual QA review already recorded: `SkipMiniGame` alone ineffective, `AutoCompleteMiniGame` plus skip ineffective, fast reel/cast acceleration ineffective, and non-instant native bite/reel behavior not matching visible option semantics.

Review record:

- User-confirmed facts: AutoFishing must be validated per visible sub-feature first, then as combinations.
- Screenshot/log observations: No new fishing screenshot attached in this turn.
- Code/doc facts inspected: `ApplyFishingWaitAutomation()` returns unless `_waitForFishBite` is true and `InstantBite` is enabled, then immediately calls `TryAdvanceFishingBite()`, so `InstantBite` currently owns both bite timing and skip/auto-hook routing. `TryApplyFishingMiniGameAutomation()` returns when `SkipMiniGame` is true. `TryApplyFishingAnimationSpeed()` returns silently if no animator path changes. AutoFishing config text currently describes skip as not independent and fast animations as may no-op.
- Codex inference: The service needs separate "bite-ready" and "post-bite routing" decisions. `InstantBite` should only prepare the bite. `SkipMiniGame` should route any bite-ready state to Pull and should win over auto-complete. `AutoCompleteMiniGame` should route a bite-ready fish into the real battle UI and force native success after the visible delay. Fast animation should publish verified or pending/failed smoke status instead of silent success.
- Ownership: GameBridge FishingAutomation service/smoke support plus AutoFishingMod config text.
- Required updates: Fishing service, AutoFishing config/i18n/README/docs, unit coverage for option precedence, smoke scenarios, API matrix, hook map, smoke matrix, update record.
- Acceptance checks: `AutoCastOnly`, `InstantBiteOnly`, `SkipOnly`, `AutoCompleteOnly`, `FastAnimationsOnly`, `CombinedSkip`, and `CombinedComplete` each have explicit smoke/log evidence. `SkipMiniGame=true` wins over minigame completion so no battle UI should open in that combination.
- Blocker conditions: Do not mark complete from the old combined AutoFishing smoke only; each single-feature scenario needs its own evidence or an explicit blocker.

## Cross-Issue Summary

- Confirmed user facts: The title DTMAPI icon regressed to a large text button; the official title menu is two-column; MoreSaves should be fixed at 12 total slots with no player adjustment; DTMAPI config menu paging should remain; AutoFishing sub-features need independent behavior and combined validation.
- Code-path findings: Title sprite load is unused; MoreSaves and SaveSlots still expose/accept variable slot counts; FishingAutomation currently couples instant bite and post-bite routing and lets fast-animation no-op silently.
- Risk: UI fixes can accidentally affect unrelated official panels; save-slot normalization can invalidate older 18+/24 smoke expectations; AutoFishing smoke needs scenario awareness to avoid proving only the old combined path.
- Suggested implementation goal: `docs/goals/2026/20260612-0004-title-saveslots-autofishing-fix.md`.
