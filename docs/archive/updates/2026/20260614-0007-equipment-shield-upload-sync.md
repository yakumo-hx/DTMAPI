# 20260614-0007 Equipment Shield Upload Sync

## Summary

Refreshed the official local upload folders for the DTMAPI runtime and MoreEquipmentSlots after the shield-hat protection update and verified source/build hashes against the local upload package contents.

## Source Request

User confirmed manual QA success for extra-slot shield hats and asked to update the local upload folders for DTMAPI and MoreEquipmentSlots, then check hashes against source/build outputs.

## Local Upload Paths

- DTMAPI runtime upload package: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- MoreEquipmentSlots upload package: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_MoreEquipmentSlots`

## Commands

- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly -OutputRoot C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild`

## Validation

- Final Release build/test passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Final third-save Steam NewContent smoke `GAME-SMOKE/20260614-120739` passed after the disabled-owner defense filter follow-up.
- DTMAPI runtime upload package retained `workshop.json`.
- MoreEquipmentSlots upload package retained `workshop.json`.
- MoreEquipmentSlots upload metadata still reports `InfoAuthor=Yuuka`, `ManifestAuthor=Yuuka`, and the requested three-line Chinese description.
- SHA256 source/build vs local upload package hashes matched:
  - `DTMAPI.BepInExBootstrap.dll`: `BB726F70EFA1A403971EB57D3B6FB759F20091C0B26B4A8160DB63E45790CD78`
  - `DTMAPI.Abstractions.dll`: `1CFEE5F61D978897646905EEA2BD91AF6ED67CCC504C0E4B1F8998809E7BA649`
  - `DTMAPI.Core.dll`: `A06AAF07458DF28219A7B12D9B7CF7E38154CD0835A3B861D153C3AAD3D89B45`
  - `DTMAPI.GameBridge.DolocTown.dll`: `BE16678A704CBFE8154E498914D1EBF0D1B7953A70CDDB4486D9514D0673B4F7`
  - `DTMAPI.ModConfigMenu.dll`: `5A61D454A2BCC1BC7FD9931198F68CBBBA3F0ECFF6A45DB622643DD39ADA398B`
  - `DTMAPI.MoreEquipmentSlots.dll`: `092807CC5C5DB359B40D5325EDD6EBC6860238E51F7F65014F5B39FBC0CF71ED`
  - MoreEquipmentSlots `icon.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
  - MoreEquipmentSlots `preview.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`

## Related Records

- Shield-hat protection: `docs/updates/2026/20260614-0006-equipment-slots-shield-hat-protection.md`
- Manual QA: `docs/reviews/manual-qa/2026/20260614-0003-equipment-slots-shield-hat-manual-qa.md`
- Previous upload metadata sync: `docs/updates/2026/20260614-0002-equipment-slots-upload-metadata-sync.md`

## Rollback

Regenerate the runtime upload package from the desired branch with the same `build-release-workshop-packages.ps1` command, then refresh MoreEquipmentSlots via `install-to-game.ps1 -Configuration Release -SkipBuild` or restore the previous `MODS\DTMAPI_MoreEquipmentSlots` folder from backup if a manual upload must be reverted.
