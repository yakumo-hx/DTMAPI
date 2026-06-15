# 20260616-0002 Runtime Description Log Retention Note

## Status

Verified Steam subscription sync.

## Source Request

User requested a DTMAPI Runtime description update: remove the sentence about local/friend long-term testing not reproducing most reported issues, then add a bottom `update 0616` note saying log files keep only the most recent ten game startup logs. The Simplified Chinese, Traditional Chinese, and English descriptions must be synchronized and the local official upload package refreshed.

## Changed Files

- `tools/release/dtmapi-mod-publish-zh.json`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260616-0002-runtime-description-log-retention-note.md`

## Implementation

- Updated the DTMAPI Runtime publish metadata date to `2026-06-16`.
- Removed the requested "本地运行和好友长期运行..." / Traditional Chinese / English equivalent sentence from DTMAPI Runtime `gameDescription`, `steamDescription`, and localized descriptions.
- Added bottom update notes:
  - Simplified Chinese: `update 0616` / `日志文件仅保留近十次游戏启动日志。`
  - Traditional Chinese: `update 0616` / `日誌檔案僅保留最近十次遊戲啟動日誌。`
  - English: `update 0616` / `Log files only keep the most recent ten game startup logs.`
- Rebuilt the DTMAPI Runtime Workshop staging package and synced it to the official local upload directory while preserving `workshop.json`.
- Confirmed the Steam subscription copy updated after Workshop upload.

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` with `ConvertFrom-Json`.
- Ran `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly`.
- Synced `dist/workshop-packages/DTMAPI` to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Verified local upload `workshop.json` still contains `workshop_id=3743016467`.
- Verified source metadata does not contain the removed phrase and has the expected Simplified/Traditional/English update tails.
- Verified dist/local upload `info.json` hashes matched immediately after package sync:
  - SHA256 `F8C408841BD8839C40F155FDAA13A19FE708392447FE8880A9FA92B6A6554AAB`
  - Length `8349`
- Verified the uploaded Steam subscription copy and official local upload copy now match for all shared files:
  - Shared files checked: 23
  - Shared-file hash mismatches: 0
  - Steam subscription `info.json`: SHA256 `F8BF2DF6CF7C4ADD01FA83B74B47477C30DF96C587208F00844E39A38BE63377`
  - Official local upload `info.json`: SHA256 `F8BF2DF6CF7C4ADD01FA83B74B47477C30DF96C587208F00844E39A38BE63377`
- Confirmed both subscription and upload descriptions contain `update 0616` and do not contain the removed local/friend-testing sentence.
- Confirmed the only `info.json` difference between staging and upload/subscription is the official tool adding an empty `localized_name` object.
- Confirmed the Steam subscription copy still includes installer-carried `Content/.tools/bepinex` files that are not kept in the local upload source folder.

## Evidence

- Local upload path: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- Steam subscription path: `D:\steam\steamapps\workshop\content\2285550\3743016467`
- Staging path: `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI`
- Source metadata hash: SHA256 `5FC6A455868E92038FD9330FACCFB8F6B61DEE855745C2FDBE0312B4CD541B35`, length `20267`.

## Rollback

Revert the DTMAPI Runtime entry in `tools/release/dtmapi-mod-publish-zh.json`, rebuild the runtime Workshop package, and resync the official local upload package.

## Follow-Up

No follow-up for this description update.
