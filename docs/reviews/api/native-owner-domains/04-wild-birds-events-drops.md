# 04 - Wild Birds Events Drops

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Wild bird behavior: birds are small outdoor events, can refresh in the wild, get scared and fly away, drop seeds, support texture replacement, custom drops, and new bird types.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Partial for texture replacement categories | `018_*` beauty docs | Existing texture paths only |
| Base content mod | Partial for drop tables/items | `026_*`, `027_*`, `046_*` | Drop data, not runtime bird AI |
| Advanced content mod | Not confirmed for new bird behavior | Official docs checked | No dedicated bird behavior pipeline |
| Runtime behavior mutation | Not public | No official bird event API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Wild bird event behavior | `EnvObjectBird`, `EnvObjectBird.OnRender`, `EnvObjectBird.OnTouch`, `EnvObject.Host`, `IDropItemHost.CreateDropItemAnimated`, `Config.Resource.EnvObjectType.BIRD` | `EnvObjectBird.cs`; `maps/Resource_Gathering.md` | `EnvObjectBird` plus env-object host | Bird render/touch/flee event and animated item drop; birds are env-object events, not livestock | Medium/high event-object risk | `IWildBirdApi.ReadOnly`, `IBirdEventApi.Experimental` | Found for existing bird event |
| Bird spawn pipeline | `RoomSpawnInfo.EnvObjectSpawnEntry`, `IEnvObjectHost.RefreshEnvObjects`, `EnvObjectManager.CreateEnvObject`, `TbEnvObject`, `RoomProto.envObjectGenInfo` | metadata CSVs; resource maps | Room/env-object spawn system | Room refresh and env-object creation for built-in env object types | High room/spawn coupling | Built-in env-object spawn metadata only | Partial |
| Bird drops | `Config.Time.SeasonInfo.BirdDropSpawnEntry`, `Config.Item.ItemSpawnRandom` | `SeasonInfo.cs`; official drop docs; metadata CSVs | Season config drop table | Seasonal random drop selection at bird touch/drop time | Medium for content-time table influence, high for runtime edits | `IBirdDropTableApi` content-time influence; runtime mutation fragile/global | Partial |
| Bird texture/appearance | `EnvObjectBird`, `EnvObjectInfo`, `Config.Resource.EnvObjectType.BIRD` | metadata CSVs; beauty docs | Env object config/render | Existing bird appearance and env-object type; bird-specific official replacement evidence still weak | High for runtime mutation | `IEnvObjectAppearanceApi` only after asset proof | Partial |
| New bird spawn/behavior | `RoomProto.envObjectGenInfo`, `RoomSpawnInfo.EnvObjectSpawnEntry`, `EnvObjectManager`, `EnvObjectBird`, `EnvObjectType.BIRD` | metadata CSVs; resource maps | Room/env-object spawn system and native class/enum dispatch | Built-in bird behavior may be reusable through config, but custom bird behavior is native class/enum owned | High/blocking | `IWildBirdContentApi.Proposed` for built-in behavior only | Blocked for custom behavior |

## API Translation Notes

- Treat birds as env-object events, not livestock or NPCs.
- Stable API should begin with read-only bird/drop table metadata and content-pack style overrides.
- Runtime spawn/scare/drop mutation needs lease/restore behavior and room/env-object host proof.
- Round 3 confidence: bird as environment event 92; spawn pipeline 86; touch/flee/drop owner 90; seasonal drop table 84; texture/appearance replacement 58; new built-in-behavior bird via config 62; custom bird behavior 12; runtime drop-table mutation stable API 20.

## Blockers And Follow-Up

- No dedicated official new-bird behavior path was found.
- Runtime drop table edits need save/load and season-change restore policy.
- New bird behavior must prove env-object manager registration, room spawn config, render assets, and drop integration.
- Custom drops must distinguish season-table/content-time influence from per-bird runtime custom behavior.

## Evidence Checked

Maps: `Resource_Gathering.md`, `Assets_Content.md`.
Classes/symbols: `EnvObjectBird`, `EnvObject`, `IDropItemHost`, `SeasonInfo.BirdDropSpawnEntry`, `ItemSpawnRandom`, `EnvObjectInfo`, `RoomSpawnInfo`, `EnvObjectManager`.
