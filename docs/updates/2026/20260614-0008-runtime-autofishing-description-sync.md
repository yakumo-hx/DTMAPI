# 20260614-0008 Runtime and AutoFishing Description Sync

## Status

verified

## Source Request

User requested two publish-description updates:

- Add `点击主菜单左上角图标打开配置。` after the first DTMAPI Runtime intro paragraph.
- Add `游戏内默认F6开启。` as the last AutoFishing description line.
- Synchronize English and Traditional Chinese wording, write the source paths back, and refresh the local upload folders.

## Changed Files

- `tools/release/dtmapi-mod-publish-zh.json`
- `testmods/AutoFishingMod/official-info.json`
- `docs/updates/2026/20260614-0008-runtime-autofishing-description-sync.md`
- `docs/updates/INDEX.md`

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` as JSON and confirmed the DTMAPI Runtime Simplified Chinese, Traditional Chinese, and English localized descriptions contain the main-menu icon configuration line.
- Parsed `testmods/AutoFishingMod/official-info.json` as JSON and confirmed Simplified Chinese, Traditional Chinese, and English localized descriptions contain the default F6 toggle line.
- Refreshed the DTMAPI Runtime Workshop upload folder under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`.
- Refreshed the AutoFishing official-local upload metadata under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_AutoFishing`.

## Evidence

- DTMAPI Runtime local upload path: `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`
- AutoFishing local upload path: `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_AutoFishing`
- AutoFishing source/upload `info.json` SHA256 match: `D87389B88F9F9C60753BBA188FC3147A0F2170BE7199F0F30BEDB9E1F2C79848`
- AutoFishing source/upload DLL SHA256 match: `CEE4DE4A5F92B6185E3CFDCE98115F9D3012BCB27969F97514E2364E978581C9`

## Related Records

- `docs/updates/2026/20260613-0018-publish-metadata-writeback.md`
- `docs/updates/2026/20260614-0003-dtmapi-runtime-update-note.md`

## Rollback

Revert the two source metadata edits and rerun the same packaging/install scripts to regenerate local upload folders from the previous descriptions.

## Follow-Up

Upload the refreshed DTMAPI Runtime and AutoFishing local packages through the in-game Workshop upload flow, then restart Steam or resubscribe if the subscribed cache does not refresh immediately.
