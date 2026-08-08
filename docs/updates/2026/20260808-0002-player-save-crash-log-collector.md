# Player SAVE and crash-log one-click collector

- Update ID: `20260808-0002`
- Date: `2026-08-08`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Area: `support/player/save/crash/log-collector`
- Source Request: 玩家找不到隐藏的 SAVE 目录；制作双击后自动定位并打包完整存档与闪退相关日志的独立脚本
- Source Review: [20260808-0001](../../reviews/manual-qa/2026/20260808-0001-save-list-null-scene-after-debug-save.md)

## Scope

Create a standalone support package, separate from the four canonical Runtime
Workshop installer actions. It must:

1. refuse collection while `DolocTown.exe` is running;
2. locate both `RedSawGames\DolocTown` and legacy spaced persistent roots,
   then copy the complete `SAVE` directory without changing source bytes;
3. collect current/previous Unity logs, all DTMAPI current/history logs,
   BepInEx `LogOutput.log`, DTMAPI state/report evidence, Unity crash trees,
   matching local Windows crash dumps and bounded Application crash events;
4. discover the game from explicit support overrides, prior DTMAPI support
   summaries, Steam libraries, the known Tencent `FileRecv` layout and script
   ancestors, while allowing unrelated missing sources;
5. verify each copied source through stable length/mtime plus source/destination
   SHA-256, write an inventory and collection summary, publish one ZIP only
   after staging completes, and report an incomplete result when SAVE is absent
   or any source copy is unstable;
6. remain ASCII-source-compatible with Windows PowerShell 5.1 and survive
   spaces, parentheses, `&`, semicolons and non-ASCII filesystem paths.

The collector is read-only with respect to every player/game source. It does
not decrypt, parse, repair, rename, delete or restore an archive. Temporary
collector-owned staging may be removed only after the completed ZIP has been
published.

## Changed Files

- `tools/release/player-save-crash-collector/1_collect_save_and_crash_logs.bat`
- `tools/release/player-save-crash-collector/collect-save-and-crash-logs.ps1`
- `tools/release/player-save-crash-collector/README.txt`
- `tools/scripts/test-player-save-crash-collector.ps1`
- this Update and the 2026-08 monthly row

## Validation

- `tools/scripts/test-player-save-crash-collector.ps1` passed against the
  source package. Evidence:
  `temp/player-save-crash-collector-test-20260808-204035-858-f41af27e`.
- The generated distributable ZIP was extracted beneath a path containing
  spaces, parentheses, a semicolon, `&` and Chinese text, then the same full
  matrix passed against those exact extracted bytes. Evidence:
  `temp/player-save-crash-collector-test-20260808-204035-855-72e60917`.
- Both runs invoked the root BAT through `cmd.exe /d /e:off /v:off`, parsed
  the collector with Windows PowerShell 5.1, and exercised a player-shaped
  fixture containing current, `.prev0`, `.prev1`, `.bak`, JSON and nested SAVE
  files. Every packaged SAVE file matched the stable source SHA-256 and all
  source SAVE hashes remained unchanged.
- The matrix also covered a one-megabyte current DTMAPI log, history log,
  BepInEx log, DTMAPI report/state and DebugConsole last-action evidence,
  `Player.log`/`Player-prev.log`, Unity `error.log`/`crash.dmp`, a Windows local
  crash dump, prior-bundle game discovery, successful staging cleanup, and an
  absent-SAVE run that published an explicitly `Incomplete` diagnostic ZIP
  with exit code 2.
- Static release checks confirmed the BAT and PowerShell source are ASCII-only
  without a UTF-8 BOM, all five changed source/record files have no trailing
  whitespace, and all three ZIP entries are byte/hash-equal to their tested
  source files.
- `tools/scripts/check-doc-governance.ps1` executed 6437 checks. Its two
  failures both pre-existed this Update: the separate untracked
  `20260807-0003-runtime-installer-convergent-candidate.md` is not linked from
  the August ledger, so the global row/record count also differs by one. This
  Update's own exact monthly row is present and metadata-aligned; the unrelated
  candidate record was left untouched.
- No live Doloc Town process was launched, no installed Runtime/Workshop tree
  was changed, no player or Steam AutoCloud save was read or written, and no
  runtime lock was required for the isolated temporary matrix.

## Output And Rollback

- Distributable:
  `dist/DTMAPI-player-save-crash-collector-20260808.zip`.
- Artifact length: `8997` bytes.
- Artifact SHA-256:
  `54606c8491715cfa4381a72b373ea54f9d413d70c245772c13c7fac283ce36aa`.
- The ZIP contains exactly the three player-facing files: the root BAT,
  PowerShell collector and `README.txt`. It contains no repository, fixture,
  test or pre-collected player data.
- Rollback is deletion of the standalone collector source and this Update row;
  no Runtime package, installed game, Workshop source or player data is
  modified by this implementation.
