# ActionSpeed Runtime Acceptance Orchestration Review

**Review ID:** `20260722-0003`
**Date:** 2026-07-22
**Status:** closed — source corrections and corrected migrated-DLL runtime revalidation passed; inferred one-process quota retracted
**Scope:** Root cause of the first ActionSpeed post-migration game attempt; no product behavior verdict, fourth-product admission, Release, L0-L5, long test, or 0.5.5 publication

## Observed Result

`GAME-SMOKE/20260722-100419` launched one Doloc Town process and returned a non-acceptance result. Startup logged `Yuuka.DTMAPI.ActionSpeed` as a Strict CodeMod from the persistent OfficialLocal root `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\Yuuka_DTMAPI_ActionSpeed`, followed by the old `Action speed bridge configured` path. The new managed Advanced entry assembly and its nine product-owned Hooks were never loaded.

The QA controller also began G6 `SaveLoadCycle` before the configured G5 world-mutation group had completed. Returning to title interrupted `ActionSpeedTool`, `ActionSpeedConfigApply`, and `ActionSpeedInteraction`. The result cannot support either a pass or failure claim about the migrated product.

Despite the invalid product route, the process-exit, forced-close, fatal-window, save, official profile, source, QA, G5 configuration, external state and deployment restoration gates all passed. No `DolocTown.exe` remained and the shared runtime lock was released normally.

## Root Causes

1. `Set-SmokeLocal11AuthorSourceState` treated ActionSpeed like the former Strict product and staged its source in the persistent official `MODS` tree. For an admitted Advanced Catalog row, Loader authority must instead be the DTMAPI-managed game `Mods/<UniqueID>` directory with the exact Author SDK receipt.
2. `QaHostParticipant` allowed the G6 title/save cycle as soon as its own prerequisites were ready, without waiting for a configured G5 group. For a combined short acceptance, title navigation is destructive orchestration and must wait until G5 reports completion.

## Rejected Product Hypotheses

- No migrated ActionSpeed Entry, atomic installer, callback, Animator restore, continuous-use or auto-fill code executed, so the incomplete behavior cases are not evidence of a product Hook or restoration defect.
- The clean exit and restored state do not prove the migrated product's disable/title cleanup, because the old compatibility owner was active.
- Historical ActionSpeed smoke and L0-L5 receipts verify only the pre-migration implementation and cannot fill this evidence gap.

## Required Corrections And Acceptance Boundary

- Route Catalog `CodeModKind=Advanced` smoke sources through the managed game `Mods` root and require an exact Author SDK identity/kind/destination receipt; keep legacy Strict staging on the official-local route.
- Start G6 only when no G5 cases are configured or the G5 group is complete.
- Prove the corrections through focused source and QA-unit checks before rerunning the smallest relevant third-save smoke. If that run finds another valid, fixable product defect, repair it, pass its focused checks, and rerun the same bounded acceptance as needed.
- Keep the owning Update below `verified` and the Hook below `verified` until a clean third-save short run loads the Advanced package, observes exactly nine product-owner patches, completes the three ActionSpeed cases, and passes disable/title/restoration/exit.

## User Constraint Correction

The earlier wording that the source request permitted only one game process was an assistant inference and is retracted. The user asked to avoid or minimize complete Release, L0-L5, GC and long tests; they did not impose a numeric game-launch cap. "One bounded acceptance" names the smallest sufficient final evidence, not a consumable run allowance. A failed or non-acceptance short run may be repaired and rerun within this same scope without separate authorization.

## Resolution Link

Implementation and validation facts are owned by [ActionSpeed Third Advanced Product](../../../updates/2026/20260722-0001-actionspeed-third-advanced-product.md).

`GAME-SMOKE/20260722-141220` is the final resolution: it selected the corrected Advanced source, completed all three ActionSpeed cases before title navigation, and reduced the real nine-target owner plus callback/instance/Core roots to zero at Loader deactivation. The same run closed the three OneActionComplete continuation conditions. Earlier `100419` and `125239` remain orchestration/fail-closed history, not current blockers.
