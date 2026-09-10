# 20260603-0008 Second Motor Mail Status Smoke

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, specifically task E: the second-motor mod should be player-facing as an alternate flying motor, expose a DTMAPI settings/status page, and use the game's official mail path to deliver the key when a save lacks it.

## Summary

- Added experimental `IMailDeliveryApi` / `MailItemDeliveryRequest` / `MailItemDeliveryResult`.
- Implemented the mail bridge in `DTMAPI.GameBridge.DolocTown` through native `DolocAPI.SendItemAsEmail`.
- Added duplicate guards before sending: native backpack count plus scan of unclaimed item-mail attachments.
- Updated `DTMAPI.SecondMotorMod` to bind the mail API on save load and request one `dtmapi_second_motor_key` through the official mail template when needed.
- Renamed the player-facing package to `DTMAPI 异色飞行摩托` / `DTMAPI Alternate Flying Motor`, updated the official-local package metadata to `0.2.3-dtmapi`, and changed the item title to `飞行摩托钥匙（异色）`.
- Added a DTMAPI title settings status page for SecondMotor and extended the title-settings screenshot smoke to capture that page.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/SecondMotorMod/ModEntry.cs`
- `testmods/SecondMotorMod/manifest.json`
- `testmods/SecondMotorMod/official-info.json`
- `testmods/SecondMotorMod/README.md`
- `testmods/SecondMotorMod/i18n/schinese.json`
- `testmods/SecondMotorMod/i18n/english.json`
- `testmods/SecondMotorMod/Content/02 DTMAPI Second Motor/item_tbitem.json`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoExerciseVehicle -AutoExitAfterSecondsOverride 105 -TimeoutSeconds 290`
- Result: third-save Steam smoke passed with `VehicleSecondMotor=true`, clean exit, and no fatal popup; the new dual-visible probe explicitly reported `dualVisibleAfterKey=False`, so simultaneous visible dual-motor UX remains a blocker rather than silently passing.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 0 -SkipBuild -AutoOpenTitleSettingsMenu -AutoExitAfterSecondsOverride 60 -TimeoutSeconds 180`
- Result: title-settings Steam smoke passed, captured the SecondMotor config/status page, exited cleanly, and left no `DolocTown.exe`.

## Evidence Links

- Final vehicle smoke: `docs/debug/evidence/GAME-SMOKE/20260603-050208`.
- Vehicle logs: `DTMAPI-latest.log` records `Mail item delivery ... sent=True ... pendingMail=1`, `Mail.ItemDelivery = experimental`, `Second motor key intercepted ... success=True`, `Second motor ride-on observed`, `Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot`, `Motor vehicle original summon OK`, `Smoke exercise VehicleSecondMotor dual-visible probe originalVisible=False ... secondVisible=True ... dualVisible=False`, and `Smoke exercise VehicleSecondMotor OK ... dualVisibleAfterKey=False`.
- Vehicle result: `result.json` has `VehicleSecondMotor=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- Vehicle exit evidence: `process-check.txt` says no `DolocTown.exe`.
- Final title-settings smoke: `docs/debug/evidence/GAME-SMOKE/20260603-045115`.
- SecondMotor status screenshot: `DTMAPI-evidence/UI-004/20260603-045153/title-settings-config-second-motor.png`.
- Title-settings logs: `DTMAPI-latest.log` records `SecondMotor 配置/状态页已注册` and `Smoke.TitleSettingsConfigPageScreenshot.second-motor = verified`.
- Title-settings exit evidence: `process-check.txt` says no `DolocTown.exe`.

## Known Facts And Rejected Hypotheses

- Official code research found `DolocAPI.SendItemAsEmail(string itemName, int count, string emailName = null, string content = null, string sender = null, string templateName = "send_item_template")`; this is the correct native chain for item-mail delivery.
- Current decompiled `EmailManager.SendItemAsEmail` ignores custom `emailName`, `content`, and `sender` when constructing the mail and instead uses the template `EmailInfo`; DTMAPI therefore treats the API as template-based delivery and records that limitation in `IMailDeliveryApi.GetStatus()`.
- The duplicate guard does not directly mutate save data. It uses native `CountItem` and read-only reflection over `EmailManager.emails` to detect unclaimed `RewardItem` attachments.
- The vehicle smoke proves the DTMAPI key path, clone summon, ride on/off, original motor restore, original motor summon, and 2x speed tuning. The added dual-visible probe now proves the current implementation does **not** keep the original motor visible at the same time after the DTMAPI key summon (`originalVisible=False`, `secondVisible=True`), so manual dual-visible/dual-click UX is a real blocker, not merely missing evidence. The base game has a singleton-oriented `DolocAPI.Motor` / `AgentControllerState.motorController` model, so the public API remains experimental.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only mail delivery, remove `IMailDeliveryApi` registration and the SecondMotor `TryDeliverKeyMail` call; the existing key/manual debug give path will continue to work.
- If rolling back only the status-page screenshot strictness, remove the `DTMAPI.SecondMotorMod` stage from `UpdateTitleSettingsConfigEvidenceScreenshots`.

## Follow-up

- Investigate whether original-motor visibility can be restored after the second clone is summoned without corrupting `DolocAPI.Motor` singleton state. If not, document an alternate product design instead of claiming the user-requested simultaneous dual-motor behavior.
- If future game builds honor `emailName/content/sender`, expose them as real custom-mail fields; for build `23465763_workshop_38581E`, keep item mail documented as template-based.
