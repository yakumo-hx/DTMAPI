# Local Mod Native Owner Source Index

Status: docs-only source map
Date: 2026-06-13

This source index records what the four-round review used and what each source may prove.

## Required Project Context

- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/README.md`
- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/native-function-map/README.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Reverse And Official Reference Baseline

Primary reverse baseline:

- `references/doloc-town/reverse/builds/23465763_workshop_38581E`

Comparison baseline:

- `references/doloc-town/reverse/builds/23249387_workshop_247ACD`

Official Workshop docs:

- Used only for content, beauty, and official-mod support boundary evidence.
- Not proof that DTMAPI can mutate runtime state safely.

## Local Testmod Sources

Primary inventory root:

- `testmods`

The 20 covered testmods are listed in [Inventory](inventory.md).

These sources can prove:

- current DTMAPI mod semantics;
- manifest and dependency expectations;
- which public APIs current examples consume;
- where sample behavior relies on GameBridge, Core, or diagnostics.

They do not prove:

- raw native owner stability;
- multi-mod ownership safety;
- save/load or disable restore safety unless supported by separate smoke/debug evidence.

## Legacy Own-Mod Sources

Primary inventory root:

- `references/doloc-town/own-mod-sources`

Covered source groups:

- `ActionSpeedMod`
- `AnimalHusbandryProgressMod`
- `AutoFishingMod`
- `FishBreedingAssistantMod`
- `OneActionCompleteMod`

These sources are semantic history only. They must not be treated as current public API stability evidence.

## Local Third-Party Samples

Primary inventory root:

- `references/third-party-mods`

Covered sample groups:

- `ExpandedEncyclopedia.zip`
- `HoldToHarvest.zip`
- `Infinite Hover.7z`
- `Genesis.ContentLoader.7z`
- `Genesis.Core.7z`
- `DolocTownEA_BepInEx_DolocPlusMod.7z`
- `DolocTownEA_Lua_FullTrainer_1.8.1.7z`
- `automated drone` sample archives and screenshot
- `larger buildable space` sample archive

These samples can prove local demand and compatibility risk. They do not grant permission to copy, migrate, distribute, or derive implementation logic.

## Prior API Review Evidence

Important review records named by the subagents:

- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/03-machine-production.md`
- fishing native responsibility records under `docs/reviews/api/2026`
- crop harvesting native responsibility records under `docs/reviews/api/2026`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`

Future API rebuild goals should re-open the exact current record for the chosen API before changing runtime code.
