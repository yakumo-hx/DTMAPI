# Game smoke runner

`../run-game-smoke.ps1` retains the existing parameters, exit codes, result fields and `DTMAPI_SMOKE_EVIDENCE_PATH` marker. Read [product validation](../../../docs/workflows/product-change-validation.md) for test selection and [PROJECT](../../../PROJECT.md#codex-游戏测试规则) for save ownership; this directory implements those rules.

## Responsibilities

| Owner | Responsibility |
| --- | --- |
| `core/session.ps1` | Deadlines, bounded log waits, stable process absence before recovery |
| `core/save-policy.ps1` | UI/native slot mapping, disposable roots, selected owner/slot recovery and migration prerequisites |
| `core/deployment.ps1` | Exact non-save asset baselines, native enablement and reversible source state |
| `core/evidence.ps1` | File evidence, unchanged checks, result and failure receipts |
| `core/diagnostics.ps1` | Crash freshness and diagnostic collection |
| `core/qa-host.ps1` | Optional QA host staging and exact owned cleanup |
| `scenarios/` | Selected product/input protocols; MoreSaves three-phase contract and source assertions |
| `phases/` | Preflight, environment scope, routing, prepare, deploy, exercise, restore, assessment and publication |

`load-modules.ps1` loads shared definitions and only the requested scenario helpers. The CLI owns termination: a phase returns an explicit exit code, and the entry stops before the next phase. A dot-sourced phase must not use `exit`; PowerShell can return from that file and continue its caller. Validation requests have an additional guard before session preparation.

Legacy product branches in `exercise-session.ps1`, routing and assessment remain compatible. Move another product only when its behavior or runner contract changes; this first migration does not turn every existing product into a new refactoring task.

## MoreSaves first migration

| Phase | Save mode | Product |
| --- | --- | --- |
| EnabledLifecycle | ArchiveMutation | Exact official Local source enabled |
| DisabledCold | NoNativeSave | Disabled |
| ReenabledCold | NoNativeSave | Same source enabled again |

Use `../run-moresaves-fixed12-acceptance.ps1`. Prepare its marked fixture once, containing valid native source slots 0–5 and empty extra slots; reuse it across all three processes. Bind `-ExpectedLocalProductRoot` to an independent frozen candidate whose package bytes match the live official Local package. The wrapper builds once unless `-SkipBuild`, holds the shared lock, checks the complete fixture SAVE state and live package/enablement, and removes only its marked fixture after success unless retention is requested. A failed phase retains evidence and the fixture.

The wrapper acquires and releases its own Runtime lock. Release any lock already held by the calling worktree before invoking it; this lock is not reentrant. Do not wrap the command in a second acquisition.

`-PlanOnly` and `-ValidateOnly` do not start a game. A real run uses the declared QA/DirectExe fixture route and one deadline per process from launch through terminal observation and exit; the existing bounded recovery wait follows. Timeout or late evidence cannot pass a phase. Explicit crash diagnostics retain their separately declared collection budgets.

The startup guard applies to every run that can load MoreSaves, even without a MoreSaves scenario flag. It checks actual native enablement, the corresponding Local/Workshop entry and the exact 18 legacy names for native indices 6–11. A stale enabled ID with an absent package does not require a fixture. An enabled present entry with those files requires disposable `ArchiveMutation` behind the pre-Runtime save redirect; an unrecognized present package must be resolved before launch. No legacy candidates means this exception does not apply.

## Failure and save evidence

The runner's `SaveSlot` is the **one-based UI position**: `1` loads native `slot0`; `7` selects native `slot6`; `0` is title-only. Repair and collector `SlotIndex` values are zero-based native indices.

Ordinary NoNativeSave runs have no routine player archive backup or writeback. Current `.data/.prevN/.bak` and committed Product metadata are compared before non-save asset cleanup. The game must be stably absent before recovery. A lingering process blocks restoration and creates `manual-recovery-required.json`; failures still emit a failed result and evidence marker, with unverified gates left unverified. Corrupt/missing directory recovery material is rejected before changing the target.

Before an ordinary run, `pending-product-recovery-preflight.json` checks only the selected slot's Catalog sidecar and the actual Local/Workshop owner selection. When MoreEquipmentSlots will load, two observed load-time writes require a different test prerequisite: Product v3 journal `origin=3/phase=0/attemptStarted=true` (`PendingProductRecovery`), and scoped flat schema 3 with top-level `ownerId/storageScope/archiveIndex` instead of Product `scope` (`PendingProductMigration`). When the owner will not load, the dormant Compatibility Host can recover its canonical Product v3 storage: occupied committed slots or known owner/orphan journal items report `PendingProductColdRecovery`. CoreOnly therefore does not by itself prove that Product metadata will remain unchanged.

Schema number, generation or escrow count alone does not establish these conditions. The check reads metadata only; explicit recovery scenarios retain their own gates. It does not scan other slots, migrate files or create a backup. Use a slot with known suitable current state for an ordinary lifecycle test; a product test still requires its selected product and relevant fixture.

## Manual observation

For title-menu entry, reopening and input checks, use `-WaitForManualExit -SaveSlot 0 -SaveTestMode NoNativeSave` without QA staging or SaveLoaded observation. This route does not auto-load a save. Record the actual UI result and exit normally before `-TimeoutSeconds`; process exit alone is not gameplay acceptance.

After loading, `-WaitForManualExit -StageQaHost -QaObserveSaveLoaded -SaveSlot <UI position>` retains the real SaveLoaded observation and keeps the completed QA participant running for manual inspection. The default still exits when its requirements finish. Both manual routes use existing fixture/save guards and the runner deadline; forced close or late exit fails manual acceptance.

This mode does not create a gameplay success marker. Record the actual mailbox, read-mail and native mission acceptance observations separately for a repair. Do not add unrelated input scenarios to keep the game open. For the repair fixture's native slot0, use UI `SaveSlot 1`; preserve the fixture, source and profile bindings from [player support](../../../docs/workflows/player-support.md).

## Offline verification

Run these with Windows PowerShell 5.1; they never start the game:

```powershell
& tools/scripts/test-game-smoke-modules.ps1
& tools/scripts/test-game-smoke-save-modes.ps1
& tools/scripts/test-game-smoke-process-boundaries.ps1
```

The first tests actual module outputs, source selection, save metadata, deadlines and recovery failures using synthetic files. The second keeps existing CLI/save-family and product routing checks. The third runs real child processes for projections and invalid prerequisites, including manual/default QA routing, then exercises the CLI/try-finally in a private copy with trapped deployment/OS boundaries. `DTMAPI.QaUnitTests --manual-exit-only` is the focused participant behavior check; invoke it with the compatible host from `Get-DotNetExe`. No game result is inferred from these tests. Actual MoreSaves acceptance still requires all three gameplay phases on the intended candidate.
