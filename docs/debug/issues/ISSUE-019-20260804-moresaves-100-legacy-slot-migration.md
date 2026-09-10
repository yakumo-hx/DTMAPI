# ISSUE-019: MoreSaves 1.00 legacy slot-family migration

- State: `verified`
- Current boundary: Corrected Local 1.0.1 passed isolated 16-role migration, populated official #7--#12 UI, native index 6 load, cold `0/0` idempotence, native index 11 load and clean exit without player archive writeback.

## Status

- Previous status wording: `verified / corrected live migration plus disposable official-UI, extra-slot load, idempotence and clean-exit revalidation passed`
- Opened: `2026-08-04`
- Historical resolution: `2026-08-04` (superseded by the `2026-08-05` user correction below)
- Reopened: `2026-08-05`
- Severity: high
- Area: MoreSaves / ProductNative / save discovery / 1.00 migration / smoke preflight
- Related review: `docs/reviews/manual-qa/2026/20260804-0001-moresaves-100-legacy-slot-migration.md`
- Owning update: `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`

## Symptom

MoreSaves successfully changes the official archive count from 6 to 12, so all
twelve UI cells render, but existing extra slots 6–11 appear empty after the
Doloc Town 1.00 upgrade. Separately, the formal AutoFishing matrix falsely
reported UI #5/index 4 missing before launch.

## Known Facts

- Doloc Town 1.00 names current archives `doloc-save-{index}.data`; the
  player's indices 0–5 use that form, including `doloc-save-2.data` and
  `doloc-save-4.data`.
- Existing extra archives remain under the old
  `ea-playtest-doloc-archive-{index}.data` family for indices 6–11.
- Native `LocalSave.Validate()` runs while the native count is still 6, writes
  `convert_data_100=true`, and migrates only indices 0–5 before DTMAPI starts
  MoreSaves and sets the count to 12.
- The current native destination family is current plus `.prev0…N` and `.bak`;
  the old family used current plus `-prev.data` and `-bak.data`.
- `tools/scripts/common.ps1` resolved the selected current archive with the old
  filename, so matrix `20260804-100952-70e21c42` stopped on a false preflight
  premise. No game process or save mutation occurred in that run.

## Root Cause

The product changes `archiveFileCount` only after the one-time native 1.00
conversion has completed. The conversion flag prevents a later native retry,
and the product had no bounded migration for its ProductNative-only indices.
The smoke helper independently froze the obsolete current filename instead of
the 1.00 native name.

## Rejected Approaches

- Increasing the native count before `LocalSave` construction through a
  mandatory Runtime or SharedNative pre-Awake patch.
- Rewriting or clearing `convert_data_100`, or copying native conversion code
  into the platform.
- Overwriting a current destination, deleting a legacy source, or adding a
  player conflict/recovery UI for this bounded migration.
- Decrypting archives, invoking private `FixArchiveIndex`, requiring a complete
  current/prev/bak family, hashing file contents, or adding a rollback journal;
  these are not part of the native filename-role conversion semantics.
- Exercising migration against the live Steam AutoCloud save tree.

## Acceptance Criteria

- The smoke helper resolves `doloc-save-{index}.data` and freezes its exact
  `.prevN/.bak` set; legacy fixed backup names remain excluded.
- Enabled MoreSaves performs one synchronous, zero-Harmony ProductNative pass
  for indices 6–11 before publishing count 12.
- The product obtains every official current path from native
  `GetDataFullPath`, requires the exact `doloc-save-{0}.data` format and one
  shared SAVE root, then maps each old current/prev/bak role independently to
  current/`.prev0`/`.bak` without reading archive contents.
- A missing source is skipped. An existing destination is never overwritten;
  its old source is retained and migration continues. Otherwise the source is
  moved and the only postcondition is source absent plus destination present.
- No legacy files is a successful non-creating no-op. An injected interruption
  keeps count 6 for that activation; already completed moves remain in place and
  a second pass resumes from the remaining old names without flag, journal or
  rollback authority.
- Any runner that enables MoreSaves while an exact legacy candidate exists must
  install and prove the current `LocalSave.cloudDirPath` disposable guard before
  Runtime Mod discovery; a live-tree `NoNativeSave` run must stop before launch.
- Source/unit/policy/package checks pass on managed temporary fixtures. A later
  player-facing close requires a clean restart, isolated migration evidence,
  official UI discovery of migrated slots, logs, and clean process exit.

## 2026-08-04 Source Implementation And Isolated Validation

- `MoreSavesArchiveMigration` now runs after the existing MoreSaves owner lease
  is acquired and before `archiveFileCount=12` is published. It reflects the
  current manager's private `GetDataFullPath` and `LocalSave.FixArchiveIndex`
  path instead of copying native decryption or archive-index logic.
- The core pass is restricted to indices 6--11 and the exact old
  current/`-prev`/`-bak` mappings. It performs a complete read-only preflight
  before the first move, preserves conflicts, verifies pre/post fingerprints,
  and can resume after an injected partial move without a journal. A failure
  releases the activation owner and leaves the native count at 6.
- Focused MoreSaves Unit tests cover zero-source no-op, all 18 mappings,
  same/different destination conflicts, index mismatch before the first move,
  interruption/resume/idempotence, post-move mutation detection, activation
  rollback and the expected private reflection shape. The focused source gate
  separately locks the exact current decompile member tokens; neither check is
  a real player-process private reflection binding.
- PowerShell 7 and Windows PowerShell 5.1 both pass the current/history Runtime
  floor, MoreSaves source-policy and Catalog gates. Two SDK-built package trees
  produced the same candidate ZIP SHA-256
  `7A74168D59191BCF0D214C8FA81B853B055673854EAA3A4707125D408C2A7F9F`.
- The smoke helper now defaults to `doloc-save-{index}.data`; both PowerShell
  hosts pass its focused current/`.prevN`/`.bak` family tests.
- No game was launched, no Runtime lock was acquired, and no player save,
  Steam subscription or installation directory was read for migration or
  mutated. These checks close the source/tooling portion only; the issue remains
  `open` pending the disposable player-facing acceptance in the criteria above.

## 2026-08-04 Fourth Parallel Review Corrections

- Review `docs/reviews/code/2026/20260804-0011-dtmapi-060-fourth-five-slice-parallel-review.md`
  found that the disposable redirect was installed after Runtime Mod Entry and
  still targeted the removed `get_dataDirPath`. Bootstrap now prepares the
  receipt-bound QA owner before `runtime.Start()`, patches exact
  `get_cloudDirPath`, and probes the current `fileDataHandler` full/backup/prev/
  temp helpers against the exact disposable SAVE root before Mod discovery.
- `run-game-smoke.ps1` now parses the actual post-profile `mod_infos.json` and
  enumerates only the 18 legacy current/prev/bak candidates for indices 6--11.
  Enabled MoreSaves plus any candidate requires disposable `ArchiveMutation`,
  StageQaHost and DirectExe with Steam launch disabled; otherwise it writes a
  preflight receipt and stops before launch.
- The migration now treats a missing SAVE directory as a non-creating no-op,
  rechecks root/candidate nodes around active operations, compares conflict
  content by length/SHA-256 rather than mtime, and reverses a changed new
  destination to its legacy name. An exact final rename followed by a reported
  move exception fails the current activation but is safely inferred on the
  next startup. Deferred activation failures share the native-six/owner-release
  cleanup path.
- Focused and full Unit, QA Unit, required exact-reference Author SDK, both-host
  save-mode/MoreSaves/release-set checks and both-host AST parsing passed. No game
  was launched and no Runtime lock or player/Steam save tree was used. The real
  current private binding, official UI discovery/load/restart and clean exit are
  still unproved, so this issue remains `open`.

## 2026-08-04 Isolated Player Acceptance

- Runtime commit `7ab6d567396b0d9e154df113d01bfd3a8d44f72b` loaded the
  SDK-built MoreSaves `1.0.1` package
  `EF1E15EB9A85B39667AAC281A88BEC432436713954DC27D2CD37517CD44F5A7C`;
  its entry DLL was
  `A4E0A0FAB6EA0A8B442AB1FFF6850FA4CA8A7D39EBE445DA5479B2B61F7A02DA`.
- `GAME-SMOKE/20260804-212108` intentionally preserves the first invalid
  fixture result. Its index-10 backup came from the player's real
  `ea-playtest-doloc-archive-10-bak.data`; native `FixArchiveIndex` proved that
  it did not already contain target index 10. MoreSaves rejected the complete
  family before the first move, left all 18 fixture sources and zero current
  destinations, kept the native count at 6, released its owner, and exited
  without a fatal window. This is expected fail-closed behavior, not an
  acceptance PASS.
- A fresh disposable fixture then supplied a complete, native-valid 6--11
  current/prev/bak family by using each index's validated current bytes for its
  three members. `GAME-SMOKE/20260804-212423` passed the pre-Runtime
  `LocalSave.cloudDirPath` isolation probe, validated and moved all 18 members,
  published `archiveFileCount 6->12`, showed populated official UI cells
  #7--#12, and loaded slot/index 6 through the official path. Post-process
  fingerprints proved all 18 legacy names absent and all 18 official
  destinations byte-exact.
- A cold second process, `GAME-SMOKE/20260804-212602`, observed
  `legacySources=0, validated=0, moved=0`, republished 12 slots without another
  migration, showed the same populated official cells, and loaded slot/index
  11. All destination length/SHA-256 values remained exact. Both acceptance
  processes passed isolation, official UI/evidence, `SaveLoaded`, process exit,
  fatal-window, profile, source-state and QA cleanup gates without a routine
  player-byte backup or archive writeback.
- The player's live 6--11 legacy candidate domain was compared read-only before
  and after the isolated work. The original receipt mistakenly represented two
  absent identities with duplicate current names; the retained
  [correction receipt](../evidence/GAME-SMOKE/20260804-212602/live-legacy-source-state-correction.json)
  now proves the exact 18-name set: all 16 present files remained
  length/SHA-256-identical, while index-10 `prev` and index-11 `bak` remained
  absent. The disposable fixture was deleted after its evidence was extracted.
  Author SDK withdraw state was validated against its exact package/receipt/tree,
  moved
  into the retained isolated build root for recoverability, and the game was
  restored to MoreSaves `AbsentNoJournal`. Final Player Doctor reported five
  Runtime artifacts, zero errors and zero warnings; no game process remained
  and the shared Runtime lock was released.

These runs close the product defect and player acceptance for a valid complete
legacy family. They do not alter or waive the native index invariant. The real
player file `ea-playtest-doloc-archive-10-bak.data` remains byte-identical and
cannot be migrated by the chosen no-rewrite design because it carries a
different internal index. Deciding whether to discard, replace, or separately
recover that one local backup is an explicit player-data decision with the user;
it is not a reason to weaken MoreSaves or block the 0.6 package.

## 2026-08-05 Final Candidate11 Live-Root Preflight

- Clean commit `564e5450f2cd2db3e745acec0f1f76c4671e1244` passed the
  canonical Release suite from the first gate and atomically produced the final
  Runtime plus nine source-product packages. Exact retained Manbo and
  MoreEquipmentSlots were added without rebuilding; PowerShell 7 and Windows
  PowerShell 5.1 both accepted the mixed candidate as `11 / 9 / 2` with nine
  Advanced bindings.
- The exact `564e5450f2cd` Runtime was installed under the shared lock and all
  five DLL bytes plus the installed `0.6.0 / 0.6.0.0` manifest projection
  matched the candidate. Candidate11 transaction
  [`20260805-034320-5e79c980`](../evidence/CANDIDATE11/20260805-034320-5e79c980/99-result.json)
  then enabled the exact Local11 package trees but stopped inside the smoke
  preflight before Steam launch or any `DolocTown.exe` process.
- The stop is the expected ISSUE-019 safety classification: enabled MoreSaves
  saw the same `16` exact legacy candidates under the live `SAVE` root that the
  isolated acceptance retained byte-identically. A live-root `NoNativeSave`
  run cannot permit the product's startup migration, while Candidate11's
  ordinary `UseSteam / no-QA / slot 3` contract cannot redirect to the
  disposable `ArchiveMutation` lane.
- The transaction proved installed Runtime pre/post exact, all eleven product
  originals restored exactly, candidate product and Runtime sources unchanged,
  process absent before/after and shared lock released. It did not read archive
  contents, launch Steam or the game, invoke migration, or mutate player save
  bytes.

This evidence does not reopen the verified MoreSaves implementation or weaken
its 0.6 package gate. It makes the previously retained local player-data
exception an explicit blocker for this player's final live-root Local11
`NoNativeSave` integration: the invalid index-10 backup and resulting legacy
source disposition require the user's data decision before that exact run can
launch, unless the user separately authorizes a different final-acceptance
boundary.

## 2026-08-05 User-Directed Official-Role Correction

The user confirmed that slot 10's current archive is healthy and only its old
`-bak` member has the mismatched internal index. They explicitly replaced the
previous fail-closed whole-family policy with the native 1.00 conversion
semantics: old current, prev and bak are filename roles; each existing source
moves independently when its official destination is absent, and an existing
destination preserves both paths without blocking the remaining work.

The current 0.6 correction therefore removes archive decryption/index probing,
SHA/mtime fingerprints, complete-family preflight and changed-destination
rollback. It retains exact native `GetDataFullPath`/format/root validation,
no-overwrite behavior, a source-absent/destination-present postcondition and the
existing runtime gate that refuses to publish 12 after any move failure. File
names themselves are the resumable state, so no DTMAPI flag or journal is
introduced. `ISaveSlotsApi` and the existing product/compatibility owner
exclusion remain unchanged.

The index-10 old backup is now expected to become
`doloc-save-10.data.bak`. Native list/load/save paths do not select that backup
as the current slot, and native deletion replaces it through the existing
backup lifecycle. The earlier invalid-fixture rejection and native-valid-family
PASS remain historical evidence for the superseded implementation; they do not
validate this corrected migration. Focused managed-fixture source/unit checks
must pass before this issue advances, while a new player-process migration run
is outside the user's bounded implementation-and-pause request.

## 2026-08-05 Focused Correction Validation

- The production migrator is now 260 lines and contains no archive-content read,
  native index probe, SHA/fingerprint, complete-family plan or rollback path.
- Focused MoreSaves Unit passed the independent 18-role mapping, opaque
  mismatched index-10 backup, destination-preserve-and-continue, partial failure
  resume, reported failure after a completed rename, postcondition failure,
  native format/path/root boundaries and all existing native-six/owner-release
  cases.
- PowerShell 7 and Windows PowerShell 5.1 both passed the focused
  source/policy/compatibility gate and Catalog contract. The formal Author SDK
  produced a valid source `1.0.1` package with SHA-256
  `3021E7340C30BC71727301E61FE02D0DC7E7070782740AD7EBF6A760C853F571`;
  its entry DLL SHA-256 is
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`.
- No game, Runtime install, Steam subscription, official `MODS`, live SAVE or
  sidecar was touched, and the Runtime lock was not acquired. The previous
  player acceptance and final candidate are stale for this product source.
  Corrected player-process revalidation, full Release and final-candidate rebuild
  remain pending after the user-directed pause; ISSUE-019 is not resolved yet.

## 2026-08-05 Live Package Identity Diagnosis

- The user's next cold start rendered twelve cells but still showed the six old
  extra slots as empty. Read-only inspection found both official Local
  `MODS/DTMAPI_MoreSaves` and subscribed Workshop `3742763050` at version
  `1.0.0`, minimum Runtime `0.5.5`, with identical 17,408-byte entry DLL SHA-256
  `09FE3D9613A3C085A0CD43ACA46F348C5C251DE3BB21B3941280D97B7C1B9A3F`.
- Official state enabled Local at priority `8` and disabled Workshop at priority
  `-1`. The Runtime log selected that Local directory and recorded only the old
  `archiveFileCount 6->12` path; it contained no current role-migration result.
- Current source and the focused SDK output are `1.0.1`, minimum Runtime `0.6.0`,
  with entry DLL SHA-256
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`.
  The observed empty slots therefore came from a correctly selected stale Local
  package, not a failed execution of the corrected migrator and not a duplicate
  source-arbitration regression.
- The next bounded action is an exact, recoverable official-Local replacement
  under the shared Runtime lock, without changing official enablement or
  launching the game. The subsequent user cold start remains the first live
  `ArchiveMutation` revalidation of the corrected bytes.

## 2026-08-05 Local Manual-QA Deployment

- Clean commit `0328978c021f78c14c93f254a133f134d5c71ce5` completed a
  fresh Release-configuration build (`build.ps1 -SkipTests`) with zero errors.
  Because the installed game is now build
  `24567135`, the Advanced SDK correctly rejected it against the tracked exact
  `24456188` policy; the existing exact-reference fixture was used instead,
  without relaxing or resigning that policy.
- The real Author SDK release check passed with `389` files and SDK ZIP SHA-256
  `9041D3C00B32DF821085E831E8C7A56C8D667C25EA80103BF52432B064B3B8E3`.
  The rebuilt MoreSaves `1.0.1 / minimum 0.6.0` package reproduced SHA-256
  `3021E7340C30BC71727301E61FE02D0DC7E7070782740AD7EBF6A760C853F571`;
  its seven-file Local tree contains entry DLL SHA-256
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`
  and Advanced receipt SHA-256
  `8555314A47E40019A57A0E268D78817CE26811B61E8DE89A8A24EC5FC8422866`.
- Under the shared Runtime lock and with no game process, Runtime-only install
  published `0.6.0 / 0.6.0.0 / 0328978c021f`; all five installed DLLs matched
  the clean Release outputs. `check-dtmapi-status.ps1` and Player Doctor exited
  zero with five artifacts, zero errors and zero warnings.
- The validated Local package was staged outside official `MODS` on the same
  volume. The old Local `1.0.0` tree was moved intact to
  `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\DTMAPI\backups\moresaves-local-1.0.0-before-0328978c-20260805-184235695`,
  then the seven-file `1.0.1` tree was moved into
  `MODS\DTMAPI_MoreSaves` and rechecked. Two validation-only staging trees from
  pre-publish assertion failures were removed after their exact seven-file set
  and DLL hash were verified; neither failure reached the directory-move phase.
- Before/after fingerprints prove both existing `mod_infos.json` files, the
  complete 38-file `SAVE` tree, Workshop MoreSaves `3742763050`, every other
  official Local product and the empty legacy `<game>\Mods` tree unchanged.
  No game was launched and the Runtime lock was released. The installed bytes
  are ready for the user's cold start, but that future startup is still the
  first corrected live migration; this issue therefore remains corrected-player
  revalidation pending rather than resolved.

## 2026-08-05 Corrected Live Migration Observation

- The user's next cold process selected enabled Local
  `MODS/DTMAPI_MoreSaves` at priority `8` and ignored the disabled Workshop
  duplicate. The loaded entry DLL is the staged `1.0.1` byte identity
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`,
  not the stale `1.0.0` package from the preceding failed observation.
- Runtime log lines recorded `legacySources=16, moved=16,
  destinationsPreserved=0`, followed by `archiveFileCount 6->12`, a committed
  Mod transaction and successful Drift activation on installed build
  `24567135`. No migration failure or retry remained pending.
- A later read-only comparison against the retained exact 18-role correction
  receipt found all 18 legacy source names absent. All 16 roles that existed
  before migration now exist at their official current/`.prev0`/`.bak`
  destinations with identical length and SHA-256; the two originally absent
  roles, index-10 `prev` and index-11 `bak`, remain absent. In particular, the
  former index-10 old backup is now `doloc-save-10.data.bak` with the exact
  original backup hash.
- The same process entered and returned from native `DolocAPI.LoadGame` for
  `slot=7`; the request coordinator closed with `nativeEnter=1`,
  `nativeReturn=1`, `saveLoaded=1`, zero duplicates/timeouts and no exception.
  This directly disproves the earlier claim that the corrected package still
  could not read any extra slot, while remaining narrower than six independent
  slot-load checks.
- Codex did not launch the game, acquire the Runtime lock, or write any save,
  package, profile or official state while reviewing this evidence. The process
  was absent at inspection time, but this ordinary player run has no harness
  receipt for the final application-shutdown/fatal-window boundary. It closes
  the live stale-package/migration symptom as `mitigated`; a disposable
  `ArchiveMutation` rerun with official UI coverage and clean-exit gates remains
  the release acceptance boundary.

## 2026-08-06 Corrected Disposable Release Revalidation

- Runtime provenance `02e186cfa8eb` was installed as the exact five 0.6.0
  production assemblies. The disposable fixture contained the 16 roles that
  actually existed in the player's 6--11 official family, copied read-only to
  their matching old role names; index-10 `prev` and index-11 `bak` remained
  absent. The current official Local MoreSaves tree was projected as exactly
  seven files with entry DLL SHA-256
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`,
  Local enabled at priority `8`, and the Workshop duplicate disabled.
- `GAME-SMOKE/20260806-004228` and `GAME-SMOKE/20260806-004555` are retained
  setup failures, not product failures. The first isolated persistent root had
  no Local package; the second had the package but no isolated
  `SAVE/mod_infos.json`. In both runs Runtime therefore never selected the
  corrected Local product, all 16 old files remained unchanged, the profile was
  restored, no player archive writeback or routine byte backup occurred, and no
  game process remained. These attempts make explicit that a post-legacy
  disposable run must project both halves of the official Local source: package
  bytes and native enablement state.
- With those fixture inputs present, `GAME-SMOKE/20260806-004819` passed the
  corrected first process. Runtime selected the exact Local tree, reported
  `legacySources=16, moved=16, destinationsPreserved=0`, and only then published
  `archiveFileCount 6->12`. The captured official panel visibly contains
  populated cells #7--#12, and the official path loaded slot #7/native index 6
  through one native enter, return and `SaveLoaded` with no timeout or exception.
- The retained
  [post-first receipt](../evidence/GAME-SMOKE/20260806-004819/archive-mutation-post-first.json)
  proves all 18 old names absent, all 16 existing official destinations exact
  against the read-only player-role baseline, and the two expected destinations
  still absent. The existing bytes total `24,553,372`; every length and SHA-256
  matches, including the opaque old index-10 backup at
  `doloc-save-10.data.bak`.
- A new cold process, `GAME-SMOKE/20260806-005111`, observed zero legacy
  candidates and product counters `legacySources=0, moved=0,
  destinationsPreserved=0`. It captured the same populated twelve-cell panel,
  selected slot #12/native index 11, and completed `SaveLoaded`. The
  [post-second receipt](../evidence/GAME-SMOKE/20260806-005111/archive-mutation-post-second.json)
  binds the first receipt hash and proves all 18 role states unchanged. The
  [official-source receipt](../evidence/GAME-SMOKE/20260806-004819/official-local-source-fixture.json)
  separately proves the seven-file Local package and isolated Local/Workshop
  enablement projection.
- Both passing processes satisfied disposable `ArchiveMutation`, pre-Runtime
  `LocalSave.cloudDirPath` isolation, official UI screenshot, save-load
  coordinator, QA lifecycle/cleanup, owner cleanup, fatal-window and normal
  process-exit gates. They created no player save backup, performed no player
  archive writeback, and restored the live official profile byte-for-byte. The
  disposable fixture was moved to the Recycle Bin after exact marker/path and
  no-reparse checks; Player Doctor finished at five Runtime artifacts, zero
  errors and zero warnings, no process remained, and the shared lock was
  released.

The user-directed lightweight official-role migration and its required
player-process boundary are therefore verified. This does not claim the later
full 0.6 Release/final-candidate or Steam publication gates, and it does not
expand the fixed-12 product into arbitrary paging, naming or a general save
migration platform.
