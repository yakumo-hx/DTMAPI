# MoreEquipmentSlots 1.00 Overlap Decision Gate

Date: 2026-08-04
Status: `recorded / Branch B / local scope decision required / implementation stopped`
Audited HEAD: `05874fb3703581bd4df55ac5f88132d23a01591e`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

Prior authorities:

- [`20260801-0003`](20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md)
- [`20260730-0016`](20260730-0016-moreequipment-production-transaction-three-pass-reaudit.md)
- [`20260729-0004`](20260729-0004-moreequipment-legacy-migration-fix-audit-and-test-plan.md)
- [MoreEquipmentSlots Hook map](../../../hook-map/focused/MoreEquipmentSlots.md)

## Scope

The 0.6 roadmap requires one read-only decision gate before changing the
retained MoreEquipmentSlots path. This review binds and compares:

- the exact retained Workshop `0.3.1-dtmapi` package;
- the frozen `IEquipmentSlotsApi` Compatibility Host;
- the deferred MoreEquipmentSlots `1.0.0` ProductNative source;
- the old `23762374_public_C416D4` and current
  `24456188_test_E861E0` native bodies.

The only question is whether the current-game overlap is limited to
`BodyController.OnAttacked`/damage/shield, which would select Branch A, or
also reaches legacy save/recovery/migration authority, which selects Branch
B. This pass does not modify Runtime or Product code, build a replacement
package, install DTMAPI, start Doloc Town, invoke native save, or authorize
MoreEquipmentSlots `1.0.0` publication.

## Bound Inputs

The native sources are local-only reverse evidence and remain outside the
repository distribution boundary:

| Build | `Assembly-CSharp.dll` length | SHA-256 |
| --- | ---: | --- |
| `23762374_public_C416D4` | 5,993,984 | `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404` |
| `24456188_test_E861E0` | 6,384,128 | `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923` |

Catalog and the sibling retained-artifact authority bind the old public
package to Workshop `3744059735`, version `0.3.1-dtmapi`, nine files,
539,565 bytes and tree SHA-256
`e0854cee94969d98b916a3f6085fd03773c67bcd35c7bc83dc2894f8156e0ca6`.
Its entry DLL is exactly 9,728 bytes with SHA-256
`092807CC5C5DB359B40D5325EDD6EBC6860238E51F7F65014F5B39FBC0CF71ED`.

The retained-ABI harness resolves all `29/29` Abstractions MemberRefs from
that DLL against the candidate `0.6.0` Abstractions assembly. Its exact frozen
EquipmentSlots set is the options constructor and setters, register result
accessors, and `IEquipmentSlotsApi.RegisterSlots`. The old DLL therefore
selects the DTMAPI Host; it does not own the broken native implementation.

## Native Responsibility Comparison

### Attack owner changed

The previously recorded native break is confirmed:

- old: `BodyController.OnAttacked(float,bool,Vector2,out bool)`;
- current: `BodyController.OnAttacked(float,bool,Vector2,AttackProperties,out bool)`.

The current body adds attackability, `AttackProperties`/Thunder, drone death
escape, post-hit invincibility and the other tail invariants already owned by
Review `20260801-0003`. Both the Compatibility Host and deferred Product still
resolve the old four-parameter target. An ignored fifth parameter remains an
invalid repair.

### Backpack and mail owners did not change

The exact reviewed `DolocAPI` bodies are text-identical between the two bound
builds for:

- `TryPlaceInBackpack(string,int,bool)`;
- `CountItem(string,bool)`;
- `CostItem(string,int,bool)`;
- `CanPlaceItem(string,int)`;
- `SendItemAsEmail(string,int,string,string,string,string)`.

The complete decompiled `EmailManager`, `EmailAttachReward` and `RewardItem`
files are also byte-identical between the two baselines. No current-game mail
or backpack body change was found by this gate. This negative evidence does
not close the deferred Product transaction defects from Review `0016`; it only
shows that a new native mail-body change is not what selects Branch B.

### Native save and recovery authority changed

The old `LocalSave` owns one current archive, one `-prev` file and one `-bak`
file. Its `SaveGame` writes a temporary file and uses one `File.Replace` into
the current file with the single previous file as backup.

The current `LocalSave` changes that authority:

- the archive family is explicitly under `cloudDirPath` and includes a
  `convert_data_100` migration;
- the previous generation is now `<current>.prev0`, with additional
  `<current>.prevN` files controlled by `backupCount`;
- the backup is now `<current>.bak`;
- `SaveGame` calls `ScrollBackups` before replacing current into `.prev0`.

The physical current-file directory remains `persistentDataPath/SAVE`; a
directory relocation is not the finding. The filename family, backup count,
rotation order and failure windows are the changed native responsibility.

The live Compatibility Host calls private `LocalSave.GetDataFullPath` through
reflection, then calls `EquipmentSlotNativeCommitFingerprint.Compute` before
and after save and during journal/candidate recovery. The optional Host
project links both
`EquipmentSlotsCompatibilityTransactions.cs` and the Product source file
`EquipmentSlotTransactionJournal.cs`. That shared fingerprint still derives
only:

```text
<name>-prev<extension>
<name>-bak<extension>
```

For a current archive such as `doloc-archive-2.data`, it therefore observes
`doloc-archive-2-prev.data` and `doloc-archive-2-bak.data`, while the current
native owner writes `doloc-archive-2.data.prev0...N` and
`doloc-archive-2.data.bak`. The Host can still notice many successful saves
because the current file itself changes, but it no longer observes the native
archive family it claims to fingerprint. It cannot use its old pre/post image
as exact proof across the new rotation and interruption windows.

This is a real legacy save/recovery overlap, not a reproduced claim that every
ordinary save loses an item. The absence of a player loss reproduction cannot
turn a changed native commit authority into an attack-only seam.

## Decision

The gate selects **Branch B**.

Branch A is rejected because the current game changed both the attack owner
and the native archive family consumed by legacy Host save/recovery logic.
Consequently the following work is stopped pending an explicit scope decision:

- no attack-only Compatibility Host implementation;
- no replacement Runtime/Product freeze or package;
- no old-package game acceptance that omits the required disposable
  save/no-save/failure matrix;
- no automatic admission or publication of MoreEquipmentSlots `1.0.0`;
- no generic protected-storage API or SharedNative generalization.

If the Branch B expansion is authorized, the owning Update requires two
separate exits in one bounded lifecycle:

1. retained `0.3.1-dtmapi`: frozen ABI registration, old data preservation and
   migration, current attack behavior, normal save, no-save rollback and
   interrupted native-save recovery;
2. deferred `1.0.0`: its ProductNative owner, Product-v3 migration and all five
   unresolved transaction findings from Review `0016`.

The Compatibility Host must remain an old-ABI executor. The Product gameplay
state machine remains ProductNative. Publication remains a separate authority
even if source corrections are authorized.

This is a local MoreEquipmentSlots blocker, not a global blocker for the
remaining 0.6 source reviews and independent product corrections. Those can
continue while the user decides the Branch B scope. Formal 11-Mod integration
cannot mark the retained MoreEquipmentSlots path compatible until this local
blocker is resolved or a separate bounded release disposition replaces it.

## Rejected Hypotheses

- **Only the fifth `OnAttacked` parameter changed.** Rejected by the exact
  `LocalSave` filename/rotation diff and the Host's active fingerprint calls.
- **The old DLL must be rebuilt.** Rejected by its exact frozen API metadata;
  the physical native owner is the optional Host.
- **Mail changed and forces the expansion.** Rejected for this baseline pair;
  the relevant API bodies and mail model files are unchanged.
- **Hashing current alone proves the same native commit contract.** Rejected;
  the Host explicitly claims an archive-family fingerprint and recovery
  decisions across interruption windows, while omitting every current prev
  generation and the current backup filename.
- **The five Product 1.0 P1 findings can be treated as closed because 1.0 is
  deferred.** Rejected; deferral prevents publication but is not a technical
  closure if Branch B is later authorized.

## Validation

Read-only/focused checks against the audited tree passed:

```text
test-retained-release-abi.ps1 -Configuration Release -NoBuild = PASS
  resolution = SiblingRetainedArtifactAuthority
  MoreEquipmentSlots = 29/29 Abstractions MemberRefs resolved
  all public products = 463/463
  candidate public API deletions = 0
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
targeted old/current native method-body and whole-file comparisons = completed
```

No Runtime install, Workshop/user directory write, game process, native save,
or Runtime lock was used. A game run would not answer the scope question more
safely than the exact native-owner evidence above.

## Rollback

This Review and the Branch B projection in the owning Update are documentation
authority only. Reverting them does not restore Branch A evidence and must not
be used to justify an attack-only implementation. A later user decision should
not rewrite the bound native comparison.

## Resolution Owner

Any later Branch B scope decision, implementation or release disposition,
changed-file inventory and validation result belongs only to the
[DTMAPI 0.6.0 owning Update](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md).
This Review remains the read-only root-cause and native-overlap record; a future
resolution should add only a concise link back to the owning Update.
