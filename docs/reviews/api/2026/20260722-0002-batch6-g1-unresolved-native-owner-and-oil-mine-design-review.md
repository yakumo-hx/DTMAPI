# Batch 6 G1 Unresolved Native Owner And Oil/Mine Design Review

**Review ID:** `20260722-0002`
**Date:** 2026-07-22
**Status:** recorded — Oil/Mine owner decisions projected by Update `20260723-0002`; full G1 and sixth-product admission remain blocked
**Scope:** read-only review of the six candidate/deferred/unresolved G1 rows plus Oil/Mine official-JSON and economy ownership; no product admission, implementation, contract-registry mutation, game, Release, L0–L5, GC, long test or 0.5.5 publication

## Current G1 Remainder

The Phase 0 authority leaves exactly six rows outside a final decided ownership state:

| Domain | Current contract state | Real consumers | Review result |
| --- | --- | ---: | --- |
| Mine machine | candidate / `ProductNative-or-ContentOwner` | 1 | Native runtime is ProductNative; static item/equipment/recipe/group remains official JSON. No host. |
| Audio replacement host | candidate / `ContentOwnerCandidate` | 1 | Split by semantic owner; current evidence does not justify a generic host. |
| BGM replacement | candidate-deferred | 0 | Keep deferred; native lifecycle owner is incomplete. |
| Custom Entities | blocked-unresolved / retire-or-internalize | 0 | Freeze/reclassify; no native runtime host. |
| Multiplayer | deferred-independent | 0 | Outside Batch 6; no stub or API. |
| Pets and vehicles | deferred-unadjudicated | 0 | Split into independent future research domains; no combined API/host. |

Update `20260723-0002` projects the Oil/Mine decisions into the existing
machine-readable contract. This Review did not itself admit an assembly or
sixth Mod, and the projection adds no assembly.

## Mine: Static ContentOwner, Runtime ProductNative

The prototype's official JSON already owns stable static content:

- `dtmapi_mine` item/equipment/recipe identity;
- an independent `EquipmentFuncCase` with 16 storage slots and 4 items per line;
- an 8×6 footprint, native appliance threshold 10 and no base-well replacement;
- the `equipment_workbench` recipe group and Industrial tech route;
- the fallback recipe: 15 metal frameworks, 10 engine cores, 20 steel ingots and 100 coal.

Those rows should remain official JSON/ContentOwner. They do not need a public machine DTO or Content Host.

The runtime production behavior has one real consumer and product policy:

- 120 game-minute default cycle;
- 10 power per cycle;
- weighted RNG and output counts;
- due-state scheduling and archive time passage;
- product configuration and optional Oil recipe switch;
- recipe/tech mutation and visual scale containment;
- delivery into Mine-owned storage.

That state belongs in a future Mine Advanced ProductNative assembly. The current default output weights are coal 18, copper 10, iron 6 and Oil 2, with counts 1–2, 1–2, 1 and 1 respectively. The optional Oil recipe is 10 metal frameworks, 5 engine cores, 20 steel ingots and 10 crude oil. These are prototype economy vectors, not accepted balance.

Four current prototype defects must be closed before any Mine admission:

1. Native electric threshold is fixed at 10 in equipment JSON. The menu accepts a 0–100 “power per cycle” value, but runtime only invokes native `Launch()` and does not write that value to the native component. The safe initial contract is fixed JSON power 10 with read-only display, not a fictitious adjustable option.
2. `config.Enabled` appears in menu/default state but is not consulted by registration, so disabling currently does not prevent Mine registration.
3. `IncludeRuntimeModMinerals=true` is copied into a DTO but has no production reader; it must not be described as automatic runtime-mineral discovery.
4. The current GameBridge engine rewrites global `TbRecipe.InputItems` and injects tech state, while owner cleanup removes definitions/states/runtime entries without a proven original recipe/tech snapshot restoration. Same-process deactivation is therefore not yet exact.

These gaps reinforce ProductNative ownership and keep the existing Catalog entry `PrototypeBlocked`. They must not be repaired by expanding the mandatory `IMachineProductionApi` promise.

There is no second independent machine product or content pack, so the existing mandatory `IMachineProductionApi` scheduler/definition engine cannot be promoted as a generic host. A future authorized Mine migration should move single-consumer scheduling/RNG/due state/configuration with the product, retain only genuinely shared native primitives if a second consumer later proves one, and leave no always-loaded Mine engine in mandatory Runtime.

This owner decision does **not** authorize Mine as the sixth product.

## Oil: Remain Pure Official JSON

Oil is already a DLL-free official JSON ContentPack with no DTMAPI/GameBridge API or Hook dependency. Static data owns:

- `crude_oil` with `electric_energy=1500`, selling price 45 and buying price 240;
- append-only `coal_mine_drop` extension, spawn weight 25, minimum 0 and unlimited maximum;
- native world-drop, draw-count and collection-bonus semantics.

The native responsibility chain is `GuaranteedManager.SpawnResourceDropItems -> ItemSpawnInfo.SpawnItems -> ISpawnLut.SpawnInternal -> IDropItemHost`. Oil is a candidate inside the native flat drop slots, not a managed post-action backpack grant.

The verified merged LUT is coal 990 unlimited, amber 10 capped at one, then Oil 25 unlimited. Oil occupies 25/1025 of the initial interval; amber becomes 10/1025. The exact chance of at least one Oil is 7.16773117% for three draws, 9.45857052% for four draws and 8.31315084% for an equally likely three/four-draw node.

These numbers and the price/fuel values are current prototype data, not a completed economy decision. The package remains `PrototypeBlocked`; no API, Hook, ProductNative DLL or Content Host should be added. If design later requires Oil to be an independent extra roll that consumes neither coal slots nor amber probability, official flat-LUT JSON is insufficient and a new generic native resource-output owner Review is required. An Oil-specific GameBridge callback must not return.

## Audio: Split The Consumer Semantics

The present “audio replacement host” row combines different owners:

- the Manbo/paper-box short-SFX route has one independent product consumer and therefore remains product-shaped at current evidence;
- AnimalVoice is a dependency of the CustomAnimals content route, not an independent second product proving a shared Wwise owner;
- a generic author-facing audio Content Host has not been admitted.

The current Wwise callback/context implementation may remain compatibility/shared infrastructure until its own extraction, but G1 must not count these related routes as two independent consumers. A later authority update should split the row into product-local short SFX and the CustomAnimals content dependency. Only new independent content packs with the same exact event/context/lifecycle owner could reopen a generic host.

## BGM: Continue Deferred

BGM has zero real consumers. Loop ownership, STOP pairing, end callbacks, RTPC/state, bus/mixer behavior and restoration are not established. Short-SFX success does not prove music lifecycle ownership. Keep BGM out of Batch 6 implementation and expose no speculative API/schema.

## Custom Entities: Freeze, Do Not Build A Native Host

`CustomEntities.cs` contains 964 lines and 97 public types. The implemented Core service is a definition registry; all native spawn/summon/attack/drone verbs remain `runtime-creation-blocked`, and no ordinary consumer exists.

This confirms the earlier `blocked-retire-or-internalize` direction. The four mixed interfaces cannot remain `StableCandidate`; preserve their ABI for the current compatibility window, add warning-bearing Frozen/Experimental metadata in a later API Update, and retire or replace them only at an explicit breaking boundary. The working official JSON+PNG+WAV custom-animal route is a separate ContentOwner path and must not be forced through the speculative C# DTO umbrella.

## Multiplayer: Independent Project Only

No Doloc Town multiplayer model, state holder or real consumer is established. Multiplayer stays outside Batch 6. Do not create a placeholder service, API, DTO or network abstraction.

## Pets And Vehicles: Split The Domain

The combined row has no common native owner:

- the historical Motor/vehicle API is retired and points only to a native singleton owner;
- no pet follower/AI/save owner has been reviewed.

Treat pet and vehicle work as two independent future ProductNative research tracks unless focused evidence proves a different owner. Do not keep a combined public API or optional host merely because both move with the player.

## Economy Boundary

Oil and Mine must not be balanced as one hidden platform system. Oil's drop probability, fuel value and prices are official content data. Mine's cycle time, electric cost, recipe replacement and output vector are future product configuration/economy. Cross-product defaults may be compared in a design sheet or test, but neither product may mutate the other's data at runtime without an explicit user-visible contract.

Before any publication decision, economy work should separately answer:

- expected Oil per coal node across native 3–8 draw paths and collection bonuses;
- Oil's fuel/price opportunity cost against coal and other fuels;
- Mine output per in-game day at the selected cycle/power vector;
- whether the Oil recipe and Oil output create a positive feedback loop;
- how fallback coal recipe and Oil-absent installs behave.

None of those balance questions changes the physical owner decision above.

With Oil enabled, Mine's current output weight total is 36: coal 50%, copper 27.7778%, iron 16.6667% and Oil 5.5556%. Without Oil, total weight is 34: coal 52.9412%, copper 29.4118% and iron 17.6471%. Enabling Oil therefore dilutes every base mineral rather than adding an independent output. That must be an explicit product economy decision. Oil's `electric_energy=1500` remains global item/fuel data and is unrelated to Mine's native appliance threshold 10.

## Disposition

**GO** to project Mine runtime as ProductNative and keep its static content in official JSON in a later authority update; no shared host.
**GO** to keep Oil pure official JSON and treat current numbers as prototype economy inputs.
**GO** to split Audio and Pets/Vehicles into their actual semantic domains in a later G1 contract update.
**NO-GO** for BGM, Multiplayer or CustomEntities native-host implementation.
**NO-GO** for a sixth product, generic machine/audio/content host, public API expansion or 0.5.5 publication from this Review.

Related authorities:

- `tools/release/contracts/batch6-phase0-domain-contract.json`
- `docs/updates/2026/20260720-0003-batch6-phase0-closure.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- `docs/reviews/api/2026/20260713-0001-customentity-public-promise-review.md`
- `testmods/OilMod`
- `testmods/MineMod`
