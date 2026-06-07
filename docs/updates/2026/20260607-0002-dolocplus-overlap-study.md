# 20260607-0002 DolocPlus Overlap Study

Status: implemented

Scope: docs/research/third-party

## Source Request

User asked to do a fuller learning pass on 小神增强包, starting with features that overlap current DTMAPI work.

## Changed Files

- `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- `docs/updates/INDEX.md`

## Summary

Added a read-only compatibility research note comparing DolocPlus / 小神增强包 overlap against current DTMAPI APIs and official-local mods.

The note covers:

- DLL/BepInEx features versus CE/Lua trainer features.
- Overlap with DTMAPI Y console, advanced debug APIs, time controls, Zoom, Chest Locator Enhancer, Strong Planting Gun, AutoFishing, animal/fish info, ActionSpeed/movement, crafting/debug, NPC teleport, and inventory/store helpers.
- DTMAPI-unique areas that do not clearly overlap with DolocPlus.
- Future API candidates such as official console metadata, fish-pool analysis, NPC info, inventory transfer, farming automation, animal/fish-tank automation, crafting debug, and panorama camera API.

Follow-up in the same study added a function-level map for overlapping features. It records confirmed Harmony patch targets, function signatures, observed native calls, CE/Lua entrypoints, and the corresponding DTMAPI API implications.

## Validation

No build or game smoke was run. This is documentation/research only.

Manual source inspection used:

- `references/third-party-mods/小神增强包`
- Temporary extraction of `DolocPlus.dll`
- Temporary extraction of `DolocTownEA_Lua_1.8.1.CT`
- Existing DTMAPI API matrix, hook map, and testmod documentation

## Evidence / Related Records

- Research note: `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
- Function map: `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- Related previous note: `references/doloc-town/research-notes/research-DolocTown-Motor-Vehicle-API.md`
- Related Zoom lesson: `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`

## Rollback

Remove the research note and this update entry if the third-party compatibility study should be withdrawn. No runtime files were changed.

## Follow-Up

- Continue with a second DolocPlus pass for non-overlapping features if desired.
- Do not convert this note into implementation work until a dedicated goal file is created.
