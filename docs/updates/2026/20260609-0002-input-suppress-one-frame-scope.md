# 20260609-0002 Input Suppress One-Frame Scope

## Metadata

- Update ID: 20260609-0002
- Date: 2026-06-09
- Status: verified
- Source: Active goal: clarify `IInputHelper.Suppress` scope without touching native input hooks
- Owner: Codex

## Summary

- Changed Core input frame cleanup so `Input.ClearFrame` clears both pressed and suppressed helper state.
- Added unit coverage `Suppress_OneFrame_ClearsAfterUpdate`.
- Documented that `IInputHelper.Suppress` is one-frame DTMAPI helper state only.
- Kept the public API matrix `Input` row at `Experimental`.
- Did not change GameBridge, Harmony patches, native input hooks, Doloc Town tool/item/menu handling, or the smoke harness.

## Changed Files

- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/README.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260609-0002-input-suppress-one-frame-scope.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Game smoke was not run because this update changes Core helper frame state, tests, and documentation only. It does not change GameBridge, native input hooks, smoke harness behavior, or installed game behavior.

## Evidence

- Unit test `Suppress_OneFrame_ClearsAfterUpdate` verifies:
  - `WasPressed("F10")` is visible before `DtmApiRuntime.Update()` and cleared after update.
  - `GetSuppressedButtons()` includes `F10` before update and is cleared after update.
  - `IsDown("F10")` remains true across frame cleanup until `RecordInputReleased("F10")`.
- `docs/api/public-api-matrix.md` keeps Input `Experimental` and states suppression is not native input isolation.
- `docs/debug/regressions/smoke-matrix.md` adds `INPUT-SUPPRESS-ONEFRAME` with the Release build/unit evidence.

## Known Facts And Rejected Hypotheses

- Known fact: the 2026-06-07 native-owner audit found the old `Suppress` set was stored but not consumed by DTMAPI polling or native input.
- Known fact: the current implementation still does not block Doloc Town native tool, item, menu, or UI input paths.
- Known fact: Y-console modal/native input isolation remains a separate GameBridge path and does not read the public suppress set.
- Rejected hypothesis: clearing the helper set after one frame is enough to claim native input suppression. It only fixes stale helper state.
- Rejected hypothesis: this Core helper cleanup requires a new native input hook. The active scope explicitly avoids native hook work.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`
- Input native-owner audit: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/01-input-helper-suppress.md`
- Remaining API audit: `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/01-framework-gameloop-event-input.md`
- Debug regression row: `docs/debug/regressions/smoke-matrix.md#dtmapi-smoke-matrix`

## Rollback Notes

- Revert `InputService.ClearFrame` to clearing only pressed state and remove `Suppress_OneFrame_ClearsAfterUpdate` if a future native input suppression design intentionally gives suppressions a longer owner-managed lifetime.
- Do not cite this record as proof of native input action suppression; a future GameBridge-native isolation design needs its own hook map, debug notes, smoke evidence, and update record.

## Follow-Up

- Any future promotion beyond `Experimental` should split helper-only suppression from native action isolation and prove UI conflict behavior with targeted game/manual evidence.
