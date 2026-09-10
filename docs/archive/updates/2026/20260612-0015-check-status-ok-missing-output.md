# 20260612-0015 - Check Status OK Missing Output

Status: verified
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: User reported that `3_check_dtmapi_status.bat` output was too subtle because success was not obvious and missing files only appeared as plain `missing`; requested old-package-style clear `OK` or `missing` prompts.

## Changed Files

- `tools/scripts/check-dtmapi-status.ps1`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0015-check-status-ok-missing-output.md`

## Summary

- Reworked status output into explicit `[OK]`, `[MISSING]`, `[WARN]`, and `[INFO]` lines.
- Added a required install-file section that checks Doorstop, BepInEx, Harmony, the DTMAPI plugin folder, the five runtime assemblies, install state, and release manifest individually.
- Added a top-level required-file summary so support screenshots show whether the install is complete.
- Kept diagnostics, official local DTMAPI packages, legacy detections, and next steps visible, but gave them clear status prefixes.

## Validation

- Ran Windows PowerShell syntax validation for `tools/scripts/check-dtmapi-status.ps1`.
- Ran `tools/scripts/check-dtmapi-status.ps1` against the local Doloc Town install.
- Output showed `[OK]` for required install files, `[INFO]` for installed/package version details, `[OK]` for current diagnostics, and `[WARN]` for legacy detections.
- Regenerated the local Runtime Workshop upload package with `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly`.
- Ran the packaged `Content/DTMAPIInstaller/tools/check-dtmapi-status.ps1` from `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`.
- Verified packaged installer tools still parse under Windows PowerShell.
- No game smoke was run because this change only affects installer/status console output.

## Evidence

- Local status output included:
  - `[OK] Doorstop winhttp.dll`
  - `[OK] BepInEx core`
  - `[OK] DTMAPI.BepInExBootstrap.dll`
  - `[OK] Required DTMAPI install files are present.`
  - `[WARN] Legacy detections: 16`
- Final local upload package evidence: `workshop.json.workshop_id=3743016467`, `info.json.version=0.5.1-alpha`, packaged `check-dtmapi-status.ps1` starts with `EF BB BF`, and packaged installer tools parsed successfully.

## Rollback

- Restore the previous compact `present`/`missing` status lines in `tools/scripts/check-dtmapi-status.ps1`.
- Regenerate the Runtime Workshop local upload package so `3_check_dtmapi_status.bat` contains the reverted status script.

## Follow-Up

- Regenerate and upload the Runtime Workshop item after this change so player-side status checks use the clearer support output.
