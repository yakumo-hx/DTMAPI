# Frozen ABI Consumer And Optional Compatibility Host Review

**Review ID:** `20260722-0010`
**Date:** 2026-07-22
**Status:** recorded — GO for one bounded optional-host design; NO-GO for ABI deletion, a sixth Mod, or a slimming claim before extraction proof
**Scope:** Phase 2 retained-ABI consumer evidence and optional Compatibility hosting only; no implementation, package-topology change, game run, Release, L0–L5, GC, long test or 0.5.5 publication

## Question

After five ProductNative ownership rehomes, are the frozen GameBridge executors still justified by real consumers, and can they leave the default-loaded mandatory Runtime without changing the old public ABI or admitting another product?

## Method And Evidence Boundary

The audit used the local retained Workshop subscription set under `D:\Steam\steamapps\workshop\content\2285550` and inspected every DLL with a `DTMAPI.Abstractions` AssemblyRef through the repository metadata inspector. Four Runtime framework DLLs were excluded, leaving fifteen consumer assemblies: eleven retained first-party products and four external products. Hashes were compared with the tracked retained authority in `tools/release/baselines/retained-runtime-052-public-api-audit-20260715.json`.

Source and byte-string scans also covered the current admitted Advanced product sources, `references/doloc-town/own-mod-sources` and `references/third-party-mods`. Compressed or unavailable ecosystem artifacts were not treated as proof of absence. The valid conclusion is therefore “five exact known retained consumers and no known external consumer among the fifteen scanned assemblies,” not “no external consumer exists anywhere.”

No project file was changed by the scan. No game, complete Release, L0–L5, GC or long test ran.

## Exact Known Consumers

| Workshop item | Assembly / product | Size | SHA-256 | Abstractions ref | Frozen surface |
| --- | --- | ---: | --- | --- | --- |
| `3742763309` | `Yuuka.DTMAPI.ActionSpeed.dll` / `ActionSpeedMod` | 24,576 | `3169DB47BA7686B124F2BC907FED89605DF5B1BD17703C33566A147DD8D7B13A` | `0.5.2.0` | `IActionSpeedApi`, `ActionSpeedOptions`, `Configure`, `GetStatus`, and the published option setters |
| `3742763540` | `Yuuka.DTMAPI.OneActionComplete.dll` / `OneActionCompleteMod` | 16,896 | `612B173653EB22E3EB46C7882D97BB1117D308A70EF00773C8031E79DF5626E6` | `0.5.1.0` | `IActionCompletionApi`, `ActionCompletionOptions`, `Configure`, `GetStatus`, and the completion-policy setters |
| `3742763706` | `Yuuka.DTMAPI.FishBreedingAssistant.dll` / `FishBreedingAssistantMod` | 13,824 | `128EE6AD1893A8C98C0A6BC879E398371350B555E7BA47CE5D54AEE248031BEA` | `0.5.1.0` | `IItemTooltipApi`, `FishRoeTooltipOptions`, `FishRoeDisplayInfo`, `ConfigureFishRoeProvider`, `GetStatus` and their published members |
| `3742763843` | `Yuuka.DTMAPI.AnimalHusbandryProgress.dll` / `AnimalHusbandryProgressMod` | 14,848 | `E5DAA3B385AD30E9671FD9BC0407534A66390F28B327358183C65D9F68DA7530` | `0.5.2.0` | `IAnimalViewerApi`, `AnimalHusbandryProgressOptions`, `ConfigureSpecialProduceProgress`, `GetStatus` and the published option setters |
| `3743799721` | `Yuuka.DTMAPI.AutoFishing.dll` / `AutoFishingMod` | 18,432 | `E573F8CA1989663B672AF481921E4C6131C061294402F654A4062844DC5CA7FA` | `0.5.1.0` | `IFishingAutomationApi`, its strategy/options/state types, `Configure`, `SetEnabled`, `GetState`, `GetStatus` and the retained `StopOnManualMove` setter |

The other retained first-party products (`YConsole`, `Zoom`, `MoreSaves`, `ChestLocator`, `MoreEquipmentSlots`, `Manbo`) and the four scanned external assemblies have zero TypeRef/MemberRef use of these five frozen surfaces and zero Lamp use. The tracked retained audit independently records `retainedPublicProductLampMemberReferenceCount=0`.

The five current Advanced product implementations do not consume their old APIs. The remaining source consumers are compatibility QA: the AutoFishing product-absent L0 driver and the general GameBridge QA fixture. They are test authorities, not ordinary product reasons to keep the executor default-loaded.

## Physical Weight At The Audit Checkpoint

The tracked C# count at `025fdf3e`, excluding `bin` and `obj`, was:

| Scope | Physical lines | Non-empty lines |
| --- | ---: | ---: |
| Five mandatory Runtime projects | 76,302 | 68,225 |
| GameBridge | 37,722 | 33,386 |
| ActionCompletion Compatibility | 696 | 605 |
| ActionSpeed Compatibility | 1,485 | 1,267 |
| FishingAutomation Compatibility | 4,164 | 3,697 |
| FishRoeTooltip Compatibility | 417 | 364 |
| AnimalViewer Compatibility | 1,149 | 1,027 |
| Five heavy executors combined | 7,911 | 6,960 |
| Lamp disabled shell | 63 | 57 |

The five heavy executors are 10.37% of mandatory physical C# and 20.97% of GameBridge physical C#. Including Lamp gives 7,974 physical / 7,017 non-empty lines. These are gross extraction candidates, not a promised net deletion: thin provider proxies, owner binding, broker/lifecycle forwarding and genuinely shared native closure glue must remain.

The current Release GameBridge DLL is 1,084,928 bytes and all five Runtime DLLs total 2,381,824 bytes. No split build was produced, so LOC proportions must not be presented as projected DLL savings.

## Constraints That Rule Out A Directory Move

The retained products call `GetApi<T>("DTMAPI.GameBridge.DolocTown")`. That provider UniqueID, every public interface/DTO signature and owner-bound behavior must remain unchanged for the 0.5.5 window.

Today the compatibility features register during GameBridge construction and depend on internal runtime machinery: `HarmonyReflectionPatcher`, native reflection helpers, the experimental bridge's internal helpers, feature lifecycle dispatch, demand coordination, owner cleanup and shared AgentState/ToolCollider routes. Moving the existing directories into another assembly without a broker contract would either create a mandatory static AssemblyRef or break those internal dependencies.

Harmony identity is also compatibility state:

- Fishing must retain `dtmapi.gamebridge.doloctown.fishingcompatibility`.
- The other frozen routes currently use the general `dtmapi.gamebridge.doloctown` owner; they must not perform a broad exact-owner unpatch that would remove unrelated GameBridge patches.
- All five managed product owner strings and product-first, compatibility-first and already-physically-installed conflict behavior remain fail-closed.

## Decision: One Optional Framework Host

The audit approves a bounded design for one `netstandard2.0` optional framework assembly, provisionally `DTMAPI.GameBridge.DolocTown.Compatibility.dll`.

It is not a Mod or sixth product: it has no manifest, Workshop identity or `BepInPlugin`. Mandatory GameBridge retains the existing provider ID, five thin public proxies, owner-bound facades and a broker, so old `GetApi<T>` calls remain non-null. Mandatory Runtime must have zero static AssemblyRef to the host.

The first actual call into any frozen surface may load package-owned bytes from a dormant path that BepInEx does not auto-scan. Before `Assembly.Load(byte[])`, the broker must validate the existing generic optional-component record: expected relative path, length, hash, simple name and assembly version. It then invokes one well-known internal factory and publishes the fully constructed backend atomically. Missing bytes, identity/hash mismatch, factory failure or incomplete preflight remain stable fail-closed states with zero demand, Hook or callback root.

Host activation itself installs no Hook. Existing per-domain `Configure`/`SetEnabled` paths still own atomic hook activation, demand, native restoration and product-owner exclusion. Mandatory lifecycle forwarding occurs only after a backend is loaded and remains isolated per domain for Update, SaveLoaded, ReturnedToTitle, EnvironmentReset and owner cleanup.

Mono cannot unload the loaded assembly. After the last consumer stops, owner/demand/native state must clear, but the host remains process-resident and must not be reported as unloaded. Already installed compatibility Hooks retain their current process-pinned semantics.

Lamp has no known real consumer and only a 63-line stateless disabled shell. Keep it in mandatory GameBridge through 0.5.5 rather than loading the heavy host for an unused disabled ABI. Any 0.6 removal requires its own warning/scan gate.

## Honest Size Claims

Shipping the dormant host inside the Runtime package can reduce default-loaded GameBridge code and ordinary process roots. It does not reduce Workshop download bytes. A not-shipped/on-demand-download design would require a new offline-deterministic installation transaction and is not approved by this Review.

Metadata pre-scan may improve Doctor/Manager guidance, but it cannot be the correctness gate because reflection-based consumers may have no static TypeRef. The provider proxy must activate on actual API use.

## Required Gates Before Implementation Can Be Accepted

1. Update the Batch 6 topology authority: the current 0.5.5 player tree is exactly five Runtime DLLs. A sixth framework DLL is not a sixth Mod, but still changes package identity and requires bounded authority.
2. Choose and state `dormant-shipped` versus `not-shipped`. Only the former is approved here, and it cannot claim download-size reduction.
3. Extend the existing Catalog/release-manifest/installer/check/status/uninstall/collect-logs machinery through one data-driven optional-component row. Do not create product branches or a second receipt family.
4. Keep the public Abstractions ABI byte/signature compatible and add exact retained-binary coverage for all five hashes, provider identity and required MemberRefs. The existing strongest member-level canary is AutoFishing and is not sufficient alone.
5. Test each domain's product-first, compatibility-first and physical-owner conflict order; owner deactivation, save/title/environment lifecycle and restart semantics must remain exact.
6. Prove the no-consumer process never loads the host and has zero compatibility demand, Hook, callback and owner roots.
7. Prove extraction leaves no heavy executor bodies/symbols in mandatory GameBridge while honestly listing retained broker/shared-hook glue. Moving code between loaded assemblies is not itself a net-source reduction.
8. Run a later bounded Unity Mono proof that BepInEx ignores the dormant path and byte-loaded dependencies resolve. This audit did not run it.
9. Do not promise 0.6 deletion while the five exact published consumers exist. A warning-bearing preview, fresh scan, migration guidance and explicit breaking-release decision are still required.

## Disposition

**GO** for one optional Compatibility Host design and a later separately authorized implementation Update.
**NO-GO** for deleting or changing the frozen public ABI, calling the host a sixth Mod, adding five host assemblies, or claiming package/download slimming from a dormant-shipped split.
**NO-GO** for implementing a sixth ProductNative Mod while the current Batch 6 authority remains closed.

Related authorities:

- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md` Phase 2
- `docs/api/public-api-matrix.md`
- `docs/updates/2026/20260715-0012-retained-autofishing-abi-host-gate.md`
- `tools/release/baselines/retained-runtime-052-public-api-audit-20260715.json`
- `docs/reviews/code/2026/20260722-0004-actionspeed-weight-and-next-product-audit.md`
- `docs/reviews/code/2026/20260722-0009-five-product-update-continuation-audit.md`
