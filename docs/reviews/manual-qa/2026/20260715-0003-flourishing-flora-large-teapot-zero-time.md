# 20260715-0003 Flourishing Flora Large-Teapot Zero-Time Review

## Review Header

- Status: resolved by user-confirmed game-build differential; no DTMAPI causal attribution
- Time: 2026-07-15
- Source: user report and two screenshots from the live game run started at 2026-07-15 20:33:02 +08:00
- Scope: targeted review of the reported tea-recipe/machine boundary only; this is not a full-package audit of every item, plant, recipe, asset, store entry, or economy value
- User constraint: inspect the running game's logs if readable; the game may be closed only if necessary
- Mutation boundary: read-only inspection; no Workshop package, game, save, DTMAPI Runtime, config, or enablement file was changed, and the running game was not closed
- Package: Workshop `3763389470`, `灰烬花园·繁盛花卉` / `Ashen Garden: Flourishing Flora`, author `非人无面&Qiuzy`, declared version `1.0`
- Package identity: `info.json` SHA256 `F97A99B5283942D10C032079C66F1EDC7F54337EE43460D117626F3BA215EF98`; `recipe_tbrecipe.json` SHA256 `AEC9E603F84AE98ED87CCD56B2A685CA414A134B9178B989DB020C4392708A01`; `mod_tbmodrecipegroupextension.json` SHA256 `79FFFCA08962F5460CEE68C5DF1425DC7F5E517F50990A647EEAD670FFFFADEB`
- Files/docs inspected: the three package recipe JSON files; current live DTMAPI/BepInEx logs; current DTMAPI creative-time Hook source; official Workshop recipe guide; current public native recipe, recipe-group, equipment, and room-effect configs; decompiled `RecipePanelUiState`, `IRecipeGroup`, `Synthesizer`, `IEquipmentWorker`, and `RecipeData`; canonical `author-docs/content-packs/json-derived-value-validation.md`
- Not inspected: a clean-process no-DTMAPI A/B; live object memory; the exact before/after daisy count; a corrected author package; every unrelated package domain

## Issue Review

### Issue 1: Daisy tea works in the small teapot but shows zero minutes and does not start in the large teapot

Original feedback:

- The external official-JSON Mod `灰烬花园·繁盛花卉` adds a daisy-tea recipe.
- The recipe works normally in the small teapot and displays a five-minute duration.
- The large teapot displays zero minutes for the same recipe, and pressing start appears to do nothing.

Screenshot/log transcription:

- Screenshot 1 shows `雏菊花茶`, output quantity `1`, input `雏菊` quantity `1`, and an enabled-looking `F 开始制作` control whose duration text is `制作耗时5分钟`.
- Screenshot 2 shows the same `雏菊花茶` and one-daisy input in the large-teapot UI, but the duration text is `制作耗时0分钟`; the backpack sidebar is visible and the user reports that pressing start produces no response.
- The game process and both current logs remained readable, so no shutdown was required. The DTMAPI and BepInEx logs contained zero Error/Fatal severity lines at inspection time and no `Synthesizer`, `Work(0)`, or recipe exception for this click.
- DTMAPI's content index identifies `daisy_tea` and `daisy` as coming from `Workshop.3763389470`. This is provenance/index evidence only; it is not evidence that DTMAPI loaded or rewrote the official JSON tables.
- The current DTMAPI run has the optional creative-time Harmony Hook installed, but the logs contain no creative-mode enable action and no `Debug.CreativeNoTime` observation. More decisively, an enabled creative mode would force both teapots to zero, which contradicts the small-teapot screenshot showing five minutes.

Review record:

- User-confirmed facts: the small teapot works at five minutes; the large teapot displays zero minutes; the large-teapot start action produces no visible work.
- Screenshot/log observations: the recipe and material resolve successfully in both UIs, so this is not a missing ID, missing asset, unlock, or JSON parse failure. The failure is silent rather than exception-driven.
- Package data facts:
  - `Content/recipe/recipe_tbrecipe.json` defines `daisy_tea` with integer `cost_time=1`, input `daisy x1`, and output `daisy_tea x1`.
  - The same file defines all seven added teas with `cost_time=1`: `pansy_tea`, `daisy_tea`, `chamomile_tea`, `jasmine_tea`, `rose_tea`, `camellia_tea`, and `violet_tea`.
  - `Content/recipe/mod_tbmodrecipegroupextension.json` adds all seven identical recipe IDs to both native groups, `teapot` and `large_teapot`.
  - The package's own `recipe_tbrecipegroup.json` defines only its custom `still` and `wax_station` groups; it does not replace either native teapot group's time ratio.
- Official/native facts:
  - The official guide defines `cost_time` in TU, with one TU equal to five in-game minutes. It documents the field unit but does not state that authors may ignore the target recipe group's ratio and native rounding.
  - In public build `23762374_public_C416D4`, `teapot` has `time_ratio=1`, while `large_teapot` has `time_ratio=0.5`; both are non-fixed-duration groups.
  - Native first-party recipes shared by these groups, including tea, coffee, herbal tea, milk tea, latte, bubble tea, hot cocoa, and refined bamboo essence, use `cost_time=2`, not `1`.
  - Current room-effect data has no teapot or large-teapot time addition, so the relevant effective ratios here remain `1` and `0.5`.
  - Native `IRecipeGroup.GetRecipeTime` calculates `RoundToInt(TimeRatio * recipe.CostTime)` before applying batch scale. The two screenshot results follow exactly:

    `Mathf.RoundToInt` uses midpoint-to-even rounding for exact half values, so `0.5 -> 0`, `1.5 -> 2`, and `2.5 -> 2`. A content author must not treat an exact effective value of `0.5` as a guaranteed positive TU.

    | Target group | Base ratio | Mod `cost_time` | Native rounded TU | Displayed time | Result |
    | --- | ---: | ---: | ---: | ---: | --- |
    | `teapot` | 1 | 1 | 1 | 5 minutes | valid |
    | `large_teapot` | 0.5 | 1 | 0 | 0 minutes | invalid |

  - `RecipeData` uses that native group result for its displayed time. `Synthesizer.StartWorkInternal` passes the same value to `Work(...)`.
  - `IEquipmentWorker.Work(int n)` returns `false` immediately when `n <= 0`, without starting the counter or producing an error. `Synthesizer.StartWorkInternal` ignores that return value. This is why the button can be pressed but the machine stays idle and no exception is logged.
  - In the normal recipe UI, `RecipePanelUiState.TryConfirmCraft` deducts input items before invoking the synthesizer's craft callback. Consequently, a confirmed large-teapot attempt can consume the selected daisy batch before `Work(0)` is rejected. The screenshots do not establish the exact before/after count, but repeated clicks are unsafe.
- Codex inference: the reported behavior is fully explained by a semantic data defect in the Mod: a `cost_time=1` recipe was shared with a group whose `0.5` multiplier rounds the effective interval to zero. The native UI/worker path amplifies the invalid value by allowing confirmation, deducting materials first, and silently ignoring the failed start.
- Ownership:
  - Primary author-owned defect: the package recipe duration and multi-group assignment are incompatible.
  - Native amplification gaps: no positive-effective-time validation in the UI; input is deducted before start is proven; the failed `Work` result is ignored and not reported.
  - DTMAPI is not the owner of the official table merge or this calculation. Its installed creative Hook cannot explain the displayed `0` because `RecipeData` calls `IRecipeGroup.GetRecipeTime` directly, outside the patched `Synthesizer.GetRecipeTime` method, and the Hook was also inactive in this run. Exclusive process-level attribution nevertheless remains open until the reported no-DTMAPI difference is reproduced under a controlled A/B.
- Scope expansion: the same defect applies to all seven flower-tea recipes listed above, not only `daisy_tea`, because they all use `cost_time=1` and are assigned to both groups.
- Rejected or unproven hypotheses:
  - Rejected for this reproduction: malformed JSON, unresolved daisy/tea item IDs, missing sprites, or a locked recipe. Both UIs render the same valid item, material, and recipe data.
  - Rejected: active DTMAPI creative mode. It would also make the small teapot display zero, and the runtime contains no enable/no-time observation.
  - Rejected as the primary mechanism: a managed exception. Current logs contain no Error/Fatal line and the native zero-value path returns `false` without throwing.
  - Not established: how many daisies the user's click consumed, whether the user saved afterward, or whether a stale non-working `taskCounter` was serialized. No machine item buffer is used by this normal recipe path, so there is no evidence of the persistent occupied-slot failure seen in the separate garbage-shredder incident.
- Minimal author correction choices:
  1. Preferred bounded repair when the large teapot is intended to be faster: change all seven shared recipes to `cost_time=2`. This matches native shared-tea data and yields small teapot `2 TU = 10 minutes`, large teapot `1 TU = 5 minutes` under current native ratios.
  2. If the design requires both machines to show exactly five minutes, one shared integer recipe cannot satisfy both ratios. Use separate recipe IDs: the small-teapot recipe uses `cost_time=1`, while the large-teapot recipe uses `cost_time=2`; keep their output and unlock semantics deliberately aligned.
  3. If large-teapot support was unintended, remove these seven IDs from `large_teapot` instead of presenting a zero-time, material-consuming action.
- Acceptance checks for an author correction:
  1. Cold-start with a corrected package and a backed-up save.
  2. Verify all seven teas in `teapot`, `large_teapot`, and `sunmao_large_teapot`/榫卯茶台 (which also uses the `large_teapot` group).
  3. For each target, record a strictly positive displayed duration, input count before/after, machine `IsWorking`, completion, and exactly one expected output.
  4. Test a batch greater than one because these groups allow different maximum craft counts (`5` and `10`).
  5. Save, exit, reload, and confirm no idle machine retains an unfinished task and no input was lost on a failed start.
- Blocker conditions: no blocker for root-cause classification. Final correction choice depends only on the author's intended balance: shared `2`-TU recipe with a faster large machine, separate per-machine recipe IDs for equal displayed time, or no large-teapot support.
- Required downstream docs if implementation happens: none in DTMAPI Runtime. The existing canonical author constraint `author-docs/content-packs/json-derived-value-validation.md` already requires per-target effective-value calculation, positive worker intervals, and validation of deduct-before-start paths. If DTMAPI later adds a read-only official-JSON doctor, this package should become a fixture for `RoundToInt(group.time_ratio * recipe.cost_time) >= 1` diagnostics; that would be a separate implementation Update.

## Cross-Issue Summary

- The effective-zero path in the current run is verified from the exact package data, native group values, responsibility functions, screenshot outputs, and silent worker rejection path. Exclusive attribution to the package, DTMAPI, or a process/load-state interaction is not considered closed until a controlled cold-start A/B addresses the author's reported no-DTMAPI success.
- This is syntactically valid official JSON but semantically invalid for one of its declared target groups.
- The affected package surface is seven teas across `large_teapot` and the upgraded equipment that reuses that group.
- The immediate player risk is silent material loss on confirmed zero-time crafts. Avoid repeated attempts; if the inventory count changed and the loss should be reverted, do not overwrite the save before reloading.
- No third-party or runtime mutation was authorized or performed, and the game was left running.

## Implementation Record Decision

- Create an implementation Update: no. This request was a read-only third-party package diagnosis, and no DTMAPI or package implementation was authorized.
- Create/update Debug, Hook Map, API Matrix, or smoke records: no. No DTMAPI Runtime defect or Codex-run smoke was established; the native amplification gaps remain reference facts in this review.
- Completion standard for this review: identify the exact data/native failure sequence, bound the affected recipes and player risk, rule out the active DTMAPI creative-time confound, and give author correction/acceptance choices without modifying the external package.

## 2026-07-15 Follow-up: Reported No-DTMAPI Success And Attribution Standard

Additional user evidence:

- The user reports that the Mod is said to work without DTMAPI and asks whether the package configuration can still be treated as the confirmed cause.
- The user explicitly compares this attribution uncertainty with the earlier garbage-shredder incident, where a successful test on one machine did not cover another machine whose recipe-group multiplier rounded the same raw recipe value to zero.

Additional exact-environment checks:

- The installed game's `Assembly-CSharp.dll` is 5,993,984 bytes with SHA256 `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404`, byte-identical to reverse baseline `23762374_public_C416D4`. The inspected native formula and group data therefore match the running game, not merely an older reference build.
- The installed DTMAPI Runtime manifest identifies version `0.5.5`, build commit `7ee65e7ce72b`, and GameBridge SHA256 `606C152AC30739B8E835D39F9E7F1E69B8DC4305EF6726A646E686A8FA422FE8`.
- A source search at that exact installed commit finds only one DTMAPI patch on `Synthesizer.GetRecipeTime`: the debug-console creative-mode Postfix. Its callback returns without changing the native result unless `ShouldBypassCreativeTimeHooks()` is true; when active it forces every synthesizer result to zero and records `Debug.CreativeNoTime`.
- The reviewed run contains no creative-mode enable transition and no `Debug.CreativeNoTime` observation. The small-teapot screenshot's positive five-minute result independently proves that the global creative-time override was not active.
- No exact-commit DTMAPI source path references `teapot`, `large_teapot`, `TimeRatio`, or `TimeAddition`, and no other Harmony registration targets `IRecipeGroup.GetRecipeTime`, `RecipeData`, `RecipePanelUiState`, or the synthesizer start/worker path.
- DTMAPI reports `ShadowContentRegistry=true` and `RegistryTakesOver=false`; its official/Workshop source indexing is observational and does not replace the native JSON table owner.
- One other direct BepInEx plugin, `连击大剑Mod 1.0.0`, was loaded in this run. Its assembly metadata names only `AgentControllerState.UseToolOrItem`, `AgentStateTool.OnEnter`, and `AgentStateTool.NextState` Harmony targets and contains no synthesizer, recipe-group, teapot, or timing symbol. It does not explain this fault, but a formal clean comparison should remove it so “without DTMAPI” does not accidentally mean a different BepInEx plugin set.

Revised attribution:

- The current package's configuration defect is confirmed independently of DTMAPI: the exact payload assigns the same `cost_time=1` recipes to `large_teapot`, while the exact installed native build computes `RoundToInt(0.5 * 1)=0`. Removing DTMAPI cannot change either operand or the native instruction in this exact environment.
- Therefore an exact same-package, same-build, same-save/context, same `daisy_tea`, same large-teapot test cannot produce a positive native duration merely because DTMAPI is absent. If a no-DTMAPI run did work, at least one comparison axis differed.
- The leading mismatch is the same class as the garbage-shredder incident: “the recipe works” may refer to the small teapot, where the multiplier is `1`, rather than the large teapot, where it is `0.5`. Other possible differences are an older/newer Workshop payload, another game build, a different recipe, `sunmao_large_teapot` versus another machine, or a non-clean plugin/profile comparison.
- The external no-DTMAPI statement is indirect and has no package hash, game DLL hash, machine ID, screenshot, or before/after timing evidence. It must be retained as an attribution question, but it does not overturn the confirmed invalid effective interval.
- Terminology boundary: “configuration defect is locked” is supported; “a controlled no-DTMAPI A/B has been run” is not. The latter should not be claimed until the matrix below is captured.

Optional exact A/B closure matrix:

1. Finish or deliberately discard the current player session; do not terminate it during active play for this review.
2. Back up/clone the test save and pin the three identities above: Workshop recipe/group hashes, game assembly hash, and DTMAPI release manifest/hash.
3. Use the same placed `large_teapot`, the same learned `daisy_tea`, the same room, and the same save state. Do not substitute the small teapot.
4. Run a clean native case with BepInEx not loaded; open the recipe UI and record its displayed duration. Pressing start is unnecessary for the timing comparison and risks consuming a daisy.
5. Run a DTMAPI-Core-only case with the unrelated Combo Sword plugin and all ordinary DTMAPI Mods disabled; record the same UI duration and confirm that creative mode remains off.
6. Optionally repeat the current full player profile only after the first two cases. A positive clean-native duration would require immediate runtime inspection of the actual `recipe.CostTime`, group identity, `BaseTimeRatio`, `TimeAddition`, and Harmony patch owners because it would contradict the exact static operands; a zero duration in both cases closes the indirect no-DTMAPI claim as a mismatched test.

No implementation is authorized by this follow-up. The running game remains untouched, and the external Workshop package remains read-only.

## 2026-07-15 Follow-up: Expanded DTMAPI Differential Static Audit

Reason for reopening attribution:

- The user treats the author's statement that the same Mod works without DTMAPI as credible evidence and asks for a review before accepting an author-configuration-only conclusion.
- This follow-up therefore supersedes the earlier wording that process attribution was “locked.” It does not retract the exact `cost_time=1` / `large_teapot time_ratio=0.5` effective-zero calculation; it changes only how confidently that calculation may be assigned as the exclusive cause of the reported environment difference.

Actual reproduction profile:

- The save loaded with seven DTMAPI Mods active, not a DTMAPI-Core-only profile: `DTMAPI.ConfigMenuExample`, `DTMAPI.HelloDtmMod`, `DTMAPI.HookProbeMod`, `DTMAPI.DebugConsoleMod`, `DTMAPI.ZoomMod`, `Yuuka.DTMAPI.ActionSpeed`, and `Yuuka.DTMAPI.AutoFishing`.
- The last four were hot-loaded after a native `ModManager.ReloadMods` at 2026-07-15 20:36:38. The save then loaded in slot 2 at 20:37:37.
- The enabled native Mod order at inspection time was: Y console priority 0, ActionSpeed priority 1, Zoom priority 2, AutoFishing priority 3, and `灰烬花园·繁盛花卉` priority 4.
- ActionSpeed was enabled with its machine-interaction option active. Source inspection shows that feature changes interaction animator speed and selected continuous-action delta values; it contains no recipe `CostTime`, recipe-group ratio, room time-addition, or synthesizer-duration write.
- The small and large teapot items used in this session were given through the Y console's native item creation/backpack placement route before placement. Static inspection shows no recipe/group mutation in that give path, but a controlled comparison should use the same pre-existing placed machine so item acquisition is not another differing axis.
- `smoke-settings.json` currently records `Enabled=false`; no active smoke-harness recipe mutation was found. HookProbe and smoke-named diagnostic statuses were present, so the run remains a development-heavy profile even though the gameplay smoke driver was not enabled by that file at inspection time.

Direct and indirect DTMAPI write audit:

- `ShadowContentRegistry` reads selected JSON files into diagnostic summaries. It does not write native Doloc tables, and its official file list does not include recipe or recipe-group JSON.
- `ContentQueryService` indexes official item metadata and source provenance for APIs and the Y console. Its rebuild path reads package files into DTMAPI-owned lists; it does not publish recipes or recipe groups into `DolocConfig.Tables`.
- `RegistryTakesOver=false` in the exact run. The shadow/content-manifest registries therefore do not replace native official-Mod loading.
- The only exact-build DTMAPI source reference to `CostTime`, `TimeRatio`, `TimeAddition`, `TbRecipeGroup`, or `Synthesizer.GetRecipeTime` that can affect duration is the conditional creative-mode Postfix on `Synthesizer.GetRecipeTime`. Its state defaults off, requires an explicit Y-console toggle, and produced neither a creative-enable log nor `Debug.CreativeNoTime` evidence here.
- More decisively, the duration shown in the recipe panel is constructed by native `RecipeData`, which calls `recipeGroup.GetRecipeTime(recipe)` directly. The DTMAPI Postfix is on the later `Synthesizer.GetRecipeTime` wrapper, so it cannot be the source of the screenshot's displayed `0分钟`.
- The experimental Machine Production API can mutate `TbRecipe.InputItems` for a specifically registered machine definition. It has no code to mutate `CostTime`; the current log contains zero machine-definition registrations and zero recipe-input override events, and MineMod was disabled.
- The four active functional Mods contain no direct Harmony usage and no recipe/group-duration strings. The other direct BepInEx plugin, Combo Sword, has no recipe/synthesizer/teapot timing targets.

Cross-package collision and load-order audit:

- A scan of all installed Workshop and local official JSON found `daisy_tea`, `teapot`, and `large_teapot` references only in Workshop package `3763389470`; there is no second installed definition of `daisy_tea` or a second enabled extension for those two groups.
- Of the five enabled official packages, only `3763389470` contains recipe or recipe-group JSON. The Y console contains one item table; ActionSpeed, Zoom, and AutoFishing contain only DTMAPI manifests/config-facing data and no official recipe files.
- This rules out an ordinary enabled-package ID collision. It does not by itself prove that native cold-load and in-process `ReloadMods` produce identical table object state, so cold start versus hot reload remains a legitimate comparison axis.

Current attribution after expanded review:

- Confirmed: the current payload declares all seven flower teas at `cost_time=1`, assigns them to `large_teapot`, and the exact current native formula yields zero TU for that combination using midpoint-to-even rounding.
- Confirmed: no static DTMAPI or active first-party-Mod code path was found that changes the displayed recipe-panel duration, `daisy_tea.CostTime`, `large_teapot.BaseTimeRatio`, or the relevant `TimeAddition`.
- Not confirmed: that the author's no-DTMAPI test used the same Workshop payload hash, game build, large-teapot group, save/room, enabled Mod set, and cold/hot load state.
- Not confirmed: that DTMAPI has no process-level effect whatsoever. The current run includes a native Mod hot reload and several active functional Mods; only a cold-start matrix can close that residual boundary.
- Therefore the defensible wording is: “the current package has a verified zero-effective-time data risk on the current native large-teapot group, while the reported DTMAPI-only reproduction remains open.” It is premature to tell the author that DTMAPI has been experimentally excluded.

Revised minimal closure matrix:

1. Preserve the current save and package/game hashes, then finish the active player session normally.
2. Cold-start a native case with BepInEx/DTMAPI not loaded, the same Workshop payload enabled, and no other official Mod enabled. Use the same pre-existing placed `large_teapot`; record the recipe-panel duration without pressing Start.
3. Cold-start a DTMAPI-Core-only case with all ordinary DTMAPI Mods and Combo Sword disabled, the same official-Mod enablement and order, and the same machine/save; record the same value.
4. Cold-start the full current profile without changing enablement in-process; record the same value. Only if needed, run a fourth case that repeats the in-process official `ReloadMods` sequence.
5. If native is positive while Core-only is zero, capture live `recipe.CostTime`, selected group ID, `BaseTimeRatio`, `TimeAddition`, final `TimeRatio`, and Harmony owners before changing code. If both are zero, the indirect no-DTMAPI report used a different comparison axis. If only the hot-reload case is zero, investigate native/DTMAPI reload lifecycle rather than changing the author's balance value first.

No runtime mutation or A/B was performed in this static follow-up. The game remains running and the Workshop package remains unchanged.

## 2026-07-15 Final Follow-up: Formal-Build Fix Confirms Version Differential

Final user confirmation:

- The failed action in the reported run is confirmed to be the native `Work(0)` path silently returning failure.
- The Mod author tested the formal/release game version, where this native problem has already been fixed.
- The earlier statement that the Mod works without DTMAPI therefore compared different game behavior, not an otherwise identical process whose only changed variable was DTMAPI.

Final attribution:

- DTMAPI is not causal for this incident. A DTMAPI/no-DTMAPI A/B is no longer required to explain the author's successful test.
- The current reproduction's `0分钟` and no-response sequence remains accurately described by the inspected build: the package/group combination derives zero TU, and that build's worker silently rejects `Work(0)`.
- The formal-build correction mechanism was not inspected in this review. Without the exact formal-build assembly and responsibility path, this record does not claim whether the game clamps the duration, blocks invalid confirmation, changes rounding, accepts zero-time work, or handles the failed return value.
- The package's `cost_time=1` assignment remains a backward-compatibility risk for any game build that still derives zero TU and silently rejects `Work(0)`. Changing it is not required merely to fix DTMAPI compatibility, because DTMAPI did not create the failure.

Closure decision:

- Close the DTMAPI attribution question as a game-version differential confirmed by the user.
- Do not modify DTMAPI Runtime or the third-party package from this review.
- Reopen only if the same package and same formal game build reproduce different behavior with and without DTMAPI under otherwise identical conditions.
