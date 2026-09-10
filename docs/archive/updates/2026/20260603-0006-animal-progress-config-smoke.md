# 20260603-0006 Animal Progress Config Smoke

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, specifically task B: stabilize the AnimalHusbandryProgress hidden-produce row and make the config color controls match the manual feedback.

## Summary

- Hid the AnimalHusbandryProgress direct hex input unless the `Custom` color preset is selected.
- Removed the title-settings renderer's right-side color-preset value text, so the swatch row no longer shows `Orange` beside the controls.
- Made the far-right `Custom` swatch visually distinct from the ordinary color presets with a `+` marker.
- Rechecked the real AnimalPanel path in the third save and the title settings config page with screenshots.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `testmods/AnimalHusbandryProgressMod/ModEntry.cs`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoOpenAnimalPanel -AutoExitAfterSecondsOverride 75 -TimeoutSeconds 220`
- Result: third-save Steam smoke passed with `AnimalViewerUi=true`, `SaveLoaded=true`, `ProcessExited=true`, and no fatal popup.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 0 -SkipBuild -AutoOpenTitleSettingsMenu -AutoExitAfterSecondsOverride 45 -TimeoutSeconds 160`
- Result: title settings smoke passed, captured the AnimalHusbandryProgress config page, exited cleanly, and left no `DolocTown.exe`.

## Evidence Links

- AnimalPanel smoke: `docs/debug/evidence/GAME-SMOKE/20260603-040354`.
- Animal evidence summary: `docs/debug/evidence/GAME-SMOKE/20260603-040354/DTMAPI-evidence/ANIMAL-001/20260603-040437/summary.txt`.
- Animal delayed screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-040354/DTMAPI-evidence/ANIMAL-001/20260603-040437/animal-viewer-ui-delayed.png`.
- Animal logs: `DTMAPI-latest.log` records `Animal viewer progress native-like UI overlay rendered rows=1, 羊毛脂 0/100` and `Animal viewer UI delayed screenshot OK`.
- Config smoke: `docs/debug/evidence/GAME-SMOKE/20260603-041053`.
- Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-041053/DTMAPI-evidence/UI-004/20260603-041133/title-settings-config-animal-husbandry-progress.png`.
- Config logs: `DTMAPI-latest.log` records `Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress = verified`.
- Exit evidence: both `process-check.txt` files say no `DolocTown.exe`; fatal-window checks say no fatal instance popup.

## Known Facts And Rejected Hypotheses

- Retained title config smoke `GAME-SMOKE/20260603-040548` showed the previous defect: the swatch row still exposed a right-side `Orange` text label and a non-Custom `FF942E` input.
- Hiding the hex input through `AddTextOption(..., canEdit, isVisible)` is enough for the default/non-Custom screenshot; selecting and saving `Custom` will expose the direct hex field through the existing config transaction model.
- The AnimalPanel smoke is automated evidence, not a substitute for final user manual multi-switch acceptance. It proves the current real viewer path renders the native-style `羊毛脂 0/100` row and captures a delayed screenshot without the old `隐藏产物` label.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only the config visual change, restore the color-preset value text in `ReflectedTitleMenuSettingsUi` and remove the `isVisible` callback from the AnimalHusbandryProgress hex input registration.

## Follow-up

- Continue remaining 0.2.3 blockers: AutoFishing movement/minigame proof, ActionSpeed strong auto-fill frequency proof, and second-motor dual-key/dual-instance 0.2.3 manual proof.
- A human manual pass can still re-open/switch multiple animals to supplement the automated AnimalPanel smoke before the whole `/goal` is marked complete.
