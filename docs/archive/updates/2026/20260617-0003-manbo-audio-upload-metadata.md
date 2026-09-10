# Manbo Audio Upload Metadata

## Source

User decided to publish `DTMAPI 曼波音频替换开纸箱子` and requested the author, description, in-game icon, Steam preview, and local upload folder hashes be synchronized before manual Workshop upload.

## Changed Files

- `testmods/ManboCardboardAudioMod/manifest.json`
- `testmods/ManboCardboardAudioMod/official-info.json`
- `testmods/ManboCardboardAudioMod/icon.png`
- `testmods/ManboCardboardAudioMod/preview.png`
- `testmods/ManboCardboardAudioMod/assets/manbo.wav`
- Local upload folder: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\Yuuka_DTMAPI_ManboCardboardAudio`

## Changes

- Updated the mod author metadata to `Yuuka`.
- Updated Simplified Chinese game/Steam description to: `实验性音频替换mod实例。将野外刷新的纸箱开启音效改为曼波。比较抽象，介意勿下。`
- Added Traditional Chinese and English localized description equivalents in `official-info.json`.
- Copied `D:\图片\封面图\曼波音频替换.png` to source and local upload `icon.png` / `preview.png`.
- Replaced `assets/manbo.wav` with the user's shorter edited WAV and synchronized the source asset, Release output asset, and local upload package asset.

## Validation

- Parsed source `manifest.json` / `official-info.json` and local upload `info.json` / `Content/DTMAPI/manifest.json` as JSON.
- Verified local upload package hashes after sync.
- No game smoke was rerun for this metadata/image-only packaging update; the previous real paper-box audio smoke remains `GAME-SMOKE/20260617-065447`.

## Rollback

Restore the previous `manifest.json`, `official-info.json`, `icon.png`, `preview.png`, and `assets/manbo.wav`, then resync `MODS\Yuuka_DTMAPI_ManboCardboardAudio`.
