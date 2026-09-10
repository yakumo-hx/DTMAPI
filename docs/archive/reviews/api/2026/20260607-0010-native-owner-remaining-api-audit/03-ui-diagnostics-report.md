# 03 - DTMAPI UI, Diagnostics, And Report APIs

## Scope

Audited remaining UI and diagnostics surfaces:

- `IUiHelper.OpenDtmApiStatusPage`
- `IUiHelper.OpenModListPage`
- `IUiHelper.OpenConfigPage`
- `IUiHelper.OpenErrorPage`
- `IUiHelper.OpenHookStatusPage`
- `IUiHelper.OpenLogsPage`
- `IUiHelper.ExportLogs`
- `IDiagnosticsHelper.GetErrors`
- `IDiagnosticsHelper.GetHookStatuses`
- `IDiagnosticsHelper.ExportLogs`
- `IDiagnosticsHelper.RecordEvidence`
- `DtmErrorInfo`, `HookStatusInfo`, and report semantics

## Files read

- `docs/api/public-api-matrix.md`: lines 54-63.
- `src/DTMAPI.Abstractions/Helpers.cs`: lines 71-109.
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`: lines 422-511.
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`: lines 25-100 and 126-132.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`: lines 223-242 and 328-357.
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`: lines 93-130, 560-608.
- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`: lines 35-82 and 374-400.
- `docs/hook-map/README.md`: lines 314-336 and 339-353.
- `docs/debug/regressions/smoke-matrix.md`: lines 53-59.

## Functions read

- `DTMAPI.Abstractions.IUiHelper.OpenDtmApiStatusPage`: `src/DTMAPI.Abstractions/Helpers.cs:73`.
- `DTMAPI.Abstractions.IUiHelper.OpenModListPage`: `src/DTMAPI.Abstractions/Helpers.cs:74`.
- `DTMAPI.Abstractions.IUiHelper.OpenConfigPage`: `src/DTMAPI.Abstractions/Helpers.cs:75`.
- `DTMAPI.Abstractions.IUiHelper.OpenErrorPage`: `src/DTMAPI.Abstractions/Helpers.cs:76`.
- `DTMAPI.Abstractions.IUiHelper.OpenHookStatusPage`: `src/DTMAPI.Abstractions/Helpers.cs:77`.
- `DTMAPI.Abstractions.IUiHelper.OpenLogsPage`: `src/DTMAPI.Abstractions/Helpers.cs:78`.
- `DTMAPI.Abstractions.IUiHelper.ExportLogs`: `src/DTMAPI.Abstractions/Helpers.cs:79`.
- `DTMAPI.Core.Services.UiRuntimeService.OpenDtmApiStatusPage`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:465`.
- `DTMAPI.Core.Services.UiRuntimeService.OpenCustomMenu`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:477`.
- `DTMAPI.Core.Services.UiRuntimeService.ExportLogs`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:494`.
- `DTMAPI.Core.Services.UiRuntimeService.Open`: `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:502`.
- `DTMAPI.Core.Diagnostics.DiagnosticsService.GetErrors`: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25`.
- `DTMAPI.Core.Diagnostics.DiagnosticsService.GetHookStatuses`: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:31`.
- `DTMAPI.Core.Diagnostics.DiagnosticsService.ExportLogs`: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:59`.
- `DTMAPI.Core.Diagnostics.DiagnosticsService.RecordEvidence`: `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.ExportLogs`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:223`.
- `DTMAPI.Core.Runtime.DtmApiRuntime.CreateSnapshot`: `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:231`.
- `DTMAPI.BepInExBootstrap.ReflectedTitleMenuSettingsUi.Update`: `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:93`.
- `DTMAPI.BepInExBootstrap.ReflectedTitleMenuSettingsUi.RenderLogs`: `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:598`.
- `DTMAPI.BepInExBootstrap.ReflectedImGuiOverlay.Render`: `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs:35`.
- `DTMAPI.BepInExBootstrap.ReflectedImGuiOverlay.DrawLogs`: `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs:392`.

## Call graph

```text
UI helper pages
  ordinary mod helper.UI.Open*
    UiRuntimeService.OpenDtmApiStatusPage/OpenModListPage/OpenConfigPage/...
      ActiveMenuId + IsOpen state
      ReflectedTitleMenuSettingsUi title page or ReflectedImGuiOverlay in-game overlay
      DTMAPI page renderer reads runtime.Mods, runtime.Diagnostics, ConfigMenu pages

Export logs / reports
  helper.UI.ExportLogs or helper.Diagnostics.ExportLogs
    DtmApiRuntime.ExportLogs
      DiagnosticsService.ExportLogs
        copies DTMAPI latest log, BepInEx latest log, Unity Player.log when present
        writes summary.txt and latest-report.txt
        creates dtmapi-report-*.zip

Diagnostics read APIs
  helper.Diagnostics.GetErrors/GetHookStatuses
    DiagnosticsService snapshots internal lists/dictionaries
    UI pages and reports render those snapshots

RecordEvidence
  helper.Diagnostics.RecordEvidence
    writes evidence summary file
    copies latest DTMAPI log when present
```

## Function body findings

- `UiRuntimeService` is the state owner for DTMAPI menus. Each `Open*` method just calls a private `Open(menuId)`, sets `IsOpen`, `ActiveMenuId`, and status message state, and blocks gameplay hotkeys/mod updates based on the menu id (`src/DTMAPI.Core/Services/WorkshopContentInputUi.cs:422-511`).
- `OpenConfigPage` is a DTMAPI menu request. The title settings UI renders config pages only in title/home context; the in-game overlay can render DTMAPI overlay pages if drawing is available, but this is still DTMAPI UI, not official Doloc Town settings ownership (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:93-130`, `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs:35-82`).
- `ReflectedTitleMenuSettingsUi.Update` hides title settings UI outside the title home state and records status only when initialized/visible. This means ordinary mods cannot rely on config UI opening in arbitrary game states (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:93-130`).
- `RenderLogs` and the overlay `DrawLogs` both call `runtime.UI.ExportLogs()`, which delegates into the diagnostics report zip writer (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:598-608`, `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs:392-400`).
- `DiagnosticsService.GetErrors` and `GetHookStatuses` return snapshots, not live mutable references (`src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:25-35`).
- `DiagnosticsService.ExportLogs` creates a timestamped report directory, copies known log files if present, writes `summary.txt`, writes `latest-report.txt`, zips the report directory, and returns the zip path (`src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:59-75`).
- `RecordEvidence` writes an evidence summary and copies the latest DTMAPI log into the named evidence path. It does not capture screenshots or prove game smoke by itself (`src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:77-85`).
- `BuildSummary` records DTMAPI version, time, latest log path, errors, and hook statuses (`src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:87-100`).

## Native owner verdict

| API group | Verdict | Native owner reached |
| --- | --- | --- |
| UI page open helpers | DTMAPI-only | DTMAPI title/reflected overlay UI owns rendering; official enablement is respected but not controlled. |
| `ExportLogs` | DTMAPI-only | Report zip writer owns DTMAPI/BepInEx/Unity log collection. |
| `GetErrors` / `GetHookStatuses` | DTMAPI-only | DiagnosticsService stores and snapshots DTMAPI error/hook records. |
| `RecordEvidence` | DTMAPI-only | Writes local DTMAPI evidence files; no native/game capture owner. |

## Ordinary mod usability

- UI helpers: 普通 mod 可用 as DTMAPI UI requests; keep `experimental`.
- `IDiagnosticsHelper.GetErrors`, `GetHookStatuses`, and `ExportLogs`: 普通 mod 可用; keep `stable`.
- `IDiagnosticsHelper.RecordEvidence`: 普通 mod 可用 for DTMAPI evidence attachments; keep `experimental`.

## Concrete failure modes

1. A mod that treats `OpenConfigPage` as an official in-game settings panel can silently show no usable page outside the title/home context because the reflected title UI hides itself away from title state.
2. A mod that opens DTMAPI overlay pages during gameplay may block gameplay hotkeys or mod updates through `UiRuntimeService` menu state; it should not assume the page is passive.
3. A mod that treats `ExportLogs` as full smoke evidence can overclaim validation: the report zips logs and summaries, but does not prove a save was loaded or a hook path was exercised.
4. A mod that caches `HookStatusInfo` as live state will miss later changes because diagnostics helpers return snapshots.
5. A mod that uses `RecordEvidence` for screenshots/video will get only text summary plus latest log copy unless separate screenshot tooling wrote files.

## Minimal rebuild direction

- Keep UI helper docs explicit: DTMAPI pages, not official UI ownership.
- Add developer docs showing which pages are title-only, overlay-capable, or report-only.
- Keep diagnostics helpers stable for DTMAPI internal evidence, but label `RecordEvidence` as a helper for attaching text/log evidence rather than running smoke.
- If future ordinary mods need native UI integration, create a separate GameBridge UI adapter with explicit native menu owners and state boundaries.

## Evidence gaps

- No native official settings or pause-menu extension point was audited in this pass.
- No game smoke was run. Existing evidence comes from public matrix lines 54-63, hook-map UI lines 314-353, and smoke-matrix debug console/report rows 53-59.
- DTMAPI report capture does not include a guarantee that Unity Player.log exists; `DiagnosticsService.FindPlayerLogPath` only searches known local paths (`src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs:126-132`).
