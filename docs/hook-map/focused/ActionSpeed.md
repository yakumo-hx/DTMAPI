# ActionSpeed Hook Map

Last updated: 2026-07-22

## Scope

This focused map owns the managed ActionSpeed product's nine-Hook inventory, ProductNative state/restoration boundary, and frozen old-ABI compatibility collision rules. The corrected implementation and migrated-DLL runtime boundary pass.

## Native Owners And Hooks

All targets are resolved before the first patch. A missing target or patch failure exact-owner unpatches the whole inventory and fails Entry closed.

| # | Native method | Patch | Product responsibility |
| ---: | --- | --- | --- |
| 1 | `DolocTown.AgentStateTool.OnEnter()` | Postfix | Capture and accelerate the active tool Animator. |
| 2 | `DolocTown.AgentStateTool.OnExit()` | Postfix | Restore the captured tool Animator speed. |
| 3 | `DolocTown.AgentStateInteract.OnEnter()` | Postfix | Classify the interaction and capture/accelerate its Animator state. |
| 4 | `DolocTown.AgentStateInteract.OnExit()` | Postfix | Restore interaction animation and clear interaction markers. |
| 5 | `DolocTown.AgentStateEat.OnEnter()` | Postfix | Capture and accelerate eat/drink animation. |
| 6 | `DolocTown.AgentControllerState.UseItemContinues(float)` | Prefix | Scale reviewed continuous item-use duration. |
| 7 | `DolocTown.AgentControllerState.InteractContinues(float)` | Prefix | Scale reviewed continuous interaction duration. |
| 8 | `DolocTown.AnimalRenderer.OnInteract()` | Prefix | Mark the reviewed animal interaction before generic state entry. |
| 9 | `AgentStateBase.OnExit()` (global namespace) | Postfix | Restore any remaining action animation/marker on generic exit. |

DTMAPI owner is the Advanced product `Yuuka.DTMAPI.ActionSpeed`; the canonical Harmony owner is `dtmapi.mod.yuuka.dtmapi.actionspeed`. `AgentStateInteract.OnExit` is also observed by OneActionComplete, but the products share neither state nor a write/restore invariant. Independent exact owners remain the correct boundary.

## Product State And Lifecycle

- ProductNative owns action classification, speed selection, Animator snapshots, continuous-use duration scaling, automatic bottle-fill cooldown, pending-animal marker/timestamp, active summary/log dedupe and configuration values. Platform registrations and their owner roots remain Core-owned.
- Configuration disable, save transition, title return, Entry failure and process shutdown restore every captured Animator speed and clear pending markers/timers. During owner deactivation, Core removes updater/event/input/config roots after product Dispose clears local flags/menu state and performs ProductNative restore/callback detach/exact-owner unpatch.
- The external optional-QA observer reflects the loaded product and counts application, continuous-use, auto-fill, summary, Animator, pending-animal/timestamp, cooldown and updater transients. It is not referenced by the player DLL and is not a public ABI.
- Missing native types/methods, invalid state or unsupported action classification returns without synthetic action. No partially installed Hook inventory survives.

## Frozen Compatibility Arbitration

`IActionSpeedApi` and its executor remain under `GameBridge/Compatibility/ActionSpeed` only for already-built Strict consumers. The new product never consumes the API.

- Product-first: compatibility configuration rejects Hook-bearing and AutoFill-only ActionSpeed demand before storing state or installing Hooks.
- Compatibility-demand-first: when the product appears before physical compatibility installation, GameBridge removes all ActionSpeed policy/parent/child demand before either Hook installation or updater work while preserving unrelated demand.
- Compatibility-physically-installed-first: product Entry refuses duplicate ownership and requires a clean restart.

Unit coverage exercises both request/load orders, including AutoFill-only demand. The rule is fail-closed before duplicate Hook or updater execution, not a last-writer-wins arbitration.

## Validation And Evidence

- Focused source gate passes with six production files, nine Hooks, one product policy and seven native authorities.
- Unit, QA-unit, Author SDK validate/build/pack, Catalog, Doctor, installer/uninstaller and live zero-leftover checks pass. The package and receipt hashes are owned by the ActionSpeed Update rather than duplicated here.
- `GAME-SMOKE/20260722-100419` is `blocked`, not acceptance. The run selected a persistent OfficialLocal old Strict package, so GameBridge installed the compatibility implementation; G6 title navigation then interrupted the three G5 ActionSpeed cases. Save/profile/source/deployment/QA restoration and process exit passed.
- `GAME-SMOKE/20260722-125239` selected the actual Advanced DLL and failed closed during all-target pre-resolution: the ninth target incorrectly named `DolocTown.AgentStateBase`, while build `23762374` defines global `AgentStateBase`. No product patch was installed and Core rollback had zero cleanup failures. This remains retained negative evidence.
- `GAME-SMOKE/20260722-141220` selected the corrected package and passed Tool, ConfigApply, Interaction, title lifecycle and restoration. The QA observer queried Harmony patch info for all nine exact targets; terminal Loader deactivation reduced one product instance, nine actual patches/targets and one callback to zero instance/actual patches/targets/callback/Core roots.

Current status: `corrected-product-runtime-verified`. Historical ActionSpeed smoke and L0-L5 receipts remain pre-migration/performance baselines only; no Release, L0–L5 or long test was rerun for this migration.

## Related Records

- `docs/reviews/code/2026/20260722-0002-actionspeed-third-product-admission-review.md`
- `docs/reviews/code/2026/20260722-0003-actionspeed-runtime-acceptance-orchestration-review.md`
- `docs/updates/2026/20260722-0001-actionspeed-third-advanced-product.md`
- `docs/api/public-api-matrix.md` row `IActionSpeedApi`
- `docs/debug/regressions/smoke-matrix.md`
