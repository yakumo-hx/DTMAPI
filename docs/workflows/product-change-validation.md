# Product change validation

Use for a product fix or the affected slice of a platform task. PROJECT owns [save/test semantics](../../PROJECT.md#codex-游戏测试规则). Select applicable sections; one Update carries the acceptance checklist and result.

## Select the changed boundary

Use this task's delta, not unrelated working-tree changes or every historical feature of the Mod.

| Change | Relevant validation | Broaden when |
| --- | --- | --- |
| Docs/text/metadata | Local links/format and affected shipped metadata | Executable package or loading behavior changes |
| Product logic | Selected project build and focused defect/boundary tests | The claim includes actual native/UI behavior |
| Native/UI/input | Focused regression, direct game result, logs and clean exit | Changed transitions, cleanup or shared consumers need cases |
| Save/sidecar/slot reuse | Affected no-save, native-save and cold-load boundary | Changed commit/recovery requires failure-window reconciliation |
| Shared Runtime/API/loader | Affected ownership, dependency, exception and retained-ABI checks | Named integration/migration/release acceptance requires the full suite |
| Hot path/retained objects/GC | Targeted reproduction or measurement | Concrete accumulation risk requires a duration/count-based long test |
| Product package/upload-folder sync | SDK package/receipt and normalized path/length/hash parity | Runtime installer bytes change; use its package matrix |

Save coverage accounts for rollback, consume/break without duplication, native-save retention and failure-window reconciliation. Run changed cases; reuse unaffected coverage only with unchanged relevant inputs. Owner/orphan recovery is a separate transaction.

Tests should distinguish a defect or broken contract from correct behavior. A CLI negative case checks the invoked command, intended exit category and specific diagnostic; a usage/internal error cannot stand in for an expected rejection. Do not add tests that merely restate a low-impact reversible text, formatting or implementation change.

## Reuse and invalidation

A result is reusable when its relevant code, dependencies, package bytes, configuration, game build and fixture assumptions are unchanged. Record the existing evidence and a short reuse reason in the Update; no new cache/receipt or exhaustive file-hash ledger is needed.

- Editing only an Update/link does not invalidate a tested binary. Run the changed document checks.
- A source or dependency change rebuilds its affected project/package and reruns its affected checks.
- A new game build, source selection, config, save fixture or runtime failure invalidates the corresponding behavioral evidence.
- At a required full-suite boundary, repair failures with the affected focused check or named diagnostic stage. After the known failures are resolved, run the complete required suite once on the final candidate; do not restart it after each individual correction. Diagnostic results cannot be spliced into a full PASS. A local failure does not create a new full-Release requirement.

## Before entering the game

Resolve these once for the candidate, before another launch:

1. **Product:** actual Local/Workshop source, built package and enabled dependencies. Advanced builds use their generated policy and a matching SDK/reference fixture.
2. **Environment:** settings-derived game/state/sidecar root, available UI slot (empty is valid) and native index. Missing dependencies or guessed directories are setup failures.
3. **Scenario:** changed behavior, expected result, PROJECT save mode and actual native-save entry when needed.
4. **Finish:** the complete observable path (entry → changed action → result → required reopen/exit), decisive evidence and expected disk changes. An internal panel or API success alone does not prove a requested native menu entry. Acquire the shared Runtime lock.

Ordinary tests use the existing environment. PROJECT allows explicitly disposable slots in place and defines when isolation is needed; record the concrete reason and reuse one suitable fixture. Reuse the built candidate/SDK while its inputs remain valid.

Direct Steam launch, selected-save testing and exit are valid evidence. Use an existing runner when it fits; neither HookProbe nor optional QA is mandatory for direct testing. Keep specialist runner assertions intact rather than expanding a test to satisfy unrelated setup.

For a reused-slot fix, prove stale target Product data is cleared, other slots stay unchanged and the new slot works; validate the affected first-commit/cold-load path. Only cold-start behavior requires another process.

A controlled call to the real native SaveGame can prove the Product's response to an unchanged, previously evidenced commit boundary. It does not prove sleeping/menu/input behavior. A changed or explicitly requested player save entry still requires that entry. Never substitute forged SaveSaved or label InstantSave as a sleep test.

## Stop and record

After a setup failure, fix the failed prerequisite before relaunching; keep the corrected command, slot mapping and known lifecycle timing in the same Update/evidence for the next attempt. A model/effort switch or context compaction is not a new candidate. If input/tutorial automation blocks a step, use an existing equivalent route only for the same evidence claim. Otherwise identify the missing required acceptance and continue independent work; do not wait for assumed day/fainting behavior.

Finish when the required affected checks, disk-state proof and clean exit pass. Add a test/rebuild/restart only for changed inputs, a new failure or a remaining named acceptance. Restore deliberately changed non-save assets; follow PROJECT for archive/sidecar proof and the designated disposable slot's final state.

Record required results and remaining gaps, not a list of every unrelated test skipped. Add smoke only for actual game/runtime runs and link evidence; keep implementation narrative in one Update. [Sync its monthly status](../updates/README.md); update Issue/Hook/API/Catalog only when their facts change.

## Commands

Use [the existing script entrypoints and focused example](../../tools/scripts/README.md#choose-validation). Check the actual runner's supported focus before use. A full Release invocation includes its build; do not prepend another full build/test cycle.

For the shared game runner and MoreSaves three-phase acceptance, use the [runner responsibilities and commands](../../tools/scripts/game-smoke/README.md). `SaveSlot` is the UI position starting at 1; collectors and repair tools use native `SlotIndex` starting at 0.
