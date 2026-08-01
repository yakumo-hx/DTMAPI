# 20260708-0002 - FullKnown Long Title Cycle Validation

Status: runtime-verified / issue-010-main-menu-pressure-success / long-term-gameplay-gc-open

## Source Request

After the hotkey edge follow-up and 50-key pressure validation, the user requested one broader regression pass using the oldest long-run shape: all DTMAPI-known mods enabled, Steam launch, one hour on the title screen, then repeated save entry/return cycles. The user shortened the old five-minute cycle interval to one minute because earlier interrupted-idle evidence showed frequent save entry can partially reset pressure.

## Changed Files

- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260708-0002-fullknown-long-title-cycle-validation.md`
- `docs/updates/INDEX.md`

## Validation

Runtime smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260708-075829`.

Command shape:

```powershell
tools/scripts/run-game-smoke.ps1 `
    -SaveSlot 3 `
    -UseSteam `
    -IncludeHookProbe `
    -OfficialModProfile FullKnown `
    -AutoExerciseSaveLoadCycle `
    -SaveLoadCycleCount 10 `
    -SaveLoadCycleInitialTitleIdleSeconds 3600 `
    -SaveLoadCycleIntervalSeconds 60 `
    -SaveLoadCycleInSaveSeconds 5 `
    -TimeoutSeconds 5400 `
    -FatalWindowCrashDumpGraceSeconds 30 `
    -FatalWindowProcessDumpMode DbgHelpFull `
    -FatalWindowPostCloseCrashDumpWaitSeconds 60 `
    -SmokeInputPollingDiagnostics `
    -SkipBuild
```

Result fields:

- `RunStatus=Passed`
- `StartupLog=Passed`
- `GameLaunched=Passed`
- `SaveLoaded=Passed`
- `SaveLoadCycle=Passed`
- `SmokeInputPollingDiagnostics=Passed`
- `NoFatalInstanceWindow=Passed`
- `ProcessExited=Passed`

The startup analyzer classified the run as `NormalDtmapiStartup`, not Steam launch blocking. `OfficialModProfile=FullKnown` was applied and restored. The runtime lock was released and no `DolocTown.exe` process remained.

Input diagnostics stayed on the rebuilt scoped input path: `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, `getKeyCalls=0`, `registeredButtonsMax=7`, and `titleButtonsPolled=0`. No virtual input pressure was used.

## Classification

This validates the strongest post-fix main-menu route so far: Steam launch, `FullKnown`, one uninterrupted title hour, and 10 post-idle save-load cycles at one-minute intervals. ISSUE-010's main-menu/title-idle input-pressure path can be marked successful for this tested full-known profile.

This does not close ISSUE-010. The broader long-term gameplay/native GC crash class remains open because the run does not cover arbitrary long play, room churn, extended fishing loops, equipment/UI usage, player-driven gameplay, or future crash packages.

## Rollback Notes

No runtime or source code changed in this update. If future evidence regresses, use `GAME-SMOKE/20260708-075829` as the successful post-hotkey baseline and compare against the earlier fatal full-profile evidence from 2026-07-05/06.

## Follow-Up

Future long-play validation should keep Unity crash evidence, title-return/load breadcrumbs, input diagnostics, and lifecycle counters enabled. Do not mark the whole long-term GC class solved from this main-menu route alone.
