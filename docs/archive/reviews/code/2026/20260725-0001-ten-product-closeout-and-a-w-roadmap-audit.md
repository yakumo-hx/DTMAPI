# Ten-Product Closeout And A-W Roadmap Audit

**Review ID:** `20260725-0001`

**Date:** 2026-07-25

**Status:** recorded — the MoreEquipmentSlots and Zoom closeout is accepted;
the ten-product Advanced baseline remains `verified/closed`

**Reviewed HEAD:** `9387e229`

**Scope:** independently recheck the final MoreEquipmentSlots save-commit
correction, Zoom derived-camera restoration, their focused gates and final game
evidence; then map the five A-W decision dockets to the current authoritative
roadmap. This Review does not admit an eleventh product, open Content Host G7,
authorize the 0.5.5 release, or run a complete Release, L0-L5, GC gradient or
long test.

## Verdict

No remaining P0 or P1 defect was found in the two reopened closeouts.

- MoreEquipmentSlots no longer advances ordinary shield gameplay state beyond
  the last successful native save. The final evidence starts from a real
  committed 80/80 shield and proves damage, break, replacement and unequip
  no-save rollback without changing the selected player archives or committed
  sidecar. The cold observer still sees exactly one committed 80/80 shield.
- The save-mode runner does not use routine player-save backup/writeback on a
  green `NoNativeSave` path. `NativeSaveExpected` uses an owned disposable
  fixture, while environment variables, junctions and fixtures are restored or
  removed through the outer cleanup path.
- Zoom now treats `CameraController.RefreshResolution` as part of restoration.
  A failed refresh is fail-closed rather than reported as a successful 1x
  restore. Final real-game evidence covers derived camera state at 2x and 4x,
  then 1x, config, title and Loader-owner restoration.
- The focused source, Unit, QaUnit, Catalog, save-mode-runner and documentation
  gates pass at the reviewed HEAD.

The first ten physical ProductNative migrations and their current behavioral
baseline may therefore remain frozen. This does not make 0.5.5 releasable:
public ecosystem compatibility, the active AutoFishing/ActionSpeed GC gates,
release integration and the still-blocked future product/content work remain
separate.

## Evidence Rechecked

| Evidence | Result |
| --- | --- |
| `GAME-SMOKE/20260724-221902` | `NoNativeSave`; Zoom ProductNative behavior and owner deactivation pass; no routine backup and no selected archive/sidecar change |
| `GAME-SMOKE/20260724-222231` | `NativeSaveExpected`; owned disposable fixture establishes the committed shield baseline |
| `GAME-SMOKE/20260724-222329` | committed shield damage followed by no-save rollback passes |
| `GAME-SMOKE/20260724-222423` | committed shield break/replacement paths followed by no-save rollback pass |
| `GAME-SMOKE/20260724-222516` | cold observer sees the same one 80/80 committed shield and fixture cleanup passes |
| `test-game-smoke-save-modes.ps1` | passes in the current PowerShell host and Windows PowerShell 5.1 |
| focused Unit filters | `zoom-product`, `zoom-acceptance-routing`, `moreequipment-product`, `moreequipment-acceptance-routing`, and `compatibility-host` pass |
| QaUnit | Release build and run pass |
| Catalog | passes with 27 products, 11 public products, 21 Workshop items and 48 API rows |
| documentation governance | 5,889 checks pass |
| whitespace | `git diff --check` passes |

No new game process was launched for this audit. No complete Release, L0-L5,
GC gradient or long test was run.

## One Non-Blocking Coverage Debt

### P2 — active-scale Zoom coherence after a native refresh is not directly covered

The accepted contract proves that 1x, disable, config, title and Loader cleanup
restore both the camera size and the native controller's derived state. It does
not directly prove this sequence while Zoom remains enabled:

```text
4x -> resolution/fullscreen native refresh -> 2x
```

The product deliberately performs the native refresh on the return-to-1x
boundary, not after every non-1x step. The current Update records that a native
refresh at 2x or 4x can temporarily derive controller state from that active
scale. No observed failure shows that the 4x-to-2x sequence breaks camera
following or room clamping, so this is a focused manual-regression target, not
a verified defect and not a reason to reopen the tenth-product closeout.

## Roadmap Layer Reached

The current implementation is at the end of the first bounded Batch 6
ProductNative migration wave:

1. Batch 0-5 platform identity, ownership, version, SDK, packaging, Doctor,
   QA-separation and compatibility foundations are closed.
2. Batch 6 Phase 0 and G2 are closed.
3. Ten exact Advanced ProductNative products are admitted, implemented and
   `verified/closed`: AutoFishing, OneActionComplete, ActionSpeed,
   FishBreedingAssistant, AnimalHusbandryProgress, MoreSaves,
   ChestLocatorEnhancer, MoreEquipmentSlots, StrongPlantingGun and Zoom.
4. The nine frozen legacy ABI surfaces remain parseable through the dormant
   Compatibility Host. No public ABI was deleted for 0.5.5.
5. An eleventh product, Content Host G7 and the 0.5.5 public release remain
   blocked until their own bounded authority and validation work.

The roadmap's checkpoint letters A-M are migration checkpoints and must not be
confused with the product-decision letters A-W below. Migration checkpoints
A-M have reached the ten-product closure; decision A-W includes later UI,
content, ecosystem and release work that is intentionally still open.

## A-W Decision Progress

| Decision | Current state | Audit result |
| --- | --- | --- |
| A | complete | Player uninstall is Runtime-owned only; `dtmapi-package.json` is non-destructive. Author deployment receipts live in the SDK/internal tool path. |
| B | core complete | Player Runtime uses lifecycle loading with visible errors and no file polling. SDK explicit reload exists for proven supported content; unsupported routes require restart. Optional development watcher B2 remains future work; B3 remains rejected. |
| C | in progress | Catalog-first C2 authority is live and admitted first-party product source is physically under `products/first-party`. Full physical classification of legacy `testmods` content into the C1 endpoint is not complete. |
| D | complete | A separately versioned Author SDK exists; the player package retains read-only Doctor/error guidance rather than author mutation tools. |
| E | partial by design | The C# CustomEntity surface is frozen/experimental and blocked from becoming a general runtime promise. The protected JSON+PNG+WAV route remains valid, but its formal general Content Host/author lane is not yet built. |
| F | governance complete | Catalog facts, product identities and prototype/rebuild states are authoritative. This does not mean every catalogued product is implemented or released. |
| G | complete | Workshop is player source authority; explicit SDK override is an author operation with restart semantics. |
| H | pending rollout | Runtime-first canary rollout was selected but has not happened because 0.5.5 publication is deliberately paused. |
| I | partial | Current public API metadata, frozen identities and warnings exist. The full publicly obtainable consumer scan, 0.5.5 warning window and any later removal epoch remain open. |
| J | pending | The player-centred Manager/GMCM information architecture, pagination and advanced diagnostics rewrite is not implemented. |
| K | implementation complete | MoreSaves is a separate 1.0.0 product with the protected fixed-12 behavior. Actual public publication remains part of rollout work. |
| L | complete for 0.5.5 | SaveSlots remains a frozen compatibility facade in the Compatibility Host. Conditional retirement belongs to a later compatibility epoch. |
| M | policy complete, product pending | The Y console is not built into Runtime, but DebugConsole has not received its own admission and extraction. |
| N | pending | Behavior-equivalent DebugConsole extraction must precede the planned UI rewrite; neither stage is closed. |
| O | pending | The unified AnimalPack/ShellCrab/Lightning product decision is recorded, but AnimalPack remains `PrototypeBlocked` and its product/economy design is deferred to that rebuild. |
| P | pending | Animal processing/economy should prefer official JSON and product-owned content. Detailed product values and processing fixtures are not implemented. |
| Q | partial | The protected short-SFX/content behavior and explicit Audio reload path exist. A formal public SoundKey catalogue and Content Host/SDK author route are not complete. |
| R | pending | Manbo has not been migrated to the selected data-first 1.0.0 compatibility canary. |
| S | partial | Experimental/frozen/stable-candidate maturity is recorded, but a complete public content schema/catalog lane is not yet available. |
| T | complete scope control | 0.5.5 makes no BGM replacement promise. BGM remains a later independent project. |
| U | mostly complete | The ten current single-consumer gameplay implementations are ProductNative and their nine legacy ABI surfaces remain frozen. The external-consumer scan required before public retirement or deletion is still open. |
| V | partial | Compatibility canary, structural product templates and protected migration are separated in architecture. Public rollout waves and active GC release gates have not run. |
| W | complete | The global questionnaire was closed and implementation proceeded through the current Batch 6 ten-product baseline. New products still require bounded admission rather than reopening the questionnaire. |

Summary:

- complete or complete for the current boundary: A, D, F, G, K, L, T, W;
- core/mostly complete with a deliberate later step: B, U;
- partially implemented: C, E, I, Q, S, V;
- policy decided but implementation/rollout pending: H, J, M, N, O, P, R.

## Remaining Route And Problems

### Before any 0.5.5 release

1. Keep release publication paused until the user reopens that boundary.
2. Complete the best-available scan of publicly obtainable DTMAPI consumers
   and preserve compatibility for real existing Workshop dependants.
3. Run the active-gameplay GC comparison for AutoFishing and ActionSpeed only
   at the final relevant integration/release boundary: 1x, enabled without
   acceleration, common acceleration, high multiplier, disabled recovery and
   title cycles. Do not assume a shared Animator owner unless evidence finds
   one.
4. Run one final clean complete Release against a frozen candidate only after
   focused failures and the diagnostic tail are green. Do not restart the full
   suite after every correction and do not treat partial runs as acceptance.

### Product and platform work that can precede broad release validation

1. Select and admit the next bounded product only if it has a complete native
   owner, save mode, packaging, recovery and acceptance plan. Current roadmap
   candidates are not automatically authorized by the ten-product closure.
2. Resolve Mine's ProductNative prerequisites before admission: remove false
   configurable-power promises, define enabled registration, remove or
   implement the unused runtime-mineral switch, and restore original values.
   Static content remains official-JSON-owned; Oil/OneAction hard coupling must
   not return.
3. Open AnimalPack only through its deferred O/P product-design node. Do not
   use the protected JSON+PNG+WAV baseline to promote the old C# CustomEntity
   runtime API.
4. Treat Content Host G7 as its own architectural project. It is required
   before DTMAPI can claim a general managed ContentPack/short-SFX/custom-animal
   author lane.
5. Admit and extract DebugConsole before rewriting the Y-console UI. The
   Manager/GMCM player-centred rewrite is a separate J task.
6. Migrate Manbo only after its data-first canary contract and author path are
   explicit.

### Current debt to keep visible

- The repository/download/all-products total is not proven smaller merely
  because gameplay code moved into product assemblies. The canonical
  mandatory compiled Runtime is smaller and default player loading is lighter;
  that is the bounded claim.
- Most gameplay APIs remain experimental or frozen rather than a stable general
  author promise. `IItemDisplayNameApi` is the established shared adapter with
  independent real consumers; future reuse alone must not move ProductNative
  code back into GameBridge.
- The active-scale Zoom sequence above should be included when camera behavior
  is next tested, without turning it into another broad release gate.

## Disposition

Accept the MoreEquipmentSlots and Zoom final closeout and retain the exact
ten-product baseline as `verified/closed`. Proceed only with bounded next-product
or prerequisite work; do not infer permission for an eleventh product, Content
Host G7, the 0.5.5 rollout, a complete Release, L0-L5, GC gradient or long test
from this audit.

## Later Disposition

This Review remains the exact `9387e229` audit and its historical A-W snapshot
is not rewritten. Later commits are evaluated separately by Review
`20260726-0001`:

- `e7276b88` closes the recorded Zoom active-scale P2 with focused Unit and
  real-game evidence;
- `83d01e41` reaches the physical C1 root layout, while later audit still finds
  live Unit-test path consumers that must be corrected before the C1 Update can
  be treated as fully closed;
- `d97c6f6d` implements the Manager information-architecture slice, while its
  player-facing localization and interaction acceptance remain follow-up.

These later changes do not alter this Review's acceptance of the original
ten-product baseline or authorize an eleventh product.
