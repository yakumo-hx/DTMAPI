# 20260830-0002: MoreEquipmentSlots 1.0.1 Workshop release closeout

## Metadata

- Update ID: `20260830-0002`
- Date: `2026-08-30`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `mitigated`
- Source: 用户确认已上传并订阅 MoreEquipmentSlots `1.0.1`，要求只读检查订阅目录，同时更新并打印简体中文、繁体中文和英文描述，回填必要发布文档后提交。
- Review: `docs/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md`
- Implementation Update: `docs/updates/2026/20260826-0001-moreequipment-newgame-slot-reset.md`

## Scope

This Update closes only the publication-observation and description-copy
boundary for the already implemented `1.0.1` NewGame slot-reuse correction.
It does not upload, edit or repair any Steam-managed content, does not stage a
new official upload tree, and grants no future Workshop mutation authority.
The Product has no installer entry point, so the Runtime Workshop installer
matrix is not applicable; the relevant player-facing audit is the native Steam
manifest plus the complete subscribed Product tree.

The three localized descriptions are a repository metadata successor created
after the published bytes were observed. They are retained in
`official-info.json` and the Author/publish-text projections for manual use on
the Workshop page. They do not retroactively change the subscribed package's
existing `info.json`, and this task does not claim those new description bytes
were uploaded.

## Changed Files

- `products/first-party/MoreEquipmentSlots/official-info.json` and
  `dtmapi.author.json`: replace the short placeholder copy with the requested
  Simplified Chinese description and section-for-section Traditional Chinese
  and English translations, including the `update0830` note.
- `tools/release/dtmapi-mod-publish-zh.json`: mirror the new Simplified Chinese
  game/Steam copy and advance its material date.
- `tools/release/dtmapi-product-catalog.json`: replace the historical `1.0.0`
  current-public projection with the exact observed `1.0.1` Steam artifact
  while retaining `ActiveNoUploadAuthorization`.
- `tools/release/current-subscription-manifest.json`: capture the stable native
  Steam ACF snapshot and new MoreEquipmentSlots manifest membership.
- `tools/scripts/check-product-catalog.ps1`: advance the current-published
  MoreEquipmentSlots version, build/evidence route and frozen current-artifact
  digest canaries.
- `products/first-party/MoreEquipmentSlots/README.md`: route current release
  facts to this closeout and summarize the `1.0.1` proven NewGame ownership
  boundary.
- The implementation Update, this Update and the August monthly ledger link
  the earlier implementation to its later user-performed publication.

## Validation

- Two reads of Steam's native `appworkshop_2285550.acf` were stable at 18,580
  bytes, last write `2026-08-30T10:51:39.0095695Z`, SHA-256
  `F757F6AFB21C42EEA0909EA17F86A7C8B6B68648864396888E09F4D29FBE6F66`.
  The manifest reports 53 installed/detail items, 116,721,449 total bytes,
  `NeedsUpdate=0`, `NeedsDownload=0`, and identical installed/latest
  MoreEquipmentSlots manifest `3792342317410441560`.
- Workshop item `3744059735` reports 210,913 bytes and update epoch
  `1788087016` (`2026-08-30T10:50:16Z`). Its complete subscription directory
  has exactly eight files, 210,913 bytes and normalized published-tree SHA-256
  `31042E6E2017255185C974A94CFD2749C84BB4BC9E68E12963B07AFB1F9D532D`.
- The subscription tree is byte-identical by relative path, length and SHA-256
  to the existing local official upload directory. Its sole entry DLL is
  206,336 bytes with SHA-256
  `3DD62441E3352671AA0ECDCDF5D93FF18BF74CC4109F8B7A1A738287B2F88749`;
  the Advanced reference receipt is
  `9B2924B668B6F8FF6F17102CE1FF5E46A2A3E52A1724CDBD649AE7A534BBFE2D`.
- Package identity remains `DTMAPI.MoreEquipmentSlotsMod`, version `1.0.1`,
  with minimum DTMAPI `0.6.0` and policy
  `doloctown-24456188-moreequipmentslots-v1`. No reparse point, alternate data
  stream, unexpected executable, duplicate entry or installer surface is
  present.
- A focused Author SDK validate/build/pack run against the existing exact
  `24456188` reference fixture passed. It regenerated the same 206,336-byte
  DLL and Advanced receipt, projected all three new descriptions into
  `info.json`, and produced a seven-file local metadata successor ZIP with
  SHA-256
  `BEDEE40246E24378243FC2D6090DE20BD4C3F171146E7D46299571348BFF07FF`.
  That successor was retained only under the repository `temp` validation
  root and was neither synchronized nor uploaded.
- The current game app manifest reports build `24966367`; no game launch was
  performed. Catalog `gameBuildId=24788406` intentionally remains the exact
  live build on which this unchanged `1.0.1` binary passed the implementation
  Update's save-slot-12 acceptance, rather than mislabeling a read-only
  subscription observation as new runtime validation.
- Focused Product Catalog checks passed under PowerShell 7 and Windows
  PowerShell 5.1 (`27` products, `11` public products, `22` Workshop items and
  `48` API rows). Author/package metadata validation passed, document
  governance passed `7,260` checks, and `git diff --check` passed. No save,
  sidecar, game directory, official upload directory or Steam subscription
  byte was changed during validation.

## Evidence

- Observed subscription root:
  `D:/Steam/steamapps/workshop/content/2285550/3744059735`.
- Native Steam manifest:
  `D:/Steam/steamapps/workshop/appworkshop_2285550.acf`.
- Local official upload comparison root:
  `%LocalAppDataLow%/RedSawGames/DolocTown/MODS/DTMAPI_MoreEquipmentSlots`.
- The previously built seven-file ZIP remains SHA-256
  `4F0B078D8E7571D662CA7C5BEB8440130894D76E6EBDD4751F2462E63B57E891`;
  Steam's eighth file is the preserved 33-byte `workshop.json`, SHA-256
  `EF4FD9462B44C8F84C61940052FBFC28143B5EB4D2E94A88979499C5D386543D`.
- Behavioral evidence remains owned by the implementation Update's slot-12
  NewGame first-save and independent cold-load runs. Exact publication parity
  reuses those already accepted bytes and does not manufacture another game
  smoke result.

## Rollback Notes

- The observed Steam `1.0.1` manifest and hashes are external facts and must
  not be rolled back in repository metadata while Steam still delivers them.
- The new description copy may be reverted independently if the user chooses
  different Workshop wording. Do not edit the subscription directory; any
  future content upload requires a new bounded authorization Update.

## Follow-Up

- The issue remains `mitigated` until later player evidence justifies a state
  change; publication parity alone does not promote gameplay acceptance.
- Embedded native-save storage and the associated recovery-capability tradeoff
  remain a separate player-vote/design decision.
