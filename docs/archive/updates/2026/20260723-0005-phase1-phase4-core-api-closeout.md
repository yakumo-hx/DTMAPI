# Phase 1 And Phase 4 Core/API Closeout

## Metadata

- Update ID: `20260723-0005`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `closed`

Independent acceptance passed. Runtime validation was not required because no
correction reached native/game behavior.

## Source Request

Close the remaining Phase 1 Core legacy entry points and Phase 4 public API
contract tails. Preserve frozen ABI shells, do not expand C# CustomEntity
support, do not implement a first-party AutoHarvest product, and validate only
with focused Core build, Unit and API checks. After implementation, run one
independent closure review, repair its findings, and commit the accepted slice.

## Existing Authority

- [`PROJECT.md`](../../../../PROJECT.md)
- [`batch6-managed-mod-identity-contract.md`](../../../architecture/batch6-managed-mod-identity-contract.md)
- [`20260722-0011-production-qa-seam-and-animal-refresh-audit.md`](../../reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)
- [`20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md`](../../reviews/api/2026/20260722-0001-public-api-consumer-owner-thread-cleanup-audit.md)
- [`public-api-matrix.md`](../../../api/public-api-matrix.md)

## Approved Boundary

Phase 1:

- remove the internal `DtmApiRuntime.RecordInputPressed`,
  `RecordInputReleased`, and `GetRegisteredInputButtons` adapters after moving
  their Unit callers to the typed frame boundary;
- remove the unowned `NotifySaveLoadFatalWindowObserved` endpoint and its
  endpoint-only tests while retaining the coordinator, lifecycle breadcrumbs,
  smoke-only fatal-window capture tooling, and native save/load diagnostics;
- retain public `IInputHelper` string members and input events as frozen
  compatibility wrappers because they are part of the shipped author ABI.

Phase 4:

- reclassify the four registry-only CustomEntity interfaces from
  `StableCandidate` to ABI-preserving `Experimental/Frozen`;
- resolve the duplicate GameBridge CustomEntity provider registration without
  treating it as a second consumer;
- remove the ordinary AutoHarvest sample's dependency on
  `IInstantSaveDebugApi`, without implementing an AutoHarvest product;
- classify helper owner, thread and stale-owner behavior, adding only the thin
  enforcement needed for mutable owner-sensitive helpers.

No new API, Host, receipt family, gate family, C# CustomEntity runtime, native
Hook, gameplay behavior, or first-party AutoHarvest product is authorized.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`
- `src/DTMAPI.Core/Services/OwnerBoundCustomEntityApis.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Abstractions/CustomEntities.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/AutoHarvestMod/ModEntry.cs`
- `testmods/AutoHarvestMod/README.md`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tests/DTMAPI.AbiCompatibilityHarness/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/api/040-stable-custom-entity-apis.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- this Update and the July monthly ledger

## Validation

Passed:

- repository-local .NET 8 Release builds for Core and Unit Tests:
  zero warnings / zero errors;
- `DTMAPI_UNIT_TEST_FOCUS=phase1-core-cleanup`;
- `DTMAPI_UNIT_TEST_FOCUS=phase4-api-cleanup`;
- `DTMAPI_UNIT_TEST_FOCUS=api-metadata`;
- synthetic retained ABI gate;
- exact source scan for removed internal entry points, retained public input
  ABI, external fatal-window capture, AutoHarvest Diagnostic-API removal, and
  the single Core CustomEntity provider;
- `git diff --check`.

The independent closure Review passed after one stale fatal-diagnostic sentence
and one helper stale-read evidence gap were corrected. Document governance
checked 5,780 rules; its only two failures are the unrelated untracked portable
reverse-capture Update's missing monthly entry and resulting row-count mismatch.
That group remains outside this Update.

No game, complete Release, L0-L5, GC ladder, or long test is planned because
this slice does not change native behavior.

## Evidence

The focused checks prove contract/source/build behavior only. No game was
started because this slice changes no native Hook, gameplay behavior, save
format, installed package or runtime deployment.

Independent acceptance:
[`20260723-0004-phase1-phase4-closeout-review.md`](../../reviews/code/2026/20260723-0004-phase1-phase4-closeout-review.md).

## Rollback

Revert this Update's implementation commit. Frozen public ABI declarations
must not be deleted as a rollback shortcut.

## Follow-up

Phase 1/4 is closed. A separate seventh-product admission Review may now
proceed; product implementation remains outside this Update.
