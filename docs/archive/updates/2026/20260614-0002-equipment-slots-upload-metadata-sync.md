# 20260614-0002 Equipment Slots Upload Metadata Sync

Status: implemented, package hash verified

## Source Request

User asked to update the local official upload packages under `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS` for DTMAPI and MoreEquipmentSlots, change the MoreEquipmentSlots author to `Yuuka`, replace the description with the new three-line Chinese text, compare source/build hashes against the local upload package, and use `D:\图片\封面图\更多饰品栏.png` as the MoreEquipmentSlots in-game icon and Steam preview image.

## Changed Files

- `testmods/MoreEquipmentSlotsMod/manifest.json`
- `testmods/MoreEquipmentSlotsMod/official-info.json`
- `testmods/MoreEquipmentSlotsMod/icon.png`
- `testmods/MoreEquipmentSlotsMod/preview.png`
- `tools/release/dtmapi-mod-publish-zh.json`
- `docs/updates/INDEX.md`

## Local Upload Paths

- DTMAPI runtime upload package: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- MoreEquipmentSlots upload package: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_MoreEquipmentSlots`

## Implementation

- Changed MoreEquipmentSlots source manifest author to `Yuuka`.
- Added the requested Chinese description to the MoreEquipmentSlots source manifest.
- Changed MoreEquipmentSlots official `info.json` source author to `Yuuka`.
- Set MoreEquipmentSlots `description`, `steamDescription`, and localized descriptions to:

```text
提供额外三个装备栏位，可以放帽子或四个特殊饰品。
只获得帽子和特殊饰品的效果，人物外观仍然加载官方自带的帽子栏位。
禁用mod后额外装备栏物品在进入存档后回退背包或邮件。
```

- Updated the local publishing metadata summary for MoreEquipmentSlots to the same author and description.
- Copied `D:\图片\封面图\更多饰品栏.png` to the MoreEquipmentSlots source package as both `icon.png` and `preview.png`.
- Rebuilt the Release outputs.
- Rebuilt the local DTMAPI upload package with `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly -OutputRoot C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`.
- Refreshed the MoreEquipmentSlots local upload package DLL, manifest, info, icon, preview, i18n, and `dtmapi-package.json` marker.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- Local DTMAPI upload package retained `workshop.json`.
- Local MoreEquipmentSlots upload package manifest and info fields verified:
  - `ManifestAuthor = Yuuka`
  - `InfoAuthor = Yuuka`
  - `ManifestDescription`, `InfoDescription`, and `SteamDescription` equal the requested Chinese text.
- MoreEquipmentSlots icon/preview image SHA256 matched the requested source image for all four targets:
  - Source image `D:\图片\封面图\更多饰品栏.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
  - Source package `testmods/MoreEquipmentSlotsMod/icon.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
  - Source package `testmods/MoreEquipmentSlotsMod/preview.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
  - Local upload `MODS\DTMAPI_MoreEquipmentSlots\icon.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
  - Local upload `MODS\DTMAPI_MoreEquipmentSlots\preview.png`: `5DF138A1093ECC7B8C20F86604E5845FBBF4776322D050257C8D15136B6C8F42`
- SHA256 source/build vs local upload package hashes matched:
  - `DTMAPI.BepInExBootstrap.dll`: `A0C8F8C994688B504F7B2EC72C0C702539D6A0FC877E4247D58145AF6C82EA8B`
  - `DTMAPI.Abstractions.dll`: `8E73E4C3D9B4D9FE0FBE7335D739366185EF86017B6FE15AB2FFB2482E729200`
  - `DTMAPI.Core.dll`: `859FCFDB85BFD55B3ADACC9D08245A017A8D297F95D221B519653AABDA8A5303`
  - `DTMAPI.GameBridge.DolocTown.dll`: `440CEEE5CFE32EA486C49117324B7C6BCA6E6D058AE2CAC2B16E264DE70B889E`
  - `DTMAPI.ModConfigMenu.dll`: `0F66A65731102675576A9EAF6AFDA27B5D95ED141D91C965477E0F54F4F08C5C`
  - `DTMAPI.MoreEquipmentSlots.dll`: `CD88EDEB47465ED311B7C89EEFAA1073D1782555B30CDE5DF097896178E3D540`

Not run:

- No game smoke was run because this update only changes package metadata/upload-source synchronization and reuses the already built protected-storage runtime.

## Related Records

- Protected storage implementation: `docs/updates/2026/20260614-0001-equipment-slots-protected-storage.md`
- Manual QA review: `docs/reviews/manual-qa/2026/20260614-0001-equipment-slots-protected-storage-review.md`

## Rollback

Restore the previous MoreEquipmentSlots manifest/official-info/publish metadata fields, remove or replace the source `icon.png` / `preview.png`, and resync `MODS\DTMAPI_MoreEquipmentSlots`. The runtime upload package can be regenerated again from the current branch with the same `build-release-workshop-packages.ps1` command.
