# DTMAPI Manager UI MVP

Status: design with internal view-model skeleton; no player-facing UI implementation.

Date: 2026-06-10

## Purpose

DTMAPI Manager is a developer/support UI for the current Refactor runtime. The MVP should help a user answer four questions without opening logs by hand:

1. Which DTMAPI mods were discovered, loaded, disabled, or blocked?
2. What errors and warnings explain current mod or hook behavior?
3. Which hooks and GameBridge features are ready, pending, failed, or experimental?
4. Can the user change registered DTMAPI config and export a report for review?

The MVP is not a Stable API promotion gate. It consumes existing Diagnostic/Experimental surfaces and must display their status honestly.

## 2026-06-10 Internal Skeleton Note

`DTMAPI.Core.Manager` now contains internal view-model rows and a snapshot mapper for the MVP surface. It maps `IDtmDiagnosticsSnapshot` into Mods, Errors, Warnings, Hooks, Features, latest log/report paths, and report-export path state. This is intentionally internal and covered by unit tests; it does not add public API, game UI, ConfigMenu integration, official enablement writes, or a stable Diagnostics promotion.

## 2026-06-11 Summary And Sorting Note

The internal Manager view model now publishes a support-oriented summary over the existing diagnostics snapshot: loaded, blocked, and disabled mod counts; error and warning counts; failed hook and feature counts; and an `OverallStatus` of `ready`, `warning`, or `failed`. Rows are pre-sorted for a real UI: blocked/error mods before warning, disabled, and loaded rows; diagnostics newest first; hooks by failed/missing/experimental/verified/ready; and features by failed/degraded/ready. These rules are internal view-model policy only and do not add public diagnostics members.

## 2026-06-11 Runtime Skeleton Note

`UiRuntimeService` now owns an internal Manager model provider. Opening a DTMAPI Manager page refreshes the model from `IDtmDiagnosticsApi.GetSnapshot()`, and Export Report calls the existing report export path, refreshes the snapshot, and records whether the returned report path matches the refreshed `LatestReportPath`. This gives future UI pages a non-stale model and explicit `exported` / `missing-export-path` / `report-path-mismatch` states without adding public API or implementing full player-facing UI.

## Non-Goals

- Do not implement UI in this design branch.
- Do not add, remove, or rename public API members.
- Do not promote `IDtmDiagnosticsApi`, UI helpers, config-menu runtime, or gameplay APIs beyond their current stability level.
- Do not parse raw logs as the primary data source when a runtime API already exposes the same information.
- Do not manage Steam Workshop subscriptions or write official enablement state in MVP.
- Do not provide native save repair, mod dependency resolution, or gameplay feature toggles beyond existing config registry writes.

## Data Sources

The MVP should be backed by current runtime-owned data sources:

- Diagnostics snapshot: `IDtmDiagnosticsApi.GetSnapshot()`
  - loaded mods
  - discovered mod status rows
  - diagnostics errors and warnings
  - hook statuses
  - GameBridge feature statuses
  - latest log path
  - latest report path
- Diagnostics helper:
  - `IDiagnosticsHelper.GetErrors()`
  - `IDiagnosticsHelper.GetWarnings()`
  - `IDiagnosticsHelper.GetHookStatuses()`
  - `IDiagnosticsHelper.ExportLogs()`
  - `IDiagnosticsHelper.GetLatestLogPath()`
- Config runtime/registry already used by DTMAPI config pages.
- Runtime API registry for API ownership/status labels when already available.
- Existing report export side effects, including `latest-report.txt` and report zip path.

If a row is missing from the snapshot, the UI should show an explicit unavailable state instead of silently falling back to log parsing.

## Navigation

Use a simple tabbed manager surface:

- Mods
- Errors / Warnings
- Hooks
- Features
- Config
- Export Report

The first screen should open on Mods when the issue is unknown. If the UI is opened from an error toast or status action, it may deep-link to Errors / Warnings.

## Mods

Primary table columns:

- Status icon
- Mod name
- Unique ID
- Version
- Entry type
- Source
- Loaded
- Status code
- Reason

Expected states:

- Loaded
- Disabled by official enablement
- Missing dependency
- Dependency cycle
- Entry DLL error
- Code load error
- API too new
- Unknown error

Sorting:

- Blocking/error rows first.
- Warning rows next.
- Officially disabled rows next.
- Loaded/ready rows last, then source, name, and unique ID.

Interactions:

- Filter by status.
- Search by name or unique ID.
- Select a mod to show manifest path, root path, entry DLL, entry type, official enablement reason, and dependency summary.
- Copy selected mod status as text for review.

Boundary:

- The MVP should not enable/disable official mods. It may show the enablement source and explain that the official Mod UI owns the state.

## Errors / Warnings

Primary table columns:

- Severity
- Time
- Owner
- Kind
- Message
- Count when aggregate summary is available

Detail panel:

- Full message
- Details/stack trace when present
- Related mod ID if inferable from owner
- Link/action to export report

Behavior:

- Errors and warnings come from retained diagnostics windows.
- Errors and warnings are shown newest first.
- If the retained window has been trimmed, show the report summary note from diagnostics export when available.
- Optional dependency version mismatches should appear as warnings, not blocking errors.

Boundary:

- Aggregated counters are report-only today; the MVP can show aggregate text only when it is present in exported report content or a future internal UI source exposes it. It must not require a new public snapshot member for MVP.

## Hooks

Primary table columns:

- Hook ID
- Status
- Target
- Details
- Updated time if available

Recommended grouping:

- Framework / Save / Workshop
- Camera
- Fishing
- ActionSpeed
- ActionCompletion / ToolCollider
- Items / Animals / Farming / Saves / Inventory
- Smoke-only statuses

Behavior:

- Use current hook status text as diagnostics, not as proof of gameplay completion.
- Sort failed and missing hooks first, then experimental, verified, ready, and other rows.
- Failed hooks should provide a direct path to Errors / Warnings and Export Report.
- Experimental/verified wording should remain exactly what the runtime reports; the UI should not relabel it as stable.

## Features

Primary table columns:

- Feature ID
- Status
- Last operation
- Success
- Cumulative failure count
- Details

Detail panel:

- Last error text when present.
- Consecutive/recovered context if present in the feature status details string.
- Related hook IDs by convention, such as `Feature.Camera` or `Feature.FishingAutomation`.

Behavior:

- Feature statuses are read from `IDtmDiagnosticsSnapshot.FeatureStatuses`.
- Sort failed features first, then degraded rows with historical failures, then ready rows.
- The UI should distinguish feature-host health from gameplay/API stability. `Feature.<Id>=ready` means the host route is functioning, not that the public API is Stable.

## Config

Scope:

- Display registered DTMAPI config pages and controls already known to the runtime config registry.
- Allow edit/apply/reset for the same controls currently supported by the DTMAPI config menu runtime.
- Show official-enable locks or read-only state when a config page is not writable.

Expected MVP controls:

- Checkbox/toggle
- Number input/stepper
- Slider
- Text input
- Keybind control
- Color preset/swatch
- Dropdown/list selection

Behavior:

- Reuse existing validation and conflict checks.
- Dirty state should be page-scoped.
- Apply writes through the same config runtime path used by the current menu.
- Cancel discards pending edits.
- Reset restores defaults through existing config runtime behavior.

Boundary:

- Do not introduce a second config persistence path.
- Do not write config for disabled official mods unless the existing runtime already allows that path.

## Export Report

Primary actions:

- Export report
- Copy latest log path
- Copy latest report path
- Open package/evidence folder when running in a local developer environment

Behavior:

- `Export report` calls `IDiagnosticsHelper.ExportLogs()`.
- After export, refresh `IDtmDiagnosticsApi.GetSnapshot()` and verify the displayed `LatestReportPath` equals the returned report path.
- Show the exact report zip path and timestamp.
- Show when the web audit package omits report zip payloads but keeps compact evidence and `latest-report.txt`.

Failure states:

- Export failed
- Report path missing
- Report path mismatch
- Latest log path missing

## Layout Notes

- This is an operational tool, not a marketing page.
- Prefer dense tables, filters, side details, and clear status icons.
- Avoid hero-style layout, decorative cards, and oversized copy.
- The first viewport should immediately show real runtime status.
- Text should be selectable/copyable where it helps support workflows.

## Refresh Model

- Poll snapshot no faster than once per second while visible.
- Pause polling when the manager is hidden.
- Refresh immediately after config apply/reset, report export, mod reload, save load, and returned-to-title when the runtime publishes those events.
- If a snapshot call fails, keep the last known snapshot visible with a stale indicator and record/report the failure.

## MVP Acceptance Gates

Before implementation is considered complete:

- Unit coverage for view-model mapping from diagnostics snapshot rows.
- Unit coverage for summary counters, overall status, and row ordering.
- Manual UI check for table overflow, long mod names, long paths, and long error messages.
- Runtime smoke with Camera and AutoFishing/ActionSpeed to prove feature/hook statuses appear.
- Report export check verifies returned report path equals snapshot `LatestReportPath`.
- Config edit check covers apply/cancel/reset and keybind conflict display.
- Clean exit evidence: no leftover `DolocTown.exe`, no fatal popup.

## Future Work

- Official enablement state management after native/official ownership review.
- Dependency graph visualization.
- Report bundle preview and one-click compact web package export.
- Runtime aggregate diagnostics display if an internal UI data source exposes aggregate counters.
- Manual QA record linking from UI rows to docs/reviews records.
