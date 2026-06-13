# 03 - Animal Husbandry Behavior

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Livestock and animal behavior, including eating, defecation, breeding, products, buy/sell, and animal movement.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Yes for existing small animal textures | `013_*`, `018_*`, `044_*` | Texture/id support only |
| Base content mod | Partial for item/package/store content | `045_*`, `046_*` | Does not prove runtime animal creation |
| Advanced content mod | Not confirmed for full animal lifecycle | Official docs checked | No save/home/AI owner guarantee |
| Runtime behavior mutation | Not public | No official animal AI API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Animal core state/docs | `Animal`, `AnimalData`, `AnimalDocumentInfo`, `DolocAPI.QueryAnimalDocument`, `GetAnimalAllProduce`, `Animal.HusbandryInfo`, `AgeInfo`, `Mood`, `Energy`, `Metabolism` | `maps/Assets_Content.md`; `Animal.cs` | `Animal` and persisted animal data | Instance state, documents, produce metadata | Medium/high raw object risk | `IAnimalStateSnapshotApi` | Found for existing animals |
| Existing animal instance creation/release | `AnimalManager.CreateAnimal`, `IAnimalHost.CreateAnimal`, `IAnimalHost.AddAnimal`, `IAnimalHost.RemoveAnimal`, `ItemAnimalPackage.TryReleaseAnimal`, `AnimalSystem`, room `DM_animal` | `AnimalManager.cs`; `ItemAnimalPackage.cs`; room/animal host classes | `Room` as `IAnimalHost`, `AnimalManager`, `AnimalSystem` | Native creation, registration, host membership, release/catch path for existing animal types | High host/save timing risk | Experimental existing-animal instance operation only | Partial |
| Feeding | `Animal.IsHungry`, `NeedMetabolism`, `RefreshMetabolismFlag`, `IFeeder.TakeFeeds`, `AnimalAIState.FindFoodInCurrentEnv`, `AnimalWork_SearchFoodToEat.GenTask`, `Feeder` | `maps/Assets_Content.md`; `maps/Action_Interaction.md`; feeder classes | `AnimalAI` and feeder interfaces | Hunger/metabolism and feeder task selection | High AI internals | `IAnimalCareApi.FeedStatus` | Partial |
| Defecation | `Animal.ShouldExcrete`, `DEBUG_ResetExcreteInterval`, `IAnimalToilet.Excrete`, `AnimalWork_SearchToiletToExcrete.GenTask`, `Toilet` | metadata CSVs; animal/toilet classes | Animal state plus toilet task | Waste interval and toilet interaction | High; debug method not API | `IAnimalCareApi.WasteStatus` | Partial |
| Breeding | `Animal.NeedBreed`, `CanBreedNow`, `_UpdateBreeding`, `_Breed`, `TryCreateNewAnimal`, `IAnimalLivestockNursery.StartBreed`, `AnimalWork_SearchLivestockNurseryToBreed.GenTask`, `LivestockNursery` | `maps/Assets_Content.md`; metadata CSVs | Animal breeding state and nursery | Breeding eligibility, nursery use, child creation | High private creation path | `IAnimalBreedingApi.Experimental` | Partial |
| Animal product eligibility | `AnimalHusbandryData.Output`, `Output_Ref`, `AnimalInfo.ProduceSpawnEntry`, `Animal.IsMoodSatisfiedProduce`, `Animal.ProduceAsItems` | `maps/Assets_Content.md`; metadata CSVs | Animal state and produce config | Product eligibility and animal-owned output definition | Medium/high | `IAnimalProductEligibilityApi` | Partial |
| Product collection tools/machines | `IAnimalMilkingMachine.Produce`, `IAnimalLintRoller.Produce`, `IAnimalHoneyComb.ProduceHoney` | machine/tool classes; metadata CSVs | Machine/tool collection interfaces | Collection transaction separate from animal eligibility | Medium/high | `IAnimalProductCollectionApi` | Partial |
| Buy/sell/package | `ItemAnimalPackage`, `ItemAnimalPackage.TryReleaseAnimal`, `Config.Item.ItemFunctionAnimalPackage.PresetAnimal`, `PresetAnimal_Ref`, `StoreManager`, `Store`, `ExchangeStore` | `maps/Shop.md`; official shop docs | Item/package/store path | Animal acquisition appears item/package driven, but sale/removal transaction remains weakly traced | High | `IAnimalAcquisitionApi.Proposed` | Partial |
| Movement/location | `Animal.CallToRoom`, `EnterRoom`, `SetCurrentRoom`, `SetHomeRoom`, `AnimalController.TryGetAnotherRoom`, `AnimalPathFinderForRoom.FindPath`, `AnimalRoomEnv.Refresh/GetFeeders/GetToilets` | `maps/Assets_Content.md`; `maps/Resource_Gathering.md` | `Animal`, `AnimalController`, `AnimalRoomEnv` | Room membership, home room, pathing, environment search | High room/path coupling | `IAnimalLocationApi`; experimental movement adapter | Partial |
| New animal species/content | `Animal` constructors, `AnimalInfo`, `AnimalDocumentInfo`, `ItemAnimalPackage`, `IAnimalHost`, `Room`, `AnimalSystem` | metadata CSVs; public matrix | Distributed native animal system | Native creation path exists for known animals, but custom proto/assets/save/render/AI timing are unproven | Blocking for stable API | Custom animal experimental preload/content research only | Blocked for stable API |

## API Translation Notes

- Public APIs should return animal snapshots and opaque animal IDs, not native `Animal`.
- Produce and care APIs should expose item ids/counts and status summaries.
- Runtime creation, forced breeding, and movement must stay experimental/blocked until home-room, host, save/load, and AI task lifecycles are reviewed.
- Existing animal instance creation is different from adding a new animal species. Round 3 confidence: existing animal native creation/release/catch 86; persistence/room host 84; feeding/excretion/breeding 70; product eligibility 80; product collection 82; buy/sell/package 66; movement/pathing 64; custom animal stable API 20; experimental native path 66.

## Blockers And Follow-Up

- Sale/removal owner is not fully traced.
- Child creation persistence and nursery transaction behavior need method-body review.
- New animal content must prove config, document, package, host, AI, renderer, and save integration.
- Movement/location must remain partial until task/pathing lifecycle is proven beyond room registration and host membership.

## Evidence Checked

Maps: `Assets_Content.md`, `Action_Interaction.md`, `Resource_Gathering.md`, `Shop.md`.
Classes/symbols: `Animal`, `AnimalManager`, `AnimalController`, `AnimalAI`, `IFeeder`, `Toilet`, `LivestockNursery`, `MilkingMachine`, `LintRoller`, `HoneyComb`, `ItemAnimalPackage`.
