# 03 - Teleport API

Scope: `ITeleportDebugApi.GetDestinations`, `GetCurrentSnapshot`, `Teleport`, and CSV export.

## 1. Files read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/INDEX.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/GameLoop_Scene.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/GameLoop_Scene.md`
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`

## 2. Functions read

- `ITeleportDebugApi.GetDestinations/GetCurrentSnapshot/Teleport/GetStatus`, `ExperimentalGameBridge.cs:81`.
- `TeleportDestination`, `TeleportSnapshot`, and `TeleportResult`, `ExperimentalGameBridge.cs:367`.
- `DolocTownExperimentalBridgeApi.GetDestinations`, `DolocTownExperimentalBridgeApi.cs:3875`.
- `GetCurrentSnapshot`, `DolocTownExperimentalBridgeApi.cs:3888`.
- `Teleport`, `DolocTownExperimentalBridgeApi.cs:3896`.
- `ExportDestinationsCsv`, `DolocTownExperimentalBridgeApi.cs:3950`.
- `BuildTeleportDestinations`, `DolocTownExperimentalBridgeApi.cs:11345`.
- `AddTeleportDestination`, `DolocTownExperimentalBridgeApi.cs:11385`.
- `ResolveMarkPoint`, `DolocTownExperimentalBridgeApi.cs:11410`.
- `GetInitMarkPointId`, `DolocTownExperimentalBridgeApi.cs:11417`.
- `IsWhitelistedMarkPoint`, `DolocTownExperimentalBridgeApi.cs:11425`.
- `BuildTeleportSnapshot`, `DolocTownExperimentalBridgeApi.cs:11537`.
- `DolocTownGameBridge.StartVehicleEdgeTransitionForSmoke`, `DolocTownGameBridge.cs:2999`.

## 3. Call graph

```text
GetDestinations()
  -> BuildTeleportDestinations()
     -> DolocAPI.gameManager.gameInitConfig.initMarkPoint
     -> DolocConfig table TbStation.DataList -> station MarkPointId
     -> DolocConfig table TbMarkPoint.DataList -> whitelisted key marks
     -> ResolveMarkPoint via TbMarkPoint.GetOrDefault

Teleport(owner, destinationId)
  -> GetDestinations whitelist lookup
  -> DolocAPI.DoTransport(markPointId, null, true, false, false)
  -> snapshot after request
```

## 4. Function body findings

- Destination enumeration is whitelist-first. It includes farm init mark, station mark points, and `TbMarkPoint` entries whose mark/room ids contain selected keywords (`DolocTownExperimentalBridgeApi.cs:11348`, `:11352`, `:11366`, `:11425`).
- The public `Teleport` path refuses any destination not returned by `GetDestinations` or missing a `MarkPointId` (`DolocTownExperimentalBridgeApi.cs:3903`).
- Execution reflects `DolocAPI.DoTransport` with a 5-parameter shape including `string` mark point and three booleans (`DolocTownExperimentalBridgeApi.cs:3911`).
- The result only records `AfterRequest`, not a full after-transition completion event (`DolocTownExperimentalBridgeApi.cs:3927`). For ordinary gameplay this is important: accepted request is not the same as all room systems settled.
- CSV export writes destination evidence files for manual naming (`DolocTownExperimentalBridgeApi.cs:3955`), but that is diagnostics, not runtime owner reach.
- Vehicle smokes reuse teleport to get outdoors or test edge transitions (`DolocTownGameBridge.cs:3004`), confirming the API is used as debug infrastructure.

## 5. Native owner verdict

`OK/Watch`. The current debug teleport reaches a native transport owner through `DolocAPI.DoTransport` and avoids arbitrary coordinate writes. The destination list reaches native config tables for station/mark-point metadata.

Reverse/research evidence: reverse maps list `DolocAPI.EnterRoom` direct candidates and `DolocAPI.QueryRoom` (`23465763.../maps/GameLoop_Scene.md:195`, `:196`, `:222`) plus `TbMarkPoint` and `TbStation` metadata rows (`:465`, `:470`). DolocPlus research notes list `DolocAPI.DoTransport(markPointId)` as a confirmed trainer-call path (`research-DolocPlus-deep-dive-20260607.md:557`). `DoTransport` itself was not found as a map row in the map search, so the DTMAPI code is the direct implementation evidence.

## 6. Ordinary mod usability

`debug-only`. The current API is safe enough for Y-console/debug whitelist use, not for ordinary mod quest, dungeon, cutscene, or scripted movement systems.

## 7. Concrete failure modes

- A mod attempting arbitrary coordinates cannot use this API; non-whitelisted destinations return `not-whitelisted`.
- A mod treating `Success=true` as "room fully settled" can race follow-up logic, because `AfterRequest` is captured immediately after native request acceptance.
- Dungeon or direct room-position destinations are not exposed; mods needing `EnterRoom(roomId, position)` semantics are outside this contract.
- If `DoTransport` signature changes or is unavailable, the API fails with `missing-dotransport`; there is no fallback to `EnterRoom`.
- Mark-point whitelist naming can omit valid game destinations and include only a curated debug subset.

## 8. Minimal rebuild direction

- Keep `ITeleportDebugApi` debug-only with whitelist wording.
- For ordinary mods, split transport into stable owners: mark-point transport, room-position transport, dungeon transport, completion event, and post-transition state refresh.
- Add explicit "request accepted" versus "transition completed" result types before allowing scripted mod flows.
- Use reverse-map candidates `EnterRoom`, `QueryRoom`, `TbMarkPoint`, and `TbStation` as the starting native-owner map for a future non-debug API.

## 9. Evidence gaps

- No new smoke was run in this round.
- `DolocAPI.DoTransport` is confirmed by DTMAPI reflection and research notes, but not by a reverse map row found in this audit.
- The audit did not verify completion callbacks or post-transition room-system readiness.
- Dungeon-specific and arbitrary-coordinate teleport owners were only identified as missing; no code-level adapter exists in DTMAPI today.
