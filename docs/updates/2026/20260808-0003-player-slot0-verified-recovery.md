# Player slot 0 verified one-click recovery

- Update ID: `20260808-0003`
- Date: `2026-08-08`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Area: `support/player/save/recovery/archive-mutation/steam-autocloud`
- Source Request: 分析原生保存时 `currentRoom` 为空的原因，并使用维护者已取得的备份制作玩家一键恢复；恢复后黑屏则继续从新支持包定点修复，不在玩家电脑额外备份
- Source Review: [20260808-0001](../../archive/reviews/manual-qa/2026/20260808-0001-save-list-null-scene-after-debug-save.md)

## Scope

Build a case-specific recovery package for the collected slot `0` archive. The
player entry must:

1. require Doloc Town and the Steam client to be fully closed;
2. locate the native LocalLow SAVE directory without asking the player to find
   hidden AppData manually;
3. accept only the exact collected damaged current hash, or return idempotent
   success when the exact recovery hash is already current;
4. verify the embedded encrypted native backup or separately prepared pinpoint
   repair source by exact bytes and length before any mutation;
5. copy those bytes to a same-directory private partial, verify again, then use
   one atomic `File.Replace` with no backup output;
6. leave `.prev0...prev4`, JSON, Steam AutoCloud metadata and every unrelated
   save file unchanged;
7. fail closed for changed current bytes, reparse points, missing files,
   process races or post-write hash mismatch;
8. remain parseable by Windows PowerShell 5.1 and safe under special-character
   and non-ASCII paths.

This is an explicit `ArchiveMutation`. All automatic tests must use a disposable
synthetic SAVE fixture outside the live Steam AutoCloud tree. The case-specific
artifact may contain the player's still-encrypted `.prev0` but must remain
ignored/untracked and be sent only to that player.

## Changed Files

- `tools/release/player-slot0-recovery/1_restore_verified_slot0.bat`
- `tools/release/player-slot0-recovery/restore-verified-slot.ps1`
- `tools/release/player-slot0-recovery/README.txt`
- `tools/scripts/build-player-slot0-recovery.ps1`
- `tools/scripts/test-player-slot0-recovery.ps1`
- the source Review, this Update and the 2026-08 monthly row

## Validation

- `tools/scripts/test-player-slot0-recovery.ps1` passed under Windows
  PowerShell 5.1. Final retained synthetic evidence:
  `temp/player-slot0-recovery-test-20260808-230454-470-1f2c8b3e`.
- The matrix parsed the recovery, builder and test scripts with the Windows
  PowerShell 5.1 parser, then exercised an extracted package beneath a path
  containing spaces, parentheses, semicolon, `&` and Chinese characters.
- The successful path replaced only synthetic `doloc-save-0.data` through the
  no-backup `File.Replace` overload. The exact directory filename set remained
  unchanged, every `.prev`/JSON hash remained unchanged, the partial was gone,
  and the resulting current hash exactly matched the recovery source.
- Refusal paths covered a changed current, tampered recovery source and missing
  current; each kept the synthetic target unchanged. A second run against the
  recovered bytes returned explicit idempotent success.
- The builder test admitted exactly one current plus one `.prev0`, rejected
  byte ambiguity through mandatory hashes, and published exactly the BAT,
  PowerShell entry, README, manifest and encrypted recovery source.
- The same matrix separately exercised the new prepared-encrypted-source lane:
  the builder still pinned the collected current hash, admitted the prepared
  source only by its caller-supplied SHA-256, copied it under the fixed package
  filename, and recorded `RecoverySourceKind=PreparedEncryptedArchive` without
  weakening the production manifest's test-override restrictions.
- All BAT/PowerShell source files are ASCII-only with no UTF-8 BOM. The root
  BAT was invoked through `cmd.exe /d /e:off /v:off`; there are no compound
  command blocks around paths, so the prior `(x86)` parse regression is not
  reintroduced.
- The real player artifact was independently reopened after publication. Its
  recovery entry starts with the native `DOLOC-TOWN:` envelope, has length
  `3137187`, and has SHA-256
  `328718D4705186E271C34D8A737C10A4E99AAE169218293AC522A753360799A6`,
  exactly matching the support ZIP `.prev0`. The artifact contains no copy of
  the damaged current and no decrypted archive.
- A disposable exact-byte acceptance extracted the real artifact and copied
  the collected damaged current into an isolated fake LocalLow tree. Because
  Steam was running on the maintainer machine, the production manifest
  correctly returned exit `20` before mutation; the target remained exactly
  `B79E6E709C4B1A3F455845AF03839C77A10E0E5D91E01C2A0CFF7036D645FEE6`.
  The private fixture was deleted after verification.
- `tools/scripts/check-doc-governance.ps1` executed 6459 checks. Its two
  failures both pre-exist and are outside this Update: the separate untracked
  `20260807-0003-runtime-installer-convergent-candidate.md` lacks an August
  ledger row, which also causes the global row/record count mismatch. This
  Update's row is present and metadata-aligned; that installer candidate was
  left untouched.
- No live Doloc Town process was launched, no installed Runtime or Workshop
  tree was changed, and no live local/player Steam AutoCloud save was written.
  The successful archive-mutation matrix used only disposable synthetic files,
  so no runtime lock was required.

## Output And Rollback

- Player-specific artifact:
  `D:\下载\DTMAPI-slot0-recovery-20260808-4ca2895f.zip`.
- Artifact length: `2382788` bytes.
- Artifact SHA-256:
  `F9B98B4A1C626EE2816A1896E9F8B948209DCEF0C476C101FE17CB5E389275EE`.
- The manifest admits only slot `0`, the exact collected damaged current hash
  and the exact verified `.prev0` recovery hash. Production packages cannot
  enable the synthetic-test path/process overrides.
- The recovery source is private player data and remains only in the ignored
  ZIP; it is not tracked in the repository. Send this artifact only to the
  player whose support package supplied the archive.
- Source rollback removes the standalone recovery files, builder, test and this
  Update row. Player-side recovery intentionally creates no new backup; the
  maintainer retains the complete original support ZIP, including the damaged
  current and all five native backups, under the recorded support-package hash.
- The recovery mitigates this player's damaged current only. It does not close
  the separate DebugConsole room-readiness defect or the still-unclassified
  original flash exit. Until the product fix ships, the player must not use the
  DebugConsole native-save action again.

## Player outcome: recovered archive loads but room world is black

- The player reported that the recovery BAT displayed `[OK]`. They can now pass
  the official archive list and enter the slot, confirming the exact current
  replacement repaired the null-scene list blocker.
- The first restored load is not full gameplay acceptance: the screenshot shows
  portrait/bars, time/date/currency and hotbar data, but the entire room/world
  viewport is dark with no actor, terrain or objects. The user reports that the
  visible HUD state appeared only after pressing an “exit” key; the exact input
  is not yet logged.
- This leaves the recovery implementation `implemented` and its byte-level
  runtime matrix passed, but keeps the upstream room-entry incident open. The
  restored archive must not be saved from this state.
- The next required evidence is one fresh run of the existing read-only player
  collector immediately after exiting this black-screen session and before any
  further game launch. Root-cause analysis and acceptance are appended to the
  owning Manual QA Review rather than duplicated here.

## Follow-up pinpoint overlap repair candidate

- The requested evidence arrived as
  `D:\下载\DTMAPI-player-support-20260808-223245-408-aee326d1.zip`, length
  `14087881`, SHA-256
  `4BE74035D4E0E3FC2BF5358C27F61E02759E07F91F8B1FD9017CF5579B43EBAD`.
  All 25 entries were readable; collection status and save-copy status were
  complete. The collected current and `.prev0` both have SHA-256
  `328718D4705186E271C34D8A737C10A4E99AAE169218293AC522A753360799A6`,
  proving both the first recovery and the absence of a later black-screen save.
- Both retained cold launches fail at the same
  `AutomateBot.OnRemove -> IEquipmentHost.__HandleCrowedOutEquipments ->
  FarmArchiveData.AfterLoadData` NRE. Offline archive replay identifies one
  empty ChickenNest overlapping one occupied AutomateBotStation in the small
  barn; every native rolling backup contains the same conflict. The owning
  Review records the complete native ordering proof and supersedes the earlier
  scene-callback-first hypothesis for this incident.
- The minimum prepared recovery source moves only that empty ChickenNest one
  cell left. It remains in the native encrypted envelope, keeps length
  `3137187`, and has SHA-256
  `CF27A15E26620A7A4AC4ED4389AC2BAAB13CE90D123A80325A25B6D3FE53C980`.
  Its decrypted JSON reparses at the same plaintext length and differs in only
  the three bytes belonging to `position.x:33.0 -> 31.5` and
  `anchor.x:11 -> 10`; the station, bot and all other archive data are unchanged.
- `tools/scripts/test-player-slot0-recovery.ps1` passed the complete Windows
  PowerShell 5.1 parser, atomic replacement, no-player-backup, unchanged-backup,
  refusal, idempotence, BAT special-path, exact-package and prepared-source
  matrix. The retained evidence is
  `temp/player-slot0-recovery-test-20260808-230454-470-1f2c8b3e`.
- The published pinpoint package was reopened and has exactly five expected
  entries. A disposable exact-byte fake LocalLow acceptance used the real
  package script and payload under a path containing spaces, parentheses,
  semicolon, `&` and Chinese characters: first replacement returned `0`, the
  current changed from the collected hash to the prepared-source hash, existing
  `.prev0` stayed byte-identical, no extra SAVE filename appeared, and a second
  run returned idempotent success. The private fake save tree was deleted after
  the checks.
- Superseding player-specific artifact:
  `D:\下载\DTMAPI-slot0-black-screen-repair-20260808-aee326d1.zip`, length
  `2382870`, SHA-256
  `9124794521DD80FE2221D38BBF708D038EE538065C53A35C189F8DED87FA535B`.
  Its manifest admits only the exact already-restored current and the exact
  pinpoint repair source. The earlier `.prev0` recovery artifact is retained as
  audit evidence but must not be sent as the next attempt because all backups
  carry the overlap.
- No live game/runtime tree or Steam AutoCloud save was touched and no game was
  launched, so no runtime lock was required. Lifecycle remains `implemented`:
  byte-level recovery validation passed, while player cold-load/world-visible
  acceptance is still pending. The player must not perform a native save unless
  the repaired room renders normally.
