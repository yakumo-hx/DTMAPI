# 20260713-0002 Garbage Shredder Last-Run Log Review

## Review Header

- Status: recorded
- Time: 2026-07-13
- Source: user report and screenshot; last completed game run at 2026-07-13 22:57-23:02 +08:00
- Scope: review and root-cause triage only; no Runtime, Mod, config, save, game, or Workshop mutation
- User constraint: inspect the previous game-run logs; suspected ActionSpeed/animation acceleration
- Related records: `20260616-0002-actionspeed-native-interaction-followup.md`, `ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Files/docs inspected: current `DTMAPI/logs/latest.log`, `BepInEx/LogOutput.log`, Unity `Player.log`, installed ActionSpeed config, ActionSpeed GameBridge source, current public reverse build `23762374_public_C416D4`, native `GarbageShredder`/`GarbageShredderUiState`, official dismantle recipe/group/item configs
- Not inspected: live runtime object state inside the station; a vanilla/ActionSpeed-off control run; a run using a confirmed valid city-shredder input

## Issue Review

### Issue 1: Krank large rubbish decomposer appears unable to decompose with DTMAPI installed

Original feedback:

- The large rubbish decomposer at Krank cannot decompose after DTMAPI is installed.
- ActionSpeed/animation acceleration is suspected.

Screenshot/log transcription:

- Screenshot: the player is standing beside the large outdoor rubbish decomposer; the native `E 使用` prompt is visible. No exception, error dialog, submit panel, or result item is visible in the captured frame.
- The last run starts at `22:57:32`, loads save slot 2 at `22:58:59`, and ends at approximately `23:02:40`.
- At `22:59:34.953-22:59:35.038`, the DTMAPI Y console gives ten `monster_drop_drone_33` items through native backpack placement. The official title for that item is `33式火控系统`.
- The run contains zero `ActionSpeed tool animation speed applied`, zero `ActionSpeed interaction animation speed applied`, zero `ActionSpeed continuous native timer scaled`, zero ActionSpeed auto-fill applications, and zero ActionSpeed animator restores.
- Runtime retention snapshots report `actionAnimators=0`, `actionAutoFillApplications=0`, and `actionPendingAnimalInteract=false`.
- DTMAPI `latest.log` has zero Error/Fatal entries. BepInEx and Unity `Player.log` have no exception class or native `GarbageShredder` error line. The 16 warnings are duplicate-package/startup/input-health warnings and do not identify a shredder failure.

Review record:

- User-confirmed facts: the player-visible decomposer attempt did not produce the expected decomposition while DTMAPI was installed.
- Screenshot/log observations: native proximity/use detection remained alive; the run does not log a native exception or an ActionSpeed application during the attempt.
- Code/doc facts inspected:
  - The installed ActionSpeed policy was enabled, including `MachineAddSpeedEnabled=true` at `3x`, but its current classifier only accelerates enumerated `AgentStateInteract`/continuous-use slices such as fuel/feed, sprinkler/light, animal, plant, water, and harvest targets. `GarbageShredder` is not a classified machine-interaction target.
  - Native city `GarbageShredder.OnInteract` validates ordinary items through `ContentFilter`; non-`ItemEquipment` items must occur in the station's dismantle recipe group.
  - The current game `Assembly-CSharp.dll` SHA256 is `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`, exactly matching reverse baseline `23762374_public_C416D4`, so the inspected native path matches the installed game assembly.
  - `garbage_shredder_city` accepts only `scrap_big_machanical` (大块的机械废料), `scrap_machanical` (小块的机械废料), `scrap_drone` (无人机残骸), `rubbish` (垃圾), and `plantbasin_eggshell` (蛋壳种植盆). Its upgraded variant uses the corresponding upgrade recipes.
  - `monster_drop_drone_33` is an ordinary `ItemFunction`, not `ItemEquipment`, and is absent from every city-shredder input recipe. Its `salable=true` flag does not bypass the non-equipment recipe requirement.
- Codex inference: the strongest explanation for this specific run is an invalid test input rejected by native `GarbageShredder.ContentFilter`, not animation acceleration. The Y-console native placement succeeded, but placement into the backpack does not imply the item is a valid decomposer input.
- Ownership: native `GarbageShredder.ContentFilter` and `garbage_shredder_city` recipe data own input eligibility. DTMAPI ActionSpeed owns only the enumerated player animation/timer paths and shows no application evidence here.
- Root-cause hypotheses:
  1. Strongly supported for this run: `monster_drop_drone_33` was used or prepared as the test input, but the native city decomposer does not accept it.
  2. Still possible but unproven: a separate DTMAPI interaction/input/UI hook prevents the station path when a valid input is used. The current logs do not instrument `GarbageShredder.OnInteract`, `ContentFilter`, question/submit UI, `RestartWork`, or `OnWorkDoneInternal`, so they cannot exclude this broader bug.
  3. Weak for this run: ActionSpeed corrupts the station. No ActionSpeed application/restore occurred, `GarbageShredder` is outside its classifier, and the station's city interaction path does not depend on the accelerated `AgentStateInteract` paths inspected.
- Rejected/unproven hypotheses:
  - No evidence supports an exception-driven failure, a retained ActionSpeed animator, OneAction garbage completion, or a Unity GC failure in this short run.
  - ActionSpeed being loaded and configured at 3x is correlation only; its Hook status being `experimental` does not prove that this station was touched.
  - The screenshot alone does not prove whether the selected/attempted item was `monster_drop_drone_33`, so the invalid-input conclusion is specific to the logged preparation and remains subject to one controlled valid-input retest.
- Required downstream updates: none before a controlled retest. If a confirmed valid input fails with ActionSpeed off/on isolation, create a Debug issue and an implementation Update; add focused Hook/native instrumentation only then.
- Acceptance checks:
  1. Cold-start the same save with DTMAPI and use one confirmed valid input, preferably `rubbish` or `scrap_drone`, at the Krank city decomposer.
  2. Record whether the confirmation/submit UI opens, money/input is consumed, work starts, and output drops after the native interval.
  3. Repeat with ActionSpeed's master switch off, then on at the current 3x interaction setting, keeping the item/save/station fixed.
  4. A valid item working in both runs rejects the reported DTMAPI/ActionSpeed regression for this boundary. Failure only with ActionSpeed on supports an ActionSpeed investigation. Failure in both DTMAPI runs but success without DTMAPI supports a broader DTMAPI Hook/input/UI investigation.
- Blocker conditions: without a valid-input control and a recorded failure stage, the logs cannot distinguish native eligibility rejection from a genuine station-path regression.

## Cross-Issue Summary

- Confirmed user facts: the expected decomposition was not observed with DTMAPI installed.
- Screenshot/log facts: the `E 使用` prompt worked; the last run generated `monster_drop_drone_33`; no ActionSpeed application, shredder exception, DTMAPI Error/Fatal, or GC failure was recorded.
- Code-path finding: `monster_drop_drone_33` is not a valid Krank city-decomposer recipe input.
- Risk: treating this run as proof of an animation bug would send a fix toward a Hook that did not execute and could destabilize already verified ActionSpeed slices.
- Suggested implementation scope: none yet. First run the valid-input ActionSpeed-off/on matrix; if it fails, add narrow native observations around `GarbageShredder.OnInteract`, `ContentFilter`, `RestartWork`, and `OnWorkDoneInternal` without changing results.
- Item not to carry forward as fact: “ActionSpeed caused the garbage decomposer failure.” It remains unproven and is contradicted by the current application counters.

## Implementation Record Decision

- Create/update an implementation Update: no; this request was review-only and the current evidence points to an invalid native input rather than a demonstrated code defect.
- Additional Debug/API/Hook/smoke records required: no before retest. A repeat with a confirmed valid input would justify a new machine-specific Debug issue and focused Hook/native-owner record.
- Suggested task title if reproduced: `GarbageShredder valid-input native-stage instrumentation and ActionSpeed isolation`.
- Completion standard: valid city-shredder input decomposes through native confirmation, work, completion, and drops with DTMAPI installed; ActionSpeed off/on produces identical machine semantics; logs remain free of errors and no process remains after exit.

## 2026-07-13 Follow-up: Mod Recipe And Persistent Machine-State Root Cause

User correction and additional reproduction facts:

- `33式火控系统` is intentionally added to the dismantle recipe table by the enabled Workshop Mod `Workshop.3749143385`, `（体验优化）怪物素材转化【海天制作组】`.
- Once one item using this Mod's dismantle recipes is submitted in a save session, the Krank city shredder stops processing that item and later vanilla inputs also fail.
- If the player saves after the failure, disabling or uninstalling the content Mod does not restore that station.
- The user therefore suspected persistent save corruption rather than a one-time invalid-input rejection.

Correction to the preliminary conclusion:

- The earlier conclusion that `monster_drop_drone_33` was merely an invalid native input is superseded. It was valid in the active runtime because the enabled Workshop package adds recipe `HTL_33shihuokongzhi` to `garbage_shredder`, `garbage_shredder_city`, and `garbage_shredder_city_upgrade`.
- The earlier ActionSpeed finding remains valid: the reviewed runs contain no ActionSpeed application to this station, and the root cause below occurs entirely in native recipe timing and machine inventory state.

New log and package evidence:

- The new run starts at `23:11:31`, loads slot 2 at `23:12:18`, gives two `monster_drop_drone_33` items at `23:13:47`, and records no managed exception, native `GarbageShredder` error, or ActionSpeed application.
- The Workshop recipe defines input `monster_drop_drone_33`, output LUT `HTL_33shihuokongzhi`, output count range `8..8`, and `cost_time=1`.
- All 13 new dismantle recipes in this package use `cost_time=1`.
- The package places all 13 recipes in both city groups, whose native/Mod table value remains `time_ratio=0.0835`.
- The output LUT exists and resolves to `gold_ore`, `iron_ore`, and `old_electric_wire`; the failure occurs before output spawning.

Confirmed native failure sequence:

1. `GarbageShredder.GetCurrentInterval()` calculates `Mathf.RoundToInt(recipeGroupProto.TimeRatio * recipeProto.CostTime)`.
2. For every new city recipe in this package, this is `RoundToInt(0.0835 * 1) = 0`.
3. The city confirmation path removes the selected item from the player's inventory, places a clone into the shredder's one-slot `LinearInventory`, and calls `RestartWork(true)`.
4. `RestartWork` calls `Work(0)`. Native `IEquipmentWorker.Work(int n)` rejects `n <= 0` and returns `false` without setting `IsWorking`.
5. No completion callback can run, so the submitted Mod item remains in the one-slot machine inventory. This silent early return explains why neither the new log nor the prior log contains an exception or `GarbageShredder` output error.
6. A later city interaction checks only `IsWorking && !inventory.isEmpty`. The poisoned state is `IsWorking=false` with a non-empty inventory, so that guard does not report normal in-progress work.
7. The later path can remove another selected item and call `LinearInventory.PlaceItem`, but the one-slot inventory is already full. `PlaceItem` returns the unplaced clone and the caller ignores that return value. This explains why vanilla dismantling also appears to fail after the first Mod item.
8. `GarbageShredder.inventory` and `LinearInventory.items` are JSON-persisted fields. Saving therefore persists the stuck item. Removing the recipe provider later does not clear or return that item, so uninstalling the Mod cannot repair the already occupied station.

Read-only slot-2 save inspection:

- The encrypted slot-2 file was decoded only in memory; no plaintext copy or save mutation was created.
- The file on disk predates the `23:11-23:15` reproduction. Its Krank city station is currently serialized as `equipmentName=garbage_shredder_city`, `roomId=city_多洛可东部`, `IsWorking=false`, and `inventory.items=[null]`.
- Therefore the current disk copy is not yet poisoned by the new reproduction and should remain recoverable as long as the player does not overwrite it after reproducing the failure.
- This inspection does not contradict the user's persistence report: native serialization proves that a submitted zero-interval item would be retained if the broken runtime state were saved.

Root-cause classification:

- Primary cause: invalid third-party content timing data. `cost_time=1` is incompatible with the native city group's `time_ratio=0.0835` because the resulting interval rounds to zero.
- Native amplification defects: `GarbageShredder` consumes the player item before proving work can start, does not reject a zero interval, does not check machine capacity on the idle/non-empty path, and ignores the `LinearInventory.PlaceItem` remainder.
- Persistence scope: station-local serialized inventory poisoning, not evidence that the whole archive graph is corrupt.
- DTMAPI/ActionSpeed: not causal in the inspected path. DTMAPI currently neither validates official content-table timing nor repairs native city-shredder inventory state.

Safe recovery and follow-up boundaries:

- Do not save over the current clean slot-2 file after reproducing the failure.
- A durable content fix must ensure every city recipe produces a positive rounded interval; changing only animations cannot fix this path.
- A player-safe recovery for an already affected save must first back up the archive, then use a targeted native/runtime operation to return or clear only the Krank shredder's stuck internal item and reset its worker state. Raw save editing is not authorized by this review.
- A future DTMAPI compatibility guard could validate `round(time_ratio * cost_time) >= 1` for dismantle recipes and report the owning content package before input is consumed. That would be a separate implementation task with its own Update, Debug issue, automatic tests, and locked third-save game smoke.

## 2026-07-13 Player Corrupt-Save Comparison And DTMAPI Attribution Reopen

Additional user evidence:

- The user supplied `D:/下载/ea-playtest-doloc-archive-2.data` as a player archive saved after this failure.
- The user's local slot-2 archive at `C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/SAVE/ea-playtest-doloc-archive-2.data` was not saved after the local reproduction and is the comparison baseline.
- The user reports that the content author is experienced and is said to have tested the Mod successfully without DTMAPI. This is indirect rather than a captured control run, but it reopens DTMAPI attribution and must not be discarded.

Read-only archive comparison:

- Both archives report game version `0.96.08` and archive index `2`.
- Player affected archive SHA256: `61CFF4BF14E4C72FA45B0BC39235517D99E40B016E25DCE6A928BF2759EE11CC`, saved on disk at `2026-07-13 23:18:22`.
- Local clean baseline SHA256: `D4755309FB34BA09BDBBF8E3BCB16B66BE3D826864C5474237594C58C2372770`, last written at `2026-07-12 08:37:34`.
- The same Krank machine is identified in both archives by `roomId=city_多洛可东部`, `guid=42ae13d5-cd36-4d62-bcc5-067ac79488d3`, and `equipmentName=garbage_shredder_city`.
- Clean baseline state: `isIdle=false`, `isWorking=false`, `counter=0/8`, `inventory.items=[null]`.
- Player affected state: `isIdle=false`, `isWorking=false`, `counter=0/8`, `inventory.items=[monster_drop_tardigrade x2]`.
- `monster_drop_tardigrade` is another input provided by this Workshop package. Its recipe also has `cost_time=1` and is included in the city group.
- The count of two is consistent with two failed submissions combining in the same one-slot `LinearInventory`: the first zero-interval call leaves the item behind; the second call removes another player item and combines it with the already-stored same item while work remains stopped.
- The unchanged stale `counter.interval=8` is also consistent with `Work(0)` returning before `ForceWork/SetInterval`; the zero-interval attempt does not replace the previous counter interval.

DTMAPI static-boundary audit:

- Workshop package `3749143385` contains official `info.json` plus JSON content only. It contains no DTMAPI manifest, Entry DLL, or code assembly.
- The package ID, title, author, and `HTL_` recipe IDs occur zero times in the reviewed DTMAPI runtime log. DTMAPI did not publish or load this package as an ordinary DTMAPI Mod.
- DTMAPI's `ModScanner` requires a DTMAPI/standard manifest before a package enters the DTMAPI registry. Its official/Workshop scanning and Manager item indexing are read-only file/index operations.
- Current refactor flags explicitly report `ShadowContentRegistry=true` and `RegistryTakesOver=false`.
- DTMAPI source contains no `TbDismantleRecipe`, `DismantleRecipeGroup`, `GarbageShredder`, or worker-counter mutation path outside smoke/reference text.
- The official `ModManager.ReloadMods` Harmony postfix only notifies DTMAPI to refresh diagnostics; it does not call or replace the official table merge.
- ActionSpeed patches `AgentStateTool`, selected `AgentStateInteract`/`AgentStateEat` animation phases, selected continuous-use deltas, and `AnimalRenderer.OnInteract`. Its machine classifier is limited to `PowerGeneratorFuel`, `Feeder`, `Sprinkler`, and `FarmLight`; it does not classify `GarbageShredder` and does not patch `GetCurrentInterval`, `RestartWork`, `IEquipmentWorker.Work`, or `Counter`.
- The relevant runs recorded zero ActionSpeed interaction/timer application and no retained ActionSpeed animator for the shredder attempt.

Revised attribution:

- Confirmed trigger/state: a `cost_time=1` Mod recipe entered the Krank city shredder, work did not start, and the input remained in the persisted one-slot inventory. The affected archive now proves this state directly.
- Not yet confirmed: why the content author's no-DTMAPI test reportedly passed. Static review found no DTMAPI path capable of changing these recipe/group values or the shredder worker interval, but static absence is not a substitute for a controlled runtime A/B.
- Leading boundary mismatch: the farm `garbage_shredder` group uses `time_ratio=1`, so the same `cost_time=1` recipes produce interval `1` and work. Krank's `garbage_shredder_city` uses `time_ratio=0.0835`, producing interval `0`. A successful farm-machine test does not cover the reported city machine.
- Other unresolved alternatives: the author tested a different Workshop payload/revision, did not test this specific city station, or there is an as-yet-unobserved DTMAPI/BepInEx runtime-order interaction despite no matching mutation or Hook in source.
- Therefore the prior statement “DTMAPI/ActionSpeed is not causal” is narrowed: ActionSpeed has strong negative evidence and no matching path; broader DTMAPI presence remains unproven rather than excluded until the same current package, game build, clean archive, item, and Krank station are tested with DTMAPI absent/present.

Required controlled comparison before implementation:

1. Duplicate the clean archive; do not use the affected player archive and do not overwrite the baseline.
2. Keep game `0.96.08`, Workshop manifest, official enablement, item, archive, and Krank station fixed.
3. Run the Krank city station with no BepInEx/DTMAPI and submit one `monster_drop_tardigrade` or `monster_drop_drone_33`; record whether work starts and output drops.
4. Repeat with DTMAPI Core but first-party ActionSpeed disabled, then with ActionSpeed enabled.
5. Separately test the farm `garbage_shredder` only to confirm the expected farm/city timing split; do not treat farm success as Krank success.
6. Instrument or inspect runtime values for `recipe.CostTime`, `group.TimeRatio`, calculated current interval, `IsWorking`, counter, and inventory immediately before/after submission. Do not introduce a repair or interval clamp until this evidence is captured.

## 2026-07-13 Authorized Player Archive Repair

Authorization and boundary:

- The user explicitly authorized repair of the supplied affected archive after the read-only diagnosis.
- The original `D:/下载/ea-playtest-doloc-archive-2.data` was not overwritten. Its SHA256 remained `61CFF4BF14E4C72FA45B0BC39235517D99E40B016E25DCE6A928BF2759EE11CC` before and after the operation.
- The repaired copy was written to `D:/下载/ea-playtest-doloc-archive-2-repaired.data`; its SHA256 is `1A171A44AF688DF46DD35EE06A0C31D8BD457A8BA40DEF6BA17DEC0BE9BBC09E` and its size is `1,165,431` bytes.
- No local active save slot, game runtime, Mod package, configuration, or DTMAPI runtime file was changed or launched.

Minimal repair performed:

- The encrypted archive was decoded in memory using the installed matching game build's native serialization parameters. No plaintext archive or cryptographic material was persisted in the workspace or review.
- The target was required to be the unique `garbage_shredder_city` at `cityData.globalInteractableObjectManager.objectLogics[9].equipment`.
- The operation required its one-slot inventory to contain exactly `monster_drop_tardigrade x2`, then changed only that serialized fragment to `inventory.items=[null]` while preserving `slotLockStates=[false]` and every other plaintext byte.
- The two stuck Mod items were cleared rather than injected into the player backpack. This kept the repair station-local and avoided an unrelated inventory mutation.

Verification:

- The repaired output was read back, decrypted again, and parsed successfully as archive index `2`, game version `0.96.08`.
- The repaired archive contains exactly one `garbage_shredder_city`, and its internal inventory now verifies as `[null]`.
- A semantic token comparison against an expected clone of the original archive with only that slot replaced passed.
- This repairs the persisted station state only. Reusing a `cost_time=1` package recipe in the Krank city shredder can reproduce the failure until the content/runtime trigger is corrected.

## 2026-07-14 User Attribution Confirmation And JSON Constraint

User-confirmed attribution:

- The user confirmed that this incident is not considered a DTMAPI problem and that the native/content-programming explanation is now overwhelmingly likely. Direct DTMAPI causation is therefore treated as excluded for the current issue unless future exact A/B evidence contradicts this confirmation.
- The prior ActionSpeed suspicion is closed for this incident. ActionSpeed has no matching `GarbageShredder`, recipe interval, worker counter, or machine-inventory mutation path, and the reviewed run recorded no applicable ActionSpeed event.
- The durable classification is: third-party JSON supplied a city-group recipe whose derived interval rounded to zero; native `GarbageShredder` transaction/state validation then amplified that invalid value into item loss and a persisted station-local inventory state.

Durable authoring constraint:

- New canonical guidance lives at `author-docs/content-packs/json-derived-value-validation.md`.
- DTMAPI first-party, sample, QA, official-local, Workshop, and author-facing JSON work must calculate the final native value after every multiplier, divisor, global unit and rounding step. Raw field validity is not sufficient.
- Production time and other divided/scaled values require a per-target-group calculation table. A recipe that is valid in one group must be split when another group's multiplier would round, floor, truncate, overflow, or otherwise move the effective value outside the native state machine's valid range.
- For this build's Krank city shredder, integer `cost_time >= 6` is the minimum that makes `RoundToInt(0.0835 * cost_time) >= 1`; `cost_time=1` remains valid only in the farm group with `time_ratio=1`.
