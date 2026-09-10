# 20260614-0003 - DTMAPI Runtime Update Note Metadata

## Status

Verified for packaging metadata.

## Source Request

User requested adding a bottom note to the DTMAPI Workshop/local description, with Simplified Chinese, Traditional Chinese, and English coverage:

> 近期更新频率较高，如果您发现有mod使用不了或报错，请先尝试运行1_xxx更新DTMAPI。

The concrete runtime package script is `1_install_dtmapi.bat`, so the published text names that file directly.

## Changed Files

- `tools/release/dtmapi-mod-publish-zh.json`
  - Added the update-frequency note to the DTMAPI Runtime `gameDescription` and `steamDescription`.
  - Added `localizedDescription.schinese`, `localizedDescription.tchinese`, and `localizedDescription.english` for the runtime entry.
- `tools/scripts/build-release-workshop-packages.ps1`
  - Runtime Workshop `info.json` generation now reads localized runtime descriptions from publish metadata instead of copying Simplified Chinese into every language field.
- Local official upload package:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI\info.json`

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` with `ConvertFrom-Json` and confirmed all three localized runtime descriptions are available.
- Rebuilt the local official DTMAPI upload package:
  - `tools\scripts\build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly -OutputRoot "C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS"`
- Confirmed local upload `info.json` contains:
  - Simplified update note in `description`, `steamDescription`, and `localized_description.schinese`.
  - Traditional update note in `localized_description.tchinese`.
  - English update note in `localized_description.english`.
  - Existing `workshop.json` preserved.

## Evidence

- Source metadata SHA256:
  - `tools/release/dtmapi-mod-publish-zh.json`: `9557658FC7684456BE56755B95D5951508B4ABBCCA8670FF835251B0FD6174ED`
- Packaging script SHA256:
  - `tools/scripts/build-release-workshop-packages.ps1`: `4E62890ADF5049B86F5D40E4890EAD0DF833794E1DB00C7055D1AF81DDF0F140`
- Local upload `info.json` SHA256:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI\info.json`: `0E7C982E9BD2CC28AA0361C51A4017A85320CDC992F949903EF048634ED192BF`

## Not Run

- Release build/test were not run because no runtime assemblies or C# source changed.
- Game smoke was not run because this is packaging metadata only.

## Rollback

Revert this update record, remove `localizedDescription` from the runtime publish metadata entry, restore the previous runtime description strings, and restore the runtime `localized_description` generation to a single copied description if needed.

## Follow-Up

After manual Steam upload, check the subscribed Workshop copy of `info.json` against the local upload package and remind users that after DTMAPI files update through Steam, running `1_install_dtmapi.bat` updates the installed runtime in the game folder.
