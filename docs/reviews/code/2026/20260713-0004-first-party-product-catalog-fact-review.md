# 20260713-0004 First-Party Product Catalog Fact Review

Status: recorded / F1 selected / Catalog implementation open
Date: 2026-07-13
Scope: first-party product/distribution facts, release-definition drift, fixture boundaries, and the minimum future Catalog model
Related decision docket: `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0002-second-round-closure-third-decision-docket.md`
Refines: `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`

## Source Request

The user selected Catalog-first gradual migration in the second round and asked to continue the third decision round. Before asking which unpublished prototypes become planned products, the existing public distribution history must be separated from stale `Published` / `DeveloperOnly` script labels.

This is a read-only source/local-state review. It does not create, update, subscribe, unsubscribe, upload, move, package, install, or reclassify a Mod in code.

## Current Script Lists Are Not One Authority

`Get-DtmApiPublishedModDefinitions` hard-codes eight products:

- Zoom;
- ActionSpeed;
- OneActionComplete;
- ChestLocatorEnhancer;
- DebugConsole / Y-key console;
- FishBreedingAssistant;
- MoreSaves;
- AnimalHusbandryProgress.

`Get-DtmApiDeveloperOfficialModDefinitions` contains those eight and adds seven entries:

- AutoFishing;
- Oil;
- Mine;
- MoreEquipmentSlots;
- StrongPlantingGun;
- CropHarvestingQA;
- ManboCardboardAudio.

Default developer deployment excludes the explicit QA entry, which explains the common 14-package installed release receipt; explicit QA makes 15. AutoHarvest appears in publish description metadata but in neither release definition, so it is a release-list orphan. Hello, ConfigMenuExample, HookProbe, and BrokenManifest use separate sample/QA/negative paths.

Only AutoFishing and Zoom are physically under `first-party-mods`; the other product sources remain under `testmods`. This is the C2 migration problem, not evidence that those products are tests.

## Public Distribution Is A Fact, Not A Maturity Rating

Read-only local subscription/upload evidence identifies eleven DTMAPI functional products with established Workshop identities:

| Product | UniqueID | Workshop item |
| --- | --- | ---: |
| Zoom | `DTMAPI.ZoomMod` | `3742717440` |
| Y-key console | `DTMAPI.DebugConsoleMod` | `3742714442` |
| MoreSaves | `DTMAPI.MoreSavesMod` | `3742763050` |
| ActionSpeed | `Yuuka.DTMAPI.ActionSpeed` | `3742763309` |
| OneActionComplete | `Yuuka.DTMAPI.OneActionComplete` | `3742763540` |
| FishBreedingAssistant | `Yuuka.DTMAPI.FishBreedingAssistant` | `3742763706` |
| AnimalHusbandryProgress | `Yuuka.DTMAPI.AnimalHusbandryProgress` | `3742763843` |
| ChestLocatorEnhancer | `DTMAPI.ChestLocatorEnhancerMod` | `3742765514` |
| AutoFishing | `Yuuka.DTMAPI.AutoFishing` | `3743799721` |
| MoreEquipmentSlots | `DTMAPI.MoreEquipmentSlotsMod` | `3744059735` |
| ManboCardboardAudio | `Yuuka.DTMAPI.ManboCardboardAudio` | `3746319981` |

AutoFishing, MoreEquipmentSlots, and Manbo are therefore already public products even though current source metadata calls them `DeveloperOnly`. An Experimental backend, a high-risk rebuild, or a stale script label can make a release ineligible; it cannot rewrite public distribution history into `Prototype`.

The eleven-product inventory is a local fact base. Batch 0 must still verify current live Workshop ownership/name/version before it writes the authoritative Catalog or changes an upload lane.

## AutoHarvest Identity Must Not Be Merged

The current workspace sample is:

```text
UniqueID: Yuuka.DTMAPI.AutoHarvest
Role: ApiDemandSample
Distribution: NeverPublish
```

The older Workshop item `3742771572` is `None.AutoHarvest`, uses the predecessor `Content/DolocSMAPI` layout, and has a different DLL/identity/history. It is compatibility/reference material, not the current sample's Workshop identity.

Do not assign that Workshop id to `Yuuka.DTMAPI.AutoHarvest`, do not import the old implementation into the clean rebuild, and do not turn the current sample into a first-party product. If the old item is controlled by the user, its retirement notice is a separate legacy-distribution task.

## Catalog Must Be Multi-Axis

One mutually exclusive `Published / DeveloperOnly` enum cannot express “already public but Experimental” or “prototype today with a future promotion target.” The minimum Catalog needs separate fields such as:

| Axis | Example values |
| --- | --- |
| `Role` | `PublishedProduct`, `PlannedProduct`, `Prototype`, `ApiDemandSample`, `QaFixture`, `Example`, `NegativeFixture`, `CompatibilityComponent` |
| `ProductType` | `FunctionalCodeMod`, `OfficialJsonContent`, `Diagnostic`, `Fixture`, `InternalAdapter` |
| `DistributionState` | `PublicWorkshop`, `LocalDeveloper`, `None`, `LegacyExternal` |
| `ReleaseEligibility` | `Eligible`, `RebuildBlocked`, `PrototypeBlocked`, `NeverPublish`, `CompatibilityOnly` |
| `Maturity` | `ProtectedCurrent`, `RebuildPending`, `Experimental`, `Prototype`, `Retired` |

Every product row must also own/freeze its UniqueID, official folder, Workshop id, config and sidecar identities, current public version/history, target 1.0.0 epoch, source path, package DLL, and truthful `MinimumDTMApiVersion`.

## Fact-Derived Baseline

The following classifications do not need a new product choice:

- the eleven identities above are `PublishedProduct / PublicWorkshop`; their next release can still be `RebuildBlocked`;
- Y-key console is a published `Diagnostic` product, not a QA fixture;
- AutoHarvest is `ApiDemandSample / NeverPublish`;
- CropHarvestingQA and HookProbe are `QaFixture`;
- HelloDtmMod and ConfigMenuExample are `Example`;
- BrokenManifestMod is `NegativeFixture`;
- the frozen fishing and failed camera compatibility adapters plus old consumer DLLs are `CompatibilityComponent`, not product owners.

## Remaining Product Choices

### F1 - fact-first conservative roster

- StrongPlantingGun becomes `PlannedProduct / RebuildBlocked`;
- Oil remains `Prototype / OfficialJsonContent` with a `PlannedProduct` promotion target until economy/package identity is fixed;
- Mine remains `Prototype` with a `PlannedProduct` promotion target until art, preview, electricity, mixed JSON/code blocking, cycle, and economy gates close;
- old `None.AutoHarvest` receives only a separate legacy/retirement record; the current sample never publishes.

This is the recommendation.

### F2 - roadmap-forward roster

Mark StrongPlantingGun, Oil, and Mine as `PlannedProduct` immediately, while using `Maturity=Prototype` and `ReleaseEligibility=PrototypeBlocked` for Oil/Mine.

This expresses a firmer product commitment but makes long-term intent look closer to the current release lane.

### F3 - minimum commitment roster

Keep every non-public Mod as `Prototype` until its 1.0.0 rebuild starts.

This minimizes promises but understates the already bounded StrongPlantingGun product direction and makes the Catalog weaker as a roadmap owner.

MoreEquipmentSlots and Manbo are not choices in F1-F3: they are public-product facts. MoreEquipmentSlots should be rebuilt late because of save/recovery/UI/shield risk. Manbo remains a published product with an Experimental short-SFX backend unless a separate sunset decision is made.

The user selected **F1** on 2026-07-13. This closes the initial roster intent but does not create the Catalog, verify the live Steam service, publish a prototype, or change any current release lane.

## Acceptance Boundary

The future Catalog gate must fail when release/build/install/publish scripts contain a product not in the Catalog, disagree on frozen identity, package a `NeverPublish`/fixture entry in a player lane, or label a known public Workshop product as developer-only. It must not mutate Workshop or local packages merely because it detects drift.

## First-Party Catalog Versus External Compatibility Matrix

The eleven locally evidenced public Workshop identities in this Review are DTMAPI first-party product facts and belong to the Product Catalog. The formal 0.5.5 scan of publicly obtainable Mods which claim DTMAPI is a separate external Compatibility Matrix owned by `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`.

Do not combine the first-party count with an estimated external ecosystem count. A live external scan records WorkshopID/UniqueID, availability, actual API reference versus prerequisite/content-only use, minimum version and test result. Its findings may add compatibility cases, but they do not silently reclassify first-party product identity or distribution history.

## 2026-07-13 Retained Subscription Snapshot

The focused baseline Review `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md` physically rechecked the subscription root and captured manifest/version/tree hashes. The final snapshot contains DTMAPI plus all eleven Catalog identities. ChestLocatorEnhancer arrived during the audit after being absent from the preliminary check, which confirms that presence is current cache evidence rather than immutable Catalog ownership. The eleven identities remain the product fact; the subscription snapshot is one regression artifact set and does not replace the live release-cutoff scan.
