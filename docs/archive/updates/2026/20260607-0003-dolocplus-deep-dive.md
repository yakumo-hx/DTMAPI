# 20260607-0003 DolocPlus Deep-Dive Research

Date: 2026-06-07

Status: implemented

Area: docs/research/third-party

## Source Request

The user asked for a per-feature deep breakdown of 小神增强包 / DolocPlus, at the same level of detail as the earlier panorama-camera investigation.

## Summary

Added a read-only deep-dive research note for DolocPlus / 小神增强包. The new note separates DLL Harmony patch behavior from CE/Lua trainer behavior, maps each feature to native Doloc Town call paths where available, and records DTMAPI API lessons without copying third-party code.

## Changed Files

- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
- `docs/updates/INDEX.md`

## Validation

Documentation-only update. No build or game smoke was run.

Read-only inspection sources:

- `references/third-party-mods/小神增强包`
- temp-extracted `DolocPlus.dll`
- temp-extracted `DolocTownEA_Lua_1.8.1.CT`
- local decompiled Doloc Town build `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## Evidence

The deep-dive note records evidence labels for each feature:

- `DLL confirmed`
- `CE confirmed`
- `Game confirmed`
- `Inferred`

## Rollback

Remove the deep-dive note and the companion links, then remove this index entry.

## Follow-Up

Use the deep-dive only as compatibility research. Future implementation goals should choose one API family at a time, update hook/API matrices, and verify real native behavior in game.
