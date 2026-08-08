# MoreSaves Admission Prerequisites

## Metadata

- Update ID: `20260723-0003`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `closed`

## Source Request And Authority

The source request requires a separate MoreSaves admission decision before a
sixth product implementation and keeps MoreEquipmentSlots independent. Review
[`20260723-0002`](../../reviews/code/2026/20260723-0002-moresaves-sixth-product-admission-review.md)
identified three prerequisites: harden the existing dormant-shipped Host,
freeze the retained SaveSlots ABI consumer, and prove both native-writer orders.
Commit-range audit
[`20260723-0003`](../../reviews/code/2026/20260723-0003-sixth-product-commit-range-audit.md)
later reopened this Update for focused Doctor/Status, frozen-state and exact
component-transaction corrections. The original admission Review remains
frozen as a decision record.

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The reviewed native holder is `GameManager.archiveFileCount`; official
`LocalSave` and `GameDataPanel` remain the file and UI owners.

## Changes

- The Compatibility broker inspects the exact in-memory PE bytes before
  `Assembly.Load`, freezing simple name, AssemblyVersion `0.5.3.0` and
  `.NETStandard,Version=v2.0`. Wrong-name, wrong-version and wrong-framework
  fixtures prove rejected bytes never become resident.
- Catalog and Install Doctor freeze the one optional component ID, path, name,
  version, framework and policy. Doctor compares both receipts and actual PE
  metadata without loading the component.
- The existing Runtime upgrade matrix now carries the real Host bytes and
  `OptionalComponents` receipt through success, the supported rollback phases,
  interrupted recovery and Runtime-only uninstall. Exact
  `MovingOldComponents` and `PlacingComponents` windows now inject after the
  directory move but before the in-memory flag write; immediate rollback
  reconstructs those flags from phase plus disk shape. It preserves unrelated
  reports/config.
- The retained MoreSaves Workshop DLL is bound by exact SHA-256 and required
  `ISaveSlotsApi` MemberRefs. The API and DTOs remain Experimental and gain the
  additive Frozen/Obsolete warning; no ABI is deleted.
- The same Compatibility Host gains one fixed-six/twelve executor. One internal
  archive-count writer guard rejects Product-first and compatibility-first
  conflicts, shares one compatibility lease across old consumers, and retains
  the lease plus one operation demand when manager lookup or final restoration
  fails.
- Doctor and Status require both 0.5.5-or-later receipts to contain exactly one
  canonical Compatibility Host row. Dual omission is red, and Doctor also
  rejects coherent receipts wrapped around substituted managed bytes.
- Frozen SaveSlots behavior again reports unregistered owners as unconfigured,
  normalizes a cold disabled registration to native six before release, and
  preserves owner-specific enabled/disabled state under a shared mixed-owner
  effective count.

No sixth product assembly, Advanced policy, package, manifest or game
installation is created by this Update.

## Changed Files

- `src/DTMAPI.Core/Manifesting/PortableAssemblyReferenceInspector.cs`
- `src/DTMAPI.GameBridge.DolocTown/CompatibilityHost/CompatibilityHostBroker.cs`
- `src/DTMAPI.InstallDoctor/InstalledRuntimeVersionProbe.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Abstractions/Internal/MoreSavesNativeOwnerCoordinator.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/SaveSlots/SaveSlotsCompatibilityService.cs`
- `src/DTMAPI.GameBridge.DolocTown.Compatibility/*`
- `tests/DTMAPI.UnitTests/*`
- `tests/DTMAPI.InstallDoctor.Tests/*`
- `tests/DTMAPI.AbiCompatibilityHarness/Program.cs`
- `tools/release/dtmapi-product-catalog.json`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/test-runtime-upgrade-transaction.ps1`
- the linked Review, Batch 6 contract and public API matrix

## Validation

- `DTMAPI.UnitTests`: PASS, including all pre-load rejection and both-order
  writer/pending/restore cases.
- `DTMAPI.InstallDoctor.Tests`: PASS.
- retained public Workshop ABI scan: PASS for eleven products, six known frozen
  consumers, exact retained MoreSaves hash/MemberRefs and zero public deletion.
- Runtime upgrade transaction matrix: PASS on both PowerShell hosts (PowerShell
  7 and Windows PowerShell 5.1), 17 cases each, with the real optional component,
  including the exact component move/place interruption windows, commit,
  rollback, interrupted recovery and uninstall.
- current 0.5.5 dual-receipt Host omission through the real Status entry: PASS
  as a required negative case.
- Phase 0 historical receipt reproducibility plus current MoreSaves source
  topology: PASS; historical and current projections are separate fields.
- game smoke: not required; this slice changes admission infrastructure and
  does not switch the live SaveSlots route or install a product.

## Rollback

Revert this Update as one unit. Do not retain the MoreSaves admission GO if the
Host preload validation, exact optional-component receipts, retained ABI row or
writer coordinator is reverted.

## Follow-up

MoreSaves was subsequently implemented by Update `20260723-0004`. Preserve
this prerequisite boundary and do not admit a seventh product until the
remaining Phase 1/4 tails receive their own bounded disposition.
