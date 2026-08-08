# 20260713-0006 Sixth-Round Scope Reclassification

## Metadata

- Update ID: `20260713-0006`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user supplied four screenshots, corrected the sixth-round decision scale, deferred animal detail to the AnimalPack rebuild, and requested a brief sixth-round response to the ordinary-recipe versus native-dismantle route

## User Scope Correction In Supplied Order

### 1. Recalibrate Against The Full Audit And Rounds One Through Five

The mainline round must stay at DTMAPI architecture, public-contract, release-governance and implementation-order scale. Per-species item/economy choices are too narrow to follow the P0 ownership, package/source, API stability, Manager/MoreSaves/Y and audio/BGM decisions as another global round.

Analysis immediately following issue 1: the actual sixth docket is narrowed to single-consumer public gameplay API governance, independent migration/Canary roles, and whether the global decision phase ends in favor of Batch 0/P0 implementation.

### 2. Defer Animal Details To The Relevant Mod Rebuild

Hatch/Mole/Drecko/Oilfloater roles, exact products, hidden outputs, processor name/unlock, Oil linkage, probabilities, quantities and sale values move to the AnimalPack 1.0.0 Review/Update.

Analysis immediately following issue 2: the former animal U-Y docket remains as a non-binding deferred design draft so its useful research is not lost, but it is not the mainline sixth round and no current user answer is requested.

### 3. Briefly Resolve The Two Official Processing Routes

Ordinary `recipe_tbrecipe` remains a multiple-input/single-item-type output route. The current public build also has a distinct native dismantle route:

```text
EquipmentFuncGarbageShredder
-> recipe_tbdismantlerecipegroup
-> recipe_tbdismantlerecipe
-> output_item_spawn_entry
-> item_tbitemspawn
```

The dismantle route uses a ranged total draw count and a multi-row spawn LUT. Native source first allocates minimum counts and then performs weighted draws, so several different outputs—and source-plausibly a fixed three-item bundle with three bounded minimum rows—can be expressed.

Analysis immediately following issue 3: the current public-build Mod loader is source-proven to discover/merge the required equipment, dismantle-group, dismantle-recipe and item-spawn tables and to map the function type to the native shredder implementation. The official author docs do not promise these dismantle tables, and no real AnimalPack package has yet passed crafting/placement/UI/power/batch/drop/save/disable/update smoke. A future AnimalPack-owned processor is therefore a strong candidate, not a completed protected behavior. It requires no species Hook and creates no Runtime/GameBridge machine API commitment.

### 4. Treat The Fourth Screenshot As Direction, Not A Binding Annex

The fourth screenshot suggested likely mainline topics and work order but explicitly did not require strict adoption.

Analysis immediately following issue 4: repository evidence independently supports three matching questions, so the new sixth docket uses them with corrected H1/I1/fifth-round details rather than copying the screenshot verbatim.

## Summary

Reclassified `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md` from a proposed mainline sixth round into a deferred AnimalPack design draft. Added the verified native dismantle-route correction and the current-build source boundary for Mod table injection.

Created `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` as the actual mainline round with three recommendations:

```text
U1  first-party internal by default for single-consumer product gameplay capabilities;
    old ABI follows I1 and real external demand stops retirement

V1  separate H1 compatibility Canary, Manbo data Canary, OneAction structural template,
    and AutoFishing/Zoom protected behavior-equivalent migrations

W1  end global product questionnaires and start Batch 0 -> two P0s -> release authority
```

No Runtime/API/content/package/asset/game/Workshop behavior changed.

## Reviews

- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md`
- `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
- `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md`
- `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`

## Changed Files

- the full boundary audit;
- the AnimalPack focused Review and fifth-round docket;
- the reclassified AnimalPack draft;
- the new mainline sixth-round docket;
- Updates `20260713-0004` and `20260713-0005` follow-up/correction links;
- this Update;
- `docs/updates/INDEX-2026-07.md`.

## Source Evidence

- ordinary recipe output shape: current `RecipeInfo` plus official new-recipe author documentation;
- dismantle function/group/recipe/spawn route: current public-build `EquipmentFuncGarbageShredder`, `DismantleRecipeGroupInfo`, `DismantleRecipeInfo`, `GarbageShredder`, `ItemSpawnEntry`, `ItemSpawnInfo` and `ISpawnLut`;
- Mod table discovery/merge and native function mapping: current public-build `ModInfo`, `DolocConfig`, `ModManager`, `Tables`, `EquipmentUtils` and `EquipmentManager`;
- existing multi-candidate proof: official `scrap_big_machanical` dismantle recipe and `scrap_big_machanical_drop` item-spawn LUT.

## Validation

- all screenshot-only scope and technical details were translated into text;
- the full audit/decision/product/API/native-content facts were cross-checked;
- `tools/scripts/check-doc-governance.ps1`: passed (`4349` checks);
- `git diff --check`: passed for tracked changes (only the repository's existing LF-to-CRLF working-copy warnings were reported);
- the scope-reclassification records were checked for trailing whitespace: passed;
- no build/runtime validation is required because no implementation or package behavior changed.

## Runtime Evidence

Not required. No game process was launched, no runtime lock was acquired, and no content package or save/Workshop state was modified.

## Rollback

Remove the new mainline sixth docket and this Update/monthly row, then revert the 0011 reclassification, dismantle-route correction and affected cross-links. The fifth-round decisions remain valid; no source/runtime rollback is required.

## Follow-Up

Obtain U/V/W decisions from the mainline sixth docket. If W1 is selected, stop global product-detail rounds and begin Batch 0 under its own Update. Reopen the AnimalPack draft only when that product rebuild starts.

## Subsequent Resolution

The user selected U1, revised V1 and W1 in `D:/下载/第六轮正式.md`. `docs/updates/2026/20260713-0007-sixth-round-closure-and-active-gc-gate.md` closes the mainline docket, records the 0.5.5 release baseline and routes the focused active-gameplay GC gate. This Update remains the verified owner of the earlier scope reclassification.
