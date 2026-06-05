# DTMAPI 0.2.6 Mine + Y Console Manual QA Goal

This file is the detailed task ledger for the next implementation Codex. The short `/goal` prompt must point here instead of embedding all details.

## Manual Feedback Header

- Time: 2026-06-05 +08:00
- Source: user manual QA feedback with 4 screenshots after the 0.2.5 pass.
- Scope: only two areas for this round: MineMod and the Y key debug console. Do not expand this goal to other mods even if nearby problems are tempting.
- Current truth: 0.2.5 automated smoke evidence is useful history, but this 2026-06-05 manual QA is newer. Old smoke must not be used as proof that the issues below are fixed.
- Forbidden: do not modify unrelated mods; do not copy old DLKsmapi code; do not mutate third-party Workshop content; do not keep the Y-console `reload` feature exposed; do not use telemetry-only scale proof when the player-visible sprite is wrong; do not mark complete if only automated API calls pass while manual UI paths fail.

## Per-Issue Manual QA Review

### Issue 1: MineMod scaling pollution and duplicated research unlock

Original feedback:

- Mine initially appears as a normal icon after placement and then loads into an enlarged icon.
- When holding the Mine and preparing to place it, the placement preview animation is not enlarged.
- A more complex bug appears: many unrelated machines also randomly become enlarged. The user observed this on arbitrary equipment, chests, and indoor/outdoor objects including flower pots. The functional behavior does not change; only the sprites become enlarged. This is probably code pollution.
- In the research UI, the `点亮此节点可解锁制作` section shows Mine twice.
- The user suspects DTMAPI did not give this UI a way to choose one item instead of two.
- The current research-point cost is fixed at 30. Change the Mine unlock cost to 1 research point.
- Screenshot transcription: in an indoor container-like room, a central blue machine/producer sprite is visibly enlarged. Other equipment, chests, machines, and the Mine are nearby. Tooltip text shows `E 大木箱`.
- Screenshot transcription: the official `科技树` industrial tab is open. The selected Mine node appears to the right of `合金材料` and above `指挥官`. The right panel title is `矿井`; under `点亮此节点可解锁制作:` it lists two separate Mine entries with the same icon/description and recipe `石油 x 10, 钢锭 x 10`. The bottom unlock button says `缺少解锁点数` and shows a cost of `x30`.

Review record:

- User-confirmed facts: Mine placement preview is not scaled; placed Mine scale changes after load; unrelated equipment/chests/pots randomly scale up; Mine unlock appears twice in the official tech UI; Mine research cost should be 1, not 30.
- Screenshot observations: a non-Mine blue machine is visibly oversized; the tech UI shows duplicate Mine unlock entries and the cost icon/number indicates 30 points.
- Codex inference: this is a high-risk global renderer/prefab/Decorator scaling pollution bug. The implementation likely scales a shared asset, shared prefab, common equipment renderer, or cached decorator path instead of only a Mine-owned instance. The preview object likely uses a separate placement ghost/preview path that was never scoped. Duplicate research unlocks may come from registering both an equipment item and a recipe/output unlock, or from duplicate official JSON extension rows.
- Ownership: MineMod official content JSON; MineMod tech/recipe metadata; DTMAPI GameBridge equipment scale path; possible installer stale-content cleanup if old duplicated rows remain installed.
- Required updates: debug issue for Mine scale pollution; update record; smoke matrix Mine/new-content rows; hook map for machine/equipment scaling; API matrix only if a safer scale/preview API is added.
- Acceptance: third-save manual/smoke evidence proves Mine placement preview is 2x; placed Mine is 2x immediately without normal-size flash; unrelated machines, chests, and flower pots never become enlarged across repeated room loads, placement, save/load, and title return; official tech UI lists Mine exactly once; Mine research cost is 1 point.
- Blocker rule: if instance-scoped scaling cannot be made safe, remove/disable the 2x runtime scaling rather than allowing global equipment sprite pollution. Do not mark complete while any unrelated equipment can enlarge.

### Issue 2: Y console reload removal, localization, hover, weather layout, and search lifecycle

Original feedback:

- Trees appear in every room because the Y console provides a `重载` feature in addition to `存档`.
- Clicking `重载` saves and immediately loads. This currently has severe problems.
- The user's interpretation is that it was meant to act like loading the previous save point, but either the DTMAPI code is wrong or the official game does not support this kind of hot rollback.
- Completely remove the `重载` feature.
- Weather buttons should be uniformly smaller and arranged in one row.
- The settings UI shows `Chinese` as `schinese`.
- Weather names and teleport names are hard-coded Chinese and do not switch with language.
- Mouse hover still has not been implemented.
- The previous search-box issue probably still exists. First entering the save showed `石油`; after deleting it, entering/exiting the save did not show it; after typing `汉堡` and saving, entering/exiting did not show it; after restarting the game, `石油` appeared again.
- Screenshot transcription: a room contains many tree sprites/wooden trunk sprites in unnatural indoor positions, consistent with scene residue after the Y-console reload path. The HUD shows normal gameplay, and the hotbar contains tools, a red/black key-like item, bottled water, feather, bucket, and ingots.
- Screenshot transcription: the Y key console 0.2.5 is open. Top title says `Y键控制台 0.2.5`. Buttons include `下个时段`, `存这里`, and `重载`. Weather buttons are large and arranged as multiple rows: `雷雨`, `多云`, `晴天`, `大风`, `烈日`, `酸雨`, `雨天`. Teleport names are Chinese. Source/category lists include mixed Chinese and English/internal category text such as `construction_...`, `equipment_...`. The visible search box is empty in this screenshot, but the user reports `石油` reappears after game restart.

Review record:

- User-confirmed facts: the reload button causes severe room sprite contamination; reload should be removed entirely; weather UI needs a compact one-line layout; language display is wrong; weather/teleport names are not localized; hover is still missing; search text can persist across full game restart.
- Screenshot observations: the Y console still exposes `重载`; tree sprites appear in an indoor room; weather buttons take multiple rows; category labels include raw/internal English ids.
- Codex inference: hot reload/save-then-load is not safe in this game path and should be deleted, not polished. The tree contamination suggests native room objects are not being fully torn down before reload or DTMAPI is replaying scene/object state incorrectly. Search text reappearing after restart means the term is likely persisted in config, PlayerPrefs, UI input state, smoke/default query data, or a stale installed config file, not just held in current save memory.
- Ownership: DebugConsoleMod; DTMAPI Bootstrap reflected debug console UI; instant save/reload debug APIs; localization/translation helpers; weather/teleport debug APIs; installed config migration/cleanup.
- Required updates: debug issue for Y-console reload scene contamination; update record; smoke matrix `DEBUGCONSOLE`, `SAVE`, `WEATHER`, `TELEPORT`; hook map for instant save/reload removal; API matrix if reload API is removed/deprecated or localization DTOs change.
- Acceptance: Y console has no `重载` button or command; no code path performs save-then-immediate-load from the player console; repeated room entry, save, title return, and restart do not create tree/equipment sprite contamination; weather buttons are one compact row; language UI shows user-facing names such as `中文`/`English` rather than `schinese`; weather and teleport labels switch with language; item hover appears near the mouse/item with name, tags, source, and give eligibility; after full game restart, first open in a save has an empty search box.
- Blocker rule: if official hot reload is unsafe, keep it removed permanently. Do not leave an experimental reload entry in player UI. If search state comes from old persisted config, add a migration/cleanup and prove `石油` no longer returns after restart.

## Problem Grouping

- MineMod visual contamination: instance scale is leaking into unrelated equipment/objects.
- MineMod official research polish: duplicate unlock output and wrong point cost.
- Y console unsafe feature: `重载` causes scene/object residue and must be removed.
- Y console product UI: compact weather row, localized language/weather/teleport labels, working hover.
- Y console lifecycle/persistence: search state must reset across full game restart and save-session boundaries.

## Boundary Constraints

Must do:

- Keep this goal limited to MineMod and Y console.
- Fix or remove unsafe Mine runtime scaling so unrelated equipment never enlarges.
- Include placement preview scale, not just placed equipment scale.
- Remove Y-console `reload` entirely.
- Clear any persisted stale search text such as `石油` on startup/save-load.
- Verify with third-save in-game behavior and screenshots/logs.

Must not do:

- Do not work on SecondMotor, AutoFishing, ActionSpeed, AnimalHusbandryProgress, MoreEquipmentSlots, or Oil except where Mine recipe/content references Oil.
- Do not use DTMAPI custom research UI for Mine.
- Do not rely on telemetry-only scale proof.
- Do not keep reload as hidden/experimental UI.
- Do not hard-code Chinese labels for weather or teleport.

Blocker conditions:

- If Mine 2x sprite cannot be scoped safely to Mine-only instances and previews, disable 2x scaling and report the blocker rather than polluting equipment globally.
- If the game cannot safely hot-load a save from within a running save, remove the reload API/UI and report the official limitation.

## Next Tasks A-G

### Task A: Baseline 0.2.6 focused manual QA and bump version

- Run `git status` first and preserve user/previous-Codex changes.
- Treat this 2026-06-05 manual QA as newer than 0.2.5 smoke evidence.
- Bump DTMAPI once, preferably from `0.2.5` to `0.2.6`, and update controlled version sources, UI title/version text, package metadata, and update record.
- Record this as a focused two-area follow-up: MineMod and Y console only.

### Task B: Mine scale containment and preview scale

- Audit all Mine scaling code and remove any shared prefab/asset/global renderer mutation.
- Make scaling instance-scoped to `dtmapi_mine` only.
- Add placement preview/ghost scaling for the Mine.
- Add cleanup/migration for stale globally scaled objects if possible.
- Verify unrelated equipment, chests, flower pots, and indoor/outdoor machines do not enlarge across repeated room loads and saves.

### Task C: Mine official research duplicate and cost

- Remove duplicate Mine unlock output in the official tech UI.
- Keep Mine under the official Industrial tech node route.
- Set Mine research cost to 1 point.
- Verify the right panel lists Mine once and the unlock button cost is 1.

### Task D: Remove Y-console reload and prevent scene residue

- Remove the `重载` button and any player-visible reload command.
- Remove or disable save-then-immediate-load execution from the Y console.
- Keep `存这里` if safe, but do not pair it with immediate reload.
- Verify room/tree/equipment residue no longer appears after save, title return, restart, and room switching.

### Task E: Y-console localization and compact weather row

- Weather buttons must be smaller and fit on one row.
- Language display must use player-facing names, not `schinese`.
- Weather names and teleport names must localize with current language.
- Avoid raw/internal category names where a localized display name is available.

### Task F: Y-console hover and search reset

- Implement real item hover near the mouse/item with name, tags, source, and give eligibility.
- Search text must be empty on first save entry and after full game restart.
- Same-save reopen may preserve the user's search.
- Add migration/cleanup for stale persisted search values such as `石油`.

### Task G: Verification and documentation closeout

- Release build and unit tests pass with 0 errors.
- Third-save game smoke is required.
- Manual/visual evidence is required for Mine scaling, non-Mine non-scaling, research duplicate/cost, removed reload, weather row, localization, hover, and search reset after restart.
- Exit check must show no leftover `DolocTown.exe` and no Steam waiting-for-exit regression.
- Update `docs/updates`, `docs/debug`, smoke matrix, hook map, and API matrix.

## Completion Standard

Only mark complete when Tasks A-G are player-visible and genuinely usable in game, with build, third-save smoke, restart/search evidence, Mine preview/placed/non-Mine scale evidence, official research UI evidence, Y-console UI evidence, exit cleanup, and documentation all complete.

Do not use these as substitutes for completion:

- old 0.2.5 smoke evidence;
- telemetry that says Mine scale is 2 while the preview is not scaled;
- screenshot of one placed Mine without checking other equipment;
- keeping reload hidden behind a different name;
- hard-coded Chinese labels;
- API-only hover/search tests that bypass the real Y-console UI.
