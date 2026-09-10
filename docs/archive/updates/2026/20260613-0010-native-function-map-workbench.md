# 20260613-0010 Native Function Map Workbench

- Date: 2026-06-13
- Status: recorded
- Branch: `codex/bottom-layer-refactor-audit-20260612`
- Source: user requested a visual web workbench and decompiled function map that starts with the data layer, shows all native functions, highlights already determined reverse/native-owner data, shows relationships, and provides statistics.
- Version: no runtime version change; docs/tooling-only.

## Changed Files

- `tools/native-function-map/build_native_function_map.py`
- `tools/scripts/build-native-function-map-data.ps1`
- `tools/scripts/README.md`
- `docs/reviews/api/native-function-map/README.md`
- `docs/reviews/api/native-function-map/index.html`
- `docs/reviews/api/native-function-map/styles.css`
- `docs/reviews/api/native-function-map/app.js`
- `docs/reviews/api/native-function-map/data/summary.json`
- `docs/reviews/api/native-function-map/data/methods.json`
- `docs/reviews/api/native-function-map/data/links.json`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/updates/INDEX.md`

## Summary

- Added a repeatable generator for the current reverse baseline's native method symbols and internal call graph.
- The generator combines `metadata/methods.csv`, `metadata/calls.csv`, `maps/index/*-methods.csv`, diff metadata, and DTMAPI-authored native-owner reports.
- Generated data for current baseline `23465763_workshop_38581E`: 42,925 methods, 74,488 internal method-call edges, 3,601 native-owner tagged methods, 31,555 system-map-only methods, and 7,769 unmapped methods.
- Added a static browser workbench with coverage metrics, domain/system charts, search/filter controls, method table, click-through function details, and selected-method caller/callee graph.
- Kept all output to symbols, signatures, tags, and call metadata. No decompiled method bodies or official binaries are copied.

## Validation

- Passed:
  - Ran `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build-native-function-map-data.ps1`.
  - Generated `summary.json`, `methods.json`, and `links.json`.
  - Verified generated summary counts: 42,925 methods, 74,488 internal links, 3,601 native-owner tagged methods, 31,555 system-map-only methods, and 7,769 unmapped methods.
  - Served the workbench at `http://localhost:8765/` and verified the browser loaded data, rendered 160 default nodes, showed 42,925 matched methods, and displayed coverage charts.
  - Verified search for `Fishing` narrowed results to 592 methods and clicking a method populated the function detail panel and local caller/callee graph.
  - `git diff --check` exited successfully with LF/CRLF warnings only.

## Evidence

- Generator run:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build-native-function-map-data.ps1`
- The generated workbench data is derived from local reverse metadata and DTMAPI-authored research docs. It is a research visualization aid only and does not promote any API.
- No game smoke, build, or unit tests were run because this update changes docs/tooling only and does not touch runtime, public APIs, hooks, mod loading, Workshop loading, or installed game files.

## Rollback

- Remove `tools/native-function-map/`, `tools/scripts/build-native-function-map-data.ps1`, and `docs/reviews/api/native-function-map/`.
- Remove the function-map references from `docs/reviews/api/native-owner-domains/INDEX.md`, `docs/reviews/README.md`, and `docs/workflows/codex-api-rebuild.md`.
- Remove this row from `docs/updates/INDEX.md`.
