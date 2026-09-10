# 20260826-0001：MoreEquipmentSlots 新游戏档位复用重置

## Metadata

- Update ID: `20260826-0001`
- Date: `2026-08-26`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Source: 用户要求先保留当前 Product-v3 外挂侧车与禁用时物品回退能力，以最小补丁修复“删除原生存档后在同一位置新建存档会复用旧 Product 数据且首会话没有三个额外槽”；授权本地第十二存档位置用于任意保存、删除和新建测试，无需恢复原样，并要求同步本地官方上传目录后提交。
- Review: `docs/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md`
- Issues: `ISSUE-024`, `ISSUE-028`

## Intended Boundary

- 仅在 `SaveLoaded.IsNewGame=true` 且原生 holder 提供确切档位、该档位 current 文件尚不存在时，删除该档位的完整 MoreEquipmentSlots Product 目录并建立三个仅内存空槽。
- 既有存档继续使用 Product-v3 的 archive/name/clock 严格校验；不增加 `DeleteGame` Hook、owner incarnation、schema v4、公共存档 API 或原生内嵌存储。
- 新游戏第一次 `SaveSaving` 在名称对话结束后重新读取 scope，并沿用现有 gameplay-candidate 协议；仅该 pending 会话允许指纹 current 为 `missing`，实际 `prev*` 与 `.bak` 仍参与哈希。
- 正常保存成功后恢复严格 current-file 证明。未发起保存就返回标题时，不创建新 Product 侧车。
- 长期原生 archive 内嵌设计保留为玩家投票后的独立路线，不由本补丁预先决定。

## Changed Files

- `products/first-party/MoreEquipmentSlots/src/ModEntry.cs` now forwards both
  `SaveSlot` and `IsNewGame` to the Product runtime.
- `MoreEquipmentSlotsNativeRuntime` owns one bounded `newGamePending` branch:
  it resolves the initialized native archive index, rejects event/index drift
  or an already-present native current file, discards the exact stale Product
  slot directory, exposes an empty three-slot in-memory document, rebinds the
  final player name at first `SaveSaving`, and commits through the existing
  gameplay-candidate path.
- `EquipmentSlotDocumentStore` adds exact-shape, reparse-safe, idempotent whole
  Product slot-directory deletion plus an internal empty-document factory.
- `EquipmentSlotNativePlacement` and
  `EquipmentSlotTransactionJournal` add strict native archive-index/current
  checks and a NewGame-only fingerprint preimage whose current member may be
  `missing`; backup members retain their real hashes.
- Focused Product Unit and Harmony-owner fixture coverage exercises complete
  slot-directory cleanup, sibling preservation, same-name and different-name
  reuse, final-name rebinding, empty first save and cold load.
- Product metadata, Catalog source/target projection and publish-text
  projection advance MoreEquipmentSlots from `1.0.0` to `1.0.1`; Catalog
  status/freeze canaries are regenerated for that tracked source fact. The
  separate `currentPublishedArtifact` remains Steam `1.0.0`, and `releaseStop`
  remains active with no upload authorization. Runtime, public API, Product-v3
  schema and the five Harmony Hooks are unchanged.
- `docs/architecture/managed-product-admission-registry.md` is regenerated
  from the Catalog; its twelve-row admission set is unchanged and only the
  Catalog status date advances. `tools/scripts/check-product-catalog.ps1`
  updates the matching date, target-version and identity-freeze canaries.
- This Update, ISSUE-024, ISSUE-028, the originating manual-QA Review, monthly
  ledger and final smoke row carry the lifecycle/evidence record.

## Validation

- `DTMAPI_UNIT_TEST_FOCUS=moreequipment-product` passed with the repository
  .NET 8 toolchain. The first invocation exposed only a fixture path-length
  problem; routing that fixture through the existing bounded scenario-root
  helper removed the environmental failure, and the clean rerun passed. The
  only build diagnostics were pre-existing DebugConsole nullable warnings.
- `git diff --check` passed; Git reported only the repository's existing
  LF-to-CRLF checkout notices.
- The live installed build `24788406` correctly failed the hash-fixed Product
  Author build with `SDK202`; no reference policy was relaxed. The same source
  then built against the exact tracked `24456188` reference fixture and passed
  Author release validation (`389` files; fixture ZIP SHA-256
  `B83913B4EEBB9FB7382A941DF24920DA6011CB1CC945D3CE6BA4D9DA458BE774`).
- The deterministic seven-file `1.0.1` package passed validate/build/pack:
  ZIP SHA-256
  `4F0B078D8E7571D662CA7C5BEB8440130894D76E6EBDD4751F2462E63B57E891`,
  entry DLL SHA-256
  `3DD62441E3352671AA0ECDCDF5D93FF18BF74CC4109F8B7A1A738287B2F88749`,
  size `206336`, and Advanced reference receipt SHA-256
  `9B2924B668B6F8FF6F17102CE1FF5E46A2A3E52A1724CDBD649AE7A534BBFE2D`.
  It targets `netstandard2.0`, carries no bundled native dependency and keeps
  minimum Runtime `0.6.0`.
- `check-product-catalog.ps1` passed (`27` products, `11` public, `22`
  Workshop items and `48` API rows), including regenerated managed admission
  registry parity. `check-doc-governance.ps1` passed all `7005` checks, and
  `git diff --check` passed with only existing checkout line-ending notices.
- Authorized slot-12 `ArchiveMutation` first-save and independent cold-load
  runs both passed; exact evidence is recorded below.

## Evidence

- An authorized live-slot diagnostic used UI slot 12 / archive index `11`.
  Before NewGame, its exact Product directory contained a stale live sentinel,
  `.previous` and nested transition residue; native current was absent. At
  `2026-08-26 01:36:47 +08:00`, the game dispatched
  `SaveLoaded slot/index=unknown isNewGame=True`, followed by
  `MoreEquipmentSlots NewGame reset established archive=11;
  previousProductDirectoryDeleted=True`. The complete Product `slot-11`
  directory was absent immediately afterward. Returning to title without a
  save left both native current and the Product directory absent, and the game
  process exited cleanly.
- `GAME-SMOKE/20260826-022728` passed an AutoCloud-isolated
  `NativeSaveExpected` run against an occupied stale slot-11 Product-v3 fixture
  with extra residue. The real NewGame boundary again resolved archive `11`
  and reported `previousProductDirectoryDeleted=True`; the first native save
  dispatched `SaveSaving` at `02:28:10.003`, `SaveSaved` at `02:28:10.342`,
  and verified `Smoke.InstantSave` without reload. The committed native current
  was `1198583` bytes / SHA-256
  `C2775A8B4A10AC8421DB54AED08AABDFA179D23A30D35AD0E0C10668381A51C4`;
  the new Product-v3 live sidecar was `560` bytes / SHA-256
  `C153E0EB30A305584588B623A83AEA175BE9329FDA46778A1F46922CEF22F876`.
  Process exit, fixture isolation and no-fatal gates passed.
- `GAME-SMOKE/20260826-023044` independently cold-loaded that retained fixture
  as `SaveLoaded slot/index=11 isNewGame=False`. It logged no Product storage
  or event-handler error and passed `SaveLoaded`, QA observation, process exit,
  no-fatal, `PlayerSaveUnchangedBeforeCleanup` and
  `CommittedSidecarsUnchangedBeforeCleanup` in `NoNativeSave` mode. The runner
  then removed the marked disposable fixture.
- These runs establish the normal-player delete/reuse, first-save and cold-load
  lifecycle needed by the bounded correction. The issue remains `mitigated`
  rather than `verified`: a first-session equipment-UI screenshot was not part
  of this acceptance, and a Product absent throughout both native deletion and
  NewGame still cannot observe the new-owner boundary.

## Package / Upload Boundary

- Product version: `1.0.1`.
- The package payload is synchronized to
  `%LocalAppDataLow%/RedSawGames/DolocTown/MODS/DTMAPI_MoreEquipmentSlots` in
  `PreparedNotSubmitted` state. All seven package files match the ZIP by
  relative path, length and SHA-256; the eighth file is the preserved local
  `workshop.json` (SHA-256
  `EF4FD9462B44C8F84C61940052FBFC28143B5EB4D2E94A88979499C5D386543D`).
- The prior `1.0.0` upload tree was moved recoverably to
  `%LocalAppDataLow%/RedSawGames/DolocTown/.dtmapi-official-backups/DTMAPI_MoreEquipmentSlots-pre-1.0.1-20260826`.
- This Update does not authorize or perform a Steam Workshop submission; subscription bytes remain read-only evidence.

## Rollback Notes

- Restore the prior `1.0.0` Product source/package. The new code deletes only the exact reused Product slot directory at a proven NewGame boundary; the user explicitly authorizes leaving the test-only twelfth native slot in its final state.

## Follow-Up

- Player voting and any embedded native-archive storage spike remain separate work.
- The user's later Steam upload and the exact `1.0.1` subscription closeout are
  recorded by
  [20260830-0002](20260830-0002-moreequipment-101-workshop-release-closeout.md);
  that later observation does not change this Update's no-submit boundary.
