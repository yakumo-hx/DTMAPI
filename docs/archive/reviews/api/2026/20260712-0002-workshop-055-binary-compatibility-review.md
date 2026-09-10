# 20260712-0002 Workshop 0.5.5 Binary Compatibility Review

Status: recorded / release blocker open
Date: 2026-07-12
Domain: DTMAPI 0.5.5 manifest, provider, assembly, and public-API compatibility for already subscribed Code Mods
Related decision review: `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
Related Update: `docs/updates/2026/20260712-0009-major-update-product-version-decision-docket.md`

2026-07-13 deployment refinement: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` keeps the old-DLL gates here but treats automatically updated Workshop Mod plus stale manually installed Runtime as the primary player skew. Old first-party DLLs are regression samples, not permanent implementation owners. Release gates now also require pre-assembly-load rejection, three-way package/installed/minimum version reporting, and detection of stale OfficialLocal copies shadowing current Workshop products.

2026-07-13 rollout decision: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md` selects a Runtime-first Canary sequence. Publish/stabilize the 0.5.5 installer and diagnostics first, then update one low-risk rebuilt AutoFishing or OneActionComplete product which truly requires 0.5.5, prove the real old-installed-DTMAPI block/update/recovery path, and only then update other 1.0.0 products one at a time. MoreEquipmentSlots cannot be the Canary.

2026-07-13 formal sixth-round refinement: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md` replaces the dynamic low-risk Canary choice. DTMAPI 0.5.5 has one public update; AutoFishing 1.0.0 is the compatibility/update Canary and releases alone; OneAction is the structural split template; ActionSpeed releases only after its active-gameplay GC/native-owner gate; low-user products may share a publication window while retaining independent identities/rollback; MoreEquipment releases alone.

## Source Request

The user selected plain DTMAPI `0.5.5`, selected a one-time plain-`1.0.0` epoch for every formal functional Mod, and required compatibility for existing Workshop Mods which already use DTMAPI as a prerequisite.

This review tests whether manifest comparison and names alone are enough. It inspects local subscribed package manifests and managed assembly metadata read-only; it does not copy third-party implementation, modify a subscription, or assert runtime compatibility without a real Unity Mono run.

## Compatibility Inventory

The initial local Steam subscription snapshot contained 12 functional Mod DLLs with a managed reference to `DTMAPI.Abstractions`:

- 6 reference assembly version `0.5.1.0`;
- 6 reference assembly version `0.5.2.0`;
- 10 are existing DTMAPI first-party Workshop products;
- 2 are other locally subscribed DTMAPI consumers.

2026-07-13 physical refresh: the current mutable subscription tree now contains **15** non-Runtime DLLs with a managed `DTMAPI.Abstractions` reference: 11 first-party products and 4 other public subscription packages. Nine reference `0.5.1.0` and six reference `0.5.2.0`. This supersedes the 12/10+2 count for the current local cache without rewriting the earlier scan history.

The four current external samples are:

| Workshop item | UniqueID | DLL/reference | Current classification need |
| --- | --- | --- | --- |
| `3743621104` | `com.user.dolocstorageexpansion` | `DolocStorageExpansionMod.dll` -> `0.5.1.0` | legacy minimum field and package-root DLL; prove actual DTMAPI load resolution. |
| `3743644065` | `com.user.dolocstorecapacity` | `DolocStoreCapacityMod.dll` -> `0.5.1.0` | legacy minimum field and package-root DLL; prove actual DTMAPI load resolution. |
| `3754869009` | `Codex.DolocTownQoL` | `DolocTownQoL.dll` -> `0.5.2.0` | ordinary CodeMod plus legacy package-marker ownership risk. |
| `3759797170` | `com.mxx.doloc.itemlimiter.installer` | `Mxx_DolocTownMod_Installer.dll` -> `0.5.2.0` | no declared minimum; classify installer/plugin placement and Doctor diagnostics. |

Metadata proves a binary reference, not successful runtime entry. All four therefore join the release-cutoff compatibility inventory, while package-layout or BepInEx placement remains a behavior/ownership result to verify.

Visible manifests use these framework/provider identities:

- framework registry identity `DTMAPI`;
- `DTMAPI.ModConfigMenu`;
- `DTMAPI.GameBridge.DolocTown`;
- `DTMAPI.DebugConsoleHost`.

Typical minimum requirements are `0.5.1-alpha` and `0.5.2-alpha`. Current Core comparison strips the text beginning with `-` or `+` and compares four numeric `System.Version` components, so those requirements are numerically satisfied by `0.5.5`. The same comparison exists separately in `DtmApiRuntime` and `ContentManifestRegistry`; one version service should replace the duplicate policy.

Current manifest compatibility inputs which must continue to parse include:

- canonical `MinimumDTMApiVersion` and legacy alias `MinimumApiVersion`;
- dependency `Required` and legacy alias `IsRequired`;
- each dependency's `UniqueID` and `MinimumVersion`.

## Confirmed Public API Deletion

The locally subscribed AutoFishing package at Workshop item `3743799721` contains:

```text
UniqueID: Yuuka.DTMAPI.AutoFishing
manifest version: 1.4.3-dtmapi
minimum DTMAPI: 0.5.1-alpha
provider: DTMAPI.GameBridge.DolocTown
DTMAPI.Abstractions reference: 0.5.1.0
public member reference: FishingAutomationOptions.set_StopOnManualMove
```

Current `FishingAutomationOptions` no longer declares `StopOnManualMove`. A public-surface comparison between the retained 0.5.2 Abstractions assembly and the current build, ignoring assembly-version changes, found 1,908 old public items and exactly one deletion: `FishingAutomationOptions.StopOnManualMove`; the current surface otherwise adds 85 items.

2026-07-15 exact-artifact correction: the preceding comparison did not cover the complete public surface physically present in the retained Runtime DLL. The strict tracked harness in `tests/DTMAPI.AbiCompatibilityHarness` reads the exact retained `DTMAPI.Abstractions.dll` (`SHA-256 39A51683034FF0B7495BEB3DD1C50F76F591D4E24C63872B6502EA360EF8880B`, Assembly/FileVersion `0.5.2.0`) and the current candidate while excluding assembly version from member identity. After `StopOnManualMove` was restored, it still found 92 deleted signature records, all belonging to `DTMAPI.Abstractions.ILampControlApi`, `LampManualToggleOptions`, `LampManualToggleRegisterResult`, or `LampManualToggleState`. The retained DLL's informational commit `8caf8403b45cf601c3c9791e778f2aeb859a77dc` does not contain those four types in tracked Abstractions source, while the physical DLL does. This is evidence of retained build/source drift, but it does not itself approve deletion from the published binary contract. None of the eleven retained first-party public product DLLs has a MemberRef whose parent is one of those four types. The complete signature list and per-product hashes are in `tools/release/baselines/retained-runtime-052-public-api-audit-20260715.json`; the zero-unapproved-deletion gate remains blocked pending an explicit restore-or-retire decision.

Resolution, 2026-07-15: the user selected L-A1/L-B1/L-C1/L-D1/L-E1. Update `20260715-0012` restores the four types, all 92 records, and historical defaults; registers a non-null owner-bound provider facade; and makes every call deterministically `retired-disabled` without hooks, native mutation, persistence, or dispatch. The exact host diff now has zero removed public records. Physical deletion is not a 0.5.5 option and may be reconsidered no earlier than 0.6.0 after a published warning cycle and renewed scans. Unity Mono validation remains a separate release gate.

At review time this was a real **binary compatibility blocker**: a matching provider ID and a satisfied minimum-version string could not prevent `MissingMethodException` when the old DLL configured the legacy fishing API. The host shape blocker is now cleared by Update `20260715-0012`; the no-recompile Unity Mono behavior gate remains open.

Historical source confirms the property defaulted to `true` and the published AutoFishing Mod set it to `true`. Movement cancellation itself was owned by the Mod's input/update path; the old GameBridge did not consume this property as a native policy. Therefore the least surprising compatibility restoration is:

- restore the public getter/setter with its historical default and mark it obsolete without compile failure;
- retain the supplied value in the legacy options DTO/facade;
- issue at most one owner-scoped warning that movement cancellation remains consumer policy;
- do not move movement polling back into GameBridge merely to make the restored setter appear active.

The final behavior must nevertheless be proven by loading the existing Workshop DLL, toggling it, and confirming its own movement-cancel route still works.

## Three Distinct Version Projections

The public version decision should be represented as three projections, not three competing product versions:

```text
product/manifest/BepInEx version   0.5.5
file/informational version         0.5.5.0 / 0.5.5
assembly compatibility version     keep 0.5.3.0 for the 0.5.5 line
```

The last value is an ABI binding identity, not player-facing release identity. The current root `Directory.Build.props` sets `AssemblyVersion`, `FileVersion`, and `Version` globally to the same 0.5.3 values, which also incorrectly projects the framework version onto ordinary Mod assemblies. The implementation should introduce one machine-readable DTMAPI product authority with separate generated projections, while each functional Mod owns its own 1.0.0 product version.

Do not change `DTMAPI.Abstractions` assembly identity to `0.5.5.0` as an incidental consequence of the public version update. Existing packages reference 0.5.1.0 or 0.5.2.0, and current local success against 0.5.3.0 suggests weak-name rebinding but does not prove every Unity Mono resolution path. Freeze 0.5.3.0 for this release, run the real old-DLL matrix, and add a narrowly scoped resident-assembly resolver for DTMAPI 0.5.x only if actual evidence shows it is necessary.

## Minimum-Version Policy

Release/install scripts currently rewrite a packaged Mod's `MinimumDTMApiVersion` to the current DTMAPI release value. That makes “minimum version” mean “version used to package it” and must stop.

- every formal first-party product entering the selected 1.0.0 rewrite epoch explicitly declares `MinimumDTMApiVersion: 0.5.5` in its source manifest as the supported platform baseline;
- legacy and external Mods which still run against an earlier contract retain their actual lower bound;
- a later DTMAPI packaging run must not silently raise it;
- future minimum `0.5.6` must remain correctly blocked on DTMAPI 0.5.5.

## Formal Functional-Mod 1.0.0 Epoch

The user explicitly authorized a one-time new formal-product epoch. It changes product display/version metadata, not identities:

- keep the Workshop item id;
- keep manifest `UniqueID`;
- keep config owner ids/directories and serialized keys;
- keep DLL names and API provider ids unless a separate compatibility migration says otherwise;
- synchronize manifest `Version`, official `info.json`, publish catalog, file/informational metadata, and Manager display to plain `1.0.0`.

Current local manifests show no third-party Mod-to-functional-Mod minimum dependency. The only discovered product dependency is Mine requiring `DTMAPI.OilMod >= 0.3.1`, which plain Oil `1.0.0` satisfies numerically. On current evidence, do not add a permanent legacy-version exception map for hypothetical dependencies on labels such as AutoFishing `1.4.3-dtmapi`; the compatibility promise in this decision is for Mods depending on the DTMAPI prerequisite/platform. If a real functional-Mod dependency is later found, record and migrate that concrete dependency.

## Compatibility Identities Frozen For 0.5.5

- BepInEx GUID `dev.dtmapi.bootstrap`;
- framework registry id `DTMAPI`;
- provider ids `DTMAPI.ModConfigMenu`, `DTMAPI.GameBridge.DolocTown`, and `DTMAPI.DebugConsoleHost`;
- assembly `DTMAPI.Abstractions`, its namespaces, existing public types/members, and owner-bound generic lookup shapes;
- existing published Mod `UniqueID` values;
- canonical manifest fields and the legacy aliases named above;
- install/config state schemas and paths unless a versioned reader/migration is included.

Provider lookup currently keys on the provider string plus the exact CLR API type. A display-name rename, `DtmApiStatus` entry, or source namespace alias does not create compatibility. Any future provider/public-type rename needs an actual facade or dual registration plus a warning release.

## Public External Ecosystem Cutoff

The twelve local DLLs remain a minimum regression inventory, not the complete public compatibility promise. Before the release cutoff, inventory every publicly obtainable Workshop Mod which claims DTMAPI or is actively supplied by an external author:

| Classification | Required evidence |
| --- | --- |
| Public and references a DTMAPI API/provider assembly | WorkshopID/UniqueID/source version, declared minimum, referenced API surface, no-recompile load result and scoped behavior result. |
| Public but only declares DTMAPI or uses JSON/content | Manifest/minimum/content-format compatibility; do not label it an API consumer without reference evidence. |
| External DLL installed directly under `BepInEx/plugins` | Doctor detection and non-ownership wording; no DTMAPI enable/disable/uninstall promise. |
| Author-registered test build | Record acquisition/source and add the relevant binary/behavior case. |
| Unpublished and unobtainable binary | Record unavailable status; it is not a default 0.5.5 release blocker. |

Public availability is sufficient for inclusion; author contact is not required. The estimated ecosystem size is not a fact until the cutoff scan records its date and inventory. Product Catalog rows for DTMAPI's own eleven public Workshop identities remain separate from this external Compatibility Matrix.

## Retained Player-Artifact Baseline

The 2026-07-13 focused baseline Review at `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md` records the retained DTMAPI `0.5.2-alpha` Workshop installer and all eleven first-party product packages with tree digests. The Runtime package passed the PowerShell 5.1 fake-directory install/status/log/uninstall matrix with zero blockers. Use these exact retained files as the old-player side of the 0.5.5 compatibility and recovery matrix; do not rebuild them from current source and call the result the published baseline.

This snapshot does not close the external cutoff. The subscription root is mutable and may omit public consumers; any package added or changed before release is classified by the public-external rules above.

## 0.5.5 Release Gates

1. Restore and test `FishingAutomationOptions.StopOnManualMove` before packaging 0.5.5.
2. Public API diff from retained 0.5.2 to 0.5.5 has zero unapproved deletions.
3. `0.5.1-alpha`, `0.5.2-alpha`, and `0.5.3-alpha` minimums load; a future `0.5.6` minimum is blocked.
4. All existing provider ids resolve through their existing public CLR API types.
5. Existing subscribed AutoFishing, Zoom, YConsole, and the two other local DTMAPI consumers load and enter without recompilation.
6. Existing AutoFishing toggles, configures the API, and cancels on movement without `MissingMethodException`.
7. A real Unity Mono run proves the old 0.5.1/0.5.2 Abstractions references bind to the installed compatibility assembly.
8. Every formal first-party product consistently reports 1.0.0 across manifest, official info, publish metadata, Manager, and file metadata.
9. Mine-to-Oil dependency resolution still succeeds after the epoch.
10. Updating the same `UniqueID` preserves config/owner state and produces only the expected restart-required transition.
11. The dated public external-ecosystem inventory classifies every obtainable DTMAPI claimant as API, prerequisite/content, direct-BepInEx, author-supplied, or unavailable.
12. Public/API and author-supplied external cases pass their applicable manifest/binary/behavior matrix; unavailable private binaries are explicitly non-blocking.

## Validation Boundary

This review inspected repository source/history, local Workshop manifests, and managed assembly metadata. It did not launch Doloc Town, acquire the runtime lock, rebuild a package, modify a DLL, run the old-binary matrix, or validate Unity Mono binding. The release blocker remains open until the gates above are implemented and exercised.

## 2026-07-15 Focused Resolution

- Update `20260715-0012` restored `StopOnManualMove` and the retained four-type/92-signature Lamp compatibility shell. The exact retained-to-candidate host audit now reports zero public deletions; Lamp remains Disabled and behavior-retired.
- `GAME-SMOKE/20260715-131518` loaded the exact retained AutoFishing DLL bytes without recompilation through an isolated Workshop canary under Unity Mono. Its old Entry ran, the restored setter was invoked, and Configure/F6 enablement reached the compatibility service without `MissingMethodException`.
- This closes only the retained-binary binding/Entry/configuration slice. Movement-triggered cancellation and the independent AutoFishing GC/speed ladder were not exercised and remain later behavior evidence; they must not be inferred from this run.

## 2026-07-28 External-Cutoff Resolution

Owning Update `20260727-0001` and
`GAME-SMOKE/20260728-230231` close the three Catalog-frozen external
`ActualLoadLanePending` rows. The exact retained DLLs for Workshop
`3743621104`, `3743644065` and `3754869009` each completed one Unity Mono
Workshop load, Entry and scoped behavior check against Runtime BuildCommit
`c7e1ec2f3697`, with zero compatibility failures and unchanged subscription
trees. Catalog records those exact rows as `ActualLoadLaneVerified`; this is
not a general legacy native-code or Advanced admission.
