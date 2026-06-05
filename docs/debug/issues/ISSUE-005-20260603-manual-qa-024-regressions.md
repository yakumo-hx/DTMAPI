# ISSUE-005: 2026-06-03 0.2.4 manual QA regressions

## Current Status

- Status: open
- Opened: 2026-06-03 +08:00
- Target: DTMAPI 0.2.4
- Source: user manual QA feedback summarized in `readme.md`
- Important boundary: older 0.2.3 smoke evidence remains historical only. It must not be cited as proof that the regressions below are solved.

## Known Facts Before Changes

- The worktree already contains the prior 0.2.3 productization pass, including smoke evidence for AnimalHusbandryProgress, AutoFishing, ActionSpeed, Y console, and SecondMotor.
- The user reported new manual regressions after that evidence. Treat this issue as newer than all 0.2.3 smoke rows.
- Required validation is third-save real gameplay, logs/screenshots where applicable, and no leftover `DolocTown.exe`.

## Manual QA Items

### 1. Animal hidden-produce UI and custom color input

- User-confirmed facts: the final `+` color swatch does not show a custom color input; switching animals still briefly shows `心情`; the hidden product row text is too small and should return to the larger previous style.
- Screenshot observation: config page `牧铃隐藏产物进度 (DTMAPI)` shows swatches including a final `+`, but no adjacent editable color input. Animal panel shows native rows `饱食`, `心情`, and a hidden product row such as `沼泽兽的角 30/120`.
- Initial hypothesis: config visibility for the custom hex input is not tied tightly enough to the selected custom preset; animal viewer row replacement still activates after the native mood row is visible.
- Acceptance: in third save, switching animals does not flash `心情` before the hidden product row; hidden product text uses the larger preferred size; selecting `+` shows editable color input, while ordinary presets hide it.

### 2. AutoFishing and ActionSpeed secondary UI plus fishing behavior

- User-confirmed facts: AutoFishing inline secondary UI is misaligned; ActionSpeed auto-fill secondary UI has the same problem; `自动完成小游戏=on` with `跳过小游戏=off` still skips the mini-game; fishing animation speed must affect cast and pull/reel animations.
- Screenshot observation: AutoFishing page shows `自动完成小游戏 开` with inline `跳过小游戏 关`, but the row is visually misaligned.
- Initial hypothesis: the config row renderer needs a stricter inline bool/bool layout, and AutoFishing still routes auto-complete through the skip path instead of separating mini-game success from skip.
- Acceptance: third-save evidence shows auto-complete without skip does not skip the mini-game, skip=true does skip it, cast and pull/reel animation speed visibly/logically changes, and both AutoFishing and ActionSpeed inline rows are aligned.

### 3. Y console save action and teleport audit

- User-confirmed facts: instant save anywhere is not discoverable; it should move into the Y console; teleport destinations need review; current destinations must be exported to CSV for manual user screening.
- Initial hypothesis: the existing smoke-only instant-save API needs a player-visible debug-console action, and teleport metadata needs a source-aware export tool rather than only UI label changes.
- Acceptance: in third save, Y console exposes an instant-save action that calls native save safely from the current location and logs the result; a CSV exists with internal id, map/room, coordinates, current display name, suggested display name, category, and source.

### 4. SecondMotor texture, map transition, dual state, and summon animation

- User-confirmed facts: earlier fixes still left texture/lifecycle regressions; the official example texture was lost and replaced by a recolored original; riding the mod motor across map boundaries can cause a stuck state; the original motor can appear at the new map entry when riding the mod motor; summon animation is direct spawn rather than original-style fly-to-player.
- Screenshot observation: two flying motors are visible in a rainy city scene, but visual identity and placement do not match the requested final behavior.
- Initial hypothesis: DTMAPI has a visible clone path, but the native motor singleton and transition snapshots are still leaking state between original and second motor.
- Acceptance: third-save evidence proves original and second motors keep independent textures/lifecycle, map transitions do not stick the player, the original motor does not follow to the new map entry when riding the second motor, and the second motor summon animation follows the native fly-to-player feel as closely as safely possible.

## Rejected Shortcuts

- Do not mark this issue solved from build-only evidence.
- Do not use 0.2.3 smoke evidence as the final proof for these newly reported regressions.
- Do not copy old DLKsmapi or DLK_SecondMotor code.
- Do not globally replace official motor assets or mutate third-party/Workshop content files.

## Attempts

### 2026-06-03 / DTMAPI 0.2.4 baseline

- Change: recorded this issue and bumped project-controlled runtime/package version sources from 0.2.3 to 0.2.4.
- Evidence: pending build and third-save smoke.
- Result: implementation still pending; this is only the regression ledger and version baseline.
- Next action: implement the smallest useful fixes for the four manual QA items, then add Oil/Mine/More Equipment Slots work with separate hook/API records.

### 2026-06-03 / DTMAPI 0.2.4 targeted regression evidence

- Change: AnimalHusbandryProgress now stages pending config values before conditional visibility is evaluated, the hidden-produce overlay is patched before/after native viewer refresh, and hidden produce text is rendered at the larger fixed size. The title smoke captures both ordinary-preset and staged Custom color states.
- Evidence: Release build/unit passed after the changes. Title smoke `docs/debug/evidence/GAME-SMOKE/20260603-160739` passed with screenshots `DTMAPI-evidence/UI-004/20260603-160812/title-settings-config-animal-husbandry-progress.png` and `title-settings-config-animal-husbandry-progress-custom.png`; the first hides direct hex editing for an ordinary preset, and the second shows `填充颜色` with `F0F0F0` after staging the `+`/Custom preset. Third-save animal smoke `docs/debug/evidence/GAME-SMOKE/20260603-152851` passed and logged `Animal viewer UI evidence OK ... overlay=rows=1, 羊毛脂 0/100`; `process-check.txt` says no `DolocTown.exe`.
- Result: item 1 is implemented and smoke-evidenced for the current automated coverage. Manual repeated animal switching remains a useful follow-up, but no stale `心情` row was captured in the fresh 0.2.4 animal screenshot.

- Change: AutoFishing skip/minigame and ActionSpeed inline/config paths were split. The config smoke now understands inline bool/number rows, AutoFishing mini-game update is a separate hook from skip-to-pull, and fishing cast/pull animation speed includes the rod renderer animator.
- Evidence: Release build/unit passed. ActionSpeed config smoke `docs/debug/evidence/GAME-SMOKE/20260603-154207` logged `Smoke exercise ActionSpeedConfigApply OK` with multiplier `2 -> 4`. ActionSpeed interaction smoke `docs/debug/evidence/GAME-SMOKE/20260603-154721` passed and logged `Smoke exercise ActionSpeedInteraction OK`, including `BottleFill`, in-water fill, and `AutoFillBottle`. AutoFishing smoke `docs/debug/evidence/GAME-SMOKE/20260603-154816` passed and logged `Smoke.AutoFishingMovementCancel = verified`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingAnimationSpeed = experimental ... phase=Pull multiplier=3`, `Smoke.AutoFishingPhase = verified`, and `Smoke.AutoFishingMiniGameSkip = verified`. Title smoke `docs/debug/evidence/GAME-SMOKE/20260603-160739` captured ActionSpeed and AutoFishing config pages with aligned inline rows.
- Follow-up evidence: first direct skip=false smoke `docs/debug/evidence/GAME-SMOKE/20260603-172124` failed after entering real `AgentStateFishingWait` because the roll produced `waste_plastic_bottle` (`FishProto.IsFish=false`), so the original game correctly did not create `FishingGameScrollBar`. After adding a smoke-only force-fish gate, `docs/debug/evidence/GAME-SMOKE/20260603-173435` passed with `AutoFishingMiniGameComplete=true`; logs show `Fishing phase hook observed phase=MiniGame source=FishingGameScrollBar`, `autoHook=AgentStateFishingBattle`, `fish=loach`, `isFish=True`, `forceFishForSmoke=True`, `forceFishSatisfied=True`, `rollAttempts=1`, `Fishing automation completed minigame ... currentGameStatus=Success visibleSeconds=0.76`, and no leftover `DolocTown.exe`.
- Result: item 2 is implemented for skip=true, skip=false minigame auto-complete, runtime animation coverage, and title config alignment. Manual visual fishing remains useful, but the third-save smoke now proves `AutoCompleteMiniGame=true` with `SkipMiniGame=false` reaches the native minigame instead of the skip-to-pull path.

- Change: the Y console now binds an instant-save API and exposes teleport CSV export. Teleport destination DTOs include suggested display name and source metadata, and export writes a CSV under DTMAPI evidence.
- Evidence: third-save smoke `docs/debug/evidence/GAME-SMOKE/20260603-152451` passed with `AutoExerciseInstantSave=True`, `AutoExerciseDebugTeleport=True`, and `AutoExerciseNewContentApis=True`. Logs show `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, limitation=none`, `Smoke exercise DebugTeleportCsv OK rows=80 path=...TELEPORT-DESTINATIONS\20260603-152541\teleport-destinations.csv`, and `Smoke exercise DebugTeleport OK ... changedRoom=True`. `process-check.txt` says no `DolocTown.exe`.
- Result: item 3 is implemented and smoke-evidenced. The CSV is ready for manual destination name screening.

- Change: SecondMotor no longer installs global official vehicle sprite replacements, keeps the original motor snapshot separate while riding the DTMAPI clone, redirects native `SetMotorPosition` during second-motor rides, and uses a clone/tint path with native-style summon attempts where available.
- Evidence: third-save vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260603-155505` passed. Logs show `dualVisible=True`, `appearanceIsolated=True`, `originalScopedTint=0/10`, `secondScopedTint=5/10`, `Second motor ride-on observed`, `Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot`, original summon OK, `speedMultiplier=2`, `baseMaxSpeed=25`, and `effectiveMaxSpeed=50`; `process-check.txt` says no `DolocTown.exe`.
- Follow-up change: GameBridge now patches `DolocAPI.EnterRoom`, captures second-motor pending room targets, synchronizes the clone/body/AgentPosition proxy through map transitions, restores the original archive snapshot, and reads original motor unlock state from archive data instead of a reflected extension-method call.
- Follow-up evidence: release build/unit passed. Third-save vehicle smoke `docs/debug/evidence/GAME-SMOKE/20260603-202948` passed with `VehicleSecondMotor=True`, `ProcessExited=True`, and no fatal popup. Logs show `Vehicle.SecondMotorMapTransition` target captured/applied, then `Smoke exercise VehicleSecondMotor edge-transition OK ... changedRoom=True ... secondInCurrentRoom=True ... distanceToDestination=0.839, nearDestination=True, originalRoomAfterTransition=farm_type1-平地, originalVisibleAfterTransition=False, originalAtNewEntry=False ... noStuck=True`; `process-check.txt` says no `DolocTown.exe`.
- Result: item 4 is implemented for dual-visible appearance isolation, ride/dismount, original restore, and automated third-save edge transition. Manual visual map-boundary crossing remains useful user-acceptance evidence, but it is no longer the blocking automated smoke gap.

### 2026-06-03 / New content/API status

- Oil: official JSON item/package now validates as a native item, uses valid `$type=ItemFunction`, and has direct third-save metadata plus coal-drop evidence. Evidence `docs/debug/evidence/GAME-SMOKE/20260603-190919` logs `OilMod content item=dtmapi_oil fuelEnergy=1500 officialJson=item_tbitem.json`, `Smoke exercise NewContentOilItemMetadata OK ... sourceKind=DTMAPI, sourceId=Local.DTMAPI_Oil, category=material_ore, categoryListed=True, salable=True, sellingPrice=45, buyingPrice=240, fuelEnergy=1500, baseHighestFuel=pumpkin:1200, indexedIcon=icon_item_coal, title=石油`, then `Smoke exercise NewContentOilCoalDrop OK ... resource=coal_mine ... oilDrop=dtmapi_oil ... placement={Placed dtmapi_oil x1 through native backpack placement.}`. This proves official JSON load, Y-console-facing source/category/search metadata, sale/icon/localization/fuel metadata, and coal-resource mining drop through the native hit/backpack path.
- Mine: official JSON item/equipment/recipe package exists, `IMachineProductionApi` registers `dtmapi.mine`, and the runtime loop now has third-save official JSON, player placement, screenshot, and placed-production evidence. Smoke `docs/debug/evidence/GAME-SMOKE/20260603-210216` logs `Smoke exercise NewContentMineOfficialJson OK ... item=矿井 ... generatedItemType=DolocTown.ItemEquipment ... cover=8x6, baseWellCover=4x3, sceneAsset=sprite_equipment_well ... recipeInputs=stonex80|iron_ingotx8|electric_wirex4|dtmapi_oilx5 ... recipeGroup=equipment_workbench includes=True ... visualScale:2/fuel:0-7200/cycleMinutes:120/costs:120-20-10`, then creates a temporary `dtmapi_mine` outdoors and logs `Mine placement evidence OK playerItem=dtmapi_mine ItemEquipment, placed=dtmapi_mine/DolocTown.Decorator/index=108/anchor=2,1/cover=8x6/scene=sprite_equipment_well`; the copied screenshot is `docs/debug/evidence/GAME-SMOKE/20260603-210216/DTMAPI-evidence/NEWCONTENT-024/20260603-210256/mine-placed-dtmapi-mine.png`. The same run logs `MachineProduction cycle OK ... output=copper_ore count=1 mode=electric fuelRemaining=7180` plus state `fuel=7180/7200`, `cycleTUs=24`, `electricPower=10`, and native backpack placement.
- More Equipment Slots: `IEquipmentSlotsApi` now exposes DTMAPI-managed extra-slot listing/equip/unequip/recovery methods and renders a read-only DTMAPI extra-slot strip from `DolocTown.UI.AccessoriesBar` clones while keeping vanilla visual slots authoritative. Smoke `docs/debug/evidence/GAME-SMOKE/20260603-210216` gives `grandmas_button 0->1`, equips it to `dtmapi.more_equipment.1` with backpack `1->0`, applies it as an attribute-only native `AgentEquipmentFunction`, records `uiRendered=True` / `occupied=1` / `readOnly=true` / `preserveVanillaVisualSlots=true`, then recovers it with backpack `0->1`, `recoverCount=1`, and stored slots back to 0. Unity screenshot capture for this in-save strip was unavailable (`screenshot=unavailable`), so the retained evidence is log/summary based and an optional manual screenshot remains polish.
