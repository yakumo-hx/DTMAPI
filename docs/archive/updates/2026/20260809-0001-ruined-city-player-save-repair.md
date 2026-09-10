# Ruined-city legacy player-save pinpoint repair

- Update ID: `20260809-0001`
- Date: `2026-08-09`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Area: `support/player/save/mission/email/pinpoint-repair/archive-mutation`
- Source Request: 对 `D:\下载\DolocTownSave绘空事.zip` 只做存档修复并生成可直接发给
  玩家的文件；同时说明哪类故障前后存档和日志最利于继续判断
- Source Review:
  [20260808-0002](../../../reviews/manual-qa/2026/20260808-0002-public-newgame-and-functional-mod-regressions.md)

## Scope

Create one player-specific encrypted archive derived from the exact collected
slot `0` current. The repair may add only the official
`ruinedcity_continue` email object that `version_patch0900` would have sent for
the already-proven `visitedArgs[ruinedcity_main_entsk] > 0` state. It must not:

- lower `baseData.version` or replay the complete 0.99 migration group;
- start or complete `ruinedcity_main` directly;
- re-add the already-visited dialogue candidate;
- change archive index, time, inventory, world, task, dialogue or other email
  state;
- write the maintainer's live Steam AutoCloud tree or overwrite the source ZIP.

The output remains private player data under `D:\下载` and must not be tracked.

## Validation

- The exact source ZIP was reopened at `1,706,548` bytes and SHA-256
  `9026222023B609FE0115068FDE91C6AED084AE0D728856387AF135007A90E551`.
  Its encrypted `doloc-save-0.data` is `2,239,927` bytes, SHA-256
  `4179FE1F8D2FEB5DBF5EBA6F469E485BE0DC1CD3D491CD545E9448AF4F6B9CA6`;
  its decrypted JSON is `1,679,925` bytes, SHA-256
  `8D5FDBAF4BAC4C8E850290B821B23E025010D0831EB6C26B85A562E5A165BA50`.
- The repair rechecked all admission conditions before generation:
  `baseData.version=1.00.02`, `ruinedcity_main_entsk` visited, the candidate
  removed, normal `wetland_main` mail present, and no active/completed
  `ruinedcity_main` or existing `ruinedcity_continue`.
- The candidate plaintext differs from the source by one exact 337-byte
  insertion at the head of `emailManager.emails`. Email count changes only
  `136 -> 137`. The inserted native object is new/unread, not recycled or
  collected, and contains one
  `DolocTown.EmailAttachMission, Assembly-CSharp` for `ruinedcity_main` with
  `autoAccept=true` and `isAccept=false`. Its send date is the archive's
  unchanged `DateNow`. Removing that one inserted object reproduces the source
  plaintext exactly.
- `baseData.version`, root and base archive index, date/time, task/dialogue
  state, inventories, world data and all other mail bytes remain unchanged.
  The repair does not replay `version_patch0900` or any adjacent migration.
- The output was re-encrypted with the native `DOLOC-TOWN:` AES-CBC envelope,
  reopened from the final ZIP, decrypted, reparsed and checked for exactly one
  recovery mail. The encrypted archive is `2,240,375` bytes, SHA-256
  `3969F4FDFCA0A5452EF7119B4BDC1AD2227C1A1729F4022F8537B516C6344748`;
  repaired plaintext is `1,680,262` bytes, SHA-256
  `C67E29E8376947BA4B4865E4F73B9A911CF85F5B713CC97581994E79609DE855`.
- For runtime validation only, an isolated copy changed the two archive-index
  values from `0` to `11`; the delivered slot-0 archive was not changed.
  `docs/debug/evidence/GAME-SMOKE/20260809-074943` passed with the installed
  Runtime and only Workshop MoreSaves enabled. It recorded
  `SaveLoaded slot/index=11 isNewGame=False` and zero deserialization,
  NullReference, TargetInvocation, logged Error or exception matches.
- The run passed `NoNativeSave`, SaveFixtureIsolation, archive and committed
  sidecar unchanged-before-cleanup, process/fatal-window, official-profile
  restore and fixture cleanup gates. The isolated archive stayed exactly
  `B00A94B48A7C854C307646F446C2FE18636A63FDBAB395C9227C19E5A12E446D`;
  `PlayerArchiveWritebackPerformed=false`. No live AutoCloud archive or local
  slot 12 was read or written; the game exited and the runtime lock was
  released.
- `tools/scripts/check-doc-governance.ps1` passed after the Update, Review and
  active smoke row were finalized.

## Changed Files

- this Update and the 2026-08 monthly row;
- the owning Manual QA Review and active smoke matrix;
- the 2026-07-31 through 2026-08-01 smoke history page, created by moving two
  older active rows unchanged after the new runtime row crossed the router's
  governed size limit;
- one ignored/untracked player-specific ZIP under `D:\下载`.

## Output And Rollback

- Player-specific artifact:
  `D:\下载\DolocTownSave绘空事-旧城区任务修复.zip`.
- The ZIP contains only `doloc-save-0.data`; it is `1,687,534` bytes with
  SHA-256
  `3EFFE108F819C6CFBB2E7911A3D4294305E947A8110F611A567E387EB946B7F2`.
- This file is valid only for the collected player's slot 0 state. On first
  reading the new “关于旧城市废墟” mail, the native auto-accept attachment
  starts `ruinedcity_main`. Player-visible mission progression after that
  first read remains the final manual acceptance gate.

Delete the derived output ZIP. The collected source ZIP is the complete
rollback authority and is not modified. No player-side mutation is part of
this maintainer-side preparation. The private archive remains ignored and must
not be committed or sent to another player.
