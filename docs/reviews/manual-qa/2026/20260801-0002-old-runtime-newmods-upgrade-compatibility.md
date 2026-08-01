# 旧 Runtime、新 Mod 与旧更多装备栏升级兼容手测

- Review ID: `20260801-0002`
- Date: `2026-08-01`
- Status: `recorded/passed-with-invalid-first-run-excluded`
- Scope: DTMAPI `0.5.2-alpha` negative version gate, Workshop-only source selection, Runtime-only `0.5.5` upgrade, legacy MoreEquipmentSlots `0.3.1-dtmapi`, local deployment restoration
- Source: user manual QA and the read-only post-test deployment/log audit
- Owning Update: [20260801-0002 0.5.5 与功能 Mod 上传目录发布收口](../../../updates/2026/20260801-0002-workshop-upload-release-closeout.md)

This Review preserves the user's test sequence and separates manual observations,
runtime-log facts and the later restoration audit. It does not authorize new
package bytes or replace the owning release Update.

## 1. Switch to old DTMAPI with the new Mod candidates

### Feedback

The user asked whether the accepted local state could be switched and restored,
then authorized a negative compatibility run with old DTMAPI plus the current new
Mod packages.

### Deployment facts and analysis

The staging receipt at
`D:/Steam/steamapps/common/Doloc Town/DTMAPI/backups/workshop-subscriptions/before-old-runtime-newmods-20260801-162820/stage-receipt.json`
binds the test to DTMAPI `0.5.2-alpha` and product candidate source commit
`d389da0fe89b`. Nine Advanced packages declare `MinimumDTMApiVersion=0.5.5`;
ManboCardboardAudio declares `0.5.2-alpha`; the retained Workshop
MoreEquipmentSlots package remains `0.3.1-dtmapi`.

This is a negative version-gate test, not a functional acceptance run for the
nine new Advanced products. Its expected result is that old Runtime refuses the
nine newer products without crashing, while the two legacy-compatible consumers
can enter.

## 2. MoreEquipmentSlots was not recognized in the first run

### Feedback

The user observed that MoreEquipmentSlots appeared enabled but was not recognized
by DTMAPI `0.5.2-alpha`.

### Runtime evidence and analysis

`latest-20260801-105221337.log` covers `18:47:30–18:52:21`. It reports 23
OfficialLocal folders and duplicate `UniqueID` rows in which old Runtime selected
the disabled OfficialLocal upload copy and ignored the enabled Workshop copy. For
MoreEquipmentSlots it then skipped `Local.DTMAPI_MoreEquipmentSlots` because that
local identity had no enabled official state. The same shadowing affected the
other packages that also existed in the upload root.

This first run is excluded from compatibility acceptance because it did not
represent a player with only subscribed Workshop packages. It reproduces the old
source-selection behavior already corrected by
[Update 20260628-0001](../../../updates/2026/20260628-0001-enabled-duplicate-mod-source.md);
it is not a new `0.5.5` defect and does not show a MoreEquipmentSlots version
failure.

## 3. Isolate OfficialLocal and repeat the old-Runtime test

### Feedback

The user requested isolation of the local upload root, restarted the game and
confirmed that the legacy MoreEquipmentSlots package now started.

### Runtime evidence and analysis

The isolation receipt at
`C:/Users/Administrator/AppData/LocalLow/RedSawGames/DolocTown/.dtmapi-old-runtime-newmods-local-upload-20260801-185329/isolation-receipt.json`
records that 23 folders and 482 files were moved out of the live `MODS` view
without deletion. In the valid rerun, `latest-20260801-105531099.log` reports:

- zero live OfficialLocal folders and 15 DTMAPI-capable Workshop folders;
- completed Mod Entry for MoreEquipmentSlots and ManboCardboardAudio;
- nine unique products rejected with the precise message that they require
  `DTMAPI >= 0.5.5` while the current Runtime is `0.5.2-alpha`;
- 36 Warning rows, consisting solely of those nine expected version messages
  repeated by four discovery passes;
- zero Error/Fatal rows.

The log proves Manbo's entry and audio registration, not a player-observed audio
replacement in this short run. The accepted scope is therefore stable rejection
of the nine new products plus successful entry of the two compatible consumers;
only MoreEquipmentSlots startup was explicitly confirmed by the user here.

## 4. Upgrade only DTMAPI and retain MoreEquipmentSlots 0.3.1

### Feedback

The user asked to upgrade only DTMAPI to `0.5.5`, leave MoreEquipmentSlots on
`0.3.1-dtmapi`, and test the ordinary player upgrade path. The user reported no
problem after loading and using that combination.

### Runtime evidence and analysis

The upgrade receipt at
`D:/Steam/steamapps/common/Doloc Town/DTMAPI/backups/workshop-subscriptions/before-runtime-055-upgrade-20260801-185829/upgrade-receipt.json`
records `0.5.2-alpha -> 0.5.5`. The local upload root remained isolated, so the
run continued to use Workshop sources only.

`latest.log` covers `19:02:37–19:03:28` and records all eleven enabled Mod
entries with zero Warning and zero Error/Fatal rows. The legacy package is
identified explicitly as `Third-party native compatibility CodeMod`; its
Compatibility equipment-slot Hooks install, one stored item is loaded and
applied, and its UI interaction completes. A normal native SaveSaving/SaveSaved
transaction then commits the user's equipment move. This corroborates, but does
not replace, the user's manual verdict.

Accepted result: upgrading Runtime alone from `0.5.2-alpha` to `0.5.5` while
retaining Workshop MoreEquipmentSlots `0.3.1-dtmapi` works in this tested player
path. This does not test or publish MoreEquipmentSlots ProductNative `1.0.0`.

## 5. Restore the local release and subscription state

### Feedback

After the upgrade test passed, the user requested restoration of the local state
and then requested an independent check that restoration was complete.

### Independent audit and correction

The first audit found one real residue in the official upload tree:
`MODS/DTMAPI/info.json` was 9,192 rather than 9,102 bytes because an empty
`localized_name` object had been added during the first pre-isolation scan. The
log does not prove which component wrote it. Product behavior was unaffected,
but the byte change violated the frozen upload authority. The unexpected variant
is retained at
`temp/20260801-old-runtime-newmods/restore-audit/unexpected-runtime-info-with-empty-localized-name.json`;
the authorized candidate byte was restored under the already-authorized rollback.

After that correction:

- installed Runtime is exact `0.5.5` from `d389da0fe89b`; all five production
  assemblies and Compatibility Host pass Player Doctor with 0 errors and 0
  warnings;
- the complete subscription root matches the retained pre-test SHA-256 manifest:
  44 top-level directories, 4,897 files, 78,638,015 bytes, no reparse or staging
  entry;
- the complete upload root matches its authorized composite authority: 23
  top-level directories, 482 files, 75,731,453 bytes, consisting of 11 frozen
  candidate folders and 12 retained original folders, with no reparse or staging
  entry;
- both active official configuration views contain exactly the intended 12
  enabled Workshop identities at priorities `0–11`, with zero enabled Local
  identity;
- game process is absent and the shared Runtime lock is released after the final
  audit.

Machine evidence is retained at
`temp/20260801-old-runtime-newmods/restore-audit/release-state-audit.json` and
`temp/20260801-old-runtime-newmods/restore-audit/complete-restoration-audit.json`.
The corrected 90-byte difference explains why the isolation receipt's transient
tree byte count is 90 bytes larger than the final authorized upload tree.

### Restore boundary

The exact-restore claim covers deployment, Workshop subscriptions, upload trees,
Runtime and official Mod enablement. It deliberately does not overwrite a
successfully committed player save or gameplay sidecar: the final manual run
performed a native save after moving an equipment item, and the Runtime log shows
that transaction reaching SaveSaved. Replacing those bytes with an earlier test
snapshot would contradict the project's native save-commit rule.

## Verdict

- The first old-Runtime run is invalid evidence because disabled OfficialLocal
  duplicates shadowed enabled Workshop packages.
- The isolated old-Runtime rerun passes its bounded negative compatibility goal:
  compatible legacy entries load, and every `0.5.5`-required product is rejected
  clearly without Error/Fatal output.
- The Runtime-only upgrade to `0.5.5` with legacy MoreEquipmentSlots
  `0.3.1-dtmapi` passes the user's manual test and the matching log audit.
- Local deployment restoration passes after correcting one 90-byte upload
  metadata residue; no Runtime or Mod source change was required.

## Post-publication resolution

A later read-only comparison of the actual upload and Steam subscription bytes
closed the writer question without rewriting the earlier audit observation.
Native `DolocTown.Config.ModManager` calls `ReloadMods()` while constructing the
official Mod manager, sends local sources through `TryMigrateData()`, and appends
the empty three-language `localized_name` object through
`EnsureLocalizedManifestField()` before writing `info.json`. Steam then
published that already-normalized local file.

The exact current Steam manifest, delivered/player tree hashes and source
generator reconciliation are owned by
[Update 20260801-0003](../../../updates/2026/20260801-0003-runtime-published-metadata-authority.md).
The original pre-upload candidate and its manual acceptance remain historical
evidence rather than being retroactively relabeled as the normalized bytes.
