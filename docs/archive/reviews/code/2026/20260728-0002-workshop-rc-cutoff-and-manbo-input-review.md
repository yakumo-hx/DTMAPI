# Workshop RC Cutoff And Retained Manbo Input Review

**Date:** 2026-07-28
**Status:** recorded — RC discovery/input cutoff frozen; final-candidate
runtime acceptance remains open
**Owning Update:** `20260727-0001-dtmapi-055-prerelease-route.md`
**Scope:** public Workshop metadata, the current read-only subscription cache,
retained DTMAPI consumers, the Runtime rollback input and the Manbo acceptance
input only

## Decision

The 0.5.5 RC discovery cutoff contains the same fifteen exact retained
`DTMAPI.Abstractions` consumers: eleven first-party Workshop products and four
external consumer DLLs. No newly obtainable real consumer was found.

The existing retained ABI gate is extended in place. It now verifies the exact
four external DLL hashes and assembly-reference versions and resolves all 23
of their `DTMAPI.Abstractions` MemberRefs against the exact candidate. This is
schema 3 of the existing retained-runtime report, not a second ABI system or a
new receipt family.

The retained Manbo subscription tree is frozen as the later step-6 input.
The current player `MODS` tree contains a different stale local Manbo copy, so
the final candidate run must first isolate that copy, prove one Workshop
source selection and load the exact retained DLL. A green run against the
local copy would not be acceptance evidence.

No game was launched and no subscription, player Mod, save or game file was
changed for this review.

## Public Metadata Cutoff

The official Steam public metadata endpoint and fully paged Workshop browse
results were captured at `2026-07-27T19:18:18Z`–`19:18:30Z`:

| Query | Current results | Pages through empty | Use |
| --- | ---: | ---: | --- |
| `DTMAPI` | 19 | 2 | authoritative discovery query |
| `DTM API` | 11 | 2 | discovery cross-check |
| `Doloc Town Modding API` | 31 | 3 | discovery-only; current relevance results contain unrelated Mods |
| `DolocTown SMAPI` | 11 | 2 | discovery-only; current relevance results contain unrelated Mods |

The broad query union is 37. Treating that union as 37 DTMAPI products would
be false. The authoritative snapshot therefore contains the exact `DTMAPI`
result plus the three reviewed compatibility identities `3726044511`,
`3742618545` and `3742771572`: 22 rows.

The first independent step-4 review found that the initial capture stopped
at page 1. The corrected schema-2 snapshot records every query's complete ID
set, pages fetched and empty-page terminal reason. Its Catalog gate freezes
all four ID-set digests and rejects truncated or internally inconsistent
captures. Page 2 of `Doloc Town Modding API` added only `3749143385`; the
existing 2026-07-13 review already proves that retained subscription is
official JSON content only, with no DTMAPI manifest, Entry DLL or assembly.
It therefore does not add a consumer or change the 22-row authority set.

Changes from the 2026-07-13 public snapshot:

- `3772057085` (`DolocTownHungerSystem`) is a search false positive. Its public
  description says it is a direct BepInEx plugin and that the author did not
  use DTMAPI.
- retired legacy Runtime `3726044511` now returns Steam result `9`; its row
  retains the last-known 2026-07-13 descriptive fields and explicitly marks
  that boundary.
- `3759797170` now reports public file size `696,149`; its exact retained
  DTMAPI installer DLL is unchanged.
- `3743790125` (`No Weeds & Trash in Farm`) still claims DTMAPI as a
  prerequisite but is absent from the current subscription cache. An isolated
  anonymous SteamCMD fetch did not provide its package. It remains
  `ExternalPrerequisiteClaimantPendingPackageInspection`; no ABI is invented.
- `3754866797` (`Price Helper`) remains a documented search false positive.

The normalized 22-row metadata digest is
`e74aa6449e00d6ddc83004078a74a03a7e3d25820c9f8a8ccf7e13e535d28ce0`.

## Read-Only Subscription Cutoff

`D:\Steam\steamapps\workshop\content\2285550` contained 44 item directories:

```text
3702492292,3705665433,3707303389,3707695848,3709176726,3711264686,
3711608705,3715788753,3716009587,3716012813,3716431651,3722791728,
3723322812,3725278565,3726536107,3726854082,3727940804,3742618545,
3742714442,3742717440,3742763050,3742763309,3742763540,3742763706,
3742763843,3742765514,3742771572,3743016467,3743621104,3743644065,
3743799721,3744059735,3744793227,3745094671,3746319981,3746326554,
3749143385,3750891996,3754869009,3755367789,3756810338,3759066788,
3759797170,3763389470
```

Compared with the 40-directory 2026-07-13 inventory, the four additions are
`3756810338`, `3759066788`, `3759797170` and `3763389470`. Metadata scanning
found a `DTMAPI.Abstractions` reference only in `3759797170`; its exact DLL is
included below. The other three do not add DTMAPI consumer bytes.

The negative scan enumerated all `*.dll` files below the 44 read-only
subscription directories, then searched their binary metadata for the exact
`DTMAPI.Abstractions` assembly token before the exact ABI harness inspected
the retained consumers. It found 36 DLLs, of which 20 contain that token.
Five are the Runtime framework assemblies in Workshop `3743016467`; the
remaining fifteen are exactly the eleven first-party and four external
consumers below. The four newly present directories add only the one already
classified external consumer in `3759797170`.

## Retained External ABI Consumers

| Workshop | DLL SHA-256 | Abstractions ref | Resolved MemberRefs |
| --- | --- | --- | ---: |
| `3743621104` / `DolocStorageExpansionMod.dll` | `45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366` | `0.5.1.0` | 3 / 3 |
| `3743644065` / `DolocStoreCapacityMod.dll` | `BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF` | `0.5.1.0` | 4 / 4 |
| `3754869009` / `DolocTownQoL.dll` | `FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10` | `0.5.2.0` | 13 / 13 |
| `3759797170` / `Mxx_DolocTownMod_Installer.dll` | `F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D` | `0.5.2.0` | 3 / 3 |

The exact retained report is
`tools/release/baselines/retained-runtime-052-public-api-audit-20260728.json`.
It also proves zero deletion from the retained `0.5.2` public surface and the
eleven existing first-party consumer identities. The default complete suite
remains private-artifact independent; this exact-binary consistency lane runs
only when the existing three explicit retained-artifact environment variables
are all supplied.

## Exact Retained Manbo Input

Workshop `3746319981` is frozen as:

- UniqueID `Yuuka.DTMAPI.ManboCardboardAudio`;
- version `0.1.0-dtmapi`;
- minimum DTMAPI `0.5.2-alpha`;
- 7 files, 229,384 bytes;
- tree SHA-256
  `23a3209e75788b68041f4e1eecfe81550f87d6579893272af67711b2c0bfc40e`;
- entry DLL 9,216 bytes, SHA-256
  `AB85C0BAB39702E7FE2689BB4D528B6EF3726F0BB272F6F172CFFE8A57A17EC4`;
- WAV 101,316 bytes, SHA-256
  `560981404121EDE8F3BA4531FFF17E7F8EF56C68A2F12284792031EB97314C8C`.

The exact seven-file tree is:

| Relative path | Bytes | SHA-256 |
| --- | ---: | --- |
| `Content/DTMAPI/assets/manbo.wav` | 101,316 | `560981404121EDE8F3BA4531FFF17E7F8EF56C68A2F12284792031EB97314C8C` |
| `Content/DTMAPI/dtmapi-package.json` | 184 | `13E13977048432A1C596FEB2A0D4EA689CF2C8F1169D6BADFB507C5537A6E3CD` |
| `Content/DTMAPI/manifest.json` | 690 | `2C3CD1F750229897C5507DC700EA8EFD50C51FAEC2FFF681D0005DED2740684D` |
| `Content/DTMAPI/Yuuka.DTMAPI.ManboCardboardAudio.dll` | 9,216 | `AB85C0BAB39702E7FE2689BB4D528B6EF3726F0BB272F6F172CFFE8A57A17EC4` |
| `icon.png` | 58,411 | `0411C0312E1176F3E391B03C394905247789BD94FFB1B3F3C2925A4FC626E622` |
| `info.json` | 1,156 | `29194BE6E7FE1FF9C796BE831134564D237B2CAFEDB3915CCF2F183EEF35888E` |
| `preview.png` | 58,411 | `0411C0312E1176F3E391B03C394905247789BD94FFB1B3F3C2925A4FC626E622` |

The current persistent
`MODS\Yuuka_DTMAPI_ManboCardboardAudio` directory is not this tree: it has
8 files, tree SHA-256
`bf42f6e788c15972d42fe01b3931da5f159a26019c3aeaec19c2d06d763e6ee0`,
and its 8,192-byte DLL has SHA-256
`EE149D16F5E623E88F91A910276EC9BC4BC6109C811D8E93E794E33A2A161D21`.
This is a test-asset/source-selection blocker, not permission to delete it.
Step 6 must acquire the runtime lock, preserve that non-save asset, stage the
exact subscription tree through the existing runner ownership, prove one
Workshop load-source record and restore the prior local asset after archive
proof and process exit.

The acceptance is third-save `NoNativeSave`. It must prove:

1. exact subscription tree and entry DLL hashes before launch;
2. the local duplicate cannot win selection;
3. Runtime accepts minimum `0.5.2-alpha`;
4. `IAudioReplacementApi`, Entry and WAV registration succeed;
5. no missing-method/type/file/provider/manifest/Loader/Fatal error;
6. title and process-exit cleanup succeed;
7. selected current/prev/bak archives remain unchanged before restoring any
   non-save test asset.

No cardboard-box sound interaction is required.

## Release And Rollback Freeze

- R0: Runtime `0.5.5`.
- R1: AutoFishing `1.0.0`.
- R2: MoreEquipmentSlots `1.0.0`.
- R3: ActionSpeed, OneActionComplete, FishBreedingAssistant,
  AnimalHusbandryProgress, MoreSaves, ChestLocatorEnhancer, Zoom and
  DebugConsole/YConsole, each as an independent `1.0.0` package.
- Manbo stays on its retained `0.1.0-dtmapi` package and is not projected into
  the Advanced wave.
- Mine, Oil, AnimalPack, G7, ShellCrab and BGM remain outside 0.5.5.
- Runtime rollback input originated from Workshop `3743016467`,
  `0.5.2-alpha`, 46 files, 4,319,645 bytes, tree SHA-256
  `ac67aef01109c9558dde5c780c1c64127c3c8e25bf9f70b1eb30dbc9a1408d25`.
  It is now retained outside the source/distribution tree under the Catalog
  location contract
  `SiblingOfRepository/DTMAPI-retained-artifacts/runtime/DTMAPI-0.5.2-alpha-workshop-3743016467.zip`
  (or the explicit `DTMAPI_RUNTIME_ROLLBACK_ARCHIVE` override). The
  read-only private/non-distribution archive is 1,934,142 bytes with SHA-256
  `095533cb256e19381d1c51018258b239d53aa01accafa575bd24bdb93e9c4ac6`;
  its `payload/` contains the exact 46 files, and internal `SHA256SUMS` plus
  `PACKAGE-INFO.txt` make the bytes independently reconstructable and
  verifiable through
  `tools/scripts/freeze-runtime-rollback-archive.ps1 -VerifyOnly`.
- Creation validates the temporary ZIP's fixed Catalog bytes and SHA-256
  before moving it to the authoritative location. Publication/read-only
  failure removes the just-published target; a payload-equivalent source
  with changed timestamps therefore fails without leaving a false authority.
- Retain that archive through the 0.5.5 rollback window. It is not project
  source, an upload package or ordinary distributable material; removal
  requires an explicit post-release retention decision.
- Candidate source is the clean committed descendant constructed in step 5
  from the commit containing this cutoff. No uncommitted workspace,
  rebuilt retained consumer or mutable subscription byte may replace it.

## Remaining Boundary

This review freezes inputs only. It does not claim that Runtime `0.5.5`,
Manbo, the external consumers or any release package passed Unity Mono. Step 5
must construct the exact candidate; step 6 owns the Manbo game run and the
other focused release gates.
