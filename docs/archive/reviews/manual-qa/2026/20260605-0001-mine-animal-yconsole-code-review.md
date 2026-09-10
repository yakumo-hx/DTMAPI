# Manual QA Code Review: Mine, Animal Progress, Y Console

Date: 2026-06-05 19:13:47 +08:00
Reviewer role: feedback-to-goal / root-cause review Codex
Scope: code-level review only; no runtime implementation, no build/game smoke.
Source: user manual QA with screenshots for Mine placement preview and animal bell UI.

## 1. Mine preview and production timing

User feedback, preserved:
- Mine placement preview does not apply the intended 2x scale.
- The default `120` minutes shown for mine production is unclear.
- Current behavior appears to produce only at 18:00.
- Sleeping one night sometimes produces nothing, reason unknown.

Screenshot-to-text:
- While holding/placing the Mine, the large placement area/preview overlay is visible, but the actual Mine preview sprite appears small and not consistently scaled to 2x.

Code review:
- `testmods/MineMod/ModEntry.cs` sets the default production interval to `CycleMinutes = 120`; this is intended to mean game minutes, and the config menu exposes it as "Cycle minutes".
- `testmods/MineMod/ModEntry.cs` registers `VisualScale = 2`, but the real scaling path is owned by DTMAPI GameBridge.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs` schedules machine production from `archive` total TU values. New machine runtime entries are initialized with `NextDueTotalTus = totalTus + cycleTus`.
- Production is gated by `newTu`, i.e. `totalTus != lastMachineProductionTotalTus`, not by a persisted per-machine clock. If the game only advances this TU value at coarse time boundaries or does not replay elapsed time during sleep/load, mine production can look like a daily/time-period event instead of "every 2 hours".
- The production runtime entries are in-memory. There is no reviewed evidence here that next-due time persists across save/load or performs sleep/offline catch-up.
- `TryRunMachineProductionCycle` advances `entry.NextDueTotalTus` before output placement/fuel checks fully succeed. A full/missing storage or failed placement can skip the due cycle and make "slept but no output" harder to diagnose.
- `ApplyMineBuilderPreviewScale` only changes `indicatorRenderer.indicator.transform.localScale`. The user-visible placement ghost appears to include other objects, so scaling this one child is not enough to prove the actual preview sprite/placement footprint is scaled.

Classification:
- MineMod side: config wording and default value presentation.
- DTMAPI upgrade side: Machine API scheduling, persistence/catch-up semantics, production failure telemetry, builder preview scaling target.

Review status:
- Not solved by `20260605-0003` smoke evidence. That evidence validates a focused path, but it does not prove real player placement preview or sleep/offline production.

## 2. Animal husbandry progress still flickers / is not reliable

User feedback, preserved:
- Animal progress still has not been fixed.
- DLKsmapi's animal husbandry mod can be used as a behavior reference because it displays reliably.

Screenshot-to-text:
- The animal bell UI opens a large animal list on the left and a detail panel on the right.
- The third progress row is still inconsistent: it may display native "mood" text rather than the hidden product label, and the visible label/font treatment is not matching the desired stable native-like row.

Code review:
- Current DTMAPI has two overlapping strategies:
  - `DecorateAnimalFullInfoData` mutates text data/state description.
  - `RenderAnimalProgressOverlay` clones a native mood bar after the native viewer has already rendered.
- `AnimalViewerShowPostfix` calls `RenderAnimalProgressOverlay` only after native `Show`, so a one-frame native mood display before replacement is structurally expected.
- `AnimalPanelRefreshViewerPostfix` currently records evidence only; it does not call `RenderAnimalProgressOverlay`, so switching animals can refresh native rows without immediately repainting DTMAPI's progress row.
- `SetUnityText` hard-codes `fontSize = 18`, which matches the user's report that the row became smaller than desired.
- DLKsmapi reference `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\src\AnimalHusbandryProgressPlugin.cs` uses `helper.Experimental.Animals.ViewerRendering += OnAnimalViewerRendering` and calls `e.AddProgressBar(...)` inside that render event. In other words, DLKsmapi's reliable path adds the row during one managed viewer-rendering event rather than patching text after native UI has already appeared.

Classification:
- DTMAPI upgrade side: Animal viewer rendering API/hook timing should provide a single stable render event similar to DLKsmapi.
- Mod side: the migrated AnimalHusbandryProgress mod should consume that event and not rely on post-render text mutation.

Review status:
- Repeated bug. Requires root-cause fix, not another delayed screenshot/smoke-only evidence pass.

## 3. Y console right-click target and input isolation

User feedback, preserved:
- The right-click action text is not translated.
- Left-click gives one item correctly.
- When the official "player obtained item" toast exists, right-click gives the previously left-clicked item regardless of where the user right-clicks.
- Input and clicks leak through the Y console into gameplay: typing `b` opens the backpack, and left-clicking the console can use the equipped tool.

Code review:
- `ReflectedDebugConsoleUi.CreateItemCell` has a direct item-cell right-click path through `PointerDown`, but `Update` also has a global `Mouse1` fallback.
- The fallback calls `TryGiveTrackedRightClickTarget`, which gives the last tracked `rightClickTargetItem` if it was observed within 8 seconds. That can be a stale hover or the item selected by an earlier left-click.
- This explains the user-observed behavior: once the official item-gained toast or pointer/event timing interferes with the real right-click target, the fallback can give the old item.
- The right-click status uses the generic item-given message; there is no separate reviewed localization key for "right-click give 10" / "right click".
- `ConsumedInputThisFrame` only prevents DTMAPI's own `PollRegisteredInputButtons` from dispatching mod hotkeys.
- `DtmApiRuntime.RecordInputPressed` blocks DTMAPI mod input when `UI.BlocksGameplayHotkeys` is true, but it does not block Doloc Town's native input handlers.
- `CreateInput` uses a Unity input field callback but does not establish a native-game input suppression boundary. This matches `b` opening the backpack while typing in the console.

Classification:
- DTMAPI bootstrap UI side: remove or hard-constrain stale right-click fallback; route right-click from the actual pointer target; localize right-click result text.
- DTMAPI GameBridge/bootstrap side: add real gameplay input isolation while modal DTMAPI UI is open, covering keyboard and mouse actions at the native game input layer.

Review status:
- Repeated input/UI bug. Build and mouse-give smoke are insufficient because they did not test native input leakage or stale target behavior under official item toast timing.

## 4. Config menu example visibility

User feedback, preserved:
- A "配置菜单示例（开发者）" entry appears in the DTMAPI UI.
- This is good as a developer example, but it should be reviewed: is it built into DTMAPI, or is it an extra mod?

Code review:
- It is an extra test mod under `testmods/ConfigMenuExample`, not DTMAPI core.
- `testmods/ConfigMenuExample/manifest.json` declares `UniqueID = DTMAPI.ConfigMenuExample` and `EntryDll = ConfigMenuExample.dll`.
- `testmods/ConfigMenuExample/ModEntry.cs` registers an example config page through `IDtmConfigMenuApi`.
- `tools/scripts/install-to-game.ps1` only installs it when `-IncludeTestMods` is used, and backs up stale sample mods during normal install.

Classification:
- No core bug if this appears during development/test install.
- Packaging/install issue if it appears in a normal player-facing install that did not request test mods.

Review status:
- Confirmed: dev sample mod, not built-in DTMAPI runtime.

## Cross-cutting review conclusion

- `docs/updates/2026/20260605-0003-026-mine-yconsole-fixes.md` and `docs/debug/issues/ISSUE-007-20260605-mine-yconsole-026.md` are useful evidence records, but the user's latest manual QA is newer and exposes gaps not covered by those smoke checks.
- The next implementation goal should not restate these as generic "polish"; it should require code-path fixes and manual third-save evidence for:
  - Mine preview root/ghost scaling and every-2-hour production including sleep/load behavior.
  - Animal viewer single-pass render path, using DLKsmapi behavior only as reference.
  - Y console actual-target right-click and native input isolation.
  - ConfigMenuExample packaging check only if it appears outside dev installs.
