# Third-Party Mod API Demand Reviews

Status: active, docs-only
Created: 2026-06-13
Source scope: `references/third-party-mods`

This directory stores read-only compatibility and API-demand reviews for local third-party Doloc Town mod samples. These reports are only demand-discovery inputs for DTMAPI API/GameBridge planning.

They must not be treated as permission to copy, redistribute, decompile, or merge third-party mod code into DTMAPI. When a report names DLLs, manifests, visible file names, configuration text, metadata strings, or Cheat Engine table descriptions, it is using those as compatibility evidence only.

## Rules

- Do not copy third-party source or binaries into DTMAPI runtime, testmods, or release packages.
- Do not publish reports that include decompiled third-party method bodies.
- Closed-source DLL or trainer behavior can suggest an API need, but it does not prove a stable native owner.
- Any future migration of a third-party mod requires author permission or a clear compatible license.
- Native-owner follow-up must start from `../native-owner-domains/INDEX.md` and current reverse maps, not from the third-party mod implementation alone.

## Four-Round Workflow

The full Round 2-4 challenge, revision, and final confidence results are consolidated in [Local Mod Native Owner Review Library](../local-mods-native-owner/INDEX.md), especially [Round 4 Final Review Confidence](../local-mods-native-owner/rounds/ROUND-4-final-review-confidence.md).

| Round | Purpose | Status | Report |
| --- | --- | --- | --- |
| Round 1 / Agent E | Extract sample inventory, visible metadata, semantic goals, candidate native-owner domains to verify, API demands, license risk, and gaps. | Complete | [Round 1 Agent E Mod Semantics](ROUND-1-agent-e-mod-semantics.md) |
| Round 2 | Challenge the Round 1 report, ask overclaim questions, and score confidence. | Complete | [Local Mod Round 2](../local-mods-native-owner/rounds/ROUND-2-review-questions-confidence.md) |
| Round 3 | Reply to Round 2 questions, add supplements, and produce revised report. | Complete | [Local Mod Round 3](../local-mods-native-owner/rounds/ROUND-3-author-supplemental-structure.md) |
| Round 4 | Final challenge and confidence scoring for the revised report. | Complete | [Local Mod Round 4](../local-mods-native-owner/rounds/ROUND-4-final-review-confidence.md) |

## Current Sample Coverage

Round 1 Agent E covered these local samples:

- `ExpandedEncyclopedia.zip`
- `HoldToHarvest.zip`
- `Infinite Hover.7z`
- `Genesis.ContentLoader.7z`
- `Genesis.Core.7z`
- `小神增强包/DolocTownEA_BepInEx_DolocPlusMod.7z`
- `小神增强包/DolocTownEA_Lua_FullTrainer_1.8.1.7z`
- `自动采集无人机与更大建造空间/自动采集无人机_v1.0.zip`
- `自动采集无人机与更大建造空间/自动采集无人机_v1.1.zip`
- `自动采集无人机与更大建造空间/【小型、中型、温室】更大的建造空间_v1.0.zip`
- `自动采集无人机与更大建造空间/屏幕截图 2026-05-21 192651.png`

## Next Use

Before turning any finding here into an API rebuild goal:

1. Pick one semantic target and one possible public API boundary.
2. Re-open the relevant native-owner domain report.
3. Inspect exact native owners in the current reverse baseline.
4. Mark the API proposal as `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, or `blocked-rebuild`.
5. Create a dedicated `docs/goals/YYYY/...` handoff only after the owner questions are concrete.
