# Manual QA Code Review: Mine Production and Y Console Time Skip

Date: 2026-06-05 20:59:53 +08:00
Reviewer role: feedback-to-goal / root-cause review Codex
Scope: code-level review and conversion input only; no runtime implementation, no build/game smoke.
Source: user manual QA follow-up with two Mine screenshots and specific comparison of sleep, container-room time skip, and same-farm-scene time skip.

## 1. Mine production differs between sleep, container time skip, and farm-scene time skip

User feedback, preserved:
- Review the Mine production logic again.
- Check how the Y-console `下一时间段` button is implemented.
- Check whether it uses normal time flow, and whether that time flow normally affects machines and crops.
- Observed behavior: sleeping over time can produce Mine output.
- Observed behavior: using time skip inside a container room can produce Mine output.
- Observed behavior: using time skip on the farm in the same scene as the Mine does not produce output.

Screenshot-to-text:
- Screenshot 1 shows the player outdoors on a farm-like scene with the Mine placement preview visible. The placement area/preview rectangle is large, but the preview sprite still looks inconsistent with the desired 2x placement preview.
- Screenshot 2 shows the Mine placed as a large pump-like machine on the farm scene.

Code review:
- The Y-console button is `ReflectedDebugConsoleUi.SkipTime`, which calls `ITimeDebugApi.SkipToNextWeatherPeriod`.
- `SkipToNextWeatherPeriod` computes the next target hour using `GetNextDebugWeatherPeriodTarget`: before 06:00 goes to 06:00; before 18:00 goes to 18:00; otherwise goes to 24:00.
- The implementation calls native `ArchiveDataHandle.PassTimeNoControl(seconds, wake, true)` and then invokes `DolocAPI.OnWakeUp`. It is not a raw save-field edit.
- Decompiled game code confirms `PassTimeNoControl` runs `_BeforeTimePass`, then repeatedly calls `UpdateNoRender`, then `_AfterTimePass`, then the callback.
- Decompiled `ArchiveDataHandle._UpdatePerSecNoRender` increments total seconds, calls `farmData.MainFarm.UpdateNoRender`, updates current room/root-room in specific cases, updates NPCs and global interactables.
- Decompiled `Room.UpdateNoRender` updates `DM_equipment`, `DM_electric`, `DM_automate`, vegetation growth, weather handling, and animal no-render updates depending on room type. This supports the conclusion that the Y-console time skip is using a native time-flow path that can affect official room systems, crops/vegetation, and equipment.
- DTMAPI Mine production is separate from official machinery. `UpdateMachineProduction` runs from DTMAPI runtime automation, not from native room/equipment `UpdateNoRender`.
- DTMAPI Mine production currently scans `DolocAPI.CurrentRoom` only, then recurses through that room's building child rooms. It does not scan all persistent rooms or all known placed Mine machines globally.
- On first observation of a Mine, DTMAPI initializes `NextDueTotalTus = totalTus + cycleTus`. If the Mine was not observed by DTMAPI before a time skip, the skipped time is not credited; the next production is scheduled after the skip.
- If the Mine was already observed and `NextDueTotalTus` is due, DTMAPI produces at most one cycle after a large time jump. It does not catch up multiple two-hour cycles.
- `TryRunMachineProductionCycle` advances `NextDueTotalTus` before fuel/output placement succeeds; a storage/fuel failure can skip the due time and obscure why the player saw no output.

Classification:
- Y-console time skip side: the button uses the native time-pass route and is likely acceptable as a debug time-advance API, but it needs documented acceptance against crops/official equipment.
- DTMAPI Machine API side: Mine production is not integrated into native pass-time hooks and is scoped to current-room scanning, so it can diverge from official time behavior.
- MineMod side: config/display should clearly say 120 means game minutes and default production is intended to be every 2 in-game hours, not only daily/weather-period production.

Review status:
- The latest user observation is compatible with the code: native time advances, but DTMAPI Mine production can miss or fail cycles depending on whether the Mine is already observed, which room is current, and whether the Mine is in the scan scope.
- This should be fixed as a Machine API lifecycle/catch-up problem, not by changing the Y-console time skip into a raw save edit.

Acceptance points for implementation:
- Y-console next-period evidence must show before/after time and confirm native `PassTimeNoControl` route.
- A placed Mine must produce every configured 120 game minutes when time passes normally.
- A placed Mine must produce/catch up correctly after sleep.
- A placed Mine must produce/catch up correctly when using Y-console next-period inside the Mine's room/container.
- A placed Mine must produce/catch up correctly when using Y-console next-period on the farm in the same scene as the Mine.
- A long skip should either process every due cycle or explicitly record/log a bounded catch-up policy; it must not silently skip due production.
- Storage full, missing storage, or no fuel must not advance `NextDueTotalTus` without a clear failure reason and visible/logged evidence.

Blocker rule:
- If DTMAPI cannot safely enumerate all persistent placed Mine machines or hook native pass-time lifecycle, leave Mine production experimental/incomplete and report the exact unsupported path. Do not mark complete based only on a forced smoke production cycle.
