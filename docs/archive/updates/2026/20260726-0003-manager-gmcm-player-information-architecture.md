# Manager/GMCM Player Information Architecture

## Metadata

- Update ID: `20260726-0003`
- Date: `2026-07-26`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime,player`
- Runtime Validation: `passed`
- Related Issue State: `none`

## Source Request And Authority

Implement Manager/GMCM pagination, Mod state/dependency/restart guidance and
advanced diagnostics while keeping Manager a platform UI that does not absorb
product logic.

The owning design is `docs/design/dtmapi-manager-ui-mvp.md`. Stable identity
and ownership remain owned by `PROJECT.md` and
`docs/architecture/batch6-managed-mod-identity-contract.md`. The July 25
closeout audit identified this player-centred rewrite as the remaining Manager
work; this Update owns implementation and validation.

## Implemented Boundary

- Config/GMCM retains separate paged Mod and item lists. Manager paging now
  uses a tested platform page-window model with empty, stale-page and partial
  final-page clamping.
- Mods renders ten compact selectable rows per page plus a read-only detail
  region. The compact row keeps state, identity, source, dependency issue count
  and current restart requirement visible before truncation.
- Dependency rows come from the existing internal authoritative
  Content/Manifest Registry, including required/optional, minimum version,
  resolved status and detail. Manager does not invent a second resolver.
- Current `restart-required` and loaded-but-officially-disabled states receive
  actionable restart text. Classification restart policy remains informational
  when no restart is currently required.
- Errors and warnings use one independently selectable, 15-row paged list,
  avoiding the previous stacked-window overflow.
- The compatibility-preserved `Features` route is labelled `Advanced` and
  offers registry/compatibility evidence separately from feature health. Hooks
  remain their own platform-health page.
- Status/Copy Summary adds dependency-issue and current-restart counters.

No public API or `DtmOverlayPage` member changed. No gameplay ProductNative
logic, Mod command, native patch, config ownership or official/Steam mutation
entered Manager.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerRuntimeModelProvider.cs`
- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`
- `src/DTMAPI.Core/Manager/ManagerPagination.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/DolocTownGameBridge.G4ManagerFixtures.cs`
- `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/QaScenarioController.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/regressions/smoke-matrix-history-superseded-20260719-through-20260722.md`
- this Update and `docs/updates/INDEX-2026-07.md`

## Validation

Passed:

- Bootstrap and QA Host Release builds: zero warnings and errors.
- focused `DTMAPI_UNIT_TEST_FOCUS=manager-ui`: dependency projection, compact
  rows/detail, restart hints, advanced counters, page-window clamping,
  refresh/export safety.
- `DTMAPI.QaUnitTests`: passed.

The first broad `tools/scripts/build.ps1 -Configuration Release` attempt built
all projects with zero warnings/errors, then stopped in an unrelated
compatibility-host test because its temporary fixture lacked
`DTMAPI/release-manifest.json`. The focused Manager test and QA suite passed
afterward; no Manager failure was observed.

The first actual game run reached and passed every requested Manager field,
NoNativeSave preservation and clean exit, but its overall result was failed by
the runner's generic idle SaveLoad coordinator check. Product growth had
length-trimmed the Hook-status final-health row before its `saveLoad` segment,
while the existing untrimmed final-health summary in the same log proved
`status=ok; requests=0`. The runner now accepts either existing authoritative
final-health representation for the no-load idle case; no Runtime behavior was
changed by that correction.

Final `GAME-SMOKE/20260726-083253` passed:

- Status, Mods, Errors, Hooks, Advanced (compatibility `Features` route) and
  Logs page observations;
- Copy Summary, report export/path match, Status and Logs screenshots;
- authoritative content/manifest/dependency/registry checks and idle SaveLoad
  coordinator state;
- NoNativeSave player archives and committed sidecars unchanged before cleanup,
  with no routine byte backup or archive writeback;
- QA Host cleanup, process exit and no fatal instance window.

The active smoke matrix owns the concise runtime row. No product behavior,
native patch, public API or save-commit behavior was exercised or changed.

### 2026-07-26 audit correction

The post-commit audit found that the production-source Catalog projection was
still frozen at 254 after `ManagerPagination.cs` raised the exact count to 255,
and that the prior player run observed page routes but did not prove the
requested controls through their real Unity button events. The same screenshots
also showed that Status remained support-oriented and that Simplified Chinese
still fell through to hard-coded English in common player fields.

The Catalog count and semantic boundary are now 255. Status now leads with
overall Mod, dependency/restart and error/warning state, with paths, registry,
Hook and feature evidence kept behind Advanced/Logs. Common Mod identity,
placement, compatibility, load reason and restart guidance are localized;
Advanced deliberately retains raw diagnostic evidence where exact support text
is the product.

The QA fixture now invokes the reflected `UnityEngine.UI.Button.onClick`
handlers and verifies the resulting visible text for:

- selecting the second Mod row and replacing its detail;
- traversing every next page to a partial last page, clamping selection, and
  returning one page;
- switching Warnings to Errors;
- switching Feature evidence to Registry evidence.

It archives Status, Mods, Advanced and Logs screenshots. The runner treats those
receipts and the interaction terminal as explicit gates rather than inferring
them from page-open status.

`GAME-SMOKE/20260726-182421` first passed the new interaction route; visual
review of its Mods screenshot found the remaining English identity,
compatibility and restart values. After localizing those values, final
`GAME-SMOKE/20260726-183020` passed on the installed current Runtime:

- real Mod-row, next-page, partial-last-page, previous-page, Warnings/Errors and
  Feature/Registry button interactions;
- visible Simplified-Chinese page ranges and distinct category/subsection text;
- all four non-empty screenshots plus Logs export/path match;
- NoNativeSave archive and committed-sidecar preservation before cleanup, no
  player archive writeback, QA cleanup, no fatal window and clean process exit.

Release builds, complete Unit, focused `manager-ui`, QA Unit and Catalog checks
also pass with zero build warnings. The active smoke-matrix row now points to
the final correction evidence.

## Rollback

Revert the internal registry projection, page model, compact/detail formatting,
category selectors, Advanced presentation, QA expectation and this record
together. Do not retain UI dependency claims without the authoritative registry
source, and do not replace them with parsed error text.

## Follow-Up

Keep product-specific state, commands and acceptance matrices in their product
Reviews/Updates. Any later search/filter/copy-selected work remains an internal
Manager slice unless a separate public-contract review authorizes otherwise.
