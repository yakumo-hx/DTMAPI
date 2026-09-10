# 20260612-0006 - Runtime Workshop Crops API Description

## Status

Verified

## Source Request

User requested the local DTMAPI Runtime upload package description be updated before Workshop upload:

> 新增 Experimental Crops / Harvesting API，供 Mod 作者制作作物盆自动收获类 Mod；暂时未实现乔木盆自动收获。

## Changed Files

- `tools/scripts/build-release-workshop-packages.ps1`

## Summary

Updated the generated `DTMAPI_Runtime/info.json` description template for the Runtime Workshop package.
The new description announces the Experimental Crops / Harvesting API for crop-basin auto-harvest mod authors and explicitly says tree-basin auto-harvest is not implemented yet.
The PowerShell script keeps the generated Chinese text through ASCII-safe codepoint construction so Windows PowerShell can parse the release script consistently.

This is packaging metadata only. It does not change gameplay behavior, public API shape, hook/status IDs, installer behavior, or the selected eight published feature-mod package list.

## Validation

- `git diff --check`
- PowerShell AST parse for `tools/scripts/build-release-workshop-packages.ps1`.
- Regenerated `dist/workshop-packages/DTMAPI_Runtime`.
- Verified generated `info.json.description` equals the requested upload text.

## Evidence

- Crops / Harvesting API hardening smoke: `GAME-SMOKE/20260612-072544`.
- Manual QA record: `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md`.

## Rollback

Revert the `description` value in `tools/scripts/build-release-workshop-packages.ps1` and regenerate the Runtime Workshop package.

## Follow-up

If tree-basin cocoa harvest support is desired, keep it as a separate native-owner review and API hardening slice before changing this wording.
