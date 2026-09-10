# 20260612-0013 - DTMAPI Mod Publish ZH JSON

Status: verified
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: User requested one JSON package of DTMAPI Runtime plus local DTMAPI mod Chinese publishing and Steam UI metadata, ordered by mod name, author, version, in-game description, Steam name, and Steam description.

## Changed Files

- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/build-release-workshop-packages.ps1`
- `testmods/*/official-info.json`
- `testmods/*/manifest.json`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0013-dtmapi-mod-publish-zh-json.md`

## Summary

- Added `tools/release/dtmapi-mod-publish-zh.json` as a single Simplified Chinese metadata package for DTMAPI Runtime plus local DTMAPI mod publishing/Steam upload review.
- Added the DTMAPI Runtime Workshop item entry with `workshopId=3743016467`, version `0.5.1-alpha`, local upload path, short game description, and full Steam description text.
- Normalized player-facing Chinese wording: DTMAPI itself is described as the `运行前置`, ordinary public mods use `依赖 DTMAPI`, and only developer/unstable packages keep the Chinese `实验性` marker.
- Removed player-facing API implementation wording from `gameDescription` and `steamDescription`, and inserted line breaks after Chinese sentence periods so descriptions are easier to copy into the game/Steam UI.
- Synchronized the same player-facing wording back into the source `testmods/*/official-info.json` descriptions and related `manifest.json` descriptions so later export/generation does not reintroduce the old API-based text.
- Updated the Runtime Workshop package generator's embedded description text to match the cleaned DTMAPI Runtime entry.
- Included all local `testmods/*` folders that have both `manifest.json` and `official-info.json`.
- Preserved the requested player-facing fields first in each entry: `modName`, `author`, `version`, `gameDescription`, `steamName`, and `steamDescription`.
- Added `uniqueId`, local folder, manifest name, source `official-info.json` path, and release scope notes so upload candidates can be traced back to their source package.
- Marked `CropHarvestingQaMod` as `qaFixture` because its own description says it is a developer hand-test fixture and not a public Workshop upload target.

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` with PowerShell `ConvertFrom-Json`.
- Verified the JSON contains 17 entries: 1 DTMAPI Runtime entry plus 16 local test mod folders with both `manifest.json` and `official-info.json`.
- Verified scope counts: `runtime=1`, `published=8`, `developerOfficial=6`, `localOfficial=1`, and `qaFixture=1`.
- Verified player-facing descriptions no longer contain `依赖 DTMAPI Runtime`, `DTMAPI experimental`, `安装 Runtime`, `Developer Preview`, or `unsupported`.
- Verified `gameDescription` and `steamDescription` no longer contain API implementation wording such as `基于`, ` API`, `APIs`, `通过 DTMAPI`, `调用 DTMAPI`, or `由 DTMAPI`.
- Verified source `testmods/*/official-info.json` and `testmods/*/manifest.json` files parse as JSON and no longer contain the old player-facing API implementation wording.
- Decoded the Runtime Workshop generator's embedded UTF-8 description and verified it matches the cleaned Runtime text.
- `git diff --check` passed with existing line-ending warnings only.
- No game smoke was run because this update changes release metadata only and does not affect runtime, hooks, public API, installer behavior, or local mod loading.

## Evidence

- Source files: Runtime Workshop package metadata plus `testmods/*/manifest.json` and `testmods/*/official-info.json`.
- Generated package: `tools/release/dtmapi-mod-publish-zh.json`.
- Runtime package generator: `tools/scripts/build-release-workshop-packages.ps1`.

## Rollback

- Remove `tools/release/dtmapi-mod-publish-zh.json` and this update record if publishing metadata should stay per-mod only.

## Follow-Up

- If the official upload tool later requires a stricter schema, derive it from this aggregate file rather than re-reading chat history.
