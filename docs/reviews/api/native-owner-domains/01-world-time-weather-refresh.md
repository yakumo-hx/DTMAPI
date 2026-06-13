# 01 - World Time Weather Refresh

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

World changes around time, weather, season, state refresh, weather changes, date jumps, and world-side refresh effects.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Not relevant | Workshop beauty docs focus on textures | No time/weather behavior API |
| Base content mod | Partial for resources/vegetation/fish constraints affected by world state | `032_*`, `036_*`, `050_*` docs | Content tables and constraints only |
| Advanced content mod | Partial for resource/platform examples | `025_*`, `028_*`, `038_*` docs | Does not grant date/weather mutation |
| Runtime behavior mutation | Not public | No official date jump/weather setter doc found | DTMAPI GameBridge only, experimental/debug |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Date/time query | `TimeArchiveData.dateNow`, `dateConfig`, `get_DayProcess`, `get_TotalDays`; `ArchiveDataHandle.get_DateNow`, `get_CurrentWeekDay`, `get_CurrentDayPeriodType` | `maps/Save_Load.md`; `GameData/TimeArchiveData.cs`; `ArchiveDataHandle.cs` | `TimeArchiveData` persisted state plus `ArchiveDataHandle` facade | Current date, day progress, weekday, day period | Medium if raw native DTOs leak | `IWorldTimeSnapshot` DTO | Found for query |
| Date jump / time pass | `ArchiveDataHandle.PassLongTime`, `PassTime`, `PassTimeNoControl`, `TrackBackTime`, `TraceBackTimeNoControl`, `UpdateDate`, `UpdateDateNoRender`; `TimeArchiveData.UpdateDate` | `maps/index/Save_Load-*.csv`; `ArchiveDataHandle.cs` | `ArchiveDataHandle` | Mutates global time and triggers refresh callbacks | High global lifecycle risk | Diagnostic/internal time advance probe with explicit side effects | Partial |
| Weather snapshot query | `TimeArchiveData.weather`, `get_WeatherMap`, `get_NextWeatherMap`, `QueryWeatherHistory`; `ArchiveDataHandle.get_GlobalWeatherInfo`, `get_CurrentWeatherInfo`, `get_CurrentWeatherType`; `Room.GetWeatherInfo` | `Save_Load` and `GameLoop_Scene` maps; `TimeArchiveData.cs`; `ArchiveDataHandle.cs`; `Room.cs` | `TimeArchiveData`, `ArchiveDataHandle`, current `Room` | Global and effective room weather snapshot | Medium if raw native DTOs leak | `IWeatherSnapshot` DTO | Found for query |
| Current-weather force | `ArchiveDataHandle.SetWeather`, `RefreshWeatherStatus`; `WeatherSystem.SetCurrentWeather`; `_OnWeatherChanged`, `_OnWeatherChangedNoRender`; `Room.SetWeatherInfo` | `ArchiveDataHandle.cs`; `WeatherSystem.cs`; `Room.cs` | `ArchiveDataHandle`, current weather system, current room | Forces current weather/render state | High global render/lifecycle risk | Diagnostic-only weather force probe | Partial |
| Forecast/history patch | `TimeArchiveData.weatherPatch`; `ArchiveDataHandle.PatchWeather`, `GetWeatherInfoOfDay`, `QueryWeatherHistory` | `TimeArchiveData.cs`; `ArchiveDataHandle.cs` | `TimeArchiveData` patch/history maps | Patches forecast/history data separate from current weather | High save/restore risk | Experimental forecast patch with restore policy | Partial |
| Season/month state | `TimeArchiveData.get_SeasonProto`, `get_SeasonIndex`, `RetrieveSeasonProto`; `ArchiveDataHandle.get_Season`, `_OnSeasonChanged`, `_OnSeasonChangedNoRender` | `Save_Load` maps; `ArchiveDataHandle.cs` | `TimeArchiveData` and private archive lifecycle callbacks | Season query and season-change side effects | Mutation high | `ISeasonSnapshot`, season changed event | Partial |
| World refresh lifecycle | `ArchiveDataHandle._BeforeTimePass`, `_AfterTimePass`, `_OnHourChanged`, `_OnDayChanged`, `_OnDailyRefresh`, `_OnWeeklyRefresh`, `_OnMonthlyRefresh`, `_OnYearlyRefresh`, `_OnWeatherChanged`, `_OnSeasonChanged`, `_OnEnterRoom`, `_OnExitRoom`, `_UpdatePerSec`, `_UpdatePerTU`, no-render variants | `maps/index/Save_Load-calls.csv`; `ArchiveDataHandle.cs` | `ArchiveDataHandle` | Central dispatcher for time/weather/season/room refresh | Private/internal hook risk | Read-only events after native callback proof | Partial |

## PassTime Ordering

- Controlled routes: `PassTime` and `PassLongTime` wrap time advance in native transition/control state and should be treated as high-risk diagnostic probes.
- No-render route: `PassTimeNoControl` advances through no-render updates, date refresh, and no-render lifecycle callbacks without proving safe public mutation.
- Observed order to re-check before implementation: before-time-pass, per-time-unit/no-render updates, date no-render update, no-render refresh callbacks, after-time-pass, then caller callback.
- `Dungeon.CurrentWeatherInfo` and custom/independent weather state exist, but active independent dungeon-weather caller ownership is not proven.

## API Translation Notes

- Start with read-only snapshots for time, weather, and season.
- Date jump/weather mutation must stay `Diagnostic` or `Experimental` until sleep, save/load, room transition, render/no-render, and multi-mod ownership are proven.
- Public DTOs should normalize date, period, weather id, season id, and room-weather override without exposing native config objects.
- Round 3 confidence: time snapshot query 92; global/effective room weather snapshot 84; current-weather force diagnostic 76; no-render pass route internal 80; full refresh ordering still incomplete for stable API 88.

## Blockers And Follow-Up

- Date jump must prove machine, crop, NPC, animal, resource, vegetation, mission, and weather refresh ordering.
- Weather setter needs restore policy and room/dungeon divergence handling.
- No official Workshop doc supports arbitrary date/weather mutation.

## Evidence Checked

Maps: `Save_Load.md`, `GameLoop_Scene.md`, `Resource_Gathering.md`, `Action_Interaction.md`.
Classes: `TimeArchiveData`, `ArchiveDataHandle`, `Room`, `Dungeon`, `FarmArchiveData`, `DungeonArchiveData`, `WeatherSystem`.
