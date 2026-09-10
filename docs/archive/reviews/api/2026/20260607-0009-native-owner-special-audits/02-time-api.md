# 02 - Time API

Scope: `ITimeDebugApi.GetState`, `SkipToNextWeatherPeriod`, and status DTOs.

## 1. Files read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/INDEX.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Save_Load.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/GameLoop_Scene.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md`

## 2. Functions read

- `ITimeDebugApi.GetState/SkipToNextWeatherPeriod/GetStatus`, `ExperimentalGameBridge.cs:99`.
- `TimeDebugState` and `TimeSkipResult`, `ExperimentalGameBridge.cs:435`.
- `DolocTownExperimentalBridgeApi.GetState(ITimeDebugApi)`, `DolocTownExperimentalBridgeApi.cs:4047`.
- `DolocTownExperimentalBridgeApi.SkipToNextWeatherPeriod`, `DolocTownExperimentalBridgeApi.cs:4052`.
- `DolocTownExperimentalBridgeApi.GetStatus(ITimeDebugApi)`, `DolocTownExperimentalBridgeApi.cs:4110`.
- `GetTimeDebugState`, `DolocTownExperimentalBridgeApi.cs:11599`.
- `GameMinutesToSeconds`, `DolocTownExperimentalBridgeApi.cs:11633`.
- `GetNextDebugWeatherPeriodTarget`, `DolocTownExperimentalBridgeApi.cs:11648`.
- `InvokeWakeUp`, `DolocTownExperimentalBridgeApi.cs:11678`.
- `InvokeNativePassTime`, `DolocTownExperimentalBridgeApi.cs:11111`.
- `DolocTownGameBridge.TryExerciseDebugTimeForSmoke`, `DolocTownGameBridge.cs:2368`.

## 3. Call graph

```text
ITimeDebugApi.SkipToNextWeatherPeriod(owner)
  -> GetTimeDebugState()
     -> WeatherDebugState from weather API
     -> DolocAPI.archiveHandle.DateNow.Minute
  -> DolocAPI.archiveHandle
  -> DolocAPI.GlobalParameter.Hour2Min / Day2Hour / GameMinutes2Secs
  -> archiveHandle.PassTimeNoControl(seconds, wake, true)
     -> wake callback -> DolocAPI.OnWakeUp(false, true, false)
  -> GetTimeDebugState() after jump
```

Smoke path:

```text
DolocTownGameBridge.TryExerciseDebugTimeForSmoke
  -> SkipToNextWeatherPeriod three times
  -> Smoke.DebugTime verified with ITimeDebugApi -> ArchiveDataHandle.PassTimeNoControl
```

## 4. Function body findings

- `SkipToNextWeatherPeriod` only targets 06:00, 18:00, or 24:00 weather-period boundaries. It is not a general "advance any amount of time" public API (`DolocTownExperimentalBridgeApi.cs:4068`, `:11648`).
- The function reads conversion constants from `DolocAPI.GlobalParameter` and prefers native `GameMinutes2Secs(float)` when available (`DolocTownExperimentalBridgeApi.cs:4064`, `:11633`).
- It locates `ArchiveDataHandle.PassTimeNoControl` by signature first, then by any public method named `PassTimeNoControl` with at least one parameter (`DolocTownExperimentalBridgeApi.cs:4073`).
- It passes a wake callback that calls `DolocAPI.OnWakeUp(false, true, false)` if present (`DolocTownExperimentalBridgeApi.cs:4079`, `:11678`).
- It does not check machine-specific catch-up, crop growth, NPC schedules, UI prompts, or player consent. Those systems may react because native time has advanced, but this API does not expose a transaction boundary for ordinary mods.
- The smoke helper exercises three transitions and records before/after snapshots; it is debug evidence, not an ordinary-mod gameplay contract (`DolocTownGameBridge.cs:2371`, `:2389`).

## 5. Native owner verdict

`Partial/Watch`. The API reaches the native time owner used by current evidence: `ArchiveDataHandle.PassTimeNoControl` plus `DolocAPI.OnWakeUp`. It does not own or verify all systems affected by time jumps.

Reverse/map evidence: `Save_Load.md` confirms `DolocTown.GameData.ArchiveDataHandle` exists in both reverse builds (`23465763.../maps/Save_Load.md:32`, `23249387.../maps/Save_Load.md:32`). Map searches for `PassTimeNoControl` did not produce a public map row, so the direct evidence for that member is the DTMAPI reflection call and existing smoke/update records, not a reverse-map signature row.

## 6. Ordinary mod usability

`debug-only`. Ordinary mods should not use this as a stable scheduler or production/crop/NPC clock API.

## 7. Concrete failure modes

- A mod using this for production timing can desync DTMAPI sidecar machine loops from native machine/electric/fuel owners if it expects every subsystem to catch up in one atomic transaction.
- A mod using it for crop or animal pacing can trigger native weather/day callbacks without exposing enough before/after hooks to repair its own state.
- A mod can surprise the player by jumping to 06:00/18:00/24:00 because the API is a debug command, not a consented gameplay time skip.
- If `PassTimeNoControl` signature changes, the fallback may find an overload with at least one parameter but incompatible semantics; failure is caught as `TimeSkipFailed`, not adapted.
- If `DolocAPI.OnWakeUp` is missing, `InvokeWakeUp` silently no-ops, so time may advance without the expected wake-up refresh.

## 8. Minimal rebuild direction

- Keep `ITimeDebugApi` debug-only.
- For ordinary mods, define a separate scheduler contract with phases: request, native pass-time, native wake callback, post-catch-up, and failure rollback.
- Make affected owner maps explicit: weather, crop/plant, machines, electricity, animal/NPC schedule, UI/input lock, and save transaction.
- Require smoke evidence for sleep, Y-console skip, scene-in/out refresh, and machine/crop side effects before exposing a non-debug API.

## 9. Evidence gaps

- No new smoke was run in this round.
- Reverse maps confirm `ArchiveDataHandle` but not a line-level `PassTimeNoControl` signature row; the direct member evidence is DTMAPI reflection plus existing smoke records.
- This audit did not prove every native subsystem invoked by `OnWakeUp`; it only verified that DTMAPI calls the owner candidates it currently names.
- There is no per-subsystem rollback or transaction evidence for ordinary-mod use.
